# Color Ball live integration

Implemented and tested in the independent JellyGarden-Codex Unity project, 20 September 2026.

The live Color Ball + Rocket flow now plays the approved Reveal (60 frames), Idle (120 authored identical frames), and Boosting (108 frames). The charged section is frames 19–78; finish is 79–108. Frame pixels, palette, timing and pivots in `Design/Animation` were not edited. Runtime WebP copies are byte-identical. Their fixed 512px canvas has 112px per board cell: the visible art contains the 3x/1x transitions, so there is no second transform multiplier.

The existing Orb target selection is unchanged. During charged spin, separate pooled beam, glow and impact renderers address each live target. Conversion calls the existing `Item.ChangeType` path using the original Rocket payload orientation. No target is destroyed until the final impact and Color Ball finish have completed. The existing `Hit -> DestroyItem -> Queue -> Rocket` path then owns all Rocket motion, directional art, FireExhaust and cell destruction.

Rendering uses the board sorting layer, unmasked sprites above pieces, and the existing screen-overlay HUD above them. Idle uses one shared texture; effects use a bounded 24-renderer pool, and boosting objects are reused. The Director cancellation path clears Color Ball effects and pooled state.

## Verified

Unity 6000.5.10f1 compiled. The isolated disposable board test passed for horizontal payload at 0.5x and vertical payload at 1x, including:

- immediate Reveal, authored three-cell size, transition to one-cell Idle and a complete Idle loop;
- unmasked, centered Boosting with a valid sprite on every observed rendered frame;
- conversion during charged spin, correct target cell/Item/orientation, no duplicate conversion;
- zero Rocket launches before Boosting finishes, exactly one handoff, every generated Rocket activated exactly once;
- no stale ball/beam/glow, bounded pool reuse, cancellation cleanup;
- no new runtime errors; coins/lives and the original board restored.

`checks.txt` contains sampled assertions (3135 assertions across two cases, not 3135 separate scenarios). The fourteen PNGs are actual Game-view captures. Existing account-service warning was unrelated. No full-game/device performance QA was run.

Run again from a settled Play Mode board: **Tools → Royal Throne → Color Ball → Verify live Rocket conversion only**, or **Ctrl+Shift+F7**.

## Backup / handoff

The live files under `Assets` are already installed. `ColorBall-Live-Integration.zip` contains their exact tested source/assets/metas; it is an integration snapshot for this development copy, not a standalone replacement project. `incremental.patch` records only this task's source edits against the captured pre-task files. Shared Director/test-runner files contain earlier work, so merge that patch when moving to another revision. `install_frames.py` reproduces runtime frame copies from the approved Design sources. Older unused runtime frames are retained to preserve existing references; the new frame contracts never load them.

Rocket source, Rocket VFX, TNT, UI and level data were not edited by this task.
