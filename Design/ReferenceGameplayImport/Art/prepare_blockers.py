from pathlib import Path
import json,sys,math,hashlib
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
HERE=Path(__file__).parent
ART=ROOT/'Assets/ReferenceGameplay/Art'
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools'))
from extract_media import objects,tree
sys.path.insert(0,str(ROOT/'Design/Backup/Reference-UI-Characters/20260923-184326/Tools'))
from spine38 import Reader
import king_frames as pose_sampler

native=json.loads((HERE/'NativeBlockers.json').read_text())
prefabs=[]
for p in native['prefabs']:
 nodes=[]
 for n in p['nodes']:
  v={k:n[k] for k in ('transformId','parentTransformId','name','active','localPosition','localRotation','localScale')}
  v['renderers']=[]
  for c in n['components']:
   if c['type']=='SpriteRenderer':
    t=c['data'];v['renderers'].append(dict(sourcePathId=c['pathId'],spritePathId=t['m_Sprite']['m_PathID'],enabled=t['m_Enabled'],sortingOrder=t['m_SortingOrder'],flipX=t['m_FlipX'],flipY=t['m_FlipY'],color=t['m_Color'],drawMode=t['m_DrawMode'],size=t['m_Size'],maskInteraction=t['m_MaskInteraction'],spriteSortPoint=t['m_SpriteSortPoint']))
  nodes.append(v)
 key=p['key'];center={'x':.5 if key in ('cupboard','potion') else 0,'y':.5 if key in ('cupboard','potion') else 0,'z':0}
 prefabs.append(dict(key=key,sourceRootTransformId=nodes[0]['transformId'],centerOffset=center,nodes=nodes,meshLayers=[]))

# The packaged Mailbox setup pose is a native Spine mesh (not a replacement drawing).
skeleton=objects[14028].read().m_Script.encode('utf8','surrogateescape')
atlastext=objects[1965].read().m_Script
j=Reader(skeleton).read();assert not j['ik'] and not j['transformConstraints'] and not j['pathConstraints']
tex=objects[592].read();image=tex.image.convert('RGBA');path=ART/'Atlases/mailbox.png';image.save(path)
at={};region=None
for line in atlastext.splitlines()[6:]:
 if line and not line.startswith(' '):region={};at[line]=region
 elif region is not None and ':' in line:
  k,v=line.strip().split(':',1);region[k]=[int(x) for x in v.split(',')] if k in ('xy','size','orig','offset') else v.strip()
pose_sampler.J=json.loads(json.dumps(j));pose_sampler.AT=at;pose_sampler.WIDTH=image.width;pose_sampler.HEIGHT=image.height
# Spine mesh UVs are in the original untrimmed region, whereas region attachments
# use 0..1 on the trimmed rectangle. Preserve the original trim before atlas rotation.
for skin in j['skins']:
 for attachments in skin['attachments'].values():
  for attachment in attachments.values():
   if attachment['type']==2:at[attachment['path']]['mesh_uv']=True
base_uv=pose_sampler.atlas_uv
def native_uv(points,region):
 if region.get('mesh_uv'):
  points=points.copy();ow,oh=region['orig'];ox,oy=region['offset'];rw,rh=region['size']
  points[:,0]=(points[:,0]*ow-ox)/rw
  points[:,1]=(points[:,1]*oh-(oh-oy-rh))/rh
 return base_uv(points,region)
pose_sampler.atlas_uv=native_uv
idle=pose_sampler.J['animations']['mailbox_idle']
layers=pose_sampler.pose(idle,0)
mb=next(p for p in prefabs if p['key']=='mailbox')
mb['meshTransformId']=next(n['transformId'] for n in mb['nodes'] if n['name']=='MailboxSpinePrefab')
mb['meshTextureAssetPath']=path.relative_to(ROOT).as_posix()
mb['meshSourceSkeletonPathId']=14028;mb['meshSourceAtlasPathId']=1965;mb['meshSourceScale']=.01
for i,(mode,verts,uv,col,tris) in enumerate(layers):
 assert mode==0,'Mailbox has an unsupported clipping operation'
 mb['meshLayers'].append(dict(name='Native mailbox slot '+str(i),vertices=[dict(x=float(v[0])*.01,y=float(v[1])*.01,z=0) for v in verts],uv=[dict(x=float(v[0]),y=float(v[1])) for v in uv],triangles=tris.tolist(),color=dict(zip('rgba',col.tolist()))))
(HERE/'mailbox.skel').write_bytes(skeleton);(HERE/'mailbox.atlas').write_text(atlastext);(HERE/'mailbox.spine38.json').write_text(json.dumps(j))
goals=[dict(key='cupboard-goal',spritePathId=16487),dict(key='potion-goal',spritePathId=21973),dict(key='mailbox-goal',spritePathId=21515),dict(key='egg-goal',spritePathId=16331)]
report=dict(prefabs=prefabs,goals=goals,limitations=['Potion rack and individual bottle views are imported separately. Bottle placement and color setup are IL2CPP-driven, not present in the serialized rack hierarchy.','Grass prefab setup is recovered, but the exact runtime 2-hit state selector is not serialized; no invented grass2 prefab is supplied.','Mailbox static mesh uses packaged mailbox_idle at t=0, native 0.01 skeleton scale, and original atlas. Seven packaged activity animations are archived; static prefab does not implement those clips.','All imported blocker prefabs preserve renderer hierarchy/poses only; state-dependent hit/removal behavior remains in existing gameplay logic.'])
(ART/'BlockerNativeData.json').write_text(json.dumps(report,separators=(',',':')))
(HERE/'blocker-import-status.json').write_text(json.dumps(dict(prefabs=[dict(key=p['key'],nodes=len(p['nodes']),spriteRenderers=sum(len(n['renderers']) for n in p['nodes']),meshLayers=len(p['meshLayers'])) for p in prefabs],goals=goals,limitations=report['limitations']),indent=2))
print('Blockers ready',[(p['key'],len(p['nodes']),len(p['meshLayers'])) for p in prefabs])
