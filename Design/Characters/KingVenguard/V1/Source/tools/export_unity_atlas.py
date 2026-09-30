"""Export the single-page runtime atlas used by the Unity integration: RigReady/spine-unity/.

The master export in RigReady/spine keeps every part at 1:1 on two 2048 pages. spine-unity 3.8 draws a multi-page atlas
through one child RawImage per submesh, and when such a child is (re)enabled uGUI's rebuild in the same frame replaces its
submesh with a white quad (visible on the first frame and whenever an attachment swap changes the submesh count; fixed only
in spine-unity 4.0). A single page keeps SkeletonGraphic on its single-renderer path: no children, one draw call.

The skeleton JSON is copied byte-identical: attachment sizes, pivots and draw order are in Spine units and do not change.
Only the texel density drops to the largest scale that fits one 2048 page (MaxRects packing, 2 px padding); the runtime
stretches each scaled region over the attachment's unchanged size, exactly like a Spine Editor export with a pack scale.
"""
import json, os, shutil
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__)); V1 = os.path.abspath(os.path.join(HERE, "..", ".."))
SRC = os.path.join(V1, "RigReady", "spine"); PARTS = os.path.join(V1, "RigReady", "parts"); OUT = os.path.join(V1, "RigReady", "spine-unity")
PAGE, PAD = 2048, 2


def maxrects(sizes):
    """Best-short-side-fit MaxRects. sizes: {name: (w, h)} -> {name: (x, y)} or None if they do not all fit."""
    free = [(0, 0, PAGE, PAGE)]; placed = {}
    for name, (w, h) in sorted(sizes.items(), key=lambda kv: (-max(kv[1]), -min(kv[1]), kv[0])):
        W_, H_ = w + PAD, h + PAD; best = None
        for fx, fy, fw, fh in free:
            if W_ <= fw and H_ <= fh:
                score = (min(fw - W_, fh - H_), max(fw - W_, fh - H_))
                if best is None or score < best[0]: best = (score, fx, fy)
        if best is None: return None
        _, x, y = best; placed[name] = (x, y); used = (x, y, W_, H_); nxt = []
        for fx, fy, fw, fh in free:
            if x >= fx + fw or x + W_ <= fx or y >= fy + fh or y + H_ <= fy: nxt.append((fx, fy, fw, fh)); continue
            if x > fx: nxt.append((fx, fy, x - fx, fh))
            if x + W_ < fx + fw: nxt.append((x + W_, fy, fx + fw - x - W_, fh))
            if y > fy: nxt.append((fx, fy, fw, y - fy))
            if y + H_ < fy + fh: nxt.append((fx, y + H_, fw, fy + fh - y - H_))
        free = [a for i, a in enumerate(nxt) if not any(j != i and a[0] >= b[0] and a[1] >= b[1] and a[0] + a[2] <= b[0] + b[2]
                                                        and a[1] + a[3] <= b[1] + b[3] and (a != b or j < i) for j, b in enumerate(nxt))]
    return placed


def scaled(im, s):
    w, h = max(1, round(im.width * s)), max(1, round(im.height * s))
    return im.convert("RGBa").resize((w, h), Image.LANCZOS).convert("RGBA")   # premultiplied filtering: no dark fringes


skel = json.load(open(os.path.join(SRC, "King_Venguard.json")))
names = sorted({a for sl in skel["skins"][0]["attachments"].values() for a, v in sl.items() if v.get("type") != "clipping"})
images = {n: Image.open(os.path.join(PARTS, n + ".png")).convert("RGBA") for n in names}
for s in (0.9, 0.875, 0.85, 0.825, 0.8, 0.75, 0.7):
    parts = {n: scaled(im, s) for n, im in images.items()}
    place = maxrects({n: p.size for n, p in parts.items()})
    if place: break
else:
    raise SystemExit("parts do not fit one 2048 page")

if os.path.isdir(OUT): shutil.rmtree(OUT)
os.makedirs(OUT)
page = Image.new("RGBA", (PAGE, PAGE), (0, 0, 0, 0))
lines = ["", "King_Venguard.png", "size: %d,%d" % (PAGE, PAGE), "format: RGBA8888", "filter: Linear,Linear", "repeat: none"]
for n in sorted(place):
    x, y = place[n]; p = parts[n]; page.paste(p, (x, y))
    lines += [n, "  rotate: false", "  xy: %d, %d" % (x, y), "  size: %d, %d" % p.size, "  orig: %d, %d" % p.size, "  offset: 0, 0", "  index: -1"]
page.save(os.path.join(OUT, "King_Venguard.png"), optimize=True)
open(os.path.join(OUT, "King_Venguard.atlas"), "w", newline="\n").write("\n".join(lines) + "\n")
shutil.copy(os.path.join(SRC, "King_Venguard.json"), os.path.join(OUT, "King_Venguard.json"))
fill = sum(p.size[0] * p.size[1] for p in parts.values()) / float(PAGE * PAGE)
info = {"texture_scale": s, "page": PAGE, "regions": len(place), "page_fill": round(fill, 3), "json": "byte-identical copy of RigReady/spine/King_Venguard.json"}
json.dump(info, open(os.path.join(OUT, "export.json"), "w"), indent=1)
print("single-page runtime atlas: texture scale %.3f, %d regions, page fill %.0f%%" % (s, len(place), 100 * fill))
