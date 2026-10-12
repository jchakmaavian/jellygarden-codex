# Color Ball V5 — targeted reference pass

V5 is selected automatically by the existing Color Ball pool when its Resources prefab exists. Both prefabs are native variants of V4. The original V4 prefabs, shaders, artwork and sound assets remain available. To compare V4 live, set `ColorBallVisualV4.UseV4Baseline = true` before the first prewarm in a fresh Play session.

## What changed

- Charged silhouette: shared-pivot body/surface projection becomes shallower, with horizontal rotating bands, stronger color coverage, lighter gaps, a white core and a soft white halo. V4's spin integration and shutter are retained. Source discharges pulse light rather than bouncing the entire ball.
- Beams: the same .095-cell core and .225-cell outer geometry now have a solid visible center and a gentler edge falloff. The source connection remains until impact, then fades over the original .14s lifetime while the next beam travels. Brighter leading stars and compact colored contact light reuse the existing pools.
- Finish: .18s contraction leads into a white flash and fading luminous rim. It uses the former blank handoff interval, preserving the .46s total and existing Rocket scheduling. There is no added spin acceleration or new gameplay event.
- Sound: original V4 cues are reused byte-for-byte. V5 lowers the two spin loops, envelopes their entry/exit, and uses a quiet existing conversion accent at charge completion. Beam/contact/conversion events remain authoritative; the existing Color Ball voice cap and global mix settings are unchanged.

Only three existing files changed: `V4/Scripts/ColorBallVisualV4.cs`, `ColorBallVfxV4.cs`, and `ColorBallAudioV4.cs`. Their V5 options default off on the original V4 prefabs. New presentation, shaders, materials, prefab variants and focused review tools live here in V5. No director, target selection, Rocket, TNT, Propeller, scene, level, UI or global-audio file was edited.

## Reference and comparison

The literal supplied path was missing. The matching supplied reference was found at:
`J:/Unity Projects/Match3-Games_ArtSource/Match-3-JC/00_References/ColorBall/Colorball+finishing up.mp4`.

The V4 panel uses the original V4 gameplay source represented in `RoyalMatch-V3-V4-Comparison.mp4`, not the unrelated `Design/Effects/ColorBall-live-v5` recording.

Reference cell pitch is approximately 231 px; our captured cell pitch is 128.64 px. Both comparison panels normalize to 72 px per cell. Across the reviewed charged frames, the reference's colored silhouette is roughly 1.6–1.8 cells wide and .7–.8 cells tall (soft edges and changing bands make these approximate). V5's charged silhouette is approximately 1.72 × .68 cells; V4 is approximately 1.57 × .84. The short moving proof shows the higher white-core coverage, brighter edges and remaining artwork differences at that equal scale.

`Reference-V4-V5-Short-Proof.mp4` aligns the first discharge, plays at 1x, and was reviewed before the full suite. One follow-up correction restored saturated bands and strengthened the outer halo. `RoyalMatch-V4-V5-Comparison.mp4` aligns activation at 1x; it does not retime the reference or hide timing differences. All comparison audio is our actual game mix, not Royal Match audio.

## Verified

Unity 6000.5.10f1 compiled the project. Focused live Color Ball + horizontal/vertical Rocket combos completed with 15, 31 and 15 targets. Every selected target received one contact and one correctly oriented conversion, after the contact reaction. Existing Rockets fired only after completion. Reveal settled to the original Idle scale; the body/surface pivot remained aligned; repeated use did not grow the visual pool. All beams, voices and visuals cleaned up. Normal player data was restored and there were no new console errors.

Actual uncaptured runtime: 30.00 FPS mean / 35.21 ms p95 frame time with 31 targets; 59.85 FPS mean / 20.79 ms p95 with pooled reuse. These measurements use real frame deltas, not capture timing. The 1080×1920 gameplay recording uses Unity's deterministic 30 FPS capture clock and actual stereo game audio, played back at 1x; recording itself ran slower in the Editor. Recorded audio peak .204, zero clipped samples.

Timing retained: .62s charge; .124s medium volley / .132→.121s high-count interval; 17 cells/sec baseline beam movement with the existing duration bounds. The recorded final envelope is .467s after frame quantization.

## Remaining differences

The analytical Royal Throne bands remain crisper and more regular than the reference's softer, irregular streaks. Our preserved V4 charge lasts longer than the reference's rapid initial spin-up. The reference's source cell becomes a Rocket; our existing gameplay removes the Color Ball and fires its selected converted Rockets. That gameplay result was deliberately preserved.

**Audible reference matching is unverified.** The reference audio was extracted and an audio-input attempt reported that this session cannot consume audio. Sound changes are limited to original cue levels/envelopes and verified event synchronization; no exact listening-based match is claimed. Please audition the delivered gameplay recording before treating the SFX balance as final.

## Delivery / reproduce

- `Animation/Previews/ColorBall-V5-Gameplay.mp4`: complete live combo and payoff, 1x with game audio.
- `Animation/Previews/RoyalMatch-V4-V5-Comparison.mp4`: activation-aligned equal-cell comparison.
- `Animation/Previews/Reference-V4-V5-Short-Proof.mp4`: first-discharge comparison.
- `Animation/Review/checks.txt`, `events.csv`, `Finish-consecutive.jpg`: compact evidence.

Enter the game in Play Mode, then use **Tools → Royal Throne → Color Ball V5**. The short proof uses Ctrl+Alt+Shift+F11. The full combo check uses its menu entry, without taking another booster's keyboard shortcut. Tests use the existing disposable QA profile and restore the player profile. Raw capture frames are local and excluded from Git.
