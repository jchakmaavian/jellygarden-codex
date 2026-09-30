from pathlib import Path
import sys,json,collections
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
import UnityPy
OUT=Path(__file__).parent
env=UnityPy.load(str(ROOT/'Design/Inspection-rfv387/datapack.unity3d_data/resources.assets'))
objs={o.path_id:o for o in env.objects}
names=[]
for o in env.objects:
 if o.type.name not in ('AnimationClip','AnimatorController','GameObject','Material','Shader','Sprite','AudioClip','MonoScript'):continue
 try:
  t=o.read_typetree();n=t.get('m_Name','')
  if 'coin' in n.lower() or (o.type.name=='AnimationClip' and any(s in n.lower() for s in ['reward','win'])):
   names.append({'id':o.path_id,'type':o.type.name,'name':n})
   if o.type.name in ('AnimationClip','AnimatorController','GameObject','Material','AudioClip','MonoScript'):
    (OUT/f'{o.type.name}-{o.path_id}.json').write_text(json.dumps(t,indent=2),encoding='utf-8')
 except Exception as e:print('ERROR',o.path_id,o.type.name,str(e)[:80])
(OUT/'coin-names.json').write_text(json.dumps(names,indent=2),encoding='utf-8')
print(json.dumps(names,indent=2))
