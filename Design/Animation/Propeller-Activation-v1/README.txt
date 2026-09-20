Propeller Activation / Flight (v1)
==================================

Sequence
  frames/Propeller_Activation_001.webp ... Propeller_Activation_120.webp
  120 frames, 60 fps, 512 x 512, transparent background, lossless WebP (ARGB).

  001-024  INTRO   plays once      0.40 s  anticipation, blade spin-up, hub flash, lift out of the cell
  025-084  LOOP    hold/repeat     1.00 s  seamless (084 flows into 025); play it for the whole flight
  085-120  FINISH  plays once      0.60 s  arrival pinch, gold star burst, clean exit. Frame 120 is fully transparent.

  Unity playback: play 001-024, loop 025-084 while gameplay carries the booster along its path, then play 085-120 when
  it reaches the target. The loop carries no net translation, so the flight path stays entirely under gameplay control.

Import note (WebP)
  Visible texels (alpha > 0) are bit-identical to the rendered PNG masters. The lossless WebP encoder stores its own
  colour under fully transparent texels (never visible). Import as a Sprite with Alpha Is Transparency on, or with the
  RoyalWebPImporter transparent-edge bleeding already used for the TNT and Color Ball frames.

Previews
  Preview.mp4       1024 x 512, 60 fps: intro, the loop three times, finish, then 0.4 s of empty background.
                    Left: dark slate. Right: board lavender.
  Preview.webp      animated, transparent, all 120 frames once per cycle (lossy preview).
  ContactSheet.png  all 120 frames, numbered.
  60FPS_WEBP.zip    the 120 WebP frames.

Beats (frame numbers)
  001      the untouched rest pose = Propeller-Idle-v1 frame 001 (byte-identical), so activation starts without a jump
  002-005  anticipation: the body compresses to 0.965 and squashes by 8.5 %
  003-018  blades accelerate from a standstill to 70 deg per frame; the motion blur spreads each frame's real sweep
  006-014  the hub charges: a white core that warms to gold, with four-point stars thrown outward
  009-024  the booster lifts 54 px out of its cell, stretching slightly and leaning 4.2 deg into the direction of travel
  025-084  flight: held at the lifted pose with a gentle 1.2 % breathing bob and a 1.5 deg lean, blades at full speed,
           a compact white/gold sparkle trail riding the blade tips. Seamless: the sweep is a whole number of turns
           across the 60 frames, so 084 flows into 025.
  085-091  arrival pinch: one last compress to 1.14 before the exit
  085-105  white-hot core and a gold/yellow four-point star burst, with small gold sparks thrown outward
  092-120  the booster shrinks away and fades; 120 is fully transparent
  No rocket flame, no smoke, no dark fire: the exhaust language is white core, warm gold, small stars and sparks.

  The impact on the target gem is deliberately NOT in these frames. The booster's own arrival is here; the target's
  white/gold flash, outline and star flash belong to gameplay VFX so they can be placed on whatever cell is hit.

Canvas alignment
  Same as Propeller-Reveal-v1: the original propeller.webp is drawn at a content scale of 0.72 about the booster's
  cell contact point:
    original art pixel (x, y)  ->  frame pixel (251.17 + 0.72 * (x - 251.17), 439.68 + 0.72 * (y - 439.68))
  Draw the frames 1 / 0.72 = 1.389x larger to match the static board sprite, with the frame pivot (0.4974, 0.3996).
  The lift in the intro and loop is inside the canvas, so the sprite never has to move to show it.

Quality gate (source/PropQa.cs, run on the final frames)
  frames                              120 of 120, all 512 x 512
  dirty alpha                         0 texels with colour under alpha 0
  stray near-invisible texels         0
  edge clipping                       max alpha within 2 px of the border = 0 in every frame
  intro 001-024                       median step 0.036, largest 0.041: no frame jump
  loop 025-084                        median step 0.032, largest 0.035
  loop seam 084 -> 025                0.033 against a median step of 0.032, an ordinary step of the spin
  finish 085-120                      median step 0.021, largest 0.041
  last frame                          fully transparent (max alpha 0.0000)
  hand-off                            frame 001 is byte-identical to Idle 001 and Reveal 040
  design                              three blades, their painted colours, the gold collar and the pink dome are the
                                      original pixels in every frame; nothing is regenerated
