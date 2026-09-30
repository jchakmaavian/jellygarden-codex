# Coin collection correction — 2026-09-28

Implemented in the current project without a commit, push or APK build.

## Changes

- Finale starts with the existing 10-coin base. Accepted booster activations, including chained activations, emit local coin bursts. Removed random-piece timer emissions. Remaining presentation value is reserved before flights launch and drained before Win; it cannot emit additional weighted coins while draining.
- Home starts the progression star first, then overlaps the coin pile and staggered collection. The amount label remains visible through initial arrivals. Coin destinations follow the actual HUD icon.
- Counter increments use equal integer amounts, with the remainder on the last of ten arrivals. Example: 64 produces 6,12,18,24,30,36,42,48,54,64.
- Repeated contiguous Win notifications cannot credit coins or life twice. Visual arrivals never award currency; the existing save-before-presentation boundary remains.
- Cancellation retires callbacks, particles and coin voices. Shutdown no longer creates an audio director simply to stop coin cues. Coin spread uses a separate random generator so presentation does not consume board-refill randomness.
- Approved coin artwork, native flip keys, imported particle assets and SFX, BGM/configuration, booster motion, and level data were retained.

## Verification

`Validation/flow-checks.json`: PASS. Real isolated level 1 completion with three remaining moves → three accepted booster bursts → Win → actual Continue → Home. Saved payout occurred once; duplicate Win did not recredit; star/coins overlapped; all ten displayed increments and the final remainder were verified. Cancellation after three arrivals produced no further callbacks or currency changes. Normal economy/progress/preference keys were restored.

`Validation/checks.json`: PASS. Native flip poses, Home/finale flight rendering, SFX, early cancellation, reuse and cleanup. Requested 30 FPS measured 28.6–30.2; requested 60 measured 59.6–59.9. No runtime errors were logged in either final test run. Unity compiled and was returned to Edit Mode. Its existing licensing-service 404 messages remain unrelated.

`Coin-Collection-Logic-20260928.mp4` is a **silent** 5.56-second capture of the real Continue/Home sequence. The 132 source frames and actual timestamps are in `Validation/HomeFlow-20260928-130419/`; capture overhead yielded about 24 FPS. Playback preserves measured time, without slowing the animation. SFX playback was checked separately through live AudioSources.

## Reference boundaries

Reused `Design/Inspection-rfv387/CoinLogic/README.md`, the recovered rfv387 metadata/animation/VFX data, and the existing official-version reference recordings identified there. The original APK is unchanged.

The full native payout formula, precise Bezier control points and original compiled scheduling code are not recovered. Existing Royal Throne rewards (10 + 5 per star) remain unchanged. Distributing that already-authorized amount among actual booster bursts is reconstructed presentation accounting, not an asserted port of the reference economy. The star-first overlap and integer/remainder counter behavior follow the recorded evidence. Later-level win-card routing remains intact.

Runtime files changed: `RoyalWinSequence.cs`, the accepted-destruction notification in `Item.cs`, and the duplicate-Win guard in `LevelManager.cs`. Test helper: `Assets/ReferenceGameplay/Editor/ReferenceCoinChecks.cs`.

Pre-edit runtime files: `Checkpoint/CollectionLogic-20260928/`. Pre-edit helper: `Checkpoint/ReferenceCoinChecks.before-flow-20260928-185004.cs`.
