# King Leo — Area 1 gate restoration, preview v1

**Integration update:** the user subsequently approved implementation. The four states and three clips are now installed; see [integration notes and verification](integration-v1/README.md). The sections below describe the original preview package and its generation-time scope.

Open [the visual review](index.html) in a browser to compare all four generated images and play the three repair animations. All deliverables live in this folder; nothing has been installed in the game.

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

## Repair animation previews

| Objective | Video | Motion |
| --- | --- | --- |
| 1. Fix the timber | [01-fix-timber.mp4](v1/animations/01-fix-timber.mp4) | Old wood clears away; individual oak boards move and rotate into position, then settle |
| 2. Hang the royal banner | [02-hang-banner.mp4](v1/animations/02-hang-banner.mp4) | A rolled banner rises to the rod, unfurls and ripples gently |
| 3. Hang the lights | [03-hang-lights.mp4](v1/animations/03-hang-lights.mp4) | Two lanterns lift onto the hooks, swing, settle and illuminate |

Each individual video is **6 seconds, 720 × 1280, 24 fps, H.264 MP4**, with no audio. [00-full-restoration.mp4](v1/animations/00-full-restoration.mp4) contains the complete **18-second** sequence. There are no camera cuts, and all repairs use the same scene and model.

These are **locally rendered 3D motion studies**, with a simplified scene built to resemble the generated gate. They demonstrate the actual object movement, cloth deformation, lighting and timing. Their geometry and material finish are simpler than the generated stills; they are not generated videos of those exact images. The available Motion video service was not connected, so no service-generated video job completed. The generated stills remain the visual reference for the final art finish.

The optional interactive viewer in the HTML review lets you scrub the full sequence or jump to states at 0, 6, 12 and 18 seconds. It uses a bundled local copy of Three.js and does not need a CDN or network connection. The videos and image gallery work without opening the interactive viewer.

## Review scope

This package is for **Area 1 only**. No Unity scene, gameplay code, story task, importer, or in-game art file was changed for this preview. Hashes of the existing Area 1 images, their metadata and `arc1.json` were recorded before the work and checked afterward.

The three objectives follow the current runtime tasks in `Assets/JellyGarden/Resources/Story/arc1.json`: replace rotted gate timbers, rehang the royal banner, and relight the gate lanterns. The older story document's ivy objective is outside this requested sequence.

Review the gate design, cumulative state consistency, and motion direction before any in-game integration. The MP4s are review renders, not Unity animation clips or production character/prop rigs. No integration is authorized by this preview request.

## Source and reproducibility

- [Generation prompts](v1/prompts.json): exact prompts and generation provenance.
- [Preview scene](v1/source/gate-scene.js): the deterministic 3D motion source.
- [Offline browser bundle](v1/source/gate-preview.js) and [scene viewer](v1/source/scene.html).
- [Build script](v1/source/build-preview.cjs): bundles Three.js 0.180.0 with esbuild 0.25.9.
- [Render script](v1/source/render-preview.cjs): renders deterministic frames through local Chrome and encodes with FFmpeg.
- [Three.js MIT license](v1/source/THREE-LICENSE.txt).
- [Asset manifest and verification](v1/manifest.json).

To rebuild the optional browser bundle, install `three@0.180.0` and `esbuild@0.25.9` outside the Unity project, set `KING_LEO_PREVIEW_NODE_MODULES` to that `node_modules` folder if needed, then run `node v1/source/build-preview.cjs` from this folder. Rendering additionally needs Chrome with local debugging on port 9228 and FFmpeg; run `node v1/source/render-preview.cjs video` once Chrome is running.
