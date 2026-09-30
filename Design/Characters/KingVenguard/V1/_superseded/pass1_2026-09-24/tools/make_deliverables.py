"""Assemble Design/ sheets and Previews/ stills for King Venguard V1 from Source/ and the rig."""
import json, os, shutil, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
from render_rig import Renderer

V1 = os.path.abspath(os.path.join(HERE, "..", ".."))
MAG = os.path.join(V1, "Source", "magnific"); CUT = os.path.join(MAG, "cutouts")
DES = os.path.join(V1, "Design"); PRE = os.path.join(V1, "Previews")
os.makedirs(DES, exist_ok=True); os.makedirs(PRE, exist_ok=True)
FONT = lambda s: ImageFont.truetype("C:/Windows/Fonts/arial.ttf", s)

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

def crop_alpha(im, x0, x1, pad=16):
    a = np.asarray(im)[..., 3][:, x0:x1 + 1]; ys = np.nonzero((a > 20).any(1))[0]
    return im.crop((max(0, x0 - pad), max(0, ys.min() - pad), min(im.width, x1 + pad + 1), min(im.height, ys.max() + pad + 1)))

# ---- master, rig pose, sheets (opaque source + clean transparent cutout)
copy(os.path.join(MAG, "02-master-hero-3q-stylized.png"), "01_KingVenguard_Master_Hero_ThreeQuarter.png")
copy(os.path.join(CUT, "02-master-hero-3q-alpha.png"), "01_KingVenguard_Master_Hero_ThreeQuarter_alpha.png")
copy(os.path.join(MAG, "03-turnaround-front-side-back.png"), "02_KingVenguard_Turnaround_Front_Side_Back.png")
copy(os.path.join(CUT, "03-turnaround-alpha.png"), "02_KingVenguard_Turnaround_Front_Side_Back_alpha.png")
copy(os.path.join(MAG, "04-neutral-rig-pose-front.png"), "03_KingVenguard_Neutral_Rig_Pose.png")
copy(os.path.join(CUT, "04-neutral-rig-pose-front-alpha.png"), "03_KingVenguard_Neutral_Rig_Pose_alpha.png")
copy(os.path.join(MAG, "05-expression-sheet.png"), "04_KingVenguard_Expression_Sheet.png")
copy(os.path.join(CUT, "05-expression-sheet-alpha.png"), "04_KingVenguard_Expression_Sheet_alpha.png")
copy(os.path.join(MAG, "06-action-poses.png"), "05_KingVenguard_Action_Poses.png")
copy(os.path.join(CUT, "06-action-poses-alpha.png"), "05_KingVenguard_Action_Poses_alpha.png")
copy(os.path.join(MAG, "07-hand-replacement-sheet.png"), "06_KingVenguard_Hand_Replacements.png")
copy(os.path.join(CUT, "07-hand-replacement-sheet-alpha.png"), "06_KingVenguard_Hand_Replacements_alpha.png")

split = os.path.join(DES, "cutouts"); os.makedirs(split, exist_ok=True)
tt = Image.open(os.path.join(CUT, "03-turnaround-alpha.png")).convert("RGBA")
for (x0, x1), n in zip(runs_x(np.asarray(tt)[..., 3]), ("front", "side", "back")):
    crop_alpha(tt, x0, x1).save(os.path.join(split, "turnaround_%s.png" % n))
ap = Image.open(os.path.join(CUT, "06-action-poses-alpha.png")).convert("RGBA")
def split_to(runs, alpha, want):
    cov = (alpha > 20).sum(0)
    while len(runs) < want:   # figures that touch (a raised fist): split the widest run at its lowest-coverage column
        i = max(range(len(runs)), key=lambda k: runs[k][1] - runs[k][0]); x0, x1 = runs[i]; q = (x1 - x0) // 4
        cut = x0 + q + int(np.argmin(cov[x0 + q:x1 - q])); runs[i:i + 1] = [(x0, cut), (cut + 1, x1)]
    return runs
for (x0, x1), n in zip(split_to(runs_x(np.asarray(ap)[..., 3], gap=20), np.asarray(ap)[..., 3], 5), ("1_happy_thumbs_up", "2_joyful_celebration", "3_excited", "4_worried", "5_sad")):
    crop_alpha(ap, x0, x1).save(os.path.join(split, "pose_%s.png" % n))
ex = Image.open(os.path.join(CUT, "05-expression-sheet-alpha.png")).convert("RGBA"); cw, ch = ex.width // 4, ex.height // 2
for i, n in enumerate(("1_neutral_idle", "2_relaxed_eyes_closed", "3_glance_side", "4_warm_smile", "5_joyful_laugh", "6_excited", "7_worried", "8_sad")):
    ex.crop(((i % 4) * cw, (i // 4) * ch, (i % 4 + 1) * cw, (i // 4 + 1) * ch)).save(os.path.join(split, "expression_%s.png" % n))
shutil.copy(os.path.join(PRE, "checks", "parts-exploded-sheet.png"), os.path.join(DES, "07_KingVenguard_Rig_Parts_Exploded.png"))

# ---- game-scale readability, rendered from the rig itself
R = Renderer((1080, 1350), 0.5, bg="studio")
shots = [("neutral", None, 0.0), ("laugh", "happy_thumbs_up", 0.62), ("thumbs-up", "happy_thumbs_up", 2.2), ("sad, eyes closed", "range_test", 3.0), ("worried", "range_test", 4.0)]
frames = [(n, R.frame(a, t).convert("RGB")) for n, a, t in shots]
body_box, head_box = (130, 140, 950, 1325), (330, 150, 750, 520)
rows = [("Full body %d px tall" % h, body_box, h) for h in (360, 220, 140)] + [("Head %d px" % h, head_box, h) for h in (160, 110, 72)]
pad = 20; colw = 360
sheet = Image.new("RGB", (pad + len(frames) * (colw + pad), 60 + sum(h + 44 for _, _, h in rows)), (246, 247, 250))
d = ImageDraw.Draw(sheet); d.text((pad, 14), "King Venguard V1 - readability at game scale (rendered from the rig, nearest in-game sizes)", fill=(30, 30, 40), font=FONT(22))
y = 60
for label, box, h in rows:
    d.text((pad, y), label, fill=(80, 80, 90), font=FONT(16)); y += 22
    for i, (n, fr) in enumerate(frames):
        c = fr.crop(box); w = int(c.width * h / c.height); c = c.resize((w, h), Image.LANCZOS)
        x = pad + i * (colw + pad) + (colw - w) // 2; sheet.paste(c, (x, y))
        if label.startswith("Full body 360"): d.text((pad + i * (colw + pad), y - 22 + 0), "", fill=(0, 0, 0))
    y += h + 22
for i, (n, _) in enumerate(frames): d.text((pad + i * (colw + pad) + 4, 40), n, fill=(40, 40, 120), font=FONT(16))
sheet.save(os.path.join(DES, "08_KingVenguard_GameScale_Readability.png"))

# ---- preview stills: keyframe strips and joint-coverage check
def strip(anim, times, name, box=body_box, h=420, title=""):
    tiles = [R.frame(anim, t).convert("RGB").crop(box) for t in times]
    w = int(tiles[0].width * h / tiles[0].height); out = Image.new("RGB", (len(tiles) * (w + 10) + 10, h + 70), (246, 247, 250)); dd = ImageDraw.Draw(out)
    dd.text((10, 8), title, fill=(30, 30, 40), font=FONT(20))
    for i, (t, im) in enumerate(zip(times, tiles)):
        out.paste(im.resize((w, h), Image.LANCZOS), (10 + i * (w + 10), 40)); dd.text((14 + i * (w + 10), h + 44), "%.2f s" % t, fill=(60, 60, 70), font=FONT(16))
    out.save(os.path.join(PRE, name))
strip("idle", [0, 0.6, 1.0, 1.6, 2.0, 2.4, 3.0, 3.6], "KingVenguard_Idle_Keyframes.png", title="idle (4.0 s loop): breath, weight shift, lagged head/whiskers/cape, blink at 1.55 s, glance 2.1-3.35 s")
strip("happy_thumbs_up", [0, 0.22, 0.48, 0.72, 1.35, 1.8, 1.9, 2.12, 2.6, 3.3, 3.8], "KingVenguard_Happy_ThumbsUp_Keyframes.png",
      title="happy_thumbs_up (3.8 s): dip, laugh bounce, settle, thumbs-up swapped in at 1.90 s, pump, return, settle")
RM = Renderer((1080, 1350), 0.5, bg="magenta")
labels = ["setup", "thumbs-up R, left arm bent", "head +8, crown lifted, laugh", "head -8, sad, eyes closed", "hips -16, arms out, elbows bent", "weight shift, skirt/torso twist, fists"]
tiles = [RM.frame("range_test", float(i)).convert("RGB").crop((150, 120, 930, 1330)).resize((390, 605), Image.LANCZOS) for i in range(6)]
out = Image.new("RGB", (3 * 400 + 10, 2 * 640 + 60), (30, 30, 30)); dd = ImageDraw.Draw(out)
dd.text((10, 10), "Joint coverage test on magenta: any magenta inside the silhouette would be a hole (magenta outside is background)", fill=(255, 255, 255), font=FONT(18))
for i, (im, lab) in enumerate(zip(tiles, labels)):
    x, y = 10 + (i % 3) * 400, 50 + (i // 3) * 640; out.paste(im, (x, y)); dd.text((x + 4, y + 608), lab, fill=(255, 255, 255), font=FONT(15))
out.save(os.path.join(PRE, "KingVenguard_Joint_Coverage_Test.png"))
print("done")
