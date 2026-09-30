"""Assemble Design/ sheets and Previews/ stills for King Venguard V1 (consistency correction pass, 24 Sep 2026).

Copied unchanged from Source/magnific (generated design art): 01 master hero, 02 turnaround, 03 neutral rig pose,
04 expression sheet, 06 hand replacement sheet.
Corrected: 05 action poses. Generated pose 1 was a LEFT-hand thumbs-up wearing two rings; it is replaced by a rig render
of the RIGHT-hand thumbs-up (signet ring on the left hand). The generated original is unchanged in
Source/magnific/06-action-poses.png and in _superseded/pass1_2026-09-24/Design/.
New: 09 consistency contact sheet, 10 hand and ring arrangement.
08 readability, the rig columns of 09/10 and every Previews still are rendered from RigReady/spine with the custom preview
player (render_rig.py), not the official Spine runtime.
"""
import json, os, shutil, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
from render_rig import Renderer
import spine_eval as se

V1 = os.path.abspath(os.path.join(HERE, "..", ".."))
MAG = os.path.join(V1, "Source", "magnific"); CUT = os.path.join(MAG, "cutouts")
DES = os.path.join(V1, "Design"); PRE = os.path.join(V1, "Previews"); SPLIT = os.path.join(DES, "cutouts")
for d in (DES, PRE, SPLIT): os.makedirs(d, exist_ok=True)
FONT = lambda s: ImageFont.truetype("C:/Windows/Fonts/arial.ttf", s)
BOLD = lambda s: ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", s)
PLAYER = "Rig images: rendered from RigReady/spine with the custom preview player - official Spine compatibility untested."
RULE = "Signet ring: the character's LEFT hand (viewer's right in front views). Thumbs-up: the character's RIGHT hand, which never wears a ring."
POSE1_T = 2.30                        # happy_thumbs_up: thumbs-up held (swapped in at 1.90 s), laugh mouth, eyes wide
INK, SUB, PAPER = (30, 32, 44), (92, 96, 110), (246, 247, 250)

meta = json.load(open(os.path.join(V1, "RigReady", "parts", "parts.json"))); W, H = meta["size"]
RF = Renderer((W, H), 1.0, ground=meta["ground_y"])
RF.ox = meta["centre_x"]; RF.C = [[1.0, 0, RF.ox], [0, -1.0, RF.oy], [0, 0, 1]]     # 1:1 with the source rig pose
CLEAR = Image.new("RGBA", (W, H), (0, 0, 0, 0))
def rig_full(anim, t): return RF.frame(anim, t, bg=CLEAR)

def copy(src, dst): shutil.copy(src, os.path.join(DES, dst))

def runs_x(alpha, gap=12, min_w=40):
    cols = (alpha > 20).any(0); xs = np.nonzero(cols)[0]; out = []; s = p = xs[0]
    for x in xs[1:]:
        if x > p + gap:
            if p - s >= min_w: out.append((s, p))
            s = x
        p = x
    if p - s >= min_w: out.append((s, p))
    return out

def split_to(runs, alpha, want):
    cov = (alpha > 20).sum(0)
    while len(runs) < want:   # figures that touch (a raised fist): split the widest run at its lowest-coverage column
        i = max(range(len(runs)), key=lambda k: runs[k][1] - runs[k][0]); x0, x1 = runs[i]; q = (x1 - x0) // 4
        cut = x0 + q + int(np.argmin(cov[x0 + q:x1 - q])); runs[i:i + 1] = [(x0, cut), (cut + 1, x1)]
    return runs

def crop_alpha(im, x0, x1, pad=16):
    a = np.asarray(im)[..., 3][:, x0:x1 + 1]; ys = np.nonzero((a > 20).any(1))[0]
    return im.crop((max(0, x0 - pad), max(0, ys.min() - pad), min(im.width, x1 + pad + 1), min(im.height, ys.max() + pad + 1)))

def alpha_box(im, thr=8, pad=0):
    a = np.asarray(im)[..., 3]; ys, xs = np.nonzero(a > thr)
    return (max(0, xs.min() - pad), max(0, ys.min() - pad), min(im.width, xs.max() + 1 + pad), min(im.height, ys.max() + 1 + pad))

def fit_h(im, h):
    """Resize an RGBA image to height h with premultiplied filtering (no dark fringes)."""
    w = max(1, int(round(im.width * h / im.height)))
    return im.convert("RGBa").resize((w, h), Image.LANCZOS).convert("RGBA")

def fit_box(im, w, h):
    s = min(w / im.width, h / im.height)
    return im.convert("RGBa").resize((max(1, int(im.width * s)), max(1, int(im.height * s))), Image.LANCZOS).convert("RGBA")

def dilate(m, r):
    return np.asarray(Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(2 * r + 1))) > 127

def harmonic_fill(img, hole, iters=(260, 120)):
    """Fill `hole` smoothly from the surrounding pixels (coarse-to-fine relaxation). Used for the studio background only."""
    ys, xs = np.nonzero(hole)
    y0, y1 = max(0, ys.min() - 8), min(img.shape[0], ys.max() + 9); x0, x1 = max(0, xs.min() - 8), min(img.shape[1], xs.max() + 9)
    sub = img[y0:y1, x0:x1].copy(); f = hole[y0:y1, x0:x1]; sub[f] = sub[~f].mean(0)
    def relax(s, fm, n):
        for _ in range(n):
            p = np.pad(s, ((1, 1), (1, 1), (0, 0)), mode="edge")
            s[fm] = ((p[:-2, 1:-1] + p[2:, 1:-1] + p[1:-1, :-2] + p[1:-1, 2:]) * 0.25)[fm]
        return s
    for k in (16, 4):
        h, w = sub.shape[:2]
        small = np.asarray(Image.fromarray(sub.clip(0, 255).astype(np.uint8)).resize((max(2, w // k), max(2, h // k)), Image.BOX), np.float32)
        fs = np.asarray(Image.fromarray((f * 255).astype(np.uint8)).resize(small.shape[1::-1], Image.BOX)) > 0
        small = relax(small, fs, iters[0])
        up = np.asarray(Image.fromarray(small.clip(0, 255).astype(np.uint8)).resize((w, h), Image.BILINEAR), np.float32)
        sub[f] = up[f]
    sub = relax(sub, f, iters[1])
    out = img.copy(); out[y0:y1, x0:x1] = sub
    return out

def main_figure(im):
    """Keep only the figure connected to the body's centre (drops slivers of a neighbouring figure in a split sheet)."""
    arr = np.asarray(im).copy(); a = arr[..., 3]
    ys, xs = np.nonzero(a > 200); cy = int(np.median(ys)); row = np.nonzero(a[cy] > 200)[0]
    cx = int(row[np.argmin(np.abs(row - np.median(xs)))])
    m = Image.fromarray(((a > 20) * 255).astype(np.uint8)).copy(); ImageDraw.floodfill(m, (cx, cy), 128)   # copy: fromarray images are read-only views
    arr[..., 3] = np.where(dilate(np.asarray(m) == 128, 2), a, 0)
    out = Image.fromarray(arr, "RGBA")
    return out.crop(alpha_box(out, pad=6))

def bone_px(anim, t, bone):
    world, _ = RF.sk.pose(anim, t); wx, wy = se.apply(world[bone], 0, 0)
    return RF.ox + wx, RF.oy - wy

def checker(w, h, c=16):
    t = Image.new("RGB", (w, h), (238, 239, 243)); d = ImageDraw.Draw(t)
    for y in range(0, h, c):
        for x in range(0, w, c):
            if (x // c + y // c) % 2: d.rectangle([x, y, x + c - 1, y + c - 1], fill=(226, 228, 234))
    return t

def tile(im, w, h, bg=(255, 255, 255)):
    """Centre an RGBA image in a w x h tile (fit inside)."""
    t = Image.new("RGBA", (w, h), bg + (255,)); f = fit_box(im, w, h); t.alpha_composite(f, ((w - f.width) // 2, (h - f.height) // 2))
    return t.convert("RGB")

def label(d, xy, text, size=18, fill=INK, bold=False):
    d.text(xy, text, fill=fill, font=(BOLD if bold else FONT)(size))

# ------------------------------------------------------------------ 1. generated design art, copied unchanged
copy(os.path.join(MAG, "02-master-hero-3q-stylized.png"), "01_KingVenguard_Master_Hero_ThreeQuarter.png")
copy(os.path.join(CUT, "02-master-hero-3q-alpha.png"), "01_KingVenguard_Master_Hero_ThreeQuarter_alpha.png")
copy(os.path.join(MAG, "03-turnaround-front-side-back.png"), "02_KingVenguard_Turnaround_Front_Side_Back.png")
copy(os.path.join(CUT, "03-turnaround-alpha.png"), "02_KingVenguard_Turnaround_Front_Side_Back_alpha.png")
copy(os.path.join(MAG, "04-neutral-rig-pose-front.png"), "03_KingVenguard_Neutral_Rig_Pose.png")
copy(os.path.join(CUT, "04-neutral-rig-pose-front-alpha.png"), "03_KingVenguard_Neutral_Rig_Pose_alpha.png")
copy(os.path.join(MAG, "05-expression-sheet.png"), "04_KingVenguard_Expression_Sheet.png")
copy(os.path.join(CUT, "05-expression-sheet-alpha.png"), "04_KingVenguard_Expression_Sheet_alpha.png")
copy(os.path.join(MAG, "07-hand-replacement-sheet.png"), "06_KingVenguard_Hand_Replacements.png")
copy(os.path.join(CUT, "07-hand-replacement-sheet-alpha.png"), "06_KingVenguard_Hand_Replacements_alpha.png")
shutil.copy(os.path.join(PRE, "checks", "parts-exploded-sheet.png"), os.path.join(DES, "07_KingVenguard_Rig_Parts_Exploded.png"))

tt = Image.open(os.path.join(CUT, "03-turnaround-alpha.png")).convert("RGBA")
turn = {}
for (x0, x1), n in zip(runs_x(np.asarray(tt)[..., 3]), ("front", "side", "back")):
    turn[n] = crop_alpha(tt, x0, x1); turn[n].save(os.path.join(SPLIT, "turnaround_%s.png" % n))
ex = Image.open(os.path.join(CUT, "05-expression-sheet-alpha.png")).convert("RGBA"); cw, ch = ex.width // 4, ex.height // 2
EXPR = ("1_neutral_idle", "2_relaxed_eyes_closed", "3_glance_side", "4_warm_smile", "5_joyful_laugh", "6_excited", "7_worried", "8_sad")
expr = {}
for i, n in enumerate(EXPR):
    expr[n] = ex.crop(((i % 4) * cw, (i // 4) * ch, (i % 4 + 1) * cw, (i // 4 + 1) * ch)); expr[n].save(os.path.join(SPLIT, "expression_%s.png" % n))

# ------------------------------------------------------------------ 2. corrected action poses: pose 1 -> rig render, right-hand thumbs-up
ap = np.asarray(Image.open(os.path.join(MAG, "06-action-poses.png")).convert("RGB")).astype(np.float32)
apa = np.asarray(Image.open(os.path.join(CUT, "06-action-poses-alpha.png")).convert("RGBA")).copy()
runs = split_to(runs_x(apa[..., 3], gap=20), apa[..., 3], 5)
f0, f1 = runs[0]
fig = apa[..., 3] > 8; fig[:, :max(0, f0 - 1)] = False; fig[:, f1 + 2:] = False
fys, fxs = np.nonzero(fig); top, bottom = int(fys.min()), int(fys.max())
feet_cx = float(np.nonzero(fig[bottom - 60:bottom + 1])[1].mean())
lum = ap.mean(-1)
gap_cols = np.nonzero(~(apa[..., 3] > 8).any(0))[0]; gap_cols = gap_cols[(gap_cols > 200) & (gap_cols < ap.shape[1] - 200)]
line_rows = np.nonzero(np.median(lum[:, gap_cols], 1) < 150)[0]               # the black ground line, measured between figures
yl0, yl1 = int(line_rows.min()) - 1, int(line_rows.max()) + 1
lx = np.nonzero(lum[int(np.median(line_rows))] < 90)[0]; lx0, lx1 = int(lx.min()), int(lx.max())
hole = dilate(fig, 8)
bgfill = harmonic_fill(ap, hole)
clean_cols = np.nonzero(~hole.any(0))[0]; clean_cols = clean_cols[(clean_cols > lx0 + 20) & (clean_cols < lx1 - 20)]
for y in range(yl0, yl1 + 1):                                                   # the ground line runs on through the removed figure
    prof = np.median(ap[y, clean_cols], 0); cols = np.nonzero(hole[y])[0]; cols = cols[(cols >= lx0) & (cols <= lx1)]
    bgfill[y, cols] = prof
pose1 = rig_full("happy_thumbs_up", POSE1_T); pose1 = pose1.crop(alpha_box(pose1))
pose1 = fit_h(pose1, bottom - top + 1)
na = np.asarray(pose1)[..., 3]; ncx = float(np.nonzero(na[-60:] > 8)[1].mean())
px, py = int(round(feet_cx - ncx)), bottom + 1 - pose1.height
# soft contact shadow like the generated poses' (their shadows were part of the cutout and went with the old figure)
sh = Image.new("L", (ap.shape[1], ap.shape[0]), 0); fw = pose1.width
ImageDraw.Draw(sh).ellipse([feet_cx - 0.62 * fw, bottom - 26, feet_cx + 0.30 * fw, bottom + 18], fill=255)
sh = np.asarray(sh.filter(ImageFilter.GaussianBlur(22)), np.float32)[..., None] / 255
bgfill = bgfill * (1 - 0.30 * sh)
sheet = Image.fromarray(bgfill.round().clip(0, 255).astype(np.uint8)).convert("RGBA"); sheet.alpha_composite(pose1, (px, py))
d = ImageDraw.Draw(sheet)
cap_y = yl1 + 26
label(d, (max(20, f0 - 10), cap_y), "1  rig render, front view (happy_thumbs_up %.2f s)" % POSE1_T, 26, SUB)
label(d, (max(20, f0 - 10), cap_y + 34), "thumbs-up: RIGHT hand  |  signet ring: LEFT hand", 26, SUB)
label(d, (runs[1][0], cap_y), "2-5  generated design art (unchanged)", 26, SUB)
sheet.convert("RGB").save(os.path.join(DES, "05_KingVenguard_Action_Poses.png"))
apa[dilate(fig, 2)] = 0
sheet_a = Image.fromarray(apa, "RGBA"); sheet_a.alpha_composite(pose1, (px, py))
sheet_a.save(os.path.join(DES, "05_KingVenguard_Action_Poses_alpha.png"))
pose1.save(os.path.join(SPLIT, "pose_1_happy_thumbs_up.png"))
poses = {"1_happy_thumbs_up": pose1}
ap_orig = Image.open(os.path.join(CUT, "06-action-poses-alpha.png")).convert("RGBA")
for (x0, x1), n in zip(runs[1:], ("2_joyful_celebration", "3_excited", "4_worried", "5_sad")):
    poses[n] = main_figure(crop_alpha(ap_orig, x0, x1, pad=40))
    poses[n].save(os.path.join(SPLIT, "pose_%s.png" % n))
print("action poses: pose 1 replaced at x=%d..%d (feet centre %.0f), ground line rows %d-%d" % (px, px + pose1.width, feet_cx, yl0, yl1))

# ------------------------------------------------------------------ 3. rig renders used by the sheets
master = Image.open(os.path.join(CUT, "02-master-hero-3q-alpha.png")).convert("RGBA"); master = master.crop(alpha_box(master, pad=10))
rigpose = Image.open(os.path.join(CUT, "04-neutral-rig-pose-front-alpha.png")).convert("RGBA")
setup = rig_full(None, 0)
RIG_EXPR = [("setup pose", None, 0.0), ("idle 1.60 s (blink)", "idle", 1.60), ("idle 2.60 s (glance)", "idle", 2.60),
            ("happy 1.49 s (smile)", "happy_thumbs_up", 1.49), ("happy 0.55 s (laugh)", "happy_thumbs_up", 0.55),
            ("range_test 5 (wide-eyed)", "range_test", 5.0), ("range_test 4 (worried)", "range_test", 4.0), ("range_test 3 (sad)", "range_test", 3.0)]
HEAD = (538, 150, 1158, 842)
rig_heads = [(n, rig_full(a, t).crop(HEAD)) for n, a, t in RIG_EXPR]

# ------------------------------------------------------------------ 4. 09 consistency contact sheet
SW = 3240; pad = 30
row1 = [("Master hero, 3/4 (generated)", master), ("Turnaround front (generated)", turn["front"]), ("Turnaround side (generated)", turn["side"]),
        ("Turnaround back (generated)", turn["back"]), ("Neutral rig pose (generated, rig source)", rigpose.crop(alpha_box(rigpose, pad=10))),
        ("Rig setup pose (rendered)", setup.crop(alpha_box(setup, pad=10)))]
row1 = [(n, im.crop(alpha_box(im, pad=6))) for n, im in row1]
ratios = [im.width / im.height for _, im in row1]
r1h = min(900, int((SW - pad * (len(row1) + 1)) / sum(ratios)))             # one height for every view, widths in proportion
ew = (SW - pad * 9) // 8; eh = int(ew * (HEAD[3] - HEAD[1]) / (HEAD[2] - HEAD[0]))
p5h = 620
SH = 150 + r1h + 60 + 2 * (eh + 70) + 90 + p5h + 70 + 60
cs = Image.new("RGB", (SW, SH), PAPER); d = ImageDraw.Draw(cs)
label(d, (pad, 22), "King Venguard V1 - consistency contact sheet (correction pass, 24 Sep 2026)", 38, INK, True)
label(d, (pad, 76), RULE, 24, (150, 40, 40), True)
label(d, (pad, 110), "Same face, apparent age, heavyset proportions, crown and costume in every view. No new generation in this pass; generated art is unchanged except action pose 1.", 20, SUB)
y = 150; x = pad
for (n, im), rt in zip(row1, ratios):
    w = int(r1h * rt); cs.paste(tile(im, w, r1h, PAPER), (x, y)); label(d, (x, y + r1h + 10), n, 20, SUB); x += w + pad
y += r1h + 60
label(d, (pad, y - 8), "Expressions - top: generated expression sheet (design target); bottom: the same expressions from the rig", 24, INK, True); y += 26
for i, n in enumerate(EXPR):
    x = pad + i * (ew + pad); cs.paste(tile(expr[n], ew, eh, PAPER), (x, y)); label(d, (x, y + eh + 6), n.split("_", 1)[1].replace("_", " "), 18, SUB)
y += eh + 40
for i, (n, im) in enumerate(rig_heads):
    x = pad + i * (ew + pad); cs.paste(tile(im, ew, eh, PAPER), (x, y)); label(d, (x, y + eh + 6), "rig: " + n, 18, SUB)
y += eh + 70
label(d, (pad, y - 8), "Action poses (Design/05, corrected): pose 1 is a rig render with the right-hand thumbs-up; poses 2-5 generated", 24, INK, True); y += 26
pw = (SW - pad * 6) // 5
for i, (n, im) in enumerate(poses.items()):
    x = pad + i * (pw + pad); cs.paste(tile(im, pw, p5h, PAPER), (x, y)); label(d, (x, y + p5h + 6), n.split("_", 1)[1].replace("_", " ") + (" (rig render)" if i == 0 else ""), 18, SUB)
label(d, (pad, SH - 40), PLAYER + "  Rig expressions use existing parts plus warps of the existing brows/eyes (no generation).", 18, SUB)
cs.save(os.path.join(DES, "09_KingVenguard_Consistency_Contact_Sheet.png"))

# ------------------------------------------------------------------ 5. 10 hand and ring arrangement
mh = Image.open(os.path.join(CUT, "02-master-hero-3q-alpha.png")).convert("RGBA")
ta = Image.open(os.path.join(CUT, "03-turnaround-alpha.png")).convert("RGBA")
def box_around(part, pad=40):
    m = meta["parts"][part]; return (m["x"] - pad, m["y"] - pad, m["x"] + m["w"] + pad, m["y"] + m["h"] + pad)
thumbs = rig_full("happy_thumbs_up", POSE1_T)
hx, hy = bone_px("happy_thumbs_up", POSE1_T, "hand_R")
hand_rows = [
    ("Generated design art - the signet ring is on the character's LEFT hand in every view", [
        ("Master 3/4: LEFT hand, ring", mh.crop((1440, 1360, 1720, 1720))), ("Master 3/4: right hand, no ring", mh.crop((110, 1320, 360, 1640))),
        ("Turnaround front: LEFT, ring", ta.crop((890, 840, 1060, 1080))), ("Turnaround front: right, no ring", ta.crop((20, 840, 190, 1080))),
        ("Turnaround side (left side): ring", ta.crop((1260, 860, 1450, 1080))), ("Turnaround back: LEFT, ring", ta.crop((1690, 860, 1850, 1080))),
        ("Rig pose source: LEFT, ring", rigpose.crop(box_around("hand_L_open"))), ("Rig pose source: right", rigpose.crop(box_around("hand_R_open")))]),
    ("Rig hands (RigReady/parts, raw parts: the ragged top of an open hand sits hidden inside the cuff) - ring only on the left-hand parts", [
        (n, Image.open(os.path.join(V1, "RigReady", "parts", n + ".png")).convert("RGBA")) for n in
        ("hand_L_open", "hand_L_fist", "hand_R_open", "hand_R_fist", "hand_R_thumbsup", "hand_R_palm")]),
    ("Action pose 1 - generated original superseded (left-hand thumbs-up with two rings); corrected pose is the rig's right-hand thumbs-up", [
        ("SUPERSEDED: generated pose 1 (kept in Source/)", ap_orig.crop((f0 - 10, top - 10, f1 + 10, bottom + 10))),
        ("SUPERSEDED: its thumbs-up hand, two rings", ap_orig.crop((430, 400, 640, 700))),
        ("CORRECTED: rig render, happy_thumbs_up %.2f s" % POSE1_T, thumbs.crop(alpha_box(thumbs, pad=10))),
        ("CORRECTED: RIGHT-hand thumbs-up, no ring", thumbs.crop((int(hx) - 200, int(hy) - 230, int(hx) + 200, int(hy) + 170))),
        ("CORRECTED: LEFT hand keeps the ring", thumbs.crop(box_around("hand_L_open", 60)))]),
]
HW = 3000; th = 400; hpad = 24
HH = 120 + sum(th + 110 for _ in hand_rows) + 50
hs = Image.new("RGB", (HW, HH), PAPER); d = ImageDraw.Draw(hs)
label(d, (hpad, 20), "King Venguard V1 - hand and ring arrangement", 38, INK, True)
label(d, (hpad, 72), RULE, 24, (150, 40, 40), True)
y = 120
for title, items in hand_rows:
    label(d, (hpad, y), title, 22, INK, True); y += 34
    tw = min(360, (HW - hpad * (len(items) + 1)) // len(items))
    for i, (n, im) in enumerate(items):
        x = hpad + i * (tw + hpad); hs.paste(tile(im, tw, th, (255, 255, 255)), (x, y)); label(d, (x, y + th + 6), n, 16, SUB)
    y += th + 76
label(d, (hpad, HH - 36), PLAYER, 18, SUB)
hs.save(os.path.join(DES, "10_KingVenguard_Hand_And_Ring_Arrangement.png"))

# ------------------------------------------------------------------ 6. 08 game-scale readability (rig renders)
R = Renderer((1080, 1350), 0.5, bg="studio")
shots = [("neutral", None, 0.0), ("laugh", "happy_thumbs_up", 0.55), ("thumbs-up (right hand)", "happy_thumbs_up", POSE1_T),
         ("wide-eyed", "range_test", 5.0), ("worried", "range_test", 4.0), ("sad, eyes closed", "range_test", 3.0)]
frames = [(n, R.frame(a, t).convert("RGB")) for n, a, t in shots]
body_box, head_box = (130, 140, 950, 1325), (330, 150, 750, 520)
rows = [("Full body %d px tall" % h, body_box, h) for h in (360, 220, 140)] + [("Head %d px" % h, head_box, h) for h in (160, 110, 72)]
pad = 20; colw = 330
sheet = Image.new("RGB", (pad + len(frames) * (colw + pad), 100 + sum(h + 44 for _, _, h in rows)), PAPER)
d = ImageDraw.Draw(sheet); label(d, (pad, 14), "King Venguard V1 - readability at game scale (nearest in-game sizes)", 22, INK, True)
y = 70
for lab, box, h in rows:
    label(d, (pad, y), lab, 16, SUB); y += 22
    for i, (n, fr) in enumerate(frames):
        c = fr.crop(box); w = int(c.width * h / c.height); c = c.resize((w, h), Image.LANCZOS)
        sheet.paste(c, (pad + i * (colw + pad) + (colw - w) // 2, y))
    y += h + 22
for i, (n, _) in enumerate(frames): label(d, (pad + i * (colw + pad) + 4, 46), n, 16, (40, 40, 120))
label(d, (pad, sheet.height - 28), PLAYER, 15, SUB)
sheet.save(os.path.join(DES, "08_KingVenguard_GameScale_Readability.png"))

# ------------------------------------------------------------------ 7. Previews stills: keyframe strips and joint-coverage test
def strip(anim, times, name, title, box=body_box, h=420):
    tiles = [R.frame(anim, t).convert("RGB").crop(box) for t in times]
    w = int(tiles[0].width * h / tiles[0].height); out = Image.new("RGB", (len(tiles) * (w + 10) + 10, h + 96), PAPER); dd = ImageDraw.Draw(out)
    label(dd, (10, 8), title, 20)
    for i, (t, im) in enumerate(zip(times, tiles)):
        out.paste(im.resize((w, h), Image.LANCZOS), (10 + i * (w + 10), 40)); label(dd, (14 + i * (w + 10), h + 44), "%.2f s" % t, 16, SUB)
    label(dd, (10, h + 70), PLAYER, 15, SUB)
    out.save(os.path.join(PRE, name))
strip("idle", [0, 0.6, 1.0, 1.6, 2.0, 2.4, 3.0, 3.6], "KingVenguard_Idle_Keyframes.png",
      "idle (4.0 s loop, original motion design): breath, weight shift, lagged head/whiskers/cape, blink 1.55 s, glance and brow lift 2.1-3.35 s")
strip("happy_thumbs_up", [0, 0.22, 0.55, 0.92, 1.49, 1.7, 1.9, 2.12, 2.3, 2.7, 3.14, 3.3, 3.8], "KingVenguard_Happy_ThumbsUp_Keyframes.png",
      "happy_thumbs_up (3.8 s): laugh with cheek/whisker response, fist forms 1.58 s, thumbs-up at 1.90 s (the one reference-timed beat), eyes wide, return to idle")
RM = Renderer((1080, 1350), 0.5, bg="magenta")
labels = ["0 setup", "1 thumbs-up (right hand), left arm bent", "2 head +8, laugh, cheeks and whiskers up",
          "3 head -8, sad brows and mouth, eyes closed", "4 hips -16, arms out, worried brows and mouth, glance",
          "5 weight shift, skirt/torso twist, fists, wide eyes"]
tiles = [RM.frame("range_test", float(i)).convert("RGB").crop((150, 120, 930, 1330)).resize((390, 605), Image.LANCZOS) for i in range(6)]
out = Image.new("RGB", (3 * 400 + 10, 2 * 640 + 84), (30, 30, 30)); dd = ImageDraw.Draw(out)
label(dd, (10, 10), "Joint coverage test on magenta: magenta inside the silhouette would be a hole (outside is background)", 18, (255, 255, 255))
for i, (im, lab) in enumerate(zip(tiles, labels)):
    x, y = 10 + (i % 3) * 400, 50 + (i // 3) * 640; out.paste(im, (x, y)); label(dd, (x + 4, y + 608), lab, 14, (255, 255, 255))
label(dd, (10, out.height - 28), PLAYER, 14, (200, 200, 200))
out.save(os.path.join(PRE, "KingVenguard_Joint_Coverage_Test.png"))
print("done")
