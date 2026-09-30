"""Loop seam and hand-off continuity of the two approval animations, evaluated with the custom preview player's
evaluator (spine_eval.py, not the official Spine runtime).

Junctions: idle 4.0 s -> idle 0.0 s (loop seam), idle 0.0 s -> happy_thumbs_up 0.0 s (start), happy_thumbs_up 3.8 s ->
idle 0.0 s (return). For each: the largest difference of any bone's world position (source px) and world rotation
(degrees) and every slot whose attachment differs. For scale, the largest single-frame (1/60 s) bone move inside each
animation is reported too. Writes Previews/checks/continuity.json.
"""
import json, math, os, sys
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import spine_eval as se

V1 = os.path.abspath(os.path.join(HERE, "..", ".."))
data = json.load(open(os.path.join(V1, "RigReady", "spine", "King_Venguard.json")))
sk = se.Skeleton(data)
DUR = {"idle": 4.0, "happy_thumbs_up": 3.8}


def diff(a, b):
    (wa, sa), (wb, sb) = sk.pose(*a), sk.pose(*b)
    dp = max(math.hypot(wa[k][4] - wb[k][4], wa[k][5] - wb[k][5]) for k in wa)
    dr = max(abs((se.world_rotation(wa[k]) - se.world_rotation(wb[k]) + 180) % 360 - 180) for k in wa)
    worst = max(wa, key=lambda k: math.hypot(wa[k][4] - wb[k][4], wa[k][5] - wb[k][5]))
    slots = {s: (sa.get(s), sb.get(s)) for s in set(sa) | set(sb) if sa.get(s) != sb.get(s)}
    return {"max_bone_move_px": round(dp, 3), "max_bone_turn_deg": round(dr, 3), "worst_bone": worst, "attachment_changes": slots}


report = {"junctions": {
    "idle loop seam (4.0 s -> 0.0 s)": diff(("idle", DUR["idle"]), ("idle", 0.0)),
    "idle -> happy_thumbs_up start": diff(("idle", 0.0), ("happy_thumbs_up", 0.0)),
    "happy_thumbs_up end -> idle": diff(("happy_thumbs_up", DUR["happy_thumbs_up"]), ("idle", 0.0))}}
report["largest_single_frame_move_px_at_60fps"] = {}
for anim, d in DUR.items():
    ts = np.arange(0, d + 1e-9, 1 / 60.0)
    steps = [(diff((anim, float(ts[i])), (anim, float(ts[i + 1]))), float(ts[i])) for i in range(len(ts) - 1)]
    best, t = max(steps, key=lambda s: s[0]["max_bone_move_px"])
    report["largest_single_frame_move_px_at_60fps"][anim] = {"px": best["max_bone_move_px"], "bone": best["worst_bone"], "at_s": round(t, 3)}
for k, v in report["junctions"].items():
    print("%-36s bone move %.3f px, turn %.3f deg (worst %s), attachment changes %s" % (k, v["max_bone_move_px"], v["max_bone_turn_deg"],
                                                                                        v["worst_bone"], v["attachment_changes"] or "none"))
print("largest single-frame bone move at 60 fps:", report["largest_single_frame_move_px_at_60fps"])
out = os.path.join(V1, "Previews", "checks"); os.makedirs(out, exist_ok=True)
json.dump(report, open(os.path.join(out, "continuity.json"), "w"), indent=1)
