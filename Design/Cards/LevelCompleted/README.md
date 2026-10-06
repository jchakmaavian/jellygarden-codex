# Level Completed card (2026-10-05)

The user asked to import the reference APK's Level Completed popup. That APK is Royal Match (Dream Games), and its art
cannot be copied into Royal Throne. The user chose instead to rebuild the card in our own art, with the same layout and
motion. They also chose to drop the score.

## What the card is now

| Part | How it is built |
| --- | --- |
| Frame | The level-end card system. It is the same frame as the Level Failed card. |
| Ribbon | "Level n" on the ribbon, with the crown ornament and the red close. |
| Title | "Well Done!", written in letter by letter. |
| Star | Our star (`RoyalGameplay/star.webp`), landing on a royal cushion. |
| Action | A green **Continue**. |
| Close | Presses Continue. Closing never loses the win. |

The card has no score, no row of three stars and no king. A win earns one star, and the star is the result.

**Motion.** The timings come from a frame-by-frame reading of the 2026-09-28 reference recording (`journey_001`, 148.2–149.6 s). Times are from the moment the star appears:

| Time | What happens |
| --- | --- |
| 0 → 0.20 s | The star shrinks from 2.7x to 0.80. A flash peaks at 0.17 s. |
| 0.20 s | The cushion gives under the star, to 0.90 high and 1.04 wide, then springs back. |
| 0.20 → 0.42 s | The star rebounds to 1.08, then settles at 1. |
| from 0.24 s | "Well Done!" is written in, one letter every 0.075 s. Each letter pops from 1.7x. |
| 0.22 → 1.3 s | Sparkles swirl round the star. |
| afterwards | The star's soft glow breathes, with the odd twinkle. |

For levels after the opening, the coins fly from the star to the balance pill once the star has landed. With the Motion setting off, the star and the title simply fade in.

## Files

| Kind | Files |
| --- | --- |
| Code | `Scripts/GUI/RoyalCards.cs` (`Won`, the layout); `Scripts/GUI/RoyalWinCard.cs` (new: the star, cushion, flash, glow and sparkles; `RoyalAppearsLater`) |
| Code | `Scripts/GUI/RoyalTextStyle.cs` (an optional `written` reveal, off by default); `Scripts/GUI/RoyalCardMotion.cs` (the entrance skips `RoyalAppearsLater` parts) |
| Code | `Scripts/GUI/RoyalMenuPresenter.cs` (the ribbon's "Level n" on the win card too); `Scripts/Juice/RoyalWinSequence.cs` (no separate star drop; coins wait for the landing) |
| Art | `Resources/RoyalGameplay/win-cushion.webp` (1024 x 684, 40 KB, `bleedTransparentEdges`) |

## The cushion art

The cushion is an original render made from geometry only: no source image and no Magnific credits (the account had 0).

- **Script:** `render_cushion.py out.png 1024`. It reproduces `win-cushion-render.png` byte for byte.
- **Shape:** a plump red velvet cushion with ear corners, a two-ply gold rope piping, four gold tassels, a dent where the star rests, and folds from the corners.
- **Rendering:** point splatting with a depth buffer at 2x.
- **Constants:** the star's rest point is at 18.16% of the art's height from the top. The cushion gives from 30% up the art. Both are constants in `RoyalCards`.

## Checks

The check is `check_body.txt`, built into a driver by the UIUX session's harness. Its result is in `checks.txt`. It runs in the QA profile at 1080x1920 and was last run on 2026-10-05.

- **Motion:** the drop, landing, give, flash, write-in and coin timing.
- **Contents:** the ribbon text, the title, the art, and that nothing old is shown.
- **Layout:** no overlaps, and the card fits at 1080x2160, 1536x2048 and 720x1280.
- **Settings:** the card with Motion off.
- **Close:** it behaves as Continue does.
- **Same checks as the dialog review:** the heading clears the close, and no control is off screen.
- **Result:** 29/29 checks passed, with 0 console errors, and the real save was unchanged.

`win-card-open.mp4` is the recorded open, at real speed. `open-frames.png` is the same at 30 fps, and `sizes-and-motion-off.png` shows the stills.
