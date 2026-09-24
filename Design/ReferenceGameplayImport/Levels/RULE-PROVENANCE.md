# Levels 01–50: imported data and incomplete runtime equivalence

The import reuses the previously extracted APK payloads, decoded schema and current campaign loader. It does not introduce new damage, refill, targeting, reward or combination rules.

## Imported and source-checked

- Fifty actual packaged TextAssets, including exact authored names, hashes and serialized path IDs.
- Board width/height, holes, fixed starting item codes, grass overlay codes, fill flags and predefined flags.
- Moves and goal names/counts, including the literal zero Coin goals on levels 20 and 40.
- Color palettes, predefined above-board entries and authored spawn sets/ratios.
- Source mapping for initial Propeller, horizontal/vertical Rocket, TNT and Color Ball codes.

`import_levels.py` compares every runtime-projection field against the existing full decoded source, every raw payload hash, and the original TextAsset bytes in `resources.assets`. It also checks that omitted blocker overlays and set limits are empty/default for all 50 levels. `source-parity.json` records this check. Full raw and decoded source files stay in `Design/Inspection-Levels01-50` rather than being duplicated into runtime assets.

## Existing reconstructed behavior: not verified reference logic

| Topic | Current Royal Throne behavior | Missing reference evidence |
| --- | --- | --- |
| Matching, swaps, gravity and cascades | Existing engine paths and match/booster creation rules | Exact native reference execution, selection priority and timing |
| Box / Grass tiers | 1/2/3 and 1/2 impacts with one goal per removed object/cell | Per-wave damage and adjacency semantics |
| Cupboard / Potion | Shared 2×2 state depleted over 10/4 contacts | Goal totals/footprints are authored; exact durability and discharge sequence are not established |
| Mailbox | Fixed obstacle credits one Mail per hit wave | Reference production cadence and simultaneous-contact handling |
| Egg | Falling nonmatch item collected by adjacent matches or direct blast | Reference damage/collection rules |
| Refill | Above-board entries consumed in increasing source y, then set weights; uniform elements within a set | Native consumption, default weight meaning, fallback and fill policies |
| Bonus levels 20 / 40 | Existing test adaptation displays available coins and ends after 20 moves | Packaged Coin goal is zero; native finish/reward rule is not recovered |
| Rewards / progression | Existing engine and separate QA campaign progress; one QA star | Reference scoring, coin rewards, star thresholds and unlock policies |
| Booster targeting / combinations | Existing Royal Throne systems | Exact reference targeting, order and combination rules |

These are incomplete equivalence items, not claims of imported native logic. The original game executable is IL2CPP; the recovered metadata identifies data schemas but is not editable gameplay source. Offline extraction alone has not established those runtime policies. No new guessed rules were added by this import.

## Runtime connection and protection

`RoyalReferenceCampaign.ResourcePath` points to `ReferenceGameplay/levels01-50`. The existing separate reference QA campaign loads it through the main `LevelManager` board, spawner and blocker hooks. Normal campaign resources and saves are retained. Existing `RoyalCampaignChecks` provides the five 10-level Play Mode batches. Data checks are distinct from playability/solvability and do not substitute for those batch results.

No homepage, branding, Settings, BGM, character, service or APK changes are part of this level import.

## Imported blocker feedback

Recovered Box, Grass, Cupboard, Potion and Egg particle groups and corresponding native SFX are connected at the existing accepted damage/clear events. Box particles select only the current damaged tier. This connects recovered presentation data; it does not establish reference-equivalent damage staging.

Native composite blockers omit the old generated numeric durability labels. Their existing health and goal logic is unchanged. Legacy fallback visuals retain their existing labels.

Remaining presentation gaps: Cupboard door mesh/shader feedback and door-versus-plate state selection; Potion bottle population/colour selection (only common glass/cork fragments are played for bottle hits); mailbox lid/mail animation and particles; coin collection particles/flight timing; and the exact Grass2 composite state. These are explicitly incomplete rather than replaced with generated effects.

### Completion-only stabilization found during fresh validation (2026-09-24)

Level 40's existing two-color refill kept generating new matches and boosters after the game had already entered `PreWinAnimations`. The campaign-specific `RegenMatches(true)` shortcut bypassed the original engine's stable completion refill in that state. Diagnostics showed 45 seconds of continuing activity with nearly equal real and simulation time; this was not a slow-frame timeout. The shortcut now excludes `PreWinAnimations`, restoring the existing engine completion path. Active gameplay cascades, source palette/data, rewards and native effect timings are unchanged. This is a repair to Royal Throne's reconstructed completion behavior, not evidence of the APK's editable runtime implementation.
