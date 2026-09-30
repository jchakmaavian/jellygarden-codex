from pathlib import Path
import sys,json
root=Path.cwd();sys.path.insert(0,str(root/'Design/ReferenceGameplayImport/Media/Tools/Python'));import UnityPy
p=root/'Design/Inspection-Levels01-50/ReferencePackage/datapack.unity3d_data/sharedassets2.assets'
e=UnityPy.load(str(p));o={x.path_id:x for x in e.objects}
print('EXTERNALS',[(str(f.path)) for f in next(iter(e.files.values())).externals])
for k,v in o.items():
 if v.type.name in ('Sprite','Texture2D','Material'):
  d=v.read_typetree();print(k,v.type.name,d.get('m_Name'),d.get('m_Rect'),d.get('m_PixelsToUnits'))
  if v.type.name=='Sprite':
   try:
    im=v.read().image;dest=root/'Design/ReferenceGameplayLayout/SourceSprites';dest.mkdir(exist_ok=True);im.save(dest/(str(k)+'-'+d['m_Name'].replace('/','_')+'.png'))
   except Exception as ex:print('IMAGE ERROR',str(ex)[:200])
rows=json.loads((root/'Design/ReferenceGameplayLayout/scene-layout.json').read_text())
for r in rows:
 if r['path'].endswith(('/Background','/Gear','/GameCamera','/Title','/MoveCount')):
  print('DETAIL',r['path'],json.dumps(r['components'])[:1300])
