# Artwork provenance

The shipped `Assets/JellyGarden/Resources/Story/TreasureHunt/UI/storybook_reference.png` is a byte-for-byte copy of the approved `ui-directions/v1/01-royal-storybook.png`. Both SHA-256 hashes are `65ca988cc6ec2e1491e6326d123634d1ebf6675e724f9c42b05eee8785cabf61`.

`StorybookArt` creates native sprite cutouts and panel edge strips from this atlas. It does not replace playable screens with screenshots. Live level numbers, reward state, task cost, progress, booster inventory and actions remain in Unity UI.

Dynamic display lettering uses [Bree Serif from Google Fonts](https://github.com/google/fonts/tree/main/ofl/breeserif); its font, OFL license and FONTLOG are included under `Resources/Story/Fonts`. Fixed illustrated ribbon/button text is taken directly from the approved artwork.

An extraction trial used the built-in image-generation tool. Its output was discarded because it changed details and painted a checkerboard instead of alpha transparency. No generated candidate is referenced by the game. Trial prompt:

```text
Use case: background-extraction / precise-object-edit.
Asset type: production Unity UI transparent victory-panel sprite, faithful extraction from the supplied Royal Storybook concept. Input image is the exact approved visual design, NOT a loose style reference.
Primary request: extract ONLY the popup illustrated assembly from the FOURTH/rightmost phone labelled LEVEL COMPLETE. Reproduce its design EXACTLY at higher resolution, not redesign it.
Include the large smiling cream/gold paper dragon Pip with blue map wings extending out to the sides, upper body peeking over the curved royal blue ribbon. Preserve Pip's exact face/pose/size from the FOURTH phone (large face, cropped torso, no full dangling tail/body). Include the SAME folded blue ribbon with swallowtail ends and fine gold edging and exact white, navy-shadowed chunky serif title “Level Complete!” following the arch. Include the SAME warm ivory parchment card below, thin inset gold double edges, softly curled/folded BOTH bottom corners. Include the SAME glossy deep royal-blue gold-rim pill button at the bottom reading “Back to Castle” in the same white chunky serif lettering.
Remove ONLY the central level label “Level 3”, the central gold reward star and its rays/sparkles, and “+1 Star” so the central cream area is BLANK for dynamic in-game content. Retain spacing reserved for all three. Keep a subtle cream-to-warm-ivory paper gradient. No other text or numbers.
Transparent background: remove the phone frame, surrounding headings, all board gems, blurred board, and all other phones. Actual alpha transparency surrounding the popup silhouette and around Pip's horns/wings, NOT a painted white/black/checkerboard background. No new crown atop the ribbon, no extra decorations. Preserve the original popup's proportions (width approximately 385 relative to complete assembly height 620). Keep 16px transparent padding around the silhouette. Portrait output, high-resolution detailed game illustration, clean edges.
```
