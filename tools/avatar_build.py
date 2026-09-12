#!/usr/bin/env python3
"""Slice a character's clips into the avatar frames the HUD plays.

The level HUD draws a head-and-shoulders avatar rather than the whole standing
figure, and it moves: the king breathes, blinks and turns his head a few
degrees. That motion is a short clip, and this cuts it into the numbered sprites
StoryArt.AvatarFrames reads.

Five clips, one per HUD state: the idle the avatar loops, and the four reactions
StoryLevelHud and StoryLosePopup fire — cheer, pop, worry, slump.

leo_idle.mp4 is the RETIRED Leo and is not in CLIPS; leo_idle_b.mp4 is the face
that ships. It is kept so the old frames can be regenerated if the redraw is
ever rolled back.

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
import re
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

# character -> {clip name (empty string = the idle) -> master file}.
#
# Put a character here only when the clips show the face the portraits show.
CLIPS = {
    "leo": {
        "": "leo_idle_b.mp4",
        "cheer": "leo_cheer.mp4",
        "pop": "leo_pop.mp4",
        "slump": "leo_slump.mp4",
        "worry": "leo_worry.mp4",
    },
}

# How many frames each kind of clip is cut down to. StoryAvatar ping-pongs what
# it is given, so these are the frames of one direction, not of a whole loop.
# The idle is the one always on screen and gets the smoothest motion; a reaction
# plays once and is over in under a second and a half, where fewer frames read
# the same. Twenty-five plus four seventeens is 93 sprites, about three
# megabytes of texture — the budget the old set already spent.
IDLE_FRAMES = 25
CLIP_FRAMES = 17


def frames_of(clip, work):
    """Every frame of the clip, resampled and resized, in order."""
    subprocess.run(
        ["ffmpeg", "-v", "error", "-y", "-i", clip,
         "-vf", "fps=%d,scale=%d:%d:flags=lanczos" % (FPS, SIZE, SIZE),
         os.path.join(work, "f_%03d.png")],
        check=True)
    return sorted(os.path.join(work, n) for n in os.listdir(work)
                  if n.startswith("f_") and n.endswith(".png"))


def thinned(frames, count):
    """`count` frames spread evenly across the clip, ends included.

    Taken by index rather than by re-encoding at a lower rate: ffmpeg's fps
    filter drops whichever frame is nearest each slot boundary, which on a
    five-second clip lands the blink on a different frame every run.
    """
    if count >= len(frames):
        return frames
    last = len(frames) - 1
    return [frames[round(i * last / (count - 1))] for i in range(count)]


def build(name, clip_name, suffix, wanted):
    clip = os.path.join(MASTERS, clip_name)
    if not os.path.exists(clip):
        return 0, "missing master: %s" % clip_name

    work = tempfile.mkdtemp(prefix="avatar_")
    try:
        frames = frames_of(clip, work)
        if not frames:
            return 0, "no frames came out of %s" % clip_name

        frames = thinned(frames, wanted)

        ensure_dir(DEST_ART)

        # Whatever was there before goes, or shortening a clip would leave the
        # tail of the previous run behind and the loop would run long. The idle
        # has to be matched exactly rather than by prefix: "avatar_leo_" is also
        # the start of every reaction's name, and clearing on the prefix would
        # delete the clip built one line earlier.
        stem = "avatar_%s_%s_" % (name, suffix) if suffix else "avatar_%s_" % name
        mine = re.compile(r"^%s\d\d\.png$" % re.escape(stem))
        for existing in os.listdir(DEST_ART):
            if mine.match(existing):
                os.remove(os.path.join(DEST_ART, existing))

        for index, source in enumerate(frames):
            dest = os.path.join(DEST_ART, "%s%02d.png" % (stem, index))
            shutil.copyfile(source, dest)

            # An existing .meta is left exactly as it is. Rewriting it would
            # hand the sprite a new GUID and reset settings a person tuned by
            # hand — these frames ship at maxTextureSize 256, which sprite_meta
            # does not know about. Only a frame that is new to the tree gets a
            # generated one.
            if not os.path.exists(dest + ".meta"):
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
    built = 0

    for name, clips in sorted(CLIPS.items()):
        for suffix, clip_name in sorted(clips.items()):
            wanted = IDLE_FRAMES if suffix == "" else CLIP_FRAMES
            count, problem = build(name, clip_name, suffix, wanted)
            if problem:
                problems.append(problem)
                continue

            total += count
            built += 1
            print("%s %s: %d frames at %dpx" %
                  (name, suffix or "idle", count, SIZE))

    for problem in problems:
        print("ERROR: %s" % problem, file=sys.stderr)

    print("clips=%d frames=%d" % (built, total))
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
