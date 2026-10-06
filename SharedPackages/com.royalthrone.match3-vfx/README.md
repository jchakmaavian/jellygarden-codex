# Royal Throne Match-3 VFX Kit

Match animation and effects from Royal Throne, packaged so any match-3 project can use them.
The kit owns **timing and visuals**. Your game keeps **its rules**: it tells the kit where the
board is and what happens when a cell is hit.

- Unity 6 (verified on 6000.5.10f1), Built-in or URP. Needs uGUI, Particle System and Audio modules.
- No scene objects to set up. A pooled rig builds itself on first use and survives scene loads.
- No assets are required. Shapes are generated; the optional art pack adds Royal Throne sprites, painted chips and SFX.

## What you get

| Feedback | Call | Default timing |
| --- | --- | --- |
| Swap / rejected swap | `Match3PieceMotion.Swap`, `InvalidSwap` | 130 ms / 220 ms push-and-settle |
| Fall + landing squash | `Match3PieceMotion.Fall`, `LandingSquash`, `FallAndLand` | speed 12, accel 55, cap 32; 100 ms squash |
| Match pop + shatter | `Match3PieceMotion.Pop` | 30 + 45 + 45 + 90 ms; chips fade over ~0.8 s |
| Group accent, sound, reaction | `Match3Vfx.PlayMatchGroup` | accent at 120 ms |
| Rocket | `Match3Vfx.PlayRocket` | 220 ms anticipation, 0.8–1.2 s sweep, 55 ms contact hold per cell |
| TNT | `Match3Vfx.PlayExplosion` | 160 ms fuse, 25 ms per ring |
| Helicopter | `Match3Vfx.PlayHelicopter` | 220 ms lift, 480 ms curved flight |
| Colour orb | `Match3Vfx.PlayColorOrb` | 160 ms charge, 180 ms convert hold, 14 ms per target |
| Any two power-ups | `Match3Vfx.PlayCombo` | Royal Throne combination rules |
| Power-up formed / blocker chipped | `PlayCreation`, `Creation` / `PlayChip` + `Shatter` | — |
| Cascade banner | `Match3Vfx.ShowComboBanner` | GREAT / BRAVO / FANTASTIC / BRILLIANT from cascade 3; 2.4 s cooldown |
| Shake, hit-stop, SFX | `Shake`, `RequestHitStop`, `PlaySfx` | hit-stop off by default |

Every number is in **Match3VfxSettings**. Sizes are in board cells.

## Install

Pick one:

- **Tarball (easiest to share):** Package Manager → **+** → *Add package from tarball…* → `com.royalthrone.match3-vfx-1.0.0.tgz`.
- **Folder:** copy `com.royalthrone.match3-vfx` into your project's `Packages/` folder. Or use *Add package from disk…* and pick its `package.json`.
- **Git (needs access to the private repo):** *Add package from git URL…* → `https://github.com/jchakmaavian/jellygarden-codex.git?path=/SharedPackages/com.royalthrone.match3-vfx`

Then open **Samples** on the package page and import what you need.

## Quick start (any match-3)

```csharp
using RoyalThrone.Match3Vfx;

// 1. Once, when the board is ready.
var host = new Match3VfxHost { cellSize = 1.2f, boardBottomY = bottomRowY };
Match3Vfx.Configure(mySettingsOrNull, host, myBoard);   // myBoard implements IMatch3Board
Match3Vfx.Prewarm();

// 2. A confirmed group.
float intensity = Match3Vfx.PlayMatchGroup(positions, groupColor, matchSize, cascadeStep);
for (int i = 0; i < pieces.Count; i++)
    StartCoroutine(Match3PieceMotion.Pop(pieces[i].transform, pieces[i].sprite, new Match3PopOptions {
        burstColor = groupColor, colorIndex = pieces[i].colorIndex, intensity = intensity,
        stagger = Match3Vfx.StaggerFor(i), onFinished = pieces[i].ReturnToPool }));

// 3. A power-up. The kit calls myBoard.HitCell(cell, source) when each cell is reached.
yield return Match3Vfx.PlayRocket(cell, horizontal: true);   // or keep playing and check Match3Vfx.Busy

// 4. Leaving the level.
Match3Vfx.Clear();
```

`IMatch3Board` needs `Columns`, `Rows`, `IsCell`, `CellToWorld` (also one cell outside the grid),
`HitCell` and `TryPickHelicopterTarget`. The Demo Board sample (`Match3VfxDemo.cs`) is a complete small implementation.

`IMatch3VfxHost` answers `CellSize`, `IsPaused`, `ReducedMotion`, `SoundEnabled`,
`BoardBottomY` and `ShakeCamera`. Motion, particles and sequences hold while `IsPaused` is true.

## JellyGarden-based projects

Import the **JellyGarden Adapter** sample and follow its `HOOKS.md`. It maps `Item` / `Square` /
`LevelManager` to the kit using only members from the original JellyGarden source. Every hook is
a short replacement inside `JuiceDestroyCor`, `FallingCor`, `Switching`, `DestroyBlock` and the resolve loop.

Class names do not collide with the existing `JellyGarden.Scripts.Juice` layer (`MatchVFXService`,
`CandyShatterService`, `JuiceConfig`). Both can live in one project while you migrate hook by hook.

## Samples

| Sample | Contents |
| --- | --- |
| Demo Board | `Match3VfxDemo` component and **Tools → Royal Throne → Match-3 VFX Kit → Create Demo Scene**. Buttons for every effect, pause and reduced-motion toggles. |
| Royal Throne Art and SFX Pack | 5 piece sprites, rocket / TNT barrel / propeller sprites, 5 burst sheets sliced into a painted-chip library, smoke puff, 10 mastered SFX, and `RoyalThroneArtSettings`. About 2 MB. |
| JellyGarden Adapter | `JellyGardenVfxHost`, `JellyGardenVfxBoard`, `JellyGardenVfxBridge` and `HOOKS.md`. |

Painted chips use the piece's colour index. Keep the burst library order equal to your colour
order (the art pack uses red, blue, gold, leaf, magenta). To cut your own sheets, select them and
use **Assets → Create → Royal Throne → Match-3 Burst Library From Selected Textures**.

## Tools

- **Tools → Royal Throne → Match-3 VFX Kit → Workshop**: edit settings and preview every effect in Play Mode, with live pool counts. Previews never hit cells.
- **Create Settings Asset**: creates `Assets/Resources/Match3VfxSettings.asset`, which loads automatically.
- `Match3VfxDiagnostics.Read()` returns pool usage for QA overlays.

## Rendering, performance and limits

- Sorting: debris 20, bursts 25, power-up layers 26–39 on the `Default` layer. Change these in settings if your pieces draw higher.
- Fixed caps: 96 overlays, 240 debris chips, 32 projectiles, 480 sparkles, 12 voices (created on first sound), 32 banner sparkles. A full pool drops or reduces an effect; it never allocates.
- The shaders are unlit CG passes. They render in the Built-in pipeline and in URP (as `SRPDefaultUnlit`). URP bloom on sparkles is optional (`enableUrpBloom`).
- Hit-stop changes `Time.timeScale` briefly and gives it back if anything else changes it.
- Reduced motion removes flash, shine and shake and cuts particle counts to a third.
- Tests prove behaviour in the editor. They do not measure frame time on phones; profile on your target devices.

## Not included

- **AllIn1 VFX Toolkit impacts.** The in-game Ice/Fire impacts come from a separately licensed Asset Store package, so they are not redistributed. If your team has a licence, assign the prefabs to `matchImpactPrefab` / `blastImpactPrefab`.
- **Match detection and scoring.** Line, square and T/L formation rules stay in each game.
- **Music playback.** Subscribe to `Match3Vfx.MusicDuckRequested` to duck your own music.

## Verification

`Tests/` holds EditMode and PlayMode tests. Add the package to `"testables"` in your manifest to
run them. They cover:
- rocket order, holes and pause
- TNT waves and clipping
- helicopter targets and payloads
- orb conversion order
- the three combo footprints
- cancel on Clear
- pop, fall and swap motion
- pool caps, hit-stop restore, banner cooldown and raycasts
