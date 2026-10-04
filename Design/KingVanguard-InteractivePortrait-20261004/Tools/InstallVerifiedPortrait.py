from pathlib import Path
import shutil

project = Path(__file__).resolve().parents[3]
task = project / 'Design/KingVanguard-InteractivePortrait-20261004'
preview = project.parent / 'KingVanguard-RigPreview-20261004'
reservations = [p for p in project.glob('*SESSION-IS-USING-THE-UNITY-EDITOR.txt') if not p.name.startswith('KING-')]
if reservations:
    raise RuntimeError('Main editor still reserved: ' + ', '.join(p.name for p in reservations))

for name, folder in [('KingVanguardCharacter.cs', 'Scripts'), ('KingVanguardLaughter.cs', 'Scripts'), ('KingImpactLaughChecks.cs', 'Editor')]:
    target = project / 'Assets/Characters/KingVenguard/V2' / folder / name
    if target.read_bytes() != (task / 'Before' / name).read_bytes():
        raise RuntimeError('Concurrent source change: ' + str(target))

for name, folder in [('KingVanguardCharacter.cs', 'Scripts'), ('KingVanguardLaughter.cs', 'Scripts'), ('KingImpactLaughChecks.cs', 'Editor'), ('KingVanguardPaintedRig.cs', 'Scripts'), ('KingVanguardPaintedGraphic.cs', 'Scripts')]:
    relative = Path('Assets/Characters/KingVenguard/V2') / folder / name
    target = project / relative
    shutil.copyfile(task / 'Staged' / name, target)
    if not Path(str(target) + '.meta').exists():
        shutil.copyfile(Path(str(preview / relative) + '.meta'), Path(str(target) + '.meta'))

relative = Path('Assets/Characters/KingVenguard/V2/Resources/KingVanguard/Interactive')
destination = project / relative
destination.mkdir(exist_ok=True)
for source in (preview / relative).iterdir():
    if source.is_file():
        shutil.copyfile(source, destination / source.name)
shutil.copyfile(Path(str(preview / relative) + '.meta'), Path(str(destination) + '.meta'))
print('Installed verified portrait scripts and matching artwork; preserved existing asset GUIDs.')
