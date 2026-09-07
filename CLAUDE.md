**Always use bright, cartoon-like colour for the castle and everything else.**

# Match3-Games — Jelly Garden kit + the King Leo story

A purchased Unity match-3 kit (Jelly Garden 2.5.2) carrying the full 150-area
King Leo story. The kit supplies the board, its animation and its particle
effects; the story supplies 150 areas, 807 repair tasks, 1209 levels and the six
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

python3 tools/validate_story.py       # content: 150 areas / 807 tasks / 1209 stars / 1209 boards
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
layout. Open the run's `shots/` and look.

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

## Regenerating the story

The markdown is the source of truth. After editing it, re-run the pipeline in
[tools/README.md](tools/README.md) — the whole of `Resources/Story`,
`Resources/Levels` and `Resources/Targets` is generated and reproducible.
