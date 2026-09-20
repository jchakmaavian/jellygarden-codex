# Rocket line clear - Royal Throne

Only the rocket sequence and its local destruction presentation changed in this pass. Original Royal Throne rocket sprites, serialized prefab references, match rules, targets, rewards and existing powerup combination logic remain in use.

Inspection covered RoyalPowerupDirector, Item.DestroyCor/JuiceDestroyCor, Square.DestroyBlock, CandyShatterService, the effects profile, projectile pool, trail material, derived AllIn1 Ice Impact prefab and imported burst atlas library. The previous sequence already traversed cells, but its 24 cells/second sweep and single 160 ms trail made it read as an instant clear.

The rebuilt sequence launches two physical heads after 220 ms: left/right for a horizontal rocket, up/down for a vertical rocket. Travel takes 800-1200 ms depending on board distance. A render hitch cannot advance a head by more than 0.4 cell in one frame. Each crossed cell receives its own white AllIn1 contact impact; its original gem/blocker stays visible for at least 55 ms before the existing destruction service runs. Chained operations retain the existing pending-operation ownership, so refill waits for destruction.

Layers include an additive white core, wider amber glow, white/gold star sparks, launch flash, local impacts and exit impacts. The existing 32-projectile pool supplies the heads. Shared AllIn1 ring/glow/star emitters have fixed capacities. Rocket debris uses the existing 240-mesh pool: six chips instead of three, mixed small/medium scale, rotation, downward gravity and smooth fade/shrink over 650-850 ms. Gem fragments use the imported color burst sheets; blocker fragments use the destroyed block sprite's UVs. No VFX objects are instantiated per crossed cell.

The source directory as spelled in the request was unavailable. The same burst sheets are already imported under Assets/JellyGarden/Resources/RoyalGameplayV4/05_effects/bursts and referenced by RoyalGameplay/Toolkit/BurstPieces.asset.

Final Play Mode validation: **234 checks passed**, with no runtime errors, on 2026-09-15T13:49:38 local time. The VFX Lab's actual speed control ran horizontal, vertical, wire blocker, chained rocket and cascade cases at both 0.5 and 1.0. Checks cover physical head/contact proximity, intact cells ahead of travel, impact-before-break timing, cleared axes, untouched off-axis cells, one-time chain activation, cascade ownership, pool cleanup and stable VFX object counts. Coins and lives remained unchanged. The original board, targets, moves, score and random state were restored; the VFX Lab was visually confirmed at 1.0 after completion.

Final Game-view captures were inspected for local white contact flashes, original rocket art, narrow trails, colored debris, downward movement and readable neighboring cells. The disposable fixture stops its idle shuffle checker after resolution so a no-moves banner cannot carry into the next capture. Production shuffle behavior is unchanged.

Runtime compilation with QA code disabled also passed; existing project warnings remain. The frame captures and object-count checks are not a phone GPU/FPS benchmark; the zero p95 in the legacy report means that metric was not sampled in this focused suite.

- [Passing checks](checks.txt)
- [Horizontal travel, normal speed](horizontal-1-04.png)
- [Vertical travel, normal speed](vertical-1-07.png)
- [Blocker debris, normal speed](blockers-1-08.png)
- [Chained rocket, normal speed](chain-1-12.png)
- [Compilation log](qa-disabled-compilation.txt)
- [Validated source hashes](tested-source-sha256.txt)
