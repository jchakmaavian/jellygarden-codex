# King Leo — first board art collection

Open [index.html](index.html) in a browser. The page works directly from disk; all artwork, fonts, styles, and scripts are local.

This is **direction batch v1**, approved and installed: six normal pieces, two rocket directions, one area bomb, one prism orb, and one swap glove. The 11 designs are installed in 29 existing PNG slots: six normal pieces, eighteen rocket/bomb variants, one prism orb, one framed glove button, and three pre-level booster icons. Sprite metadata, scene/prefab references, and gameplay code were preserved.

## Review

- Compare current and proposed art on identical classic, dense, or irregular boards.
- Select any of the six arc backdrops; inspect warm ivory, current arc fill, dark blue, and transparency-grid surfaces.
- Switch to grayscale to compare silhouettes.
- Inspect each candidate at 24, 32, 48, and 64 px or open its full-size image.
- Mark Keep, Change, or Undecided, and add notes. Browser storage retains notes when available; export them as Markdown to keep/share a reliable copy.

Review controls remain available for feedback on future revisions. This approved batch is installed. Additional art families remain outside this installation.

## Files

| Location | Contents |
| --- | --- |
| [candidates/v1/](candidates/v1/) | Eleven original generated transparent RGBA PNG masters, 1254 × 1254 px |
| [candidates/v1/exports-128/](candidates/v1/exports-128/) | Eleven 128 × 128 px downsampled previews, preserving alpha |
| [current/](current/) | Copies of the current eleven counterpart files for a stable comparison |
| [context/](context/) | Existing HUD, portrait, blockers, pre-level booster icons, backgrounds, and local fonts |
| [prompts.json](prompts.json) | Complete common art brief and individual prompts |
| [generation-record.json](generation-record.json) | Built-in image-generation output provenance and exact prompts |
| [manifest.json](manifest.json) | Candidate paths, measured dimensions/alpha, hashes, and eventual old-file mapping |
| [preview-desktop.png](preview-desktop.png) | Browser capture at 1400 px wide |
| [preview-board.png](preview-board.png) | Full current/proposed board comparison |
| [preview-collection.png](preview-collection.png) | Current/candidate comparison cards |
| [preview-mobile.png](preview-mobile.png) | Browser capture at 393 px wide |
| [preview-mobile-board.png](preview-mobile-board.png) | Proposed board at 393 px wide |
| [verification.json](verification.json) | Browser validation results |

The built-in `image_gen` tool generated each object individually. The requested size was 1024 square; returned masters are 1254 square and are retained unchanged. Preview exports only downsample the masters; the approved exports now replace the existing project sprites listed in the installation record. These are flattened raster masters, not layered source files.

## Boundaries of this review

The boards are HTML art compositions, not a running level. Both sides use the same grid, HUD art, existing blockers, and chosen background. The HUD layout is recreated for context. Arc fill previews use the project's colour/alpha values but do not reproduce Unity's entire rendering pipeline.

The original glove snapshot includes its blue/gold button plate; the approved master is an isolated icon. The UI calls out that distinction. The installed glove uses the fitted blue-and-gold button composite linked below. The pre-level comparison preserves the old candy badges beside the matching power-up icons now installed in their place. Other button families remain for a later batch.

The new crown and coin both use gold: review their separation at the smallest sizes. The horizontal and vertical rockets use the same material family, with generation-specific details that should be reviewed as a pair. Blocker redesigns, collectibles, other boosters, and animated effects are still planned, not complete.

See the [full inventory and phased art plan](../../docs/king-leo-board-assets-plan.md).

## Installation verification

- [In-game Unity capture](integration-v1/unity-installed.png): the new pieces and framed glove in the running Game view.
- [Installation record](integration-v1/installation.json): all 29 file mappings, before/after hashes, metadata hashes, and backup paths.
- [Glove button master](integration-v1/glove-button-master.png) and [exact compositing prompt](integration-v1/glove-button-generation.json), produced with built-in image generation.
- Unity's Assets → Refresh imported every changed texture. Dimensions, all `.meta` files, `game.unity`, and `Item.prefab` were verified unchanged.
- `tools/verify_pieces.py` passed. It warned about the warmer leaf versus its existing particle tint and similar coin/crown hues; the silhouette and colour-distance checks passed.
- The HTML and earlier preview screenshots remain an art comparison against the original snapshots. Use the Unity capture for the installed appearance.
