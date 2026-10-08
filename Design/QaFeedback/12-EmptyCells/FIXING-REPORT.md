# Persistent empty playable cells under blockers

8 October 2026.

## Reproduced cause

The reported screenshot has an empty left-edge cell directly below a roof of wooden blockers. In an isolated practice board with solid cells at (0,3) and (1,3), an empty playable cell at (0,4) remained empty after six seconds. The resolver had finished and input was unlocked: `empty=True resolving=False blocked=False`.

Normal spawning intentionally skips cells with solid blockers above them. Diagonal falling cannot reach a cell when both its vertical and diagonal entrances are blocked. The resolver previously accepted that state as settled.

## Change

After normal falling, refill and diagonal movement finish, the resolver fills any remaining unoccupied playable cells before ending. The new pieces initialize and fall, and any resulting matches run through the normal resolver.

The fallback preserves reserved special-creation cells, existing pieces, blockers and board voids. New gem entry is aligned to the top of its passable column segment, including segments beneath blockers. Temporary gaps during clearing/falling are normal; a settled playable cell must be occupied.

## Verification scope

Blocked-refill fixtures at 15, 30 and 60 FPS cover left and right edges, a middle roof, multiple stacked holes, an available diagonal route, a top-row vacancy and an intentional board void. The checks require full playable-cell occupancy, preserved blockers/voids, resolver completion, unique reciprocal ownership and no watchdog repairs. A separate check confirms a reserved special-creation cell is not filled.

The corrected four-gem Propeller suite and rapid-swap/overlap regression suite are also run. Tests use isolated QA saves; normal balances and lives are restored on completion. Physical-phone QA and a rebuilt Android APK are outside this verification.

Result: **572 checks passed, zero failures** across the blocked-refill, four-gem Propeller and overlap/rapid-swap suites. Normal balances and lives were restored.

Visual reproduction: [before](before.png) and [after](after.png). The formerly empty cell is filled and the resolver is settled. Console errors: zero.
