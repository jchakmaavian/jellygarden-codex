"""Package Area 1 task 2–4 motion studies, outside the Unity project assets."""
import base64
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
ART = ROOT / 'Assets/JellyGarden/Resources/Story/TreasureHunt/Art'
html = (HERE / 'preview.template.html').read_text()
for state in range(1, 5):
    encoded = base64.b64encode((ART / f'area_001_state{state}.png').read_bytes()).decode()
    html = html.replace(f'__STATE_{state}__', 'data:image/png;base64,' + encoded)
(HERE / 'index.html').write_text(html)
print('Packaged three standalone Area 1 previews.')
