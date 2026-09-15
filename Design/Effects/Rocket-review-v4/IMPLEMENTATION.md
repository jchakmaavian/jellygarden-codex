# Five-cell Rocket FireExhaust

The moving exhaust is bounded to five actual board-cell pitches. Gold flame around the white centre gradually becomes warm white, then soft white light and transparency. The tail moves with the rocket instead of remaining attached to the launch cell.

## Scope

Only the rocket exhaust runtime, rocket-only shader and its new white-smoke material changed. Existing original rocket textures, sprite metadata, movement profile, per-cell impact and destruction, debris, other boosters, UI, masks, sorting layers, scene and level data are preserved. The editor regression was extended for this review. `preservation-verification.json` checks 4,828 other asset files and exact cell-contact / screen-exit method preservation.

## Layers and tuning

| Layer | Behaviour |
| --- | --- |
| Nozzle flame | Existing pooled mesh, attached to the rocket, warm yellow/gold edges around a white centre; no dark orange/red edge. |
| Flame body | Existing pooled LineRenderer, normalized UVs, length `min(travelledCells, 5) × axisCellPitch`; turbulence, width taper and longitudinal colour/opacity fade. |
| White core | Additive, 0.28-cell starting width; remains bright through the middle, narrows and fades toward the tail. |
| Outer glow | Additive, 1.12-cell soft starting width; gold becomes warm white with a progressive opacity taper. |
| Star sparkles | Dedicated fixed particle pool; four-point white/gold stars, randomized size, brightness, rotation, lifetime and slight drift. Occasional larger sparkle. |
| Burning flecks | Existing fixed ember pool, small warm gold light particles concentrated near the head/middle. |
| White tail | New soft additive material and fixed particle pool; low-opacity white puffs only in the final roughly 1–1.5 cells. Slight expansion, drift and smooth fade. |

Core/glow lifetime is `5 / actualRocketSpeedInCellsPerSecond`. For an eight-cell board this is about 0.625–0.833 game seconds, depending on the existing launch position and sweep speed. Rocket speed itself is unchanged. Spatial samples every 0.10 cell keep density independent of frame rate and time scale. Particle lifetime budgets include initial offset and rearward drift so old sparks do not create a seven-cell tail. The pooled slot remains occupied until its trails finish. The short startup tail also has a soft cap, and the original borrowed trail gradient is restored on retirement or cancellation so other boosters retain their appearance.

The 0.22-second anticipation, original sprite halves, constant flight speed, offscreen exit, white contact flash, 0.055-second contact delay and existing destruction callbacks are unchanged. Renderer sort orders and the unmasked container are unchanged.

## Review procedure

Open a settled board in Unity Play Mode and run **Tools → Royal Throne → Effects → Verify rocket travel at half and full speed** (Ctrl+Shift+F11). The existing VFX Lab drives actual game speed at 0.5 then 1.0. The suite uses a disposable board and restores the original board/state.

The suite compares 4.5 / 5.0 / 5.5 cells in both directions at both speeds. It measures mature plume length in actual cell units every frame, checks nozzle attachment, measured speed × core/glow lifetime, supported shader, fixed object count and complete particle cleanup. It then exercises horizontal, vertical, blockers, chained rockets, cascades, edge launches, HUD/bar crossings, opposite rockets and repeated non-mutating previews.

Five cells was retained: 4.5 reduces the white tail; 5.5 extends the pale section farther than the supplied reference. Final images and `checks.txt` provide the measured comparison.

The first capture run exposed a test-observation flaw: contact was judged only against the rocket's position at the end of a captured frame. The final test checks the actual nose segment swept between samples, while retaining the independent checks that untouched cells remain intact and every break follows its local flash. Production movement and cell clearing were not altered to address this test issue. `first-run-checks.txt` retains that initial result.

## Source handoff

`rocket-v4.patch` is the focused change relative to the inspected v3 working files in `before/`. `verified-source/` contains the final affected file snapshots with their metadata and the new material. Apply the patch to that existing rocket implementation; these snapshots are not a standalone game or a replacement for the full Unity project. Do not replace the original rocket artwork.

The current working project contains unrelated earlier uncommitted work. The private backup for this revision is a review/source snapshot so that work is not accidentally swept into this focused change.

## Verification status

Final Play Mode regression: **812 checks passed**, at 0.5� then 1�, with no runtime errors during the run. All 12 length/direction/speed combinations measured the requested 4.500 / 5.000 / 5.500 cell extents. The final 5-cell setting passed the full rocket scenarios and shared-pool gradient restoration checks. The original player board, moves, score, coins and lives were restored. QA-disabled C# compilation passed with inherited warnings. Editor screenshots are visual evidence, not a mobile GPU benchmark.
