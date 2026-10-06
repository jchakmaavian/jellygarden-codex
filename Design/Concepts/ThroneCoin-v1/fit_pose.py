"""Fit a rounded-cylinder coin model to each CoinItem frame's alpha silhouette, so new frames drop into the same canvases
at the same turn angle, size and position. Model: radius 1, half thickness h, edge rounding rc (shared by all frames); per
frame: turn angle theta about the vertical axis and pixels per unit s. The projected silhouette is symmetric about the turn
axis, so the axis sits at the silhouette's centre. Writes poses.json. usage: fit_pose.py"""
import json, os
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
SP = r"C:\Users\chakm\Documents\Codex\2026-09-08\144\outputs\JellyGarden-Codex\Assets\ReferenceGameplay\Art\Resources\ReferenceGameplay\Sprites"
FRAMES = ["coin.10__21779", "coin.11__22228", "coin__16660", "coin.02__21355", "coin.03__22083", "coin.04__17107",
          "coin.05__17096", "coin.10_g__16629", "coin.11_g__17115"]


def hz(r, h, rc):
    return np.where(r <= 1 - rc, h, (h - rc) + np.sqrt(np.clip(rc * rc - (r - (1 - rc)) ** 2, 0, None)))


def silhouette(W, H, theta, cx, cy, s, h, rc, ss=3, n=500):
    ys = (np.arange(H * ss) + 0.5) / ss
    ly = (cy - ys) / s
    c, sn = np.cos(theta), np.sin(theta)
    cov = np.zeros((H * ss, W))
    j = np.arange(W)
    t = np.linspace(-1, 1, n)
    for i, y in enumerate(ly):
        if abs(y) >= 1: continue
        X = np.sqrt(1 - y * y); x = t * X; r = np.sqrt(x * x + y * y); z = hz(r, h, rc)
        p = np.concatenate([x * c + z * sn, x * c - z * sn])
        a = cx + p.min() * s; b = cx + p.max() * s
        cov[i] = np.clip(np.minimum(j + 1, b) - np.maximum(j, a), 0, 1)
    return cov.reshape(H, ss, W).mean(1)


def iou(a, b):
    return np.minimum(a, b).sum() / max(1e-9, np.maximum(a, b).sum())


def load(name):
    im = np.asarray(Image.open(os.path.join(SP, "Items-CoinItem-" + name + ".png")).convert("RGBA")).astype(np.float64) / 255
    a = im[..., 3]; H, W = a.shape
    cols = np.where(a.max(0) > .5)[0]; rows = np.where(a.max(1) > .5)[0]
    # centre from the coverage-weighted extents: the silhouette is symmetric about the turn axis
    wx = a.sum(0); wy = a.sum(1)
    cx = (wx * (np.arange(W) + .5)).sum() / wx.sum(); cy = (wy * (np.arange(H) + .5)).sum() / wy.sum()
    return dict(name=name, a=a, W=W, H=H, cx=cx, cy=cy, h_px=a.sum(0).max(), w_px=a.sum(1).max())


def golden(f, lo, hi, it=28):
    g = (5 ** .5 - 1) / 2; x1 = hi - g * (hi - lo); x2 = lo + g * (hi - lo); f1, f2 = f(x1), f(x2)
    for _ in range(it):
        if f1 < f2: hi, x2, f2 = x2, x1, f1; x1 = hi - g * (hi - lo); f1 = f(x1)
        else: lo, x1, f1 = x1, x2, f2; x2 = lo + g * (hi - lo); f2 = f(x2)
    return (lo + hi) / 2


frames = [load(n) for n in FRAMES]
for f in frames:
    print("%-18s canvas %dx%d centre (%.2f, %.2f) coverage height %.2f width %.2f" % (f["name"], f["W"], f["H"], f["cx"], f["cy"], f["h_px"], f["w_px"]))


def fit_frame(f, h, rc):
    s0 = f["h_px"] / 2
    def loss_theta(th):
        return 1 - iou(silhouette(f["W"], f["H"], th, f["cx"], f["cy"], s0, h, rc), f["a"])
    th = golden(loss_theta, 0, np.pi / 2, 22)
    s = golden(lambda s: 1 - iou(silhouette(f["W"], f["H"], th, f["cx"], f["cy"], s, h, rc), f["a"]), s0 * .97, s0 * 1.03, 16)
    th = golden(lambda t: 1 - iou(silhouette(f["W"], f["H"], t, f["cx"], f["cy"], s, h, rc), f["a"]), max(0, th - .15), min(np.pi / 2, th + .15), 18)
    return th, s, iou(silhouette(f["W"], f["H"], th, f["cx"], f["cy"], s, h, rc), f["a"])


# Thickness and rounding are shared: judge them on the turned frames, where they shape the silhouette.
turned = [f for f in frames if f["name"].split("__")[0] in ("coin.02", "coin.03", "coin.04", "coin.05", "coin")]
best = None
for h in np.linspace(.20, .44, 9):
    for k in (.45, .65, .85, 1.0):
        rc = h * k
        tot = sum(1 - fit_frame(f, h, rc)[2] for f in turned)
        if best is None or tot < best[0]: best = (tot, h, rc)
        print("  h %.3f rc %.3f -> mean loss %.4f" % (h, rc, tot / len(turned)))
_, h, rc = best
# refine h, then rc
h = golden(lambda hh: sum(1 - fit_frame(f, hh, min(rc, hh))[2] for f in turned), h - .03, h + .03, 10)
rc = golden(lambda r: sum(1 - fit_frame(f, h, r)[2] for f in turned), max(.02, rc - .08), min(h, rc + .08), 10)
print("shared: half thickness h=%.4f edge radius rc=%.4f" % (h, rc))
poses = dict(h=h, rc=rc, frames={})
for f in frames:
    th, s, q = fit_frame(f, h, rc)
    poses["frames"][f["name"]] = dict(W=f["W"], H=f["H"], cx=f["cx"], cy=f["cy"], s=s, theta_deg=float(np.degrees(th)), iou=q)
    print("%-18s theta %6.2f deg  s %.3f px/unit  IoU %.4f" % (f["name"], np.degrees(th), s, q))
json.dump(poses, open(os.path.join(HERE, "poses.json"), "w"), indent=1)
print("wrote poses.json")
