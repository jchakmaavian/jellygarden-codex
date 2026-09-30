"""Build King Venguard V1 rig parts from the Magnific neutral rig pose and its region edits.

Every part is written as a transparent PNG cropped to its content, with its placement in the
1696 x 2528 source image recorded in RigReady/parts/parts.json.  Hidden areas that Magnific
did not paint are synthesised here and listed in the 'fill' field of each part:
  diffusion  - smooth harmonic inpaint from the surrounding pixels (skin, sclera, breeches)
  reflect    - pixels mirrored across the covering edge (velvet under the cape, breeches under the skirt)
  magnific   - painted by a Magnific region edit (file named in 'fill_source')
"""
import json, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from kv_geometry import *

V1 = os.path.abspath(os.path.join(HERE, "..", ".."))
MAG = os.path.join(V1, "Source", "magnific")
OUT = os.path.join(V1, "RigReady", "parts")
os.makedirs(OUT, exist_ok=True)

def rgb(p):
    return np.asarray(Image.open(p).convert("RGB"), np.float32)

ORIG = rgb(os.path.join(MAG, "04-neutral-rig-pose-front.png"))
A = np.asarray(Image.open(os.path.join(MAG, "cutouts", "04-neutral-rig-pose-front-alpha.png")).convert("RGBA"))[..., 3].astype(np.float32) / 255
RT = {k: rgb(os.path.join(MAG, "retouch", f)) for k, f in {
    'F1': "F1-no-crown.png", 'F2': "F2-no-facehair.png", 'F4': "F4-collar-under-chin.png",
    'S1': "S1-eyes-closed.png", 'S2': "S2-eyes-happy.png", 'S3': "S3-mouth-laugh.png",
    'S4': "S4-mouth-worried.png", 'S5': "S5-mouth-sad.png"}.items()}
YY, XX = np.mgrid[0:H, 0:W]

# ---------------------------------------------------------------- helpers
def pmask(*polys, grow=0):
    m = Image.new("L", (W, H), 0); d = ImageDraw.Draw(m)
    for p in polys: d.polygon(p, fill=255)
    if grow > 0: m = m.filter(ImageFilter.MaxFilter(grow * 2 + 1))
    if grow < 0: m = m.filter(ImageFilter.MinFilter(-grow * 2 + 1))
    return np.asarray(m) > 127

def ellipse(cx, cy, rx, ry):
    return ((XX - cx) / rx) ** 2 + ((YY - cy) / ry) ** 2 <= 1.0

def disc(cx, cy, r):
    return (XX - cx) ** 2 + (YY - cy) ** 2 <= r * r

def morph(m, grow):
    im = Image.fromarray((m * 255).astype(np.uint8))
    im = im.filter(ImageFilter.MaxFilter(grow * 2 + 1) if grow > 0 else ImageFilter.MinFilter(-grow * 2 + 1))
    return np.asarray(im) > 127

def soft(m, radius):
    if radius <= 0: return m.astype(np.float32)
    im = Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(radius))
    return np.asarray(im).astype(np.float32) / 255

def channels(img):
    r, g, b = img[..., 0], img[..., 1], img[..., 2]
    mx = img.max(-1); mn = img.min(-1); sat = (mx - mn) / (mx + 1e-3)
    return r, g, b, mx, sat

r, g, b, mx, sat = channels(ORIG)
HAIR = (sat < 0.17) & (mx > 90) & (mx < 252)
FUR_ORIG = (r > 160) & (g > 140) & (r - b > 24) & (r - g < 38) & (sat > 0.10) & (sat < 0.36)
BLUE = b > r + 25
GOLD = (r > 140) & (g > 90) & (b < 125) & (r - b > 60)
IVORY = (r > 170) & (g > 155) & (b > 115) & (r - b < 72) & (r - b > 6) & ~BLUE
CUFF = (sat < 0.17) & (mx > 165) & ~BLUE
SKIN = (r - g > 28) & (r > 150) & (r - b > 40)
SOLID = A > 0.05

def inpaint(img, fill, iters=500, pad=6):
    """Harmonic fill of `fill` from its surroundings, coarse-to-fine, on the fill's bounding box.
    pad=0 with a fill that is the whole box minus the seed pixels fills from those seeds only (edge-padded box border)."""
    ys, xs = np.nonzero(fill)
    if len(ys) == 0: return img
    y0, y1 = max(0, ys.min() - pad), min(H, ys.max() + pad + 1)
    x0, x1 = max(0, xs.min() - pad), min(W, xs.max() + pad + 1)
    sub = img[y0:y1, x0:x1].copy(); f = fill[y0:y1, x0:x1]
    known = ~f
    sub[f] = sub[known].mean(0) if known.any() else 128
    def relax(s, fm, n):
        for _ in range(n):
            p = np.pad(s, ((1, 1), (1, 1), (0, 0)), mode="edge")
            avg = (p[:-2, 1:-1] + p[2:, 1:-1] + p[1:-1, :-2] + p[1:-1, 2:]) * 0.25
            s[fm] = avg[fm]
        return s
    # coarse pass on a 4x reduced grid to seed large holes
    if max(sub.shape[:2]) > 80:
        h, w = sub.shape[:2]
        small = np.asarray(Image.fromarray(sub.clip(0, 255).astype(np.uint8)).resize((max(2, w // 4), max(2, h // 4)), Image.BILINEAR), np.float32)
        fs = np.asarray(Image.fromarray((f * 255).astype(np.uint8)).resize(small.shape[1::-1], Image.BILINEAR)) > 20
        small = relax(small, fs, iters)
        up = np.asarray(Image.fromarray(small.clip(0, 255).astype(np.uint8)).resize((w, h), Image.BILINEAR), np.float32)
        sub[f] = up[f]
    sub = relax(sub, f, iters)
    out = img.copy(); out[y0:y1, x0:x1] = sub
    return out

def reflect_vertical(dst, src, region, edge_y, span, source_ok, gap=3, upward=True):
    """For every column, mirror source pixels across edge_y[x] into `region` within `span` px."""
    changed = np.zeros((H, W), bool)
    for x in range(W):
        e = edge_y[x]
        if e < 0: continue
        ys = np.arange(e - span, e + 1) if upward else np.arange(e, e + span + 1)
        for y in ys:
            if y < 0 or y >= H or not region[y, x]: continue
            sy = 2 * e - y + (gap if upward else -gap)
            if 0 <= sy < H and source_ok[sy, x]:
                dst[y, x] = src[sy, x]; changed[y, x] = True
    return changed

def bottom_edge(mask):
    e = np.full(W, -1)
    cols = mask.any(0)
    e[cols] = H - 1 - np.argmax(mask[::-1, cols], axis=0)
    return e

def top_edge(mask):
    e = np.full(W, -1)
    cols = mask.any(0)
    e[cols] = np.argmax(mask[:, cols], axis=0)
    return e

PARTS = {}
def save(name, color, alpha, **meta):
    alpha = np.clip(alpha, 0, 1)
    ys, xs = np.nonzero(alpha > 0.004)
    if len(ys) == 0: raise RuntimeError("empty part " + name)
    y0, y1 = max(0, ys.min() - 2), min(H, ys.max() + 3); x0, x1 = max(0, xs.min() - 2), min(W, xs.max() + 3)
    rgba = np.dstack([color[y0:y1, x0:x1].clip(0, 255), alpha[y0:y1, x0:x1] * 255]).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(os.path.join(OUT, name + ".png"), optimize=True)
    PARTS[name] = dict(x=int(x0), y=int(y0), w=int(x1 - x0), h=int(y1 - y0), **meta)

# ---------------------------------------------------------------- crown
x0c, y0c, x1c, y1c = CROWN_BOX
gb = np.full(W, -1)
GOLD_BAND = GOLD & (sat > 0.45)                       # saturated gold only; shaded skin is not gold
for x in range(x0c, x1c):
    for y in range(440, 299, -1):                     # scan up from the brow line: first gold hit is the band's lower edge
        if GOLD_BAND[y, x] and SOLID[y, x]: gb[x] = y; break
valid = gb > 0
gbs = gb.copy()
for x in range(x0c, x1c):
    if valid[x]:
        w = gb[max(x0c, x - 6):x + 7]; w = w[w > 0]; gbs[x] = int(np.median(w))
crown = np.zeros((H, W), bool)
for x in range(x0c, x1c):
    if gbs[x] > 0: crown[y0c:gbs[x] + 2, x] = SOLID[y0c:gbs[x] + 2, x]
crown |= (disc(*CROWN_BEADS[0], 16) | disc(*CROWN_BEADS[1], 16)) & SOLID
bx = (XX >= x0c) & (XX < x1c) & (YY >= y0c) & (YY <= 445)
crown |= bx & SOLID & (BLUE | GOLD_BAND) & ((XX < 690) | (XX > 1006))   # side finials and their sapphires
crown = morph(morph(crown, 1), -1) & SOLID
save("crown", ORIG, A * soft(crown, 0.8), bone="crown", fill=None)

# ---------------------------------------------------------------- head base
base = ORIG.copy()
whisker_cover = ((pmask(MUSTACHE, grow=6) | pmask(CHOP_R, grow=5) | pmask(CHOP_L, grow=5)) & HAIR & SOLID & ~FUR_ORIG)
whisker_cover = morph(morph(morph(whisker_cover, 1), -1), -3)
base[whisker_cover] = RT['F2'][whisker_cover]                                   # thinner hair painted by Magnific, only under opaque whiskers
brow_hair = pmask(BROW_R, BROW_L, grow=3) & HAIR & SOLID
near_temple = morph(HAIR & ~morph(brow_hair, 2) & pmask(BROW_R, BROW_L, grow=20), 12)   # temple hair beside a brow tail
brow_fill = morph(brow_hair, 2) | (pmask(BROW_R, BROW_L, grow=7) & ~HAIR & ~(mx < 110) & ~near_temple)   # temple hair, crown shadow and the skin beside them stay on the head
ys, xs = np.nonzero(morph(brow_fill, 14)); bb = np.zeros((H, W), bool); bb[ys.min():ys.max() + 1, xs.min():xs.max() + 1] = True
SKINLIKE = (r > g) & (g > b) & (r - b > 25) & ~HAIR & (mx > 128)               # lit and shaded skin, not hair or the crown's dark shadow
fore = inpaint(base, bb & ~(bb & ~brow_fill & SKINLIKE & (A > 0.9)), pad=0)   # continues the local forehead and socket shading
base[brow_fill] = fore[brow_fill]                                               # bare forehead under the brows
for (cx, cy, rr), box in ((IRIS_R, EYE_R_BOX), (IRIS_L, EYE_L_BOX)):          # blank sclera under the irises
    bx0, by0, bx1, by1 = box
    inbox = (XX >= bx0) & (XX <= bx1) & (YY >= by0) & (YY <= by1)
    opening = morph(morph(inbox & (((mx > 165) & (sat < 0.24)) | disc(cx, cy, rr - 1)), 3), -3)
    under_iris = disc(cx, cy, rr + 3) & opening & ~((mx < 150) & ~disc(cx, cy, rr + 1))   # the eyeball behind the iris; lids and lash shadow stay as painted
    clean = opening & (mx > 195) & (sat < 0.15)             # clean white of the eye, also the band above the iris inside its circle
    ebox = np.zeros((H, W), bool); ebox[by0:by1 + 1, bx0:bx1 + 1] = True
    white = inpaint(base, ebox & ~clean, iters=700, pad=0)  # seeded by clean white only: no lid pink, no iris edge
    base[under_iris & ~clean] = white[under_iris & ~clean]
band = crown & (YY > (gbs[None, :] - 40)) & (XX >= 630) & (XX <= 1066)
under_crown = morph(crown, -14) & ~band
tmp = base.copy()
reflect_vertical(tmp, base, band, np.where(gbs > 0, gbs, -1), 44, np.ones((H, W), bool), gap=4, upward=True)
base[band] = tmp[band]                                                          # forehead continued under the band
base[under_crown] = RT['F1'][under_crown]                                       # hair dome painted by Magnific
fur_f4 = None
head_region = pmask(HEAD_POLY) & SOLID & ~crown & ~(FUR_ORIG & (YY > 680) & ((XX < 745) | (XX > 951) | (YY > 800)))  # chin skin is fur-coloured; keep it
head_alpha = np.maximum(A * head_region, (band | under_crown).astype(np.float32))
save("head", base, soft(head_alpha > 0.5, 0.6) * np.maximum(A, band | under_crown), bone="head",
     fill="magnific F1 (hair dome), magnific F2 (hair under whiskers), diffusion (forehead under brows, sclera under irises), reflect (forehead under crown band)")

# ---------------------------------------------------------------- facial features
LUM = np.array([0.299, 0.587, 0.114], np.float32)
for side, poly in (("R", BROW_R), ("L", BROW_L)):
    m = pmask(poly, grow=3) & HAIR & SOLID
    m = morph(morph(m, 1), -1)
    h = (A * soft(m, 1.0))[..., None]
    # the soft shadow the brow casts on the forehead travels with the brow: over the painted-out skin band a darkened copy
    # of the bare forehead, with the opacity that reproduces the master's shading (darkening only, never hair or crown shadow)
    lb, lt = base @ LUM, ORIG @ LUM
    halo = (pmask(poly, grow=7) & brow_fill & ~HAIR & ~near_temple & (lt < lb - 1))[..., None]
    a_lum = np.clip((lb - lt) / np.maximum(lb * 0.55, 1), 0, 1)
    need = np.where(ORIG < base, (base - ORIG) / np.maximum(base, 1), (ORIG - base) / np.maximum(255 - base, 1)).max(-1)
    a_s = (np.clip(np.maximum(a_lum, need), 0, 0.85) * halo[..., 0])[..., None]   # smallest opacity that reaches the master in every channel
    C = np.where(a_s > 0.01, np.clip((ORIG - (1 - a_s) * base) / np.maximum(a_s, 1e-3), 0, 255), ORIG)
    out_a = h + (1 - h) * a_s
    out_rgb = (h * ORIG + (1 - h) * a_s * C) / np.maximum(out_a, 1e-3)
    save("brow_" + side, out_rgb, out_a[..., 0], bone="brow_" + side, fill="shadow (the brow's cast shadow on the forehead, taken from the master)")
eye_clip = {}
for side, (cx, cy, rr), box in (("R", IRIS_R, EYE_R_BOX), ("L", IRIS_L, EYE_L_BOX)):
    save("iris_" + side, ORIG, soft(disc(cx, cy, rr), 0.9), bone="iris_" + side, fill=None)
    bx0, by0, bx1, by1 = box
    inbox = (XX >= bx0) & (XX <= bx1) & (YY >= by0) & (YY <= by1)
    opening = inbox & (((mx > 165) & (sat < 0.24)) | disc(cx, cy, rr - 1))
    opening = morph(morph(opening, 3), -3)
    ecx, ecy = (bx0 + bx1) / 2, (by0 + by1) / 2
    pts = []
    for k in range(32):
        ang = 2 * np.pi * k / 32; dx, dy = np.cos(ang), np.sin(ang); last = (ecx, ecy)
        for t in np.arange(0, 80, 0.5):
            px, py = int(round(ecx + dx * t)), int(round(ecy + dy * t))
            if not (0 <= px < W and 0 <= py < H) or not opening[py, px]: break
            last = (ecx + dx * t, ecy + dy * t)
        pts.append([round(last[0], 1), round(last[1], 1)])
    eye_clip[side] = pts
for tag, key in (("closed", "S1"), ("happy", "S2")):
    for side, box in (("R", EYE_R_BOX), ("L", EYE_L_BOX)):
        bx0, by0, bx1, by1 = box
        m = ellipse((bx0 + bx1) / 2, (by0 + by1) / 2, (bx1 - bx0) / 2 - 2, (by1 - by0) / 2 - 2)
        save("eyes_%s_%s" % (tag, side), RT[key], soft(morph(m, -2), 3.0), bone="head", fill="magnific " + key)
cx, cy, rx, ry = MOUTH_ELLIPSE
mouth_m = soft(morph(ellipse(cx, cy, rx - 3, ry - 3), -2), 4.0)
save("mouth_neutral", ORIG, mouth_m, bone="mouth", fill=None)
near_must = morph(pmask(MUSTACHE, grow=6) & HAIR & SOLID, 26)
ys, xs = np.nonzero(mouth_m > 0.02); mbox = np.zeros((H, W), bool); mbox[ys.min():ys.max() + 1, xs.min():xs.max() + 1] = True
for tag, key in (("laugh", "S3"), ("worried", "S4"), ("sad", "S5")):
    img = RT[key]; pr, pg, pb, pmx, psat = channels(img)
    fringe = near_must & (psat < 0.14) & (pmx > 110) & (pmx < 222)
    if tag == "laugh":
        save("mouth_" + tag, img, mouth_m * (1 - soft(fringe, 1.5)), bone="mouth", fill="magnific " + key)
        continue
    # the region edit painted a dark stubble fringe between the mustache and the upper lip: repaint that band from the edit's
    # own skin and lip, opaque, so neither the stubble nor the master's lip underneath shows when the mustache lifts
    lip = (psat > 0.33) & (pr > pg + 25)
    run = lip & np.roll(lip, -1, 0) & np.roll(lip, -2, 0) & mbox & (mouth_m > 0.3)   # three lip rows in a row: the top of the upper lip
    lip_top = np.where(run.any(0), np.argmax(run, 0), H)
    stub = mbox & near_must & (YY < lip_top[None, :] - 1) & (((pmx < 200) & (psat < 0.40)) | fringe)
    seeds = (mouth_m > 0.3) & ~stub & ~fringe
    img = img.copy(); img[stub] = inpaint(img, mbox & ~seeds, iters=500, pad=0)[stub]
    save("mouth_" + tag, img, mouth_m, bone="mouth", fill="magnific " + key + " (stubble under the mustache repainted as skin)")
m = pmask(MUSTACHE, grow=6) & HAIR & SOLID
save("mustache", ORIG, A * soft(morph(morph(m, 1), -1), 1.0), bone="mustache", fill=None)
for side, poly in (("R", CHOP_R), ("L", CHOP_L)):
    m = pmask(poly, grow=5) & HAIR & ~FUR_ORIG & SOLID
    save("chop_" + side, ORIG, A * soft(morph(morph(m, 1), -1), 1.0), bone="chop_" + side, fill=None)

# ---------------------------------------------------------------- neck and collar
chin = pmask(CHIN_ZONE)
f4r, f4g, f4b, f4mx, f4sat = channels(RT['F4'])
FUR_F4 = (f4r > 160) & (f4g > 140) & (f4r - f4b > 24) & (f4r - f4g < 38) & (f4sat > 0.10) & (f4sat < 0.36)
neck_m = chin & ~FUR_F4 & ~(f4b > f4r + 25) & SOLID
neck_m = morph(morph(neck_m, 2), -2)
save("neck", RT['F4'], soft(neck_m, 1.0), bone="neck", fill="magnific F4 (neck and shirt collar behind the chin)")
# The F4 region edit repainted the whole chin zone (its chin, a stubble strip and its shirt collar differ from the master).
# Where the setup pose shows the collar area the master pixels are kept; F4 is used only under the head's outline, where
# it is hidden at rest and only its lower edge can show when the head lifts.
under_head = soft(morph(head_alpha > 0.5, -3), 1.5)[..., None] * chin[..., None]
collar_src = ORIG * (1 - under_head) + RT['F4'] * under_head
csr, csg, csb, csmx, cssat = channels(collar_src)
collar_m = pmask(COLLAR_POLY) & SOLID & ~(csb > csr + 25)   # everything inside the fur outline except coat blue; pale fur highlights included
collar_m = morph(morph(collar_m, 2), -2)
save("collar", collar_src, soft(collar_m, 0.8) * np.where(under_head[..., 0] > 0.5, 1.0, A), bone="chest",
     fill="magnific F4 (neck and collar under the chin only; master pixels wherever the setup pose shows them)")

# ---------------------------------------------------------------- cape, torso
cape_m = {}
LUMI = ORIG @ np.array([0.299, 0.587, 0.114], np.float32)
for side, poly in (("R", CAPE_R), ("L", CAPE_L)):
    m = pmask(poly) & SOLID & ~(FUR_ORIG & pmask(COLLAR_POLY, grow=12))   # fur test only near the collar; gold trim highlights stay
    m = morph(morph(m, 1), -1)
    # the cape's lighter fabric edge just under the gold trim lies inside the sleeve outline; it belongs to the cape, or it
    # rides away with the arm as a pale flap when the arm lifts
    m |= morph(m, 10) & ~m & SOLID & (LUMI > 70) & pmask(SLEEVE_R if side == "R" else SLEEVE_L) & (YY > 1000)
    cape_m[side] = m
    save("cape_" + side, ORIG, A * soft(cape_m[side], 0.8), bone="cape_" + side, fill=None)
sleeve_poly = {"R": pmask(SLEEVE_R), "L": pmask(SLEEVE_L)}
hand_poly = {"R": pmask(HAND_R), "L": pmask(HAND_L)}
torso_m = pmask(TORSO_POLY) & SOLID & ~sleeve_poly["R"] & ~sleeve_poly["L"] & ~hand_poly["R"] & ~hand_poly["L"]
torso_c = ORIG.copy()
coat_ok = pmask(TORSO_POLY) & SOLID & BLUE | (pmask(TORSO_POLY) & GOLD)
for side in ("R", "L"):
    cm = cape_m[side] & pmask(TORSO_POLY)
    reflect_vertical(torso_c, ORIG, cm, bottom_edge(cm), 60, coat_ok & ~cape_m[side], gap=4, upward=True)
    # inner front edge of each cape half: mirror the chest horizontally
    rows = np.nonzero(cm.any(1))[0]
    for y in rows:
        xs = np.nonzero(cm[y])[0]
        e = xs.max() if side == "R" else xs.min()
        rng = range(e - 50, e + 1) if side == "R" else range(e, e + 51)
        for x in rng:
            sx = 2 * e - x + (4 if side == "R" else -4)
            if 0 <= sx < W and cm[y, x] and coat_ok[y, sx] and not cape_m[side][y, sx]:
                torso_c[y, x] = ORIG[y, sx]
save("torso", torso_c, A * soft(torso_m, 0.7), bone="belly", fill="reflect (coat under the cape's lower and front edges)")

# ---------------------------------------------------------------- arms
def split_limb(mask, a, j, b, radius, wedge_to):
    """Split a limb at joint j. Both halves share the disc around the joint. The wedge on the outside of the bend, beyond
    the disc, belongs to neither half line; it goes to the half drawn underneath (`wedge_to`), so turning the joint
    never opens a crack at the sleeve or trouser edge."""
    a, j, b = np.array(a, float), np.array(j, float), np.array(b, float)
    d1 = (j - a) / np.linalg.norm(j - a); d2 = (b - j) / np.linalg.norm(b - j)
    P = np.dstack([XX - j[0], YY - j[1]])
    near = (P[..., 0] ** 2 + P[..., 1] ** 2) <= radius * radius
    wedge = ((P @ d1) > 0) & ((P @ d2) < 0)
    upper = mask & (((P @ d1) <= 0) | near | (wedge if wedge_to == "upper" else False))
    lower = mask & (((P @ d2) >= 0) | near | (wedge if wedge_to == "lower" else False))
    return upper, lower

for side in ("R", "L"):
    S, E, Wr = J["shoulder_" + side], J["elbow_" + side], J["wrist_" + side]
    ew = np.array(Wr, float) - np.array(E, float); ew /= np.linalg.norm(ew)
    near_wrist = ((XX - Wr[0]) * ew[0] + (YY - Wr[1]) * ew[1]) <= 22             # cuff band only, never the fingers
    cuffish = hand_poly[side] & (CUFF | BLUE) & near_wrist
    proj_w = (XX - Wr[0]) * ew[0] + (YY - Wr[1]) * ew[1]
    wrist_skin = sleeve_poly[side] & (proj_w >= -45) & (proj_w <= 22) & SKIN & ~GOLD & ~cuffish   # wrist skin inside the sleeve outline belongs to the hand
    sleeve = (((sleeve_poly[side] & ~wrist_skin) | cuffish) & SOLID & ~cape_m[side]  # cuff corners inside the hand outline stay on the forearm
              & ~(GOLD & morph(cape_m[side], 6)))                                     # the cape's gold trim edge stays with the cape
    arm_c = ORIG.copy()
    # hidden sleeve under the cape and behind the belly's side edge: a capsule along the shoulder-elbow axis, as wide as the
    # sleeve where it leaves the cape. (The hand-drawn outline's straight edges poked out as a hard tab when the cape swung,
    # and without the part behind the belly a 2-3 degree outward swing opened a crack between arm and torso.)
    s_, e_ = np.array(S, float), np.array(E, float); axv = e_ - s_
    tt = np.clip(((XX - s_[0]) * axv[0] + (YY - s_[1]) * axv[1]) / (axv @ axv), 0, 1)
    dist = np.hypot(XX - (s_[0] + tt * axv[0]), YY - (s_[1] + tt * axv[1]))
    cedge = bottom_edge(cape_m[side] & sleeve_poly[side])
    below = sleeve & (cedge[None, :] > 0) & (YY > cedge[None, :]) & (YY <= cedge[None, :] + 24)
    r_arm = float(np.percentile(dist[below], 97)) if below.any() else 100.0
    behind = morph(sleeve, 16) & torso_m                                               # the arm continues 16 px behind the belly's side edge,
    under = (((dist <= r_arm) & cape_m[side]) | behind) & SOLID & ~sleeve               # following its own contour (covers a ~4 degree swing)
    # near the shoulder, keep only hidden pixels that stay inside the silhouette while the arm turns up to 24 degrees either
    # way (the thumbs-up lift is ~23); otherwise the shoulder end swings out through the cape's outline as a dark tab
    solid_im = Image.fromarray((SOLID * 255).astype(np.uint8)); stays = np.ones((H, W), bool)
    for th in (-24, -16, -8, 8, 16, 24):
        stays &= np.asarray(solid_im.rotate(th, resample=Image.NEAREST, center=(float(S[0]), float(S[1])))) > 127
    under &= stays | (tt > 0.55)
    ext = reflect_vertical(arm_c, ORIG, under & cape_m[side], cedge, 120, sleeve, gap=3, upward=True)
    seed = sleeve | ext                                                                 # every visible sleeve pixel keeps the master's colour
    ys, xs = np.nonzero(under | below); box = np.zeros((H, W), bool); box[ys.min():ys.max() + 1, xs.min():xs.max() + 1] = True
    arm_c = inpaint(arm_c, box & ~seed, iters=300, pad=0)                               # the rest of the capsule, from the sleeve only
    ext = ext | under
    sleeve_full = sleeve | ext
    upper, fore = split_limb(sleeve_full, S, E, Wr, 96, wedge_to="upper")          # the upper arm is drawn under the forearm
    save("upperarm_" + side, arm_c, np.maximum(A * upper, ext * 1.0) * soft(upper, 0.7), bone="upperarm_" + side,
         fill="reflect (sleeve under the cape)")
    save("forearm_" + side, ORIG, A * soft(fore & ~ext, 0.7), bone="forearm_" + side, fill=None)
    hand = ((hand_poly[side] & ~cuffish & ~(sleeve_poly[side] & near_wrist)) | wrist_skin) & SOLID
    hand = morph(morph(hand, 2), -2)
    hand_c = ORIG.copy()
    cuffzone = sleeve_poly[side] | hand_poly[side]
    hext = reflect_vertical(hand_c, ORIG, cuffzone & ~hand & SOLID, top_edge(hand), 26, hand, gap=2, upward=True)   # stays inside the silhouette (under the cuff)
    save("hand_%s_open" % side, hand_c, np.maximum(A * hand, hext * 1.0) * soft(hand | hext, 0.7), bone="hand_" + side,
         fill="reflect (wrist continued under the cuff)")

# ---------------------------------------------------------------- legs, pelvis, skirt
skirt_poly = pmask(SKIRT)
belt_band = pmask(TORSO_POLY) & ~pmask([(x, y - 70) for x, y in TORSO_POLY]) & (YY > 1380)   # bottom 70 px of the coat body, full waist width, sides included
skirt_m = (skirt_poly & SOLID & (BLUE | GOLD | ((r < 120) & (b > r)) | ((YY < 1700) & ~IVORY))) | (belt_band & SOLID)  # lit coat-side rims above the hem are skirt too
skirt_m = morph(morph(skirt_m, 1), -1) & SOLID
from render_sequence import interior_holes
_, specks = interior_holes(np.where(skirt_m, 255, 0).astype(np.uint8), 128, max_px=80)
skirt_m |= specks & SOLID                         # velvet highlight specks enclosed by the skirt are skirt, not holes
skirt_m |= BLUE & SOLID & morph(skirt_m, 8) & ~pmask(SKIRT) & (YY > 1700)   # the hem's dark underside just below the outline
save("skirt", ORIG, A * soft(skirt_m, 0.7), bone="skirt", fill=None)
leg_all = np.zeros((H, W), bool)
for side, poly in (("R", LEG_R), ("L", LEG_L)):
    Hj, K, An = J["hip_" + side], J["knee_" + side], J["ankle_" + side]
    leg = pmask(poly) & SOLID & ~(BLUE & (YY < 1950)) & ~(GOLD & (YY < 1930)) & ~skirt_m
    leg_c = ORIG.copy()
    top = top_edge(leg & (YY < 1950))
    zone = pmask(poly) & (YY < 1950)
    ext = reflect_vertical(leg_c, ORIG, zone & ~leg & SOLID, top, 90, leg & IVORY, gap=2, upward=True)
    # rows the mirror skipped (the contact shadow right under the hem is not ivory) and rows beyond its span, up to the
    # hip line, are filled harmonically from the visible breeches and the mirrored rows, so a lifting hem never shows a gap
    under = zone & ~leg & SOLID & (YY < top[None, :])
    seed = ((leg | (under & ~skirt_m)) & SOLID) | ext            # anything the source shows keeps its own pixels
    ys, xs = np.nonzero(under | (leg & (YY < 1950)))
    box = np.zeros((H, W), bool); box[ys.min():ys.max() + 1, xs.min():xs.max() + 1] = True
    leg_c = inpaint(leg_c, box & ~seed, iters=400, pad=0)
    ext = ext | (under & ~ext)
    legf = leg | ext
    leg_all |= legf
    thigh, rest = split_limb(legf, Hj, K, An, 96, wedge_to="lower")               # the shin is drawn under the thigh
    k, an = np.array(K, float), np.array(An, float); d = (an - k) / np.linalg.norm(an - k)
    Pa = (XX - an[0]) * d[0] + (YY - an[1]) * d[1]
    shin = rest & (Pa <= 24)
    foot = legf & (Pa >= -22) & (YY > 2100)
    save("thigh_" + side, leg_c, np.maximum(A * thigh, ext * 1.0) * soft(thigh, 0.7), bone="thigh_" + side,
         fill="reflect (breeches continued under the coat skirt)")
    save("shin_" + side, ORIG, A * soft(shin, 0.7), bone="shin_" + side, fill=None)
    save("foot_" + side, ORIG, A * soft(foot, 0.7), bone="foot_" + side, fill=None)
pel = pmask(PELVIS_POLY)
# seeds: every breeches pixel the source actually shows (any shade, so the shadowed coat opening keeps the master's
# pixels), never the white studio background or the coat; everything else in the box is filled from those seeds only
seen = ((pel | pmask(LEG_R) | pmask(LEG_L)) & (A > 0.9) & ~morph(skirt_m, 2) & ~BLUE & ~GOLD
        & ~pmask(HAND_R, HAND_L, SLEEVE_R, SLEEVE_L, grow=4) & (YY < 1950))
ys, xs = np.nonzero(morph(pel, 16))
box = np.zeros((H, W), bool); box[ys.min():ys.max() + 1, xs.min():xs.max() + 1] = True
pel_c = inpaint(ORIG.copy(), box & ~seen, iters=900, pad=0)
pel_vis = pel & (SOLID | morph(skirt_m, 6))
save("pelvis", pel_c, soft(pel_vis, 1.0) * np.where(SOLID, A, 1.0), bone="hips",
     fill="diffusion (hips under the coat skirt, seeded only by the breeches the source shows; master pixels in the coat opening)")

# ---------------------------------------------------------------- hand replacements from the hand sheet
sheet = np.asarray(Image.open(os.path.join(MAG, "cutouts", "07-hand-replacement-sheet-alpha.png")).convert("RGBA"))
SCALE = 0.41
def hand_from_sheet(name, box, wrist, keep, bone, rotation):
    x0, y0, x1, y1 = box
    crop = sheet[y0:y1, x0:x1].copy()
    yy, xx = np.mgrid[y0:y1, x0:x1]
    crop[..., 3] = (crop[..., 3] * keep(xx, yy)).astype(np.uint8)
    im = Image.fromarray(crop, "RGBA")
    im = im.resize((max(1, int(im.width * SCALE)), max(1, int(im.height * SCALE))), Image.LANCZOS)
    im.save(os.path.join(OUT, name + ".png"), optimize=True)
    PARTS[name] = dict(x=None, y=None, w=im.width, h=im.height, bone=bone, sheet=True,
                       wrist_in_image=[(wrist[0] - x0) * SCALE, (wrist[1] - y0) * SCALE], rotation=rotation,
                       fill=None, source="Source/magnific/07-hand-replacement-sheet.png")
# thumbs-up: forearm enters from the image left, fingers point to image right, thumb up
hand_from_sheet("hand_R_thumbsup", (72, 403, 771, 1069), (356, 845), lambda xx, yy: (xx >= 322).astype(np.float32), "hand_R", 0)
# fists and palm: forearm enters from the image bottom, fingers point up
hand_from_sheet("hand_R_fist", (866, 504, 1349, 1200), (1106, 958), lambda xx, yy: (yy <= 986).astype(np.float32), "hand_R", -90)
hand_from_sheet("hand_L_fist", (1449, 504, 1917, 1201), (1683, 958), lambda xx, yy: (yy <= 986).astype(np.float32), "hand_L", -90)
hand_from_sheet("hand_R_palm", (2033, 337, 2682, 1203), (2290, 968), lambda xx, yy: (yy <= 992).astype(np.float32), "hand_R", -90)

json.dump(dict(source="Source/magnific/04-neutral-rig-pose-front.png", size=[W, H], centre_x=CX, ground_y=GROUND,
               joints=J, eye_clip=eye_clip, parts=PARTS), open(os.path.join(OUT, "parts.json"), "w"), indent=1)
print(len(PARTS), "parts")
for k, v in PARTS.items(): print("%-18s %4sx%-4s at (%s,%s)  %s" % (k, v['w'], v['h'], v['x'], v['y'], v.get('fill') or ''))
