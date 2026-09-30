"""Close hairline seams between rig parts: every pixel that is opaque in the source cutout but not
covered by the setup-pose parts is given to the nearest visible part (grown one pixel at a time,
up to 8 px). Colour comes from the source rig pose, so the setup pose is unchanged; the result is
that part boundaries meet without gaps. Updates RigReady/parts/*.png and parts.json in place."""
import json, os, sys
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
# ---- pocket pass: tiny partly transparent pockets that the parts now enclose (e.g. beside a crown finial) become opaque,
# owned by the topmost part touching them, with source colour.
sys.path.insert(0, HERE)
from render_sequence import interior_holes
cov = np.zeros((H, W))
for n in SETUP:
    a = full[n][..., 3] / 255; cov = a + cov * (1 - a)
pockets = [c for c in interior_holes((cov * 255).astype(np.uint8), 230) if c[0] <= 400]
pocket_px = {}
for cnt, x0, y0, x1, y1 in pockets:
    box = (slice(y0, y1), slice(x0, x1)); pm = (cov[box] < 0.9)
    for n in reversed(SETUP):                                   # topmost part that touches the pocket owns it
        core = full[n][..., 3] > 127
        if (np.asarray(Image.fromarray((core[max(0, y0 - 3):y1 + 3, max(0, x0 - 3):x1 + 3] * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(7))) > 127).any():
            c = full[n]; sub = c[box]; sub[pm, :3] = src[box][pm]; sub[pm, 3] = 255
            changed.add(n); pocket_px[n] = pocket_px.get(n, 0) + int(pm.sum()); break
print("pocket pass:", len(pockets), "pockets", pocket_px)

# ---- overlap pass: every lower part extends OVERLAP px under the parts drawn above it, so abutting soft edges never
# composite to partial coverage (the conflation seam separately drawn layers show at game scale). The band is filled by
# bleeding the part's OWN edge colours outward (not the upper part's look), so a moving flap such as the coat skirt never
# leaves a detached copy of its edge behind. Free-moving forearms and hands only get their arm-chain parts beneath them.
OVERLAP = 4
# a turning joint opens a gap at the limb edge of about r*sin(angle) beyond the shared joint disc (r ~ 110 px at the
# elbow), so the part underneath a joint reaches further under its joint partner only: 12 px covers the forearm
# turning ~6 degrees; 8 px for the knee. Everywhere else the overlap stays at 4 px.
JOINT_OVERLAP = {"upperarm_R": ("forearm_R", 12), "upperarm_L": ("forearm_L", 12), "shin_R": ("thigh_R", 8), "shin_L": ("thigh_L", 8)}
FREE = {"forearm_R": {"upperarm_R", "hand_R_open"}, "forearm_L": {"upperarm_L", "hand_L_open"},
        "hand_R_open": set(), "hand_L_open": set()}
def dilate(m, r):
    return np.asarray(Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(2 * r + 1))) > 127
def bleed(rgb, filled, target, steps):
    """Grow colour from `filled` pixels into `target` one pixel ring at a time (mean of filled 4-neighbours)."""
    rgb = rgb.copy(); filled = filled.copy()
    for _ in range(steps):
        acc = np.zeros(rgb.shape, np.float32); cnt = np.zeros(filled.shape, np.float32)
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            sf = np.roll(filled, (dy, dx), (0, 1)); sc = np.roll(rgb, (dy, dx), (0, 1))
            acc += sc * sf[..., None]; cnt += sf
        ring = target & ~filled & (cnt > 0)
        rgb[ring] = acc[ring] / cnt[ring][:, None]; filled |= ring
    return rgb, filled
overlap_px = {}
for i, n in enumerate(SETUP):
    c = full[n]
    upper = np.zeros((H, W), bool)
    for q in SETUP[i + 1:]:
        if q in FREE and n not in FREE[q]: continue
        upper |= full[q][..., 3] > 2
    core = c[..., 3] > 250
    ext = dilate(c[..., 3] > 127, OVERLAP) & upper & (A > 0.5) & ~core
    ov = OVERLAP
    if n in JOINT_OVERLAP:
        partner, ov = JOINT_OVERLAP[n]
        ext |= dilate(c[..., 3] > 127, ov) & (full[partner][..., 3] > 2) & (A > 0.5) & ~core
    if ext.any():
        ys, xs = np.nonzero(ext); y0, y1 = max(0, ys.min() - ov - 2), ys.max() + ov + 3; x0, x1 = max(0, xs.min() - ov - 2), xs.max() + ov + 3
        rgb, got = bleed(c[y0:y1, x0:x1, :3], core[y0:y1, x0:x1], ext[y0:y1, x0:x1], ov + 2)
        e = ext[y0:y1, x0:x1] & got
        sub = c[y0:y1, x0:x1]; sub[e, :3] = rgb[e]; sub[e, 3] = 255
        changed.add(n); overlap_px[n] = int(e.sum())
print("overlap pass px:", overlap_px)
for n in changed:
    c = full[n]; ys, xs = np.nonzero(c[..., 3] > 1)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    Image.fromarray(c[y0:y1, x0:x1].round().clip(0, 255).astype(np.uint8), "RGBA").save(os.path.join(P, n + ".png"), optimize=True)
    meta["parts"][n].update(x=int(x0), y=int(y0), w=int(x1 - x0), h=int(y1 - y0))
    meta["parts"][n]["seams_closed_px"] = int((owner == SETUP.index(n)).sum())
    if n in overlap_px: meta["parts"][n]["overlap_px"] = overlap_px[n]
json.dump(meta, open(os.path.join(P, "parts.json"), "w"), indent=1)
print("assigned:", {n: int((owner == SETUP.index(n)).sum()) for n in changed}, "unassigned:", int(left.sum()))
