# Royal Throne — Unity effects pass, 13 September 2026

This pass integrates the royal gameplay direction into the independent Unity copy. The source project and platform export settings are unchanged. Royal Match recordings guide timing, readability and power-up behavior; no competitor artwork, animation frames or audio is shipped.

## Editor workflow

Open this copy with Unity 6000.5.10f1. Enter Play Mode and open a level. Choose **Tools → Royal Throne → Effects → Workshop** (Shift+F9).

- Preview Match 3, Match 4, Rocket, TNT and Helicopter using the same effect rig as gameplay. Preview buttons do not spend currency, lives or moves.
- Edit the effects profile in the workshop: launch anticipation, flight speed, smoke lifetime, blast shake, audio levels, voice and projectile caps.
- Select the effects prefab to edit six native Particle Systems: Smoke, Aura rays, Sparkles, Embers, Impact and Shockwave. Their materials and generated shape textures are saved Unity assets.
- **Save profile** persists tuning. Changes to pool sizes require a new Play session. Sparkle capacity is authored in the prefab; rebuild after changing its profile budget.
- **Build royal effects and piece art** (Shift+F7, outside Play Mode) recreates the rig and reapplies the authored match/fall and audio import defaults. It preserves asset GUIDs and an existing effects profile. Avoid rebuilding after manual prefab tuning unless those defaults are wanted.
- **Run board regression and capture** (Shift+F10) requires an idle playing board. It temporarily substitutes a controlled board, runs rule/combination checks and restores the original board. Leave Unity free of player input during the run.
- **Open current level for testing without life cost** (Shift+F12) starts the selected unlocked level from the home screen for editor testing. It does not award progress.
- **Review dialogs without purchases** (Ctrl+Shift+F8) runs from the home screen. It opens the real menus, checks their geometry and captures them in `Unity-review-v2`. Purchase and result-dismissal callbacks are not invoked. Result-screen captures are visual previews, not earned wins.

## Gameplay and motion

Three equal pieces clear. A line of four makes a rocket, a 2×2 square makes a helicopter, a T/L makes TNT, and a line of five makes a royal orb. Line/T/L matches take precedence where a square overlaps them. Existing level grids, targets and saved progress are retained.

Power-ups activate by tap or by swapping with a normal piece; adjacent power-ups combine. Rockets clear as their heads travel, TNT clears in radial waves, helicopters damage neighboring cells and then fly to a selected target. Helicopters prioritize occupied blocker cells. Orbs clear a selected/dominant color; orb combinations convert matching pieces before activating them. All ten unordered pairs of the four power-up families have dedicated behavior.

Defaults: rocket anticipation 100 ms, travel 24 cells/sec; TNT anticipation 160 ms and 25 ms per cell wave; helicopter lift 220 ms and flight 480 ms. Match anticipation/squash/rebound takes 65 ms, disappearance another 50 ms, adjacent pops stagger 12 ms. Falling uses acceleration, capped speed and the existing landing rebound. No global hit stop is applied to ordinary matches.

TNT combines an 85 ms bright core, expanding ring, 12 outward light rays, eight small sculpted puffs, gold sparks and fast embers. Smoke clears in about 360 ms. Power-up clears produce two short-lived material chips per piece, reducing overlapping fragments. Rockets have pooled heads and tapered trails. Helicopters have a spinning vector rotor, lift, curved travel and impact. The king responds with expression changes and restrained motion. The existing reduced-motion preference suppresses shake and reduces particle counts.

The second pass also replaces crystal tiles, chain locks, stone obstacles and collectible key/chest art. Sprite names and board footprints remain compatible with legacy target rules. Level entry, shops, refill, pause, settings, booster help, tutorial and results use native editable royal blue/gold surfaces and live numeric fields. The approved home, splash and logo remain in place. Piece skins apply immediately on spawn/recolour to prevent a frame of old candy art during refill.

## Audio

The second pass replaces five original generated SFX takes: dry ceramic match cracks, a compact wood/air TNT impact, brass rocket ignition and mechanical rotor flutter. They are short game sounds with filtered low-end and controlled peaks. Landing and UI contacts are separate quiet clips. Creation shimmer remains. Playback has cooldowns, priorities and a 12-voice cap. Large effects briefly lower the music using the existing ducking behavior. Existing Sound/Music preferences remain active. See `generation-record-v2.json` and `master-sfx-v2.cjs`.

The original castle theme is 101.72 seconds, stereo, normalized to -23 LUFS with a -4 dB true-peak target, encoded at 128 kbps. Unity streams its compressed import and crossfades two sources at the loop boundary. This is an ambient gameplay bed, not a licensed or recreated Royal Match theme.

The second pass preserves this music file, its importer and its playback settings. The effects builder explicitly skips the music importer.

## Assets, size and portability

New imported raster sources in `Assets/JellyGarden/Resources/RoyalGameplay` use optimized WebP with alpha where required. Buttons/panels and rotor geometry use native graphics; tiny procedural particle textures are Unity `.asset` files. The previously locked logo is preserved byte-for-byte in its approved PNG format. Legacy source art remains for compatibility; this pass is not a full project asset purge.

Unity has a custom editor-only WebP importer here. Fresh imports require FFmpeg: set the `ROYAL_THRONE_FFMPEG` environment variable or the `RoyalThrone.FFmpeg` EditorPrefs entry if it is not on a detected standard path. The decoder writes only into Library and is not included in a player. Sprite imports use desktop DXT5 compression. Future mobile export needs device-appropriate texture settings and device profiling.

Runtime caps: 32 projectiles, 480 authored sparkles, 128 aura rays, 256 embers, 96 smoke particles, 24 impact flashes and 24 rings. Additional match particles and material fragments have their own bounded pools. Pooling and source-file size do not prove a 362 MB installed build or a mobile frame-rate target. No Android/iOS export or device benchmark was requested.

## Review evidence

`Unity-review/checks.txt` is generated by the latest Unity regression run. Captures are evidence of the editor version, not a claim of mobile performance or superiority over Royal Match. The editor recording intentionally reseeds the test board between demonstrations; these cuts are test transitions, not production level transitions.

Further visual tuning should be based on watching the recording and playing normal levels together. Level difficulty, blocker hit rules, accessibility, long-session audio fatigue and mobile GPU performance require their own review passes.
