"""Minimal Spine 3.8 JSON evaluator used for the King Venguard V1 approval previews.

Implements only what the rig uses: bone hierarchy with translate/rotate/scale (no shear),
rotate/translate/scale timelines with linear, stepped and Bezier curves, slot attachment
timelines, region attachments and clipping attachments.  It is NOT the official Spine runtime;
the rig has not been opened in the Spine Editor or spine-unity.
"""
import math

def _bezier_y_at_x(cx1, cy1, cx2, cy2, x):
    # cubic Bezier from (0,0) to (1,1); solve for parameter s with x(s) = x, then return y(s)
    lo, hi = 0.0, 1.0
    for _ in range(40):
        s = (lo + hi) * 0.5
        xs = 3 * (1 - s) ** 2 * s * cx1 + 3 * (1 - s) * s * s * cx2 + s ** 3
        if xs < x: lo = s
        else: hi = s
    s = (lo + hi) * 0.5
    return 3 * (1 - s) ** 2 * s * cy1 + 3 * (1 - s) * s * s * cy2 + s ** 3

def _mix(k0, k1, t):
    """Interpolation fraction between keyframes k0 and k1 at time t using k0's curve."""
    t0, t1 = k0["time"], k1["time"]
    if t1 <= t0: return 1.0
    x = (t - t0) / (t1 - t0)
    c = k0.get("curve")
    if c is None: return x
    if c == "stepped": return 0.0
    return _bezier_y_at_x(float(c), k0.get("c2", 0.0), k0.get("c3", 1.0), k0.get("c4", 1.0), x)

def sample(keys, t, fields, default):
    if not keys: return default
    if t <= keys[0]["time"]: return tuple(keys[0].get(f, d) for f, d in zip(fields, default))
    if t >= keys[-1]["time"]: return tuple(keys[-1].get(f, d) for f, d in zip(fields, default))
    for i in range(len(keys) - 1):
        k0, k1 = keys[i], keys[i + 1]
        if k0["time"] <= t < k1["time"]:
            a = _mix(k0, k1, t)
            return tuple(k0.get(f, d) + (k1.get(f, d) - k0.get(f, d)) * a for f, d in zip(fields, default))
    return default

class Skeleton:
    def __init__(self, data):
        self.data = data
        self.bones = data["bones"]
        self.index = {b["name"]: i for i, b in enumerate(self.bones)}
        self.slots = data["slots"]
        skin = data["skins"][0]["attachments"]
        self.skin = skin

    def pose(self, anim=None, t=0.0):
        """Returns {bone: (a, b, c, d, wx, wy)} world matrices and {slot: attachment name or None}."""
        tl = (self.data["animations"].get(anim) or {}) if anim else {}
        btl = tl.get("bones", {})
        world = {}
        for b in self.bones:
            name = b["name"]
            x, y = b.get("x", 0.0), b.get("y", 0.0)
            rot = b.get("rotation", 0.0)
            sx, sy = b.get("scaleX", 1.0), b.get("scaleY", 1.0)
            ch = btl.get(name, {})
            if "rotate" in ch: rot += sample(ch["rotate"], t, ("angle",), (0.0,))[0]
            if "translate" in ch:
                dx, dy = sample(ch["translate"], t, ("x", "y"), (0.0, 0.0)); x += dx; y += dy
            if "scale" in ch:
                ksx, ksy = sample(ch["scale"], t, ("x", "y"), (1.0, 1.0)); sx *= ksx; sy *= ksy
            r = math.radians(rot); ry = math.radians(rot + 90)
            la, lb, lc, ld = math.cos(r) * sx, math.cos(ry) * sy, math.sin(r) * sx, math.sin(ry) * sy
            p = b.get("parent")
            if p is None:
                world[name] = (la, lb, lc, ld, x, y)
            else:
                pa, pb, pc, pd, px, py = world[p]
                world[name] = (pa * la + pb * lc, pa * lb + pb * ld, pc * la + pd * lc, pc * lb + pd * ld,
                               pa * x + pb * y + px, pc * x + pd * y + py)
        stl = tl.get("slots", {})
        active = {}
        for s in self.slots:
            att = s.get("attachment")
            keys = stl.get(s["name"], {}).get("attachment")
            if keys:
                cur = att if t < keys[0]["time"] else None
                for k in keys:
                    if k["time"] <= t + 1e-9: cur = k.get("name")
                att = cur
            active[s["name"]] = att
        return world, active

def world_rotation(m):
    return math.degrees(math.atan2(m[2], m[0]))

def apply(m, x, y):
    a, b, c, d, wx, wy = m
    return a * x + b * y + wx, c * x + d * y + wy

def region_matrix(bone_m, att):
    """Affine 3x3 (as nested lists) mapping image pixel (u, v) -> skeleton world (x, y)."""
    w, h = att["width"], att["height"]
    sx, sy = att.get("scaleX", 1.0), att.get("scaleY", 1.0)
    r = math.radians(att.get("rotation", 0.0)); cr, sr = math.cos(r), math.sin(r)
    # pixel -> attachment local: (u - w/2, h/2 - v)
    P = [[sx, 0, -w / 2 * sx], [0, -sy, h / 2 * sy], [0, 0, 1]]
    R = [[cr, -sr, att.get("x", 0.0)], [sr, cr, att.get("y", 0.0)], [0, 0, 1]]
    a, b, c, d, wx, wy = bone_m
    B = [[a, b, wx], [c, d, wy], [0, 0, 1]]
    return mat(B, mat(R, P))

def mat(A, B):
    return [[sum(A[i][k] * B[k][j] for k in range(3)) for j in range(3)] for i in range(3)]

def inv(M):
    a, b, c = M[0]; d, e, f = M[1]
    det = a * e - b * d
    return [[e / det, -b / det, (b * f - c * e) / det], [-d / det, a / det, (c * d - a * f) / det], [0, 0, 1]]
