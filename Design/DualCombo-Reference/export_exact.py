
exec(__import__('pathlib').Path(r'G:/Game/JellyGarden-Codex/Design/DualCombo-Reference/audit_source.py').read_text().split('effects=[]')[0])
import fmod_toolkit
out=ROOT/'Design/DualCombo-Reference'
art=json.loads((out/'particle-art-comparison.json').read_text());sprites=[];textures=[]
# Dedicated body artwork, in addition to all particle dependencies.
bodyids=set()
for rootid in (54127,54128):
 for _,g,_ in walk(tr_id(tree(rootid))):
  for c in g['m_Component']:
   p=pid(c['component'])
   if objs[p].type.name=='SpriteRenderer':
    r=tree(p)['m_Sprite']
    if pid(r):bodyids.add(pid(r))
for clip in json.loads((out/'decoded-combo-clips.json').read_text()):
 bodyids.update(k['objectPathId'] for c in clip['curves'] for k in c['keys'] if k.get('objectPathId'))
ids=sorted({x['sourcePathId'] for x in art}|bodyids)
texdir=ROOT/'Assets/ReferenceGameplay/VFX/Textures';texdir.mkdir(exist_ok=True)
for p in ids:
 t=tree(p);im=objs[p].read().image.convert('RGBA');path=texdir/f'rfv387-combo-{p}.png';im.save(path)
 sprites.append(dict(sourcePathId=988000000+p,sourceTexturePathId=988000000+p,name=t['m_Name'],rect=dict(x=0,y=0,width=im.width,height=im.height),pivot=t['m_Pivot'],border=t['m_Border'],pixelsPerUnit=t['m_PixelsToUnits']))
 textures.append(dict(pathId=988000000+p,name=t['m_Name'],assetPath=path.relative_to(ROOT).as_posix()))
(out/'ExactComboSprites.json').write_text(json.dumps(dict(prefabs=[],materials=[],textures=textures,sprites=sprites),separators=(',',':')))
with zipfile.ZipFile(apk) as z:stream=z.read('assets/bin/Data/resources.resource')
audios=[]
for p in (17431,17641,17741):
 t=tree(p);r=t['m_Resource'];buf=stream[r['m_Offset']:r['m_Offset']+r['m_Size']];waves=fmod_toolkit.raw_to_wav(buf,t['m_Name'],t['m_Channels'],t['m_Frequency']);assert len(waves)==1
 name=t['m_Name']+f'__rfv387_{p}';path=ROOT/f'Assets/ReferenceGameplay/Audio/Resources/ReferenceGameplay/Audio/{name}.wav';path.write_bytes(next(iter(waves.values())))
 audios.append(dict(name=name,length=t['m_Length'],channels=t['m_Channels'],frequency=t['m_Frequency'],path=path.relative_to(ROOT).as_posix(),payloadSHA256=hashlib.sha256(buf).hexdigest()))
(out/'ExactComboAudio.json').write_text(json.dumps(audios,indent=2))
print('Exported',len(sprites),'exact sprites and',len(audios),'native combo cues');print(audios)
