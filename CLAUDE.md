**Always use bright, cartoon-like colour for the castle and everything else.**

# Match3-Games — Jelly Garden kit + the King Leo story

A purchased Unity match-3 kit (Jelly Garden 2.5.2) carrying the full 150-area
King Leo story. The kit supplies the board, its animation and its particle
effects; the story supplies 150 areas, 806 repair tasks, 1208 levels and the six
arcs that connect them.

- Unity **6000.5.10f1**
- Story source of truth: `/Users/avian/Desktop/story` (markdown)
- Story layer: [Assets/JellyGarden/Scripts/Story/](Assets/JellyGarden/Scripts/Story/) — see its [CLAUDE.md](Assets/JellyGarden/Scripts/Story/CLAUDE.md)
- Generators: [tools/](tools/) — see [tools/README.md](tools/README.md)
- Adaptation plan: [PLAN.md](PLAN.md) — what changes next, and what must not

## The one hard rule

**The kit's animation, particle effects and VFX must not regress.** Every change
to it is additive. Enforced, not asserted:

```bash
python3 tools/check_vfx_intact.py
```

## Typecheck

Unity's generated csproj lists files explicitly, so a newly added script is not
typechecked by it. Use the globbed project:

```bash
python3 tools/make_typecheck_csproj.py
dotnet build Story.Typecheck.csproj -v q --nologo     # expect 0 errors
```

`Story.Typecheck.csproj` is gitignored along with every other csproj; the
generator is what is committed.

## Full verification

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/MacOS/Unity

python3 tools/validate_story.py       # content: 150 areas / 806 tasks / 1208 stars / 1208 boards
python3 tools/check_vfx_intact.py     # no visual regression since the branch point
python3 tools/check_launch_args.py    # nothing can launch the game full screen

"$UNITY" -batchmode -quit -nographics -projectPath . -logFile - \
  -executeMethod JellyGarden.Scripts.Story.Editor.StorySmokeTest.RunAll
"$UNITY" -batchmode -quit -nographics -projectPath . -logFile - \
  -executeMethod JellyGarden.Scripts.Story.Editor.StoryScreenTests.RunHeadless
```

## Running the game

`tools/run_player.sh` is the only thing that launches the player. It holds the
window flags — 393x852, an iPhone 15/16 in portrait, never full screen — and
passes anything else through:

```bash
tools/run_player.sh                 # play it
tools/run_player.sh -storyplaytest  # play levels by itself and report
tools/run_player.sh -storydemo      # walk the story loop, shot at each beat
```

## Sweeping levels

`tools/playtest_sweep.py` plays a band in batches, kills a batch that hangs, and
prints every finding grouped by level. Reports and screenshots land under
`~/Library/Application Support/DefaultCompany/King Leo/sweeps/<timestamp>/`.

```bash
python3 tools/playtest_sweep.py --levels 1-16           # batches of 8
python3 tools/playtest_sweep.py --levels 1-16 --batch 4
```

Zero findings does not mean the band is good — nothing automated here can see a
layout. Open the run's `shots/` and look. US-009 swept levels 1-16 at `0
finding(s), 0 error(s)` over a set of pictures that showed the kit's gems shop,
the previous level's popup and a HUD chip too small to read; all five faults in
`ralph/findings.md` F-006..F-010 came out of opening them.

**Run one batch in the foreground, never in the background.** A backgrounded
sweep has twice been killed part-way without a word, and once raced a later run
and left it reading the wrong report. One batch of 8 takes about six minutes.

**The Editor's project lock does not stop you building a player.** Clone the
project into the scratchpad and build there, which leaves the open Editor alone:

```bash
C=<scratchpad>/kl-build
cp -Rc Assets Packages ProjectSettings Library UserSettings "$C"/   # APFS, seconds
rsync -a --delete Assets/ "$C/Assets/"                             # between builds
"$UNITY" -batchmode -quit -nographics -projectPath "$C" -logFile "$C/build.log" \
  -executeMethod JellyGarden.Scripts.Story.Editor.StoryBuild.BuildMac
KINGLEO_APP="$C/Builds/Mac/KingLeo.app" python3 tools/playtest_sweep.py --levels 1-8
```

A NEW script's generated `.meta` has to be copied back out of the clone, or the
repo gets a script with no guid.

## Checking the board themes

Two halves, because half the question is data and half of it is what reaches a
screen.

```bash
python3 tools/check_themes.py            # the data: fast, no Unity
python3 tools/check_themes.py --strict   # count the NOTE lines too
python3 tools/theme_sweep.py             # the screen: one board per arc, photographed
```

`check_themes.py` reads `Resources/Story/themes.json` and the art beside it and
reports the arcs that cannot look the way they claim to — a missing backdrop
(named by path), an arc that falls back to a kit picture, two arcs that are the
same board, a ground one of the kit's six gems merges into. Its thresholds are
measured rather than chosen: the gem floor is the worst separation the kit's own
five shipped boards already accept, and the brightness bar is the dimmest of
`area_001_state0..3`, the project's own bright cartoon room art.

`NOTE` lines are the ones only new artwork can answer. There are ten today: all
six backdrops are dark, near-photographic paintings where the brief asks for
bright cartoon colour. They do not fail the run unless you pass `--strict`.

`theme_sweep.py` runs the built player once with `-storythemetest`, opens the
first level of each of the six arcs in its own area, and writes a 393x852 PNG
per arc plus a report. Zero findings is not a verdict — open the six and look.

## Screenshots are the back buffer, not a re-render

**`StoryWindow.Capture` reads the frame at the window's own size.** It used to
render the frame again at superSize 2, and that re-render drops the Screen Space
- Camera canvas — which in this game carries the level HUD *and* the arc's
backdrop. Every screenshot taken before 2026-09-07 is missing the whole HUD and
shows the room behind the board as a smear, and two entries in
`ralph/findings.md` were written from one. `StoryWindow.CaptureSuperSized` is
the old behaviour, kept and unused. **Re-run any sweep whose shots you are about
to judge.**

## Linting levels

`tools/lint_levels.py` reads every board and its goal asset and reports the
levels that contradict themselves — without playing them. Every rule is
anchored to a line of the kit (which cell digits `CreateSquare` understands,
`TargetBlocks = BLOCK + 2 * DOUBLEBLOCK`, colours are `Random.Range(0,
colorLimit)`, `COLOR LIMIT` clamped to 3..6), so a finding is a real fault, not
taste.

```bash
python3 tools/lint_levels.py                 # every level, ~0.2s
python3 tools/lint_levels.py --levels 1-16   # one band
```

One line per problem as `<level>: <problem>`; non-zero exit when there is at
least one. Run it before a band sweep — it catches unwinnable goals in a
fraction of a second that a playtest would take an hour to reach.

It currently reports zero over all 1208 levels, and a level is only ever fixed
by fixing the generator that wrote it (`tools/level_gen.py`,
`tools/target_gen.py`) and regenerating — never by hand-editing one file, which
the next regeneration would undo. The generators are deterministic and
idempotent: running the three of them back to back leaves the tree byte-for-byte
unchanged.

```bash
python3 tools/level_templates.py && python3 tools/level_gen.py && python3 tools/target_gen.py
```

## Regenerating the story

The markdown is the source of truth. After editing it, re-run the pipeline in
[tools/README.md](tools/README.md) — the whole of `Resources/Story`,
`Resources/Levels` and `Resources/Targets` is generated and reproducible.
