"""Read literal defaults and method signatures; does not decompile or run native code."""
from pathlib import Path
import zipfile,struct,json,importlib.util,re
ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('metadata_reader',ROOT/'Design/Inspection-Levels01-50/read_reference_metadata.py')
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
with zipfile.ZipFile(ROOT/'Design/Backup/apk/royal_thron_rfv387.apk') as z:
    data=z.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
md=module.Metadata.__new__(module.Metadata);md.b=data
assert struct.unpack_from('<II',data)==(0xfab11baf,31)
md.strings,md.string_size=md.pair(24);md.types_offset,md.types_size=md.pair(160)
md.methods_offset,_=md.pair(48);md.parameters_offset,_=md.pair(88);md.fields_offset,_=md.pair(96);md.values_offset,_=md.pair(72)
md.field_defaults=md.defaults(64);md.parameter_defaults=md.defaults(56)
types=[];primitives={}
for i in range(md.types_size//88):
    a=struct.unpack_from('<16I8H2I',data,md.types_offset+i*88);name=md.string(a[0]);ns=md.string(a[1])
    if ns=='System' and name in ['Single','Int32','Boolean','Double']:primitives[a[2]]=name
    if ns.startswith('Royal.') and re.search('Rocket|Propeller|Tnt',name) and not re.search('Machine|SpaceMission|Fireworks|Offer|Event|Rush|Icon|Reward|Tooltip|Tutorial|Prelevel|Magic|SuperDuke',name):types.append((i,a,name,ns))
def default(entry):
    ty,index=entry;raw=data[md.values_offset+index:md.values_offset+index+4]
    result=dict(typeIndex=ty,dataIndex=index,raw=raw.hex(),primitiveType=primitives.get(ty))
    if primitives.get(ty)=='Single':result['value']=struct.unpack_from('<f',raw)[0]
    elif primitives.get(ty)=='Int32':result['value']=md.integer_default(index)
    elif primitives.get(ty)=='Boolean':result['value']=bool(raw[0])
    return result
rows=[];constants=[]
for i,a,name,ns in types:
    row=dict(typeIndex=i,name=name,namespace=ns,fields=[],methods=[])
    for fi in range(a[8],a[8]+a[18]):
        ni,ty,token=struct.unpack_from('<III',data,md.fields_offset+fi*12);f=dict(name=md.string(ni),fieldIndex=fi,typeIndex=ty)
        if fi in md.field_defaults:
            f['default']=default(md.field_defaults[fi]);constants.append(dict(type=name,field=f['name'],**f['default']))
        row['fields'].append(f)
    for mi in range(a[9],a[9]+a[16]):
        ma=struct.unpack_from('<7I4H',data,md.methods_offset+mi*36);m=dict(name=md.string(ma[0]),methodIndex=mi,parameters=[])
        for pi in range(ma[4],ma[4]+ma[-1]):
            ni,token,ty=struct.unpack_from('<III',data,md.parameters_offset+pi*12);p=dict(name=md.string(ni),typeIndex=ty)
            if pi in md.parameter_defaults:p['default']=default(md.parameter_defaults[pi])
            m['parameters'].append(p)
        row['methods'].append(m)
    rows.append(row)
(OUT/'booster-metadata.json').write_text(json.dumps(dict(primitiveTypeIndices=primitives,types=rows),indent=2),encoding='utf8')
(OUT/'booster-constants.json').write_text(json.dumps(constants,indent=2),encoding='utf8')
print('TYPES',len(rows),[r['name'] for r in rows]);print('CONSTANTS',json.dumps(constants,indent=2))
