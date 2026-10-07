# QA #10 - Color Ball timing refinement

Implemented 7 October 2026.

Color Ball now starts discharging sooner and moves through its target sequence with shorter gaps. The same 15-target Color Ball + Rocket case decreased from **3.33 seconds to 2.55 seconds** from activation to Rocket handoff at 60 FPS: **0.78 seconds / approximately 24% faster**.

[Updated Royal Match vs Royal Throne comparison](after/Royal-Match-vs-Royal-Throne-ColorBall.mp4)

## Scope

- Spin-up gate: 0.62 -> 0.32 seconds.
- Ordinary beam launch interval: 0.124 -> 0.090 seconds.
- More than 28 targets: 0.132-0.121 -> 0.095-0.085 seconds.
- Existing reveal, target preparation beat, beam travel, impact/flash staging, target order, conversion rules and finish timing are preserved. TNT's native reveal completion gate remains in place.
- Updated the existing V4 timing assertions to reference the configured charge duration instead of the old hardcoded value.

The reference remains about 2.1 seconds for its example. Different layouts and an uncertain reference target count prevent an exact benchmark. This measured reduction is intentionally moderate; it preserves visible impacts and the complete finish instead of forcing every activation to the reference's duration.

## Verification

**114 checks passed, zero failures**, across 16 isolated runtime cases:

- Standalone Color Ball and Rocket, TNT, Propeller combinations at 15, 30 and 60 FPS: 85 checks.
- Dense checkerboard selections of 31/32 targets, standalone and Rocket at 15 and 60 FPS: 29 checks.
- Checked command completion, activation/handoff, no early refill, complete target arrivals and conversions/clears, Rocket launch ordering, input release and 64 settled cells with unique owners.
- Normal player balances and lives restored after both isolated sessions. Scene remained clean. Unity console reported no errors after the primary suite.
- Updated comparison decoded successfully: 317 frames, 60 FPS, 1280x960. Start, conversion, handoff and settled frames inspected. Clips remain at 1x; held reference ending is explicitly labeled.

| Case | 60 FPS | 30 FPS | 15 FPS |
| --- | ---: | ---: | ---: |
| Rocket, TNT or Propeller; 15 targets | 2.55 s | 2.57 s | 3.20 s |
| Standalone; 16 targets | 2.65 s | 2.67 s | 3.33 s |
| Dense Rocket; 31 targets | 3.98 s | Not tested | 5.40 s |
| Dense standalone; 32 targets | 4.03 s | Not tested | 5.47 s |

Times measure activation to handoff using fixed-step Unity simulation, not device performance. At 15 FPS the per-frame preparation/launch staging takes longer; it remains ordered and resolves correctly. Physical-phone QA and the standalone Royal Match timing comparison remain unverified.

Raw local results: after/checks.txt, dense/checks.txt and after/current-events.csv. The original comparison and capture remain available beside this report. No spreadsheet was edited in this step.
