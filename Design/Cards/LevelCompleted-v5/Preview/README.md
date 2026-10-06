# Editable offline preview

Open `preview.html` locally. The folder is self-contained and makes no external network requests. This is an editable reconstruction using project artwork and current runtime timing, not a native Unity capture or a pixel-match claim. Home scenery, HUD, random flight paths, and browser additive light are illustrative reconstructions.

- `config.js`: dimensions, reward amount, and timeline values.
- `preview.css`: common card shell, light falloff, Home layout, and controls.
- `preview.js`: deterministic card and Home choreography.
- `assets/win-star.png`: approved high-resolution project source; other copied assets include the existing pillow, crest, font, castle, native Home icons, and native coin-turn frames.

The card appears immediately with Continue visible. It contains no coin badge, currency balance, or coin flights. Continue and Close switch to Home; Replay returns to the card. Reduced motion uses a brief card opacity transition and immediately shows final Home counters with no reward flights.

The ten-second review timeline automatically presses Continue at 4.5 seconds. Relative to Home becoming ready: star appears at 0.08 seconds, pops for 0.20, holds/rocks for 0.50, then curves to its counter over 0.55. The floating +65 starts at 0.95; ten coins begin appearing at 1.20 with 0.02 stagger, varied pile holds, and 0.60-second flights. Star arrival is 1.33; coin arrivals run from 2.35 to 3.34. The offline balances are illustrative 70→135 coins and 4→5 stars; no game save is read or changed.

## Automation

Await `window.previewReady` before capturing. `preview.seek(seconds)` pauses and returns state; it is deterministic over 0–10 seconds. `preview.play(startSeconds)` runs the timeline; `preview.continue()` jumps to the Home handoff; `preview.setReduced(true)` changes motion mode without restarting. `preview.state()` exposes phase, counters, visible flight count, and minimum coin-frame margin.

Use `preview.html?capture&t=0.2` to hide review controls and capture an impact pose. Add `&reduced=1` for reduced motion. The image keeps an “OFFLINE MOTION PREVIEW” label. `verify_preview.cjs` provides an offline DOM/API smoke check and does not verify browser rendering or Unity behavior.
