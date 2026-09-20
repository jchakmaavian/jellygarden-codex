TNT Idle - seamless 2D loop (v1)
=================================

Sequence
  frames/TNT_Idle_001.webp ... TNT_Idle_120.webp
  120 frames, 60 fps, exactly 2.00 s, seamless loop (frame 120 flows into frame 001).
  512 x 512, transparent background, lossless WebP (ARGB).
  Visible pixels are bit-identical to the rendered masters.

Previews
  TNT_Idle_Preview.mp4       1024 x 512, 60 fps, THREE loops back to back (6.00 s) so the wrap
                             can be checked on any player. Left: dark slate. Right: board lavender.
  TNT_Idle_Preview.webp      animated, transparent, 2.00 s, loops forever.
  TNT_Idle_ContactSheet.png  all 120 frames, numbered.
  TNT_Idle_60FPS_WEBP.zip    the 120 WebP frames.

Motion (frame numbers)
  1        rest pose - identical to the last frame of TNT-Reveal-v1, so the reveal hands off
           straight into this loop
  1-48     slight inhale peaking at frame 25 (body +1.2% wide, +1.6% tall), return by 49
  33       head/rim delayed secondary bob (vertical flex, rim top moves about 1.2 px)
  73-85    tiny secondary reaction: fuse flick, brief spark glint, echo in body and head
  97       settle
  fuse     bends progressively toward the tip (max 3.4 degrees at the tip), never rigid
  spark    six fixed shape variants cycled with crossfades (5 frames each, 4 cycles per loop)
           under a small -> brighter (31) -> small (57) -> brief sparkle (77) -> small envelope
  glow     warm halo around the spark, peaks at 31 and 77, zero at the loop boundary
  sparkles three tiny white/gold stars near the fuse (born at 19, 75, 97; all gone before 120)
  No lid opening, no flame burst, no position drift.

Canvas alignment
  Same as TNT-Reveal-v1: the TNT is drawn at a fixed content scale of 0.84 of the original
  bomb_barrel art, with the base pivot fixed:
    original art pixel (x, y)  ->  frame pixel (256 + 0.84 * (x - 256), 480 + 0.84 * (y - 458))
  The barrel base is on pixel row 478 in every frame.
  To match the idle board sprite size, draw these frames 1 / 0.84 = 1.19x larger with the pivot
  at the barrel base.

Quality gate (source/IdleCheck.cs, run on the final frames)
  base row                          478 in all 120 frames
  drift, first vs last frame        centroid within 0.05 px
  crown badge brightness            max frame-to-frame change 0.0002 (no flicker)
  spark core path                   largest step 2.3 px per frame (no teleport)
  spark brightness                  96 -> 185; worst single-frame deviation 3.7% (no popping)
  frame-to-frame change             median 0.0032, max 0.0064
  loop seam 120 -> 001              0.0011 - smaller than the neighbouring steps (0.0014)
  frame 001 vs TNT-Reveal-v1 120    identical

How it was made
  Same layered rig as TNT-Reveal-v1. The TNT body, crown badge, gold bands, proportions and
  lighting are the original artwork, never redrawn. Motion comes from transforms only.
  No new Magnific / GPT 2.5 generation was needed: the spark variations are derived from the
  existing clean spark (ray-length modulation, spin, scale, white-hot brightening), and the
  sparkles and glow are procedural. The only generated input is the fuse-less barrel from the
  reveal, used solely for the rim area hidden under the rope when the fuse bends.
  Every curve is periodic over 120 frames with matching slope at the wrap, so the loop has no
  jump and no kink.

  source/ contains the renderer and quality gate; see source/RENDER.txt to change the motion
  and re-render (about 35 s).
