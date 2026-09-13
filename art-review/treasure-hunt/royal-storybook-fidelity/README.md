# Royal Storybook — HTML fidelity correction

The original native implementation approximated the approved artwork with generic shapes and a different font. This correction uses the **approved image itself** for the illustrated elements and matches the four reference layouts.

Open [the comparison](index.html) to compare Home, Area Tasks, Before Level and Level Complete at 393×852 and 320×568.

- Victory: the reference's larger Pip, folded ribbon and curved headline, illustrated star and Back to Castle button, inset paper edges and bottom folds. Native rays, glints and entrance animation remain live. The captured board now has the correct orientation and a lighter blur.
- Home: original title ribbon, Pip medallion, crown coin, royal star, blue button rim and Home/Map/Collection icons. The next repair and level still follow saved progress.
- Area Tasks: larger paper sheet, completion summary and check, illustrated flags/lanterns and full-width tappable task rows with a right-aligned star price. The normal purchase guard enforces affordability and order. Compact phones reduce summary spacing so two remaining rows fit.
- Before Level: folded blue level ribbon, serif headings, parchment frame, illustrated close/Play controls and booster icons. Goals, booster ownership, selection and launch callbacks remain live.
- Shared controls: paper edges, glossy button rims and the thin gold screen border also carry through the surrounding menus.

The reference is a flattened art board, with illustrative board pieces and a 404×777 phone shape. The game keeps its real boards, current castle repair artwork, dynamic values and responsive phone dimensions. These are functional content differences, not substituted decorative assets. Area 2 remains gated. The four native repair transitions and four shortened dialogue cards remain in place.

## Verification

- `StorybookTests.RunHeadless`: navigation, task order, affordability, boosters, goal values, one-shot callbacks, victory and replay.
- `v5/runtime-check.json`: real door purchase through the new whole-row target, then a legal first win on board 3; screenshots at both sizes; no errors; Area 2 locked.
- `menus-final/runtime-check.json`: surrounding menus, all four goal cards, replay/failure states, native flag repair and saved progress.
- Typecheck: zero errors. Unity development build: zero errors (existing warnings remain).
- Treasure Hunt content: 17 tests pass. Kit VFX guard and launch-argument guard pass.

All runtime checks use isolated review saves. The player's normal save is never reset.

## Reproduce

Use an app built from the current Assets. This command starts from a checkpoint earned during the earlier legal playtest, buys the door through its normal UI callback and wins board 3:

```sh
tools/run_player.sh --exec -treasure-review -treasure-review-fidelity \
  -treasure-review-dir=/private/tmp/king-leo-storybook-review \
  -treasure-review-save=/Users/avian/Desktop/Project/match3/king-leo/art-review/treasure-hunt/royal-storybook-runtime/v2/normal/earned-before-task-2.json
```

Use a fresh review output directory. Reviews capture through `StoryWindow.Capture` and auto-quit.

[Artwork provenance and font license](asset-provenance.md).

## Installed player

The tested build is installed at `Builds/Mac/KingLeo.app`. A checksum-based rsync
dry run found no differences from the reviewed scratch build. The player's old
process was closed normally and the new build reopened through `tools/run_player.sh`
at 393×852. Normal saved progress was preserved.
