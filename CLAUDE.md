# Match3-Games — Jelly Garden kit + the King Leo story

A purchased Unity match-3 kit (Jelly Garden 2.5.2) carrying the full 150-area
King Leo story. The kit supplies the board, its animation and its particle
effects; the story supplies 150 areas, 807 repair tasks, 1209 levels and the six
arcs that connect them.

- Unity **6000.5.10f1**
- Story source of truth: `/Users/avian/Desktop/story` (markdown)
- Story layer: [Assets/JellyGarden/Scripts/Story/](Assets/JellyGarden/Scripts/Story/) — see its [CLAUDE.md](Assets/JellyGarden/Scripts/Story/CLAUDE.md)
- Generators: [tools/](tools/) — see [tools/README.md](tools/README.md)

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

"$UNITY" -batchmode -quit -nographics -projectPath . -logFile - \
  -executeMethod JellyGarden.Scripts.Story.Editor.StorySmokeTest.RunAll
"$UNITY" -batchmode -quit -nographics -projectPath . -logFile - \
  -executeMethod JellyGarden.Scripts.Story.Editor.StoryScreenTests.RunHeadless
```

## Regenerating the story

The markdown is the source of truth. After editing it, re-run the pipeline in
[tools/README.md](tools/README.md) — the whole of `Resources/Story`,
`Resources/Levels` and `Resources/Targets` is generated and reproducible.
