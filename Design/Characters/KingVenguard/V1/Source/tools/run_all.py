"""Rebuild everything from Source/magnific: parts -> seam pass -> expression parts -> part checks -> Spine rig ->
validation -> setup/coverage and continuity checks -> the two approval previews -> sheets -> PSD.
usage: python run_all.py [frames_dir]   (frames_dir defaults to a temporary folder; MP4s go to Previews/)

The previews are rendered by render_sequence.py with the existing custom player (render_rig.py) and carry the burned-in
label "Custom-player preview - official Spine compatibility untested." Per-frame hole and hairline reports are copied to
Previews/checks/render-idle.json and render-happy.json."""
import os, shutil, subprocess, sys, tempfile
HERE = os.path.dirname(os.path.abspath(__file__)); V1 = os.path.abspath(os.path.join(HERE, "..", "..")); PY = sys.executable
frames = sys.argv[1] if len(sys.argv) > 1 else tempfile.mkdtemp(prefix="kv_frames_")
def run(*a): print(">", " ".join(os.path.basename(x) for x in a), flush=True); subprocess.run([PY, *a], cwd=HERE, check=True)
for step in ("build_parts", "fill_orphans", "build_expressions", "check_parts", "build_rig", "validate_rig", "check_fidelity", "check_continuity"):
    run(os.path.join(HERE, step + ".py"))
PREVIEWS = [("KingVenguard_Idle_Loop_60fps_CustomPlayerPreview.mp4", "idle", "idle:0-4,idle:0-4"),                       # two loops, 8.0 s
            ("KingVenguard_Happy_ThumbsUp_60fps_CustomPlayerPreview.mp4", "happy", "idle:3.0-4.0,happy_thumbs_up:0-3.8,idle:0-1.2")]   # idle -> happy -> idle, 6.0 s
procs = []
for mp4, name, spec in PREVIEWS:
    out = os.path.join(frames, name); shutil.rmtree(out, ignore_errors=True)
    procs.append(subprocess.Popen([PY, os.path.join(HERE, "render_sequence.py"), out, spec, "--fps", "60"], cwd=HERE))
for p in procs: assert p.wait() == 0
os.makedirs(os.path.join(V1, "Previews", "checks"), exist_ok=True)
for mp4, name, spec in PREVIEWS:
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", "60", "-i", os.path.join(frames, name, "%05d.png"), "-c:v", "libx264",
                    "-pix_fmt", "yuv420p", "-crf", "17", "-preset", "slow", "-movflags", "+faststart", "-an", os.path.join(V1, "Previews", mp4)], check=True)
    shutil.copy(os.path.join(frames, name, "render.json"), os.path.join(V1, "Previews", "checks", "render-%s.json" % name))
run(os.path.join(HERE, "make_deliverables.py")); run(os.path.join(HERE, "write_psd.py"))
print("frames in", frames)
