from pathlib import Path
import json,sys
from PIL import Image,ImageDraw
root=Path.cwd(); d=json.loads((root/'Design/ReferenceGameplayImport/Art/NativeSpriteCatalog.json').read_text());print(type(d),list(d)[:5] if isinstance(d,dict) else len(d))
a=d.get('sprites',d.get('entries',[])) if isinstance(d,dict) else d
arr=[x for x in a if 'Border-' in x.get('name','') or x.get('name')=='cell_front']
print(json.dumps(arr[:2],indent=2));(root/'Design/ReferenceGameplayLayout/board-sprite-list.json').write_text(json.dumps(arr,indent=2))
sys.path.insert(0,str(root/'Design/ReferenceGameplayImport/Media/Tools/Python'));import UnityPy
e=UnityPy.load(str(root/'Design/Inspection-Levels01-50/ReferencePackage/datapack.unity3d_data/sharedassets2.assets'));ob={o.path_id:o for o in e.objects}
for n in [9,11,12,13,17,18,20,21]:
 t=ob[n].read_typetree();print(n,'pivot',t['m_Pivot'],'border',t['m_Border'])
rows=json.loads((root/'Design/ReferenceGameplayLayout/scene-layout.json').read_text())
for r in rows:
 if ('/Top/Target/' in r['path'] or '/Top/Moves/' in r['path'] or '/Bottom/Settings/' in r['path'] or '/Bottom/Background/' in r['path']) and 'landscape' not in r['path'].lower():
  for c in r['components']:
   if c['type']=='SpriteRenderer' and c['data']['m_Sprite']['m_PathID']:print(r['path'],c['data']['m_Size'],c['data']['m_FlipX'],c['data']['m_DrawMode'])
