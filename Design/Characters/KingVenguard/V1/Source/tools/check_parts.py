"""Reassemble the setup-pose parts in draw order and compare them with the source rig pose."""
import json, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont
HERE = os.path.dirname(os.path.abspath(__file__)); V1 = os.path.abspath(os.path.join(HERE, "..", ".."))
P = os.path.join(V1, "RigReady", "parts"); meta = json.load(open(os.path.join(P, "parts.json")))
ORDER = ["pelvis","shin_R","shin_L","foot_R","foot_L","thigh_R","thigh_L","skirt","upperarm_R","upperarm_L","torso",
         "cape_R","cape_L","neck","collar","hand_R_open","forearm_R","hand_L_open","forearm_L","head","iris_R","iris_L",
         "chop_R","chop_L","mustache","brow_R","brow_L","crown"]
W, H = meta["size"]
src = Image.open(os.path.join(V1, "Source", "magnific", "04-neutral-rig-pose-front.png")).convert("RGB")
alpha = np.asarray(Image.open(os.path.join(V1, "Source", "magnific", "cutouts", "04-neutral-rig-pose-front-alpha.png")).convert("RGBA"))[..., 3] / 255
bg = (215, 215, 222)
canvas = Image.new("RGBA", (W, H), bg + (255,))
for n in ORDER:
    m = meta["parts"][n]; im = Image.open(os.path.join(P, n + ".png"))
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0)); layer.paste(im, (m["x"], m["y"])); canvas = Image.alpha_composite(canvas, layer)
ref = np.asarray(src, np.float32) * alpha[..., None] + np.array(bg, np.float32) * (1 - alpha[..., None])
got = np.asarray(canvas.convert("RGB"), np.float32)
d = np.abs(got - ref).max(-1)
inside = alpha > 0.5
print("reassembly vs source inside silhouette: mean %.2f  p99 %.1f  max %.0f  pixels>24: %d (%.3f%%)" % (d[inside].mean(), np.percentile(d[inside], 99), d[inside].max(), (d[inside] > 24).sum(), 100 * (d[inside] > 24).mean()))
from PIL import ImageFilter
cover = np.asarray(canvas)[..., 3] * 0  # placeholder to keep numpy import local
alpha_parts = np.zeros((H, W))
for n in ORDER:
    m = meta["parts"][n]; a = np.asarray(Image.open(os.path.join(P, n + ".png")))[..., 3] / 255.0
    sub = alpha_parts[m["y"]:m["y"] + m["h"], m["x"]:m["x"] + m["w"]]; sub[:] = a + sub * (1 - a)
far_out = np.asarray(Image.fromarray(((alpha < 0.02) * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(5))) > 127
print("setup pixels drawn more than 2 px outside the source silhouette: %d" % int(((alpha_parts > 0.5) & far_out).sum()))
print("source-opaque pixels not covered by the setup parts (<90%%): %d" % int(((alpha > 0.9) & (alpha_parts < 0.9)).sum()))
out = os.path.join(V1, "Previews", "checks"); os.makedirs(out, exist_ok=True)
canvas.convert("RGB").save(os.path.join(out, "reassembled-setup-pose.png"))
heat = np.clip(d * 4, 0, 255).astype(np.uint8)
Image.fromarray(heat).save(os.path.join(out, "reassembly-difference-x4.png"))
# exploded sheet on a checkerboard
names = list(meta["parts"].keys()); cols = 8; cell = 260; f = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 15)
rows = (len(names) + cols - 1) // cols
sheet = Image.new("RGB", (cols * cell, rows * (cell + 20)), (255, 255, 255))
chk = Image.new("RGB", (cell, cell)); dc = ImageDraw.Draw(chk)
for yy in range(0, cell, 16):
    for xx in range(0, cell, 16): dc.rectangle([xx, yy, xx + 15, yy + 15], fill=(200, 200, 200) if (xx + yy) // 16 % 2 else (240, 240, 240))
for i, n in enumerate(names):
    im = Image.open(os.path.join(P, n + ".png")); im.thumbnail((cell - 10, cell - 10))
    t = chk.copy().convert("RGBA"); t.alpha_composite(im, ((cell - im.width) // 2, (cell - im.height) // 2))
    x, y = (i % cols) * cell, (i // cols) * (cell + 20)
    sheet.paste(t.convert("RGB"), (x, y + 20)); ImageDraw.Draw(sheet).text((x + 4, y + 2), n, fill=(0, 0, 0), font=f)
sheet.save(os.path.join(out, "parts-exploded-sheet.png")); print("sheet", sheet.size)
