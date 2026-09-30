"""Setup-pose fidelity and joint coverage, rendered from RigReady/spine at 1:1 source pixels with the custom preview
player (render_rig.py; not the official Spine runtime).

1. The setup pose must reproduce the master rig pose (Source/magnific/04-neutral-rig-pose-front.png) wherever the master
   is opaque: mean difference, pixels off by more than 40 and 80 levels (max over RGB), and the largest clusters.
2. Every range_test pose is scanned for enclosed holes (alpha < 230 not connected to the frame border).
Writes Previews/checks/setup-vs-master-difference-x3.png and Previews/checks/fidelity.json.
"""
import json, os, sys
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
from render_rig import Renderer
from render_sequence import interior_holes

V1 = os.path.abspath(os.path.join(HERE, "..", ".."))
meta = json.load(open(os.path.join(V1, "RigReady", "parts", "parts.json"))); W, H = meta["size"]
R = Renderer((W, H), 1.0, ground=meta["ground_y"])
R.ox = meta["centre_x"]; R.C = [[1.0, 0, R.ox], [0, -1.0, R.oy], [0, 0, 1]]      # world -> source pixels exactly
clear = Image.new("RGBA", (W, H), (0, 0, 0, 0))

src = np.asarray(Image.open(os.path.join(V1, "Source", "magnific", "04-neutral-rig-pose-front.png")).convert("RGB")).astype(np.float32)
A = np.asarray(Image.open(os.path.join(V1, "Source", "magnific", "cutouts", "04-neutral-rig-pose-front-alpha.png")).convert("RGBA"))[..., 3] / 255.0
lay = np.asarray(R.frame(None, 0, bg=clear)).astype(np.float32)
a = lay[..., 3:4] / 255.0
opaque = A > 0.9
err = np.abs(lay[..., :3] * a - src * a).max(-1) * opaque
clusters = interior_holes(np.where(err > 40, 0, 255).astype(np.uint8), 128)
report = {"setup_vs_master": {"mean": round(float(err[opaque].mean()), 3), "px_over_40": int((err > 40).sum()),
                              "px_over_80": int((err > 80).sum()), "opaque_px": int(opaque.sum()),
                              "largest_clusters_[px,x0,y0,x1,y1]": clusters[:8]}}
print("setup vs master (opaque master px): mean %.2f | >40: %d | >80: %d | largest clusters %s"
      % (report["setup_vs_master"]["mean"], report["setup_vs_master"]["px_over_40"], report["setup_vs_master"]["px_over_80"],
         [c[0] for c in clusters[:5]]))

steps = sorted({int(k["time"]) for b in R.data["animations"]["range_test"]["bones"].values() for ch in b.values() for k in ch})
report["range_test_holes"] = {}
for t in steps:
    hs = interior_holes(np.asarray(R.frame("range_test", float(t), bg=clear))[..., 3])
    report["range_test_holes"][str(t)] = {"holes": len(hs), "px": int(sum(h[0] for h in hs)), "largest": hs[0] if hs else None}
    print("range_test pose %d: %d enclosed holes, %d px, largest %s" % (t, len(hs), sum(h[0] for h in hs), hs[0] if hs else None))

out = os.path.join(V1, "Previews", "checks"); os.makedirs(out, exist_ok=True)
Image.fromarray(np.clip(err * 3, 0, 255).astype(np.uint8)).save(os.path.join(out, "setup-vs-master-difference-x3.png"))
json.dump(report, open(os.path.join(out, "fidelity.json"), "w"), indent=1)
