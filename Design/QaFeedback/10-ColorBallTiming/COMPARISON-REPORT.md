# QA #10 - Color Ball timing comparison

Created 7 October 2026. Review only; no gameplay or animation changes.

## Review video

[Royal Match vs Royal Throne - Color Ball + Rocket](Royal-Match-vs-Royal-Throne-ColorBall.mp4)

Two panels, shared activation timer, 1x playback, 60 fps output. Royal Match is on the left; the current Royal Throne Play Mode capture is on the right. Audio is omitted. The reference ends before the current sequence; its last frame is held with a visible label. The current ending is held for review.

## Finding

The reference visually starts firing its converted Rockets roughly **2.1 seconds after Color Ball activation**. Royal Throne reaches its Rocket launch handoff at **3.33 seconds** in this 15-target capture, about **1.2 seconds later**.

Current captured phase durations:

| Phase | Seconds |
| --- | ---: |
| Activation / spin-up | 0.63 |
| Target conversion until Finish begins | 2.23 |
| Finish until Rocket handoff | 0.47 |
| Activation to handoff | 3.33 |

This supports reviewing QA #10 for the Color Ball + Rocket path. It does not establish the correct replacement duration for every Color Ball activation.

## Alignment and limitations

- Royal Throne activation: capture frame 4. Conversion begins at frame 42, Finish at frame 176, and 15 Rocket launches are reported at frame 204. Capture advances simulation by 1/60 second per frame. This measures game animation scheduling, not phone performance.
- Royal Match activation is aligned visually to approximately 0.20 seconds in the archived source; first visible Rocket firing is around 2.30-2.33 seconds. Allow about 0.05 seconds for visual landmark uncertainty. Source presentation timestamps are used when selecting frames.
- The boards have different layouts. Royal Throne selects 15 targets; the full reference target count cannot reliably be established from the archived crop. No claim of a controlled identical-board benchmark is made.
- This is the combo action found in the archived reference, not a standalone Color Ball activation. The recent user-supplied matching Tranform.mp4 shows Rocket formation and does not supply a Color Ball timing reference.
- The reference panel is extracted from the archived RoyalMatch-V3-V4-Comparison.mp4, not from either of its older Royal Throne panels. The raw source is unavailable at its historic path. Previous export scripts identify that source as Royal Match Colorball+finishing up.mp4. Metadata records the archived source path and SHA-256.
- Crops preserve aspect ratio. Neither recording is sped up or slowed down.

## Capture and verification

The actual current QA Combine selected command ran in an isolated level-1 practice session, using Color Ball and Rocket. It completed successfully; normal player balance and lives were restored. Two capture checks passed and the Unity console returned no errors. No scene or production asset was saved or modified.

Local supporting artifacts: current-events.csv, capture-checks.txt, comparison-metadata.json, comparison-preview.jpg and render_comparison.py. Capture frames are in Temp/QA10-ColorBall-frames. Media and capture data remain local under the repository's existing ignore policy.
