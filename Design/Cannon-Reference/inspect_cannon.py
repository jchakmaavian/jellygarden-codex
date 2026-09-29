from pathlib import Path
import sys,json,re
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
import UnityPy
env=UnityPy.load(str(ROOT/'Design/Inspection-rfv387/datapack.unity3d_data/resources.assets'))
objs={o.path_id:o for o in env.objects}
found=[]
for p,o in objs.items():
 if o.type.name not in ('GameObject','AnimationClip','AnimatorController','Sprite','AudioClip','Material','Texture2D'):continue
 n=o.peek_name()
 if n and re.search(r'cannon|canon|columnbooster',n,re.I):
  found.append(dict(id=p,type=o.type.name,name=n));print(p,o.type.name,n,flush=True)
out=Path(__file__).parent
(out/'index.json').write_text(json.dumps(found,indent=2))
for x in found:
 if x['type'] in ('GameObject','AnimationClip','AnimatorController'):
  (out/(str(x['id'])+'.json')).write_text(json.dumps(objs[x['id']].read_typetree(),indent=2))
def walk(gid,depth=0):
 g=objs[gid].read_typetree();print(' '*depth,gid,g['m_Name'])
 for c in g['m_Component']:
  p=c['component']['m_PathID'];o=objs[p]
  if o.type.name=='MonoBehaviour': print(' '*depth,p,'MonoBehaviour RAW',o.get_raw_data().hex());continue
  t=o.read_typetree()
  (out/(str(p)+'.json')).write_text(json.dumps(t,indent=2))
  print(' '*depth,p,o.type.name, str(t)[:130] if o.type.name=='MonoBehaviour' else '')
  if o.type.name in ('Transform','RectTransform'):
   for ch in t['m_Children']:walk(objs[ch['m_PathID']].read_typetree()['m_GameObject']['m_PathID'],depth+1)
for gid in [46035,56812,45016,49054]:walk(gid)
for p,o in objs.items():
 if o.type.name=='TextAsset' and re.search(r'cannon|canon',o.peek_name() or '',re.I):
  t=o.read();print('TEXT',p,t.m_Name);(out/(str(p)+'-'+t.m_Name+'.txt')).write_bytes(t.m_Script.encode('utf8','surrogateescape'))
