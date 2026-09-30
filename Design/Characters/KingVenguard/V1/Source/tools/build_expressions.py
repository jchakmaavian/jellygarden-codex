"""Expression and deformation parts for King Venguard V1, derived only from existing rig artwork.

No new generation and no Magnific calls. Runs after build_parts.py and fill_orphans.py and updates
RigReady/parts/*.png and parts.json:

  brow_*_worried, brow_*_sad  the existing brow parts bent by a smooth column warp (inner ends lifted)
  eyes_wide_*                 the head's own eye region with the upper lid raised by a smooth vertical warp
  eye_clip_wide               the iris clip polygons pushed through the same warp
  cheek_*                     feathered copies of the head's cheek skin, moved by the cheek bones for smiles
  hand swaps                  colour-matched to the rig hand of the same side (mean/std of skin pixels)
"""
import json, math, os, sys
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
from kv_geometry import CHEEK_R, CHEEK_L

V1 = os.path.abspath(os.path.join(HERE, "..", ".."))
P = os.path.join(V1, "RigReady", "parts")
meta = json.load(open(os.path.join(P, "parts.json")))
W, H = meta["size"]


def load(n):
    return np.asarray(Image.open(os.path.join(P, n + ".png")).convert("RGBA")).astype(np.float32)


def save(name, rgba, x, y, **info):
    a = rgba[..., 3]; ys, xs = np.nonzero(a > 1)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    Image.fromarray(rgba[y0:y1, x0:x1].round().clip(0, 255).astype(np.uint8), "RGBA").save(os.path.join(P, name + ".png"), optimize=True)
    meta["parts"][name] = dict(x=int(x + x0), y=int(y + y0), w=int(x1 - x0), h=int(y1 - y0), **info)


def smoothstep(a, b, x):
    t = np.clip((np.asarray(x, np.float64) - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def premul(img):
    pm = img.copy(); pm[..., :3] *= pm[..., 3:4] / 255.0
    return pm


def sample(pm, sx, sy):
    """Bilinear sample of a premultiplied RGBA array; returns straight alpha. Outside the array is transparent."""
    h, w = pm.shape[:2]
    x0 = np.floor(sx).astype(int); y0 = np.floor(sy).astype(int)
    fx = (sx - x0)[..., None].astype(np.float32); fy = (sy - y0)[..., None].astype(np.float32)

    def at(yy, xx):
        ok = (xx >= 0) & (xx < w) & (yy >= 0) & (yy < h)
        v = np.zeros(xx.shape + (4,), np.float32); v[ok] = pm[yy[ok], xx[ok]]
        return v

    c = (at(y0, x0) * (1 - fx) * (1 - fy) + at(y0, x0 + 1) * fx * (1 - fy)
         + at(y0 + 1, x0) * (1 - fx) * fy + at(y0 + 1, x0 + 1) * fx * fy)
    a = c[..., 3:4]
    rgb = np.where(a > 1e-3, c[..., :3] * 255.0 / np.maximum(a, 1e-3), 0)
    return np.concatenate([rgb, a], -1)


# ------------------------------------------------------------------ brows (worried, sad)
BROWS = {"worried": dict(lift=22, start=0.20, outer=3.0, pinch=3.0),
         "sad": dict(lift=16, start=0.00, outer=12.0, pinch=1.5)}
for side in ("R", "L"):
    p = meta["parts"]["brow_" + side]; pm = premul(load("brow_" + side))
    pt, pb, ps = 36, 24, 12
    Y, X = np.mgrid[0:p["h"] + pt + pb, 0:p["w"] + 2 * ps].astype(np.float32)
    lx, ly = X - ps, Y - pt
    u = np.clip(lx / (p["w"] - 1), 0, 1) if side == "R" else np.clip(1 - lx / (p["w"] - 1), 0, 1)   # 0 outer end, 1 inner end
    for kind, k in BROWS.items():
        dy = -k["lift"] * smoothstep(k["start"], 1.0, u) ** 1.25 + k["outer"] * (1 - u) ** 1.5
        dx = k["pinch"] * smoothstep(0.5, 1.0, u) * (1 if side == "R" else -1)   # inner end toward the nose bridge
        out = sample(pm, lx - dx, ly - dy)
        save("brow_%s_%s" % (side, kind), out, p["x"] - ps, p["y"] - pt, bone="brow_" + side, fill=None,
             derived="brow_%s bent: inner end lifted %d px, outer end moved %+.0f px down, inner end drawn %.1f px toward the nose (existing art, no generation)"
                     % (side, k["lift"], k["outer"], k["pinch"]))

# ------------------------------------------------------------------ wide eyes
hp = meta["parts"]["head"]; HEAD = np.zeros((H, W, 4), np.float32)
HEAD[hp["y"]:hp["y"] + hp["h"], hp["x"]:hp["x"] + hp["w"]] = load("head")
HEAD_PM = premul(HEAD)
D0, HA = 12.0, 30.0          # upper-lid lift at the eye centre, and the height of skin above the lid that absorbs it


def lid_curves(poly):
    poly = np.asarray(poly, float); cx, cy = poly.mean(0)
    up = poly[poly[:, 1] <= cy]; lo = poly[poly[:, 1] >= cy]
    return cx, cy, poly[:, 0].min(), poly[:, 0].max(), up[np.argsort(up[:, 0])], lo[np.argsort(lo[:, 0])]


def forward_y(y, x, xl, xr, up, lo):
    yt = np.interp(x, up[:, 0], up[:, 1]); yb = np.interp(x, lo[:, 0], lo[:, 1])
    wx = np.sin(np.pi * np.clip((x - xl) / (xr - xl), 0, 1)) ** 0.75
    g = np.where(y <= yt, smoothstep(yt - HA, yt, y), 1 - smoothstep(yt, yb, y))
    return y - D0 * wx * g


meta["eye_clip_wide"] = {}
for side in ("R", "L"):
    cx, cy, xl, xr, up, lo = lid_curves(meta["eye_clip"][side])
    x0, x1 = int(xl) - 3, int(math.ceil(xr)) + 4
    y0, y1 = int(up[:, 1].min() - HA - 4), int(math.ceil(lo[:, 1].max())) + 3
    ys_src = np.arange(y0 - 2, y1 + 2, 0.25)
    src_y = np.zeros((y1 - y0, x1 - x0), np.float64)
    for i, x in enumerate(range(x0, x1)):
        fy = forward_y(ys_src, float(x), xl, xr, up, lo)            # monotonic by construction (D0 * slope < 1)
        assert np.all(np.diff(fy) > 0), "wide-eye warp folds at column %d" % x
        src_y[:, i] = np.interp(np.arange(y0, y1, dtype=float), fy, ys_src)
    src_x = np.tile(np.arange(x0, x1, dtype=float), (y1 - y0, 1))
    out = sample(HEAD_PM, src_x, src_y)
    Y, X = np.mgrid[y0:y1, x0:x1]
    out[..., 3] *= (smoothstep(x0, x0 + 4, X) * (1 - smoothstep(x1 - 5, x1 - 1, X))
                    * smoothstep(y0, y0 + 4, Y) * (1 - smoothstep(y1 - 4, y1 - 1, Y)))
    save("eyes_wide_" + side, out, x0, y0, bone="head", fill=None,
         derived="head eye region warped: upper lid raised %.0f px at the centre, corners and lower lid fixed (existing art, no generation)" % D0)
    meta["eye_clip_wide"][side] = [[round(float(px), 1), round(float(forward_y(np.array([py]), float(px), xl, xr, up, lo)[0]), 1)]
                                   for px, py in meta["eye_clip"][side]]

# ------------------------------------------------------------------ cheeks (feathered copies of the head's cheek skin)
Yf, Xf = np.mgrid[0:H, 0:W].astype(np.float32)
for side, c in (("R", CHEEK_R), ("L", CHEEK_L)):
    e = ((Xf - c["cx"]) / c["rx"]) ** 2 + ((Yf - c["cy"]) / c["ry"]) ** 2
    w = (1 - smoothstep(0.35, 1.0, e)) * smoothstep(c["x_min"], c["x_min"] + 16, Xf) * (1 - smoothstep(c["x_max"] - 14, c["x_max"], Xf))
    cx, cy, xl, xr, up, lo = lid_curves(meta["eye_clip"][side])
    yb = np.interp(Xf, lo[:, 0], lo[:, 1])
    near = (Xf > xl - 30) & (Xf < xr + 14)
    w = np.where(near, w * smoothstep(yb + 6, yb + 16, Yf), w)          # fades in below the lower lid: the lid line never moves
    out = HEAD.copy(); out[..., 3] = HEAD[..., 3] * w
    save("cheek_" + side, out, 0, 0, bone="cheek_" + side, fill=None,
         derived="feathered copy of the head's cheek skin; identical to the head at rest, lifted and scaled by cheek_%s for smiles (existing art)" % side)

# ------------------------------------------------------------------ hand swaps colour-matched to the rig hand of the same side
def skin_and_gold(img):
    r, g, b, a = img[..., 0], img[..., 1], img[..., 2], img[..., 3]
    gold = (r > 150) & (g > 110) & (b < 120) & (r - b > 80) & (g - b > 45)   # ring gold: green well above blue (warm skin creases are not)
    return (a > 200) & (r - g > 25) & (r > 140) & (r - b > 40) & ~gold, gold


from PIL import ImageFilter
for swap, target in (("hand_R_thumbsup", "hand_R_open"), ("hand_R_fist", "hand_R_open"),
                     ("hand_R_palm", "hand_R_open"), ("hand_L_fist", "hand_L_open")):
    s = load(swap); t = load(target)
    ms, gs = skin_and_gold(s); mt, _ = skin_and_gold(t)
    mu_s, sd_s = s[ms][:, :3].mean(0), s[ms][:, :3].std(0)
    mu_t, sd_t = t[mt][:, :3].mean(0), t[mt][:, :3].std(0)
    k = np.clip(sd_t / sd_s, 0.85, 1.15)
    ring = gs if swap.startswith("hand_L") else np.zeros_like(gs)        # only the left hand wears the signet ring
    ring_im = Image.fromarray((ring * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.GaussianBlur(1.5))
    keep = (np.asarray(ring_im, np.float32) / 255)[..., None]           # the ring keeps its own colour, with a feathered edge
    shifted = (s[..., :3] - mu_s) * k + mu_t
    s[..., :3] = np.where((s[..., 3] > 0)[..., None], shifted * (1 - keep) + s[..., :3] * keep, s[..., :3])
    Image.fromarray(s.round().clip(0, 255).astype(np.uint8), "RGBA").save(os.path.join(P, swap + ".png"), optimize=True)
    meta["parts"][swap]["colour_matched_to"] = target
    meta["parts"][swap]["colour_shift_rgb"] = [round(float(v), 1) for v in (mu_t - mu_s)]

json.dump(meta, open(os.path.join(P, "parts.json"), "w"), indent=1)
print("expression parts:", [n for n in meta["parts"] if n.startswith(("brow_R_", "brow_L_", "eyes_wide", "cheek_"))])
print("hand colour shifts:", {n: meta["parts"][n]["colour_shift_rgb"] for n in ("hand_R_thumbsup", "hand_R_fist", "hand_R_palm", "hand_L_fist")})
