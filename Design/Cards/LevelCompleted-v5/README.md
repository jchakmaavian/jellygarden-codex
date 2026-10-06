# Level-complete experience from the supplied video

Source inspected on 6 October 2026:

`G:/Game/old Backup/JellyGarden-Codex-20261005/Design/RoyalMatchReference/recordings/game completed experience.mp4`

SHA-256: `2B5CAA9586B6F9E65A7C518947C578AAA054D9BA30E5C2E26DC470BC009F6CAF`.
The file is 17,350,496 bytes, 19.38 seconds, 60 fps, H.264 with a 90-degree display rotation and stereo AAC audio. It was read in place and left unchanged. The reference is Royal Match footage; implementation retains Royal Throne artwork, branding, BGM, save data and economy.

## Observed choreography

| Clip time | Evidence |
| --- | --- |
| About 3–6 seconds | Dimmed board, celebration logo, hanging pennants, fireworks and skip prompt. |
| About 6–10.30 seconds | Remaining move becomes a booster; board effects deliver coins to the Reward HUD. |
| 10.317 seconds | Complete popup and Continue appear, with a very large tilted star. |
| 10.317–10.517 seconds | Fast scale reduction and alternating rotation around a nearly fixed star centre. |
| 10.517–10.95 seconds | Star undershoots, bright impact flash fires behind it, then star rebounds and settles. |
| 10.60–11.30 seconds | Cushion stretches upward, flattens/widens, then returns to rest. |
| 10.50–11.10 seconds | The title writes in with individual letter overshoot. |
| 10.70–15.78 seconds | Initial sparkle ring becomes sparse rear glints. No popup coin badge or coin flight. |
| 15.75–15.817 seconds | Continue depresses and the scene changes to Home. |
| 16.683–16.90 seconds | Home HUD arrives. |
| 17.017–18.25 seconds | Star materializes over the lower lawn, holds/rocks, then curves into the star counter. |
| About 17.95 seconds | Coin pile and amount appear while the star is still airborne. |
| About 18.60–19.38 seconds | Coins stream into the coin counter. The clip ends before all coins arrive. |

Entrance timing is observed within roughly one 60-fps frame. Later transitions were reviewed at 30/10 fps. Scale ratios are visual estimates, not recovered animation curves. Inspection sheets are in `Reference/overview.png` and `Reference/Detail/`.

## Implementation

- Keep the shared royal frame, crown, blue close, emerald Continue and the requested **414-unit star**, exactly 1.5 times its former 276 units.
- Use the existing 1254-pixel gold-star master from `Design/Concepts/RoyalBlue-Home-v1/assets/icon-star.png`. The dedicated reward sprite is a byte-identical copy, imported at up to 1024 pixels. Other star sprites and their metadata are preserved.
- Reveal the full card and action immediately. The star owns a rapid zoom/wobble, a 0.20-second impact, undershoot/rebound, cushion recoil, rear flash and continuous halo sparkles. The initial zoom is normalized for the larger settled star and Royal Throne card proportions.
- Remove the coin badge and flight from the popup. Every won level, including levels after the opening, queues Home collection. Continue and Close select the next authored level but keep Home visible instead of automatically opening its next-level popup.
- On Home, the star pops, holds and rocks before its curved flight; the coin pile follows while it is airborne. Counter animation presents the already committed reward and never writes currency. Motion off shows the saved balances directly without flights.
- Preserve the existing faster celebration and remaining-move finale from concurrent work, including its no-refill board sweep. The reference's 13-coin reward is not imported as an economy rule.

## Review

- [Actual Unity full flow](Royal-Throne-Level-Complete-v5-Unity-Full-Flow.mp4): celebration, remaining-move finale, card, Continue and Home collection.
- [Reference versus Unity card animation](Royal-Throne-v5-Reference-vs-Unity-Card.mp4): 3.5 seconds aligned to star onset, played at real time without retiming.
- [Unity card close review](Royal-Throne-Level-Complete-v5-Unity-Card.mp4) and [six-stage comparison](Verification/CardMotion/Reference-vs-Unity-Star-Entrance.png).
- [Editable offline preview](Preview/preview.html), [browser movie](Preview/Royal-Throne-Level-Complete-v5-Offline-Preview.mp4), and [preview still](Preview/preview-settled.png).

The Unity movies are silent captures with recorded wall-time durations resampled to a 30-fps file. JPEG capture overhead means fewer distinct captured frames; the movie is not evidence of a native 30-fps capture rate. The full-flow scenario forces goals complete in an isolated QA profile, then exercises the real finale and collection code. It is not a naturally played level recording.

The browser preview is explicitly labeled offline. It uses the same artwork and timing, with reconstructed Home layout and deterministic flight paths. Browser text and additive light differ from Unity. The real star settles at the requested larger size and the impact light is softer than the reference; this is an adaptation to Royal Throne's card family, not a pixel-identical recreation.

## Verification

- Unity 6000.5.10f1 loaded the sources with zero compile errors. The independent Roslyn compile also passed; existing project warnings remain.
- [Card checks](Verification/CardMotion/checks.txt): **152 passes, zero failures and zero Unity errors**, at 1080x1920, 720x1280 and 1536x2048. Covers initial shell/Continue, zoom/recoil/title/halo, shared artwork, exact 1.5x size, pooling, reduced motion, real raycast taps, and normal-save restoration.
- [Full experience checks](Verification/reference-experience-checks.txt): **189 passes, zero failures and zero Unity errors**. Covers levels 13/23, Continue/Close, early Continue, replay, Motion off, full sprite bounds, star/coin overlap, ten distinct landed coins, actual HUD counters, next-level selection, duplicate-award protection and queue cleanup. Together with the card suite: **341 passing checks**.
- The full-flow fixture explicitly starts with four spendable stars. A first win must render four until star arrival, then five; replay stays at four. Coins start at 820 and end at 875 for the configured one-move reward. All-restored areas and the QA-only bought-star balance form a controlled test fixture; no shop transaction or player reward is issued by setup.
- Normal PlayerPrefs, runtime coins/lives/selected level and the original Home level catalog are snapshotted and checked after the owned QA session ends. Original BGM and reward economy are unchanged.
- Unity was returned to Edit Mode with `main.unity` clean, no compilation/import activity and no QA profile active; the temporary editor reservation was released.
- Browser font and asset loading passed. The deterministic preview smoke check covers 601 coin-bound samples, star/coin overlap, exact ten-arrival totals and reduced motion.

The first presentation run exposed queued Home rewards starting behind a card directly opened over Home. Collection now explicitly waits for that card to close. Star posing runs in LateUpdate after activation events, avoiding a first rendered frame without its star.

`verify_card_motion.cs`, `verify_reference_experience.cs` and `import_reward_star.cs` reproduce the editor checks/import. `render_unity_capture.py`, `render_card_comparison.py` and `render_preview.py` reproduce media. Raw capture frames and browser profiles remain local intermediates; the committed review movies, logs, timestamp tables and sheets are the portable record. Android/device testing was not part of this change.
