# Story layer — King Leo

Everything in this folder is **additive** to the Jelly Garden kit. The kit is a
purchased asset with hand-made art, animation and particle effects; the story
sits on top of it and must never cost it any of those.

## The hard rule

**No existing prefab, Animator, Animation clip, ParticleSystem or sprite may be
deleted, renamed or restructured.** `tools/check_vfx_intact.py` enforces this
against the branch point and fails the build if it is broken.

The board simulation (`LevelManager`, `Item`, `Square`) is off limits beyond
additive hooks. In particular, do not swap gem sprites: `Item` derives its own
colour by looking its sprite up in its array (`color = Array.IndexOf(items,
sprite)`), and items spawned mid-cascade come from the prefab, so a themed gem
can end up a colour that does not match how it looks.

## How the kit and the story meet

Four marked hooks, all early-returns, all a two-line deletion from the kit's
original behaviour:

| File | Hook | What the story does instead |
|---|---|---|
| `InitScript.OnLevelClicked` | `StoryFlow.HandleMapNodeClicked` | opens the area screen, not the pre-play popup |
| `AnimationManager.CloseMenu` | `StoryFlow.HandleLevelComplete` / `HandleLevelFailed` | returns to the area, not the next level |
| `InitScript.SpendLife` | `StoryFreeGame.ShouldSpendLife` | never takes a life |
| `InitScript.CheckAdsEvents` | `StoryFreeGame.AdsAllowed` | never requests an ad |
| `MenuManager.ShowGemsShop` / `ShowLiveShop` | `StoryFreeGame.ShopsAllowed` | never opens a shop |

Star awarding rides on `LevelManager.OnWin`, an event the kit already raises, so
nothing in the board is touched at all.

## The data contract

Nothing here hardcodes an area id. Everything comes from `Resources/Story`:

| File | Written by | Read by |
|---|---|---|
| `arc1..arc6.json` | `tools/story_sync.py` | `StoryDatabase` |
| `index.json` | `tools/story_sync.py` | `StoryDatabase.Arcs` |
| `level_plan.json` | `tools/level_plan.py` | `StoryLevelPlan` |
| `cutscenes.json` | `tools/cutscene_build.py` | `CutsceneRunner` |
| `themes.json` | `tools/themes_build.py` | `StoryTheme` |
| `Art/*`, `Audio/*` | `tools/asset_import.py` | `StoryArt`, `StoryAudio`, `StorySfx` |

Art is resolved **by convention**, not by inspector reference:
`area_{id:000}_state{n}`, `clue_{id:000}`, `backdrop_{theme}`,
`portrait_{name}`. Dropping a correctly-named file into `Resources/Story/Art`
wires it up with no code change. A miss warns **once per key** and falls back to
the arc backdrop — never per frame, never blank.

## The economy

A level win pays **one star into that area's pool**. Repairs are bought with
those stars, in the order the story wrote them. The clue fires on the **last
repair of an area, never on a level win** — that rule is in `StoryRepair` and it
is the one thing not to move. Replaying a beaten level pays nothing.

Stars do not travel between areas, so each area's own levels fund its own
repairs exactly. `StorySmokeTest` walks all 150 areas proving that balances.

## Testing

There are no asmdefs in this project, so its scripts live in the predefined
`Assembly-CSharp`. Unity gives that assembly NUnit but **not**
`UnityEngine.TestTools`, and an asmdef test assembly cannot reference a
predefined assembly — so `[UnityTest]` and the play-mode runner are unavailable.
Story screens are therefore built and driven directly from editor tests, with
`StoryClock` standing in for the frame loop so a three-second clue card is
verified deterministically instead of slept through.

Two seams exist only for this and should be preserved:

- `StoryObjects.Persist` — `DontDestroyOnLoad`, no-op outside play mode
- `StoryClock` — Unity's unscaled time, overridable by a test

## Two traps that have bitten more than once

**`System.*` resolves into the kit's namespace.** The kit has
`JellyGarden.Scripts.System`, so inside `JellyGarden.Scripts.Story` a bare
`System.Random` or `System.Text.StringBuilder` fails to compile. Write
`global::System.Random`, or add a `using`.

**`RuntimeInitializeOnLoadMethod` runs once, in the startup scene.** This game
opens on `main` and loads `game` afterwards, so anything needing the map or the
board found nothing and silently did nothing. Per-scene setup belongs in
`StorySceneHooks`, which subscribes to `SceneManager.sceneLoaded`; only
once-per-process work belongs in `StoryBoot`.

## Typecheck

Unity writes an explicit `<Compile Include>` per file, so a newly added script is
**not typechecked** by the generated csproj. Use the globbed one:

```bash
python3 tools/make_typecheck_csproj.py
dotnet build Story.Typecheck.csproj -v q --nologo
```
