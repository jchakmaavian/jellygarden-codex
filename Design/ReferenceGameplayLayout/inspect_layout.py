from pathlib import Path
import sys,json
root=Path.cwd();sys.path.insert(0,str(root/'Design/ReferenceGameplayImport/Media/Tools/Python'))
import UnityPy
env=UnityPy.load(str(root/'Design/Inspection-Levels01-50/ReferencePackage/datapack.unity3d_data/level2'))
objs={o.path_id:o for o in env.objects}; cache={}
def tree(i):
 if i not in cache:cache[i]=objs[i].read_typetree()
 return cache[i]
def pid(v):return v['m_PathID']
def walk(i,path=''):
 t=tree(i);g=tree(pid(t['m_GameObject']));path=path+'/'+g['m_Name']; comps=[]
 for c in g['m_Component']:
  k=pid(c['component']);o=objs[k];typ=o.type.name
  if typ in ('Transform','RectTransform'):continue
  try:d=tree(k) if typ!='MonoBehaviour' else {'rawSize':o.byte_size}
  except Exception as e:d={'unreadable':str(e)}
  comps.append({'id':k,'type':typ,'data':d})
 rows.append({'path':path,'active':g['m_IsActive'],'id':i,'transform':t,'components':comps})
 for c in t['m_Children']:walk(pid(c),path)
rows=[]
for k,o in objs.items():
 if o.type.name in ('Transform','RectTransform') and pid(tree(k)['m_Father'])==0:walk(k)
out=root/'Design/ReferenceGameplayLayout';(out/'scene-layout.json').write_text(json.dumps(rows,indent=2))
for r in rows:
 print(r['id'],r['active'],r['path'],'pos=',r['transform']['m_LocalPosition'],'scale=',r['transform']['m_LocalScale'],[(c['type'],c['data'].get('m_Sprite'),c['data'].get('m_Text')) for c in r['components'] if c['type'] in ('SpriteRenderer','Camera')])
for f in env.files.values():
 if hasattr(f,'externals'):print('EXTERNALS',[(str(e.path)) for e in f.externals])
