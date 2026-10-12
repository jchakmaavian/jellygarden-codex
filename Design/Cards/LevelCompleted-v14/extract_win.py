from pathlib import Path
import sys,types,json
ROOT=Path.cwd()
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
tools=ROOT/'Design/ReferenceGameplayImport/Media/Tools'
backup=Path('G:/Game/old Backup/JellyGarden-Codex-20261005/Design/Inspection-rfv387')
out=ROOT/'Design/Cards/LevelCompleted-v14';out.mkdir(parents=True,exist_ok=True)
for name in ['extract_media','export_visuals','export_particles']:
 m=types.ModuleType(name);m.__file__=str(tools/(name+'.py'));sys.modules[name]=m
 source=(tools/(name+'.py')).read_text()
 source=source.replace("ROOT/'Design/Inspection-Levels01-50/ReferencePackage/datapack.unity3d_data/resources.assets'",repr(str(backup/'datapack.unity3d_data/resources.assets')))
 source=source.replace("ROOT/'Design/Inspection-Levels01-50/ReferencePackage/data.unity3d_data/globalgamemanagers.assets'",repr(str(backup/'data.unity3d_data/globalgamemanagers.assets')))
 if name=='export_particles':
  source=source.replace("art=json.loads((ROOT/'Assets/ReferenceGameplay/Art/NativeSpriteCatalog.json').read_text()); allsprites={s['sourcePathId']:s for s in art['sprites']}; alltex={s['pathId']:s for s in art['textures']}", """allsprites={};alltex={}
    for sp in sprites:
        st=tree(sp);rd=st['m_RD']
        allsprites[sp]=dict(sourcePathId=sp,sourceTexturePathId=pid(rd['texture']),name=st['m_Name'],rect=rd['textureRect'],pivot=st['m_Pivot'],border=st['m_Border'],pixelsPerUnit=st['m_PixelsToUnits'])""")
  source=source.replace("ROOT/'Assets/ReferenceGameplay/VFX/Textures'","ROOT/'Assets/ReferenceGameplay/WinVFX/Textures'")
 exec(compile(source,m.__file__,'exec'),m.__dict__)
 m.OUT=out
e=sys.modules['export_particles']
e.main([38664,45587],'NativeWinParticles.json')
d=json.loads((out/'NativeWinParticles.json').read_text())
print('NODES',[(p['name'],len(p['nodes'])) for p in d['prefabs']])
(ROOT/'Assets/ReferenceGameplay/WinVFX/991-star-emitter.obj').write_text(e.objects[991].read().export())
