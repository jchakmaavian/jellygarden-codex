"""Build Royal Throne area art and the runtime catalogue from Design/Areas.

For every area in area-specs.json:
  Design/Areas/AreaNN/after.png   the restored scene (Area 1: the approved Home castle)
  Design/Areas/AreaNN/before.png  the same scene with every task region retouched to its unrestored state
Writes:
  Design/Areas/AreaNN/masks/task-KK.png   binary masks (white = task region) for Magnific retouch
  Assets/JellyGarden/Resources/RoyalAreas/AreaNN/{before,after,thumb-before,thumb-after}.webp
  Assets/JellyGarden/Resources/RoyalAreas/AreaNN/task-KK.webp   the restored object, soft-edged, cropped to its region
  Assets/JellyGarden/Resources/RoyalAreas/areas.json            the catalogue RoyalAreas reads at runtime

Usage: build_areas.py [--placeholder]
  --placeholder  when before.png is missing, make a temporary before-scene locally (task regions greyed and softened).
                 It is marked "placeholder" in the catalogue and must be replaced by the Magnific retouch result.
"""
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
DESIGN = os.path.join(ROOT, "Design", "Areas")
OUT = os.path.join(ROOT, "Assets", "JellyGarden", "Resources", "RoyalAreas")
CASTLE = os.path.join(ROOT, "Assets", "ReferenceGameplay", "Home", "Resources", "ReferenceGameplay", "Home", "Sky", "Homepage_bg.png")
# Scenes are stored at 940 x 1672 (both multiples of 4, so GPU block compression applies), 9:16 within 0.1%.
SCENE = (940, 1672)
THUMB = (240, 428)
FEATHER = 6          # px of soft edge around each task region
QUALITY = 90


def area_dir(number):
    return os.path.join(DESIGN, "Area%02d" % number)


SKY = {}


def sky_of(scene):
    """Open sky: light sky-blue pixels connected to the top edge (roofs and banners are darker and are not reached)."""
    key = id(scene)
    if key not in SKY:
        a = np.asarray(scene.convert("RGB")).astype(int)
        r, g, b = a[..., 0], a[..., 1], a[..., 2]
        # A copy: an image made by fromarray shares the array read-only, and floodfill then changes nothing.
        skylike = Image.fromarray(((b > 190) & (g > 120) & (b - r > 60)).astype(np.uint8) * 255).copy()
        for x in range(0, skylike.width, 8):
            if skylike.getpixel((x, 0)) == 255:
                ImageDraw.floodfill(skylike, (x, 0), 128)
        sky = Image.fromarray((np.asarray(skylike) == 128).astype(np.uint8) * 255)
        # Keep a few pixels of sky around the silhouette so the object's own edge blends into the soft patch edge.
        SKY[key] = sky.filter(ImageFilter.MinFilter(9))
    return SKY[key]


def mask_for(task, size, feather=0, scene=None):
    m = Image.new("L", size, 0)
    d = ImageDraw.Draw(m)
    for poly in task["regions"]:
        d.polygon([tuple(p) for p in poly], fill=255)
    if task.get("excludeSky") and scene is not None:
        m = Image.fromarray((np.asarray(m) > 0) & (np.asarray(sky_of(scene)) == 0)).convert("L").point(lambda v: 255 if v else 0)
    if feather:
        m = m.filter(ImageFilter.GaussianBlur(feather))
    return m


def fit_scene(img):
    """Crop to 9:16 around the centre, then scale to SCENE."""
    w, h = img.size
    target = SCENE[0] / SCENE[1]
    if w / h > target:
        nw = round(h * target); img = img.crop(((w - nw) // 2, 0, (w - nw) // 2 + nw, h))
    else:
        nh = round(w / target); img = img.crop((0, (h - nh) // 2, w, (h - nh) // 2 + nh))
    return img.resize(SCENE, Image.LANCZOS)


def placeholder_before(after, tasks):
    """Temporary stand-in for the unrestored scene: each task region greyed, darkened and softened."""
    base = after.convert("RGB")
    grey = ImageEnhance.Brightness(ImageEnhance.Color(base).enhance(.15)).enhance(.72).filter(ImageFilter.GaussianBlur(5))
    for t in tasks:
        base = Image.composite(grey, base, mask_for(t, base.size, 3, after))
    return base


def save_webp(img, path, lossless=False):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path, "WEBP", quality=QUALITY, method=6, lossless=lossless)


def round4(v, up):
    return (v + 3) // 4 * 4 if up else v // 4 * 4


def build_area(area, allow_placeholder):
    number, tasks = area["number"], area["tasks"]
    entry = {k: area.get(k) for k in ("number", "name", "subtitle", "outdoor", "rewardCoins", "rewardBoost")}
    entry["tasks"] = [{"name": t["name"], "cost": t["cost"]} for t in tasks]
    entry["placeholder"] = False
    folder = area_dir(number)
    after_path = os.path.join(folder, "after.png")
    if number == 1 and not os.path.exists(after_path):
        os.makedirs(folder, exist_ok=True)
        # The user-supplied castle: drop one pixel column to reach 940 wide; the aspect stays 9:16.
        Image.open(CASTLE).convert("RGB").crop((0, 0, 940, 1672)).save(after_path)
    if not os.path.exists(after_path) or not all("regions" in t for t in tasks):
        return entry  # art not generated yet: the catalogue lists the area, the game shows it as coming soon
    after = fit_scene(Image.open(after_path).convert("RGB"))
    os.makedirs(os.path.join(folder, "masks"), exist_ok=True)
    for k, t in enumerate(tasks):
        mask_for(t, after.size, 0, after).save(os.path.join(folder, "masks", "task-%02d.png" % (k + 1)))
    before_path = os.path.join(folder, "before.png")
    if os.path.exists(before_path):
        before = fit_scene(Image.open(before_path).convert("RGB"))
    elif allow_placeholder:
        before = placeholder_before(after, tasks)
        before.save(os.path.join(folder, "before-placeholder.png"))
        entry["placeholder"] = True
    else:
        return entry
    target = os.path.join(OUT, "Area%02d" % number)
    save_webp(after, os.path.join(target, "after.webp"))
    save_webp(before, os.path.join(target, "before.webp"))
    save_webp(after.resize(THUMB, Image.LANCZOS), os.path.join(target, "thumb-after.webp"))
    save_webp(before.resize(THUMB, Image.LANCZOS), os.path.join(target, "thumb-before.webp"))
    key = "Area%02d/" % number
    entry.update(before=key + "before", after=key + "after", thumbBefore=key + "thumb-before", thumbAfter=key + "thumb-after")
    rgba = after.convert("RGBA")
    W, H = after.size
    for k, t in enumerate(tasks):
        soft = mask_for(t, after.size, FEATHER, after)
        box = soft.getbbox()
        # Grow to whole 4 x 4 blocks so the patch compresses cleanly; stay inside the scene.
        x0, y0 = max(0, round4(box[0], False)), max(0, round4(box[1], False))
        x1, y1 = min(W, round4(box[2], True)), min(H, round4(box[3], True))
        patch = rgba.crop((x0, y0, x1, y1))
        patch.putalpha(soft.crop((x0, y0, x1, y1)))
        save_webp(patch, os.path.join(target, "task-%02d.webp" % (k + 1)))
        entry["tasks"][k].update(patch=key + "task-%02d" % (k + 1), x0=x0 / W, y0=y0 / H, x1=x1 / W, y1=y1 / H)
    return entry


def main():
    allow_placeholder = "--placeholder" in sys.argv
    specs = json.load(open(os.path.join(DESIGN, "area-specs.json"), encoding="utf-8"))
    areas = [build_area(a, allow_placeholder) for a in specs["areas"]]
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT, "areas.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump({"areas": areas}, f, indent=1, ensure_ascii=False)
        f.write("\n")
    for a in areas:
        state = "art ready" + (" (PLACEHOLDER before)" if a["placeholder"] else "") if "before" in a else "waiting for art"
        print("Area %2d  %-22s %2d tasks, %2d stars  %s" % (a["number"], a["name"], len(a["tasks"]), sum(t["cost"] for t in a["tasks"]), state))
    print("total stars", sum(sum(t["cost"] for t in a["tasks"]) for a in areas))


if __name__ == "__main__":
    main()
