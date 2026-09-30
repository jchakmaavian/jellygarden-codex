"""Prepare only the unimported TNT+TNT fuse and its one required exact source sprite."""
from audit_boosters import *
sys.path.insert(0,str(TOOLS))
import extract_media as old
gid=51481
needed=set();owned=set();nodes=[];notes=[]
for tid,g,path in traverse(transform(tree(gid))):
    tr=tree(tid);comps=[];owned.update([tid,pid(tr['m_GameObject'])])
    for c in g['m_Component']:
        cp=pid(c['component']);typ=objects[cp].type.name
        if typ=='MonoBehaviour':
            notes.append(dict(path=path,pathId=cp,note='Reference runtime component excluded. Loop lifetime must be owned by host TNT+TNT animation.'))
            continue
        owned.add(cp);t=tree(cp);needed.update((r['m_FileID'],pid(r)) for r in refs(t) if pid(r))
        native=old.editor_value(t,objects[cp]._get_typetree_node())
        comps.append(dict(pathId=cp,type=typ,json=json.dumps(native,separators=(',',':'))))
    nodes.append(dict(pathId=pid(tr['m_GameObject']),transformId=tid,parentTransformId=pid(tr['m_Father']) if tid!=transform(tree(gid)) else 0,name=g['m_Name'],active=g['m_IsActive'],localPosition=tr['m_LocalPosition'],localRotation=tr['m_LocalRotation'],localScale=tr['m_LocalScale'],components=comps))
external=[];deps=[]
globalenv=UnityPy.load(str(ROOT/'Design/Inspection-rfv387/data.unity3d_data/globalgamemanagers.assets'))
global_objects={o.path_id:o for o in globalenv.objects}
catalog=json.loads((ROOT/'Assets/ReferenceGameplay/Art/NativeSpriteCatalog.json').read_text())
old_vfx=json.loads((TOOLS.parent/'NativeParticles.json').read_text())
for f,p in sorted(needed):
    if f:external.append(dict(file=f,pathId=p));continue
    if p in owned:continue
    obj=objects[p];typ=obj.type.name;t=tree(p);entry=dict(pathId=p,type=typ,name=obj.peek_name())
    if typ=='Material':
        sh=t['m_Shader'];shader=tree(pid(sh)) if not sh['m_FileID'] else global_objects[pid(sh)].read_typetree()
        entry.update(shaderName=shader['m_ParsedForm']['m_Name'],native=t,currentNameMatches=[m['pathId'] for m in old_vfx['materials'] if m['name']==t['m_Name']])
    elif typ=='Sprite':
        matches=[s for s in catalog['sprites'] if s['name']==t['m_Name']]
        identical=[]
        image=objects[p].read().image.convert('RGBA')
        for s in matches:
            op=s['sourcePathId']
            if op not in old.objects:continue
            oldim=old.objects[op].read().image.convert('RGBA')
            if image.size==oldim.size and image.tobytes()==oldim.tobytes():identical.append(op)
        entry.update(rect=t['m_Rect'],pivot=t['m_Pivot'],pixelsPerUnit=t['m_PixelsToUnits'],texture=t['m_RD']['texture'],identicalImportedSpriteIds=identical)
    else:entry['native']=t
    deps.append(entry)
write('TntTntComboFuseParticles-source.json',dict(source='royal_thron_rfv387.apk/resources.assets',sourcePathId=gid,prefab=dict(name='TntTntComboFuseParticles',nodes=nodes),dependencies=deps,externalReferences=external,limitations=notes))
print('FUSE',len(nodes),'nodes',[(d['type'],d['pathId'],d['name'],d.get('shaderName'),d.get('identicalImportedSpriteIds')) for d in deps],external)
with zipfile.ZipFile(APK) as z:stream=z.read('assets/bin/Data/resources.resource')
older=json.loads((TOOLS.parent/'AudioManifest.json').read_text());audio=[]
names=json.loads((OUT/'booster-names.json').read_text())
for n in names:
    if n['type']!='AudioClip' or not re.match(r'^(rocket_|tnt_|propeller_(creation|hit|fly|take_off|tnt_combo))',n['name']):continue
    p=n['id'];t=tree(p);r=t['m_Resource'];data=stream[r['m_Offset']:r['m_Offset']+r['m_Size']];h=digest(data)
    previous=[a for a in older if a['name']==n['name']]
    audio.append(dict(pathId=p,name=n['name'],length=t['m_Length'],sampleRate=t['m_Frequency'],channels=t['m_Channels'],payloadSHA256=h,currentAudioMatches=[a['assetPath'] for a in previous if a['sourcePayloadSHA256']==h]))
write('audio-comparison.json',audio)
print('AUDIO',len(audio),'payload-identical',sum(bool(a['currentAudioMatches']) for a in audio))

# The existing importer accepts explicit reused object references. Keep all existing
# material/sprite GUIDs and bytes, and create only the exact pixel-different dependency.
OFFSET=388000000
reuse={3:387000003,11:387000011,18596:16582,24183:21650}
def mapped(p):return reuse.get(p,p+OFFSET) if p else 0
def remap(v):
    if isinstance(v,dict):
        if 'm_PathID' in v:return dict(m_FileID=v['m_FileID'],m_PathID=mapped(pid(v)) if v['m_FileID']==0 else pid(v))
        return {k:remap(x) for k,x in v.items()}
    if isinstance(v,list):return [remap(x) for x in v]
    if isinstance(v,float) and not math.isfinite(v):return 0.0
    return v
import_nodes=[]
for n in nodes:
    row=dict(n)
    for key in ['pathId','transformId','parentTransformId']:row[key]=mapped(row[key])
    row['components']=[dict(pathId=mapped(c['pathId']),type=c['type'],json=json.dumps(remap(json.loads(c['json'])),separators=(',',':'))) for c in n['components']]
    import_nodes.append(row)
texture_path=ROOT/'Assets/ReferenceGameplay/VFX/Textures/rfv387-24556-Effects-color_particle.png'
texture_path.parent.mkdir(parents=True,exist_ok=True)
im=objects[24556].read().image.convert('RGBA');im.save(texture_path)
t=tree(24556)
sprite=dict(sourcePathId=mapped(24556),sourceTexturePathId=mapped(24556),name=t['m_Name'],rect=dict(x=0,y=0,width=im.width,height=im.height),pivot=t['m_Pivot'],border=t['m_Border'],pixelsPerUnit=t['m_PixelsToUnits'])
write('TntTntComboFuseParticles-import.json',dict(prefabs=[dict(name='TntTntComboFuseParticles',nodes=import_nodes)],materials=[],textures=[dict(pathId=mapped(24556),name=t['m_Name'],assetPath=texture_path.relative_to(ROOT).as_posix())],sprites=[sprite]))
write('missing-fuse-import-contract.json',dict(sourceGameObjectId=gid,integrationIdOffset=OFFSET,reusedReferences={str(v):('Assets/ReferenceGameplay/VFX/Materials/'+str(v)+'.mat' if k in (3,11) else 'Assets/ReferenceGameplay/VFX/AtlasSprites/'+str(v)+'.asset') for k,v in reuse.items()},newTexture=texture_path.relative_to(ROOT).as_posix(),textureSHA256=digest(texture_path.read_bytes()),sourceDecodedRGBA=digest(im.tobytes()),notes=['Only the missing TNT+TNT fuse prefab and one exact source sprite are imported.','Native ParticleSystem stop actions and sorting-layer IDs use the established importer compatibility policy.','Runtime MonoBehaviour is excluded; owner must stop this looping fuse at detonation/recycle.']))
