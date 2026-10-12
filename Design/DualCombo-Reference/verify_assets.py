exec(__import__('pathlib').Path(r'G:/Game/JellyGarden-Codex/Design/DualColorBall-Reference/inspect_combo.py').read_text().split('roots=[]')[0])
from PIL import Image
import re
out=ROOT/'Design/DualCombo-Reference';document=json.loads((out/'ExactComboSprites.json').read_text());issues=[];checks=[]
for entry in document['sprites']:
 p=entry['sourcePathId']-988000000;native=objs[p].read().image.convert('RGBA');path=ROOT/f'Assets/ReferenceGameplay/VFX/Textures/rfv387-combo-{p}.png';actual=Image.open(path).convert('RGBA');same=native.size==actual.size and native.tobytes()==actual.tobytes()
 s=(ROOT/f"Assets/ReferenceGameplay/VFX/AtlasSprites/{entry['sourcePathId']}.asset").read_text();texguid=re.search(r'guid: (\w+)',Path(str(path)+'.meta').read_text())[1]
 checks.append(dict(sourceId=p,name=entry['name'],nativeRGBAHash=hashlib.sha256(native.tobytes()).hexdigest(),pixelIdentical=same,textureReferenceValid=texguid in s))
 if not same or texguid not in s:issues.append(p)
(out/'Verification/art-validation.json').write_text(json.dumps(dict(sprites=len(checks),differences=issues,checks=checks),indent=2))
print('Exact sprites',len(checks),'differences',len(issues))
