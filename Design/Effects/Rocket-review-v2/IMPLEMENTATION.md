# Royal Throne — custom rocket fire exhaust, revision 2

Implemented and verified 15 September 2026 in the existing independent Unity project `outputs/JellyGarden-Codex`, Unity 6000.5.10f1. Scope: rocket visuals and their existing QA replay entry point. The original J: Unity project was not edited.

## Visible result

The supplied horizontal and vertical rocket textures remain unchanged. Four cached sprite views split each two-headed image at its centre so its two original ends can travel in opposite directions. No replacement rocket artwork was generated or painted. Source WebP and imported PNG decoded pixels are identical; source/imported files and existing import metas retain the hashes in `art-preservation.json`.

Each rocket has its own animated fire exhaust: orange outer tongues, a gold body and a white-hot centre. A wider additive glow trail and narrow core trail follow it, with dense spatially emitted white/gold stars and embers. Local launch, cell and end flashes use a new procedural shader and ordinary Unity ParticleSystems. The rocket path no longer uses AllIn1VfxToolkit effects or shaders. Existing unrelated effects retain their current implementation.

Horizontal rockets fire left/right; vertical rockets fire up/down. Existing cell destruction still waits for the moving head and a local contact flash. The existing pooled, color-matched break pieces fall under gravity, rotate, then fade and shrink. Rocket destruction retains six mixed small/medium fragments per gem in normal motion mode, twice the former power-clear count; the shared pool reduces this gracefully when full.

## Files and integration

- `Assets/JellyGarden/Scripts/Juice/RoyalRocketSequence.cs`: shared live/preview flight, source sprite views, custom emitters, exhaust mesh and contact scheduling.
- `Assets/JellyGarden/Scripts/Juice/RoyalPowerupDirector.cs`: prewarms exhaust on the existing 32-projectile pool, resets it on reuse, retains fading trails after exit and routes rocket previews to the same sequence.
- `Assets/JellyGarden/Shaders/RoyalRocketFire.shader` and `Assets/JellyGarden/Resources/RoyalRocketFX/`: the new shader and six referenced materials. These assets and their `.meta` files must travel together.
- `Assets/JellyGarden/Scripts/RoyalQaService.cs`: existing Rocket trails control now replays this animation, with direction taken from power selector A. Other controls are unchanged.
- `Assets/JellyGarden/Scripts/Editor/RoyalRocketChecks.cs` and `RoyalEffectsChecks.cs`: focused disposable-board regression and review capture.

Live entry points remain `RoyalPowerupDirector.Single/Pair → Rocket → RocketCell → Item.DestroyItem / Square.DestroyBlock`. Existing rule resolution, blockers, rewards, matching and cascade ownership are reused. Visual preview calls `Preview("Rocket", position)` or `Preview("Rocket V", position)` and disables board mutations inside that same flight sequence.

## Timing and budgets

- Anticipation: existing profile 0.22 game seconds, clamped to 0.15–0.30.
- Sweep: 0.8–1.2 game seconds based on board distance. A frame-hitch cap keeps a visible head from jumping over cells.
- Local contact holds an intact gem for at least 0.055 seconds before the existing destruction path.
- Fire follows each moving nozzle; glow/core trail lives are 0.42/0.36 seconds. The pooled head retires after a 0.46-second tail interval.
- Star lifetime: 0.30–0.52 seconds. Rocket debris: 0.65–0.85 seconds.
- Four fixed shared emitters: 128 rings, 128 flashes, 2,048 stars and 512 embers maximum. No particle GameObject is created per contact. Each of the 32 pooled projectiles owns one prewarmed exhaust renderer; cached sprite views/mesh/material are released with the director.
- Animation and particles follow scaled game time and existing pause handling.

## Review and verification

In GAME TESTING MODE, use VFX TEST LAB → Rocket trails. Set A to Rocket H or Rocket V, choose 0.5× or 1×, then Replay/Loop. This visual-only control keeps gems intact; use the existing booster spawn/activate controls to review actual destruction.

In Unity Play Mode with a settled board, use Tools → Royal Throne → Effects → Verify rocket travel at half and full speed (Ctrl+Shift+F11). The suite uses the VFX Lab's actual time-scale controls and restores the original board, moves, targets and score afterward.

Final result: **324 checks passed**, zero runtime errors recorded by the suite. Covered horizontal and vertical sweeps, an encountered wire blocker, chained H/V rockets, cascades, contact-before-break ordering, intact cells before arrival, source-art identity, both fire exhausts, dense stars, stable effect object counts and cleanup. Repeated H/V visual previews at both speeds preserved every gem, score, moves, coins and lives. QA-disabled C# compilation also passed; inherited compiler warnings remain in `qa-disabled-compilation.txt`.

Evidence: `checks.txt`, the horizontal/vertical/blockers/chain/cascade screenshots, and `preview-*.png`. The test harness captures completed game frames for visual review; this is not a 60 FPS recording or a device performance benchmark. No phone/GPU performance claim is made. The report's unsampled Editor p95 value of zero is not a measured frame-time result.

`rocket-v2-changes.diff` compares this revision with the local files saved before it, not with repository HEAD. `implementation-sha256.json` identifies the verified implementation. The project also contains earlier uncommitted gameplay/QA work, so the diff and manifest isolate this rocket revision for review.
