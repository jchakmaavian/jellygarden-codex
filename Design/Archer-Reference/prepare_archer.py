from pathlib import Path
import sys,json,ast,zlib,struct,math,hashlib,zipfile
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
TOOLS=ROOT/'Design/ReferenceGameplayImport/Media/Tools'
sys.path.insert(0,str(TOOLS/'Python'))
import UnityPy
from PIL import Image
STAGE=ROOT.parent/'Archer-Import';DEST=STAGE/'Assets/ReferenceGameplay/Tools/Archer'
DEST.mkdir(parents=True,exist_ok=True)
env=UnityPy.load(str(ROOT/'Design/Inspection-rfv387/datapack.unity3d_data/resources.assets'))
objs={o.path_id:o for o in env.objects}
native=objs[16190].read_typetree()
tree=ast.parse((TOOLS/'export_visuals.py').read_text())
fn=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='decode_curves')
HASHES={zlib.crc32(s.encode()):s for s in ['m_IsActive','m_Enabled','m_SortingOrder','m_FlipX','m_FlipY']+['m_Color.'+x for x in 'rgba']}
exec(compile(ast.Module(body=[fn],type_ignores=[]),'<existing decoder>','exec'))
nodes=json.loads((OUT/'hierarchy-46037.json').read_text())
nodes=[n for n in nodes if n['path']=='Root/Animator' or n['path'].startswith('Root/Animator/')]
paths={0:''}
for n in nodes:
    n['path']=n['path'][len('Root/Animator'):].lstrip('/')
    paths[zlib.crc32(n['path'].encode())]=n['path']
clip=decode_curves(native,paths)
# Retain the initial sprite held by the streamed negative-infinity sentinel.
stream=native['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']
raw=struct.pack('<'+'I'*len(stream['data']),*stream['data']);offset=0;initial={}
while offset<len(raw):
    time,count=struct.unpack_from('<fI',raw,offset);offset+=8
    for _ in range(count):
        channel,a,b,slope,value=struct.unpack_from('<I4f',raw,offset);offset+=20
        if time < -1e10:initial[channel]=value
channel=0;ptrmap=native['m_ClipBindingConstant']['pptrCurveMapping']
for b in native['m_ClipBindingConstant']['genericBindings']:
    count=3 if b['typeID']==4 and b['attribute'] in (1,3,4) else 4 if b['typeID']==4 and b['attribute']==2 else 1
    if b['isPPtrCurve'] and channel in initial:
        c=next(c for c in clip['curves'] if c['path']==paths[b['path']] and c['isPPtr'])
        if not c['keys'] or c['keys'][0]['time']>1e-7:
            v=initial[channel];c['keys'].insert(0,dict(time=0,value=v,inSlope=0,outSlope=0,objectPathId=ptrmap[round(v)]['m_PathID']))
    channel+=count
assert not clip['missingBindings'],clip['missingBindings']
spriteids={k['objectPathId'] for c in clip['curves'] for k in c['keys'] if k.get('objectPathId')}
for n in nodes:
    if 'SpriteRenderer' in n['components']:spriteids.add(n['components']['SpriteRenderer']['m_Sprite']['m_PathID'])
sprites=[]
for pid in sorted(spriteids-{0}):
    s=objs[pid].read_typetree();im=objs[pid].read().image.convert('RGBA');r=s['m_Rect'];rd=s['m_RD'];off=rd['textureRectOffset']
    # Restore trimmed atlas regions to the original sprite canvas and pivot.
    full=Image.new('RGBA',(round(r['width']),round(r['height'])))
    full.paste(im,(round(off['x']),round(r['height']-off['y']-im.height)))
    path=DEST/'Sprites'/f"{s['m_Name']}__{pid}.png";path.parent.mkdir(exist_ok=True);full.save(path)
    sprites.append(dict(id=pid,name=s['m_Name'],assetPath=path.relative_to(STAGE).as_posix(),pivot=s['m_Pivot'],ppu=s['m_PixelsToUnits'],width=full.width,height=full.height,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    print('sprite',pid,s['m_Name'],im.size,full.size,off,s['m_Pivot'])
simple=[]
for n in nodes:
    t=n['components']['Transform'];sr=n['components'].get('SpriteRenderer',{})
    simple.append(dict(name=n['name'],path=n['path'],active=n['active'],position=t['m_LocalPosition'],rotation=t['m_LocalRotation'],scale=t['m_LocalScale'],spriteId=sr.get('m_Sprite',{}).get('m_PathID',0),color=sr.get('m_Color',dict(r=1,g=1,b=1,a=1)),sortingOrder=sr.get('m_SortingOrder',0),enabled=sr.get('m_Enabled',True)))
import fmod_toolkit
audio=[]
with zipfile.ZipFile(ROOT/'Design/Backup/apk/royal_thron_rfv387.apk') as z:stream=z.read('assets/bin/Data/resources.resource')
for pid in [17939]:
    t=objs[pid].read_typetree();r=t['m_Resource'];buf=stream[r['m_Offset']:r['m_Offset']+r['m_Size']]
    for name,wav in fmod_toolkit.raw_to_wav(buf,t['m_Name'],t['m_Channels'],t['m_Frequency']).items():
        path=DEST/'Audio'/(t['m_Name']+'.wav');path.parent.mkdir(exist_ok=True);path.write_bytes(wav)
        audio.append(dict(id=pid,name=t['m_Name'],assetPath=path.relative_to(STAGE).as_posix(),sha256=hashlib.sha256(wav).hexdigest()))
fall=decode_curves(objs[16191].read_typetree(),paths)
assert not fall['missingBindings'],fall['missingBindings']
(OUT/'DecodedArcherFall.json').write_text(json.dumps(fall,indent=2))
doc=dict(clip=clip,fall=fall,sprites=sprites,nodes=simple,audio=audio)
(DEST/'ArcherSource.json').write_text(json.dumps(doc,indent=2))
(OUT/'DecodedArcherAnimation.json').write_text(json.dumps(clip,indent=2))
source=dict(apk='Design/Backup/apk/royal_thron_rfv387.apk',apkSHA256=hashlib.sha256((ROOT/'Design/Backup/apk/royal_thron_rfv387.apk').read_bytes()).hexdigest(),clipId=16190,controllerId=18043,prefabId=46037,sprites=sprites,audio=audio)
(OUT/'source.json').write_text(json.dumps(source,indent=2))
print('CLIP',clip['name'],clip['duration'],clip['frameRate'],'curves',len(clip['curves']))
for c in clip['curves']:print(c['path'],c['property'],len(c['keys']))


