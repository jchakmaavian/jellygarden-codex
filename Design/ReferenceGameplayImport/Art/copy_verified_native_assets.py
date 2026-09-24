"""Copy scoped native Unity outputs after isolated validation; preserve main GUIDs."""
import hashlib, json, re, shutil
from datetime import datetime
from pathlib import Path

project=Path(__file__).resolve().parents[3]
workspace=project/'Design/ReferenceGameplayImport/ImportWorkspace'
report=workspace/'Design/ReferenceGameplayImport/IsolatedImportReport.json'
assert json.loads(report.read_text())['status']=='passed', 'Isolated validation must pass before delivery'
source=workspace/'Assets/ReferenceGameplay'
target=project/'Assets/ReferenceGameplay'
out=project/'Design/ReferenceGameplayImport/Validation'
out.mkdir(parents=True,exist_ok=True)
guid_pattern=re.compile(r'(?m)^guid: ([a-f0-9]{32})$')
guid_map={}
for p in source.rglob('*.meta'):
    q=target/p.relative_to(source)
    if q.exists():
        old=guid_pattern.search(p.read_text());new=guid_pattern.search(q.read_text())
        if old and new and old[1]!=new[1]:guid_map[old[1]]=new[1]

files=[]
for p in source.rglob('*'):
    if p.is_file() and p.suffix in {'.anim','.controller','.prefab','.mat','.asset'}:
        files.extend([p,Path(str(p)+'.meta')])
# Newly generated metadata for existing source dependencies and folders is needed
# so the native asset references resolve without changing existing source GUIDs.
for p in source.rglob('*.meta'):
    q=target/p.relative_to(source)
    dependency=Path(str(q)[:-5])
    if not q.exists() and dependency.exists():files.append(p)
    if dependency.exists() and dependency.suffix.lower() in {'.png','.wav'}:files.append(p)

backup=project/'Design/ReferenceGameplayImport/GeneratedCheckpoint'/datetime.now().strftime('%Y%m%d-%H%M%S')
manifest=[]
for p in sorted(set(files)):
    assert p.exists(),str(p)
    q=target/p.relative_to(source)
    if p.suffix=='.meta' and q.exists() and Path(str(q)[:-5]).suffix.lower() not in {'.png','.wav'}:continue
    data=p.read_bytes()
    if data.startswith((b'%YAML',b'fileFormatVersion:')):
        for old,new in guid_map.items():data=data.replace(old.encode(),new.encode())
    if q.exists() and q.read_bytes()==data:continue
    if q.exists():
        saved=backup/q.relative_to(project);saved.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(q,saved)
    q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes(data)
    manifest.append({'path':q.relative_to(project).as_posix(),'sha256':hashlib.sha256(data).hexdigest(),'bytes':len(data)})

for name in ['IsolatedImportReport.json']:
    shutil.copy2(workspace/'Design/ReferenceGameplayImport'/name,out/name)
for p in (workspace/'Design/ReferenceGameplayImport/Validation').glob('*'):
    if p.is_file():shutil.copy2(p,out/p.name)
shutil.copy2(workspace/'Design/ReferenceGameplayImport/Media/ImportedNativeModules.json',out/'ImportedNativeModules.json')
shutil.copy2(workspace/'Design/ReferenceGameplayImport/Media/ImportNotes.txt',out/'ImportNotes.txt')
if (workspace/'Design/ReferenceGameplayImport/Media/VfxIncomplete.txt').exists():
    shutil.copy2(workspace/'Design/ReferenceGameplayImport/Media/VfxIncomplete.txt',out/'VfxIncomplete.txt')
shutil.copy2(workspace/'Unity-Import-Verified.log',out/'Unity-Import-Verified.log')
previous=out/'Generated-Asset-Manifest.json'
if previous.exists():
    prior=json.loads(previous.read_text());by_path={entry['path']:entry for entry in prior['files']};by_path.update({entry['path']:entry for entry in manifest});manifest=sorted(by_path.values(),key=lambda e:e['path'])
result={'scope':'Generated native Unity assets, validated texture/audio importer settings, and missing dependency metadata; existing GUIDs preserved','guidRemap':guid_map,'files':manifest,'copiedFileCount':len(manifest),'mainGameplayVerified':False}
(out/'Generated-Asset-Manifest.json').write_text(json.dumps(result,indent=2))
print(json.dumps({'copied':len(manifest),'guidRemaps':len(guid_map),'manifest':str(out/'Generated-Asset-Manifest.json')}))
