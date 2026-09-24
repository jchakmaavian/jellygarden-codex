from pathlib import Path
import json,sys,uuid,re
from PIL import Image,ImageDraw
root=Path.cwd();sys.path.insert(0,str(root/'Design/ReferenceGameplayImport/Media/Tools/Python'));import UnityPy
out=root/'Design/ReferenceHomeFinish'; dest=root/'Assets/ReferenceGameplay/Home/Resources/ReferenceGameplay/Home';dest.mkdir(parents=True,exist_ok=True)
env=UnityPy.load(str(root/'Design/Inspection-Levels01-50/ReferencePackage/datapack.unity3d_data/resources.assets'));objs={o.path_id:o for o in env.objects}
ids=[16613,17034,17072,22081,16282,16715,21706,21591,21830,16674,16310,16521,21316,21955,17031,16593,16395,21453,16927,22238,16525,17106,16492,21286,16361]
for o in env.objects:
 if o.type.name=='Sprite':
  d=o.read_typetree();n=d['m_Name']
  if 'profile' in n.lower() and ('frame' in n.lower() or 'default' in n.lower()):print(o.path_id,n)
rows=[];sheet=Image.new('RGB',(800,((len(ids)+4)//5)*155),(45,59,80));draw=ImageDraw.Draw(sheet)
base=(root/'Assets/ReferenceGameplay/Tools/Resources/ReferenceGameplay/Tools/16599.png.meta').read_text()
for i,pid in enumerate(ids):
 o=objs[pid];d=o.read_typetree();s=o.read();im=s.image;im.save(dest/f'{pid}.png')
 existing=dest/f'{pid}.png.meta'
 if existing.exists():
  rows.append(dict(id=pid,name=d['m_Name'],size=im.size,ppu=d['m_PixelsToUnits'],pivot=d['m_Pivot'],border=d['m_Border']));continue
 meta=base;meta=re.sub(r'guid: [0-9a-f]+','guid: '+uuid.uuid4().hex,meta,count=1)
 b=d['m_Border'];p=d['m_Pivot'];meta=re.sub(r'spriteBorder: .*',f"spriteBorder: {{x: {b['x']}, y: {b['y']}, z: {b['z']}, w: {b['w']}}}",meta);meta=re.sub(r'spritePivot: .*',f"spritePivot: {{x: {p['x']}, y: {p['y']}}}",meta)
 (dest/f'{pid}.png.meta').write_text(meta)
 rows.append(dict(id=pid,name=d['m_Name'],size=im.size,ppu=d['m_PixelsToUnits'],pivot=p,border=b))
 thumb=im.copy();thumb.thumbnail((142,112));x=i%5*160;y=i//5*155;sheet.paste(thumb,(x+(160-thumb.width)//2,y),thumb);draw.text((x+3,y+113),str(pid)+' '+d['m_Name'][:16],fill='white')
(out/'home-art-inventory.json').write_text(json.dumps(rows,indent=2));sheet.save(out/'source-ui.png')
print('Exported',len(rows))
