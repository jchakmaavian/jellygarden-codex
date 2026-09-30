"""Render a sequence of animation segments with the existing custom player (render_rig.Renderer, unchanged),
burn in the preview label and a timecode, write PNG frames, and report enclosed holes in every frame.

usage: render_sequence.py <out_dir> <spec> [--fps 60]
  spec: comma-separated segments "animation:start-end", e.g. "idle:3.0-4.0,happy_thumbs_up:0-3.8,idle:0-1.2"
Each frame is first rendered on a transparent canvas. Pixels with alpha < 230 that are enclosed by the character
(not connected to the frame border) are counted as holes; the frame is then composited onto the studio background.
"""
import argparse, json, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
from render_rig import Renderer

LABEL = "Custom-player preview \u2014 official Spine compatibility untested."


def interior_holes(alpha, thresh=230, max_px=None):
    """Connected components (4-connected) of low-alpha pixels that do not touch the border. Scanline union-find.
    Returns [count, x0, y0, x1, y1] per component, largest first; with max_px, also a mask of the pixels of every
    component of at most max_px pixels."""
    empty = alpha < thresh
    Hh, Ww = empty.shape
    parent = []

    def find(a):
        root = a
        while parent[root] != root: root = parent[root]
        while parent[a] != root: parent[a], a = root, parent[a]
        return root

    runs, prev = [], []
    for y in range(Hh):
        d = np.diff(np.concatenate(([0], empty[y].astype(np.int8), [0])))
        starts, ends = np.flatnonzero(d == 1).tolist(), np.flatnonzero(d == -1).tolist()
        cur, j = [], 0
        for s, e in zip(starts, ends):
            rid = len(parent); parent.append(rid)
            while j < len(prev) and prev[j][1] <= s: j += 1
            k = j
            while k < len(prev) and prev[k][0] < e:
                ra, rb = find(rid), find(prev[k][2])
                if ra != rb: parent[ra] = rb
                k += 1
            cur.append((s, e, rid)); runs.append((y, s, e, rid))
        prev = cur
    border = {find(rid) for (y, s, e, rid) in runs if y == 0 or y == Hh - 1 or s == 0 or e == Ww}
    comps = {}
    for (y, s, e, rid) in runs:
        r = find(rid)
        if r in border: continue
        c = comps.setdefault(r, [0, Ww, Hh, 0, 0])
        c[0] += e - s; c[1] = min(c[1], s); c[2] = min(c[2], y); c[3] = max(c[3], e); c[4] = max(c[4], y + 1)
    ranked = sorted(comps.values(), key=lambda c: -c[0])
    if max_px is None: return ranked
    mask = np.zeros((Hh, Ww), bool)
    for (y, s, e, rid) in runs:
        r = find(rid)
        if r in comps and comps[r][0] <= max_px: mask[y, s:e] = True
    return ranked, mask


def hairline_px(alpha):
    """Pixels a 1 px morphological closing would fill: cracks and slits up to ~2 px wide, including ones that open to
    the silhouette edge (which interior_holes cannot see). Natural concave corners contribute a small baseline."""
    from PIL import ImageFilter
    op = Image.fromarray(((alpha > 200) * 255).astype(np.uint8))
    closed = np.asarray(op.filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.MinFilter(3))) > 127
    return closed & ~(np.asarray(op) > 127)


def burn_in(frame, text_bottom):
    d = ImageDraw.Draw(frame, "RGBA")
    f1 = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 24); f2 = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 20)
    tw = d.textlength(LABEL, font=f1)
    d.rounded_rectangle([14, 12, 14 + tw + 24, 12 + 38], radius=8, fill=(20, 24, 36, 190))
    d.text((26, 18), LABEL, fill=(255, 255, 255, 255), font=f1)
    d.text((18, frame.height - 34), text_bottom, fill=(70, 76, 92, 255), font=f2)
    return frame


def main():
    ap = argparse.ArgumentParser(); ap.add_argument("out"); ap.add_argument("spec"); ap.add_argument("--fps", type=float, default=60)
    a = ap.parse_args()
    segs = []
    for part in a.spec.split(","):
        anim, rng = part.split(":"); t0, t1 = (float(v) for v in rng.split("-")); segs.append((anim, t0, t1))
    R = Renderer((1080, 1350), 0.5, bg="studio")
    clear = Image.new("RGBA", R.size, (0, 0, 0, 0))
    os.makedirs(a.out, exist_ok=True)
    report, i = [], 0
    for anim, t0, t1 in segs:
        n = int(round((t1 - t0) * a.fps))
        for k in range(n):
            t = t0 + k / a.fps
            layer = R.frame(anim, t, bg=clear)
            al = np.asarray(layer)[..., 3]
            holes = interior_holes(al)
            hl = hairline_px(al)
            # cluster the hairline pixels (dilated 1 px so a diagonal crack stays one cluster); a crack is one long cluster,
            # natural concave corners are many tiny ones
            from PIL import ImageFilter
            hd = np.asarray(Image.fromarray((hl * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3))) > 127
            cl = interior_holes(np.where(hd, 0, 255).astype(np.uint8), 128)
            report.append({"frame": i, "anim": anim, "t": round(t, 4), "holes": len(holes), "hole_px": int(sum(h[0] for h in holes)),
                           "largest": holes[0] if holes else None, "hairline_px": int(hl.sum()),
                           "hairline_largest_cluster": cl[0] if cl else None})
            frame = Image.alpha_composite(R.bg, layer).convert("RGB")
            burn_in(frame, "%s  %.2f s" % (anim, t)).save(os.path.join(a.out, "%05d.png" % i)); i += 1
    json.dump({"spec": a.spec, "fps": a.fps, "frames": i, "label": LABEL, "per_frame": report}, open(os.path.join(a.out, "render.json"), "w"), indent=0)
    bad = [r for r in report if r["hole_px"]]
    hc = lambda r: r["hairline_largest_cluster"][0] if r["hairline_largest_cluster"] else 0
    print("frames", i, "| frames with enclosed holes:", len(bad), "| max hole px in a frame:", max([r["hole_px"] for r in report] or [0]),
          "| largest hairline cluster: median %d, max %d" % (int(np.median([hc(r) for r in report])), max(hc(r) for r in report)))
    for r in sorted(report, key=lambda r: -r["hole_px"])[:6]: print("   most hole px:", r["frame"], r["anim"], r["t"], r["hole_px"], r["largest"])
    for r in sorted(report, key=hc)[::-1][:6]: print("   largest hairline cluster:", r["frame"], r["anim"], r["t"], r["hairline_largest_cluster"])


if __name__ == "__main__":
    main()
