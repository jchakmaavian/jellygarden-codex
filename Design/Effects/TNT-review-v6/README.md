# TNT Reveal and Idle frame animations in the game

This revision puts the approved frame sequences from `Design/Animation/TNT-Reveal-v1` and `Design/Animation/TNT-Idle-v1` on the board TNT. A match that creates TNT now plays the Reveal once, switches straight into the seamless Idle loop, and stops the Idle the moment the TNT is activated. The existing anticipation and explosion then run unchanged.

## What the player sees

- **Reveal.** All 120 Reveal frames play once over the approved gameplay reveal time, `TNT3DAnimator.RevealDuration` = 0.40 s. At 60 fps every fifth frame is on screen (001, 006, … 116). The frames already contain the lift, flames and sparkles, so they replace the old procedural sprite pop, rim glow and reveal sparkles. The creation sound is unchanged.
- **Reveal to Idle.** Idle 001 follows on the same clock. It is pixel-identical to Reveal 120 (0 differing visible pixels), so there is no jump.
- **Idle.** The 120 Idle frames loop at 60 fps (2.00 s) and wrap from 120 to 001 without a seam.
- **Activation.** On the next rendered frame no animation frame is drawn. The director's existing anticipation (the static TNT art squashing and warming for 0.12 s) takes over, followed by the existing blast, debris and cleanup.
- **Size and position.** Idle 001 lands on the previous static board TNT. An off-screen A/B render of the same cell (TNT about 259 × 309 px) found the best pixel shift at (0, 0), with a mean channel difference of 1.68/255. The remaining difference is resampling on the outline, plus the fuse spark, which the frames rebuild by design (see `normal-alignment-*.png`).
- **TNT without a reveal**, for example a colour-ball conversion or the QA "TNT animation" tool, joins the Idle loop at frame 001 in the same frame, so the static art never flashes.

## How it is built

- **Frames.** `Assets/JellyGarden/Resources/RoyalTNT/Frames/Reveal/TNT_Reveal_001–120.webp` and `…/Idle/TNT_Idle_001–120.webp`. They are unmodified copies; SHA-256 matches the approved files for all 240.
- **Import.** The frames use the existing `RoyalWebPImporter` with two new opt-in settings, enabled on these 240 assets only. Every other WebP imports exactly as before.
  - `bleedTransparentEdges` copies nearby colour under fully transparent pixels, as Unity's *Alpha Is Transparency* does for the static TNT art. Filtered or compressed edges no longer pull in black. Alpha and visible pixels are unchanged.
  - `astcOnMobile` uses ASTC 6×6 when importing for Android or iOS. Desktop stays BC3.
  - Every frame is 512 × 512, with no mipmaps, bilinear filtering and clamp: the same sampling as the static TNT art.
- **Memory.** About 28 MB for all 240 frames on Android and iOS, and 63 MB on desktop. Phone GPUs cannot use BC3, so without `astcOnMobile` they would decompress the frames to about 250 MB.
- **Runtime.** `TNTFrameAnimator` (new, `Scripts/Juice`) sits on the TNT piece and drives a child sprite renderer, `TNT frames`, under the piece's sprite:
  - The sprite pivot is the barrel base, frame pixel (256, 480) = (256, 32) from the bottom. The rig maps art (256, 458) to that point.
  - The child sits at the art's barrel base under the board sprite's centred pivot, local (0, −2.02), at scale 1 / 0.84 = **1.19×**.
  - It follows the piece's position, cell fit, hint pose, colour and sorting layer. It draws one sorting order above pieces, so the Reveal's lift and flames overlap neighbouring pieces rather than hiding behind them.
  - The piece keeps its static TNT sprite, hidden, so cell fit, collect-target matching (by sprite name) and hint geometry behave as before.
- **Hooks.**
  - `TNT3DAnimator.RevealSprite` starts the frame Reveal. The pooled reveal actor still holds the new piece in place, plays the creation sound and returns to the pool at 0.40 s, but it no longer draws the procedural pop or glow while frames play.
  - `RoyalPieceSkin` attaches the animator to any board TNT.
  - Activation is detected from the piece's `destroying` flag, which every activation path sets: tap, swap, combo, chain blast and booster.
- **Loading.** `TNT3DAnimator.Prewarm` loads the sequences once, when the effects director first starts. If the frames are missing (for example, the editor had no FFmpeg during import), one warning is logged and TNT keeps the previous static art and procedural reveal.
- **Unchanged.** The explosion VFX, anticipation, damage order and radius, pool sizes, gameplay rules, rockets, UI and levels were not edited.

## Verification

Unity 6000.5.10f1, Android build target, Play Mode. I opened the current level with *Tools ▸ Royal Throne ▸ Effects ▸ Open current level for testing without life cost*. Then I ran *Tools ▸ Royal Throne ▸ TNT ▸ Verify TNT only at quarter and full speed*, which builds a disposable 8 × 8 board and restores the level afterwards. **All 35 checks passed** (`checks.txt`), at 0.25× and 1×:

- A T-match resolved by the production `FindMatches` pass creates the TNT.
- The TNT's first rendered frame is Reveal 001, with no static TNT and exactly one frame renderer.
- Every rendered frame (617 at 0.25×, 155 at 1×) draws exactly one expected frame. The fixed barrel-base pivot and 1.19× scale hold throughout, with no static flash, missing sprite, procedural pop or duplicate.
- The Reveal plays once, never goes backwards and never replays: 24 rendered frames at 1× and 96 at 0.25×, both 0.40 s.
- Idle 001 directly follows the last Reveal frame.
- The Idle loops 120 → 001 with no skipped or stalled frame; at 1×, every one of the 120 frames is shown.
- At 1×, the Reveal runs on a reused pooled TNT actor and still starts at 001.
- Activation during the Idle stops it on the next rendered frame, and exactly one static TNT sprite (the existing anticipation) takes over.
- The existing explosion checks still pass: white-hot ball, gold edge, shockwave, debris, residual light, stars and local impacts. All pooled effects and debris finish, and pooled actors reuse cleanly.
- Coins and lives are preserved, and no runtime errors were logged during the run.

The three v5 reveal checks asserted the procedural glow and the static idle sprite that these frames replace. They are superseded by the frame checks above.

Two further checks ran outside the suite:

- **No-reveal TNT.** On the live level board, a piece switched to TNT showed Idle 001 immediately, with the static art hidden. It kept looping, and switching it back restored its normal sprite.
- **Console.** After the run, the console had no new errors. The restored level was back in play with input unblocked.

Not tested: full-game QA, phone performance and first-load time on a device, and a TNT under a chain overlay, which also draws at sorting order 3.

## Evidence

- `TNT-reveal-idle-normal.mp4`: 155 consecutive rendered frames from TNT creation, 1×, 60 fps capture, cropped on the TNT at 2×.
- `TNT-reveal-idle-quarter-playback.mp4`: the same frames played at quarter speed for inspection.
- `reveal-idle-sequence.jpg`: labelled samples from Reveal 001 to Idle 120.
- `TNT-activation-normal.mp4`: 78 rendered frames from activation through cleanup.
- Phase PNGs at both speeds: reveal-001, reveal-peak, reveal-to-idle-001, idle-031, ignition, blast-peak, rays, falling-debris.
- `normal-alignment-idle001-frames.png`, `normal-alignment-static-board-sprite.png`, `normal-alignment-difference-x4.png`: the A/B size and position check.

## Source backup

`verified-source` holds the six changed files and their metadata. `baseline` holds the five pre-change originals. `incremental.patch` applies to the baseline and reproduces the verified sources, including the new `TNTFrameAnimator.cs`.

- Runtime edits: `TNTFrameAnimator.cs` (new), plus small hooks in `TNT3DAnimator.cs` and `RoyalPieceSkin.cs`.
- Editor edits:
  - `RoyalWebPImporter.cs`: the two opt-in import settings.
  - `RoyalTNTChecks.cs`: the frame checks above.
  - `RoyalEffectsChecks.cs`: this output folder.

The project still contains earlier unrelated uncommitted changes, so this review directory is backed up independently, as in v5.
