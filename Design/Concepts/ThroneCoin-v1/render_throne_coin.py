"""Royal Throne coin with a throne emblem: an original coin rendered in 3D (numpy ray marching), not derived from any
imported coin art. Its gold is mapped onto the luminance ramp of Royal Throne's own coin (RoyalGameplay/coin.webp,
own_gold_ramp.json). Each output frame reuses a CoinItem frame's canvas size, turn angle, scale and position (poses.json,
fitted to the existing silhouettes by fit_pose.py) so the new art drops into the same sprites.

usage: render_throne_coin.py <out dir> [frame ...]      (no frame names: every frame in poses.json)
       render_throne_coin.py --emblem <out.png>          (a flat preview of the emblem's relief only)
"""
import json, os, sys, time
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
POSES = json.load(open(os.path.join(HERE, "poses.json")))
RAMP = json.load(open(os.path.join(HERE, "own_gold_ramp.json")))

# ------------------------------------------------------------------ shape (coin radius 1)
H = POSES["h"]            # half thickness at the rim top
RC = POSES["rc"]          # rounding of the outer edge
FIELD_R = 0.720           # the recessed field ends here
BEVEL_W = 0.070           # inner bevel from the field up to the rim top
FIELD_DEPTH = 0.115       # the field sits this far below the rim top
EMBLEM_SCALE = 1.20
EMBLEM_BEVEL = 0.034
PANEL_DEPTH = float(os.environ.get("PANEL_DEPTH", "0"))   # a recessed back panel read as double outlines: solid back
N_GRID = 1400             # relief grid over [-1, 1]^2

# ------------------------------------------------------------------ throne emblem (2D signed distances, <0 inside)


def sd_box(x, y, cx, cy, hx, hy, r):
    qx = np.abs(x - cx) - (hx - r); qy = np.abs(y - cy) - (hy - r)
    return np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - r


def sd_circle(x, y, cx, cy, r):
    return np.hypot(x - cx, y - cy) - r


def bevel(sd, width):
    """Rounded (quarter-circle) emboss profile: 0 outside, rising steeply at the outline, flat on top."""
    t = np.clip(-sd / width, 0, 1)
    return np.sqrt(np.clip(1 - (1 - t) ** 2, 0, 1))


def throne_parts(x, y):
    """The throne seen from the front, centred on the face: a tall arched back with one small knob, sturdy armrests,
    a seat slab, and a solid base apron standing on two short feet. Design units span y -0.32..0.58."""
    x = x / EMBLEM_SCALE; y = y / EMBLEM_SCALE + 0.13
    ax = np.abs(x)
    body = np.minimum(sd_box(x, y, 0, 0.14, 0.215, 0.16, 0.004),                       # back, up to the arch
                      np.maximum(sd_circle(x, y, 0, 0.30, 0.232), ax - 0.215))          # round arch top (peak 0.532)
    back = np.minimum(body, sd_circle(x, y, 0, 0.545, 0.052))                          # one knob on the peak
    panel = body + 0.078                                                               # the back's recessed panel
    arms = np.minimum(sd_box(ax, y, 0.305, 0.04, 0.064, 0.09, 0.026), sd_circle(ax, y, 0.305, 0.14, 0.072))
    seat = sd_box(x, y, 0, -0.075, 0.372, 0.064, 0.032)
    apron = np.maximum(sd_box(x, y, 0, -0.195, 0.31, 0.07, 0.022), -sd_box(x, y, 0, -0.31, 0.13, 0.09, 0.05))
    feet = sd_box(ax, y, 0.258, -0.275, 0.062, 0.055, 0.024)
    return dict(back=back, panel=panel, arms=arms, seat=seat, base=np.minimum(apron, feet))


def emblem_height(x, y):
    """Layered emboss: the seat stands in front (highest), arms and base next, the back behind with a recessed panel."""
    p = throne_parts(x, y); b = EMBLEM_BEVEL
    back = 0.062 * bevel(p["back"], b) - PANEL_DEPTH * bevel(p["panel"], b * 1.2)
    return np.maximum.reduce([back, 0.080 * bevel(p["arms"], b), 0.074 * bevel(p["base"], b), 0.098 * bevel(p["seat"], b)])


def radial_profile(r):
    """Half thickness by radius: recessed field, inner bevel, flat rim top, rounded outer edge."""
    t = np.clip((r - FIELD_R) / BEVEL_W, 0, 1); s = t * t * t * (t * (6 * t - 15) + 10)       # smootherstep
    z = (H - FIELD_DEPTH) + FIELD_DEPTH * s
    edge = (H - RC) + np.sqrt(np.clip(RC * RC - (r - (1 - RC)) ** 2, 0, None))
    return np.where(r > 1 - RC, edge, z)


def radial_slope(r):
    t = np.clip((r - FIELD_R) / BEVEL_W, 0, 1); ds = 30 * t * t * (t - 1) ** 2 / BEVEL_W
    inner = np.where((r > FIELD_R) & (r < FIELD_R + BEVEL_W), FIELD_DEPTH * ds, 0.0)
    return inner          # the outer rounding is shaded with its own analytic normal


# relief grid (row 0 = y +1, column 0 = x -1), its gradient and a cavity (ambient occlusion) term
g = np.linspace(-1, 1, N_GRID); GX, GY = np.meshgrid(g, -g)
REL = emblem_height(GX, GY) * (np.hypot(GX, GY) < FIELD_R)
DX = g[1] - g[0]
REL_Y, REL_X = np.gradient(REL, DX)      # np.gradient: axis 0 (rows) first; rows run downward in y
REL_Y = -REL_Y


def blur(a, radius):
    k = np.exp(-0.5 * (np.arange(-3 * radius, 3 * radius + 1) / radius) ** 2); k /= k.sum()
    a = np.apply_along_axis(lambda v: np.convolve(v, k, mode="same"), 0, a)
    return np.apply_along_axis(lambda v: np.convolve(v, k, mode="same"), 1, a)


FACE = radial_profile(np.hypot(GX, GY)) + REL
# contact shadow at two scales: tight in the emblem's corners, broad where the field meets the emblem and the rim
CAVITY = np.clip(blur(FACE, 10) - FACE, 0, None) + 0.6 * np.clip(blur(FACE, 34) - FACE, 0, None)
AO = 1 - np.clip(CAVITY / 0.025, 0, 1) * 0.60


def sample(grid, x, y):
    fj = np.clip((x + 1) * .5 * (N_GRID - 1), 0, N_GRID - 1.001); fi = np.clip((1 - y) * .5 * (N_GRID - 1), 0, N_GRID - 1.001)
    j0 = fj.astype(np.int32); i0 = fi.astype(np.int32); wx = fj - j0; wy = fi - i0
    return (grid[i0, j0] * (1 - wx) * (1 - wy) + grid[i0, j0 + 1] * wx * (1 - wy)
            + grid[i0 + 1, j0] * (1 - wx) * wy + grid[i0 + 1, j0 + 1] * wx * wy)


def surface(x, y):
    r = np.hypot(x, y)
    return np.where(r <= 1, radial_profile(r) + sample(REL, x, y) * (r < FIELD_R), -1.0), r


# ------------------------------------------------------------------ ray marching (orthographic, turn about the vertical axis)


def trace(x, y, theta, steps=340, refine=14):
    """World rays at (x, y) looking along -z into the coin turned by theta: hit mask, local hit point."""
    c, s = np.cos(theta), np.sin(theta); Z0 = 3.0
    ox = x * c - Z0 * s; oy = y; oz = x * s + Z0 * c
    dx, dz = s, -c
    zmax = H + 0.002
    # slab |z| <= zmax and cylinder x^2 + y^2 <= 1, intersected
    with np.errstate(divide="ignore", invalid="ignore"):
        ta, tb = (oz - zmax) / c, (oz + zmax) / c
        tz0, tz1 = np.minimum(ta, tb), np.maximum(ta, tb)
        X = np.sqrt(np.clip(1 - y * y, 0, None))
        if abs(s) > 1e-9:
            ua, ub = (-X - ox) / dx, (X - ox) / dx; tx0, tx1 = np.minimum(ua, ub), np.maximum(ua, ub)
        else:
            inside_x = np.abs(ox) <= X; tx0 = np.where(inside_x, -1e9, 1e9); tx1 = np.where(inside_x, 1e9, -1e9)
    if abs(c) < 1e-9: tz0, tz1 = np.full_like(x, -1e9), np.full_like(x, 1e9)
    t0 = np.maximum(tz0, tx0); t1 = np.minimum(tz1, tx1)
    live = (np.abs(y) < 1) & (t1 > t0)
    idx = np.where(live)[0]; t0, t1 = t0[idx], t1[idx]; dt = (t1 - t0) / steps
    hit_t = np.full(len(idx), np.nan); prev_t = np.zeros(len(idx))
    act = np.arange(len(idx))
    for k in range(steps + 1):
        if len(act) == 0: break
        t = t0[act] + k * dt[act]
        px = ox[idx[act]] + t * dx; py = oy[idx[act]]; pz = oz[idx[act]] + t * dz
        zs, r = surface(px, py)
        inside = (r <= 1) & (np.abs(pz) <= zs)
        if inside.any():
            got = act[inside]; hit_t[got] = t[inside]; prev_t[got] = t[inside] - dt[got]
            act = act[~inside]
    ok = ~np.isnan(hit_t)
    lo, hi = prev_t[ok], hit_t[ok]; sel = idx[ok]
    for _ in range(refine):
        mid = (lo + hi) * .5
        px = ox[sel] + mid * dx; py = oy[sel]; pz = oz[sel] + mid * dz
        zs, r = surface(px, py); inside = (r <= 1) & (np.abs(pz) <= zs)
        hi = np.where(inside, mid, hi); lo = np.where(inside, lo, mid)
    t = hi
    return sel, ox[sel] + t * dx, oy[sel], oz[sel] + t * dz


def normals(px, py, pz):
    """Local-space outward normals: side wall, rounded edge (torus) or the face's height field."""
    r = np.hypot(px, py); rs = np.maximum(r, 1e-9); sz = np.where(pz >= 0, 1.0, -1.0)
    zs, _ = surface(px, py)
    wall = (1 - r) < (zs - np.abs(pz)) - 1e-6          # entered through the cylinder, below the rounded edge
    corner = (~wall) & (r > 1 - RC)
    n = np.zeros((len(px), 3))
    n[wall] = np.stack([px[wall] / rs[wall], py[wall] / rs[wall], np.zeros(wall.sum())], -1)
    if corner.any():
        cx = (1 - RC) * px[corner] / rs[corner]; cy = (1 - RC) * py[corner] / rs[corner]; cz = sz[corner] * (H - RC)
        v = np.stack([px[corner] - cx, py[corner] - cy, pz[corner] - cz], -1); n[corner] = v / np.linalg.norm(v, axis=1, keepdims=True)
    face = ~(wall | corner)
    if face.any():
        fx, fy = px[face], py[face]; fr = rs[face]
        slope = radial_slope(fr)
        gx = slope * fx / fr + sample(REL_X, fx, fy) * (fr < FIELD_R)
        gy = slope * fy / fr + sample(REL_Y, fx, fy) * (fr < FIELD_R)
        v = np.stack([-gx, -gy, sz[face]], -1); n[face] = v / np.linalg.norm(v, axis=1, keepdims=True)
    ao = np.where(face, sample(AO, px, py), 1.0)
    lift = np.where(face & (r < FIELD_R + BEVEL_W), np.clip((np.abs(pz) - (H - FIELD_DEPTH)) / 0.085, 0, 1), 1.0)
    return n, ao, wall | corner, lift


# ------------------------------------------------------------------ shading (world space, light from the top left)

def nrm(v): v = np.asarray(v, float); return v / np.linalg.norm(v)
KEY = nrm([-0.40, 0.66, 0.64]); VIEW = np.array([0, 0, 1.0]); HALF = nrm(KEY + VIEW)


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t)


def environment(R):
    """A small studio seen in reflections: a tall softbox on the left, a brighter top, a darker floor and back.
    Flat faces reflect the neutral front (0.70), so their tone stays set by the key light. The softbox streak lands on
    normals about 75 degrees from the view, so it sits on the edge band in every turned frame."""
    az = np.arctan2(R[:, 0], R[:, 2]); vy = R[:, 1]
    window = smoothstep(-0.45, -0.15, vy) * (1 - smoothstep(0.70, 0.95, vy))
    box = np.exp(-((az + 2.62) / 0.30) ** 2) * window
    top = smoothstep(0.45, 0.95, vy)
    floor = smoothstep(-0.25, -0.75, vy)
    back = smoothstep(2.2, 2.9, np.abs(az)) * (1 - top)
    return 0.70 + 0.26 * box + 0.12 * top - 0.34 * floor - 0.20 * back


def shade(nw, ao, edge, wx, wy, lift, warm=0.0):
    """Toon-metal gold on the own coin's ramp. A wrapped key light from the top left sets the tone of faces; grazing
    surfaces lean on the studio reflection (more metal the more they turn away), which gives the edge band its streak
    and the rim its deeper underside. The recessed field is held a little deeper than the raised emblem and rim, contact
    shadow darkens corners, a gentle sheen runs from the upper left to the lower right, and glints go near white:
    a broad one that survives anti-aliasing and a small sharp sparkle."""
    ndl = nw @ KEY
    wrap = np.clip((ndl + 0.45) / 1.45, 0, 1)
    ndv = np.clip(nw @ VIEW, 0, 1)
    R = 2 * ndv[:, None] * nw - VIEW
    ndh = np.clip(nw @ HALF, 0, 1)
    lit = 0.16 + 0.74 * wrap + 0.08 * ndh ** 24
    metal = 0.30 + 0.50 * (1 - ndv)
    lum = (1 - metal) * lit + metal * environment(R)
    lum = lum * (0.84 + 0.16 * lift) * (0.62 + 0.38 * ao) + 0.045 * (wy - 0.6 * wx)
    col = ramp(np.clip(lum, 0, 1), warm)
    glint = np.clip(0.70 * ndh ** 60 + 0.55 * ndh ** 320, 0, 1)
    col = col + (np.array([1.0, 0.99, 0.90]) - col) * glint[:, None]
    return np.clip(col, 0, 1)


RY = np.array(RAMP["y"]); RRGB = np.array(RAMP["rgb"])


def ramp(lum, warm=0.0):
    # the own coin's luminance -> colour relationship; lum 0..1 is spread over its luminance range
    y = RY[0] + (RY[-1] - RY[0]) * np.clip(lum, 0, 1)
    col = np.stack([np.interp(y, RY, RRGB[:, ch]) for ch in range(3)], -1)
    if warm:  # the "_g" frames' warmer grade: pull green and blue down, as the originals do
        col = col * np.array([1.0, 1 - 0.16 * warm, 1 - 0.45 * warm])
    return col


# ------------------------------------------------------------------ frames


def render(W, Hh, cx, cy, s, theta_deg, ss=4, warm=0.0):
    th = np.radians(theta_deg); c, sn = np.cos(th), np.sin(th)
    us = (np.arange(W * ss) + .5) / ss; vs = (np.arange(Hh * ss) + .5) / ss
    U, V = np.meshgrid(us, vs); x = ((U - cx) / s).ravel(); y = ((cy - V) / s).ravel()
    sel, px, py, pz = trace(x, y, th)
    n, ao, edge, lift = normals(px, py, pz)
    nw = np.stack([n[:, 0] * c + n[:, 2] * sn, n[:, 1], -n[:, 0] * sn + n[:, 2] * c], -1)    # local -> world
    wx = px * c + pz * sn                                                           # hit point, world x
    col = shade(nw, ao, edge, wx, py, lift, warm)
    rgb = np.zeros((len(x), 3)); a = np.zeros(len(x)); rgb[sel] = col; a[sel] = 1
    rgb = rgb.reshape(Hh, ss, W, ss, 3).sum((1, 3)); a = a.reshape(Hh, ss, W, ss).sum((1, 3))
    straight = np.where(a[..., None] > 0, rgb / np.maximum(a, 1e-9)[..., None], 0)
    return straight, a / (ss * ss)


def bleed(rgb, a, rounds=400):
    """Fill every fully transparent pixel with its neighbours' colour (grown outwards until none is left), so bilinear
    filtering, or mipmaps if they are ever enabled, never pull black into the coin's edge."""
    rgb = rgb.copy(); known = a > 0
    for _ in range(rounds):
        if known.all(): break
        acc = np.zeros_like(rgb); cnt = np.zeros(a.shape)
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                if dx == 0 and dy == 0: continue
                sh = np.roll(np.roll(rgb * known[..., None], dy, 0), dx, 1); k = np.roll(np.roll(known, dy, 0), dx, 1)
                acc += sh; cnt += k
        grow = (~known) & (cnt > 0)
        rgb[grow] = acc[grow] / cnt[grow][:, None]; known = known | grow
    return rgb


def smear_left(rgb, a, width):
    """The .11_g frame's short motion smear on its left edge: a fading copy of the left silhouette, drawn behind."""
    out_a = a.copy(); out_rgb = rgb * a[..., None]
    for k in range(1, width + 1):
        f = 0.075 * (1 - k / (width + 1))
        sa = np.roll(a, -k, 1) * f; sa[:, -k:] = 0; srgb = np.roll(rgb, -k, 1)
        add = sa * (1 - out_a)
        out_rgb += srgb * add[..., None]; out_a += add
    return np.where(out_a[..., None] > 0, out_rgb / np.maximum(out_a, 1e-9)[..., None], 0), out_a


def save(rgb, a, path):
    rgb = bleed(rgb, a)
    img = np.dstack([np.clip(rgb, 0, 1) * 255, np.clip(a, 0, 1) * 255]).round().astype(np.uint8)
    Image.fromarray(img, "RGBA").save(path, optimize=True)


# per frame: pose from poses.json; the "_g" frames share their base frame's pose, with the warmer grade
BASE_POSE = {"coin.10_g__16629": "coin.10__21779", "coin.11_g__17115": "coin.11__22228"}
BACK_FACE = {"coin.05__17096"}          # past side-on: the relief shows on the left, so the back face is seen


def frame_params(name):
    f = POSES["frames"][name]; p = dict(W=f["W"], Hh=f["H"], cx=f["cx"], cy=f["cy"], s=f["s"], theta_deg=f["theta_deg"], warm=0.0)
    if name in BASE_POSE:
        b = POSES["frames"][BASE_POSE[name]]; p.update(theta_deg=b["theta_deg"], s=b["s"], warm=1.0)
    if name in BACK_FACE: p["theta_deg"] = 180 - p["theta_deg"]
    return p


if __name__ == "__main__":
    if sys.argv[1] == "--emblem":
        hgt = emblem_height(GX, GY); gy, gx = np.gradient(hgt, DX); n = np.dstack([gx, gy, np.ones_like(hgt)]); n /= np.linalg.norm(n, axis=2, keepdims=True)
        lit = np.clip(n @ nrm([-0.5, 0.6, 0.62]), 0, 1); img = (np.clip(lit * 0.85 + (hgt > 1e-4) * 0.1, 0, 1) * 255).astype(np.uint8)
        Image.fromarray(img).resize((700, 700), Image.LANCZOS).save(sys.argv[2]); print("emblem preview", sys.argv[2]); sys.exit()
    out = sys.argv[1]; os.makedirs(out, exist_ok=True)
    names = sys.argv[2:] or list(POSES["frames"])
    for name in names:
        t0 = time.time(); p = frame_params(name)
        rgb, a = render(p["W"], p["Hh"], p["cx"], p["cy"], p["s"], p["theta_deg"], warm=p["warm"])
        if name == "coin.11_g__17115": rgb, a = smear_left(rgb, a, 5)
        save(rgb, a, os.path.join(out, "Items-CoinItem-" + name + ".png"))
        print("%-18s %dx%d theta %.2f -> %.1fs" % (name, p["W"], p["Hh"], p["theta_deg"], time.time() - t0), flush=True)
