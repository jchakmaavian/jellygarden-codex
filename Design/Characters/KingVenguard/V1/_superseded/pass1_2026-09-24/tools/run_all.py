"""Rebuild everything from Source/magnific: parts -> seam pass -> checks -> Spine rig -> previews -> sheets -> PSD.
usage: python run_all.py [frames_dir]   (frames_dir defaults to a temporary folder; MP4s go to Previews/)"""
import os, shutil, subprocess, sys, tempfile
HERE = os.path.dirname(os.path.abspath(__file__)); V1 = os.path.abspath(os.path.join(HERE, "..", "..")); PY = sys.executable
frames = sys.argv[1] if len(sys.argv) > 1 else tempfile.mkdtemp(prefix="kv_frames_")
def run(*a): print(">", " ".join(a[1:])); subprocess.run([PY, *a], cwd=HERE, check=True)
run(os.path.join(HERE, "build_parts.py")); run(os.path.join(HERE, "fill_orphans.py")); run(os.path.join(HERE, "check_parts.py"))
run(os.path.join(HERE, "build_rig.py")); run(os.path.join(HERE, "validate_rig.py"))
jobs = [("idle", "--loops", "2"), ("happy_thumbs_up", "--pre", "0.4", "--post", "0.6")]
procs = []
for anim, *extra in jobs:
    out = os.path.join(frames, anim); shutil.rmtree(out, ignore_errors=True)
    procs.append(subprocess.Popen([PY, os.path.join(HERE, "render_rig.py"), anim, out, "--fps", "60", *extra], cwd=HERE))
for p in procs: assert p.wait() == 0
for anim, mp4 in (("idle", "KingVenguard_Idle_Loop_4s_x2_60fps.mp4"), ("happy_thumbs_up", "KingVenguard_Happy_ThumbsUp_3.8s_60fps.mp4")):
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", "60", "-i", os.path.join(frames, anim, "%05d.png"), "-c:v", "libx264",
                    "-pix_fmt", "yuv420p", "-crf", "17", "-preset", "slow", "-movflags", "+faststart", "-an", os.path.join(V1, "Previews", mp4)], check=True)
run(os.path.join(HERE, "make_deliverables.py")); run(os.path.join(HERE, "write_psd.py"))
print("frames in", frames)
