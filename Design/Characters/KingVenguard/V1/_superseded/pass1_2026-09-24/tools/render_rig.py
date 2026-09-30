"""Render King Venguard V1 frames directly from RigReady/spine/King_Venguard.json + .atlas pages.

usage: render_rig.py <animation> <out_dir> [--fps 60] [--start 0] [--end <duration>] [--pre 0] [--post 0]
                     [--bg studio|magenta] [--scale 0.5] [--size 1080x1350] [--times t1,t2,...]
Frames are drawn with the minimal evaluator in spine_eval.py (not the official Spine runtime).
"""
import argparse, json, math, os, sys
from PIL import Image, ImageDraw, ImageFilter
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import spine_eval as se

V1 = os.path.abspath(os.path.join(HERE, "..", ".."))
SP = os.path.join(V1, "RigReady", "spine")

def load_atlas(path):
    regions, page, cur = {}, None, None
    lines = open(path).read().splitlines()
    i = 0
    while i < len(lines):
        ln = lines[i]
        if not ln.strip(): page = None; i += 1; continue
        if page is None:
            page = Image.open(os.path.join(os.path.dirname(path), ln.strip())).convert("RGBA"); i += 1
            while i < len(lines) and ":" in lines[i] and not lines[i].startswith(" "): i += 1
            continue
        if not ln.startswith(" "):
            name = ln.strip(); props = {}; i += 1
            while i < len(lines) and lines[i].startswith(" "):
                k, v = lines[i].strip().split(":", 1); props[k] = v.strip(); i += 1
            x, y = [int(v) for v in props["xy"].split(",")]; w, h = [int(v) for v in props["size"].split(",")]
            regions[name] = page.crop((x, y, x + w, y + h)).convert("RGBa")
            continue
        i += 1
    return regions

def background(size, kind):
    W, H = size
    if kind == "magenta": return Image.new("RGBA", size, (255, 0, 255, 255))
    bg = Image.new("RGBA", size)
    top, bot = (238, 241, 246), (214, 220, 232)
    d = ImageDraw.Draw(bg)
    for y in range(H):
        t = y / (H - 1); d.line([(0, y), (W, y)], fill=tuple(int(top[k] + (bot[k] - top[k]) * t) for k in range(3)) + (255,))
    return bg

class Renderer:
    def __init__(self, size=(1080, 1350), scale=0.5, ground=None, bg="studio"):
        self.data = json.load(open(os.path.join(SP, "King_Venguard.json")))
        self.sk = se.Skeleton(self.data)
        self.regions = load_atlas(os.path.join(SP, "King_Venguard.atlas"))
        self.size, self.scale = size, scale
        self.ox = size[0] / 2; self.oy = ground if ground is not None else size[1] - 62
        self.bg = background(size, bg)
        if bg == "studio":
            sh = Image.new("L", size, 0); ImageDraw.Draw(sh).ellipse([self.ox - 250 * scale * 2, self.oy - 18, self.ox + 250 * scale * 2, self.oy + 22], fill=70)
            sh = sh.filter(ImageFilter.GaussianBlur(10))
            self.bg = Image.composite(Image.new("RGBA", size, (90, 96, 112, 255)), self.bg, sh)
        self.C = [[scale, 0, self.ox], [0, -scale, self.oy], [0, 0, 1]]

    def _draw(self, target, bone_m, att):
        img = self.regions[att["_name"]]
        M = se.mat(self.C, se.region_matrix(bone_m, att))
        w, h = img.size
        pts = [(M[0][0] * u + M[0][1] * v + M[0][2], M[1][0] * u + M[1][1] * v + M[1][2]) for u, v in ((0, 0), (w, 0), (0, h), (w, h))]
        x0, y0 = max(0, int(math.floor(min(p[0] for p in pts))) - 1), max(0, int(math.floor(min(p[1] for p in pts))) - 1)
        x1, y1 = min(self.size[0], int(math.ceil(max(p[0] for p in pts))) + 1), min(self.size[1], int(math.ceil(max(p[1] for p in pts))) + 1)
        if x1 <= x0 or y1 <= y0: return
        Mi = se.inv(se.mat([[1, 0, x0], [0, 1, y0], [0, 0, 1]], [[1, 0, 0], [0, 1, 0], [0, 0, 1]]))
        Minv = se.inv(M)
        # output pixel (X', Y') inside the bbox -> canvas (X'+x0, Y'+y0) -> image
        a, b, c = Minv[0]; d, e, f = Minv[1]
        data = (a, b, a * x0 + b * y0 + c, d, e, d * x0 + e * y0 + f)
        out = img.transform((x1 - x0, y1 - y0), Image.AFFINE, data, resample=Image.BICUBIC).convert("RGBA")
        target.alpha_composite(out, dest=(x0, y0))

    def frame(self, anim, t, bg=None):
        world, active = self.sk.pose(anim, t)
        canvas = (bg or self.bg).copy()
        clip = None
        for s in self.data["slots"]:
            name = s["name"]; att_name = active.get(name)
            if att_name is None:
                if clip and clip["end"] == name: self._end_clip(canvas, clip); clip = None
                continue
            att = dict(self.sk.skin[name][att_name]); att["_name"] = att_name
            m = world[s["bone"]]
            if att.get("type") == "clipping":
                v = att["vertices"]; poly = []
                for k in range(0, len(v), 2):
                    wx, wy = se.apply(m, v[k], v[k + 1]); poly.append((self.C[0][0] * wx + self.C[0][2], self.C[1][1] * wy + self.C[1][2]))
                clip = {"end": att["end"], "poly": poly, "layer": Image.new("RGBA", self.size, (0, 0, 0, 0))}
                continue
            self._draw(clip["layer"] if clip else canvas, m, att)
            if clip and clip["end"] == name: self._end_clip(canvas, clip); clip = None
        return canvas

    def _end_clip(self, canvas, clip):
        ss = 4; W, H = self.size
        mask = Image.new("L", (W * ss, H * ss), 0); ImageDraw.Draw(mask).polygon([(x * ss, y * ss) for x, y in clip["poly"]], fill=255)
        mask = mask.resize((W, H), Image.LANCZOS)
        layer = clip["layer"]; r, g, b, a = layer.split()
        from PIL import ImageChops
        layer.putalpha(ImageChops.multiply(a, mask)); canvas.alpha_composite(layer)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("anim"); ap.add_argument("out")
    ap.add_argument("--fps", type=float, default=60); ap.add_argument("--start", type=float, default=0.0)
    ap.add_argument("--end", type=float, default=None); ap.add_argument("--loops", type=int, default=1)
    ap.add_argument("--pre", type=float, default=0.0); ap.add_argument("--post", type=float, default=0.0)
    ap.add_argument("--bg", default="studio"); ap.add_argument("--scale", type=float, default=0.5)
    ap.add_argument("--size", default="1080x1350"); ap.add_argument("--times", default=None)
    a = ap.parse_args()
    size = tuple(int(v) for v in a.size.split("x"))
    R = Renderer(size, a.scale, bg=a.bg)
    os.makedirs(a.out, exist_ok=True)
    anim = None if a.anim == "setup" else a.anim
    dur = a.end if a.end is not None else max([k["time"] for b in R.data["animations"][a.anim]["bones"].values() for ch in b.values() for k in ch] +
                                               [k["time"] for s in R.data["animations"][a.anim].get("slots", {}).values() for ch in s.values() for k in ch]) if anim else 0
    if a.times:
        times = [float(v) for v in a.times.split(",")]
    else:
        n = int(round(dur * a.fps)); body = [a.start + i / a.fps for i in range(n)] * a.loops
        times = [0.0] * int(round(a.pre * a.fps)) + body + [dur] * int(round(a.post * a.fps))
    for i, t in enumerate(times):
        R.frame(anim, t).convert("RGB").save(os.path.join(a.out, "%05d.png" % i))
    json.dump({"animation": a.anim, "fps": a.fps, "frames": len(times), "duration_s": dur, "loops": a.loops, "pre_s": a.pre, "post_s": a.post},
              open(os.path.join(a.out, "render.json"), "w"))
    print(a.anim, "frames", len(times), "duration", dur)

if __name__ == "__main__":
    main()
