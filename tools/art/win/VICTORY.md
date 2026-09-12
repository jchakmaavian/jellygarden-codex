# Royal victory card

Approved direction: crowned blue victory ribbon, ivory/gold card, one bright
gold star, a positive reward badge and a large green Continue button.

New masters (built-in imagegen, using the approved chat concept as reference):

- `victory_banner.png`: isolated royal-blue gently arched satin ribbon, folded
  tails and polished gold rims, small central gold crown with a red diamond;
  transparent background, blank writing area, no lettering or external shadow.
- `victory_headline.png`: separate transparent “Level Complete!” wordmark, cream
  faces and fine gold/navy edges, following the ribbon's upward arch.
- `victory_star.png`: isolated front-facing five-point gold reward star, rounded
  tips, thick amber bevel, luminous smooth face and polished highlights;
  transparent background, no external glow, confetti or shadow.
- `victory_button.png`: isolated wide green capsule, lime upper face and emerald
  lower face, rounded gold rim, soft gloss; transparent background, no lettering
  or arrow, no external shadow.

The prompts requested separate production sprites faithful to the approved
mobile-game mockup, with no other UI objects. Generation used the built-in
image tool, not the API/CLI fallback. Original generated files are preserved;
these masters are copied into `Assets/JellyGarden/Resources/Story/Art/` by:

```sh
python3 tools/win_art.py
```

The existing panel remains nine-sliced. New sprites render whole with preserved
aspect ratios. The headline is a painted Image parented to the ribbon; level
number, reward copy, Continue, checkmark and arrow are live Unity UI; the star's halo,
rays, confetti, glints and animation are runtime UI geometry. No reward amounts
are painted into the art.

The curved headline replaces the straight Unity Text label. Its full generation
prompts and source are in [reference/headline-prompts.md](reference/headline-prompts.md).
The built-in generator returned an RGB checkerboard preview; the matte is
converted to alpha during asset preparation, including the holes in the letters:

```sh
swift tools/win_headline.swift tools/art/win/reference/headline-generated.png tools/art/win/victory_headline.png
python3 tools/win_art.py
```

## Final generation prompts

All three calls used the approved chat mockup as `referenced_image_paths`.

### Star

Use case: product-mockup. Production game UI sprite. Input image is a style reference for the approved King Leo victory card. Generate ONLY its single chunky five-point golden reward star as an isolated sprite, front facing, symmetric, rounded tips, thick warm amber/gold bevel, smooth luminous yellow gold face, dimensional polished soft 3D casual mobile puzzle game finish. Faithful to the star in the reference. Center it, fill 92 percent of a square canvas. Truly transparent RGBA background. NO halo, NO glow outside its silhouette, NO confetti, NO particles, NO shadow outside silhouette, no lettering, no frame, no extra items. These effects will be animated separately in Unity. Smooth highlights and subtle facets, not angular low-poly polygons. Output one 1024x1024 asset.

### Banner

Use case: product-mockup. Production game UI sprite. Input image is the approved visual reference. Generate ONLY the royal blue victory ribbon WITH the small gold crown attached centered above it, like the header in the reference, but REMOVE ALL LETTERING so Unity can render live text. An elegant gently arched wide blue satin banner with folded tails, polished gold rims, small rounded gold crown with a single red diamond jewel, warm friendly 3D casual puzzle game finish. Empty dark royal blue writing area occupies central 78 percent of width and middle/lower 40 percent of total height. Crown occupies central top 25 percent of total height. Isolated on truly transparent RGBA background. No panel, no star, no UI, no words, no shadow outside the silhouette. Entire asset fully visible with only 2 percent transparent outer padding, width to height about 2.8 to 1. Output one landscape asset.

### Button

Use case: product-mockup. Production game UI sprite. Input image is the approved visual reference. Generate ONLY its vibrant green pill-shaped Continue button face, WITHOUT any text or arrow. Chunky rounded capsule, lime green top transitioning to richer emerald green lower half, smooth glossy highlights upper left, rounded dimensional gold outer rim, friendly polished 3D casual puzzle game style matching reference. Front facing symmetric long capsule width to height about 3.4 to 1. Fill canvas with minimum padding, isolated on truly transparent RGBA background. No text, no arrow, no crown, no star, no external shadow, no other objects. Output one landscape asset.
