# Dual TNT animation test

Tested the Royal Match-derived TNT+TNT animation as integrated into **Royal Throne**, in the live Unity 6000.5.10f1 Editor. This is not a test of the official Royal Match mobile app.

Result: **84 checks passed**, with no new Unity errors. The dual-TNT prefab now uses the existing approved sun band instead of the original lettered atlas sprite. The combo importer preserves this choice during rebuilds. Animation clips, transforms, timing and damage code are unchanged.

- Existing `RoyalSuperBombFuseChecks`: 30/60 FPS fuse attachment on every rendered frame, burning-tip movement, rendering above the TNT body, pause/resume, continued emission, explosion, cancellation and stale pooled-handle protection.
- `RoyalDualTntCheckSession`: horizontal and vertical merges at simulated 15/30/60 FPS through `Item.SwitchDirection`; repeated swap input and taps during the merge; exactly one move charged and one native combo activation; approved sun band retained throughout every activation frame; source preparation retained at 1.583333 seconds; explosion follows preparation; particle shaders supported; board settles and input recovers; actors and VFX leases retire.
- Tests used `RoyalQaService.StartSession/EndSession`, checking the real player's coin/life values and normal save routing after restoration. The editor was returned to its initial Edit Mode.

`RoyalTntNativeSequence.cs` plays `TntTntComboExplodeAnimation` via `TNTTNTComboRfv387`, with `TntTntComboFuseParticles` following `FitilPartikul` above the body. The dedicated `TntTntComboExplodeParticles` begins after preparation. The correct sun artwork already existed, but the native dual prefab referenced the older lettered atlas sprite. Its sprite reference is corrected; no new artwork was needed.

`preview.gif` records the 60 FPS horizontal merge from the actual Game view, sampled every three simulation frames and exported at 50 ms per frame. `fuse.png` shows the native fuse burning during preparation. See `checks.txt` for all assertions. Simulated frame rates validate timing behavior; they do not measure device performance. Android and the official Royal Match app were not exercised.
