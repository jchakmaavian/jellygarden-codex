from pathlib import Path
import sys,json,struct,collections
ROOT=Path(__file__).resolve().parents[3]
TOOLS=ROOT/'Design/Backup/Reference-UI-Characters/20260923-184326/Tools'
sys.path.insert(0,str(TOOLS))
from inspect_scope import string,objects
src=ROOT/'Design/Inspection-Levels01-50/ReferencePackage/datapack.unity3d_data/resources.assets'
d,rows,meta=objects(src);by={r['path_id']:r for r in rows}
out=[]
for r in rows:
 if r['class_id']!=213:continue
 b=d[r['byte_offset']:r['byte_offset']+r['size']]
 try:
  name,p=string(b,0);rect=struct.unpack_from('<4f',b,p);ppu=struct.unpack_from('<f',b,p+40)[0];pivot=struct.unpack_from('<2f',b,p+44);border=struct.unpack_from('<4f',b,p+24)
  q=p+84;tag_count=struct.unpack_from('<I',b,q)[0];q+=4
  for _ in range(tag_count):_,q=string(b,q)
  q+=12 # sprite atlas PPtr
  f,j=struct.unpack_from('<iq',b,q)
  tex=j if f==0 and j in by and by[j]['class_id']==28 else None
  out.append(dict(name=name,path_id=r['path_id'],texture_path_id=tex,texture_name=by[tex]['name'] if tex else None,rect=rect,ppu=ppu,pivot=pivot,border=border,offset=struct.unpack_from('<2f',b,p+16),render_data_offset=q))
 except Exception as e:print('ERR',r['path_id'],e)
(Path(__file__).parent/'sprite-source-index.json').write_text(json.dumps(out,indent=2))
if __name__=='__main__':
 print('Sprites by texture:')
 print(collections.Counter(a['texture_name'] for a in out))
 for a in out:
  if a['texture_name'] in ('GameplayAtlas','GameplayExtensionAtlas') and ('Items-MatchItem-' in a['name'] or a['name'].endswith(('rocket','tnt','lightball','propeller'))):print(a)
