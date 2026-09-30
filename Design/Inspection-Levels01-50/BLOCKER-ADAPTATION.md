# Campaign blocker implementation

This implementation is enabled only on the isolated reference campaign. Existing normal-player levels retain the inherited Square/Item path. Original Royal Throne artwork is loaded from `Resources/RoyalCampaignArt`; APK art is inspection material only.

## Verified data and implemented rules

| Family | Verified from packaged level data | Royal Throne behavior implemented |
| --- | --- | --- |
| Boxes | Codes 21/22/23 identify Box1/2/3; each occupied box contributes one Box goal | One layer per hit wave; one goal when its final layer clears; opens its square for gravity |
| Grass | Authored Grass1/2 cell overlays; one Grass goal per cell | One/two direct clearing hits under the item; goal on final layer; pieces can fall over it |
| Cupboard | Codes 41/42/51/52 form a shared 2×2 footprint; ten Cupboard goals per footprint | Ten shared durability units, one goal per hit; clears all four squares at the end |
| Mailbox | Code 34 fixed cells, authored Mail goal count | Persistent solid; produces one Mail per hit wave, with credits capped at the level goal |
| Egg | Code 26, one Egg goal per egg; can also appear in authored spawn sets | Uses the existing falling Item; cannot match or swap; adjacent normal match or direct special hit collects it once |
| Potion | Codes 43/44/53/54 form a shared 2×2 footprint; four Potion goals per footprint | Four shared units, one goal per hit; clears all four squares at the end |
| Coin | Code 60 on the authored bonus boards | Falling nonmatch item using the existing Item destruction path; one Coin credit when destroyed; bonus outcome policy belongs to the campaign loader |

Goal quantities, level placements, tier labels, and 2×2 groupings are package evidence. The damage cadence, fixed-mail production rule, exact egg durability, and shared-goal depletion sequence are authored adaptations. They are not represented as observed reference runtime behavior.

## Integration

- `RoyalCampaignBlockers.Build` runs after the Square board is created and before item generation. All members of a multi-cell obstacle reference the same logical state.
- `Square.DestroyBlock` routes campaign hits to that state; a block-list marker lets the established rocket/TNT/propeller impact paths recognize an occupied square. No booster animation, effect asset or effect timing is changed.
- The existing match loop begins a damage wave. Items capture that wave before their existing destruction animation, so staggered pieces cannot accidentally remove multiple layers from the same adjacent blocker.
- Only cleared normal matched pieces cause neighboring chips. A destroyed box, egg, bonus coin, or directly blasted cell does not recursively spread adjacency damage.
- Eggs and coins keep a safe ordinary palette index for inherited score/animation lookups, while scoped match, swap and hint guards prevent them from becoming ordinary color matches.
- Earned power-up replacements bypass campaign refill selection, preserving queued eggs/coins and preventing a power-up from inheriting their nonmatch identity.
- `RoyalPieceSkin` selects the campaign item's original sprite while retaining its existing sizing, fall and destruction presentation.

## Verification entry point

`RoyalThrone.Editor.RoyalCampaignBlockerChecks.VerifyBehavior(LevelManager, Action<bool,string>)` exercises actual `Square.DestroyBlock` and `Item.DestroyItem` on the verification suite's disposable QA board. Checks cover tier removal, repeated-contact suppression, footprint sharing, footprint release, exact goal credit, fixed mailboxes, grass layers, and egg/coin exclusions. It deliberately changes that QA board and requires a fresh load afterwards. It does not force a win or write campaign progress.

The main verification owner must run these checks in Unity and separately verify live gravity/refill, level 46 egg spawning, actual normal-match adjacency, and natural win/fail transitions. Adding this check entry point does not by itself constitute a passing runtime test.
