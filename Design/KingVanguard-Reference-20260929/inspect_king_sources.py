"""Read-only source inspection; outputs are confined beside this script."""
from pathlib import Path
import collections, contextlib, hashlib, importlib.util, io, json, re, struct, sys, zipfile
sys.stdout.reconfigure(encoding='utf8')

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[1]
OLD=ROOT/'Design/Backup/Reference-UI-Characters/20260923-184326'
sys.path.insert(0,str(OLD/'Tools'))
from inspect_scope import objects,string
from spine38 import Reader

def digest(b):return hashlib.sha256(b).hexdigest()
def save(name,data):
    (OUT/name).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf8')
def summary(j):
    attachments=[dict(skin=s['name'],slot=j['slots'][int(i)]['name'],name=n,type=a['type'],path=a.get('path'),weighted=a.get('vertices',{}).get('weighted',False)) for s in j['skins'] for i,aa in s['attachments'].items() for n,a in aa.items()]
    return dict(version=j['version'],bounds=j['bounds'],fps=j.get('fps'),bones=len(j['bones']),bone_names=[b['name'] for b in j['bones']],slots=len(j['slots']),slot_names=[b['name'] for b in j['slots']],skins=[s['name'] for s in j['skins']],attachment_type_counts=dict(collections.Counter(a['type'] for a in attachments)),attachments=attachments,ik_constraints=len(j['ik']),transform_constraints=len(j['transformConstraints']),path_constraints=len(j['pathConstraints']),events=j['events'],animations=[dict(name=n,duration=a['duration'],bone_timelines=len(a['bones']),slot_timelines=len(a['slots']),deform_timelines=len(a['deform']),draw_order_keys=len(a['drawOrder']),events=a['events']) for n,a in j['animations'].items()])

def main():
    old=(OLD/'Characters/King/Original/King.skel').read_bytes()
    j=json.loads((OLD/'Characters/King/Recovered/King.spine38.json').read_text(encoding='utf8'))
    save('old-king-summary.json',dict(sha256=digest(old),bytes=len(old),**summary(j)))
    inspection=ROOT/'Design/Inspection-rfv387'
    matches=[];skeletons=[];all_assets=[]
    for folder in ('data.unity3d_data','datapack.unity3d_data'):
        for file in sorted((inspection/folder).iterdir()):
            if file.is_dir() or file.suffix=='.resS':continue
            try:data,rows,meta=objects(file)
            except Exception as ex:
                print('SKIP',file.name,type(ex).__name__,str(ex));continue
            print(file.name,len(rows))
            for row in rows:
                row=dict(row,file=str(file.relative_to(ROOT)))
                if row['name']:all_assets.append(row)
                if re.search(r'(?i)king|kral|robert',row['name']):matches.append(row)
                if row['class_id']!=49:continue
                raw=data[row['byte_offset']:row['byte_offset']+row['size']]
                name,p=string(raw,0)
                if p+4>len(raw):continue
                length=struct.unpack_from('<I',raw,p)[0];payload=raw[p+4:p+4+length]
                if b'3.8.' not in payload[:100]:continue
                rec=dict(row,payload_bytes=len(payload),sha256=digest(payload),identical_old_king=payload==old)
                try:
                    with contextlib.redirect_stdout(io.StringIO()):parsed=Reader(payload).read()
                    rec['skeleton']=summary(parsed)
                    if re.search(r'(?i)king|kral|robert',name):
                        (OUT/(name+'.skel')).write_bytes(payload)
                        save(name+'.spine38.json',parsed)
                except Exception as ex:rec['parse_error']=repr(ex)
                skeletons.append(rec)
                print('SPINE',name,len(payload),rec.get('skeleton',{}).get('bones'),rec.get('skeleton',{}).get('slots'),rec.get('identical_old_king'),rec.get('parse_error',''))
                if rec.get('skeleton') and re.search(r'(?i)king|kral|robert',name):
                    print('ANIMATIONS',[(a['name'],a['duration']) for a in rec['skeleton']['animations']])
    save('rfv387-king-named-objects.json',matches)
    save('rfv387-spine-skeletons.json',skeletons)
    save('rfv387-named-assets.json',all_assets)
    with zipfile.ZipFile(ROOT/'Design/Backup/apk/royal_thron_rfv387.apk') as z:
        save('apk-king-named-entries.json',[dict(name=i.filename,bytes=i.file_size) for i in z.infolist() if re.search(r'(?i)king|kral|robert',i.filename)])
        meta=z.read(next(i.filename for i in z.infolist() if i.filename.endswith('/global-metadata.dat')))
        spec=importlib.util.spec_from_file_location('read_reference_metadata',ROOT/'Design/Inspection-Levels01-50/read_reference_metadata.py')
        module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
        md=module.Metadata.__new__(module.Metadata);md.b=meta
        md.strings,md.string_size=md.pair(24);md.types_offset,md.types_size=md.pair(160);md.methods_offset,_=md.pair(48);md.parameters_offset,_=md.pair(88);md.fields_offset,_=md.pair(96)
        types=[]
        for i in range(md.types_size//88):
            a=struct.unpack_from('<16I8H2I',meta,md.types_offset+i*88);name=md.string(a[0]);ns=md.string(a[1])
            if not ns.startswith('Royal.') or not re.search(r'(?i)king|kral|robert',ns+'.'+name):continue
            t=dict(name=name,namespace=ns,index=i,fields=[],methods=[])
            for fi in range(a[8],a[8]+a[18]):
                ni,ty,tok=struct.unpack_from('<III',meta,md.fields_offset+fi*12);t['fields'].append(dict(name=md.string(ni),type_index=ty))
            for mi in range(a[9],a[9]+a[16]):
                ma=struct.unpack_from('<7I4H',meta,md.methods_offset+mi*36);m=dict(name=md.string(ma[0]),parameters=[])
                for pi in range(ma[4],ma[4]+ma[-1]):
                    ni,tok,ty=struct.unpack_from('<III',meta,md.parameters_offset+pi*12);m['parameters'].append(dict(name=md.string(ni),type_index=ty))
                t['methods'].append(m)
            types.append(t)
        save('rfv387-king-metadata-types.json',types)
        print('KING TYPES',[(t['namespace'],t['name']) for t in types])
        strings=[x.decode('utf8','replace') for x in meta[md.strings:md.strings+md.string_size].split(b'\0')]
        save('rfv387-king-metadata-strings.json',[dict(index=i,value=s,context=strings[max(0,i-3):i+4]) for i,s in enumerate(strings) if re.search(r'(?i)king|kral|robert',s)])
    print('MATCHES',[(r['class_id'],r['name'],Path(r['file']).name) for r in matches])

if __name__=='__main__':main()
