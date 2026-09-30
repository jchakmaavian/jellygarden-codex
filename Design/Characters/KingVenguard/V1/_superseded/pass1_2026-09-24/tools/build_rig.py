"""Build the King Venguard V1 Spine 3.8 skeleton (JSON + atlas) from RigReady/parts.

Writes RigReady/spine/King_Venguard.json, King_Venguard.atlas, King_Venguard*.png and images/.
Animations (reference timing notes in ReferenceNotes.md):
  idle             4.0 s loop  - breathing, weight shift, lagged head/whiskers/cape, one blink, one glance
  happy_thumbs_up  3.8 s       - laugh bounce, then right thumbs-up swapped in at 1.9 s, hold, settle
  range_test       6 stepped poses used only for the joint-coverage check (not an approval animation)
"""
import json, math, os, shutil, sys
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import spine_eval as se

V1 = os.path.abspath(os.path.join(HERE, "..", ".."))
PARTS_DIR = os.path.join(V1, "RigReady", "parts")
OUT = os.path.join(V1, "RigReady", "spine")
meta = json.load(open(os.path.join(PARTS_DIR, "parts.json")))
J = {k: tuple(v) for k, v in meta["joints"].items()}
CX, GROUND = meta["centre_x"], meta["ground_y"]

def W(p): return (p[0] - CX, GROUND - p[1])                    # image px -> Spine world (y up)
def ang(a, b): A, B = W(a), W(b); return math.degrees(math.atan2(B[1] - A[1], B[0] - A[0]))
def dist(a, b): return math.hypot(a[0] - b[0], a[1] - b[1])

# ------------------------------------------------------------------ bones (world setup pose)
BONES = [  # name, parent, world position (image px), world rotation, length
    ("root", None, (CX, GROUND), 0, 0),
    ("hips", "root", J["hips"], 0, 0),
    ("skirt", "hips", J["skirt"], 0, 0),
    ("thigh_R", "hips", J["hip_R"], ang(J["hip_R"], J["knee_R"]), dist(J["hip_R"], J["knee_R"])),
    ("thigh_L", "hips", J["hip_L"], ang(J["hip_L"], J["knee_L"]), dist(J["hip_L"], J["knee_L"])),
    ("shin_R", "root", J["knee_R"], ang(J["knee_R"], J["ankle_R"]), dist(J["knee_R"], J["ankle_R"])),
    ("shin_L", "root", J["knee_L"], ang(J["knee_L"], J["ankle_L"]), dist(J["knee_L"], J["ankle_L"])),
    ("foot_R", "root", J["ankle_R"], 0, 0),
    ("foot_L", "root", J["ankle_L"], 0, 0),
    ("torso", "hips", J["torso"], 0, 0),
    ("belly", "torso", J["torso"], 0, 0),
    ("chest", "torso", J["chest"], 0, 0),
    ("cape_R", "chest", J["cape_R"], 0, 0),
    ("cape_L", "chest", J["cape_L"], 0, 0),
    ("upperarm_R", "chest", J["shoulder_R"], ang(J["shoulder_R"], J["elbow_R"]), dist(J["shoulder_R"], J["elbow_R"])),
    ("forearm_R", "upperarm_R", J["elbow_R"], ang(J["elbow_R"], J["wrist_R"]), dist(J["elbow_R"], J["wrist_R"])),
    ("hand_R", "forearm_R", J["wrist_R"], ang(J["elbow_R"], J["wrist_R"]), 90),
    ("upperarm_L", "chest", J["shoulder_L"], ang(J["shoulder_L"], J["elbow_L"]), dist(J["shoulder_L"], J["elbow_L"])),
    ("forearm_L", "upperarm_L", J["elbow_L"], ang(J["elbow_L"], J["wrist_L"]), dist(J["elbow_L"], J["wrist_L"])),
    ("hand_L", "forearm_L", J["wrist_L"], ang(J["elbow_L"], J["wrist_L"]), 90),
    ("neck", "chest", J["neck"], 0, 0),
    ("head", "neck", J["head"], 0, 0),
    ("crown", "head", J["crown"], 0, 0),
    ("gaze", "head", J["gaze"], 0, 0),
    ("iris_R", "gaze", J["iris_R"], 0, 0),
    ("iris_L", "gaze", J["iris_L"], 0, 0),
    ("brow_R", "head", J["brow_R"], 0, 0),
    ("brow_L", "head", J["brow_L"], 0, 0),
    ("mustache", "head", J["mustache"], 0, 0),
    ("chop_R", "head", J["chop_R"], 0, 0),
    ("chop_L", "head", J["chop_L"], 0, 0),
    ("mouth", "head", J["mouth"], 0, 0),
]
WORLD = {n: (W(p), r) for n, par, p, r, l in BONES}

def local(name, parent):
    (px, py), pr = WORLD[parent]; (x, y), r = WORLD[name]
    c, s = math.cos(math.radians(-pr)), math.sin(math.radians(-pr))
    dx, dy = x - px, y - py
    return c * dx - s * dy, s * dx + c * dy, r - pr

bones_json = []
for n, par, p, r, l in BONES:
    b = {"name": n}
    if par is None:
        b.update(x=0, y=0)
    else:
        lx, ly, lr = local(n, par); b["parent"] = par; b["x"] = round(lx, 2); b["y"] = round(ly, 2)
        if abs(lr) > 1e-4: b["rotation"] = round(lr, 3)
    if l: b["length"] = round(l, 2)
    bones_json.append(b)

# ------------------------------------------------------------------ slots in draw order (back to front)
SLOTS = [("pelvis", "hips", "pelvis"), ("shin_R", "shin_R", "shin_R"), ("shin_L", "shin_L", "shin_L"),
         ("foot_R", "foot_R", "foot_R"), ("foot_L", "foot_L", "foot_L"), ("thigh_R", "thigh_R", "thigh_R"),
         ("thigh_L", "thigh_L", "thigh_L"), ("skirt", "skirt", "skirt"), ("upperarm_R", "upperarm_R", "upperarm_R"),
         ("upperarm_L", "upperarm_L", "upperarm_L"), ("torso", "belly", "torso"), ("cape_R", "cape_R", "cape_R"),
         ("cape_L", "cape_L", "cape_L"), ("neck", "neck", "neck"), ("collar", "chest", "collar"),
         ("hand_R", "hand_R", "hand_R_open"), ("forearm_R", "forearm_R", "forearm_R"),
         ("hand_L", "hand_L", "hand_L_open"), ("forearm_L", "forearm_L", "forearm_L"), ("head", "head", "head"),
         ("eyeclip_R", "head", "eyeclip_R"), ("iris_R", "iris_R", "iris_R"), ("eyeclip_L", "head", "eyeclip_L"),
         ("iris_L", "iris_L", "iris_L"), ("eyelid_R", "head", None), ("eyelid_L", "head", None), ("mouth", "mouth", None),
         ("chop_R", "chop_R", "chop_R"), ("chop_L", "chop_L", "chop_L"), ("mustache", "mustache", "mustache"),
         ("brow_R", "brow_R", "brow_R"), ("brow_L", "brow_L", "brow_L"), ("crown", "crown", "crown")]
slots_json = []
for n, bone, att in SLOTS:
    s = {"name": n, "bone": bone}
    if att: s["attachment"] = att
    slots_json.append(s)

SLOT_ATTS = {  # slot -> attachment names
    "eyelid_R": ["eyes_closed_R", "eyes_happy_R"], "eyelid_L": ["eyes_closed_L", "eyes_happy_L"],
    "mouth": ["mouth_neutral", "mouth_laugh", "mouth_worried", "mouth_sad"],
    "hand_R": ["hand_R_open", "hand_R_thumbsup", "hand_R_fist", "hand_R_palm"], "hand_L": ["hand_L_open", "hand_L_fist"],
}
bone_of_slot = {n: b for n, b, a in SLOTS}

def region_for(part, bone):
    p = meta["parts"][part]
    (bx, by), br = WORLD[bone]
    c, s = math.cos(math.radians(-br)), math.sin(math.radians(-br))
    if p.get("sheet"):
        wu, wv = p["wrist_in_image"]; rr = p["rotation"]
        du, dv = p["w"] / 2 - wu, -(p["h"] / 2 - wv)
        cr, sr = math.cos(math.radians(rr)), math.sin(math.radians(rr))
        lx, ly = -10 + cr * du - sr * dv, sr * du + cr * dv
        return {"x": round(lx, 2), "y": round(ly, 2), "rotation": rr, "width": p["w"], "height": p["h"]}
    cx, cy = W((p["x"] + p["w"] / 2, p["y"] + p["h"] / 2))
    dx, dy = cx - bx, cy - by
    return {"x": round(c * dx - s * dy, 2), "y": round(s * dx + c * dy, 2), "rotation": round(-br, 3), "width": p["w"], "height": p["h"]}

skin = {}
for n, bone, att in SLOTS:
    names = SLOT_ATTS.get(n, [att] if att else [])
    if n.startswith("eyeclip_"):
        side = n[-1]; (hx, hy), hr = WORLD["head"]; verts = []
        for x, y in meta["eye_clip"][side]:
            wx, wy = W((x, y)); verts += [round(wx - hx, 2), round(wy - hy, 2)]
        skin[n] = {n: {"type": "clipping", "end": "iris_" + side, "vertexCount": len(verts) // 2, "vertices": verts}}
        continue
    skin[n] = {a: region_for(a, bone) for a in names}

# ------------------------------------------------------------------ animation authoring helpers
EASE = dict(curve=0.25, c2=0, c3=0.75, c4=1)
def eased(keys, field_names):
    """keys: list of (time, values...) with ease-in-out between them."""
    out = []
    for i, k in enumerate(keys):
        d = {"time": round(k[0], 4)}
        for f, v in zip(field_names, k[1:]): d[f] = round(float(v), 4)
        if i < len(keys) - 1: d.update(EASE)
        out.append(d)
    return out

def hermite(f, df, t0, t1, step, field):
    ts = list(np.arange(t0, t1 + 1e-9, step)); out = []
    for i, t in enumerate(ts):
        d = {"time": round(float(t), 4), field: round(float(f(t)), 4)}
        if i < len(ts) - 1:
            ta, tb = t, ts[i + 1]; va, vb = f(ta), f(tb); dt = tb - ta
            if abs(vb - va) > 1e-6:
                d.update(curve=round(1 / 3, 4), c2=round(float(df(ta) * dt / 3 / (vb - va)), 4),
                         c3=round(2 / 3, 4), c4=round(float(1 - df(tb) * dt / 3 / (vb - va)), 4))
        out.append(d)
    return out

def hermite_scale(fx, fy, df_shape, t0, t1, step):
    """Scale keys whose x and y share the same normalised shape (proportional offsets)."""
    ts = list(np.arange(t0, t1 + 1e-9, step)); out = []
    for i, t in enumerate(ts):
        d = {"time": round(float(t), 4), "x": round(float(fx(t)), 5), "y": round(float(fy(t)), 5)}
        if i < len(ts) - 1:
            ta, tb = t, ts[i + 1]; va, vb = fy(ta), fy(tb); dt = tb - ta
            if abs(vb - va) > 1e-7:
                d.update(curve=round(1 / 3, 4), c2=round(float(df_shape(ta) * dt / 3 / (vb - va)), 4),
                         c3=round(2 / 3, 4), c4=round(float(1 - df_shape(tb) * dt / 3 / (vb - va)), 4))
        out.append(d)
    return out

def dense(fn, t0, t1, step, fields):
    out = []
    for t in np.arange(t0, t1 + 1e-9, step):
        v = fn(t); d = {"time": round(float(t), 4)}
        for f, x in zip(fields, v): d[f] = round(float(x), 4)
        out.append(d)
    return out

def thigh_keys(hip_offset, t0, t1, step, side):
    """Rotate/scale the thigh so its knee end stays fixed while the hips move (front-view foreshortening)."""
    (hx, hy), hr = WORLD["thigh_" + side]; (kx, ky), _ = WORLD["shin_" + side]
    v0 = (kx - hx, ky - hy); a0 = math.degrees(math.atan2(v0[1], v0[0])); l0 = math.hypot(*v0)
    rot, sca = [], []
    for t in np.arange(t0, t1 + 1e-9, step):
        dx, dy = hip_offset(t); v = (kx - hx - dx, ky - hy - dy)
        rot.append({"time": round(float(t), 4), "angle": round(math.degrees(math.atan2(v[1], v[0])) - a0, 4)})
        sca.append({"time": round(float(t), 4), "x": round(math.hypot(*v) / l0, 5), "y": 1})
    return rot, sca

def keyed_value(keys, t):
    """Evaluate an eased key list [(t, v)] (used to drive secondary springs)."""
    ks = eased(keys, ["v"]); return se.sample(ks, t, ("v",), (0.0,))[0]

def spring(drive, dt, w0, zeta):
    x = v = 0.0; out = []
    for a in drive:
        acc = -w0 * w0 * x - 2 * zeta * w0 * v - a
        v += acc * dt; x += v * dt; out.append(x)
    return np.array(out)

def normalise(arr, peak):
    m = np.abs(arr).max(); return arr * (peak / m) if m > 1e-9 else arr

# ------------------------------------------------------------------ idle (4.0 s loop)
T = 4.0; w = 2 * math.pi / T
S = lambda amp, lag: (lambda t: amp * math.sin(w * (t - lag)))
dS = lambda amp, lag: (lambda t: amp * w * math.cos(w * (t - lag)))
breath = lambda t: 0.5 * (1 - math.cos(w * t)); dbreath = lambda t: 0.5 * w * math.sin(w * t)
hip_idle = lambda t: (6.0 * math.sin(w * t), -2.4 * math.sin(w * t) ** 2)
idle = {"bones": {}, "slots": {}}
B = idle["bones"]
B["hips"] = {"translate": dense(hip_idle, 0, T, 0.1, ("x", "y"))}
for side in ("R", "L"):
    r_, s_ = thigh_keys(hip_idle, 0, T, 0.1, side); B["thigh_" + side] = {"rotate": r_, "scale": s_}
B["torso"] = {"rotate": hermite(S(0.9, 0.12), dS(0.9, 0.12), 0, T, 0.5, "angle")}
B["belly"] = {"scale": hermite_scale(lambda t: 1 + 0.010 * breath(t), lambda t: 1 + 0.014 * breath(t), lambda t: 0.014 * dbreath(t), 0, T, 0.5)}
B["chest"] = {"translate": [dict(k, x=0) for k in hermite(lambda t: 3.5 * breath(t - 0.06), lambda t: 3.5 * dbreath(t - 0.06), 0, T, 0.5, "y")]}
for bone, amp, lag in (("neck", 0.5, 0.25), ("head", 1.1, 0.33), ("crown", 0.9, 0.55), ("cape_R", 1.4, 0.45), ("cape_L", 1.4, 0.45),
                       ("skirt", 0.9, 0.30), ("upperarm_R", 1.6, 0.38), ("forearm_R", 1.2, 0.52), ("hand_R", 1.5, 0.66),
                       ("upperarm_L", 1.6, 0.38), ("forearm_L", 1.2, 0.52), ("hand_L", 1.5, 0.66), ("chop_R", 1.2, 0.50),
                       ("chop_L", 1.2, 0.50), ("mustache", 0.7, 0.45)):
    B.setdefault(bone, {})["rotate"] = hermite(S(amp, lag), dS(amp, lag), 0, T, 0.5, "angle")
B["gaze"] = {"translate": eased([(0, 0, 0), (2.1, 0, 0), (2.28, 9, 0), (3.15, 9, 0), (3.35, 0, 0), (T, 0, 0)], ("x", "y"))}
for side in ("R", "L"):
    B["brow_" + side] = {"translate": eased([(0, 0, 0), (2.05, 0, 0), (2.25, 0, 5), (3.2, 0, 5), (3.45, 0, 0), (T, 0, 0)], ("x", "y"))}
    idle["slots"]["eyelid_" + side] = {"attachment": [{"time": 0, "name": None}, {"time": 1.55, "name": "eyes_closed_" + side}, {"time": 1.69, "name": None}]}

# ------------------------------------------------------------------ happy_thumbs_up (3.8 s)
D = 3.8
H = {"bones": {}, "slots": {}}; HB = H["bones"]
hip_keys = [(0, 0, 0), (0.22, 0, -10), (0.48, 0, 7), (0.72, 0, -4), (0.92, 0, 3), (1.12, 0, -1.5), (1.35, 0, 0), (1.7, 3, 0),
            (2.15, 3, -3), (2.35, 3, 0), (3.2, 3, 0), (3.55, 0, -3), (D, 0, 0)]
HB["hips"] = {"translate": eased(hip_keys, ("x", "y"))}
hip_x = eased([(k[0], k[1]) for k in hip_keys], ["v"]); hip_y = eased([(k[0], k[2]) for k in hip_keys], ["v"])
hip_fn = lambda t: (se.sample(hip_x, t, ("v",), (0.0,))[0], se.sample(hip_y, t, ("v",), (0.0,))[0])
for side in ("R", "L"):
    r_, s_ = thigh_keys(hip_fn, 0, D, 0.05, side); HB["thigh_" + side] = {"rotate": r_, "scale": s_}
HB["belly"] = {"scale": eased([(0, 1, 1), (0.22, 1.015, 0.982), (0.48, 0.992, 1.028), (0.72, 1.012, 0.99), (0.92, 0.996, 1.012),
                               (1.12, 1.004, 0.996), (1.35, 1, 1), (2.15, 1.006, 0.994), (2.35, 1, 1), (3.55, 1.008, 0.99), (D, 1, 1)], ("x", "y"))}
chest_y = [(0, 0), (0.22, -4), (0.48, 8), (0.72, -2), (0.92, 4), (1.12, 0), (1.35, 1), (1.8, 3), (3.2, 3), (3.6, 0), (D, 0)]
HB["chest"] = {"translate": eased([(t, 0, v) for t, v in chest_y], ("x", "y"))}
HB["torso"] = {"rotate": eased([(0, 0), (1.5, 0), (1.9, -1.6), (3.2, -1.6), (3.6, 0.3), (D, 0)], ("angle",))}
HB["neck"] = {"rotate": eased([(0, 0), (0.22, -1.5), (0.5, 2.5), (0.95, 1.5), (1.4, 0.5), (2.2, -2), (2.45, 0.5), (2.7, 0), (3.6, 0), (D, 0)], ("angle",))}
head_rot = [(0, 0), (0.26, -2.5), (0.55, 4), (0.78, 1.5), (0.98, 3), (1.2, 1), (1.45, 1.5), (1.9, 2.5), (2.25, -1.5), (2.5, 2), (3.2, 2), (3.65, -0.5), (D, 0)]
HB["head"] = {"rotate": eased(head_rot, ("angle",)), "translate": eased([(0, 0, 0), (0.26, 0, -4), (0.55, 0, 5), (0.8, 0, 0), (D, 0, 0)], ("x", "y"))}
for side in ("R", "L"):
    HB["brow_" + side] = {"translate": eased([(0, 0, 0), (0.3, 0, 12), (1.3, 0, 10), (1.6, 0, 4), (2.0, 0, 9), (2.7, 0, 5), (3.6, 0, 0), (D, 0, 0)], ("x", "y"))}
    H["slots"]["eyelid_" + side] = {"attachment": [{"time": 0, "name": None}, {"time": 0.18, "name": "eyes_happy_" + side}, {"time": 1.5, "name": None},
                                                   {"time": 2.78, "name": "eyes_closed_" + side}, {"time": 2.9, "name": None}]}
H["slots"]["mouth"] = {"attachment": [{"time": 0, "name": None}, {"time": 0.2, "name": "mouth_laugh"}, {"time": 1.48, "name": None},
                                      {"time": 1.95, "name": "mouth_laugh"}, {"time": 2.6, "name": None}]}
H["slots"]["hand_R"] = {"attachment": [{"time": 0, "name": "hand_R_open"}, {"time": 1.72, "name": "hand_R_fist"}, {"time": 1.9, "name": "hand_R_thumbsup"},
                                       {"time": 3.22, "name": "hand_R_fist"}, {"time": 3.38, "name": "hand_R_open"}]}
HB["gaze"] = {"translate": eased([(0, 0, 0), (1.55, 0, 0), (1.85, -8, -4), (2.4, -8, -4), (2.6, 0, 0), (D, 0, 0)], ("x", "y"))}
HB["upperarm_R"] = {"rotate": eased([(0, 0), (1.45, 0), (1.82, 23), (2.0, 20), (2.12, 21.5), (2.32, 20), (3.12, 20), (3.55, -2), (D, 0)], ("angle",))}
HB["forearm_R"] = {"rotate": eased([(0, 0), (1.45, 0), (1.8, 128), (1.97, 118), (2.12, 124), (2.32, 118), (3.1, 118), (3.5, -4), (D, 0)], ("angle",))}
HB["hand_R"] = {"rotate": eased([(0, 0), (1.45, 0), (1.8, -18), (1.97, -12), (2.12, -16), (2.32, -12), (3.1, -12), (3.5, 0), (D, 0)], ("angle",))}

# secondary motion: damped springs driven by the body's vertical acceleration and head rotation, baked every 1/20 s
dt = 1 / 240.0; ts = np.arange(0, D + 1e-9, dt)
yv = np.array([hip_fn(t)[1] + keyed_value(chest_y, t) for t in ts])
av = np.gradient(np.gradient(yv, dt), dt)
hr = np.array([keyed_value(head_rot, t) for t in ts]); ha = np.gradient(np.gradient(hr, dt), dt)
xs = np.array([hip_fn(t)[0] for t in ts]); ax = np.gradient(np.gradient(xs, dt), dt)
def bake(bone, channel, arr, peak_to, fields=("angle",), xy=None, stepn=12):
    vals = normalise(arr, peak_to); keys = []
    for i in range(0, len(ts), stepn):
        t = float(ts[i]); v = float(vals[i])
        if channel == "rotate": keys.append({"time": round(t, 4), "angle": round(v, 4)})
        else: keys.append({"time": round(t, 4), "x": round(v * xy[0], 4), "y": round(v * xy[1], 4)})
    keys[-1] = dict(keys[-1], **({"angle": 0} if channel == "rotate" else {"x": 0, "y": 0}))
    HB.setdefault(bone, {})[channel] = keys
bake("mustache", "translate", spring(av, dt, 2 * math.pi * 3.2, 0.35), 3.0, xy=(0, 1))
bake("mustache", "rotate", spring(ha, dt, 2 * math.pi * 3.0, 0.4), 1.5)
bake("chop_R", "rotate", spring(av, dt, 2 * math.pi * 2.6, 0.4), 3.5)
bake("chop_L", "rotate", -spring(av, dt, 2 * math.pi * 2.6, 0.4), 3.5)
bake("crown", "rotate", spring(ha, dt, 2 * math.pi * 2.2, 0.45), 2.2)
bake("crown", "translate", spring(av, dt, 2 * math.pi * 2.8, 0.45), 3.0, xy=(0, 1))
bake("cape_R", "rotate", spring(av, dt, 2 * math.pi * 1.8, 0.4), 2.5)
bake("cape_L", "rotate", -spring(av, dt, 2 * math.pi * 1.8, 0.4), 2.5)
bake("skirt", "rotate", spring(ax + 0.3 * av, dt, 2 * math.pi * 1.6, 0.45), 1.2)
bake("upperarm_L", "rotate", spring(av + ax, dt, 2 * math.pi * 1.4, 0.35), 4.0)
bake("forearm_L", "rotate", spring(av + ax, dt, 2 * math.pi * 1.7, 0.35), 3.0)
bake("hand_L", "rotate", spring(av + ax, dt, 2 * math.pi * 2.2, 0.35), 3.0)

# ------------------------------------------------------------------ range_test (stepped poses, coverage check only)
ST = {"curve": "stepped"}
def steps(vals, fields):
    return [dict({"time": float(i)}, **{f: v for f, v in zip(fields, val)}, **ST) for i, val in enumerate(vals)]
R = {"bones": {}, "slots": {}}; RB = R["bones"]
RB["upperarm_R"] = {"rotate": steps([(0,), (20,), (0,), (0,), (-25,), (0,)], ("angle",))}
RB["forearm_R"] = {"rotate": steps([(0,), (118,), (0,), (0,), (45,), (0,)], ("angle",))}
RB["hand_R"] = {"rotate": steps([(0,), (-12,), (0,), (0,), (0,), (0,)], ("angle",))}
RB["upperarm_L"] = {"rotate": steps([(0,), (-28,), (0,), (0,), (25,), (0,)], ("angle",))}
RB["forearm_L"] = {"rotate": steps([(0,), (-70,), (0,), (0,), (-45,), (0,)], ("angle",))}
RB["head"] = {"rotate": steps([(0,), (0,), (8,), (-8,), (0,), (0,)], ("angle",))}
RB["crown"] = {"rotate": steps([(0,), (0,), (3,), (-3,), (0,), (0,)], ("angle",)), "translate": steps([(0, 0), (0, 0), (0, 6), (0, -3), (0, 0), (0, 0)], ("x", "y"))}
RB["gaze"] = {"translate": steps([(0, 0), (0, 0), (10, 2), (-10, -5), (0, 0), (0, 0)], ("x", "y"))}
for side, sg in (("R", 1), ("L", -1)):
    RB["brow_" + side] = {"translate": steps([(0, 0), (0, 0), (0, 14), (0, -6), (0, 0), (0, 0)], ("x", "y"))}
    RB["chop_" + side] = {"rotate": steps([(0,), (0,), (6 * sg,), (-6 * sg,), (0,), (0,)], ("angle",))}
    RB["cape_" + side] = {"rotate": steps([(0,), (0,), (5 * sg,), (-5 * sg,), (0,), (3 * sg,)], ("angle",))}
    R["slots"]["eyelid_" + side] = {"attachment": [{"time": 0, "name": None}, {"time": 2, "name": "eyes_happy_" + side}, {"time": 3, "name": "eyes_closed_" + side}, {"time": 4, "name": None}]}
RB["mustache"] = {"translate": steps([(0, 0), (0, 0), (0, -4), (0, 4), (0, 0), (0, 0)], ("x", "y"))}
RB["skirt"] = {"rotate": steps([(0,), (0,), (0,), (3,), (0,), (-3,)], ("angle",))}
RB["torso"] = {"rotate": steps([(0,), (0,), (0,), (0,), (0,), (-3,)], ("angle",))}
rt_hips = [(0, 0), (0, 0), (0, 0), (0, 0), (0, -16), (8, 0)]
RB["hips"] = {"translate": steps(rt_hips, ("x", "y"))}
for side in ("R", "L"):
    rr, ss = [], []
    for i, off in enumerate(rt_hips):
        r_, s_ = thigh_keys(lambda t, o=off: o, 0, 0, 1, side); rr.append(dict(r_[0], time=float(i), **ST)); ss.append(dict(s_[0], time=float(i), **ST))
    RB["thigh_" + side] = {"rotate": rr, "scale": ss}
RB["belly"] = {"scale": steps([(1, 1), (1, 1), (1, 1), (1, 1), (1.03, 0.97), (1, 1)], ("x", "y"))}
R["slots"]["mouth"] = {"attachment": [{"time": 0, "name": None}, {"time": 2, "name": "mouth_laugh"}, {"time": 3, "name": "mouth_sad"}, {"time": 4, "name": "mouth_worried"}, {"time": 5, "name": None}]}
R["slots"]["hand_R"] = {"attachment": [{"time": 0, "name": "hand_R_open"}, {"time": 1, "name": "hand_R_thumbsup"}, {"time": 2, "name": "hand_R_open"}, {"time": 5, "name": "hand_R_fist"}]}
R["slots"]["hand_L"] = {"attachment": [{"time": 0, "name": "hand_L_open"}, {"time": 5, "name": "hand_L_fist"}]}

# ------------------------------------------------------------------ atlas packing (shelf packer, 2048 pages, 2 px padding)
names = [a for sl in skin.values() for a, v in sl.items() if v.get("type") != "clipping"]
names = sorted(set(names), key=lambda n: -Image.open(os.path.join(PARTS_DIR, n + ".png")).height)
PAGE = 2048; pad = 2; pages = []; place = {}
cur = None
for n in names:
    im = Image.open(os.path.join(PARTS_DIR, n + ".png")); w_, h_ = im.size
    placed = False
    for pg in pages:
        for shelf in pg["shelves"]:
            if h_ <= shelf["h"] and shelf["x"] + w_ + pad <= PAGE:
                place[n] = (pg["i"], shelf["x"], shelf["y"]); shelf["x"] += w_ + pad; placed = True; break
        if placed: break
        if pg["y"] + h_ + pad <= PAGE:
            pg["shelves"].append({"x": w_ + pad, "y": pg["y"], "h": h_}); place[n] = (pg["i"], 0, pg["y"]); pg["y"] += h_ + pad; placed = True; break
    if not placed:
        pg = {"i": len(pages), "shelves": [{"x": w_ + pad, "y": 0, "h": h_}], "y": h_ + pad}; pages.append(pg); place[n] = (pg["i"], 0, 0)
if os.path.isdir(OUT): shutil.rmtree(OUT)
os.makedirs(os.path.join(OUT, "images"))
atlas_lines = []
for pg in pages:
    fname = "King_Venguard%s.png" % ("" if pg["i"] == 0 else str(pg["i"] + 1))
    page_im = Image.new("RGBA", (PAGE, PAGE), (0, 0, 0, 0))
    atlas_lines += ([""] if atlas_lines else []) + [fname, "size: %d,%d" % (PAGE, PAGE), "format: RGBA8888", "filter: Linear,Linear", "repeat: none"]
    for n, (i, x, y) in place.items():
        if i != pg["i"]: continue
        im = Image.open(os.path.join(PARTS_DIR, n + ".png")); page_im.paste(im, (x, y))
        atlas_lines += [n, "  rotate: false", "  xy: %d, %d" % (x, y), "  size: %d, %d" % im.size, "  orig: %d, %d" % im.size, "  offset: 0, 0", "  index: -1"]
    page_im.save(os.path.join(OUT, fname), optimize=True)
open(os.path.join(OUT, "King_Venguard.atlas"), "w", newline="\n").write("\n".join(atlas_lines) + "\n")
for n in names: shutil.copy(os.path.join(PARTS_DIR, n + ".png"), os.path.join(OUT, "images", n + ".png"))

skel = {"skeleton": {"hash": "", "spine": "3.8.99", "x": -848, "y": 0, "width": 1696, "height": 2528, "images": "./images/", "audio": ""},
        "bones": bones_json, "slots": slots_json, "skins": [{"name": "default", "attachments": skin}],
        "animations": {"idle": idle, "happy_thumbs_up": H, "range_test": R}}
json.dump(skel, open(os.path.join(OUT, "King_Venguard.json"), "w"), indent=1)
print("bones", len(bones_json), "slots", len(slots_json), "attachments", len(names), "atlas pages", len(pages))
