from pathlib import Path
import sys,json,shutil,hashlib,math
PROJECT=Path(__file__).resolve().parents[2]
SOURCE=PROJECT.parent/'RoyalThrone-Area-Motion-Preview'
ROOT=PROJECT/'Assets/JellyGarden/Resources/RoyalAreas/NativeMotion'
AUDIT=PROJECT/'Design/AreaMotionImport'
sys.path.insert(0,str(PROJECT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
from PIL import Image

def save(p,d):
 p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(d,separators=(',',':'),allow_nan=False),encoding='utf-8')
def copy(a,b):
 b.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(a,b)
def keys(curve):return [dict(t=k['time'],v=k['value'],i=k.get('inSlope',0),o=k.get('outSlope',0)) for k in curve.get('m_Curve',[])]
def mm(d=None):
 d=d or {};return dict(mode=d.get('minMaxState',0),max=d.get('scalar',0),min=d.get('minScalar',0),keys=keys(d.get('maxCurve',{})),lower=keys(d.get('minCurve',{})))
def grad(d=None):
 d=d or {};return dict(colors=[dict(t=d.get('ctime'+str(i),0)/65535,c=d['key'+str(i)]) for i in range(d.get('m_NumColorKeys',0))],alphas=[dict(t=d.get('atime'+str(i),0)/65535,a=d['key'+str(i)]['a']) for i in range(d.get('m_NumAlphaKeys',0))])
fx=json.loads((SOURCE/'assets/effects.json').read_text())
def particle(ps,r):
 ini=ps['InitialModule'];shape=ps['ShapeModule'];em=ps['EmissionModule'];sz=ps['SizeModule'];rot=ps['RotationModule'];vel=ps['VelocityModule'];col=ps['ColorModule']
 return dict(loop=ps['looping'],duration=ps['lengthInSec'],delay=mm(ps['startDelay']),space=ps.get('moveWithTransform',0),scaling=ps.get('scalingMode',0),lifetime=mm(ini['startLifetime']),speed=mm(ini['startSpeed']),size=mm(ini['startSize']),rotation=mm(ini['startRotation']),gravity=mm(ini['gravityModifier']),startColor=ini['startColor'].get('maxColor'),startColorMin=ini['startColor'].get('minColor'),startColorMode=ini['startColor'].get('minMaxState'),rate=mm(em['rateOverTime']),bursts=[dict(time=b.get('time',0),count=mm(b.get('count'))) for b in em.get('m_Bursts',[])],shape=shape.get('type',0),shapeEnabled=shape['enabled'],radius=shape.get('radius',{}).get('value',.1),angle=shape.get('angle',0),radiusThickness=shape.get('radiusThickness',1),shapePosition=shape.get('m_Position'),shapeRotation=shape.get('m_Rotation'),shapeScale=shape.get('m_Scale'),sizeEnabled=sz['enabled'],sizeCurve=mm(sz.get('curve')),rotationEnabled=rot['enabled'],rotationCurve=mm(rot.get('curve')),velocityEnabled=vel['enabled'],velocityWorld=vel.get('inWorldSpace',False),vx=mm(vel.get('x')),vy=mm(vel.get('y')),vz=mm(vel.get('z')),colorEnabled=col['enabled'],color=grad(col.get('gradient',{}).get('maxGradient')),sprites=[str(p['sprite']['m_PathID']) for p in ps.get('UVModule',{}).get('sprites',[])],renderMode=r.get('m_RenderMode',0),lengthScale=r.get('m_LengthScale',1),velocityScale=r.get('m_VelocityScale',0),order=r.get('m_SortingOrder',0),sortingLayer=r.get('m_SortingLayer',0),additive=any(x.get('m_PathID')==11 for x in r.get('m_Materials',[])))
before=AUDIT/'Before';before.mkdir(parents=True,exist_ok=True)
for rel in ['Assets/JellyGarden/Resources/RoyalAreas/areas.json','Assets/JellyGarden/Scripts/GUI/RoyalAreaScene.cs','Assets/JellyGarden/Scripts/GUI/RoyalAreas.cs','Assets/JellyGarden/Scripts/GUI/RoyalAreaView.cs']:
 p=before/Path(rel).name
 if not p.exists():copy(PROJECT/rel,p)

propertyNames=['m_LocalPosition.x','m_LocalPosition.y','m_LocalPosition.z','m_LocalRotation.x','m_LocalRotation.y','m_LocalRotation.z','m_LocalRotation.w','m_LocalScale.x','m_LocalScale.y','m_LocalScale.z','localEulerAnglesRaw.z','m_Color.r','m_Color.g','m_Color.b','m_Color.a','m_IsActive','m_Enabled','m_SortingOrder','m_FlipX','m_FlipY','m_Sprite','localEulerAnglesRaw.x','localEulerAnglesRaw.y']
report=[]
for area in range(1,4):
 d=json.loads((SOURCE/f'assets/Area{area:02}/runtime.json').read_text());folder=ROOT/f'Area{area:02}';folder.mkdir(parents=True,exist_ok=True)
 nodeMap={n['path']:i for i,n in enumerate(d['nodes'])};spriteMap={s['id']:i for i,s in enumerate(d['sprites'])};clipMap={c['id']:i for i,c in enumerate(d['animations'])}
 hierarchy=json.loads((PROJECT.parent/f'RoyalMatch-Areas-1-5/Area{area:02}/Metadata/Hierarchy.json').read_text())['nodes']
 groups={n['path']:c['data']['m_SortingOrder'] for n in hierarchy for c in n['components'] if c['type']=='SortingGroup'}
 nodes=[]
 for n in d['nodes']:
  x={k:n[k] for k in ['name','position','rotation','scale','color','active','enabled','order','flipX','flipY','maskInteraction']}
  x.update(parent=nodeMap.get(n['parent'],-1),sprite=spriteMap.get(n['spriteId'],-1),bones=[nodeMap.get(b,-1) for b in n.get('bones',[])],flag=n['name']=='CastleFlagSpine')
  x['sortingGroupOrder']=groups.get(n['path'],-2147483648)
  if n.get('mask'):x.update(maskSprite=spriteMap.get(str(n['mask']['m_Sprite']['m_PathID']),-1),maskBack=n['mask']['m_BackSortingOrder'],maskFront=n['mask']['m_FrontSortingOrder'])
  else:x['maskSprite']=-1
  nodes.append(x)
 sprites=[]
 for s in d['sprites']:
  pos=[dict(x=v[0],y=v[1],z=0) for v in s['vertices']]
  uv=[dict(x=(v[0]*s['ppu']+s['rect']['width']*s['pivot']['x']-s['offset']['x'])/s['size'][0],y=(v[1]*s['ppu']+s['rect']['height']*s['pivot']['y']-s['offset']['y'])/s['size'][1]) for v in s['vertices']]
  copy(SOURCE/f'assets/Area{area:02}/RoyalSprites/{s["id"]}.png',folder/f'Sprites/{s["id"]}.png')
  sprites.append(dict(name=s['name'],texture=f'Area{area:02}/Sprites/{s["id"]}',positions=pos,uv=uv,indices=s['indices'],weights=[dict(x=w[0],y=w[1],z=w[2],w=w[3]) for w in s.get('weights') or []],boneIndices=[dict(x=w[0],y=w[1],z=w[2],w=w[3]) for w in s.get('boneIndices') or []],bindposes=s.get('bindposes') or [],water=s['material']=='water',flow=2 if 'deniz' in s['name'].lower() else 1 if 'kale_su' in s['name'].lower() else 0))
 clips=[]
 for c in d['animations']:
  curves=[]
  for cv in c['curves']:
   ni=nodeMap.get(c['root']+('/'+cv['path'] if cv['path'] else ''),-1)
   prop=20 if cv.get('isPPtr') else propertyNames.index(cv['property']) if cv['property'] in propertyNames else -1
   if ni<0 or prop<0:continue
   curves.append(dict(node=ni,property=prop,keys=[dict(t=k['time'],v=spriteMap.get(str(k.get('objectPathId',0)),-1) if prop==20 else k['value'],i=k['inSlope'],o=k['outSlope']) for k in cv['keys']]))
  clips.append(dict(name=c['name'],duration=c['duration'],loop=c['loop'],curves=curves))
 tasks=[]
 for t in d['tasks']:
  offsets=[]
  for ci in t['clipIds']:
   name=d['animations'][clipMap[ci]]['name'];offsets.append({'LeftCarpetReveal':0,'MiddleCarpetReveal':.25,'RightCarpetReveal':.55,'BottomCarpetReveal':.85}.get(name,.35 if name=='PianoReveal' else 0))
  tasks.append(dict(name=t['name'],root=nodeMap[t['root']],duration=t['duration'],audio='Audio/'+Path(t['audio']).stem,clips=[clipMap[i] for i in t['clipIds']],offsets=offsets,effect=t['effectName'],reconstructed=not t['nativeClip']))
 particles=[dict(node=nodeMap[p['path']],data=particle(p['data'],p.get('renderer') or {})) for p in d['particles'] if p['data'].get('looping')]
 save(folder/'scene.json',dict(area=area,camera=d['camera'],nodes=nodes,sprites=sprites,clips=clips,tasks=tasks,particles=particles))
 report.append(dict(area=area,nodes=len(nodes),sprites=len(sprites),clips=len(clips),curves=sum(len(c['curves']) for c in clips),tasks=len(tasks),particles=len(particles)))
 # UI task icons retain authored task group identity; independent from the moving meshes.
 originals=PROJECT.parent/f'RoyalMatch-Areas-1-5/Area{area:02}/Sprites'
 icons=sorted(originals.glob('*task*'))
 print('AREA',area,'ICONS',[p.name for p in icons])

effects=[]
for e in fx['effects']:
 ns=[];mapping={n['path']:i for i,n in enumerate(e['nodes'])}
 for n in e['nodes']:
  tr=next(c['data'] for c in n['components'] if c['type']=='Transform');ps=next((c['data'] for c in n['components'] if c['type']=='ParticleSystem'),None);renderer=next((c['data'] for c in n['components'] if c['type']=='ParticleSystemRenderer'),{})
  ns.append(dict(name=n['path'].split('/')[-1],parent=mapping.get(n['path'].rsplit('/',1)[0],-1),active=n['active'],position=tr['m_LocalPosition'],rotation=tr['m_LocalRotation'],scale=tr['m_LocalScale'],data=particle(ps,renderer) if ps else None))
 effects.append(dict(name=e['name'],nodes=ns))
# One texture for all particle frames; SpriteSheetAnimation uses sprites from the same texture.
atlas=Image.new('RGBA',(2048,2048));rects=[];x=y=2;row=0
for s in sorted(fx['sprites'],key=lambda s:-s['size'][1]):
 im=Image.open(SOURCE/s['file']).convert('RGBA');w,h=im.size
 if x+w+2>2048:x=2;y+=row+4;row=0
 assert y+h+2<=2048
 atlas.paste(im,(x,y));rects.append(dict(id=s['id'],x=x,y=2048-y-h,w=w,h=h,ppu=s['ppu']));x+=w+4;row=max(row,h)
(ROOT/'VFX').mkdir(exist_ok=True);atlas.save(ROOT/'VFX/particles.png');save(ROOT/'effects.json',dict(effects=effects,sprites=rects))
copy(SOURCE/'assets/Area01/Textures/707.png',ROOT/'VFX/distortion.png')
copy(SOURCE/'assets/Area01/Textures/865.png',ROOT/'VFX/flags.png')
flags=json.loads((SOURCE/'assets/castle-flags.json').read_text())
save(ROOT/'flags.json',dict(duration=flags['duration'],fps=flags['fps'],frames=[dict(meshes=[dict(positions=[dict(x=m['positions'][j],y=m['positions'][j+1],z=0) for j in range(0,len(m['positions']),2)],uv=[dict(x=m['uv'][j],y=1-m['uv'][j+1]) for j in range(0,len(m['uv']),2)],indices=m['indices']) for m in frame]) for frame in flags['frames']]))
for p in (SOURCE/'assets/RoyalAudio').glob('*.wav'):copy(p,ROOT/'Audio'/p.name)
save(AUDIT/'import-report.json',dict(source=str(SOURCE),areas=report,material='Baked RoyalSprites; preserve original geometry and alpha.',fountain={'emissionMultiplier':1.75,'speedMultiplier':1.22,'sizeMultiplier':1.08},water={'flow':'directional two-phase flow, micro-ripples, edge foam, previous-frame reflection'},sourceHash='a2cbee4ecf021ee464dc6f17766ba6c0b2addb2d65007f611e5c368532aa8151'))
print(report)
