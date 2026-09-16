# Inspection and verification

The supplied Royal Match and Royal Throne screenshots were compared with the current v4 captures before editing. The v4 plume was already continuous, but its broad glow and core merged into a mostly white strip and the tail ended in a bright bulb. The Rocket prefab path is runtime-pooled through `RoyalPowerupDirector`; `RoyalRocketSequence` owns movement and plume sampling; `RoyalRocketFire.shader` draws the nozzle and continuous flame ribbon; `EffectsProfile.asset` supplies the approved 10-cells-per-second speed.

Unity 6000.5.10f1 ran the dedicated `RoyalRocketChecks` disposable board only. The check launched one horizontal and one vertical Rocket at TimeScale 0.5, then one horizontal and one vertical Rocket at TimeScale 1.0.

All 52 checks passed. They verified constant 10-cells-per-second motion, seven-cell mature exhaust length, speed-derived lifetime, nozzle attachment, supported custom shader, dense stars, pooled cleanup, debris cleanup, no object growth, preserved coins/lives, and no runtime errors. Unity was left out of Play Mode.

