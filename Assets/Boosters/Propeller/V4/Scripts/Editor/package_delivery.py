from pathlib import Path
import hashlib,json,re,subprocess,zipfile
P=Path(__file__).resolve().parents[6];R=P/'Assets/Boosters/Propeller/V4';OUT=R/'Animation/Delivery';OUT.mkdir(exist_ok=True)
integration=['Assets/JellyGarden/Scripts/Item.cs','Assets/JellyGarden/Scripts/Juice/RoyalPieceSkin.cs','Assets/JellyGarden/Scripts/Juice/RoyalPowerupDirector.cs']
git=['git','-c','safe.directory='+P.as_posix(),'-C',str(P)]
patch=subprocess.check_output(git+['diff','54c2062','--']+integration);(OUT/'Integration.patch').write_bytes(patch)
locked=json.loads((R/'Animation/Review/locked-hashes.json').read_text());changed=[e['path'] for e in locked if hashlib.sha256((P/e['path']).read_bytes()).hexdigest()!=e['hash']]
(R/'Animation/Review/locked-verification.json').write_text(json.dumps({'checked':len(locked),'changed':changed},indent=2));assert not changed,changed
guids=set()
for p in R.rglob('*.meta'):
 m=re.search(r'^guid: (\w+)',p.read_text(),re.M)
 if m:guids.add(m[1])
refs=set()
for pattern in ['*.prefab','*.mat']:
 for p in R.rglob(pattern):refs.update(re.findall(r'guid: ([a-f0-9]{32})',p.read_text()))
external=refs-guids-{'0000000000000000e0000000000000000','0000000000000000f0000000000000000'};assert not external,external
(R/'Animation/Review/asset-reference-check.json').write_text(json.dumps({'referencedGuids':len(refs),'referencesOutsideV4':sorted(external)},indent=2))
with zipfile.ZipFile(OUT/'Propeller-V4-Source.zip','w',zipfile.ZIP_DEFLATED) as z:
 for p in R.rglob('*'):
  if not p.is_file() or any(part.startswith('.') for part in p.relative_to(R).parts) or '/Animation/Delivery/' in p.as_posix():continue
  if '/Animation/' in p.as_posix() and (p.suffix in ['.mp4','.png','.jpg','.wav'] or p.name.endswith(('.mp4.meta','.png.meta','.jpg.meta','.wav.meta'))):continue
  z.write(p,p.relative_to(P))
 for rel in integration+['Assets/Boosters/Propeller/V4.meta']:z.write(P/rel,rel)
 z.writestr('Integration.patch',patch)
print('Independent source, original WAVs and integration package ready. Protected:',len(locked))
