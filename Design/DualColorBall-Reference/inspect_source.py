from pathlib import Path
import sys,json,zipfile,hashlib
ROOT=Path(r'G:/Game/JellyGarden-Codex')
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
import UnityPy
apk=Path(r'G:/Game/old Backup/JellyGarden-Codex-20261005/Design/Backup/apk/royal_thron_rfv387.apk')
pack=ROOT/'Design/Inspection-Levels01-50/ReferencePackage/datapack.unity3d'
with zipfile.ZipFile(apk) as z:
    entries=[n for n in z.namelist() if 'datapack' in n]
    print('PACKS',entries)
    source=z.read(entries[0])
    digest=hashlib.sha256(source).hexdigest()
    existing= digest==hashlib.sha256(pack.read_bytes()).hexdigest()
env=UnityPy.load(source)
asset=next(a for a in env.assets if a.name=='resources.assets')
objs={o.path_id:o for o in asset.objects.values()}
found=[]
for o in objs.values():
    if o.type.name not in ('GameObject','AnimationClip','AnimatorController','AudioClip','Material'):continue
    n=o.peek_name() or ''
    if any(s in n.lower() for s in ('ballball','ball_ball','lightball_lightball_combo','ballcombo')):
        found.append(dict(id=o.path_id,type=o.type.name,name=n))
        if o.type.name=='GameObject' or (o.type.name=='AnimationClip' and 'ballball' in n.lower()):
            (ROOT/'Design/DualColorBall-Reference'/f'{o.type.name}-{o.path_id}.json').write_text(json.dumps(o.read_typetree(),indent=2))
report=dict(apk=str(apk),apkSHA256=hashlib.sha256(apk.read_bytes()).hexdigest(),datapackSHA256=digest,existingExtractionMatches=existing,assets=found)
(ROOT/'Design/DualColorBall-Reference/source-inventory.json').write_text(json.dumps(report,indent=2))
print(json.dumps(found,indent=2))
