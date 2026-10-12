# Royal Reward card (2026-10-05)

The user said of the "A ROYAL REWARD" card: "the animation is so simple. make it feel more Royal Vibe." They chose to have the new art rendered in code; the Magnific account had 0 credits.

The card appears after a rewarded ad. It shows coins (`InitScript.rewardedGems`) or lives (refilled to full), as `InitScript.currentReward` says.

## The ceremony

Times are from the moment the card opens. The timings are constants in `RoyalRewardCard`, and everything runs on unscaled time.

| Time | What happens |
| --- | --- |
| 0.12 s | The royal chest drops onto the card. |
| 0.48 s | It lands with a squash (0.90 high, 1.08 wide) and settles. |
| 0.72–1.02 s | It shakes three times, harder each time, and the lid rattles. |
| 1.02 s | It bursts open. A flash peaks, and golden rays fade in and keep turning behind it. A fountain of coins (or hearts) sprays out to the sides. The lid swings open past its rest (112°) and back to 102° through the rendered frames. |
| 1.10–1.55 s | The reward (our coin or heart) rises out of the chest, grows past its size and settles. It then hovers and glows. |
| 1.45 s | The amount ("+5 Coins" or "Lives Refilled!") is written in, letter by letter. |
| 1.25 s, then every 3.6 s | A shine crosses the "A ROYAL REWARD" title. |
| from 1.5 s | Twinkles appear round the chest. |
| from 1.6 s | COLLECT breathes in a golden aura. |

With the Motion setting off, the chest is simply open and everything fades in.

## Art

All the art is original, rendered in code:

| Asset | Where it lives | How it is made |
| --- | --- | --- |
| The chest | `Resources/RoyalGameplay/RewardChest/chest_000 … chest_112.webp`: 10 frames by lid angle, 512 px, 226 KB in all. | `render_chest.py <prefix> 512 <angles…>`, a signed-distance-field ray march with ambient occlusion and a studio reflection. |
| The rays | `Resources/RoyalGameplay/reward-rays.webp`: 512 px, 47 KB. | `render_rays.py`. |

The chest's design:
- the body is royal-blue lacquer with gold rims, posts and bands;
- the details are rivets, scalloped corner plates and ball feet;
- the lock plate carries a crown and a sapphire, with amethysts on the sides;
- a crown finial sits on the domed lid;
- inside it is a glowing heap of gold coins.

The coin and heart are the game's own art.

## Code

| File | Status | What it does |
| --- | --- | --- |
| `Scripts/GUI/RoyalCards.cs` | changed | `Reward()` sets the layout, rows from the card's top edge. The card is 820 tall. |
| `Scripts/GUI/RoyalRewardCard.cs` | new | Runs the ceremony. |
| `Scripts/GUI/RoyalTextStyle.cs` | changed | Gains `Shine()`, a light band across the letters. It is off by default. |

## Checks

The check is `check_body.txt`; its result is in `checks.txt`. It ran in the QA profile and is presentation only: no coins or lives are added. It covers:
- the drop, squash, shake, burst, lid frames, rays, the rise of the reward, the amount and the aura;
- lives;
- the layout at 1080x1920, 1080x2160, 1536x2048 and 720x1280;
- Motion off;
- COLLECT pressed with a real tap.

**Result:** 19/19 checks passed, with 0 console errors, and the real save was unchanged.

The open is recorded in `reward-card-open.mp4` at real speed. The stills are in `ceremony-stills-final.png`.
