from pathlib import Path
import sys,json,zlib,struct
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools'))
from export_visuals import decode_curves, HASHES
import UnityPy
OUT=Path(__file__).parent
env=UnityPy.load(str(ROOT/'Design/Inspection-rfv387/datapack.unity3d_data/resources.assets'));objs={o.path_id:o for o in env.objects};cache={}
def tr(i):
 if i not in cache:cache[i]=objs[i].read_typetree()
 return cache[i]
def components(go):return [(x['component']['m_PathID'],objs[x['component']['m_PathID']].type.name) for x in go['m_Component']]

def decode_coin_curves(native, paths):
 # The general decoder omits a streamed channel's negative-infinity sentinel
 # whenever that channel also has later keys. For object references this is the
 # actual initial sprite, not a disposable polynomial pre-roll key. Recover it
 # from each binding's own mapping, including CoinTap's longer initial hold.
 result=decode_curves(native, paths)
 stream=native['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']
 raw=struct.pack('<'+'I'*len(stream['data']),*stream['data']);offset=0;initial={}
 while offset<len(raw):
  time,count=struct.unpack_from('<fI',raw,offset);offset+=8
  for _ in range(count):
   channel,a,b,slope,value=struct.unpack_from('<I4f',raw,offset);offset+=20
   if time < -1e10:initial[channel]=value
 channel=0
 ptrmap=native['m_ClipBindingConstant']['pptrCurveMapping']
 for binding in native['m_ClipBindingConstant']['genericBindings']:
  ty=binding['typeID'];attribute=binding['attribute'];components=1
  if ty==4 and attribute in (1,3,4):components=3
  elif ty==4 and attribute==2:components=4
  if binding['isPPtrCurve'] and ty==212 and channel in initial:
   path=paths.get(binding['path'])
   curve=next(c for c in result['curves'] if c['path']==path and c['property']=='m_Sprite')
   if not curve['keys'] or curve['keys'][0]['time']>1e-7:
    index=int(round(initial[channel]));assert 0<=index<len(ptrmap)
    curve['keys'].insert(0,dict(time=0,value=initial[channel],inSlope=0,outSlope=0,objectPathId=ptrmap[index]['m_PathID'],sourceInitialSentinel=True))
  channel+=components
 return result
HASHES[zlib.crc32(b'm_FlipX')]='m_FlipX'
paths={0:'',zlib.crc32(b'Shadow'):'Shadow'};nodes=[]
def walk(goid,p=''):
 go=tr(goid);cs=components(go);tid=next(i for i,t in cs if t in ('Transform','RectTransform'));t=tr(tid)
 paths[zlib.crc32(p.encode())]=p
 node={'id':goid,'name':go['m_Name'],'path':p,'transform':t,'components':{ty:tr(i) for i,ty in cs if ty not in ('Transform','RectTransform','MonoBehaviour')}}
 nodes.append(node)
 for child in t['m_Children']:
  ct=tr(child['m_PathID']);cg=tr(ct['m_GameObject']['m_PathID']);walk(ct['m_GameObject']['m_PathID'],p+'/'+cg['m_Name'] if p else cg['m_Name'])
for go in [56642,45442,58260]:walk(go)
(OUT/'coin-hierarchies.json').write_text(json.dumps(nodes,indent=2),encoding='utf-8')
clips=[]
for i in [16383,16384,16385,16386]:
 c=decode_coin_curves(tr(i),paths);c['pathId']=i
 for curve in c['curves']:
  for key in curve['keys']:
   if key.get('objectPathId'):key['spriteName']=tr(key['objectPathId']).get('m_Name','')
 clips.append(c)
 print(c['name'],c['duration'],c['loop'],'missing',c['missingBindings'])
 for curve in c['curves']:
  print(' ',curve['path'],curve['property'],len(curve['keys']),curve['keys'][:3])
(OUT/'DecodedCoinAnimations.json').write_text(json.dumps(clips,indent=2),encoding='utf-8')


