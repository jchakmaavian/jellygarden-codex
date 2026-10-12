# Hammer reference implementation

The gameplay hammer now flies from its bottom-bar button to the chosen cell, plays the recovered strike and impact particles, then shrinks back to the button. The native appearance, hit, and return sounds follow the action. The bottom bar stays visible, as in the supplied video.

## Sources and timing

- Video inspected: `G:/Unity Projects/Match3-Games_ArtSource/Match-3-JC/00_References/animation-VFX/Hammer.mp4`, 16.16 seconds. The hammer action is around 9.1–10.3 seconds; `ReferenceVideo/overview.jpg` and `strike-detail.jpg` record the inspection.
- APK: `Design/Backup/apk/royal_thron_rfv387.apk`. SHA-256 provenance and the matching packaged/local datapack hashes are in `source.json` and `apk-payload-verification.json`.
- Native clip: `HammerExplodeAnimation2`, object 16192, 1.0 second at 60 FPS; controller 18044; animated subtree of prefab 46036. Its 44 curves, 15 sprite keys, stepped renderer switches, 18 sprite dependencies, pivots, pixel scale, and tangents are preserved. Raw serialized clip/controller bytes and decoded dictionaries are retained here.
- Runtime playback is fitted to the video at 1.5x speed. The source's split-head impact pose at 0.60 seconds occurs at sequence time 0.40 seconds. Button travel lasts 0.30 seconds. Return starts at 0.90 seconds and finishes at 1.20 seconds; the action remains alive to 1.36 seconds so the return sound can finish.
- The three original WAV cues match the recording by waveform correlation (0.895 appearance, 0.997 hit, 0.970 disappearance). Their relative starts are approximately 0.405 and 0.896 seconds after appearance. Absolute recording audio/video alignment is not used as a claim of frame-perfect synchronization. Travel and playback speed are locally fitted presentation behavior; compiled APK gameplay code was not imported.

## Imported media and integration

`Assets/ReferenceGameplay/Tools/Hammer` contains the native clip, source manifest, controller, prefab, sprites, VFX textures, and WAVs. Runtime assets load from `Resources/ReferenceGameplay/Hammer`. The clip remains editable in Unity.

`HammerImpactRfv387` imports the six original particle systems under source `smash_particle` (28758): the root flash, background color, smoke, fire blast, blast streaks, and glare. `HammerRewardRfv387` separately preserves the five source reward systems (53989); it is available as an asset and is not played on cell impact. VFX dependencies use ID offset 389000000 to avoid collisions with the earlier APK and cannon imports. All source sprite slots resolve; the limitations report is empty. The source's mesh reference on a stretched-billboard renderer is unused by that render mode.

`ReferenceHammerImporter.Build` installs runtime resource paths, lossless audio settings, and the native particle assets. Its editor menu is **Royal Throne > Reference Gameplay > Import Hammer Animation VFX and SFX**.

`ReferenceHammerSequence` samples the actual recovered clip, places the split-head impact on the target, simulates native particles with the same clock, and routes sounds through `RoyalAudioDirector`. It preserves the sound preference. Pause freezes motion, particles, and owned sound cues; cancellation disables/destroys the owned objects and stops those cues. Source gameplay methods, services, and event receivers are not imported.

`ReferenceBoosterBar` dispatches the hammer's one existing cell-damage operation at impact. It uses the same immediate projectile destruction presentation as the cannon. The existing inventory, move accounting, blocker rules, and refill logic remain authoritative. Missing media prevents stock from being spent.

## Verification

- Isolated Unity asset import/reload: all 44 recovered curves matched source values, key times, and tangents with maximum error 0; all object references resolved; 61 source poses rendered and reviewed.
- Actual game project: compilation passed; animation/VFX/audio resource checks passed; impact has six systems and the expected original shader names.
- `Review/gameplay-checks.txt`: all checks passed. Actual board-pointer targeting of a wire blocker; center/left/right targets; 60/30/15 FPS; a 0.53-second step crossing the impact; exactly one impact at the native split-head pose; no early damage; duplicate/invalid input; one stock spent and no move consumed; wire clearing; refill ownership; unchanged bottom bar; pause; cancellation; object cleanup; normal player profile restored.
- `Review/media-checks.txt`: original appearance/hit/return cues play; native particles emit; pause freezes particle simulation and owned sounds; cancellation stops hammer cues; mute suppresses them; original sound preference restored.
- `Review/Hammer-Gameplay.mp4`: actual Unity Game-view footage at 60 FPS on the synthetic QA board. Its soundtrack is a review mix of the imported hammer WAVs at the implemented cue times, not a loopback recording of Unity's entire audio mix. `Hammer-Reference-vs-Gameplay.mp4` compares the supplied video on the left with the implementation on the right.

The synthetic board's colors, goals, and arrangement are test fixtures. No Android rebuild or physical-device performance test was performed for this change.
