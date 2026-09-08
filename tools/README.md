# Story generators

Everything under `Resources/Story`, every level board and every target asset is
generated. Nothing here is hand-edited; to change the game, change the story
markdown in `/Users/avian/Desktop/story` or a generator, and re-run.

## Run order

The scripts depend on each other in this order. Running them out of order will
either fail loudly or read a stale file.

```bash
python3 tools/story_sync.py         # story markdown JSON  -> Resources/Story/arc1..6, index.json
python3 tools/cutscene_build.py     # arc markdown         -> Resources/Story/cutscenes.json
python3 tools/ui_sprites.py         # kit UI sprites       -> Resources/Story/UI
python3 tools/asset_import.py       # generated art/audio  -> Resources/Story/Art, Audio
python3 tools/themes_build.py       # spec + the backdrops -> Resources/Story/themes.json

python3 tools/level_templates.py    # tools/shipped_levels -> tools/out/level_templates.json
python3 tools/level_plan.py         # story JSON           -> level_plan.json (1208 levels)
python3 tools/level_gen.py          # templates + plan     -> Resources/Levels/1..1208.txt
python3 tools/target_gen.py         # plan + boards        -> Resources/Targets/Level1..1208.asset
```

The art reskins are independent of the story data. **None of them is applied —
the game currently ships the kit's own candy art**, which is what the owner
chose. Each script rewrites kit textures in place and can be run, or undone,
at any time:

```bash
python3 tools/item_skin.py          # painted jewels  -> the 27 board sprites
python3 tools/item_skin_flat.py     # flat minimal set -> the 27 board sprites
python3 tools/booster_skin.py       # -> the 14 booster icons
python3 tools/brand_skin.py         # -> logo, title, loading, map tiles, ivy blocker
python3 tools/splash_build.py       # -> the King Leo wordmark, splash art, display face
python3 tools/icons_build.py        # -> the 11 HUD, nav and chest icons
python3 tools/repair_regions_build.py   # -> where in each room a repair happens
```

`repair_regions_build.py` is not a reskin at all: it reads the room artwork and
writes a table of rectangles, so the repair sparkles land on the thing being
repaired rather than over the whole picture. Re-run it whenever per-state room
art changes.

`splash_build.py` and `icons_build.py` are additions rather than reskins: they
import into `Resources/Story/Art`, where `StoryArt.Load` finds them by name and
the drawn fallbacks in `StoryUi` step aside. Deleting what they wrote gives the
drawn primitives straight back.

`item_skin.py` and `item_skin_flat.py` write the same 27 files, so run one or
the other, never both. Each script takes `--preview` and writes a contact sheet
to `/tmp` instead of touching the project.

To undo any of them:

```bash
git checkout HEAD -- Assets/JellyGarden/Textures
```

## Why a reskin overwrites pixels instead of importing new assets

The kit is a candy game and the story is about a king, so if the pieces the
player swaps are ever changed, they are changed by rewriting each PNG **in
place, at its original size, under its original name**. That is deliberate:

- Unity references sprites by the GUID in the `.meta`, so every prefab,
  animation clip, particle material and `Target` asset keeps pointing at the
  same sprite and nothing needs rewiring.
- `Square.cs` and `LevelManager` match target icons by `sprite.name`, which is
  the file name. Renaming a file would silently break target matching.
- `check_vfx_intact.py` fails on a deleted or renamed visual asset. Overwriting
  passes it honestly rather than by exception.
- Reverting is `git checkout` of one directory, with nothing to re-import.

The six colours must stay in their kit order (red, orange, purple, blue, green,
yellow) because the explosion particle textures are per item index — a green
piece that shattered into red sparks would be the VFX regressing.

Masters live in `tools/art/`: `pieces/` and `brand/` and `map/` for the painted
set, `flat/` for the minimal one, `icons/` for the HUD and nav sheets. Most of the painted masters are the story
prototype's own art at
`/Users/avian/Desktop/story/game/unity/Assets/Resources/Art`, already paid for.

## Checking the board themes

```bash
python3 tools/check_themes.py       # the six arc themes, as data
python3 tools/theme_sweep.py        # one board per arc, in the built player
```

`check_themes.py` is to a theme what `lint_levels.py` is to a level. Every rule
is anchored to a line of the story layer, and both of its thresholds are
computed rather than written down — the gem-separation floor from the kit's own
five shipped boards, the brightness bar from `area_001_state0..3`. `NOTE` lines
are the ones only new art answers; `--strict` makes them fail.

`themes_build.py` reads the same helpers: each arc's board ground is its own
colour moved toward `STONE` (StoryUi.RoyalCream) by the least amount that keeps
all six of the kit's gems readable on it, over that arc's own backdrop. So it
must run AFTER `asset_import.py`, which is what puts the backdrops on disk. Arcs
1, 2 and 6 keep their authored colour untouched; 3, 4 and 5 need to move,
because a lavender or pale-blue ground sits where the purple and blue gems are.

## Verify

```bash
python3 tools/validate_story.py      # the files: 150 areas, 806 tasks, 1208 stars, 1208 boards
python3 tools/check_vfx_intact.py    # no animation, particle or VFX regression since the branch point
python3 tools/check_launch_args.py   # nothing can launch the game full screen

python3 tools/make_typecheck_csproj.py
dotnet build Story.Typecheck.csproj -v q --nologo    # 0 errors

UNITY=/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/MacOS/Unity
"$UNITY" -batchmode -quit -nographics -projectPath . -logFile - \
  -executeMethod JellyGarden.Scripts.Story.Editor.StorySmokeTest.RunAll
"$UNITY" -batchmode -quit -nographics -projectPath . -logFile - \
  -executeMethod JellyGarden.Scripts.Story.Editor.StoryScreenTests.RunHeadless
```

## One-off editor steps

These change assets rather than generating them, and are idempotent:

```bash
"$UNITY" -batchmode -quit -nographics -projectPath . -logFile - \
  -executeMethod JellyGarden.Scripts.Story.Editor.StoryMapExtender.RunHeadless
```

Grows `LevelsMap.prefab` from the kit's 100 level nodes to the story's 150 area
nodes, tiles the background to cover them, registers the new PathPivots with
`Path.Waypoints`, and raises `MapCamera.Bounds` in `game.unity` so the new nodes
are reachable. Running it twice does nothing.

## Gotchas worth knowing

**`tools/shipped_levels/` and `tools/shipped_targets/` are the template source,
not `Resources/`.** They hold pristine copies of the kit's own 100 boards and
their 100 target assets. After `level_gen.py` and `target_gen.py` have run,
`Resources/Levels` and `Resources/Targets` hold 1208 of each; re-harvesting them
would feed generated output back into the library that generated it, and would
make a second run of the generators produce different files from the first.

**A level cell's two digits take disjoint sets of `SquareTypes`.**
`LevelMakerEditor.SetType` is the authority: the block digit is only
NONE/EMPTY/BLOCK/DOUBLEBLOCK, the obstacle digit only
NONE/WIRE/SOLID/UNDESTROYABLE/THRIVING. Writing an obstacle code into the block
digit does not make a tougher blocker — `LevelManager`'s switch has no branch for
it, so the cell silently becomes plain. `parse_level` rejects it.

**A non-zero BLOCKS target is not the board's block count.** Level 19 ships a
target of 24 against a board of 56. It is how many blocks the player must
destroy. Never rewrite it; only assert it is reachable.

**A target of count 0 is not a goal, and is repaired.** `TargetObject.Done()` is
`targetCount - countCollected <= 0`, so a zero target is met before the first
move: `CheckWinLose` gets nothing from it and `TargetIcon` still draws a chip
that ticks itself on frame one. `target_gen.repair_dead_targets` drops a dead
COLLECT or INGREDIENT entry and fills a dead BLOCKS one from the board, capped at
the steepest blocks-per-move the kit itself ships (level 75: 112 blocks in 37
moves). Three kit assets carry one — Level15, Level28, Level35 — and every level
generated from those templates inherited it.

**A generated level's COLOR LIMIT has a floor, not just a range.** A level
inherits its template's COLLECT targets verbatim, `TargetObject.SetOff` matches
one by `item.color == target.color`, and `Item.GenItem` only ever rolls
`Random.Range(0, colorLimit)`. So `level_gen.scale_colours` never drops the limit
below one past the highest colour any inherited COLLECT target names — otherwise
the target asks for an item that can never spawn and the level is unwinnable.

**Every generated Unity asset needs a sibling `.meta`.** GUIDs are derived from
the asset path (`story_common.guid_for`) so a regeneration does not move them and
break every reference.

## Running the game

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/MacOS/Unity

# Build a macOS player
"$UNITY" -batchmode -quit -nographics -projectPath . -logFile - \
  -executeMethod JellyGarden.Scripts.Story.Editor.StoryBuild.BuildMac

# Play it. run_player.sh is the only thing that launches the game: it holds the
# window flags, so a new script cannot get them wrong.
tools/run_player.sh

# Anything extra is passed through to the player
tools/run_player.sh -storydemo         # walk the story loop, shot at each beat
tools/run_player.sh -storyplaytest     # play levels by itself and report
tools/run_player.sh -storyselftest     # the powerup and shell self-tests
# The product name is "King Leo"; the old "Match3 Games" paths are the kit's.
tail -f ~/Library/Logs/DefaultCompany/"King Leo"/Player.log | grep StoryDemo
open ~/Library/Application\ Support/DefaultCompany/King\ Leo/demo
```

**The player is always windowed at 393x852 — an iPhone 15/16 in portrait — and
never full screen.** Full screen takes over the display for as long as a sweep
runs, and its screenshots come out the shape of this desktop rather than the
shape of the phone the game ships on, which is the wrong aspect to judge a
layout in. `tools/run_player.sh` passes the flags; `StoryWindow.Apply()` puts a
player that was opened some other way back into a window; and

```bash
python3 tools/check_launch_args.py
```

fails if those two ever disagree, if any other file spells the flags out for
itself, or if a screenshot is taken by anything but `StoryWindow.Capture`
(which renders at superSize 2, so a 393-point-wide shot is readable).

## Sweeping a band of levels

One command, not a hand-built Unity invocation per band:

```bash
python3 tools/playtest_sweep.py --levels 1-16          # batches of 8
python3 tools/playtest_sweep.py --levels 1-16 --batch 4
python3 tools/playtest_sweep.py --levels 1,5,9 --timeout 200
python3 tools/playtest_sweep.py --levels 1-8 --headless   # batchmode Editor, no shots
```

It runs the built player through `tools/run_player.sh --exec` — the foreground
launch, so the sweep owns the process and can kill it — and falls back to a
batchmode Editor running `StoryPlaytestRunner.RunHeadless` when there is no
player. A batch that does not write a report inside its timeout is killed and
listed as hung, naming its levels, so one bad level cannot stall the sweep.

Every batch gets its own report, log and PNGs under one run directory in
`~/Library/Application Support/DefaultCompany/King Leo/sweeps/<timestamp>/`.
The shots are moved there between batches and prefixed `b001_`, because each
run numbers its own shots from 0001 and would otherwise overwrite the last one.

It exits non-zero if there was any finding or any hung batch. **Zero findings is
not the same as a good band**: nothing here can see a layout. Open
`<run>/shots/` and look at the pictures.

A batchmode Editor cannot take the project lock while your own Editor holds it,
so `--headless` needs the Editor closed. With the Editor open, use the built
player (the default) or `King Leo > Preview > Playtest the band file`.

`-storydemo` exists because driving the player with synthetic mouse clicks does
not work here: the window cascades to a new position on every launch and Unity
moves it again after start, so screen coordinates go stale between reading them
and clicking. The driver walks the game through its own API instead.

## Linting a level before playing it

```bash
python3 tools/lint_levels.py                 # every level, ~0.2s
python3 tools/lint_levels.py --levels 1-16   # one band
python3 tools/lint_levels.py --quiet         # counts only
python3 tools/lint_levels.py --strict        # count the NOTE lines too
```

`lint_levels.py` is the static half of the sweep: it reads
`Resources/Levels/<n>.txt` beside `Resources/Targets/Level<n>.asset` and reports
levels that contradict themselves, without launching Unity. One line per problem
as `<level>: <problem>`, non-zero exit when there is at least one.

A fault on one of the kit's preserved 100 boards prints as `<level>: NOTE ...`
and fails only under `--strict`. Those files are bought data: `level_gen.py`
re-emits them byte for byte and a hand-edit is undone by the next run, so a rule
that fires on them has nowhere to go. Everything above level 100 is the
generator's, and there the same fault is a problem.

Every rule is anchored to a line of the kit rather than to taste, so a finding is
a real fault:

| what it flags | why the kit makes it a fault |
| --- | --- |
| `SIZE` against the grid actually written | `ProcessGameDataFromString` indexes `mapLine * maxCols + i` and runs off the array |
| a cell that is not two digits, or a block digit outside `0/1/2/5` | `CreateSquare` branches on NONE/EMPTY/BLOCK/DOUBLEBLOCK only; anything else leaves an empty square and never calls `CreateObstacles` |
| an obstacle digit outside `0/3/4/6/7` | `CreateObstacles` branches on WIREBLOCK/SOLIDBLOCK/UNDESTROYABLE/THRIVING only |
| `LIMIT` of 0, moves or time | `CheckWinLose` fails the level on the first check |
| `COLOR LIMIT` outside 3..6 | `LevelMakerEditor` clamps the slider to that range |
| `STARS` not strictly ascending | the three thresholds are compared in order in `CheckStars` |
| a `BLOCKS` target above the board's blocks | the board builds `TargetBlocks = BLOCK + 2 * DOUBLEBLOCK`; a higher goal can never be spent |
| a `BLOCKS` target of 0 on a board with blocks | `TargetObject.Done()` is already true, so a blocks level is won without touching a block |
| a `COLLECT` target whose colour is `>= COLOR LIMIT` | `Item.GenItem` picks `Random.Range(0, colorLimit)`, so that colour never spawns |
| a `SCORE` target that disagrees with the first star | both are the pass mark; they must be the same number |
| no `SCORE` target at all | a level run out of moves has nothing to check against |
| `MODE` whose `Target` value is absent from the asset | the mode names a goal the level does not carry |
| a region of the board with no three cells in a line | a match is three of a colour in a line and a swap only reaches an orthogonal neighbour, so neither can happen there; whatever spawns in it is frozen for the level |

It does not judge difficulty and it cannot see a layout — that is still
`playtest_sweep.py` and your eyes.
