from pathlib import Path
import shutil
root=Path(__file__).resolve().parents[3]
task=root/'Design/KingVanguard-Lively-20261004'
preview=root.parent/'KingVanguard-RigPreview-20261004'
reservations=[p for p in root.glob('*SESSION-IS-USING-THE-UNITY-EDITOR.txt') if not p.name.startswith('KING-')]
if reservations: raise RuntimeError('Shared editor reserved: '+', '.join(p.name for p in reservations))
base=Path('Assets/Characters/KingVenguard/V2')
for p in (task/'Before').iterdir():
 folder='Resources/KingVanguard/Interactive' if p.suffix=='.shader' else 'Scripts'
 if (root/base/folder/p.name).read_text()!=p.read_text(): raise RuntimeError('Concurrent King source edit: '+p.name)
for p in (task/'Staged').iterdir():
 if p.suffix not in ['.cs','.shader']:continue
 folder='Resources/KingVanguard/Interactive' if p.suffix=='.shader' else 'Scripts'
 rel=base/folder/p.name
 shutil.copyfile(p,root/rel)
 meta=Path(str(root/rel)+'.meta')
 if not meta.exists():shutil.copyfile(task/'Staged'/(p.name+'.meta'),meta)
shutil.copyfile(task/'Tools/KingLivelyChecks.cs',root/base/'Editor/KingLivelyChecks.cs')
print('Installed verified King motion and facial morph mesh; existing GUIDs preserved.')
