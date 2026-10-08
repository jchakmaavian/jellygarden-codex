exec(__import__('pathlib').Path(r'G:/Game/JellyGarden-Codex/Design/DualColorBall-Reference/inspect_combo.py').read_text().split('roots=[]')[0])
import math
OUT=ROOT/'Design/DualColorBall-Reference'
def refs(v):
    if isinstance(v,dict):
        if 'm_PathID' in v:yield v
        else:
            for x in v.values():yield from refs(x)
    elif isinstance(v,list):
        for x in v:yield from refs(x)
OFFSET=987000000
reuse={11:387000011}
def mapped(p):return reuse.get(p,p+OFFSET) if p else 0
def remap(v):
    if isinstance(v,dict):
        if 'm_PathID' in v:return dict(m_FileID=v['m_FileID'],m_PathID=mapped(pid(v)) if v['m_FileID']==0 else pid(v))
        return {k:remap(x) for k,x in v.items()}
    if isinstance(v,list):return [remap(x) for x in v]
    if isinstance(v,float) and not math.isfinite(v):return 0.0
    return v
prefabs=[];needed=set();owned=set();modules=[]
for gid in (51492,48774):
    root=tr_id(tree(gid));nodes=[]
    for tid,g,path in walk(root):
        t=tree(tid);components=[];owned.update((tid,pid(t['m_GameObject'])))
        for c in g['m_Component']:
            cp=pid(c['component']);typ=objs[cp].type.name
            if typ=='MonoBehaviour':continue
            native=old.editor_value(tree(cp),objs[cp]._get_typetree_node());owned.add(cp)
            needed.update((r['m_FileID'],pid(r)) for r in refs(native) if pid(r))
            components.append(dict(pathId=mapped(cp),type=typ,json=json.dumps(remap(native),separators=(',',':'))))
            if typ=='ParticleSystem':modules.append(dict(prefab=tree(gid)['m_Name'],path=path,sourcePathId=cp,source=remap(native)))
        nodes.append(dict(pathId=mapped(pid(t['m_GameObject'])),transformId=mapped(tid),parentTransformId=mapped(pid(t['m_Father'])) if tid!=root else 0,name=g['m_Name'],active=g['m_IsActive'],localPosition=t['m_LocalPosition'],localRotation=t['m_LocalRotation'],localScale=t['m_LocalScale'],components=components))
    prefabs.append(dict(name=tree(gid)['m_Name'],nodes=nodes))
print('DEPS',[(f,p,objs[p].type.name if f==0 else 'external',objs[p].peek_name() if f==0 else '') for f,p in sorted(needed) if p not in owned])
textures=[];sprites=[];texdir=ROOT/'Assets/ReferenceGameplay/VFX/Textures';texdir.mkdir(exist_ok=True)
for f,p in sorted(needed | {(0,18576)}):
    if p in owned or p in reuse or f:continue
    assert objs[p].type.name=='Sprite',(p,objs[p].type.name)
    t=tree(p);im=objs[p].read().image.convert('RGBA');path=texdir/f'rfv387-dual-{p}.png';im.save(path)
    textures.append(dict(pathId=mapped(p),name=t['m_Name'],assetPath=path.relative_to(ROOT).as_posix()))
    sprites.append(dict(sourcePathId=mapped(p),sourceTexturePathId=mapped(p),name=t['m_Name'],rect=dict(x=0,y=0,width=im.width,height=im.height),pivot=t['m_Pivot'],border=t['m_Border'],pixelsPerUnit=t['m_PixelsToUnits']))
(OUT/'NativeDualColorBallParticles.json').write_text(json.dumps(dict(prefabs=prefabs,materials=[],textures=textures,sprites=sprites),separators=(',',':')))
(OUT/'source-particle-modules.json').write_text(json.dumps(modules,separators=(',',':')))
# Native combo sound is shared only if its compressed payload really matches.
with zipfile.ZipFile(apk) as z:stream=z.read('assets/bin/Data/resources.resource')
t=tree(17531);r=t['m_Resource'];buf=stream[r['m_Offset']:r['m_Offset']+r['m_Size']];h=hashlib.sha256(buf).hexdigest()
manifest=json.loads((ROOT/'Design/ReferenceGameplayImport/Media/AudioManifest.json').read_text());matches=[a for a in manifest if a['name']==t['m_Name'] and a['sourcePayloadSHA256']==h]
if not matches:
    import fmod_toolkit
    waves=fmod_toolkit.raw_to_wav(buf,t['m_Name'],t['m_Channels'],t['m_Frequency'])
    audio=ROOT/'Assets/ReferenceGameplay/Audio/Resources/ReferenceGameplay/Audio/lightball_lightball_combo__rfv387_17531.wav'
    assert len(waves)==1
    audio.write_bytes(next(iter(waves.values())))
    matches=[dict(name=t['m_Name'],pathId=17531,assetPath=audio.relative_to(ROOT).as_posix(),resourcesPath='ReferenceGameplay/Audio/'+audio.stem,sourcePayloadSHA256=h,length=t['m_Length'],channels=t['m_Channels'],sampleRate=t['m_Frequency'])]
(OUT/'import-contract.json').write_text(json.dumps(dict(reusedReferences={str(mapped(11)):'Assets/ReferenceGameplay/VFX/Materials/387000011.mat'},sourceParticleSystems=len(modules),newSprites=len(sprites),sourceSoundId=17531,audio=matches,limitations=['Native MonoBehaviour excluded; C# scheduling reconstructed from authored animation completion and particle lifetimes.','Custom BlackBg mesh-mask runtime is not part of the imported particle hierarchies.']),indent=2))
print('EXPORT',len(prefabs),'prefabs',len(modules),'systems',len(sprites),'sprites','AUDIO',matches[0]['resourcesPath'])
