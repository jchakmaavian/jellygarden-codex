"""Refresh the static, offline gallery from generated image records."""
import json
import shutil
from pathlib import Path

root = Path(__file__).resolve().parent
inventory = json.loads((root / 'inventory.json').read_text())
art = root.parents[1] / 'Assets/JellyGarden/Resources/Story/Art'
before = root / 'before'
before.mkdir(exist_ok=True)
areas = []
for area in inventory['areas']:
    area = dict(area)
    old = art / f"area_{area['id']:03d}_state0.jpg"
    area['beforeLabel'] = 'Original starting scene'
    if not old.exists():
        old = art / f"area_{area['id']:03d}_restored.jpg"
        area['beforeLabel'] = 'Original finished scene'
    backup = before / old.name
    if not backup.exists():
        shutil.copy2(old, backup)
    area['before'] = backup.relative_to(root).as_posix()
    area['states'] = []
    folder = root / 'v1' / f"area_{area['id']:03d}".replace('_', '-')
    for record_path in sorted(folder.glob('state-*.json')):
        record = json.loads(record_path.read_text())
        image = record_path.with_suffix('.png')
        if image.exists():
            area['states'].append({'state': record['state'], 'label': record['label'], 'path': image.relative_to(root).as_posix()})
    areas.append(area)
(root / 'data.js').write_text('window.KING_LEO_AREAS = ' + json.dumps({'areas': areas}, ensure_ascii=False, indent=2) + ';\n')
print(f"Gallery: {sum(len(a['states']) for a in areas)}/{inventory['stateCount']} images across {len(areas)} areas")
