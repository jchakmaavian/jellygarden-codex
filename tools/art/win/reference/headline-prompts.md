# Curved victory headline generation

Generated with the built-in imagegen tool. The final generated RGB file is `headline-generated.png`; `../victory_headline.png` is the transparent production master. The generator returned a painted neutral checkerboard, so `tools/win_headline.swift` converts that matte to alpha and trims empty padding for import. Existing alpha is preserved when supplied.

## Initial lettering prompt

Use case: stylized-concept.
Asset type: a transparent PNG typography sprite for an existing mobile match-3 victory ribbon.
Input image: the attached blue-and-gold crowned ribbon is a REFERENCE ONLY, showing the exact curve the lettering must follow.
Create ONLY the lettering, with genuine transparent alpha around and between the letters. Do not include or redraw the ribbon, crown, blue background, a plaque, or any decoration.
Text (verbatim, one line): "Level Complete!"
Friendly bold rounded chunky display lettering, title case exactly as written. Warm ivory/cream letter faces, very subtle golden bevel, a narrow warm-gold edge and restrained dark navy depth/shadow so it reads crisply over sapphire blue. Bright polished cartoon game UI.
The entire wordmark follows a smooth shallow UPWARD arch: the CENTER of the phrase is raised and BOTH ENDS sit lower, matching the curved ribbon reference. The baseline at the phrase's middle is approximately 6% of the total lettering width ABOVE the baseline at the two ends. Each letter gently rotates with the tangent of this arc, middle letters upright, left end tilting slightly counterclockwise, right end tilting slightly clockwise. Both top and bottom of the lettering follow this arch, NOT a straight baseline. Keep glyphs bold, compact, readable at phone size. No dramatic rainbow curve.
Wide horizontal composition, whole phrase centered, consistent spacing and cap height, comfortable transparent padding on all sides. No other text, no watermark, no sparkles, no extra icons.

## Transparency correction request

Use case: background-extraction. Edit the provided lettering asset. Preserve exactly the curved "Level Complete!" wordmark, its cream/gold faces, navy depth, and letter shapes. Remove the entire gray checkerboard and all background artifacts, INCLUDING spaces between letters and inside closed letters. The checkerboard is currently painted into the source and must be removed. Output an actual RGBA PNG with transparent alpha (alpha 0 outside the lettering), NOT a drawing of a checkerboard, NOT a gray or white matte, NOT a screenshot. Keep the wordmark fully visible and its curve unchanged. Tight transparent padding, wide horizontal asset, no new elements.

## Final refinement prompt

Use case: precise-object-edit. Input 1 is the existing curved "Level Complete!" lettering to refine. Input 2 is the actual ribbon it must fit on, REFERENCE ONLY. Output only the refined lettering, never the ribbon.
Make the lettering MUCH SHALLOWER AND MORE COMPACT so it fits inside the ribbon's narrow blue writing band. Exact text "Level Complete!" on one line, with a clear space between words. Keep friendly bold rounded cream letters, fine warm gold rim. Reduce the navy extrusion/shadow to a very thin 4-pixel dark outline at this resolution, not a thick slab.
Precise shape target: if the wordmark is 2000 pixels wide, capital glyph height is approximately 165 pixels; the overall ink height, including the arch, is approximately 235 pixels. Baseline at the center is ONLY 60 pixels above the baseline at the two ends (3% of width). A gently curved upward arc, not a rainbow arch. All capitals equal height; "C" must not be oversized. Left-end letter tilt about -7 degrees, middle upright, right end +7 degrees. Maintain that shallow arc through the top and bottom of the line.
Cream faces should be mostly flat bright ivory with a subtle bevel, matching the clean mobile UI reference. Keep all letters fully visible. Actual transparent background with alpha; no painted checkerboard, no icons, no crown, no ribbon, no backdrop.

