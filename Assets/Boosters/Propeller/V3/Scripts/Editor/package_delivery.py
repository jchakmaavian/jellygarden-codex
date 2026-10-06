"""Package focused source and evidence for the authorized private backup."""
from pathlib import Path
import hashlib, json, re, subprocess, zipfile
ROOT=Path(__file__).resolve().parents[2]
PROJECT=ROOT.parents[3]
OUT=ROOT/'Animation/Delivery'
OUT.mkdir(exist_ok=True)
integration=['Assets/JellyGarden/Scripts/Item.cs','Assets/JellyGarden/Scripts/Juice/RoyalPieceSkin.cs','Assets/JellyGarden/Scripts/Juice/RoyalPowerupDirector.cs']
git=['git','-c','safe.directory='+PROJECT.as_posix(),'-C',str(PROJECT)]
patch=subprocess.check_output(git+['diff','6cb863d','--']+integration)
(OUT/'Integration.patch').write_bytes(patch)
guids=set()
for p in ROOT.rglob('*.meta'):
    m=re.search(r'^guid: (\w+)',p.read_text(),re.M)
    if m:guids.add(m[1])
references=set()
for ext in ['*.prefab','*.mat']:
    for p in ROOT.rglob(ext):references.update(re.findall(r'guid: ([a-f0-9]{32})',p.read_text()))
external=references-guids-{'0000000000000000e0000000000000000','0000000000000000f0000000000000000'}
assert not external, external
(ROOT/'Animation/Review/asset-reference-check.json').write_text(json.dumps({'referencedGuids':len(references),'referencesOutsideV3':sorted(external)},indent=2))
locked=json.loads((ROOT/'Animation/Review/locked-hashes.json').read_text(encoding='utf-8-sig'))
assert all(hashlib.sha256((PROJECT/e['path']).read_bytes()).hexdigest().upper()==e['hash'].upper() for e in locked)
with zipfile.ZipFile(OUT/'Propeller-V3-Source.zip','w',zipfile.ZIP_DEFLATED) as z:
    for p in ROOT.rglob('*'):
        if not p.is_file() or '/.Capture/' in p.as_posix() or '/.Reference/' in p.as_posix() or '/Animation/Delivery/' in p.as_posix():continue
        if '/Animation/' in p.as_posix() and p.suffix in ['.mp4','.png','.jpg']:continue
        if '/Animation/' in p.as_posix() and p.name.endswith(('.mp4.meta','.png.meta','.jpg.meta')):continue
        z.write(p,p.relative_to(PROJECT))
    for rel in integration:z.write(PROJECT/rel,rel)
    z.writestr('Integration.patch',patch)
    for rel in ['Assets/Boosters.meta','Assets/Boosters/Propeller.meta','Assets/Boosters/Propeller/V3.meta']:
        z.write(PROJECT/rel,rel)
print('Source package ready. Independent GUID references:',len(references),'; protected files:',len(locked))
