"""Bounded, read-only source booster audit. Writes only beside this script."""
from pathlib import Path
import hashlib, json, sys, re, zipfile, zlib, struct, importlib.util, math

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
TOOLS = ROOT / 'Design/ReferenceGameplayImport/Media/Tools'
sys.path.insert(0, str(TOOLS / 'Python'))
import UnityPy

APK = ROOT / 'Design/Backup/apk/royal_thron_rfv387.apk'
SOURCE = ROOT / 'Design/Inspection-rfv387/datapack.unity3d_data/resources.assets'
env = UnityPy.load(str(SOURCE))
objects = {o.path_id:o for o in env.objects}
cache = {}
def tree(p):
    if p not in cache: cache[p] = objects[p].read_typetree()
    return cache[p]
def pid(p): return p['m_PathID']
def transform(g):
    return next(pid(c['component']) for c in g['m_Component'] if objects[pid(c['component'])].type.name in ('Transform','RectTransform'))
def traverse(tid,path=''):
    tr=tree(tid);go=tree(pid(tr['m_GameObject']));path=path+'/'+go['m_Name'] if path else go['m_Name']
    yield tid,go,path
    for child in tr['m_Children']:yield from traverse(pid(child),path)
def refs(v):
    if isinstance(v,dict):
        if 'm_PathID' in v:yield v
        else:
            for x in v.values():yield from refs(x)
    elif isinstance(v,list):
        for x in v:yield from refs(x)
def write(name,data):
    (OUT/name).write_text(json.dumps(data,indent=2,allow_nan=True),encoding='utf8')
def digest(data):return hashlib.sha256(data).hexdigest()
def index():
    names=[]
    types={'GameObject','AnimationClip','AnimatorController','MonoScript','AudioClip','Material','Texture2D','Sprite'}
    for p,o in objects.items():
        if o.type.name not in types:continue
        n=o.peek_name()
        if n and re.search(r'rocket|propeller|tnt',n,re.I):names.append(dict(id=p,type=o.type.name,name=n))
    write('booster-names.json',names)
    with zipfile.ZipFile(APK) as z:
        entry=z.getinfo('assets/bin/Data/datapack.unity3d')
        pack=ROOT/'Design/Inspection-rfv387/datapack.unity3d'
        provenance=dict(apk=str(APK.relative_to(ROOT)),bytes=APK.stat().st_size,sha256=digest(APK.read_bytes()),unityVersion=env.file.unity_version if hasattr(env.file,'unity_version') else '2021.3.55f1',resourcesAssetsSHA256=digest(SOURCE.read_bytes()),datapackZipCRC=entry.CRC,datapackZipSize=entry.file_size,unpackedBundleSHA256=digest(pack.read_bytes()),bundleMatchesAPK=digest(pack.read_bytes())==digest(z.read(entry)))
    write('provenance.json',provenance)
    print(json.dumps(provenance,indent=2))
    print(json.dumps([n for n in names if n['type'] in ('AnimationClip','AnimatorController')],indent=2))
    print('Game objects:',json.dumps([n for n in names if n['type']=='GameObject'],separators=(',',':')))

def audit():
    sys.path.insert(0,str(TOOLS))
    import export_visuals as old
    old_visual=json.loads((TOOLS.parent/'NativeVisuals.json').read_text())
    old_particles=json.loads((TOOLS.parent/'NativeParticles.json').read_text())
    names=json.loads((OUT/'booster-names.json').read_text())
    byname={n['name']:n['id'] for n in names if n['type']=='GameObject'}
    roots={n:byname[n] for n in ['RocketItemPrefab','TntItemPrefab','TntTntComboPrefab','PropellerItemPrefab']}
    sprite_ids=set();material_ids=set();visuals=[];clips=[];states=[]
    def normalize(v,obs):
        if isinstance(v,dict):
            if 'm_PathID' in v:
                p=pid(v);o=obs.get(p) if v['m_FileID']==0 else None
                return dict(referenceType=o.type.name,referenceName=o.peek_name()) if o else dict(externalFile=v['m_FileID'],pathId=p)
            return {k:normalize(x,obs) for k,x in v.items()}
        if isinstance(v,list):return [normalize(x,obs) for x in v]
        if isinstance(v,float) and not math.isfinite(v):return str(v)
        return v
    def canon(v):return json.dumps(v,sort_keys=True,separators=(',',':'))
    def dependencies(t):
        for r in refs(t):
            p=pid(r)
            if not p or r['m_FileID']!=0:continue
            if objects[p].type.name=='Sprite':sprite_ids.add(p)
            if objects[p].type.name=='Material':material_ids.add(p)
    for label,gid in roots.items():
        seq=list(traverse(transform(tree(gid))));anims=[];node_rows=[]
        for tid,go,path in seq:
            components=[]
            for c in go['m_Component']:
                cp=pid(c['component']);typ=objects[cp].type.name
                components.append(dict(id=cp,type=typ));
                if typ=='Animator':anims.append((cp,path))
                if typ!='MonoBehaviour':dependencies(tree(cp))
            node_rows.append(dict(gameObjectId=pid(tree(tid)['m_GameObject']),transformId=tid,path=path,components=components))
        visuals.append(dict(name=label,sourceGameObjectId=gid,sourceTransformId=transform(tree(gid)),nodes=node_rows))
        for aid,apath in anims:
            paths={0:''}
            for _,_,path in seq:
                if path.startswith(apath+'/'):
                    relative=path[len(apath)+1:];paths[zlib.crc32(relative.encode())]=relative
            cid=pid(tree(aid)['m_Controller']);controller=tree(cid);cids=[pid(p) for p in controller['m_AnimationClips']]
            for cp in cids:
                native=tree(cp);clip=old.decode_curves(native,paths);clip['pathId']=cp
                previous=next((x for x in old_visual['clips'] if x['name']==clip['name']),None)
                def curve_norm(c,obs):
                    c=json.loads(json.dumps(c));c.pop('pathId',None)
                    for curve in c['curves']:
                        for k in curve['keys']:
                            if 'objectPathId' in k:
                                p=k.pop('objectPathId');k['objectName']=obs[p].peek_name() if p else None
                    return c
                same=previous is not None and canon(curve_norm(clip,objects))==canon(curve_norm(previous,old.objects))
                dependencies(native)
                clips.append(dict(name=clip['name'],pathId=cp,previousPathId=previous['pathId'] if previous else None,duration=clip['duration'],frameRate=clip['frameRate'],loop=clip['loop'],curveCount=len(clip['curves']),missingBindings=clip['missingBindings'],events=native['m_Events'],normalizedCurvesIdenticalToImported=same,sourceRawSHA256=digest(objects[cp].get_raw_data())))
                write('Clips/'+str(cp)+'-'+clip['name']+'.json',clip)
            tos=dict(controller['m_TOS']);sm=controller['m_Controller']['m_StateMachineArray'][0]['data'];rows=[]
            for st in sm['m_StateConstantArray']:
                st=st['data'];node=st['m_BlendTreeConstantArray'][0]['data']['m_NodeArray'][0]['data']
                rows.append(dict(name=tos.get(st['m_NameID'],str(st['m_NameID'])),clipPathId=cids[node['m_ClipID']],clipName=tree(cids[node['m_ClipID']])['m_Name'],speed=st['m_Speed'],transitions=st['m_TransitionConstantArray']))
            states.append(dict(name=controller['m_Name'],pathId=cid,defaultState=sm['m_DefaultState'],states=rows))
    particles=[];particle_names=[p['name'] for p in old_particles['prefabs'] if re.match(r'^(RocketItem|Tnt|Propeller)',p['name'])]+['TntTntComboFuseParticles']
    for name in particle_names:
        if name not in byname:particles.append(dict(name=name,missing=True));continue
        gid=byname[name];seq=list(traverse(transform(tree(gid))));previous=next((p for p in old_particles['prefabs'] if p['name']==name),None)
        old_nodes=[]
        if previous:
            ot=previous['nodes'][0]['transformId']
            old_nodes=list(old.traverse(ot))
        nodes=[];same_all=previous is not None
        for node_index,(tid,go,path) in enumerate(seq):
            components=[];oldg=old_nodes[node_index][1] if node_index<len(old_nodes) and old_nodes[node_index][2]==path else None
            for c in go['m_Component']:
                cp=pid(c['component']);typ=objects[cp].type.name
                if typ=='MonoBehaviour':continue
                t=tree(cp);dependencies(t)
                older=[old.pid(c['component']) for c in oldg['m_Component'] if old.objects[old.pid(c['component'])].type.name==typ] if oldg else []
                same=bool(older) and canon(normalize(t,objects))==canon(normalize(old.tree(older[0]),old.objects))
                same_all &= same
                entry=dict(pathId=cp,type=typ,normalizedDataIdenticalToImported=same,previousPathIds=older)
                if typ=='ParticleSystem':
                    entry.update(duration=t['lengthInSec'],loop=t['looping'],playOnAwake=t['playOnAwake'],startDelay=t['startDelay'],startLifetime=t['InitialModule']['startLifetime'],emission=t['EmissionModule'])
                components.append(entry)
            nodes.append(dict(path=path,gameObjectId=pid(tree(tid)['m_GameObject']),transformId=tid,components=components))
        particles.append(dict(name=name,sourceGameObjectId=gid,previousImported=previous is not None,sourceNodeCount=len(seq),previousNodeCount=len(old_nodes),allNormalizedComponentsIdentical=same_all,nodes=nodes))
    write('visual-hierarchies.json',visuals);write('clip-summary.json',clips);write('controllers.json',states);write('particle-summary.json',particles)
    dependencies_rows=[]
    for p in sorted(material_ids):
        t=tree(p);dependencies_rows.append(dict(id=p,type='Material',name=t['m_Name'],shader=t['m_Shader'],properties=t['m_SavedProperties']))
    previous_sprites={o.peek_name():o for o in old.env.objects if o.type.name=='Sprite'}
    for p in sorted(sprite_ids):
        t=tree(p);previous=previous_sprites.get(t['m_Name']);im=objects[p].read().image.convert('RGBA');imhash=digest(im.tobytes());oldim=previous.read().image.convert('RGBA') if previous else None
        identical=oldim is not None and oldim.size==im.size and digest(oldim.tobytes())==imhash
        delta={}
        if oldim is not None and oldim.size==im.size and not identical:
            from PIL import ImageChops,ImageStat
            diff=ImageChops.difference(im,oldim);stats=ImageStat.Stat(diff)
            delta=dict(meanAbsoluteRGBA=stats.mean,maxAbsoluteRGBA=[a[1] for a in stats.extrema])
        dependencies_rows.append(dict(id=p,type='Sprite',name=t['m_Name'],texture=t['m_RD']['texture'],rect=t['m_Rect'],pivot=t['m_Pivot'],pixelsPerUnit=t['m_PixelsToUnits'],decodedRGBAHash=imhash,previousId=previous.path_id if previous else None,identicalPreviousPixels=identical,pixelDelta=delta))
    write('dependencies.json',dependencies_rows)
    print('CLIPS',len(clips),'IDENTICAL',sum(c['normalizedCurvesIdenticalToImported'] for c in clips));print('PARTICLES',json.dumps([{k:v for k,v in p.items() if k!='nodes'} for p in particles],indent=2));print('SPRITES',len(sprite_ids),'PIXEL DIFFERENT',sum(x['type']=='Sprite' and not x['identicalPreviousPixels'] for x in dependencies_rows))

if __name__=='__main__':
    (OUT/'Clips').mkdir(exist_ok=True)
    if '--audit' in sys.argv:audit()
    else:index()
