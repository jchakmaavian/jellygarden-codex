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
python3 tools/themes_build.py        # (static spec)        -> Resources/Story/themes.json
python3 tools/ui_sprites.py         # kit UI sprites       -> Resources/Story/UI
python3 tools/asset_import.py       # generated art/audio  -> Resources/Story/Art, Audio

python3 tools/level_templates.py    # tools/shipped_levels -> tools/out/level_templates.json
python3 tools/level_plan.py         # story JSON           -> level_plan.json (1209 levels)
python3 tools/level_gen.py          # templates + plan     -> Resources/Levels/1..1209.txt
python3 tools/target_gen.py         # plan + boards        -> Resources/Targets/Level1..1209.asset
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

## Verify

```bash
python3 tools/validate_story.py      # the files: 150 areas, 807 tasks, 1209 stars, 1209 boards
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

**`tools/shipped_levels/` is the template source, not `Resources/Levels`.** After
`level_gen.py` runs, `Resources/Levels` holds 1209 files; re-harvesting it would
feed generated levels back into the library that generated them.

**A level cell's two digits take disjoint sets of `SquareTypes`.**
`LevelMakerEditor.SetType` is the authority: the block digit is only
NONE/EMPTY/BLOCK/DOUBLEBLOCK, the obstacle digit only
NONE/WIRE/SOLID/UNDESTROYABLE/THRIVING. Writing an obstacle code into the block
digit does not make a tougher blocker — `LevelManager`'s switch has no branch for
it, so the cell silently becomes plain. `parse_level` rejects it.

**A BLOCKS target is not the board's block count.** Level 19 ships a target of 24
against a board of 56, and levels 28 and 35 ship 0 because their real objective is
the score. It is how many blocks the player must destroy. Never rewrite it; only
assert it is reachable.

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

`-storydemo` exists because driving the player with synthetic mouse clicks does
not work here: the window cascades to a new position on every launch and Unity
moves it again after start, so screen coordinates go stale between reading them
and clicking. The driver walks the game through its own API instead.
