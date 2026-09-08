# Prioritized Gap Analysis

Every gap cites the project evidence that produced it. This document describes **what deserves attention and why** — it does not prescribe implementations.

Severity reflects impact on the product and on a designer's ability to work, not implementation cost.

---

## CRITICAL

### C1 — `UNITY_ADS` is defined on every shipping platform, but no Unity Ads package is installed

**Evidence.** `ProjectSettings/ProjectSettings.asset:851-855` defines `UNITY_ADS` for Android, iPhone and Windows Store. Four files contain `using UnityEngine.Advertisements;` inside `#if UNITY_ADS`: `GUI/AnimationManager.cs:19-21`, `InitScript.cs:21-23`, and both `UnityAdsController.cs` files. Neither `Packages/manifest.json` nor `packages-lock.json` contains any ads package.

**Why it matters.** The define is on, so the `using` is compiled, and the assembly it names does not exist. This is a near-certain **compile failure on every shipping platform**. If true, there is no mobile build to iterate against, which blocks all downstream work.

**Status.** INFERRED — the evidence is unambiguous but no platform build was attempted. **Verify this first, before scheduling anything else.**

### C2 — No feedback of any kind when the player touches a piece

**Evidence.** `Item.Update()` (`Item.cs:1403-1436`) sets `dragThis = true` and caches a position. No visual, audio or motion response follows anywhere in the file.

**Why it matters.** The first moment of every single interaction — the moment of commitment — is silent and invisible. This is the highest-value feedback gap in the game and is independent of every other constraint here.

**Status.** CONFIRMED.

### C3 — Cascade pacing is bounded by hard-coded waits, not by art

**Evidence.** `LevelManager.FallingDown()` contains `WaitForSeconds(0.1f)` at line 1587, `(0.2f)` at 1747, `(0.2f)` at 1770 and `(0.1f)` at 1773, plus `while (!IsAllDestoyFinished()) WaitForSeconds(0.1f)` at 1728 and an equivalent fall gate. Additional flat costs: 500 ms whenever the player earns a special (`:1723`), 100 ms per special piece in a destroyed group (`:1678`).

**Why it matters.** **Floor of roughly 0.8 s per cascade wave before any animation plays.** A four-deep cascade cannot resolve in under about 3.5 s regardless of how fast the art is. Any brief to "make the game feel snappier" has a low ceiling until these literals change — and they live inside a 98 KB class with no tests.

**Status.** CONFIRMED (the literals and the structure). The arithmetic floor is CONFIRMED; the perceived-quality impact is INFERRED.

---

## HIGH

### H1 — The board and the HUD scale by different rules, tuned for only three device shapes

**Evidence.** All five gameplay canvases use `m_MatchWidthOrHeight: 0` (width-driven). The board is sized by `LevelManager.cs:411-443`, which does exact float equality on a rounded aspect: `6.5` default, `7.6` only if aspect `== 2.06`, `8.1` only if `== 2.17`. `targetDevice: 2` includes iPad (aspect 1.33).

**Why it matters.** On any device that is not exactly 2.06 or 2.17 — including 20:9 phones at 2.22 and every iPad — the board falls to an untuned default while the UI scales by width. The spacing between HUD, board and booster bar will differ from every mockup, and cannot be corrected by moving objects in the scene.

**Status.** CONFIRMED (the code). The visual consequence is INFERRED — not measured on-device.

### H2 — The added bloom cannot render

**Evidence.** `Assets/Settings/JG_2DRenderer.asset:57` is `m_PostProcessData: {fileID: 0}`. URP 17.5's `Renderer2DRendergraph.cs:156` sets `supportedRenderingFeatures.postProcessing = m_Renderer2DData.postProcessData != null`, and line 136 only constructs the post-process pass under the same check. The renderer's GUID is the sole entry in `JG_URP_2D.asset`'s renderer list.

**Why it matters.** The project has been migrated to URP — a real, live change to how everything renders — partly in order to gain glow, and the glow does not work. The renderer was created programmatically, bypassing the editor path that normally assigns this resource automatically.

**Status.** CONFIRMED.

### H3 — No Sprite Atlases

**Evidence.** Zero `.spriteatlas` or `.spriteatlasv2` files. `m_SpritePackerMode: 2` is set and legacy packing tags exist (51 `Pack1`, 24 `Pack3`, 15 `Pack2`, 6 `Game`, 115 untagged), but no atlas asset consumes them.

**Why it matters.** Each distinct sprite tends to cost its own draw call. With 156 Image components and 225 sprites this is the project's cheapest performance win — **pure import-settings work with no art rework**.

**Status.** CONFIRMED.

### H4 — Nothing in the project is 9-sliced

**Evidence.** Every `.meta` under `Assets/JellyGarden/Textures/` has `spriteBorder: {0,0,0,0}` — all 225 sprites. Of 156 Images, 141 are Simple and the 14 "Sliced" ones all point at Unity's built-in default sprite, not project art.

**Why it matters.** **No button, panel or bar can be resized without distorting its corners.** A wider chip or a taller popup requires a new PNG. Conversely, adding borders to the dozen or so chrome sprites is cheap and unlocks free resizing.

**Status.** CONFIRMED. Note the one counter-example: `Assets/GameRedesign/Resources/HUD_Card.png` has `spriteBorder: {165,140,165,185}` — the only 9-sliced sprite in the project, in the orphaned concept folder.

### H5 — No TextMeshPro

**Evidence.** No TMP package in `manifest.json`; zero TMP font assets. 57 legacy `Text` components in `game.unity`, plus 109 more `m_FontData` blocks across prefabs — roughly **167 project-wide**. All have `m_BestFit: 0`.

**Why it matters.** Text sharpness is resolution-dependent; there is no real outline, gradient or auto-sizing. The candy stroke is faked by the `Outline` component duplicating the text mesh **four times**, applied 53 times in the gameplay scene. Long localised strings will overflow rather than shrink.

**Status.** CONFIRMED.

### H6 — Audio has no mixer, no pitch variation and no cascade escalation

**Evidence.** No `.mixer` asset anywhere. Both AudioSources have `OutputAudioMixerGroup: {fileID: 0}`. A grep for `.pitch` across all `.cs` returns nothing. Volume is two binary PlayerPrefs toggles. `m_DSPBufferSize: 0` is the highest-latency preset.

**Why it matters.** No ducking, no limiting, no volume slider, no stereo width (both sources weld to the camera at zero distance), and **a 3-match and a 7-piece cascade sound identical**. `Combo 01.wav` exists in the project, wired to nothing.

**Status.** CONFIRMED.

### H7 — Popup and HUD reference resolutions disagree by about 11%

**Evidence.** `CanvasGlobal` (all popups) is 800 x 1280; `Canvas` and `CanvasBack` (HUD, background) are 720 x 1280. Both use match 0 (width-driven).

**Why it matters.** Artwork of identical source pixel size renders about 11% smaller in popups than in the HUD. This is almost certainly unintentional drift and it undermines any shared spacing or type scale.

**Status.** CONFIRMED.

### H8 — No localization, and some text is baked into images

**Evidence.** No localization package, no `LocalizationSettings`. All strings are literals in scene YAML and in `LevelManager.targetDiscriptions[]`. "SCORE", "MOVES", "TARGET" and "LEVEL" are PNGs (`score_txt.png`, `moves_txt.png`, `target_txt.png`, `level_txt.png`).

**Why it matters.** Translation would require retrofitting a localization system *and* repainting word art. Combined with `m_BestFit: 0`, longer languages will overflow.

**Status.** CONFIRMED.

### H9 — Safe-area handling is applied to one object and does not survive rotation

**Evidence.** `SafeArea.cs`'s GUID appears **once** in `game.unity` and **zero** times in `main.unity`. It reads `Screen.safeArea` in `Awake` only. `defaultScreenOrientation: 4` with both portrait autorotate flags enabled.

**Why it matters.** Only the top HUD dodges the notch. Because rotation is enabled, flipping the phone 180 degrees leaves the inset pinned to the wrong edge. The splash screen has no safe-area handling at all.

**Status.** CONFIRMED.

---

## MEDIUM

| # | Gap | Evidence | Why it matters |
|---|---|---|---|
| M1 | Score popup animation is destroyed at 30% of its length | `flyupText.anim` is 1.0 s; `LevelManager.cs:2232` does `Destroy(poptxt, 0.3f)` | A designed animation is cut mid-flight on every single match |
| M2 | Two button states are unreachable | `button_controller.controller`: all state transition lists empty; `AnyState` has only `Normal->Normal`, `Pressed->Pressed`, `Disabled->Disabled` | `Highlighted` and the 1.03 s `button_idle` loop can never play |
| M3 | Half of the colour-bomb effect renders behind the fruit | `Replace.prefab`: `ParticleSorting` (order 3) is on the root only; the child `Bubble` renderer stays at order 0 vs the fruit's order 2 | The same occlusion defect as the original destroy puff, on a premium payoff moment |
| M4 | Package detonation plays the generic explosion | `destroyPackage` and `explosion` share GUID `1fb040ee...`; `Package_destroy.wav` (226 KB) is referenced by nothing | One of three special-piece payoffs has no distinct sound |
| M5 | Input is blocked for a full second at level start | `LevelManager.AnimateField` adds a code-created animation event at t=1.0 calling `EndAnimGamField`; the motion settles at 0.7-0.8 s | About 200 ms of dead air after the board has visibly stopped |
| M6 | `FindGameObjectsWithTag` called ~10x/second during cascades | `IsAllDestoyFinished`, `IsAllItemsFallDown`, `GetRandomItems`, `Item.DestroyColor` | Allocates a ~99-element array plus a `GetComponent` per element, during the busiest visual moments |
| M7 | Almost nothing is pooled | One 20-slot pool; `Firework`, `Replace`, `CircleExpl`, boosters, `StripesEffect`, score popups and debris all Instantiate/Destroy | Steady GC pressure and intermittent hitches on mobile |
| M8 | The destroy-effect pool silently drops effects | `GetExplFromPool()` returns `null` when all 20 are busy; each slot is held 1.0 s for a 267 ms visual | Effects are lost precisely on the biggest, most rewarding clears |
| M9 | The added shader is SRP-Batcher incompatible and double-multiplies alpha | `_Color`, `_Tint`, `_Boost` declared outside any `UnityPerMaterial` CBUFFER; `Blend SrcAlpha One` combined with `col.rgb *= col.a` | Sparkles fade faster and dimmer than the boost value implies; tuning behaves non-linearly |
| M10 | 38 MB of orphaned art, 21 PNGs of it inside a `Resources` folder | Every one of `Assets/GameRedesign/`'s 65 GUIDs returns zero references | Anything in a `Resources` folder ships in the build whether used or not |
| M11 | Two parallel animation systems | 36 legacy `Animation` components and 21 `Animator` components in `game.unity`; 19 of 42 clips are `m_Legacy: 1` | "Where do I change this animation?" has no single answer |
| M12 | Popup dwell time is baked into the clip | `banner.anim` is 3.03 s: enters by 0.867 s, holds to 2.467 s, exits by 3.0 s | Popup timing cannot be adjusted without editing the animation file |
| M13 | Animation clip length is a gameplay timing value | `destroy.anim` fires `SetAnimationDestroyingFinished` at t=0.250, which releases the cascade gate | An animator retiming a clip is unknowingly changing game pacing |
| M14 | New sprites will likely render black | URP 2D's default sprite material is `Sprite-Lit-Default`; there are zero `Light2D` in either scene | The most probable trap for an artist adding art today |
| M15 | No per-platform texture or audio overrides | `platformSettingOverrides: {}` on all 35 audio files; no texture platform overrides | Desktop-grade assets ship to phones; music is Decompress-On-Load |
| M16 | Everything lives in one 787 KB scene | `game.unity`, 261 GameObjects, holds map + board + all popups | Concurrent editing causes merge conflicts |

---

## LOW

| Gap | Evidence |
|---|---|
| Three water/snow particle materials filed under `Fonts/Materials/` | Path inspection |
| About 747 KB of orphaned audio, including an unused `Combo 01.wav` | Five clips with zero GUID references |
| `item_01.controller` and its `destroyItem.anim` are orphaned | Controller appears only in its own `.meta` |
| `UNDESTROYABLE` blocker type unused by all 100 levels | Cell-code census across the level files |
| A `demo` scene is referenced but not in build settings | `TestLevelGUI.cs:47` |
| `Assets/_Recovery/0.unity` duplicates `game.unity` | Same 261 GameObjects and 693 names; differs only in line endings |
| An orphaned `.meta` with no matching folder | git status vs filesystem |
| `NicerOutline.cs` — a better outline component — is used in exactly one prefab | GUID appears zero times in both scenes |
| `GetRandomItems` can loop forever | `LevelManager.cs:1397-1428`: a `catch` that sets `gameStatus = Win` **inside** a `while` that never advances |
| `CheckWinLose` has a silent-stall branch | Out of moves + all objectives done + `Score < star1` sets no state |
| Transposed coordinates in `Item.GenColor` | `Item.cs:180, 192, 204` pass `(row, col)` to `GetSquare(col, row)`; the anti-instant-match heuristic inspects the wrong cells |
| Zero automated tests | Test framework installed, no test assemblies |
| No bundle identifier set for any platform | `applicationIdentifier` empty |

---

## Suggested reading order for a redesign brief

1. **C1** — establish whether a mobile build exists at all.
2. **C3 / H1** — understand the two hard constraints on pacing and layout before designing anything.
3. **H3, H4, H5** — three import- and component-level changes that unlock most visual work and require no art rework.
4. **C2 and the Feedback Experience Map** — the cheapest, highest-impact experience wins.
5. **M10 and section 9.6 of the main report** — read the existing `GameRedesign` concept art before starting a new direction.
