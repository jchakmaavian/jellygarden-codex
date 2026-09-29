"""Check provenance against the supplied APK and imported media."""
from pathlib import Path
import hashlib,json,zipfile
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
report={}
for name in ['Archer','Hammer']:
    source=json.loads((ROOT/f'Design/{name}-Reference/source.json').read_text())
    assert sha(ROOT/source['apk'])==source['apkSHA256']
    checked=[]
    for asset in source['sprites']+source['audio']:
        path=ROOT/asset['assetPath']
        if not path.exists():path=ROOT/f'Assets/ReferenceGameplay/Tools/{name}/Resources/ReferenceGameplay/{name}'/Path(asset['assetPath']).name
        assert path.exists(),path
        assert sha(path)==asset['sha256'],path
        checked.append(str(path.relative_to(ROOT)))
    report[name]={'apkSHA256':source['apkSHA256'],'verifiedMediaCount':len(checked),'media':checked}
apk=ROOT/'Design/Backup/apk/royal_thron_rfv387.apk'
with zipfile.ZipFile(apk) as archive:
    entry=next(n for n in archive.namelist() if n.endswith('/datapack.unity3d'))
    packaged=hashlib.sha256(archive.read(entry)).hexdigest()
local=sha(ROOT/'Design/Inspection-rfv387/datapack.unity3d')
assert packaged==local
report['payload']={'entry':entry,'sha256':packaged,'localExtractionMatches':True}
(OUT/'source-verification.json').write_text(json.dumps(report,indent=2))
print('PASS archer and hammer media hashes; APK and local datapack hashes match.')
