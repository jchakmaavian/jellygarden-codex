# Level completed card: velvet, glow and sparkles

Implemented 2026-10-05 after the user approved the preview and requested a glowing star with sparkle effects on the pillow.

The win popup now uses the new crimson velvet cushion, warm gold edging, a larger single star, a coin reward badge, and a softer landing. A warm light pools in the pillow's dent; a halo and subtle rays sit behind the star. Twenty-four sparkles celebrate the landing, then eight reusable slots create occasional twinkles on the star tips and gold braid. These effects never intercept taps.

The star enters at 1.70x, lands at 0.28 seconds, rebounds to 1.055x and settles by 0.60 seconds. The cushion finishes settling by 0.70 seconds. The title starts at 0.32 seconds; the reward badge starts at 0.69 seconds. Times are relative to Star1 activation, after the card entrance.

Coins wait for the star to settle, then leave the reward badge along the right side of the card. They no longer cover the star during its landing. The badge displays the committed reward amount. Neither this card nor its Continue/close buttons awards currency or stars; existing game logic still owns accounting and navigation. Opening-level Home collection remains intact.

With Motion off, the card and its reward fade in with a steady glow. The star and cushion remain still; flashes, rays, sparkles and coin flights are suppressed for this card. Other popup motion is unchanged.

## Review and verification

- [Actual Unity recording](Royal-Throne-Level-Complete-v2.mp4): 720 x 1280 H.264, 30 fps playback, silent. Built from 84 actual-time capture samples over four seconds; the concat durations preserve observed timing.
- [Animation frames](animation-review.png) and [settled card](Verification/settled-1080x1920.png).
- [Checks](Verification/checks.txt): **47 passed, 0 failed, 0 Unity errors**.
- Verified at 1080 x 1920, 720 x 1280 and 1536 x 2048, including title/star spacing, cushion/reward/button separation and tap ownership.
- Verified landing timing, glow visibility, burst and persistent sparkles, three reopens without duplicate objects, reward count, coin ordering, reduced motion, Continue/close and no duplicate awards.
- The isolated QA profile ended normally. Real PlayerPrefs and runtime currency/lives/level were restored unchanged. Unity returned to Edit Mode.
- No Android build or physical-device performance test was run for this change.

`verify_win_card.cs` runs through Unity Pipeline's `eval_file` while the project is in Play Mode on Home. It refuses to adopt an existing QA session. Its captures and timing samples are under `Verification`; frame JPGs remain local and are not part of the Git delivery. `encode_review.py` builds and decodes the review movie.

## Assets and implementation

- `Assets/JellyGarden/Resources/RoyalGameplay/win-cushion-v2.webp` is a 768 x 512 alpha-preserving size/format conversion of the approved preview cushion, generated with the built-in image tool. The existing cushion and metadata remain available unchanged.
- The original generated master is `cushion-master.png`; `cushion-prompt.txt` records the exact request. Existing Royal Throne star, coin, display font and crown are reused.
- `RoyalCards.Won` owns the layout and color changes; `RoyalWinCard` owns landing/light/sparkles/count presentation; `RoyalWinRaysGraphic` draws the ray fan without a texture.
- `RoyalCardMotion` has a win-only opt-in to honor reduced motion. `RoyalWinSequence.RunCardCoins` waits for `RewardsReady` and uses a local side route. Other coin-flight callers are unchanged.
- Concurrent reward-chest and shared text-style work was preserved and excluded from this change's commit.
