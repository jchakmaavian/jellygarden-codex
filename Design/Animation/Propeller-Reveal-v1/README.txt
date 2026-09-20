Propeller Reveal (v1)
=====================

Sequence
  frames/Propeller_Reveal_001.webp ... Propeller_Reveal_040.webp
  40 frames, 60 fps, 0.667 s, plays once.
  512 x 512, transparent background, lossless WebP (ARGB).

Import note (WebP)
  Visible texels (alpha > 0) are bit-identical to the rendered PNG masters. The lossless WebP encoder stores its own
  colour under fully transparent texels (never visible). Import as a Sprite with Alpha Is Transparency on, or with the
  RoyalWebPImporter transparent-edge bleeding already used for the TNT and Color Ball frames.

Previews
  Preview.mp4       1024 x 512, 60 fps: the reveal twice, each followed by a 0.5 s hold on the rest pose.
                    Left: dark slate. Right: board lavender.
  Preview.webp      animated, transparent, loops (lossy preview; the frames/ WebPs are the deliverable).
  ContactSheet.png  all 40 frames, numbered, on the board lavender.
  60FPS_WEBP.zip    the 40 WebP frames.

Beats (frame numbers)
  001-003  compact arrival: the booster enters at about a third of its size with the blades already at speed, and a
           short white/gold flash at the hub. Four-point stars flick outward.
  004-008  elastic pop to 1.18x with squash and stretch, and a small tilt that overshoots and comes back
  009-013  counter-dip to 0.94x, tilt reverses
  014-020  second, smaller overshoot to 1.05x, wobble decaying
  003-026  blades spin fast and decelerate; motion blur spreads them over the angle swept in each frame, so what
           smears is the real painted blade, never a drawn streak
  027-033  the last degrees of rotation; individual blades become readable again
  034-040  seated: the exact rest pose, held
  040      = Propeller-Idle-v1 frame 001 = Propeller-Activation-v1 frame 001 (byte-identical)

Canvas alignment (same for all three propeller sequences)
  Every frame draws the original propeller.webp at a content scale of 0.72 about the booster's cell contact point:
    original art pixel (x, y)  ->  frame pixel (251.17 + 0.72 * (x - 251.17), 439.68 + 0.72 * (y - 439.68))
  The headroom is what keeps the reveal overshoot, the lifted flight pose and the star bursts clear of the canvas edge:
  no frame in any of the three sequences puts a single texel within 2 px of the border.
  To match the static board sprite's size, draw these frames 1 / 0.72 = 1.389x larger.
  Pivot: to sit exactly where the static sprite (pivot at its centre) sits, use the frame pivot (0.4974, 0.3996)
  (Unity normalised, x right, y up).

How it was made (the artwork is never redrawn)
  The original propeller.webp is split once into a body layer (the blue sphere with its gold band) and a rotor layer
  (the three blades, the gold collar and the pink dome), by hue plus the rotor's own geometry. The three blades sit
  120 deg apart in a rotor plane tilted 54.2 deg from face-on; that tilt was solved from the painted blade angles and
  reproduces them to 0.12 deg rms. Spinning is therefore a pure affine in that plane - undo the roll, undo the
  foreshortening, turn, put both back - which is the identity at rest, so every seated frame is the untouched artwork.
  Blades on the far half of the sweep are drawn behind the body and the near half in front, so a blade never passes
  through the sphere. The sphere's hidden top cap, which the rotor covers in the source art, is rebuilt from the body's
  own blue by fitting its shading against the key light, so no colour is invented.
  Only light is procedural: the hub flash, the four-point stars and the gold sparks.

Quality gate (source/PropQa.cs, run on the final frames)
  frames                              40 of 40, all 512 x 512
  dirty alpha                         0 texels with colour under alpha 0
  stray near-invisible texels         0
  edge clipping                       max alpha within 2 px of the border = 0 in every frame
  popping                             median frame-to-frame step 0.012, largest 0.066 (the elastic pop at 004 -> 005)
  hand-off                            frame 040 vs Idle 001 and Activation 001: byte-identical
  design                              three blades, their painted colours, the gold collar and the pink dome are the
                                      original pixels in every frame; nothing is regenerated
