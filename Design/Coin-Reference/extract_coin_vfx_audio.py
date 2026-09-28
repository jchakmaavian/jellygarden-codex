"""Focused extraction from the user supplied rfv387 package; no Assets writes."""
from pathlib import Path
import sys, json, math, hashlib, zipfile, wave, io
ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).parent
sys.path.insert(0, str(ROOT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
import UnityPy
SOURCE = ROOT/'Design/Inspection-rfv387/datapack.unity3d_data/resources.assets'
env = UnityPy.load(str(SOURCE))
objects = {o.path_id:o for o in env.objects}
cache = {}
def tree(p):
    if p not in cache: cache[p] = objects[p].read_typetree()
    return cache[p]
def pid(p): return p['m_PathID']
def traverse(tid,path=''):
    tr=tree(tid); go=tree(pid(tr['m_GameObject'])); path=path+'/'+go['m_Name'] if path else go['m_Name']
    yield tid,go,path
    for child in tr['m_Children']: yield from traverse(pid(child),path)
def refs(v):
    if isinstance(v,dict):
        if 'm_PathID' in v: yield v
        else:
            for x in v.values():yield from refs(x)
    elif isinstance(v,list):
        for x in v:yield from refs(x)
def editor_value(value,node):
    if isinstance(value,dict):
        result={}
        if node.m_Version>1: result['serializedVersion']=node.m_Version
        children={c.m_Name:c for c in node.m_Children}
        for key,v in value.items():result[key]=editor_value(v,children[key]) if key in children else v
        return result
    if isinstance(value,list):
        n=node
        if n.m_Children and n.m_Children[0].m_Type=='Array':n=n.m_Children[0]
        data=next((c for c in n.m_Children if c.m_Name=='data'),None)
        return [editor_value(v,data) for v in value] if data else value
    return value
def editor_tree(p): return editor_value(tree(p),objects[p]._get_typetree_node())
def clean(v):
    if isinstance(v,float) and not math.isfinite(v):return 0.0
    if isinstance(v,list):return [clean(x) for x in v]
    if isinstance(v,dict):return {k:clean(x) for k,x in v.items()}
    return v
def transform(g): return next(pid(c['component']) for c in g['m_Component'] if objects[pid(c['component'])].type.name in ('Transform','RectTransform'))
def inspect():
    names=json.loads((OUT/'coin-names.json').read_text())
    for n in names:
        if n['type']!='GameObject' or not any(w in n['name'] for w in ['Particles','Tracer']):continue
        g=tree(n['id']);tid=transform(g); seq=list(traverse(tid));print(n['id'],n['name'],len(seq),[(path,[(pid(c['component']),objects[pid(c['component'])].type.name) for c in go['m_Component']]) for _,go,path in seq])
OFFSET = 387000000
def iid(p): return p+OFFSET if p else 0
def remap(v):
    if isinstance(v,dict):
        if 'm_PathID' in v and v['m_FileID']==0:return dict(m_FileID=0,m_PathID=iid(v['m_PathID']))
        return {k:remap(x) for k,x in v.items()}
    if isinstance(v,list):return [remap(x) for x in v]
    return v
def export():
    from PIL import Image
    original=OUT/'NativeVFXOriginal';original.mkdir(exist_ok=True)
    texture_dir=OUT/'Textures';texture_dir.mkdir(exist_ok=True)
    selected=[(54592,'CoinItemExplodeParticles'),(58260,'RemainingMovesCoinTracer'),(25684,'CoinHitParticles'),(33370,'CoinCollectHitParticles')]
    prefabs=[];needed=set();owned=set();notes=[];module_summary=[]
    for gid,label in selected:
        root=transform(tree(gid));nodes=[]
        for tid,g,path in traverse(root):
            tr=tree(tid); comps=[];owned.update([tid,pid(tr['m_GameObject'])])
            for c in g['m_Component']:
                cp=pid(c['component']);typ=objects[cp].type.name
                if typ=='MonoBehaviour':
                    notes.append(dict(prefab=label,path=path,sourcePathId=cp,note='MonoBehaviour data saved separately; runtime script logic is not a native particle module.'))
                    # Managed component type information is stripped, raw data remains exact.
                    (original/f'{cp}-MonoBehaviour.unitybin').write_bytes(objects[cp].get_raw_data())
                    continue
                if typ not in ['Transform','ParticleSystem','ParticleSystemRenderer','SpriteRenderer','LineRenderer','SortingGroup']:
                    notes.append(dict(prefab=label,path=path,unsupported=typ,sourcePathId=cp));continue
                owned.add(cp);t=tree(cp);needed.update((r['m_FileID'],pid(r)) for r in refs(t) if pid(r))
                native=clean(editor_tree(cp))
                (original/f'{cp}-{typ}.json').write_text(json.dumps(native,separators=(',',':')),encoding='utf8')
                comps.append(dict(pathId=iid(cp),sourcePathId=cp,type=typ,json=json.dumps(remap(native),separators=(',',':'))))
                if typ=='ParticleSystem':
                    init=t['InitialModule'];emission=t['EmissionModule']
                    module_summary.append(dict(prefab=label,path=path,sourcePathId=cp,duration=t['lengthInSec'],loop=t['looping'],simulationSpace=t['moveWithTransform'],scalingMode=t['scalingMode'],startLifetime=init['startLifetime'],startSize=init['startSize'],startSpeed=init['startSpeed'],emission=emission))
            nodes.append(dict(pathId=iid(pid(tr['m_GameObject'])),sourcePathId=pid(tr['m_GameObject']),transformId=iid(tid),sourceTransformId=tid,parentTransformId=iid(pid(tr['m_Father'])) if tid!=root else 0,name=g['m_Name'],active=g['m_IsActive'],localPosition=tr['m_LocalPosition'],localRotation=tr['m_LocalRotation'],localScale=tr['m_LocalScale'],components=comps))
        prefabs.append(dict(name=label,sourceName=tree(gid)['m_Name'],sourcePathId=gid,nodes=nodes))
    global_env=UnityPy.load(str(ROOT/'Design/Inspection-rfv387/data.unity3d_data/globalgamemanagers.assets'))
    globals={o.path_id:o for o in global_env.objects}
    mats=[];texture_ids=set();sprite_ids=set();external=set()
    for f,p in sorted(needed):
        if f!=0:external.add((f,p));continue
        if p in owned:continue
        ob=objects[p]
        if ob.type.name=='Sprite':sprite_ids.add(p)
        elif ob.type.name=='Material':
            t=tree(p);sh=t['m_Shader'];shader=tree(pid(sh)) if sh['m_FileID']==0 else globals[pid(sh)].read_typetree()
            m=dict(pathId=iid(p),sourcePathId=p,name=t['m_Name'],shaderName=shader['m_ParsedForm']['m_Name'],queue=t['m_CustomRenderQueue'],keywords=' '.join(t.get('m_ValidKeywords',[])),textures=[],floats=[],colors=[])
            for key,v in t['m_SavedProperties']['m_TexEnvs']:
                tp=pid(v['m_Texture']);m['textures'].append(dict(name=key,pathId=iid(tp),scale=v['m_Scale'],offset=v['m_Offset']))
                if tp and v['m_Texture']['m_FileID']==0:texture_ids.add(tp)
            m['floats']=[dict(name=k,value=v) for k,v in t['m_SavedProperties']['m_Floats']]
            m['colors']=[dict(name=k,color=clean(v)) for k,v in t['m_SavedProperties']['m_Colors']]
            mats.append(m)
            (original/f'{p}-Material.json').write_text(json.dumps(t,indent=2),encoding='utf8')
        elif ob.type.name=='Transform':notes.append(dict(dependencyPathId=p,type='Transform',note='Source parent outside the extracted hierarchy. Importer deliberately detaches prefab roots; component Transform JSON is retained for provenance and not applied.'))
        else:notes.append(dict(dependencyPathId=p,unsupported=ob.type.name,name=ob.peek_name()))
    sprites=[]
    for p in sorted(sprite_ids):
        t=tree(p);rd=t['m_RD'];tp=pid(rd['texture']);texture_ids.add(tp)
        assert rd['settingsRaw']==0,(p,rd['settingsRaw'])
        assert t['m_Rect']==rd['textureRect'],p
        sprites.append(dict(sourcePathId=iid(p),originalSourcePathId=p,sourceTexturePathId=iid(tp),originalSourceTexturePathId=tp,name=t['m_Name'],rect=t['m_Rect'],pivot=t['m_Pivot'],border=t['m_Border'],pixelsPerUnit=t['m_PixelsToUnits']))
    textures=[]
    old_catalog=json.loads((ROOT/'Assets/ReferenceGameplay/Art/NativeSpriteCatalog.json').read_text())
    for p in sorted(texture_ids):
        tx=objects[p].read();im=tx.image.convert('RGBA');out=texture_dir/f'rfv387-{p}-{tx.m_Name}.png';im.save(out)
        same=[]
        for existing in old_catalog['textures']:
            if existing['name']!=tx.m_Name:continue
            existing_file=ROOT/existing['assetPath'];old=Image.open(existing_file).convert('RGBA')
            if old.size==im.size and old.tobytes()==im.tobytes():same.append(existing['assetPath'])
        textures.append(dict(pathId=iid(p),sourcePathId=p,name=tx.m_Name,assetPath=f'Assets/ReferenceGameplay/VFX/Textures/rfv387-{p}-{tx.m_Name}.png',extractedPath=out.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(out.read_bytes()).hexdigest(),decodedRgbaSHA256=hashlib.sha256(im.tobytes()).hexdigest(),width=im.width,height=im.height,identicalExistingAssets=same))
    doc=dict(source='royal_thron_rfv387.apk/resources.assets',integrationIdOffset=OFFSET,prefabs=prefabs,materials=mats,textures=textures,sprites=sprites,externalReferences=sorted(external),limitations=notes)
    (OUT/'NativeCoinParticles.json').write_text(json.dumps(doc,separators=(',',':')),encoding='utf8')
    (OUT/'NativeCoinParticleSummary.json').write_text(json.dumps(module_summary,indent=2),encoding='utf8')
    print('VFX',len(prefabs),'systems',len(module_summary),'materials',[(m['name'],m['shaderName']) for m in mats],'textures',[(t['name'],t['identicalExistingAssets']) for t in textures],'sprites',len(sprites),'external',external)
    # APK stream bytes are used directly, preserving per-clip provenance even for duplicate names.
    apk=ROOT/'Design/Backup/apk/royal_thron_rfv387.apk'
    with zipfile.ZipFile(apk) as z: data=z.read('assets/bin/Data/resources.resource')
    import fmod_toolkit
    audio_dir=OUT/'Audio';audio_dir.mkdir(exist_ok=True)
    audio=[]
    for p in [17638,17675,17679,17782]:
        t=tree(p);r=t['m_Resource'];payload=data[r['m_Offset']:r['m_Offset']+r['m_Size']]
        assert len(payload)==r['m_Size']
        entry=dict(name=t['m_Name'],sourcePathId=p,sourceFile='resources.assets',sourceResource='assets/bin/Data/resources.resource',sourceOffset=r['m_Offset'],sourceSize=r['m_Size'],sourcePayloadSHA256=hashlib.sha256(payload).hexdigest(),length=t['m_Length'],channels=t['m_Channels'],sampleRate=t['m_Frequency'],decoded=[])
        (audio_dir/f'{t["m_Name"]}__rfv387_{p}.fsb').write_bytes(payload)
        try:
            wavs=fmod_toolkit.raw_to_wav(payload,t['m_Name'],t['m_Channels'],t['m_Frequency'])
            for index,(name,wav) in enumerate(wavs.items()):
                path=audio_dir/f'{t["m_Name"]}__rfv387_{p}{"_"+str(index) if index else ""}.wav';path.write_bytes(wav)
                with wave.open(io.BytesIO(wav),'rb') as w: info=dict(channels=w.getnchannels(),sampleRate=w.getframerate(),frames=w.getnframes(),length=w.getnframes()/w.getframerate())
                matches=[p.relative_to(ROOT).as_posix() for p in (ROOT/'Assets/ReferenceGameplay/Audio').rglob('coin*.wav') if p.read_bytes()==wav]
                entry['decoded'].append(dict(path=path.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(wav).hexdigest(),identicalExistingAssets=matches,**info))
        except Exception as e:entry['decodeError']=str(e)
        audio.append(entry)
    (OUT/'CoinAudioManifest.json').write_text(json.dumps(audio,indent=2),encoding='utf8')
    print('AUDIO',[(a['name'],a['sourcePathId'],len(a['decoded']),a.get('decodeError')) for a in audio])
if __name__=='__main__':
    if '--inspect' in sys.argv:inspect()
    else:export()
