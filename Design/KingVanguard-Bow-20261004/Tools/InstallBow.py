"""Install the verified bow after the shared editor is released; preserve GUIDs."""
from pathlib import Path
import shutil, uuid, hashlib
task=Path(__file__).resolve().parents[1]
root=task.parents[1]
reserved=[p.name for p in root.glob('*SESSION*USING*UNITY*') if not p.name.startswith('KING-')]
if reserved:raise SystemExit('Editor reserved: '+', '.join(reserved))
base=root/'Assets/Characters/KingVenguard/V2'
for name in ['KingVanguardPaintedGraphic.cs','KingVanguardPerformance.cs','KingVanguardFaceMesh.cs','KingVanguardPainted.shader']:
    dest=base/('Resources/KingVanguard/Interactive' if name.endswith('.shader') else 'Scripts')/name
    before=task/'Before'/name
    if before.read_text()!=dest.read_text():raise SystemExit('Concurrent source change: '+name)
for p in (task/'Staged').iterdir():
    dest=base/('Resources/KingVanguard/Interactive' if p.name.endswith('.shader') else 'Scripts')/p.name
    shutil.copy2(p,dest)
for name in ['King-Curious-Bow.png','King-Curious-Bow.png.meta']:
    shutil.copy2(task/'Source'/name,base/'Resources/KingVanguard/Interactive'/name)
dest=base/'Editor/KingBowChecks.cs'
shutil.copy2(task/'Tools/KingBowChecks.cs',dest)
meta=Path(str(dest)+'.meta')
if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n')
png=base/'Resources/KingVanguard/Interactive/King-Curious-Bow.png'
assert png.read_bytes()==(task/'Source/King-Curious-Bow.png').read_bytes()
print('Installed approved bow artwork and runtime. Source PNG SHA256:',hashlib.sha256(png.read_bytes()).hexdigest())
