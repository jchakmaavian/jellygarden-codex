# King Leo — Area 1 gate restoration, preview v1

**Video removal — 2026-09-12:** All three installed repair clips and all four MP4 previews, including the full restoration, were removed at the owner's request. The still artwork remains. For the historical integration, see [integration notes and verification](integration-v1/README.md). The sections below describe the original preview package and its generation-time scope.

Open [the visual review](index.html) to compare the four archived generated images. The removed video players and download links have been taken out of that page.

The sequence uses one gate throughout: a damaged oak door is repaired, a royal banner is hung above that repaired door, then two warm brass lanterns are hung beside it. Each completed repair persists into the next objective.

## Generated image assets

The art uses warm sandstone, honey oak, blue roofs and cloth, antique gold, and amber lantern light. The camera, doorway, stonework and surroundings remain visually consistent across successive edits.

| State | Asset | Visible restoration |
| --- | --- | --- |
| 0 | [Broken gate](v1/states/00-broken.png) | Damaged timber; empty banner rod and lantern hooks |
| 1 | [Timber fixed](v1/states/01-timber-fixed.png) | Repaired closed oak door; loose timber removed |
| 2 | [Banner hung](v1/states/02-banner-hung.png) | Repaired door plus a blue and gold crown banner |
| 3 | [Lights hung](v1/states/03-lights-hung.png) | Repaired door and banner plus two lit brass lanterns |

All four are opaque PNGs at **941 × 1672**. These are complete environment images, not isolated sprites or transparent layers. They were generated using the **built-in image_gen tool**, with states 1–3 created as sequential edits. The exact prompt set is saved in [v1/prompts.json](v1/prompts.json). The original outputs have been copied into this folder without image retouching.

## Removed repair animation previews

The former timber, banner, lantern, and full-restoration MP4s have been deleted. Their historical dimensions, hashes, and removal status remain in [the manifest](v1/manifest.json). Archived rendering source is retained for provenance; it is not used by the game or the review gallery.

## Review scope

This package is for **Area 1 only**. No Unity scene, gameplay code, story task, importer, or in-game art file was changed for this preview. Hashes of the existing Area 1 images, their metadata and `arc1.json` were recorded before the work and checked afterward.

The three objectives follow the current runtime tasks in `Assets/JellyGarden/Resources/Story/arc1.json`: replace rotted gate timbers, rehang the royal banner, and relight the gate lanterns. The older story document's ivy objective is outside this requested sequence.

The original preview and integration history below predates the removal of the older videos.

## Source and reproducibility

- [Generation prompts](v1/prompts.json): exact prompts and generation provenance.
- [Preview scene](v1/source/gate-scene.js): the deterministic 3D motion source.
- [Offline browser bundle](v1/source/gate-preview.js) and [scene viewer](v1/source/scene.html).
- [Build script](v1/source/build-preview.cjs): bundles Three.js 0.180.0 with esbuild 0.25.9.
- [Render script](v1/source/render-preview.cjs): renders deterministic frames through local Chrome and encodes with FFmpeg.
- [Three.js MIT license](v1/source/THREE-LICENSE.txt).
- [Asset manifest and verification](v1/manifest.json).

To rebuild the optional browser bundle, install `three@0.180.0` and `esbuild@0.25.9` outside the Unity project, set `KING_LEO_PREVIEW_NODE_MODULES` to that `node_modules` folder if needed, then run `node v1/source/build-preview.cjs` from this folder. Rendering additionally needs Chrome with local debugging on port 9228 and FFmpeg; run `node v1/source/render-preview.cjs video` once Chrome is running.
