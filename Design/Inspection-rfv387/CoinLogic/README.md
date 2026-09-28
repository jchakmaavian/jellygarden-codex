# Coin logic evidence from rfv387

Source: `Design/Backup/apk/royal_thron_rfv387.apk`, entry
`assets/bin/Data/Managed/Metadata/global-metadata.dat`, IL2CPP metadata v31.
Extracted with `../inspect_coin_metadata.py`; machine-readable fields, method
signatures and defaults are in `metadata-coin-types.json`. These are actual
metadata values, not estimates from video. Primitive type IDs were verified
against `System.Boolean` (16204), `System.Int32` (23131), and `System.Single`
(31427) in this metadata. Integer defaults use the metadata's compressed signed
encoding; float defaults are IEEE-754 little endian.

## Recovered presentation constants

| Type | Field | Value |
| --- | --- | --- |
| Home `CoinCollectAnimation` | `Count` | 10 |
| Home `CoinCollectAnimation` | `ApproximateAnimationDuration` | 2.4 s |
| Home `CoinCollectAnimation` | `AnimationTimeout` | 4.8 s |
| Home `CoinCollectAnimation` | `AmountScale` | 0.25 |
| `CoinInfoView` | `FirstScale`, `SecondScale` | 1.05, 0.98 |
| `CoinInfoView` | `FirstDuration`, `SecondDuration`, `ThirdDuration` | 0.08, 0.13, 0.06 s |
| Gameplay `CoinCollectHelper` | `MaxDelay`, `MaxNormalizedTime` | 0.2, 0.2 |
| `RemainingMovesCoinCollectAnimation` | `Count` | 10 |
| `RemainingMovesSingleCoinPilot` | `FirstVectorStartTime`, `FirstVectorFinalSpeed` | 0, 0 |
| `RemainingMovesSingleCoinPilot` | `SecondVectorStartTime`, `SecondVectorDuration` | 0.45, 0.1 s |
| `RemainingMovesSingleCoinPilot` | `SecondVectorInitialSpeed`, `SecondVectorFinalSpeed` | 3, 0 |
| `RemainingMovesSingleCoinPilot` | `FinalVectorDuration` | 0.6 s |
| `RemainingMovesSingleCoinPilot` | `FinalVectorInitialSpeed`, `FinalVectorFinalSpeed` | 0, 20 |

The pilot's `FirstVectorInitialSpeed`, `FirstVectorDuration`, and
`FinalVectorStartTime` are mutable fields without metadata defaults. Their actual
values and mathematical interpolation require native method bodies or a measured
runtime trace. Therefore the table does not establish an exact full trajectory.

## Recovered responsibilities

- `CoinCollectAnimation.Play` takes amount, start position, flags for level win,
  reward source and ad reward, and collect type. It has separate
  `PlayAmountAnimation`, `PlayCollectAnimation`, `PlayCoinAppearSound`, and
  `OnItemReached(itemView, isLast)` methods; fields include `completedAmount`,
  `increment`, `lastCoinAlreadyHit`, and `hitParticles`.
- `CoinInfoView` tracks pending animation data separately from the current
  displayed amount. Methods include `AddAnimationData`,
  `RemoveCompletedAnimationData`, `AnimationCantFinish`, `UpdateCoinsText`, and
  `PlayHitAnimation(amount, timeFactor = 1)`.
- `CoinItemView` has idle, flip, non-looping flip and tap animation identifiers;
  separate `baseView` and `shadowView`; and methods for UI sorting and collect
  shadow preparation. `PlayFlipNoLoopAnimation` defaults to speed 1, time 0.
- `CoinBezierCollect` has distinct `CollectToGoal` and `CollectToUserInfo`
  coroutine methods. Its compiler-generated state fields include collect
  duration, speed variation, start/max rotation and shadow positions. A
  landscape-specific collector also exists. Method names establish the use of
  separate paths, but do not expose control points or exact easing.
- `RemainingMovesCoinCollectAnimation` tracks `totalItemsSent` and
  `totalItemsReached`; chooses the first position by index; supports tap to skip;
  and calls `OnItemReached` per pilot. `RemainingMovesCoinTracer` adjusts particle
  rate over distance by a ratio.

## Economy constants (do not infer a complete formula)

`RemainingMovesManager` declares `RemainingMoveCoinValue = 2.5`,
`DefaultInitialCoinAmount = 10`, `HardInitialCoinAmount = 100`,
`SuperHardInitialCoinAmount = 150`, `ExtremelyHardInitialCoinAmount = 200`.
There are runtime reduced-difficulty amounts and an `extraCoinAmount` field.
`CollectRemainingMovesCoin` receives both cell and special-item model. The
formula and rounding are in compiled native method bodies, so these constants
alone do not justify changing Royal Throne's economy.

## Video evidence already captured

`Design/RoyalMatchReference/OBSERVATIONS.md` and `PROGRESS.md` document the
official Play-signed version 38174 run; the supplied merged APK was not run.
The latter report states that its native library and injected Java classes
differ from the official version. This inspection only reads APK contents.

- `recordings/journey_001_024023.mp4`: 130.5 s celebration begins; around 133.5 s
  targets become Reward; 134.0-135.5 s leftover moves launch; 135.5-147.5 s booster
  finale sends gold coin bursts into Reward; 148.0 s results card. Final reward 64.
- The closer sampling below supersedes the earlier approximate Home timing and
  order in `OBSERVATIONS.md`: Home collection is in the end of
  `journey_004_024927.mp4`, and the star flies before the coins.
- Existing measurements have approximately +/-0.5 s sampling uncertainty and
  were captured under ARM translation. They must not be called exact timings.

## Closer motion inspection (2026-09-28)

Two compact 8-by-3 contact sheets were extracted with the existing local
`C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe` and visually read.
Read each sheet left-to-right, top-to-bottom. These are sampled video frames;
no graphical reconstruction was used.

`reference-finale-135.5-147.0.jpg`: `journey_001_024023.mp4`, sampled every 0.5 s
starting at 135.5 s.

- Coins burst from several booster positions across the board. The initial
  spread forms small outward fans/rings around a burst, before coins stream
  toward the upper-left Reward icon.
- The coin visibly alternates between a round face and thin gold edge; it has a
  dark offset shadow during collection. Different coins show different flip
  phases simultaneously.
- Gameplay coin flights have bright white-gold trails with small star/sparkle
  points. Several trails overlap during the large first burst. The effect fades
  behind coins after they reach the counter.
- Arrivals update the Reward number during the flight wave, rather than waiting
  for the entire sequence. The opening large wave moves it from 10 to 51; later
  booster bursts bring it to 64. The last visible pair reaches the counter by
  around 146.0-146.5 s, after which trail remnants fade.
- The board keeps resolving behind the coin flights. Coins can fly beyond the
  board rectangle; collection is not clipped to the grid.

`reference-home-157.75-160.625.jpg`: `journey_004_024927.mp4`, sampled every
0.125 s starting at 157.75 s.

- The star appears first around 157.75 s, with a bright yellow beam and nearby
  sparks. It starts toward the top-right star counter while the coin pile and
  `+64` appear beneath/behind it around 158.375 s. The star reaches the HUD around
  158.75 s. This corrects the previous broad observation's order.
- The coin origin is near screen center horizontally and about two-thirds down
  the screen. Coins briefly gather in a small pile beneath the white/gold `+64`
  label before departing one after another around 159.125 s.
- Coins accelerate upward on a gently leftward route to the top-left inventory
  coin icon. The stream is visibly staggered, with simultaneous coins at the
  source, middle of the path and counter. Face-to-edge flips continue in flight.
- A large bright white-gold tracer is visible behind the star. The Home coin
  stream does **not** show the strong continuous gameplay tracer in these frames.
  Small contact flashes/sparkles are visible at the coin HUD.
- The counter remains 2000 during the initial pile/spawn. First arrival is around
  159.625 s (2006), followed by 2012, 2024, 2030, 2036, 2042, 2054, and 2064 by
  around 160.5 s. Sampling misses individual arrivals between some frames.
  This is consistent with the metadata's ten representative coins, increments
  of six for a 64 reward, and a final remainder adjustment; exact arithmetic
  code is still not recovered.
- The `+64` label fades as the final coins depart. The first-time tutorial dim
  starts around 160.125 s while a few final coins are still traveling; it does
  not delay all further UI until every particle disappears.

Home timings have approximately +/-0.125 s sampling uncertainty; gameplay sheet
timings have +/-0.5 s uncertainty. They are timestamps in this recorded emulator
playback, not recovered native animation timings. The emulator used ARM
translation and its frame pacing may be slow or irregular. No constant scale
factor from recorded seconds to native animation seconds has been established.

## Limits

This is metadata extraction, not recovery of original C# source or native
method bodies. Constant values are exact to their serialized float precision.
Behavior inferred from fields/method names is identified above. Full control
flow, Bezier points, timing expressions, curve selection, sound pitch/gain, and
reward formula remain unverified unless backed by a serialized asset or trace.
