# Level completed: a larger royal reward

Archived preview. The supplied completed-game recording was subsequently located and inspected;
[v5](../LevelCompleted-v5/README.md) supersedes this card's timing and reward handoff. V4 review media remain unchanged.

Updated 6 October 2026 after the user requested a card consistent with the other dialogs, a star at 1.5 times its previous size, and a more precious reward reveal.

The win popup now uses the Level Start / Royal Reward family's shared `RoyalCardKit.Card`, full crown and draped crest, layered gold bevels, blue close control, emerald primary button and bottom ornament. Its dark royal-blue stage is retained. The crest's level number is refreshed every time the card opens.

The star is **414 units**, exactly **1.5 x the former 276**. A 500-unit velvet pillow supports it. The heading and coin reward are subordinate to the star, with a taller 950-unit card providing room for the larger centerpiece. The existing Continue and close callbacks and committed reward amounts are preserved.

The star fades into a brief suspension at 1.12 scale, settles onto velvet at 0.48 seconds, rebounds gently and reaches rest by 0.90 seconds. The pillow compresses only seven percent. A contact shadow, restrained amber halo, soft gold rays, orbiting rear glints and a sweep over the star tips and gold braid build the reveal. The reward badge begins at 1.03 seconds; coin flights wait until reward readiness at 1.10 seconds. All timings are relative to Star1 activation. Motion off presents a still star and shadow with an opacity transition.

## Review

- [Actual Unity animation capture](Royal-Throne-Level-Complete-v4-Unity-Preview.mp4), recorded during the isolated presentation checks on 6 October. Measured frame timestamps preserve the observed timing; the review movie is encoded at 30 fps, not a claim of 30 captured frames per second. No audio was recorded.
- Actual Unity stills: [portrait](Verification/settled-1080x1920.png), [small portrait](Verification/settled-720x1280.png), [tablet](Verification/settled-1536x2048.png), and [Motion off](Verification/motion-off.png).
- Open [the editable animated preview](preview.html); Replay and Motion off controls sit outside the game frame.
- [Preview image](preview-settled.png).
- [Six-second motion preview](Royal-Throne-Level-Complete-v4-Offline-Preview.mp4).

**The editable HTML preview, preview-settled.png and Offline-Preview.mp4 are browser renderings.** They use the project's existing art, font, layout and motion values. The preview uses the castle backdrop for presentation; the game keeps its existing scene behind the popup. The isolated Unity checks also open the popup on Home without committing a real win. CSS text rasterization, light compositing and UI rendering differ from Unity: the in-game rays are subtler and the halo is more diffuse. `config.js`, `preview.css` and `preview.js` remain editable; `render_preview.py` renders deterministic frames in a separate headless browser.

## Validation and limits

- The current game C# sources were compiled with Unity 6000.5.10f1's Roslyn compiler and the project's compiler references into a separate output directory: zero compile errors. Project warnings remain. No loaded Unity assemblies were replaced.
- Browser asset, font, animation and reduced-motion observations are recorded in `offline-checks.json`.
- After the shared editor reservation was released, `verify_win_card.cs` ran in Unity: **139 passes, zero failures and zero Unity errors**. [Full results](Verification/checks.txt) cover the common card family, exact star size, live level number on reopens, animation, rear halo, complete flying-coin sprite bounds at 1080x1920 / 720x1280 / 1536x2048, reduced motion, actual raycast taps, pooling and save isolation.
- The checks confirmed real PlayerPrefs stayed unchanged and runtime coins, lives and level were restored after ending the owned QA session. Unity was returned to its original Edit Mode. No scene or asset was saved by the verification script.
- `Verification/samples.tsv`, `coin-bounds.tsv` and `capture-timestamps.tsv` contain observed values. `render_unity_capture.py` encodes the generated timestamped JPG capture. Raw capture frames are local intermediates and regenerate when the Unity check runs.
- These are presentation checks; no new end-to-end campaign completion or Android/device test was performed. They do not prove fidelity to the user's newly mentioned video: that exact source has not been supplied or identified. The earlier documented `journey_001_024023.mp4` reference is not present at its recorded location.

Runtime changes are confined to the completed-level card builder, its motion component and its rear-ray graphic. Existing art and metadata, gameplay/economy code and unrelated concurrent work are preserved.
