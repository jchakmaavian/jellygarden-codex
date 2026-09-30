"""Structural validation of RigReady/spine/King_Venguard.json against its atlas and images (no Spine runtime involved)."""
import json, os, sys
HERE = os.path.dirname(os.path.abspath(__file__)); V1 = os.path.abspath(os.path.join(HERE, "..", "..")); SP = os.path.join(V1, "RigReady", "spine")
d = json.load(open(os.path.join(SP, "King_Venguard.json"))); errors = []
bones = [b["name"] for b in d["bones"]]; bset = set(bones)
for i, b in enumerate(d["bones"]):
    if i and b.get("parent") not in bones[:i]: errors.append("bone %s parent %s missing or declared later" % (b["name"], b.get("parent")))
slots = [s["name"] for s in d["slots"]]
for s in d["slots"]:
    if s["bone"] not in bset: errors.append("slot %s bone %s missing" % (s["name"], s["bone"]))
skin = d["skins"][0]["attachments"]
atlas = open(os.path.join(SP, "King_Venguard.atlas")).read().splitlines()
regions = {l.strip() for l in atlas if l and not l.startswith(" ") and ":" not in l and not l.endswith(".png")}
for s in d["slots"]:
    if s.get("attachment") and s["attachment"] not in skin.get(s["name"], {}): errors.append("slot %s default attachment missing" % s["name"])
    for a, v in skin.get(s["name"], {}).items():
        if v.get("type") == "clipping":
            if v["end"] not in slots: errors.append("clip %s end slot missing" % a)
            if len(v["vertices"]) != 2 * v["vertexCount"]: errors.append("clip %s vertex count" % a)
        else:
            if a not in regions: errors.append("attachment %s not in atlas" % a)
            if not os.path.exists(os.path.join(SP, "images", a + ".png")): errors.append("image %s missing" % a)
for an, anim in d["animations"].items():
    for b, ch in anim.get("bones", {}).items():
        if b not in bset: errors.append("%s: bone %s missing" % (an, b))
        for c, keys in ch.items():
            ts = [k["time"] for k in keys]
            if ts != sorted(ts): errors.append("%s: %s.%s keys not in time order" % (an, b, c))
    for s, ch in anim.get("slots", {}).items():
        if s not in slots: errors.append("%s: slot %s missing" % (an, s))
        for k in ch.get("attachment", []):
            if k.get("name") is not None and k["name"] not in skin.get(s, {}): errors.append("%s: slot %s swaps to unknown %s" % (an, s, k["name"]))
print("bones %d, slots %d, attachments %d, atlas regions %d, animations %s" % (len(bones), len(slots), sum(len(v) for v in skin.values()), len(regions), list(d["animations"])))
print("VALIDATION", "PASS" if not errors else "FAIL", *errors[:20], sep="\n  ")
sys.exit(1 if errors else 0)
