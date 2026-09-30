"""Writes region registration for Unity's decoded atlas. Does not edit images."""
import json
from pathlib import Path
base=Path(__file__).resolve().parents[1]
data=json.loads((base/'Logic/KingDrill/UnityImportData.json').read_text())
lines=['','KingDrill.png','size: 1024,2048','format: RGBA8888','filter: Linear,Linear','repeat: none']
for sprite in data['sprites']:
    name=sprite['name']
    if not any(term in name for term in ('bacak','govde','kol_a_omuz','kol_b_omuz','kol_c_omuz','kol_a_dirsek','kol_a_el','kol_b_el','kafa')):continue
    if name.endswith('_G'):continue
    r=sprite['textureRect'];x,y,w,h=[round(r[k]) for k in ('x','y','width','height')]
    y=2048-y-h
    # Head keeps its face exactly; only the crown portion receives new surface paint.
    if 'kafa' in name:h=105
    lines += ['drill_costume_'+name,'  rotate: false',f'  xy: {x}, {y}',f'  size: {w}, {h}',f'  orig: {w}, {h}','  offset: 0, 0','  index: -1']
(base/'SurfacePaint/KingDrill-technical.atlas').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print('registered',sum(x.startswith('drill_costume_') for x in lines),'costume islands')
