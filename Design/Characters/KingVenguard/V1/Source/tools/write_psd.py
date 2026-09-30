"""Write Source/KingVenguard_V1_RigParts.psd: every rig part as a named RGBA layer at its rig-pose position.

Layer order follows the Spine draw order (bottom to top). Replacement attachments (eyelids, mouths,
hand swaps) are included as hidden layers directly above their slot. Uncompressed PSD v1, 8-bit RGB.
Then reopens the file with Pillow and checks every layer's name, bounds and pixels.
"""
import json, os, struct, sys
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__)); V1 = os.path.abspath(os.path.join(HERE, "..", ".."))
P = os.path.join(V1, "RigReady", "parts"); meta = json.load(open(os.path.join(P, "parts.json")))
rig = json.load(open(os.path.join(V1, "RigReady", "spine", "King_Venguard.json")))
W, H = meta["size"]; J = meta["joints"]
OUT = os.path.join(V1, "Source", "KingVenguard_V1_RigParts.psd")

layers = []  # (name, rgba ndarray, left, top, visible)
skin = rig["skins"][0]["attachments"]
for s in rig["slots"]:
    atts = [a for a, v in skin.get(s["name"], {}).items() if v.get("type") != "clipping"]
    for a in atts:
        m = meta["parts"][a]; im = np.asarray(Image.open(os.path.join(P, a + ".png")).convert("RGBA"))
        if m.get("sheet"):
            side = "R" if a.startswith("hand_R") else "L"; wx, wy = J["wrist_" + side]; wu, wv = m["wrist_in_image"]
            left, top = int(round(wx - wu)), int(round(wy - wv))
        else:
            left, top = m["x"], m["y"]
        visible = (a == s.get("attachment"))
        if (im.shape[0] * im.shape[1]) % 2:   # odd channel length: some readers (Pillow) expect even-padded raw channels
            im = np.concatenate([im, np.zeros((im.shape[0], 1, 4), np.uint8)], 1)
        layers.append(("%s | %s" % (s["name"], a), im, left, top, visible))

def pascal(name):
    b = name.encode("ascii", "replace")[:255]; raw = bytes([len(b)]) + b
    return raw + b"\0" * ((4 - len(raw) % 4) % 4)

def luni(name):
    u = name.encode("utf-16-be"); body = struct.pack(">I", len(name)) + u
    if len(body) % 2: body += b"\0"
    return b"8BIM" + b"luni" + struct.pack(">I", len(body)) + body

records, chdata = b"", b""
composite = np.zeros((H, W, 4), np.float32)
for name, im, left, top, vis in layers:
    h, w = im.shape[:2]
    extra = struct.pack(">I", 0) + struct.pack(">I", 0) + pascal(name) + luni(name)
    rec = struct.pack(">iiii", top, left, top + h, left + w) + struct.pack(">H", 4)
    for cid in (0, 1, 2, -1): rec += struct.pack(">hI", cid, 2 + w * h)   # R, G, B, then transparency
    rec += b"8BIMnorm" + struct.pack(">BBBB", 255, 0, 0 if vis else 2, 0) + struct.pack(">I", len(extra)) + extra
    records += rec
    for ch in (0, 1, 2, 3): chdata += struct.pack(">H", 0) + im[..., ch].tobytes()
    if vis:
        y0, x0 = max(0, top), max(0, left); y1, x1 = min(H, top + h), min(W, left + w)
        sub = im[y0 - top:y1 - top, x0 - left:x1 - left].astype(np.float32) / 255
        dst = composite[y0:y1, x0:x1]; a = sub[..., 3:4]
        dst[..., :3] = sub[..., :3] * a + dst[..., :3] * (1 - a); dst[..., 3:4] = a + dst[..., 3:4] * (1 - a)
layer_info = struct.pack(">h", len(layers)) + records + chdata
if len(layer_info) % 2: layer_info += b"\0"
lmi = struct.pack(">I", len(layer_info)) + layer_info + struct.pack(">I", 0)
flat = composite[..., :3] * composite[..., 3:4] + (1 - composite[..., 3:4])      # composite preview flattened on white
flat = (flat * 255).round().clip(0, 255).astype(np.uint8)
with open(OUT, "wb") as f:
    f.write(b"8BPS" + struct.pack(">H", 1) + b"\0" * 6 + struct.pack(">HIIHH", 3, H, W, 8, 3))
    f.write(struct.pack(">I", 0)); f.write(struct.pack(">I", 0))
    f.write(struct.pack(">I", len(lmi)) + lmi)
    f.write(struct.pack(">H", 0)); [f.write(flat[..., c].tobytes()) for c in range(3)]

# ---- verification with an independent parser (Pillow's PSD plugin): names, bounds and channel data at the offsets it reports
ps = Image.open(OUT)
assert ps.size == (W, H), ps.size
assert [l[0] for l in ps.layers] == [l[0] for l in layers], "layer names differ"
bad = 0
raw = open(OUT, "rb").read()
BASE = 26 + 4 + 4 + 4 + 4   # Pillow reports channel offsets relative to the layer-info block (after header, colour, resources, LMI and layer-info lengths)
for (lname, mode, bbox, tiles), (name, im, left, top, vis) in zip(ps.layers, layers):
    if bbox != (left, top, left + im.shape[1], top + im.shape[0]): bad += 1; print("bbox mismatch", name, bbox)
    n = im.shape[0] * im.shape[1]
    for t in tiles:
        ch = {"R": 0, "G": 1, "B": 2, "A": 3}[t.args]
        data = np.frombuffer(raw, np.uint8, n, BASE + t.offset)
        if not np.array_equal(data, im[..., ch].reshape(-1)): bad += 1; print("pixel mismatch", name, t.args)
print("PSD", os.path.relpath(OUT, V1), os.path.getsize(OUT) // 1024, "KB,", len(layers), "layers,", sum(1 for l in layers if l[4]), "visible; Pillow verification errors:", bad)
