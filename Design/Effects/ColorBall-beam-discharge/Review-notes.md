# Color Ball beam presentation correction

The revised discharge keeps recent real links visible as new links launch, draws all beam layers behind the solid ball, and adds a short directional aura exit flare. Incoming contact energy opens into a compact warm flare; the separate star is smaller. Body motion, artwork, targeting, audio wiring and Rocket systems are unchanged.

## Reference and visible result

- Original source verified: `J:/Unity Projects/Match3-Games_ArtSource/Match-3-JC/00_References/ColorBall/Colorball+finishing up.mp4` (2160×2700, 60 FPS).
- Inspected 6.05–7.55s plus twelve consecutive native frames at 6.60–6.80s. Peak discharge has roughly 3–5 visible connections, with staggered launches and progressive fading. This is an observed range, not an exact rule inferred from screenshots.
- Revised short proofs sustain 3–5 links, averaging about 4. The complete sequence averages 3.61 after the initial ramp, with 2–5 including the final wind-down. Short links partly concealed by the body appear less prominent than long links; target geometry is preserved.
- Image 2 measurement: representative perpendicular luminous-body widths average 0.121 cells. Revised render samples average 0.219 cells, approximately 1.8×. These are local-background-relative luminance half-height widths, including the white core and pale-gold body, excluding source/contact flares. Flow, background and compression cause variation; this is not an exact width at every pixel/frame. Probe coordinates and results are in `measurement-and-timing.json`.
- `RoyalMatch-Current-Revised-Beams.mp4` shows board and detail views at equal cell scale, first-discharge aligned, normal 1× playback. No exposure boost, interpolation or retiming. Reference is original 60 FPS; gameplay has 30 distinct captured frames/sec. Comparison audio is the revised gameplay only.

## Verification

- Actual uncaptured checks: 29.97 mean FPS at 30; 59.98 mean FPS at 60 (55.37 median, 22.98ms p95). Editor frame pacing varies; this is not a locked-60/mobile performance claim.
- All 15 selected targets converted once. All 47 logged launch/hit/conversion/finish/completion events match the previous recording exactly, including timestamps. Longer visual residue does not extend gameplay completion or Rocket scheduling.
- Shared rigid-body transforms, source attachment, below-body beam sorting, repeated pooling, cancellation, saved-data restoration, and VFX/audio cleanup passed. No new errors during the focused sessions. The existing unrelated Home navigation exception on Editor transitions was not changed.
- Project compiled and the isolated gameplay combo completed. No full-game QA was run.

## Changed implementation files

- `Assets/Boosters/ColorBall/V4/Scripts/ColorBallVfxV4.cs`: V5 beam rendering, separate pooled visual residue, aura exit flare, local contact accent.
- `Assets/Boosters/ColorBall/V5/Shaders/ColorBallLight.shader`: directional source flare and connected contact shape.
- `Assets/Boosters/ColorBall/V5/Scripts/Editor/ColorBallV5Checks.cs`: focused overlap/sorting/handoff checks and separate capture folder.
- `Assets/Boosters/ColorBall/V5/.gitignore`: omit raw test frames. Reports and comparison exports accompany the change.

Baseline checkpoint: local commit `3ed375c`; source snapshot `colorball-rigid-work/Before-beam-density.zip` in the parent workspace. Remaining differences are the preserved body silhouette, original target layout/order and launch rhythm, rather than an exact recreation of the reference board.
