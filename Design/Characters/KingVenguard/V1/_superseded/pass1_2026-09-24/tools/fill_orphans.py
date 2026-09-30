"""Close hairline seams between rig parts: every pixel that is opaque in the source cutout but not
covered by the setup-pose parts is given to the nearest visible part (grown one pixel at a time,
up to 8 px). Colour comes from the source rig pose, so the setup pose is unchanged; the result is
that part boundaries meet without gaps. Updates RigReady/parts/*.png and parts.json in place."""
import json, os
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__)); V1 = os.path.abspath(os.path.join(HERE, "..", ".."))
P = os.path.join(V1, "RigReady", "parts"); meta = json.load(open(os.path.join(P, "parts.json"))); W, H = meta["size"]
SETUP = ["pelvis", "shin_R", "shin_L", "foot_R", "foot_L", "thigh_R", "thigh_L", "skirt", "upperarm_R", "upperarm_L", "torso",
         "cape_R", "cape_L", "neck", "collar", "hand_R_open", "forearm_R", "hand_L_open", "forearm_L", "head", "iris_R", "iris_L",
         "chop_R", "chop_L", "mustache", "brow_R", "brow_L", "crown"]
NO_GROW = {"iris_R", "iris_L", "brow_R", "brow_L", "mustache", "chop_R", "chop_L", "neck"}   # facial detail layers keep their exact outline
src = np.asarray(Image.open(os.path.join(V1, "Source", "magnific", "04-neutral-rig-pose-front.png")).convert("RGB"))
A = np.asarray(Image.open(os.path.join(V1, "Source", "magnific", "cutouts", "04-neutral-rig-pose-front-alpha.png")).convert("RGBA"))[..., 3] / 255.0

full = {}
for n in SETUP:
    m = meta["parts"][n]; im = np.asarray(Image.open(os.path.join(P, n + ".png")).convert("RGBA")).astype(np.float32)
    canvas = np.zeros((H, W, 4), np.float32); canvas[m["y"]:m["y"] + m["h"], m["x"]:m["x"] + m["w"]] = im; full[n] = canvas
cov = np.zeros((H, W))
for n in SETUP:
    a = full[n][..., 3] / 255; cov = a + cov * (1 - a)
orphan = (A > 0.5) & (cov < 0.9)
print("orphan pixels before:", int(orphan.sum()))
owner = np.full((H, W), -1, np.int32)
grow = {n: (full[n][..., 3] > 127) for n in SETUP if n not in NO_GROW}
names = [n for n in reversed(SETUP) if n in grow]                     # topmost part wins a tie
for step in range(8):
    if not (orphan & (owner < 0)).any(): break
    for idx, n in enumerate(names):
        g = np.asarray(Image.fromarray((grow[n] * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3))) > 127
        claim = g & orphan & (owner < 0)
        owner[claim] = SETUP.index(n); grow[n] = g
left = orphan & (owner < 0)
changed = set()
for n in names:
    k = SETUP.index(n); sel = owner == k
    if not sel.any(): continue
    c = full[n]; c[sel, :3] = src[sel]; c[sel, 3] = np.maximum(c[sel, 3], A[sel] * 255); changed.add(n)
for n in changed:
    c = full[n]; ys, xs = np.nonzero(c[..., 3] > 1)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    Image.fromarray(c[y0:y1, x0:x1].round().clip(0, 255).astype(np.uint8), "RGBA").save(os.path.join(P, n + ".png"), optimize=True)
    meta["parts"][n].update(x=int(x0), y=int(y0), w=int(x1 - x0), h=int(y1 - y0))
    meta["parts"][n]["seams_closed_px"] = int((owner == SETUP.index(n)).sum())
json.dump(meta, open(os.path.join(P, "parts.json"), "w"), indent=1)
print("assigned:", {n: int((owner == SETUP.index(n)).sum()) for n in changed}, "unassigned:", int(left.sum()))
