# Dimensional Well Done lettering

The user rejected the flat v6 title and requested a 3D Royal Match look. V7 gives each letter a substantial bronze-to-gold sidewall, directional gold bevel, dark inner edge and raised ivory-to-champagne face. The previous v6 review is preserved.

Watch the [actual Unity card](Royal-Throne-v7-Unity-Card.mp4), [reference comparison](Royal-Throne-v7-Reference-vs-Unity-Title.mp4), and [six-stage comparison](Royal-Throne-v7-Title-Wave-Comparison.png). The [editable browser preview](Preview/preview.html) reconstructs the same treatment with CSS depth layers; Unity captures are the implementation evidence.

## Appearance and motion

The supplied `game completed experience.mp4` remains the visual reference. Its substantial gold body, dark depth and playful letter angles were missing from the previous thin outline. V7 retains Royal Throne's font and builds the dimensional look from its font atlas, rather than claiming an exact reproduction of Royal Match's typeface.

- Eight solid depth slices form a 7.5-unit sidewall. Directional gold bevels and the raised face project together through perspective; a back shadow grounds the lettering. This is projected UI extrusion, not a separate scene camera or rasterized title animation.
- The title is 62 units, positioned at row 190 to accommodate the larger body and its upward pop. Each letter has a small resting fan angle, from -5 to +5 degrees.
- Starting at 0.18 seconds, characters enter every 0.045 seconds, including the word-space beat. Each receives a 0.48-second pop: 0.96 to 1.62 scale, a 0.92 compression, then rest. Pitch moves -24 to +7 to -3 to 0 degrees while yaw counter-rotates. The final exclamation settles at 1.065 seconds.
- Motion off shows the complete still dimensional title. Other text retains its existing rendering. Buttons, star/pillow motion, background and reward accounting are unchanged in this revision.

## Verification

The independent Roslyn compile and live Unity compile completed without errors. [Focused native verification](Verification/title-buttons-checks.txt) passed **167 checks, zero failures**, with zero Unity errors.

The checks read actual rendered glyph meshes at 1080x1920, 720x1280 and 1536x2048. They verify layered depth, changing perspective, scale overshoot/compression, logical character order, a complete final exclamation, settled reduced motion, reusable hierarchy and screen bounds. Every projected bevel and shadow is included in crown-clearance checks; the minimum gap is 12.24 local units. The title uses 6,102 streamed vertices across nine glyphs, below the UI vertex limit and with the existing single font material.

Continue/Close route once, shared primary buttons remain consistent, and normal save keys, runtime resources/level and the Home catalog are restored exactly. Unity is returned to clean Edit Mode in main.unity after the owned QA session. Android/device performance has not been measured in this pass.

The movies are silent native captures, resampled to 30 fps while retaining their recorded wall-time cadence; they are not interpolated 30-fps performance measurements. Mesh motion is sampled separately. The comparison aligns activation only, with no retiming to hide differences. Raw capture frames and compile intermediates remain local; the harness regenerates them.
