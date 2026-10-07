# Win fanfare (Royal Throne)

Requested by the user on 7 October 2026: "add a trumpet celebration music" to the level-won celebration (logo, pennants, four herald trumpets, "Tap to Skip"), so the player feels the satisfaction of the win.

- **In game:** `Assets/JellyGarden/Resources/RoyalGameplay/Audio/royal-fanfare.wav` (3.5 s, stereo 44.1 kHz, -1 dBFS peak). `RoyalWinSequence.RunCelebration` plays it 0.70 s into the celebration, replacing the inherited JellyGarden "complete" jingle. That jingle remains the fallback if the clip is missing. If the level is abandoned mid-celebration, the fanfare stops. After a win, it rings on into the booster session.
- **Source:** original and synthesised from scratch by `make_fanfare.py` in this folder (numpy only, with no samples or third-party audio). The script stays local, like other non-Markdown files in `Design/`. Regenerate with `python make_fanfare.py <out.wav>`. `analyze.py` prints its articulation and brightness.
- **Score (A major; trumpets in three parts, trombones, horns, timpani, snare, cymbal, glockenspiel), in clip seconds:**
  - 0.05–0.23: "ta-ta-ta" pickups as the pennants unroll.
  - 0.32: first chord.
  - 0.62–0.89: rising arpeggio. The held top note lands as the trumpets swing in.
  - 1.23–1.86: IV–I–ii–I6–V cadence over a snare roll.
  - 1.95: the final chord with cymbal, timpani and a glockenspiel sparkle, on the logo's swell.
  - 2.45: release; it fades out by 3.5 s.
- **Verified 7 Oct 2026** in Play mode (QA practice session, forced win): the fanfare started 0.75 s after the win began and ran 3.5 s. The level reached Win, and the console showed no errors.
