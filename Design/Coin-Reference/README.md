# Coin presentation — rfv387 integration

Source: `Design/Backup/apk/royal_thron_rfv387.apk`, inspected offline and left unchanged. The approved Royal Throne club coin artwork and current BGM are retained.

## Recovered and connected

- Four editable native clips: `CoinFlipAnimation`, `CoinFlipNoLoopAnimation`, `CoinIdleAnimation`, `CoinTapAnimation`, under `Assets/ReferenceGameplay/Animation/Clips/`. Native sprite keys, mirror steps, durations and transform curves are preserved; the source sprite names map to the approved club artwork. Reward flights sample the looping flip keys directly for UI Images instead of squeezing a single sprite.
- Four native particle prefabs / ten particle systems: board coin burst, moving finale tracer, counter hit and home collection hit. Board collection uses the existing world VFX pool. Reward UI uses a small native-particle-to-Canvas adapter, with trails behind coins and contacts in front. The existing particle modules, atlas slices and lifetimes remain on the imported prefabs.
- Four original coin audio payloads and losslessly decoded WAVs in `Audio/`. The two collect clips match existing imported audio exactly and are reused. The new source appear/counter cues are imported and connected to actual spawn/arrival events. Coin cancellation stops only coin cues.
- Ten representative home reward coins gather and stream into the visible coin icon; each arrival advances the display once. The final coin supplies the remainder. The existing currency award and save logic are unchanged. Board/finale flights retain native sparkling trails; home coins use compact arrival effects without a long tracer.
- Image and particle reuse, cancellation before launch, level replacement, and home counter hold cleanup are owned by the presentation. The QA coin button now exercises the same renderer as gameplay.

## Exact data versus reconstruction

`DecodedCoinAnimations.json`, `NativeVFXOriginal/`, `CoinAudioManifest.json`, and `import-result.json` identify the recovered data and source IDs. `NativeCoinParticles.json` is the reproducible import input. Run **Royal Throne → Reference Gameplay → Import Coin Media Only** to regenerate just these assets.

The APK's compiled gameplay method bodies were **not recovered as editable source**. Coin count (10), approximate collect duration (2.4 s), final acceleration phase (.6 s), timeout (4.8 s), and counter pulse timing are verified serialized constants. Bézier control points, application of the counter pulse and callback scheduling are reconstructed in the existing Royal Throne system. This is not a claim of a fully exact original logic port. The imported idle/tap clips are available as native editable data; the existing board idle/input system is retained.

The motion comparison uses existing official-version reference recordings, not our own footage: `journey_001_024023.mp4` (board finale) and `journey_004_024927.mp4` (home collection). Compact annotated findings/contact sheets are under `Design/Inspection-rfv387/CoinLogic/`. Emulator video timing is observational evidence; native clip timing is authoritative. Its home star precedes and overlaps the coin burst; this coin-only change does not rewrite the existing star celebration sequence.

## Verification and recovery

Affected pre-edit scripts are in `Checkpoint/`; prior coin art and metadata are unchanged. `Validation/reward-checks.json` and `Validation/board-checks.json` both pass, with zero errors in the corrected runs. All 12 flip poses were verified; ten unique arrivals, cancellation, reuse, native particle output, live coin AudioSources and cleanup passed. A real coin on isolated level 20 credited its goal once, released its cell and retired its particles; moves and currency were unchanged. Normal coin balance was restored to 155 after each check. Unity compiled successfully and is back in Edit Mode.

The requested 30/60 FPS runs measured 26.7–29.8 / 50.8–57.2 FPS in this Editor, including screenshot overhead in the first case. This is not a claim of locked 60 FPS device performance. The capture uses measured timestamps to retain real playback duration.

`Coin-Reference-Preview.mp4` is a silent, 3-second preview of the **finale flight/tracer** on the isolated UI canvas. Its background is Home for this focused test; normal Home coin collection does not use that tracer. `Validation/coin-1.5s.png` shows the actual home-style collection and contact flash. Exact SFX can be inspected separately in `Audio/` and were verified playing through live AudioSources. Full-sequence star choreography is outside this coin-only change.

Changed runtime code: `RoyalWinSequence.cs`, `RoyalQaMenu.cs`, the coin-only branches in `RoyalCampaignBlockers.cs` / `Item.cs`, a clip-specific stop method in `RoyalAudioDirector.cs`, and new `ReferenceCoinAnimation.cs` / `ReferenceCoinParticles.cs`. Import/check helpers live under `Assets/ReferenceGameplay/Editor/`. No booster, BGM, UI layout, level, targeting or currency rule changes are included.
