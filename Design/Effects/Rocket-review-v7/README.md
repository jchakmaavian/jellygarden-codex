# Rocket Reveal, Idle and Launch animation

This revision animates the board Rocket through its whole life: Reveal (created by a Match-4), Idle (a calm loop) and Launch (anticipation, then hand-off to the directional Passing sprites). The existing FireExhaust, travel speed and flight system are unchanged.

The behaviour follows the Royal Match reference `00_References/animation-VFX/rocket animation.mp4` (60 fps), studied frame by frame alongside the `Rocket Screenshots` stills:
- **Reveal (frames 96–132):** a Match-4 piece merges, then a small Rocket appears inside a light pop. It emerges full size inside a sparkle ring and settles.
- **Idle (frames 0–31):** calm. The only motion is a highlight sheen passing across the body.
- **Launch (frames 32–40):** the Rocket stretches along its travel axis on the first activated frame. The halves separate about 50 ms later, and flame shows by about 100 ms.

`reference-vs-ours-reveal.jpg` and `reference-vs-ours-launch.jpg` place the reference and our Rocket at the same frame spacing.

## What the player sees

**Reveal: 0.48 s, then Idle with no pop.**
- A quick light flash and white stars pop in the cell as the Match-4 completes.
- The Rocket appears at 30% size with a bright highlight, tilted 16°.
- By 50 ms it stretches to 1.20 × 0.82 along its axis. At 120 ms it squashes to 0.88 × 1.12.
- It flicks back 8° while overshooting to 1.15 of its size (130 ms).
- It rebounds to 0.93 (250 ms), then settles through 1.025 (360 ms) to rest at 480 ms.
- Its largest footprint is 0.89 of a cell. The first Idle frame differs from the last Reveal frame by at most 0.2%.

**Idle: a 2.2 s seamless loop.**
- The body breathes by up to 2.2% along its axis and 1.2% across it, and rocks ±1.6°.
- Once per loop, a soft highlight sheen crosses the body over 0.48 s. It is clipped to the Rocket's own silhouette.
- A small four-point glint then flashes on the nose cone the sheen left (0.36 s). The sweep alternates direction each loop, so the glint alternates noses.
- The Rocket never moves off its cell centre (at most 0.0008 world units) and never exceeds 0.81 of the cell.

**Launch: activation to Passing sprites in 0.133 s at 1×.**
- On the activation frame, a stand-in copies the Idle Rocket's exact pose, so the piece continues without lag.
- It compresses along its travel axis to 0.84 (with 1.10 across) and straightens its rocking.
- It stretches to 1.24, then punches to 1.36 while a charge glow builds.
- In the ignition frame, the correct directional sprites take over together with the existing launch flash, and the existing travel begins.
  - Horizontal Rockets use `rocket_left` and `rocket_right`.
  - Vertical Rockets use `rocket_up` and `rocket_down`.
- No frame is missing and no frame shows both.
- The anticipation still uses the existing 0.12 s `rocketAnticipation`. Gameplay timing is identical.

## What did not change

- **FireExhaust.** The shader and all eight Rocket FX materials are unchanged since the approved v6 review.
- **Flight.** Travel is still 15 cells/second with a seven-cell exhaust. The Passing sprites keep their 0.64-cell size, nozzle pivots and exhaust attachment.
- **Directional art.** The four in-game directional PNGs are pixel-identical (0 differing visible pixels) to `Royal_Throne_Gameplay_Assets/02_powerups/rocket_4_direction/*.webp`. No sprite is rotated.
- **Everything else.** Gameplay rules, damage order, the match detector, TNT, UI, levels and the other power-ups were not edited.
- **Combos without a stand-in.** Rocket combos, helicopter payloads and Effects Workshop previews launch without a board piece to copy. They keep the previous flyer compression.

## How it is built

- **`Scripts/Juice/RoyalRocketPieceAnimator.cs`** (new) sits on Rocket pieces.
  - It poses only the piece's sprite child, after `RoyalPieceSkin` fits it to the cell. The Item transform, collider and state are untouched.
  - Overlays are children of the sprite: the reveal highlight, the sheen with its SpriteMask, and the glint.
  - Each new Rocket starts its own Reveal at 0 s, because pieces are not pooled.
- **`Scripts/Juice/RoyalRocketLaunch.cs`** (new, part of `RoyalPowerupDirector`) adds six pre-built launch stand-ins, the stand-in pose curve and the reveal light pop.
  - The pop emits into the existing Rocket glow and star emitters.
  - Stand-ins are released at ignition and on Cancel.
- **`RoyalRocketSequence.cs`** changes only in the anticipation loop: a stand-in replaces the flyers' compression when the activated piece is at the origin. It also builds and clears the stand-ins.
- **`Item.RoyalRocketCreation`** now starts the animator's Reveal and waits for it, keeping `RoyalRocketRevealing` for `Item.Start`. The former root-transform reveal code is removed.
- **`RoyalPieceSkin`** has one line that applies the pose.
- **Editor tests:** `RoyalRocketChecks.cs` gains the reveal, idle and launch checks below, and `RoyalEffectsChecks.cs` writes to this folder.

## Verification

Unity 6000.5.10f1, Android build target, Play Mode on the current level. I ran *Tools ▸ Royal Throne ▸ Effects ▸ Verify rocket travel at half and full speed*. It uses a disposable 8 × 8 board, the real Match-4 detector and destroy/replace path, and the real Rocket activation path, at VFX Lab speeds 0.5× and 1×. A fixed 60 fps step keeps every sampled frame reproducible. **All 168 checks passed with no runtime errors** (`checks.txt`).

**Reveal (horizontal and vertical, both speeds)**
- The Rocket appears in place as the Match-4 finishes, as a fresh Reveal at 0 s.
- It starts at 30% size inside the light pop, tilted.
- Stretch reaches 1.46 and squash 1.27 (length ÷ width ratios), with a 16° flick and counter-swing.
- It overshoots to 1.15, rebounds to 0.94 and settles in 0.483 s.
- Reveal → Idle changes by at most 0.2%. The peak footprint is 0.886 of the cell, and the Item scale is restored.

**Idle**
- The loop runs past its 2.2 s loop point, at most 0.807 of the cell with no drift.
- It rocks 1.60° and breathes 2.2%.
- The sheen and glint both reach full strength.
- The largest frame-to-frame step is 0.05% and 0.076°.

**Launch**
- The first launch frame continues the Idle Rocket in place.
- It compresses to 0.83, then stretches to 1.34 along the travel axis.
- The switch to Passing sprites happens in one frame: after 8 anticipation frames (0.133 s) at 1×, and 15 frames (0.125 s) at 0.5×.
- Up/down and left/right use their dedicated sprites, unrotated.
- The v6 flight checks still pass:
  - the anticipation is still 0.12 s;
  - flight holds 15.00 cells/s;
  - the seven-cell FireExhaust stays attached to each nozzle;
  - trail lifetimes are unchanged, and pools and debris clean up with no object growth.

I also inspected the live board off-screen. The sheen draws only inside the Rocket silhouette. It uses the default alpha-blended sprite material, because the additive overlay material did not draw through a SpriteMask in this setup.

Not tested:
- Full-game QA.
- Phone performance.
- Combos that launch several Rockets at once.
- A Rocket under a chain overlay.

The disposable board uses the porcelain-vase colour for its Match-4. Those shards (the existing match-break effect) fly over the reveal in the captures.

## Evidence

- `Rocket-horizontal-1x.mp4` and `Rocket-vertical-1x.mp4` show consecutive rendered frames at 60 fps. Each clip covers the Match-4, reveal, a full idle loop, then launch into flight.
- `Rocket-horizontal-quarter-playback.mp4` and `Rocket-vertical-quarter-playback.mp4` play the same frames at quarter speed.
- Phase PNGs at both speeds:
  - `reveal-0-000ms`, `-1-050ms`, `-2-130ms`, `-3-250ms`, `-4-settled`;
  - `idle-sheen`, `idle-sparkle`;
  - `launch-first-frame`, `launch-stretch`, `launch-passing`;
  - `mid-*`: the unchanged mature exhaust in flight.
- `reference-vs-ours-reveal.jpg` and `reference-vs-ours-launch.jpg`: the reference against our Rocket.

## Source backup

- `verified-source/` holds the seven changed files and their metadata.
- `baseline/` holds the five pre-change originals.
- `incremental.patch` applies to the baseline and reproduces the verified sources.
- `source-hashes.txt` records the verified files.

This folder is backed up independently, as in earlier reviews, because the project contains unrelated uncommitted work.
