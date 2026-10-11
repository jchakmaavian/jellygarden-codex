# Rocket flame gap fix

Applied to `G:/Game/royal-throne`, the editor project used in Rocket 2.mp4. The imported world-space fire is now emitted along each head's movement, ending at its current nozzle on the same frame. Ignition also uses the correct tail rotation before its first sample. The native artwork, lifetime curves, speed, hit timing, and board release rules remain in place.

Live Unity validation on 2026-10-11: PASS 533 checks. Shipping tap activation, one move per shot, both axes at 15/30/60 FPS, native nozzle continuity, pause/resume, pool retirement, and refill/item ownership. QA profile restored; player money and lives unchanged.

Captured live frames: `horizontal-30-07.png` and `vertical-30-08.png`.

Repeat in Play Mode through `RoyalThrone.Editor.RoyalRocketFlameGapChecks.Run()` with no existing QA session. The checker uses an isolated QA profile and restores it afterward. Android rebuild and phone coverage were not run.
