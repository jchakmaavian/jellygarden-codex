# Project Architecture — Designer Reference

A map of how this Unity project is put together, written for someone who knows UI/UX but not Unity production.

---

## The 60-second version

- **Two scenes.** One is a 10-object splash screen. The other holds the world map, the gameplay board, the HUD and every popup in a single 787 KB file.
- **The board is not UI.** Fruit and cells are free-floating 2D objects in a 3D world, filmed by a flat camera. The HUD is a separate overlay drawn on top.
- **One class runs everything.** `LevelManager` (about 98 KB of code) sits on the *camera* and owns the board, the cascade, scoring, objectives, boosters and the VFX pool.
- **Levels are plain text files** you can open in Notepad, and there is a full in-Editor level editor.
- **Nothing is modular.** No assembly definitions, so every code change recompiles the entire project.

---

## Startup and scene flow

```
App launch
  |
  v
main.unity  (10 GameObjects)
  Logo, Loading wheel, Play button, Privacy link
  |
  |  GUIEvents.cs -> SceneManager.LoadScene("game")
  v
game.unity  (261 GameObjects)  <-- everything else lives here
  |
  +-- gameStatus = Map          -> LevelsMap root ON,  Level root OFF
  +-- gameStatus = WaitForPopup -> pre-level panel
  +-- gameStatus = PrepareGame  -> parse level file, build the board
  +-- gameStatus = Playing      -> the board is live
  +-- gameStatus = PreWinAnimations / PreFailed -> end sequences
```

**There is no loading screen driven by progress and no async loading.** `SceneManager.LoadScene` is synchronous — the app freezes for the duration of the load.

> **Why the map and the board share one scene:** `LevelManager.EnableMap(bool)` simply switches two root GameObjects on and off, and destroys the board's children on the way back to the map. This is why the scene file is so large, and why two people cannot edit different screens at the same time without a merge conflict.

---

## The object hierarchy that matters

```
game.unity
|
+-- Main Camera                  <-- orthographic; also HOSTS the game logic
|     +- LevelManager.cs           the god class
|     +- InitScript.cs             lives, gems, boosters, ads, rate prompt
|     +- AudioListener
|     (SoundBase and MusicBase re-parent themselves here at Awake)
|
+-- AI                           <-- hint scanner AND dead-board detector
|
+-- LevelsMap        [toggled]   <-- world map: spline path, 101 level nodes
|
+-- Level            [toggled]   <-- the gameplay screen
|     |
|     +-- CanvasBack   Screen-Space Camera, UI order 0
|     |     +- Background   (changes every 20 levels)
|     |
|     +-- GameField    WORLD SPACE, not a canvas
|     |     +- Square x N     the cells
|     |         +- Block / WireBlock / SolidBlock / ThrivingBlock
|     |         +- Item        the fruit
|     |
|     +-- Canvas       Screen-Space Camera, UI order 1
|     |     +- Panel (SafeArea)
|     |     |    +- Panel
|     |     |         +- Panel        <-- the HUD plate image
|     |     |              +- Score, Limit, Labels, BackBar, Stars, Pause, Targets
|     |     +- BottomPanel
|     |          +- Panel  -> Level number + 5 booster buttons
|     |
|     +-- CanvasScore  WORLD SPACE  -> floating +score numbers
|     +-- TutorialManager -> BlackLayer (dimmer), Hand, CanvasTutorial
|
+-- CanvasGlobal     Screen-Space Camera, UI order 2  <-- ALL popups
      MenuPlay, MenuPause, PreFailed, GemsShop, BoostShop, LiveShop,
      Leadboard, NoMoreMatches, banners
```

Note the **three nested GameObjects all named `Panel`** in the HUD. Only the innermost carries the plate artwork. This is a common source of confusion when someone says "select the HUD panel".

---

## How state changes happen

There is no event bus driving the game. **Assigning to a property *is* the transition:**

```csharp
LevelManager.gameStatus = GameState.Playing;
```

The setter for `gameStatus` (`LevelManager.cs:276-372`) is a long `if/else if` chain that, on assignment, shows and hides menus, switches music, changes `Time.timeScale`, initialises the level, checks ads and writes PlayerPrefs.

Eight `static` C# events *do* exist (`OnWin`, `OnLose`, `OnEnterGame`, and so on) but only one script subscribes, and they are raised without null-guards — so if that listener is missing, they throw.

> **Designer consequence.** If you want something to happen "when the player wins", the reliable hook is the `gameStatus` setter, not the event. And because that setter does a dozen unrelated things, changes there carry regression risk.

---

## Singletons — everything is globally reachable

`LevelManager.THIS` **and** `LevelManager.Instance` (two static references to the same object), `AI.THIS`, `InitScript.Instance`, `MenuManager.Instance`, `SoundBase.Instance`, `MusicBase.Instance`, `InputManager.Instance`, and the added `MatchVFXService.Instance`.

> **Designer consequence.** Any script can reach any other from anywhere. That makes the code easy to extend and very hard to test, preview or reason about in isolation. There is no way to open just the HUD and play with it.

---

## How assets are loaded

Everything uses `Resources.Load` — Unity's oldest mechanism. **No Addressables, no AssetBundles.**

| What | Loaded from |
|---|---|
| Level layout | `Resources/Levels/<n>.txt` |
| Level objectives | `Resources/Targets/Level<n>.asset` |
| Effects | `Resources/Prefabs/Effects/...` |
| Input actions | `Resources/InputActions` |
| Juice tuning *(added)* | `Resources/JuiceConfig` |

> **Two consequences you should know:**
> 1. **Anything in a folder named `Resources` is packed into the shipped app whether or not it is used.** This is why the 21 orphaned PNGs in `Assets/GameRedesign/Resources/` will ship.
> 2. **Assets are loaded by typing their filename as a string.** Renaming a file silently breaks the load at runtime with no compile error.

---

## Save and progression

`PlayerPrefs` only — an unencrypted local key/value file on the device.

Keys: `OpenLevel`, `Score`, `Score<n>`, `Level.{000}.StarsCount`, `Gems`, `Lifes`, `RestLifeTimer`, `DateOfExit`, `UserID`, `Music`, `Sound`, `Rated`, `Lauched` *(misspelled in the original)*, `Facebook_Logged`.

No cloud save is active. PlayFab and GameSparks managers exist but are compiled out.

---

## What is installed but dormant

| System | State |
|---|---|
| Unity IAP | Installed; `UNITY_INAPPS` is **not defined on Android or iOS**, so purchases are compiled out of mobile builds |
| Unity Ads | `UNITY_ADS` **is** defined on all three shipping platforms, but **no Unity Ads package is installed** — a probable compile failure |
| Google Mobile Ads | Behind `GOOGLE_MOBILE_ADS`, not defined |
| Analytics | Behind `UNITY_ANALYTICS`, not defined — the event hooks are no-ops |
| Facebook, PlayFab, GameSparks | Behind undefined symbols |
| Timeline | Installed; no `.playable` asset exists |
| Visual Scripting, AI packages, Multiplayer Center | Installed, no evidence of use |
| Test Framework | Installed; **zero tests exist** |
| Localization | **Not present at all** |

---

## Compilation and iteration speed

**There are no assembly definitions.** Every script — gameplay, the 85 KB level editor, all the dormant integration code — compiles into one `Assembly-CSharp`.

> **What this means day to day.** Change one number in a C# file and Unity recompiles everything and reloads, which takes seconds to tens of seconds each time. This directly throttles how fast anyone can iterate on timing and feel *in code*.
>
> **The fast path exists:** `Assets/JellyGarden/Resources/JuiceConfig.asset` exposes about 45 tuning values as Inspector sliders. Those are *data*, so they can be changed while the game is running with no recompile at all. For any feel tuning, use that asset.
