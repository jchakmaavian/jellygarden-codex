"""Checks the rendered CoinItem frames against the frames they replace, then draws the preview sheet.
Checks per frame: identical canvas size; silhouette overlap (IoU of alpha) with the old frame; transparent corners; no dark
fringe (semi-transparent edge pixels keep the colour of the opaque pixels next to them); fully transparent pixels carry a
bled colour rather than black. usage: verify_and_sheet.py <new dir> <sheet.png>"""
import os, sys
import numpy as np
from PIL import Image, ImageDraw

new_dir, sheet_path = sys.argv[1], sys.argv[2]
SP = r"C:\Users\chakm\Documents\Codex\2026-09-08\144\outputs\JellyGarden-Codex\Assets\ReferenceGameplay\Art\Resources\ReferenceGameplay\Sprites"
SPIN = ["coin.10__21779", "coin.11__22228", "coin__16660", "coin.02__21355", "coin.03__22083", "coin.04__17107", "coin.05__17096"]
EXTRA = ["coin.10_g__16629", "coin.11_g__17115"]
L = lambda p: np.asarray(Image.open(p).convert("RGBA")).astype(np.float64) / 255
ok = True
print("%-18s %-9s %-7s %-8s %-18s %-10s" % ("frame", "canvas", "IoU", "corners", "edge/inner luma", "bled rgb"))
for n in SPIN + EXTRA:
    old = L(os.path.join(SP, "Items-CoinItem-" + n + ".png")); new = L(os.path.join(new_dir, "Items-CoinItem-" + n + ".png"))
    same = old.shape == new.shape
    a0, a1 = old[..., 3], new[..., 3]
    iou = np.minimum(a0, a1).sum() / np.maximum(a0, a1).sum()
    corners = max(a1[0, 0], a1[0, -1], a1[-1, 0], a1[-1, -1])
    Y = new[..., :3] @ np.array([.2126, .7152, .0722])
    edge = (a1 > .05) & (a1 < .95)
    inner = (a1 >= .95)
    # opaque pixels within 2 px of an edge pixel: the colour the edge should continue
    near = np.zeros_like(edge)
    for dy in range(-2, 3):
        for dx in range(-2, 3): near |= np.roll(np.roll(edge, dy, 0), dx, 1)
    near &= inner
    ratio = Y[edge].mean() / Y[near].mean()
    bled = new[..., :3][a1 == 0]; black = (bled.max(1) < .02).mean() if len(bled) else 0
    good = same and iou > .965 and corners == 0 and ratio > .9 and black < .02
    ok &= good
    print("%-18s %-9s %.4f  %-8s %.3f / %.3f=%.2f  black %.1f%%  %s" % (n, "%dx%d" % (new.shape[1], new.shape[0]), iou, "clear" if corners == 0 else "%.2f" % corners,
                                                                   Y[edge].mean(), Y[near].mean(), ratio, black * 100, "PASS" if good else "FAIL"))
print("ALL FRAMES PASS" if ok else "SOME FRAMES FAIL")

# ---------------------------------------------------------------- preview sheet
BG = (34, 60, 118, 255); K = 2


def tile(path, label, note=None):
    im = Image.open(path).convert("RGBA"); bg = Image.new("RGBA", im.size, BG); bg.alpha_composite(im)
    t = bg.resize((im.width * K, im.height * K), Image.LANCZOS)
    out = Image.new("RGBA", (max(t.width, 150), t.height + 40), (16, 18, 26, 255)); out.alpha_composite(t, ((out.width - t.width) // 2, 0))
    d = ImageDraw.Draw(out); d.text((4, t.height + 5), label, fill=(235, 235, 240, 255))
    if note: d.text((4, t.height + 21), note, fill=(255, 205, 90, 255))
    return out


def row(title, items):
    tiles = [tile(*it) for it in items]
    w = sum(t.width + 12 for t in tiles) + 16; h = max(t.height for t in tiles) + 44
    r = Image.new("RGBA", (w, h), (16, 18, 26, 255)); d = ImageDraw.Draw(r); d.text((10, 10), title, fill=(255, 255, 255, 255)); x = 10
    for t in tiles: r.alpha_composite(t, (x, 36)); x += t.width + 12
    return r


own = r"C:\Users\chakm\Documents\Codex\2026-09-08\144\outputs\JellyGarden-Codex\Assets\JellyGarden\Resources\RoyalGameplay\coin.webp"
ownp = os.path.join(os.path.dirname(sheet_path), "_own_133.png"); Image.open(own).convert("RGBA").resize((133, 133), Image.LANCZOS).save(ownp)
N = lambda n: os.path.join(new_dir, "Items-CoinItem-" + n + ".png"); O = lambda n: os.path.join(SP, "Items-CoinItem-" + n + ".png")
lab = lambda n: n.split("__")[0]
rows = [
    row("1. ORIGINAL coin frames (imported, being replaced)", [(O(n), lab(n), "on the board" if n == "coin__16660" else None) for n in SPIN + EXTRA]),
    row("2. NEW front views (throne readable)   -   Royal Throne's own shop coin for reference", [(N(n), lab(n)) for n in ("coin.10__21779", "coin.11__22228", "coin.10_g__16629", "coin.11_g__17115")] + [(ownp, "own shop coin", "palette source")]),
    row("3. NEW 3/4 views (throne compresses with the face)", [(N(n), lab(n), "on the board" if n == "coin__16660" else None) for n in ("coin__16660", "coin.02__21355", "coin.03__22083")]),
    row("4. NEW side views (throne only where the face shows; coin.05 is past side-on: the back face)", [(N(n), lab(n)) for n in ("coin.04__17107", "coin.05__17096")]),
]
W = max(r.width for r in rows); H = sum(r.height for r in rows) + 8 * len(rows)
sheet = Image.new("RGBA", (W, H), (10, 11, 16, 255)); y = 0
for r in rows: sheet.alpha_composite(r, (0, y)); y += r.height + 8
sheet.save(sheet_path); print("sheet", sheet_path, sheet.size)
