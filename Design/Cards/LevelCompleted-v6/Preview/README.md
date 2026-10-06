# Editable offline preview — v6

Open `preview.html` locally. The folder is self-contained and makes no external network requests. This is an editable reconstruction using project artwork and current runtime timing, not a native Unity capture or a pixel-match claim. Home scenery, HUD, random flight paths, and browser additive light are illustrative reconstructions.

- `config.js`: dimensions, reward amount, and timeline values.
- `preview.css`: common card shell, light falloff, Home layout, and controls.
- `preview.js`: deterministic card and Home choreography.
- `assets/win-star.png`: approved high-resolution project source; other copied assets include the existing pillow, crest, font, castle, native Home icons, and native coin-turn frames.

The card appears immediately with Continue visible. It contains no coin badge, currency balance, or coin flights. Continue and Close switch to Home; Replay returns to the card. Reduced motion uses a brief card opacity transition and immediately shows final Home counters with no reward flights.

The title is positioned at row 166, leaving clearance below the crest during the peak letter bounce. Its reference-style wave starts at 0.18 seconds, staggers each character (including the space) by 0.04 seconds, and gives each glyph a full 0.40-second bounce. Each letter scales 1→1.72 over 0.10 seconds with quadratic ease-out, 1.72→0.88 over 0.20 with quadratic ease-in, then 0.88→1 over 0.10 with smoothstep. Each complete glyph has its own ivory face, navy inner edge, gold outer edge, and shadow, scaling about its visible bottom centre. The browser measures the loaded font to position that pivot; its rasterization remains distinct from Unity.

Continue uses the shared primary treatment: proportional burnished-gold bevels, deep emerald enamel (#18a44e→#034522), an inner green bevel, and a narrow warm reflection. The preceding v5 preview and captures are unchanged.

The ten-second review timeline automatically presses Continue at 4.5 seconds. Relative to Home becoming ready: star appears at 0.08 seconds, pops for 0.20, holds/rocks for 0.50, then curves to its counter over 0.55. The floating +65 starts at 0.95; ten coins begin appearing at 1.20 with 0.02 stagger, varied pile holds, and 0.60-second flights. Star arrival is 1.33; coin arrivals run from 2.35 to 3.34. The offline balances are illustrative 70→135 coins and 4→5 stars; no game save is read or changed.

## Automation

Await `window.previewReady` before capturing. `preview.seek(seconds)` pauses and returns state; it is deterministic over 0–10 seconds. `preview.play(startSeconds)` runs the timeline; `preview.continue()` jumps to the Home handoff; `preview.setReduced(true)` changes motion mode without restarting. `preview.state()` exposes phase, counters, visible flight count, and minimum coin-frame margin.

Use `preview.html?capture&t=0.28` to hide review controls and capture the first title letter at its peak. Useful title poses are 0.18, 0.28, 0.48, 0.58, and 0.94 seconds. Add `&reduced=1` for reduced motion. The image keeps an “OFFLINE MOTION PREVIEW” label. `preview.state().letterScales` reports each character’s current scale for automation.
