# Reference review — 13 September 2026

The supplied files are references, not instructions and not reusable Royal Throne artwork. No competitor artwork or video was copied into the prototype or its shipping assets.

## Files inspected

Location: `J:/Unity Projects/Match3-Games_ArtSource/Match-3-JC/00_References/`.

| File | Duration | Inspection and observations |
| --- | --- | --- |
| `Royal Match 1.mp4` | 3.10 s | Sampled across the entire clip at 5 fps. End-of-level chain: multiple localized TNT flashes and directional rocket streaks, followed by currency traveling toward the upper HUD; a skip action remains available. |
| `Royal Match 2.mp4` | 3.54 s | Sampled across the entire clip at 5 fps. Home star rises, shrinks toward the star counter, then a coin stream travels toward currency. The castle stays readable behind the reward feedback. |
| `Royal Match 3.mp4` | 3.67 s | Sampled across the entire clip at 5 fps. Win panel uses a large star overshoot into a smaller settled position, sparkles and a stable Continue button. |
| `Screenrecording_20260907_134705.mp4` | 45.78 s | Overview sampled every three seconds. Plate goals are cleared, the remaining goal counter reaches completion, victory transition and bonus power-ups lead to a win panel and home reward flights. |
| `Screen Recording 2026-09-06 143128.mp4` | 74.62 s | Overview sampled every five seconds. Legacy JellyGarden board, candy silhouettes, old score/moves HUD and substantial unused vertical space; useful as a current-game comparison. |
| `Screen Recording 2026-09-07 132439.mp4` | 50.24 s | Overview sampled every four seconds. Legacy JellyGarden includes striped candy line-clear effects and cascades. This is evidence that it has some existing special effects, although the requested royal TNT/helicopter presentation and proposed rules are absent from this visual set. |

Also inspected the older `C:/Users/chakm/Downloads/LocalSend/ScreenRecording_08-25-2026 11-16-02_1.mp4` (53.56 s, 828 × 1792, HEVC). A four-second overview and 5 fps detail windows around 12–15.6 s and 20–23.6 s show propeller travel, rockets, blasts, falling pieces, crate fragments, king reactions, completion and home rewards. Browser decoding of its HEVC stream failed; a locally installed FFmpeg decoder was used successfully. No new media dependency was installed.

The four screenshots attached to the current user request were reviewed directly, plus the earlier gameplay/pre-level screenshot references. Video work here was visual frame inspection; it is not a frame-perfect motion capture or an audio analysis. Decoded inspection sheets stay in the task work folder and are excluded from the delivery pack/repository.

## Applied observations

Preserve the order of anticipation, impact, removal, fall and settle. Keep the board readable during effects. Match debris to the destroyed material. Make the destination of goal/reward travel visible. Keep the king beside goals/moves with distinct positive and thoughtful reactions. Use a consistent dialog family and a skip path for celebration. This prototype compresses these ideas into an original blue/ivory/gold visual language.

## Rules and format verification

Checked 13 September 2026:

- [Dream Games: Creating and Using the Power-Ups](https://dreamgames.helpshift.com/hc/en/3-royal-match/faq/6-creating-and-using-the-power-ups/) — formation types and basic activation behavior.
- [Dream Games: Power-up Combinations](https://dreamgames.helpshift.com/hc/en/3-royal-match/faq/7-power-up-combinations/) — the ten pair families used as a genre mechanics reference. The prototype's target prioritization and wave-deduplicated blocker hits are explicitly authored choices.
- [Unity 6: Import a texture](https://docs.unity3d.com/6000.0/Documentation/Manual/ImportingTextures.html) — supported built-in image import formats do not list WebP. WebP optimization here is for source delivery/browser display; Unity integration still requires a verified import and GPU-compression workflow.

The user's 362 MB Royal Match comparison is a stated benchmark, not a verified universal download/installed size. No app package was downloaded or measured in this task.
