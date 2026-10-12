# Reference campaign 01â€“50

The independent Unity copy retains its original `Resources/Levels` and `Resources/Targets` assets and their metadata. The inspected campaign is a separate resource, selected using **GAME TESTING MODE â†’ Start test profile â†’ LEVEL TESTING â†’ Reference campaign 01â€“50**, then **Open level**. Returning to **Original levels** or ending the test session restores the original campaign selection.

Reference progress uses `RoyalThrone.QA.Reference50.v1/` keys. Its level stars and scores start separately from normal-player progress. Normal resources, volume, random state, refill timer and level selection are restored by the existing QA session lifecycle. Home selection and next-level entry are bounded at level 50 while the reference campaign is selected.

## Verified source data

`reference-levels01-50-inventory.json` records exact payload hashes and Unity TextAsset offsets. `reference-schema-evidence.json` records field order, metadata type/method indices and numeric enums extracted from the APK's own IL2CPP v31 metadata. `reference-levels01-50-decoded.json` preserves complete authored values. The compact CSV and 50 layout documents provide a readable review.

All 50 payload hashes and grid item/cell dimensions passed validation. All 109 authored Cupboard/Potion footprints confirm row-major, bottom-up source order. Unity cells use the reversed row order. All 84 applicable static goal totals agree with their board contents. Normal Box goals count boxes, not hit layers; Grass goals count occupied cells, not layers. A Cupboard footprint contributes 10 goal units and a Potion footprint contributes 4.

## Explicit inspection adaptations

The metadata and board payloads establish layouts, moves, goals, named blocker codes, colors, feed entries and set ratios. They do not establish every runtime rule. The playable QA implementation therefore makes these differences explicit:

- Box1/2/3 use 1/2/3 impacts and credit one Box goal on removal. Grass1/2 use 1/2 impacts and credit one Grass cell on removal.
- Cupboards and Potions share state across their four cells. They credit one authored unit per contact wave, release after 10/4 units, and deduplicate repeated contacts during a match wave.
- Fixed mailboxes emit one Mail unit per contact wave until the authored Mail goal is reached. Eggs are falling nonmatch items removed by neighboring matches or blasts.
- Above-board predefined feed entries are consumed by increasing source y within each column, before weighted spawn sets. Set-level ratios provide weights; elements within a set are uniform. Exact source refill consumption, weighting and fallback policy remain unverified.
- Bonus levels 20/40 contain 79/70 coins and two TNT/color balls with 20 moves. The source Coin goal is literally zero. This build shows available coins, lets the existing impacts collect them, and completes at the end of 20 moves. This finish rule is an adaptation, not a source-verified bonus rule.
- The source campaign has no score or star thresholds. Completion uses one QA star for the existing unlock system; it is not presented as recovered scoring balance.
- Existing rocket, TNT, propeller and color-ball execution paths remain in use. The source's predefined starting power-up codes map to those existing types.

## Validation entrypoints

`RoyalCampaignChecks.Data()` validates all normalized resources. `Batch01`, `Batch11`, `Batch21`, `Batch31`, `Batch41` run from Play Mode with no existing QA session. Each creates a disposable campaign profile, opens its 10 levels, checks dimensions, holes, fixed colors, goals, layers, shared footprints and settled ownership, then exercises real blocker hit/destruction paths. Normal-player save keys are compared before/after and the test profile is ended. Reports are written under `Validation/`.

Passing these checks establishes data/runtime integration and focused obstacle behavior. It does not establish human difficulty, solvability, exact reference behavior or device performance; those require gameplay validation.

## Dedicated Android build and complete check run

`RoyalAndroidBuild.BuildLevels01To50Test()` passes `ROYAL_REFERENCE_CAMPAIGN` only to that player build. Its separate Android application ID preserves the existing installation and data. After the splash enters the game scene, the bootstrap starts the separate reference profile and selects the 50-level campaign automatically. The ordinary build does not include this bootstrap behavior.

In Play Mode, **Ctrl+Alt+Shift+F6** (Tools → Royal Throne → Campaign → Run all 50 checks) runs data verification and the five batches sequentially. `Validation/status.txt` names the current level/batch and `all-batches.txt` records completed batches. Concurrent runs are rejected. Each level also performs a production swap or power action and verifies move spending, scoring, clear/refill settlement and item ownership. Each batch exercises failure/give-up/retry, a prepared final-move win through the full production celebration, life reward, saved completion/score and next-level unlock. Both bonus boards are checked against immediate accidental wins and against the documented 20-move finish rule. These are integration/flow tests with prepared outcome fixtures, not claims of unassisted 50-level wins or pointer-input testing.
