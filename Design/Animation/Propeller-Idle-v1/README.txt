Propeller Idle - seamless loop (v1)
===================================

Sequence
  frames/Propeller_Idle_001.webp ... Propeller_Idle_120.webp
  120 frames, 60 fps, exactly 2.00 s, seamless loop (frame 120 flows into frame 001).
  512 x 512, transparent background, lossless WebP (ARGB).

Import note (WebP)
  Visible texels (alpha > 0) are bit-identical to the rendered PNG masters. The lossless WebP encoder stores its own
  colour under fully transparent texels (never visible). Import as a Sprite with Alpha Is Transparency on, or with the
  RoyalWebPImporter transparent-edge bleeding already used for the TNT and Color Ball frames.

Previews
  Preview.mp4       1024 x 512, 60 fps, THREE loops back to back (6.00 s) so the wrap can be checked on any player.
                    Left: dark slate. Right: board lavender.
  Preview.webp      animated, transparent, loops forever (lossy preview; the frames/ WebPs are the deliverable).
  ContactSheet.png  all 120 frames, numbered.
  60FPS_WEBP.zip    the 120 WebP frames.

Motion (frame numbers)
  001      the untouched rest pose = Propeller-Reveal-v1 frame 040 = Propeller-Activation-v1 frame 001
  001-120  blades twitch by at most 1.7 deg and return; the body bobs by at most 0.8 px. Both are cosine-based and
           exactly zero at the loop boundary, so the loop closes on the artwork itself.
  031-091  a faint highlight glides across the gold collar, peaking near 061
  001-120  a very small glow variation around the pink dome, zero at both ends of the loop
  027, 082 one tiny four-point sparkle each, born and gone well inside the loop
  The booster does not spin, orbit, bounce or drift. The three blades, their red tips, the gold collar and the pink
  dome stay individually readable in every frame.

Canvas alignment
  Same as Propeller-Reveal-v1: the original propeller.webp is drawn at a content scale of 0.72 about the booster's
  cell contact point:
    original art pixel (x, y)  ->  frame pixel (251.17 + 0.72 * (x - 251.17), 439.68 + 0.72 * (y - 439.68))
  Draw the frames 1 / 0.72 = 1.389x larger to match the static board sprite, with the frame pivot (0.4974, 0.3996).

Quality gate (source/PropQa.cs, run on the final frames)
  frames                              120 of 120, all 512 x 512
  dirty alpha                         0 texels with colour under alpha 0
  stray near-invisible texels         0
  edge clipping                       max alpha within 2 px of the border = 0 in every frame
  loop seam 120 -> 001                0.000, the same as the median step inside the loop
  position drift                      alpha centroid moves 0.11 px in x and 0.31 px in y across the whole loop
  popping                             no isolated frame-to-frame jumps
  hand-off                            frame 001 is byte-identical to Reveal 040 and Activation 001
