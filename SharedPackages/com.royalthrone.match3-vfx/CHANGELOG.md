# Changelog

## 1.0.0 — 15 September 2026

First shared release, extracted from the Royal Throne effects work in the JellyGarden-Codex project.

- Match pop: anticipation, squash, rebound with additive shine, sprite shatter on the break frame. Softer timing for confirmed matches, snappier timing for power-up clears.
- Match burst: flash, stars, micro sparkles, tinted fragments and a shared group accent. Intensity scales with match size and cascade depth under a ceiling.
- Piece motion: swap, rejected swap with a small overshoot, accelerating fall, landing squash, power-up creation.
- Power-up sequences with per-cell hit callbacks:
  - rocket heads that never skip a cell, with a contact hold before each hit
  - TNT waves with a staged corona
  - helicopters with lift, curved flight and payloads
  - colour orb conversion
  - all ten combinations of rocket, TNT, helicopter and orb
- Feedback: camera shake, optional hit-stop, capped SFX voices with cooldowns and headroom, a non-blocking combo banner, reaction and music-duck events.
- Pooling: fixed pools for overlays, debris, projectiles, particles and voices, with no instantiation during play. Everything respects pause and reduced motion.
- Samples: Demo Board, Royal Throne Art and SFX Pack, JellyGarden Adapter.
- Editor: Workshop preview window, settings asset creation, burst-sheet slicer.
- Differences from the in-game version:
  - The AllIn1 VFX Toolkit Ice/Fire impacts are not included, because that toolkit is separately licensed. Built-in accents replace them; licensed prefabs can be assigned in the settings.
  - Music playback is not included.
  - Match detection rules stay in each game.
