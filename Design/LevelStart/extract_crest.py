"""Cut the Level Start card's header (crown, scroll ornament, raised tab, ribbon ends) out of the supplied reference.

Output: an RGBA PNG on the reference's own pixel grid (1:1), placed by RoyalLevelCard with its crown axis (x = 538) on the
card's centre line.
- Above the card, the dim background is keyed out by colour distance (dark navy against gold and blue). Only background
  reachable from the top edge, the enclosed openings inside the ornament and a stray magenta glint are keyed.
- Below the card's top border only the ribbon is kept: both ribbon ends (traced polygons, anti-aliased) and the band under
  the gold bar, which fades out just above the LEVEL label. The card face everywhere else is transparent, so the face,
  the halo and the live text drawn in code show through and no reference text or face colour is baked in.
- The sprite's left and right ends overlap the straight part of the drawn top border and fade into it.
"""
import json, sys
import numpy as np
from PIL import Image, ImageDraw

SRC, OUT = sys.argv[1], sys.argv[2]
X0, X1, Y0, Y1 = 236, 841, 34, 324           # crop box in reference pixels (x1/y1 exclusive)
im = np.asarray(Image.open(SRC).convert('RGB')).astype(np.float64)
crop = im[Y0:Y1, X0:X1].copy()
H, W = crop.shape[:2]
ys, xs = np.mgrid[0:H, 0:W]
gy, gx = ys + Y0, xs + X0

# 1. Colour-distance key against the dim background.
BG = np.array([16.0, 41.0, 68.0])
dist = np.sqrt(((crop - BG) ** 2).sum(-1))
D_LO, D_HI = 52.0, 105.0
key = np.clip((dist - D_LO) / (D_HI - D_LO), 0, 1)

# 2. Background region: flood fill from the top row through low-key pixels, plus the ornament's enclosed openings.
low = key < 0.5
bg = np.zeros_like(low)
stack = [(0, x) for x in range(W) if low[0, x]]
for y, x in stack: bg[y, x] = True
while stack:
    y, x = stack.pop()
    for ny, nx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
        if 0 <= ny < H and 0 <= nx < W and low[ny, nx] and not bg[ny, nx] and gy[ny, nx] < 190:
            bg[ny, nx] = True; stack.append((ny, nx))
seen = bg.copy()
for sy in range(H):
    for sx in range(W):
        if low[sy, sx] and not seen[sy, sx] and gy[sy, sx] < 160:
            comp = [(sy, sx)]; seen[sy, sx] = True; i = 0
            while i < len(comp):
                y, x = comp[i]; i += 1
                for ny, nx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
                    if 0 <= ny < H and 0 <= nx < W and low[ny, nx] and not seen[ny, nx]:
                        seen[ny, nx] = True; comp.append((ny, nx))
            if max(c[0] for c in comp) + Y0 < 160 and len(comp) > 12:
                for y, x in comp: bg[y, x] = True
mag = (crop[..., 0] > 55) & (crop[..., 1] < 45) & (crop[..., 2] > 45) & (crop[..., 0] > 1.8 * crop[..., 1])
mag &= (gx >= 582) & (gx <= 600) & (gy >= 86) & (gy <= 106)
bg |= mag; key[mag] = 0
near = bg.copy()
for _ in range(2):
    n = near.copy()
    n[1:, :] |= near[:-1, :]; n[:-1, :] |= near[1:, :]; n[:, 1:] |= near[:, :-1]; n[:, :-1] |= near[:, 1:]
    near = n
alpha = np.where(near, key, 1.0)
alpha[bg] = np.minimum(alpha[bg], key[bg])
a3 = alpha[..., None]
rgb = np.clip(np.where(a3 > 0.02, (crop - (1 - a3) * BG) / np.maximum(a3, 1e-3), crop), 0, 255)

# 3. What stays below the top border: the ribbon ends and the band under the gold bar.
def coverage(points, scale=4):
    """Anti-aliased coverage of a polygon given in reference pixels (pixel centres at +0.5)."""
    img = Image.new('L', (W * scale, H * scale), 0)
    ImageDraw.Draw(img).polygon([((x - X0) * scale, (y - Y0) * scale) for x, y in points], fill=255)
    return np.asarray(img.resize((W, H), Image.BOX)).astype(np.float64) / 255
left_end = [(330, 176), (330, 287), (334, 290), (342, 293), (351, 297), (359, 300), (366, 302), (372, 299), (377, 293),
            (377, 286), (377, 280), (380, 274), (381, 268), (382, 262), (386, 256), (386, 250), (385, 244), (390, 238),
            (390, 232), (395, 226), (395, 220), (400, 214), (405, 208), (411, 202), (416, 196), (421, 190), (423, 176)]
right_end = [(749, 176), (748, 281), (743, 287), (738, 293), (723, 299), (713, 302), (703, 299), (699, 293), (698, 287),
             (698, 281), (698, 275), (693, 269), (693, 262), (693, 256), (688, 250), (688, 244), (683, 238), (683, 232),
             (678, 226), (678, 220), (673, 214), (673, 208), (668, 202), (660, 197), (656, 191), (652, 176)]
ribbon = np.maximum(coverage(left_end), coverage(right_end))
# The top border's lower edge (its thin blue highlight included) rises from y 190.5 at the card's straight top to 187.5
# where the tab begins.
def border_bottom(x):
    d = np.minimum(np.abs(x - 330.5), np.abs(x - 748.5))
    return np.where((x > 330) & (x < 749), 197.0, 187.5 + np.clip((d - 30) / 50, 0, 1) * 3)
above = np.clip(border_bottom(gx + .5) + .5 - gy, 0, 1)
# Between the ribbon ends the band under the gold bar fades out above the LEVEL label (y 196 -> 206).
band = np.where((gx > 330) & (gx < 749), np.clip((206 - gy) / 10.0, 0, 1), 0)
keep = np.maximum.reduce([above, band, ribbon])
alpha = alpha * keep

# 4. The ends overlap the straight top border drawn in code; fade them into it.
FADE = 30.0
side = np.clip(np.minimum(xs, W - 1 - xs) / FADE, 0, 1)
alpha = alpha * np.where(gy >= 150, side, 1.0)

out = np.dstack([rgb, alpha * 255]).round().astype(np.uint8)
Image.fromarray(out, 'RGBA').save(OUT)
meta = {"crop": [X0, Y0, X1, Y1], "size": [W, H], "crownAxisX": 538.0}
json.dump(meta, open(OUT.replace('.png', '.json'), 'w'), indent=1)
print('saved', OUT, W, H, 'opaque px', int((alpha > .99).sum()), 'transparent px', int((alpha < .01).sum()))
