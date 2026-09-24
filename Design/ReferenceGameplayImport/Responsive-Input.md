# Responsive board input — 24 September 2026

Stable gems, Rockets, TNT, Color Balls and Propellers remain tappable/swappable during another move's effects. Independent swaps can animate together. Moving, destroying, matched or conversion-reserved pieces remain protected; popup, pause, blocker and move-limit rules still apply.

Changes:
- `Item.cs`: per-piece input checks and swap leases; atomic cell ownership; charge valid moves once before yielding; defer overlapping hits until the pair commits.
- `LevelManager.cs`: allow input during resolution, keep scans/gravity synchronized with active swaps, process newly submitted matches before refill, and remove the competing tap-release reset.
- `CombineManager.cs` / `RoyalPatterns.cs`: retain the preferred booster-creation cell for each concurrent swap.
- `RoyalPowerupDirector.cs`: use the same tap eligibility, reserve only Color Ball-selected pieces, release reservations on completion and cancellation.
- Editor-only `ResponsiveInputChecks.cs` and inbox commands: focused repeatable regression checks, including actual Input System press/move/release events using an isolated virtual mouse.

Validation: runtime and Editor compilation pass. Play Mode covers same-frame two-Rocket/TNT taps, concurrent swaps, normal matching during TNT, overlapping hits, rejected swaps, Color Ball interactions and Rocket combo, cancellation, pause/popups, blockers, falling pieces, last-move protection, unique ownership, settlement and normal-save isolation. Final pointer/cancellation run reports zero errors. See `Validation/Responsive-Input.txt` and `Validation/Responsive-Input-Status.json`.

The earlier diagnostic log records a virtual-pointer test synchronization/device-isolation issue and the subsequently fixed cancellation lease issue. It is retained as `Validation/Responsive-Input-Diagnostics.txt`; the final report contains the passing runs. No Android or full-game QA was performed.

Pre-change source checkpoint: `Design/Backup/Responsive-Board-Input-20260924-143537/`.
No artwork, effect timing, levels, UI layout or normal player-save data changed for this task. Existing unrelated edits remain in place.
