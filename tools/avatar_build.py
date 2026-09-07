#!/usr/bin/env python3
"""Slice a character's idle clip into the avatar frames the HUD plays.

The level HUD draws a head-and-shoulders avatar rather than the whole standing
figure, and it moves: the king breathes, blinks and turns his head a few
degrees. That motion is a short clip, and this cuts it into the numbered sprites
StoryArt.AvatarFrames reads.

PARKED. The one clip this had — tools/art/avatar/leo_idle.mp4 — animates the
retired Leo, and the cast has since been redrawn. Running it would put sixty
frames of the old face in the HUD while every portrait, card and cutscene shows
the new one, so CLIPS is empty and this builds nothing. It is kept, not deleted:
the moment there is an idle clip of the new Leo, adding it back to CLIPS is the
whole job.

The clip is the master and lives in tools/art/avatar/. It was made by cropping
the head and shoulders out of the full-body portrait — pixels (585,25)-(965,405)
of the 1536x1536 portrait_leo.jpg, the window StoryArt.AvatarCrop named before
the redraw — and animating that crop. The prompt asked for a static camera, an
unchanged background and a return to the opening pose, so the last frame lands
back on the first and the loop closes without a cut: frame 0 and frame 120
differ by about 1.1 levels out of 255, which is below what an eye catches.

    python3 tools/avatar_build.py

Needs ffmpeg on PATH.
"""

import os
import shutil
import subprocess
import sys
import tempfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from asset_import import sprite_meta  # noqa: E402
from story_common import STORY_RES, ensure_dir  # noqa: E402

TOOLS = os.path.dirname(os.path.abspath(__file__))
MASTERS = os.path.join(TOOLS, "art", "avatar")
DEST_ART = os.path.join(STORY_RES, "Art")

# Twelve frames a second over the whole five-second clip. The motion is a slow
# idle, so it reads correctly well under cinema rates; the count is what decides
# how much texture memory one avatar costs, and sixty 256px frames is about two
# megabytes compressed.
FPS = 12
SIZE = 256

# character -> the clip that carries their idle.
#
# Empty since the redraw: leo_idle.mp4 is the retired Leo. Put a character back
# here only when the clip shows the face the portraits show.
CLIPS = {
}


def frames_of(clip, work):
    """Every frame of the clip, resampled and resized, in order."""
    subprocess.run(
        ["ffmpeg", "-v", "error", "-y", "-i", clip,
         "-vf", "fps=%d,scale=%d:%d:flags=lanczos" % (FPS, SIZE, SIZE),
         os.path.join(work, "f_%03d.png")],
        check=True)
    return sorted(os.path.join(work, n) for n in os.listdir(work)
                  if n.startswith("f_") and n.endswith(".png"))


def build(name, clip_name):
    clip = os.path.join(MASTERS, clip_name)
    if not os.path.exists(clip):
        return 0, "missing master: %s" % clip_name

    work = tempfile.mkdtemp(prefix="avatar_")
    try:
        frames = frames_of(clip, work)
        if not frames:
            return 0, "no frames came out of %s" % clip_name

        # The clip returns to its opening pose, so its last frame is a near
        # duplicate of its first. Keeping both would hold that pose for two
        # frames every loop, which reads as a stutter once a second.
        frames = frames[:-1]

        ensure_dir(DEST_ART)

        # Whatever was there before goes, or shortening a clip would leave the
        # tail of the previous run behind and the loop would run long.
        for existing in os.listdir(DEST_ART):
            if existing.startswith("avatar_%s_" % name):
                os.remove(os.path.join(DEST_ART, existing))

        for index, source in enumerate(frames):
            dest = os.path.join(DEST_ART, "avatar_%s_%02d.png" % (name, index))
            shutil.copyfile(source, dest)
            with open(dest + ".meta", "w", encoding="utf-8") as fh:
                fh.write(sprite_meta(dest))

        return len(frames), None
    finally:
        shutil.rmtree(work, ignore_errors=True)


def main():
    if shutil.which("ffmpeg") is None:
        print("ERROR: ffmpeg is not on PATH", file=sys.stderr)
        return 1

    problems = []
    total = 0

    for name, clip_name in sorted(CLIPS.items()):
        count, problem = build(name, clip_name)
        if problem:
            problems.append(problem)
        else:
            total += count
            print("%s: %d frames at %dfps, %dpx" % (name, count, FPS, SIZE))

    for problem in problems:
        print("ERROR: %s" % problem, file=sys.stderr)

    print("avatars=%d frames=%d" % (len(CLIPS) - len(problems), total))
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
