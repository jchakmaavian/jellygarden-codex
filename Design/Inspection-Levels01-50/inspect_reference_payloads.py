"""Read-only, standard-library inspection of an already unpacked reference APK.

Uses object offsets emitted by Unity binary2text; no assets are installed into Assets.
"""
from pathlib import Path
import csv
import hashlib
import json
import re
import struct

ROOT = Path(__file__).resolve().parent
PACKAGE = ROOT / "ReferencePackage"
DATA = (PACKAGE / "datapack.unity3d_data/resources.assets").read_bytes()
OBJECTS = [tuple(map(int, v)) for v in re.findall(
    r"Object #(\d+) \(ClassID: (\d+)\) at byte (\d+)",
    (PACKAGE / "resources.assets.txt").read_text())]

def uint(buf, pos):
    return struct.unpack_from("<I", buf, pos)[0]

def string(buf, pos):
    size = uint(buf, pos)
    assert size < 100000 and pos + 4 + size <= len(buf)
    return buf[pos + 4:pos + 4 + size].decode("utf-8"), (pos + 4 + size + 3) & ~3

levels = []
names = []
dest = ROOT / "ReferenceLevelsRaw"
dest.mkdir(exist_ok=True)
for obj, cls, offset in OBJECTS:
    if cls not in (1, 28, 49, 213):
        continue
    try:
        name_offset = offset
        if cls == 1:
            count = uint(DATA, offset)
            assert count < 1000
            name_offset = offset + 4 + count * 12 + 4
        name, end = string(DATA, name_offset)
    except (AssertionError, UnicodeDecodeError, struct.error):
        continue
    names.append(dict(object=obj, class_id=cls, byte_offset=offset, name=name))
    if cls == 49 and name.isdigit() and 1 <= int(name) <= 50:
        size = uint(DATA, end)
        payload = DATA[end + 4:end + 4 + size]
        assert len(payload) == size
        target = dest / f"level{int(name):02d}.bytes"
        target.write_bytes(payload)
        levels.append(dict(level=int(name), text_asset_name=name, object=obj,
                           byte_offset=offset, bytes=size,
                           sha256=hashlib.sha256(payload).hexdigest(),
                           source="assets/bin/Data/datapack.unity3d -> resources.assets"))

assert sorted(row["level"] for row in levels) == list(range(1, 51))
levels.sort(key=lambda x: x["level"])
(ROOT / "reference-levels01-50-inventory.json").write_text(json.dumps(levels, indent=2))
with (ROOT / "reference-asset-names.csv").open("w", newline="", encoding="utf-8") as f:
    writer = csv.DictWriter(f, fieldnames=["object", "class_id", "byte_offset", "name"])
    writer.writeheader()
    writer.writerows(names)

print(f"Extracted {len(levels)} exact raw level payloads; total {sum(r['bytes'] for r in levels):,} bytes.")
for cls in (28, 213, 1):
    subset = [v for v in names if v["class_id"] == cls and re.search(
        r"(?i)(king|palace|castle|matchitem|reditem|blueitem|yellowitem|greenitem|piece|leveltop|levelbottom|goal|move|rocket|propeller|lightball|tnt)", v["name"])]
    print(f"Class {cls}, {len(subset)} relevant names:")
    for row in subset[:70]:
        print(row)
