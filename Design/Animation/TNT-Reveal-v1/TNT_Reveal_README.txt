TNT Reveal - 2D frame sequence (v1)
====================================

Sequence
  frames/TNT_Reveal_001.webp ... TNT_Reveal_120.webp
  120 frames, 60 fps, exactly 2.00 s, non-looping.
  512 x 512, transparent background, lossless WebP (ARGB).
  Visible pixels are bit-identical to the rendered masters. Colour under fully
  transparent pixels is normalised by the WebP encoder (invisible).

Previews
  TNT_Reveal_Preview.mp4      1024 x 512, 60 fps, 2.00 s. Left: dark slate. Right: board lavender.
  TNT_Reveal_Preview.webp     animated, transparent, plays once (identical idle frames merged, total 2.00 s).
  TNT_Reveal_ContactSheet.png all 120 frames, numbered.
  TNT_Reveal_60FPS_WEBP.zip   the 120 WebP frames.

Beats (frame numbers)
  1        compressed, small (fuse hidden inside)
  4-12     first appearance, body rising
  14-30    head separates from the gold belt and lifts; light in the seam
  27-40    fuse emerges out of the mouth tip-first, spark leading
  36-60    ignition -> flame builds to maximum; head at peak lift
  64       overshoot
  68-75    head rebounds down past seated
  71-100   flame reduces; gold/white sparkles
  84       secondary bounce
  104-120  settles; 117-120 are the static idle pose

Canvas alignment (important for integration)
  Every frame draws the TNT at a fixed content scale of 0.84 of the original
  bomb_barrel art, with a fixed base pivot:
    original art pixel (x, y)  ->  frame pixel (256 + 0.84 * (x - 256), 480 + 0.84 * (y - 458))
  The barrel base sits at y = 478 in every frame. The headroom is needed for the
  lift, the extended fuse and the flames at the peak.
  To match the idle board sprite, draw these frames 1 / 0.84 = 1.19x larger with the
  pivot at the barrel base.

How it was made
  The TNT design is never redrawn. The original artwork was cut into layers (body with
  belt and crown badge, head/rim, rope, spark, front rim) and animated with transforms,
  so the badge, gold bands and proportions are identical in every frame. The rest pose
  reproduces the original art.
  GPT 2.5 (via Magnific) generated only what the art does not contain:
    - the barrel with the fuse removed, used solely to fill the rim/interior hidden under
      the rope (auto-registered to the art: scale 1.001, offset < 0.5 px);
    - a sheet of nine flame-tongue shapes, cycled and animated around the opened seam.
  The spark is rebuilt from its clean quarter by 4-fold symmetry, because the original
  spark glow overlaps the rim.
  Seam light, bloom and sparkles are procedural.

  source/ contains the renderer and the two generated inputs, so the timing, scale or
  flame can be changed and all 120 frames re-rendered (about 25 s).
