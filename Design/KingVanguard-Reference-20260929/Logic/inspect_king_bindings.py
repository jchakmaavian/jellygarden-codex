"""Read KingUi serialized payload from the exact rfv387 extraction, without mutations."""
from pathlib import Path
import sys, struct, json, re, hashlib
OUT = Path(__file__).resolve().parent
PROJECT = OUT.parents[2]
sys.path.insert(0,str(PROJECT / 'Design/Backup/Reference-UI-Characters/20260923-184326/Tools'))
from inspect_scope import objects, string

def external_files(path):
    d=path.read_bytes();p=d.index(0,48)+1+4+1
    count=struct.unpack_from('<I',d,p)[0];p+=4
    for _ in range(count):
        cls=struct.unpack_from('<i',d,p)[0];p+=7+(16 if cls==114 else 0)+16
    count=struct.unpack_from('<I',d,p)[0];p+=4
    for _ in range(count):p=((p+3)&~3)+24
    count=struct.unpack_from('<I',d,p)[0];p+=4
    for _ in range(count):
        p+=4;p=((p+3)&~3)+8
    count=struct.unpack_from('<I',d,p)[0];p+=4
    files=[]
    for i in range(count):
        p=d.index(0,p)+1+16+4
        end=d.index(0,p);files.append({'file_id':i+1,'path':d[p:end].decode('utf8')});p=end+1
    return files

def main():
    base=PROJECT/'Design/Inspection-rfv387'
    d,rows,meta=objects(base/'data.unity3d_data/globalgamemanagers.assets')
    scripts={}
    for row in rows:
        if row['class_id']!=115: continue
        raw=d[row['byte_offset']:row['byte_offset']+row['size']]
        if b'KingUi' in raw or b'KingAnimationStateManager' in raw:
            scripts[row['path_id']]={'name':row['name'],'raw_hex':raw.hex()}
    print('scripts',scripts.keys())
    result=[]
    for path in sorted(base.glob('*_data/*')):
        if path.suffix not in ('.assets','') or not path.is_file():continue
        try:d,rows,meta=objects(path)
        except Exception:continue
        by={r['path_id']:r for r in rows}
        for row in rows:
            if row['class_id']!=114:continue
            raw=d[row['byte_offset']:row['byte_offset']+row['size']]
            if len(raw)<28:continue
            script_file,script_id=struct.unpack_from('<iq',raw,16)
            if script_id not in scripts:continue
            game_file,game_id=struct.unpack_from('<iq',raw,0)
            entry={'file':str(path.relative_to(PROJECT)),'path_id':row['path_id'],
                   'script_file_id':script_file,'script_path_id':script_id,'script':scripts[script_id]['name'],
                   'game_object_path_id':game_id,'game_object_name':by.get(game_id,{}).get('name'),
                   'payload_size':len(raw),'payload_sha256':hashlib.sha256(raw).hexdigest(),
                   'strings':[s.decode('ascii') for s in re.findall(rb'[ -~]{4,}',raw)],'raw_hex':raw.hex()}
            result.append(entry)
            (OUT/f'{path.name}-{row["path_id"]}-{entry["script"]}.unitybin').write_bytes(raw)
    d,rows,meta=objects(base/'datapack.unity3d_data/sharedassets2.assets')
    assets={}
    for row in rows:
        if row['class_id']!=114:continue
        raw=d[row['byte_offset']:row['byte_offset']+row['size']]
        assets[row['path_id']]={'file':'Design/Inspection-rfv387/datapack.unity3d_data/sharedassets2.assets',
                               'path_id':row['path_id'],'payload_size':len(raw),
                               'strings':[s.decode('utf8') for s in re.findall(rb'(?:[ -~]|[\xc0-\xff][\x80-\xbf]+){4,}',raw)],
                               'raw_hex':raw.hex()}
        if 83<=row['path_id']<=95:
            name,p=string(raw,28)
            skeleton_file,skeleton_id=struct.unpack_from('<iq',raw,p);p+=12
            animation_name,p=string(raw,p)
            assert p==len(raw),(row['path_id'],p,len(raw))
            assets[row['path_id']].update(asset_name=name,animation_name=animation_name,skeleton_ref={'file_id':skeleton_file,'path_id':skeleton_id})
    ui=next(e for e in result if e['script']=='KingUi')
    raw=bytes.fromhex(ui['raw_hex']);p=32
    def ptr():
        nonlocal p
        f,j=struct.unpack_from('<iq',raw,p);p+=12
        return {'file_id':f,'path_id':j}
    bindings={'kingAnim':ptr()}
    for field in ['idleConnects','idleLoops']:
        count=struct.unpack_from('<I',raw,p)[0];p+=4
        bindings[field]=[ptr() for _ in range(count)]
    for field in ['tableGameFirstIdleLoop','tableGameAlternateIdleLoop','sad','happy','worryStart','worryLoop','extraMoveHappy','excited','relief']:
        bindings[field]=ptr()
    for name,items in bindings.items():
        for item in items if isinstance(items,list) else [items]:
            if item['file_id']==3:
                item['asset']=assets[item['path_id']]
    externals=external_files(base/'datapack.unity3d_data/level2')
    assert next(x for x in externals if x['file_id']==3)['path'].endswith('sharedassets2.assets')
    (OUT/'king-serialized-bindings.json').write_text(json.dumps({'scripts':scripts,'objects':result,'external_files':externals,'field_order_source':'IL2CPP KingUi metadata and serialized Unity field order','bindings':bindings},indent=2,ensure_ascii=False),encoding='utf8')
    print(json.dumps(bindings,indent=2,ensure_ascii=True))

if __name__=='__main__':main()
