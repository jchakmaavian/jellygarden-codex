# Royal Throne — Color Ball V2

Implemented in the independent JellyGarden-Codex Unity project. The live Color Ball creation, seated appearance, single-color clear and Color Ball + horizontal/vertical Rocket conversion paths use V2. Existing target selection, converted Rocket orientation and subsequent Rocket activation remain owned by the original gameplay code.

## Visual and audio delivery

- `Animation/Previews/ColorBall-V2-Gameplay.mp4`: complete live horizontal combo, normal 1x, actual Unity audio.
- `Animation/Previews/ColorBall-V1-V2-Comparison.mp4`: supplied previous gameplay capture beside V2, both at normal speed; V2 audio.
- `Animation/Previews/Reveal-closeup.mp4`: isolated reveal without an adjacent Rocket reveal obscuring it.
- `Animation/Previews/Idle-closeup.mp4`, `Activation-closeup.mp4`, `Finish-closeup.mp4`.
- `Animation/Previews/high-target-count.mp4`: 31 converted targets from a bottom-left origin.
- `Animation/Previews/combo-60fps-reuse.mp4`: top-left origin, vertical Rockets, pooled reuse at 60 FPS capture cadence.
- `Animation/Review/`: consecutive rendered frame sheets, phase screenshots, event and sound logs, checks and protected-file verification.

## Design comparison

The supplied previous `Design/Effects/ColorBall-live-v5/ColorBall-rocket-combo-full.mp4` and the relevant Color Ball section of the Royal Match booster reference were reviewed as consecutive frames before implementation. The new captures were reviewed again after the live test.

| Beat | Previous presentation | V2 |
| --- | --- | --- |
| Body/surface | Flattened frame animation during the active effect | Stable shaded sphere; colored candy details rotate independently in spherical coordinates and foreshorten around its silhouette |
| Reveal | Wide disc presentation | Compressed body, delayed surface appearance, soft flash, elastic settle into exact idle scale |
| Idle | Animated image layer | Stationary body with 0.025 radians/second surface movement; silent; one restrained highlight about every 4.8 seconds |
| Charge | Very short transition into disc/beam state | Readable 0.60-second acceleration, small compression, growing core and restrained orbit; sphere remains visible |
| Beam | Fast line connection and retained links | Travelling four-point head, narrow white core, colored edge and short fading tail |
| Conversion | Soft marking/contact presentation | Contact flash, tiny squash, existing booster creation and star accent; conversion waits for arrival |
| Payoff | Frame-animation finish | Brief contraction, compact center pulse, clean release, then existing Rocket handoff |

V2 follows the new spherical/two-layer brief rather than reproducing the old flat disc. This is a visual revision for review, not a claim of exact Royal Match artwork or sound reproduction.

## Timing and ownership

| Phase | Setting |
| --- | --- |
| Reveal | 0.66 seconds |
| Idle | 0.84 cell visual diameter, stable body |
| Charge | 0.60 seconds; smooth surface speed ramp from 0.025 to 7.2 radians/second |
| Beam launches | 0.095 seconds; middle of large volleys targets 0.068 seconds, quantized to render frames |
| Beam travel | Distance-based 17 cells/second, bounded to 0.14–0.37 seconds for readable near/far contact |
| Beam tail | At most 1.75 cells; retracts/fades over 0.14 seconds after contact |
| Contact reaction | Approximately 0.065 seconds, quantized to render frames |
| Final payoff | 0.18 seconds after the last beam finishes |

`BodyLayer` and `ColorEnergyLayer` share a pivot, position and scale. Their shader layers separate body shading from spherical surface rotation. A short analytical shutter smooths the surface at 30/60 FPS without rotating the body sprite. Detached pooled visuals follow the seated item, and preserve their current rotation when the consumed item hands ownership to the combo.

`ColorBallV2Sequence` receives the existing selected cells. It owns visual contact and conversion scheduling only. `ChangeType()` creates real Rockets with the existing orientation and reveal. The existing `Hit()`/Rocket queue owns destruction after the final payoff. TNT, Propeller and double-Color-Ball combination behavior remains on its existing path.

Twelve visual rigs prewarm and cache any extra seated rigs. Moving beams and accents use a fixed pool of 32 beams and 192 accents. Audio uses 24 reusable voices. Cancellation clears visual and sound pools. No beam or accent instantiation is needed during a volley.

## Sound

Six original generated source cues are included, with their prompts in `Audio/PROVENANCE.md`: Reveal, Charge, Beam, TargetHit, Convert and FinalPulse. Idle is intentionally silent. Originals are retained separately from trimmed runtime WAVs. No Royal Match audio was copied.

Audio follows actual visual events. Charge pitch/gain follows the charge value; beams use small pitch variation; the final pulse stops the charge loop. Existing sound preferences and effects volume are respected. Peak mixed Color Ball gain is capped.

## Verification — 21 September 2026

Unity 6000.5.10f1, Windows Editor/D3D11. 249 focused assertions passed, with 1,287 continuous surface-phase samples. Capture cadence was 30 and 60 FPS at unchanged `Time.timeScale == 1`; this is animation cadence verification, not a mobile hardware benchmark.

- Isolated reveal and 15 seconds of stable, silent idle.
- Same visual lease preserved through Idle → Charge; body/surface alignment and supported shaders checked every captured frame.
- Main combo: 15 targets; charge 0.600s; first beam to last conversion 1.733s; final payoff 0.200s at 30 FPS.
- High-count combo: 31 targets; charge 0.600s; volley 3.433s; final payoff 0.200s at 30 FPS.
- Reuse/60 FPS: 15 targets; charge 0.600s; volley 1.833s; final payoff 0.183s.
- Every selected target contacted and converted exactly once with the expected Rocket orientation. No Rocket launches before Color Ball payoff.
- Legitimate new Color Balls created by the refill cascade stay seated; no orphaned active visuals. All pools empty after the test session.
- Peak effect objects: 26/224; peak audio voices: 9/24. Zero clipped audio samples; maximum captured mix peak 0.2782.
- No new console errors; gameplay resolution and board input restored; normal player data restored after the isolated QA session.
- All 7,353 protected Propeller/TNT/RoyalRocket files match their pre-change SHA-256 hashes. Shared integration diffs are limited to Color Ball hooks.

The first review caught a consumed-item handoff recreating the idle visual. The final retest verified that the same object/rotation continues into charge. The cleanup check also distinguishes legitimate cascade-created Color Balls from abandoned effects.

## Files and integration

All new implementation, shaders, materials, native prefabs, original audio and evidence live under this V2 folder. Previous Color Ball assets/scripts are retained.

Shared edits:

- `Assets/JellyGarden/Scripts/Item.cs`: V2 creation hook; suppress the generic creation burst for Color Ball only.
- `Assets/JellyGarden/Scripts/Juice/RoyalPieceSkin.cs`: V2 seated visual hook.
- `Assets/JellyGarden/Scripts/Juice/RoyalPowerupDirector.cs`: V2 visual ownership, cleanup and Color Ball conversion-sequence call.
- `Assets/JellyGarden/Scripts/RoyalQaService.cs`: wait for V2 reveal in the existing Color Ball test action.

To repeat the focused check, enter Play Mode and open the game, then use **Tools → Royal Throne → Color Ball V2 → Run focused checks with audio**. It uses the existing disposable QA profile and restores it afterward. Raw capture frames/audio remain local in the ignored `.Capture` directory; the exported previews and reports are retained. `Scripts/Editor/export_review.py` exports those frames into normal-speed review movies.

No full-game QA, Android build, package upgrades, UI edits, or changes to Rocket, TNT or Propeller animation were performed for this task.
