# Brighter, more expressive reward lettering

This revision follows the user's request to improve the title after comparing v7 with the supplied Royal Match recording. V7 is preserved for comparison. The title keeps dimensional gold edges while moving away from the heavy metallic-plaque appearance.

- Cream faces replace the yellow-gold face gradient. A stronger navy keyline separates the face from a bright gold bevel.
- Side depth is reduced from 7.5 to 3.6 units, with a smaller, lighter cast shadow. Per-glyph proportions are 4% narrower and 8% taller; the type size is 60.
- The wave starts at 0.18 seconds, staggers characters by 0.04 seconds and gives each glyph a 0.40-second grow/compress/settle. Peak scale is 1.72, compression 0.88. Reduced pitch/yaw keeps the wave readable and lively.
- A glyph appears fully opaque as one unit. This removes the brief brown flash caused by separately blending many partially transparent depth layers before its cream face became opaque.
- The final exclamation is 10% larger at rest, angled by eight degrees, and peaks at 1.82 times its resting size. A shallow six-unit dip through its bounce protects crown clearance. Its complete bounce ends at 0.94 seconds, versus 1.065 seconds in v7.
- The title sits at row 196 to allow its taller peaks. Other text, the card shell, star/pillow, buttons, audio and reward accounting are unchanged by this revision.

## Review

- [Actual Unity animation](Royal-Throne-v8-Unity-Card.mp4)
- [Royal Match reference versus Unity](Royal-Throne-v8-Reference-vs-Unity-Title.mp4)
- [Six-stage title comparison](Royal-Throne-v8-Title-Wave-Comparison.png)

These are silent native Unity captures. The comparison aligns activation without changing either animation's speed. Capture cadence is recorded in the timing data; a 30-fps export repeats captured frames and is not a device-performance measurement. Royal Throne's own font remains in use.

## Verification

The independent compile and live Unity compile passed without errors. **167 native checks passed, zero failed, with zero Unity errors.** Results are recorded in [the focused verification log](Verification/title-buttons-checks.txt). The harness checks three portrait dimensions, complete silhouettes against the crown, visible depth, changing perspective, the final exclamation's larger peak, reduced motion, reuse, and Continue/Close routing. Minimum crown clearance was 4.50 local units, including bevels and shadows. Normal save keys, runtime resources/level and the Home catalog were restored exactly. The editor was returned to clean Edit Mode in main.unity. Android device performance has not been tested in this pass.
