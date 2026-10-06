# Level completed: a larger royal reward

Updated 6 October 2026 after the user requested a card consistent with the other dialogs, a star at 1.5 times its previous size, and a more precious reward reveal.

The win popup now uses the Level Start / Royal Reward family's shared `RoyalCardKit.Card`, full crown and draped crest, layered gold bevels, blue close control, emerald primary button and bottom ornament. Its dark royal-blue stage is retained. The crest's level number is refreshed every time the card opens.

The star is **414 units**, exactly **1.5 x the former 276**. A 500-unit velvet pillow supports it. The heading and coin reward are subordinate to the star, with a taller 950-unit card providing room for the larger centerpiece. The existing Continue and close callbacks and committed reward amounts are preserved.

The star fades into a brief suspension at 1.12 scale, settles onto velvet at 0.48 seconds, rebounds gently and reaches rest by 0.90 seconds. The pillow compresses only seven percent. A contact shadow, restrained amber halo, soft gold rays, orbiting rear glints and a sweep over the star tips and gold braid build the reveal. The reward badge begins at 1.03 seconds; coin flights wait until reward readiness at 1.10 seconds. All timings are relative to Star1 activation. Motion off presents a still star and shadow with an opacity transition.

## Review

- Open [the editable animated preview](preview.html); Replay and Motion off controls sit outside the game frame.
- [Preview image](preview-settled.png).
- [Six-second motion preview](Royal-Throne-Level-Complete-v4-Offline-Preview.mp4).

**These are offline browser previews, not Unity captures.** They use the project's existing art, font, layout and motion values. The preview uses the castle backdrop for presentation; the game keeps its dimmed gameplay board behind the popup. CSS text rasterization, light compositing and UI rendering can differ from Unity. `config.js`, `preview.css` and `preview.js` remain editable; `render_preview.py` renders deterministic frames in a separate headless browser.

## Validation and limits

- The current game C# sources were compiled with Unity 6000.5.10f1's Roslyn compiler and the project's compiler references into a separate output directory: zero compile errors. Project warnings remain. No loaded Unity assemblies were replaced.
- Browser asset, font, animation and reduced-motion observations are recorded in `offline-checks.json`.
- `verify_win_card.cs` is prepared for the next available Unity Play Mode session. It checks the common card family, exact star size, live level number on reopens, animation, rear halo, coin bounds across three resolutions, reduced motion, taps, pooling and save isolation.
- **The v4 Unity runtime checks have not been run.** `SOCLOSE-SESSION-IS-USING-THE-UNITY-EDITOR.txt` reserves the shared editor for another gameplay-fix session. No exit-play, recompile or Unity build command was sent for this revision.
- Earlier v3 checks do not certify this revised layout. No Android/device test was performed.

Runtime changes are confined to the completed-level card builder, its motion component and its rear-ray graphic. Existing art and metadata, gameplay/economy code and unrelated concurrent work are preserved.
