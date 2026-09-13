"""Package a standalone Area 1 motion preview. Does not write game assets."""
import base64
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
ART = ROOT / "Assets/JellyGarden/Resources/Story/TreasureHunt/Art"
html = (HERE / "preview.template.html").read_text()
for name, state in (("BEFORE", 0), ("AFTER", 1)):
    encoded = base64.b64encode((ART / f"area_001_state{state}.png").read_bytes()).decode()
    html = html.replace(f"__{name}_DATA__", "data:image/png;base64," + encoded)
(HERE / "index.html").write_text(html)
print("Packaged Area 1 preview:", HERE / "index.html")
