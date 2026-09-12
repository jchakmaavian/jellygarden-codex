# Implemented victory card

These are native captures from the running Unity player, not design mockups.
The temporary preview player uses a separate company/product name and save.
Its development watermark is not part of the card.

- [First win, 393x852](first-win-393x852.png)
- [Replay, 346x641](replay-346x641.png)
- [Replay, 768x1024](replay-768x1024.png)
- [Star entrance and idle animation](star-animation.gif)

## Behavior

The crowned blue banner reads “Level Complete!” above the level number.
The star bounces in, then floats and tilts with a soft pulsing halo, slow rays
and twinkles. A short gold confetti burst accompanies the entrance. The reward
badge and Continue fade in after the star. All animation uses unscaled time.

First wins read “+1 Star earned”; replays read “Star already earned” with the
same bright gold star. StoryFlow still owns all awarding. Continue follows its
existing callback once. External close or an interrupted entrance releases the
background snapshot and cancels presentation.

The whole card fits inside the screen safe area with uniform scaling. The
background is a one-time blurred GPU snapshot of the actual board and HUD.

## Try it in Unity

Enter Play mode, then select either:

- JellyGarden > Story > Preview > Open Victory (first win)
- JellyGarden > Story > Preview > Open Victory (replay)

These previews do not award a star. Completing a real level uses the same UI
through the existing StoryFlow hook.

## Verification (2026-09-10)

Passed:

- Unity macOS development build: zero errors (existing project warnings remain).
- Globbed C# typecheck: zero errors, 74 warnings.
- `StoryWinPopupTests.Run`: first/replay presentation, no reward mutation,
  duplicate Continue, stale callback cancellation, reopen, bounce and idle at
  timeScale zero, and imported art/shader.
- Running player: interrupted entrance, repeated Continue, animation while
  paused, and card bounds at 393x852, 346x641 and 768x1024. No probe failures.
- Content validation, VFX preservation check, launch check and diff whitespace.

Existing broader-suite failures:

- Screen tests: avatar idle frame expectation; two navigation signs versus four;
  25 shell repairs versus 24; 29 shell stars versus 28. Running the suite with
  the original pre-redesign popup produced the identical four failures.
- Smoke tests: expected 807 tasks / 1209 stars versus the current 806 / 1208.
  The same run successfully simulated restoring all 150 areas with 1208 stars.
  This redesign does not change story data or these expectations.

## Artwork

The three new independent sprites are saved as
`Assets/JellyGarden/Resources/Story/Art/win_victory_{banner,star,button}.png`.
Masters, built-in imagegen provenance and exact prompts are documented in
[tools/art/win/VICTORY.md](../../tools/art/win/VICTORY.md).
Reinstall from the masters with `python3 tools/win_art.py`.
