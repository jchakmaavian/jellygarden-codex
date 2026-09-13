# Royal Storybook production layers

The decorative outer gold screen frame has since been removed at the owner's
request. The comparison captures below predate that border-only change.

[Compare the four screens and inspect the transparent assets](index.html).

The Unity screens now consume 33 individual RGBA sprites from
`Assets/JellyGarden/Resources/Story/TreasureHunt/UI/Layers/`.

- Home: illustrated wallet badges, progress badge, next-task card, blue button
  and navigation dock, alongside the original title and Pip medallion.
- Area Tasks: complete parchment sheet and the original row surfaces.
- Before Level: complete paper-and-ribbon layer, recessed goal well, blue score
  token, booster tiles, original illustrations and Play button.
- Level Complete: complete parchment with the original rays and bottom curls,
  plus separate original Pip, title ribbon, star and return button.

Numbers, task text, progress, costs and inventory remain native UI text. Buttons
retain their actual handlers, order/affordability checks and replay guards. The
four Area 1 repair transitions and shortened dialogue remain in place.

This integrates the artwork; it is not a pixel-identical reproduction of the
entire concept screenshot. The existing restoration paintings and playable
board geometry/pieces remain in use. Text uses the installed fonts, and phone
aspect ratios differ. The actual board is shorter than the concept board, so a
modal covers much of it. The normal home-to-level popup shows home behind it.

## Sources and packaging

Original: `../ui-directions/v1/01-royal-storybook.png`, also used by the approved
HTML. Pip, icons, fixed victory lettering, star and fixed buttons are cropped
from this original, without regenerating their artwork.

The **built-in imagegen tool** cleared foreground content from the surfaces.
Accepted source edits and exact prompts are stored together:

- `source/clean-board.png` and `source/prompt.txt`.
- `source/clean-board-v2.png` and `source/prompt-v2.txt`.
- `source/controls-clean.png` and `source/controls-prompt.txt`.

These are RGB source sheets. `StorybookLayerBuilder.Run` packages their
silhouettes as actual RGBA PNGs using a 4×4 coverage mask. Original illustrations
are packaged from their recorded source outlines. For cleared small icons, the
matching empty surface also removes background pixels connected to the exterior,
preserving enclosed highlights. The final panel edit returned
1702 columns; the exporter normalizes that one-column difference to the original
1701-coordinate layout. The full edited sheets are not drawn as game screens.

```sh
"$UNITY" -batchmode -quit -nographics -projectPath "$SCRATCH_PROJECT" \
  -executeMethod JellyGarden.Scripts.Story.Editor.StorybookLayerBuilder.Run \
  -storybook-source=/absolute/path/to/king-leo -logFile /private/tmp/layers-export.log
```

Copy the output `UI/Layers` PNGs and metadata back together. Every output is
checked for opaque artwork and fully transparent exterior pixels.

## Verification

The visual harness buys the wooden door through the task row, captures the four
screens at 393×852 and 320×568, and wins board 3 using legal moves. Latest captures
and report: `verified/`. Surrounding-menu captures/report: `menus/`.

```sh
tools/run_player.sh --exec -treasure-review -treasure-review-fidelity \
  -treasure-review-dir=/private/tmp/storybook-layer-review \
  -treasure-review-save=/absolute/path/to/king-leo/art-review/treasure-hunt/royal-storybook-runtime/v2/normal/earned-before-task-2.json
```

All review saves are isolated from normal progress. `checks/` records the Unity
interaction tests, typecheck and release-build summaries. The release player has
no development watermark.

Final results: both real-player reports passed with no errors; Area 2 remains
locked. Unity interaction checks passed, typecheck had zero errors, and the
release build had zero errors (348 existing warnings). The 17 content tests,
kit VFX guard and windowed-launch guard passed. All eight four-screen comparison
captures and all 33 alpha sprites were checked. Installed `Builds/Mac/KingLeo.app`
matches the reviewed scratch build by checksum and was reopened through
`tools/run_player.sh` at 393×852.
