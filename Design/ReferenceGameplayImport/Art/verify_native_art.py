from pathlib import Path
import json,hashlib,struct,sys
from PIL import Image
ROOT=Path(__file__).resolve().parents[3]
ART=ROOT/'Assets/ReferenceGameplay/Art'
manifest=json.loads((ART/'NativeSpriteCatalog.json').read_text())
atlas={a['name']:Image.open(ROOT/a['assetPath']).convert('RGBA') for a in manifest['textures']}
fail=[];guids=set();paths=set()
for a in manifest['sprites']:
 path=ROOT/a['assetPath'];im=Image.open(path).convert('RGBA');r=a['rect'];src=atlas[a['textureName']]
 x,y,w,h=map(round,(r['x'],r['y'],r['width'],r['height']))
 cut=src.crop((x,src.height-y-h,x+w,src.height-y))
 if im.size!=(w,h) or im.tobytes()!=cut.tobytes():fail.append(a['name']+' pixel mismatch')
 if a['guid'] in guids or a['assetPath'] in paths:fail.append(a['name']+' duplicate identity')
 guids.add(a['guid']);paths.add(a['assetPath'])
 if 'guid: '+a['guid'] not in path.with_suffix('.png.meta').read_text():fail.append(a['name']+' metadata GUID mismatch')
report=dict(spriteCount=len(manifest['sprites']),sourceAtlasCount=len(atlas),nativePixelCropsExact=not fail,stableIdentities=len(guids),nativePixelsPerUnit=sorted(set(a['pixelsPerUnit'] for a in manifest['sprites'])),failures=fail)
(Path(__file__).parent/'art-validation.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
assert not fail
