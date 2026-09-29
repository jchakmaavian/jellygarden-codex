"""Import render/audio dependencies only from the user-supplied rfv387 APK payload."""
from pathlib import Path
import sys,json,math,hashlib,zipfile
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools'))
import UnityPy
from extract_media import editor_value
env=UnityPy.load(str(ROOT/'Design/Inspection-rfv387/datapack.unity3d_data/resources.assets'))
objs={o.path_id:o for o in env.objects};cache={}
def tree(p):
 if p not in cache:cache[p]=objs[p].read_typetree()
 return cache[p]
def pid(p):return p['m_PathID']
def refs(v):
 if isinstance(v,dict):
  if 'm_PathID' in v:yield v
  else:
   for x in v.values():yield from refs(x)
 elif isinstance(v,list):
  for x in v:yield from refs(x)
def clean(v):
 if isinstance(v,float) and not math.isfinite(v):return 0.
 if isinstance(v,list):return [clean(x) for x in v]
 if isinstance(v,dict):return {k:clean(x) for k,x in v.items()}
 return v
dest=ROOT/'Assets/ReferenceGameplay/Tools/Cannon';dest.mkdir(parents=True,exist_ok=True)
for p,name in [(1715,'export_anim_cannon.skel.bytes'),(14089,'export_anim_cannon.atlas.txt')]:
 (dest/name).write_bytes(objs[p].read().m_Script.encode('utf8','surrogateescape'))
objs[885].read().image.save(dest/'export_anim_cannon.png')
prefabs=[];needed=set();owned=set()
def walk(gid,nodes,parent=0):
 g=tree(gid);tid=next(pid(c['component']) for c in g['m_Component'] if objs[pid(c['component'])].type.name=='Transform');t=tree(tid);comps=[]
 owned.update([gid,tid])
 for c in g['m_Component']:
  cp=pid(c['component']);o=objs[cp]
  if o.type.name not in ['Transform','ParticleSystem','ParticleSystemRenderer','SpriteRenderer','SortingGroup']:continue
  owned.add(cp);needed.update((r['m_FileID'],pid(r)) for r in refs(tree(cp)) if pid(r))
  comps.append(dict(pathId=cp,type=o.type.name,json=json.dumps(clean(editor_value(tree(cp),o._get_typetree_node())),separators=(',',':'))))
 nodes.append(dict(pathId=gid,transformId=tid,parentTransformId=parent,name=g['m_Name'],active=g['m_IsActive'],localPosition=t['m_LocalPosition'],localRotation=t['m_LocalRotation'],localScale=t['m_LocalScale'],components=comps))
 for ch in t['m_Children']:walk(pid(tree(pid(ch))['m_GameObject']),nodes,tid)
for gid,name in [(41043,'CannonFireRfv387'),(38903,'CannonFuseRfv387'),(56812,'CannonBallRfv387')]:
 nodes=[];walk(gid,nodes);prefabs.append(dict(name=name,nodes=nodes))
globalenv=UnityPy.load(str(ROOT/'Design/Inspection-rfv387/data.unity3d_data/globalgamemanagers.assets'));globs={o.path_id:o for o in globalenv.objects}
mats=[];textures=set();sprites=[];external=[]
for f,p in sorted(needed):
 if f:external.append([f,p]);continue
 if p in owned:continue
 o=objs[p];t=tree(p)
 if o.type.name=='Material':
  sh=t['m_Shader'];shader=(tree(pid(sh)) if sh['m_FileID']==0 else globs[pid(sh)].read_typetree())['m_ParsedForm']['m_Name']
  m=dict(pathId=p,name=t['m_Name'],shaderName=shader,queue=t['m_CustomRenderQueue'],keywords=t.get('m_ShaderKeywords',''),textures=[],floats=[],colors=[])
  for k,v in t['m_SavedProperties']['m_TexEnvs']:
   tp=pid(v['m_Texture']);m['textures'].append(dict(name=k,pathId=tp,scale=v['m_Scale'],offset=v['m_Offset']))
   if tp:textures.add(tp)
  m['floats']=[dict(name=k,value=v) for k,v in t['m_SavedProperties']['m_Floats']]
  m['colors']=[dict(name=k,color=clean(v)) for k,v in t['m_SavedProperties']['m_Colors']];mats.append(m)
 elif o.type.name=='Sprite':
  rd=t['m_RD'];tp=pid(rd['texture']);textures.add(tp)
  # Texture rectangles are the source atlas regions; particles use their original pivots.
  sprites.append(dict(sourcePathId=p,sourceTexturePathId=tp,name=t['m_Name'],rect=rd['textureRect'],pivot=t['m_Pivot'],border=t['m_Border'],pixelsPerUnit=t['m_PixelsToUnits']))
 else:print('UNHANDLED',p,o.type.name)
texdata=[]
for p in sorted(textures):
 tx=objs[p].read();path=dest/(str(p)+'-'+tx.m_Name+'.png');tx.image.save(path)
 texdata.append(dict(pathId=p,name=tx.m_Name,assetPath=path.relative_to(ROOT).as_posix()))
# Namespace source IDs: the existing VFX catalog comes from an older APK.
OFFSET=388000000
def remap(v):
 if isinstance(v,list):return [remap(x) for x in v]
 if isinstance(v,dict):
  if 'm_PathID' in v:return {k:(x+OFFSET if k=='m_PathID' and x and v['m_FileID']==0 else x) for k,x in v.items()}
  return {k:(x+OFFSET if k in ('pathId','transformId','parentTransformId','sourcePathId','sourceTexturePathId') and x else json.dumps(remap(json.loads(x)),separators=(',',':')) if k=='json' else remap(x)) for k,x in v.items()}
 return v
(OUT/'NativeCannonParticles.json').write_text(json.dumps(remap(dict(prefabs=prefabs,materials=mats,textures=texdata,sprites=sprites)),separators=(',',':')))
import fmod_toolkit
audio=dest/'Resources/ReferenceGameplay/Cannon';audio.mkdir(parents=True,exist_ok=True)
with zipfile.ZipFile(ROOT/'Design/Backup/apk/royal_thron_rfv387.apk') as z:stream=z.read('assets/bin/Data/resources.resource')
for p in [17903,17936,17969]:
 t=tree(p);r=t['m_Resource'];buf=stream[r['m_Offset']:r['m_Offset']+r['m_Size']]
 for _,wav in fmod_toolkit.raw_to_wav(buf,t['m_Name'],t['m_Channels'],t['m_Frequency']).items():(audio/(t['m_Name']+'.wav')).write_bytes(wav)
print('IMPORTED',len(prefabs),'effects',len(sprites),'sprites',len(textures),'textures',[(m['name'],m['shaderName']) for m in mats])
(OUT/'source.json').write_text(json.dumps(dict(apk='Design/Backup/apk/royal_thron_rfv387.apk',spine='3.8.84',skeletonPathId=1715,atlasPathId=14089,texturePathId=885,external=external),indent=2))
