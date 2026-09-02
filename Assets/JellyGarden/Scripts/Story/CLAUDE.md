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

## The repair cutscene

Pressing Repair plays a four-second, skippable sequence: the room fills the
screen, the task's own words appear, the work happens, and the repaired state
wipes across with the story's line for it.

Nothing about it is authored per task. The 807 tasks use 83 verbs but describe
ten kinds of work, so `RepairWork.Classify` reads the label and
`RepairVfx` picks the effect from that — dust for clearing, sparks for
rebuilding, embers for lighting, a gold sweep for polishing. 93% of tasks
classify specifically; the rest get a generic effect. The classifier reads the
WHOLE label, not the first word: "Clear the flooded corridor" is water, "Clear
the last rubble" is stone.

Two things worth knowing before changing it:

**The particles are UI quads, not a ParticleSystem.** A ParticleSystem does not
render inside a ScreenSpaceOverlay canvas at all — it is a mesh renderer and the
overlay draws after everything in the scene, so the effect is simply invisible.

**Layer order in the cutscene is the whole trick**: Before, then the neglect
wash, then After on top. Putting the wash above the clean layer dirties the
thing the repair just cleaned. Below it, an area with only ONE picture still
reads as before-and-after — which is why 23 of the 25 Arc 1 areas ship a single
`area_NNN_restored` image rather than a picture per repair.

Outside play mode `RepairCutscene.Play` calls straight back: there is no frame
loop for its coroutine, so it would otherwise hang. Editor tests cover the
economy and the classifier; the cutscene itself is verified by running the
built player with `-storydemo`.

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

## The Royal Match screen kit

`StoryUi` is a **partial class**. The generic builders — canvas, rect, text,
button — stay in `StoryUi.cs`; the Royal Match shapes are in
`StoryUi.Royal.cs`. Add a new screen primitive to the Royal file, not the base
one, so the kit-neutral half stays kit-neutral.

Four primitives carry every screen:

| Primitive | What it gives back |
|---|---|
| `RoyalPanel(name, parent, title, onClose)` | `RoyalPanelParts` — `Root`, `Body`, `Content`, `Title`, `Close` |
| `GreenButton` / `DoItButton` | the one affirmative action, with an optional star price |
| `StarBadge(name, parent, cost)` | the ★N a task costs |
| `TopBar(parent, onSettings)` | `TopBarParts`, refreshed by `RefreshTopBar` |

Rules that are easy to get wrong:

- **Colours are sampled, not chosen.** Every `Royal*` colour came out of
  `docs/royal-match/*.jpg` with PIL. Match a new one the same way rather than
  eyeballing it, and say which file it came from.
- **A builder hands its parts back as an object.** Nothing looks a child up by
  name afterwards, so renaming a child cannot silently break a screen. Keep it
  that way.
- **A rim is two nested rects, not an `Outline`.** `Outline` offsets the whole
  quad and leaves the corners square; a gold rim needs a rounded rect behind a
  smaller rounded rect.
- **A close X is two rotated bars, not a glyph.** The kit's font may not carry
  ✕, and a missing glyph renders as nothing — which reads as a dead button.
- **`SetGreenInteractable`, not `SetInteractable`.** The green button is three
  layers (base, face, gloss); the base-class helper only recolours one of them.
- Lives read **"Full"** while `StoryFreeGame.Enabled`; coins are the kit's
  `InitScript.Gems`, since the kit has no coin currency at all.

The primitives are proven by `StoryScreenTests.RoyalPrimitivesConstruct`, which
builds each one for real and logs a `[Story] primitive ...` line per shape. With
the Editor open, batchmode is locked out — run it from the menu instead:
**JellyGarden > Story > Run Screen Tests**, then read `Logs/Editor.log`.

## The in-level HUD

`StoryLevelHud` draws Royal Match's Target + portrait + Moves strip over the
kit's score bar. Four things about it are not obvious:

- **The board does not exist when a scene finishes loading.** `LevelManager`
  assigns `THIS` in `Start`, and the level UI is inactive until a level opens,
  so a `StorySceneHooks` pass finds nothing. Anything that needs the board
  attaches on `LevelManager.OnLevelLoaded` / `OnStartPlay` instead — the HUD and
  `StoryBoardTheme` both do.
- **Never deactivate the kit's `Targets` group.** `TargetsGroup.OnDisable`
  destroys the icons it made, and `LevelManager.CheckCollectedTarget` flies a
  matched piece to `TargetObject.guiObj` — one of those icons. Deactivating it
  animates to a destroyed transform. Hide it with a `CanvasGroup` at alpha 0
  instead, which is also why the HUD moves those invisible icons onto its own
  slots: the kit's collect animation then lands where the player is reading.
- **`Level/Canvas/BottomPanel` draws over `Level/Canvas/Panel`.** A screen built
  under `Panel` cannot put anything on top of the booster row, which is why the
  kit's pause button is moved *into* that row rather than drawn over it.
- The HUD is a child of the kit's canvas rather than an overlay canvas of its
  own, so it inherits the kit's sort order and visibility for free. That canvas
  is authored at 720x1280 and the Royal metrics are in 1080, so every vertical
  measurement is multiplied by `referenceResolution.x / 1080`; horizontal ones
  are normalised anchors and need no scaling at all.

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
