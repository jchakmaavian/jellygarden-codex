Per-frame logs from the Unity Play Mode verification on 2026-09-24 (level 12, QA test profile).
Each row: step, frame, game time, unscaled time, real time, game state, HUD alpha, HUD excitement,
slot, track-0 animation, track time, loop, mixing-from, number of tracks, actions fired this step.
Rows that are not per-slot are notes: QA,<status> or CELL,<col/row> (power-up placed there).

rocketclip.csv    the recorded clip: rocket at step 45, happy once from 2.27 s game time, back to idle
rocketrepeat.csv  three rockets; the second lands during the first reaction and is ignored
win1.csv/win2.csv win card: happy on show, and a fresh start on the second show
pause.csv         in-game pause and Continue
fail.csv          fail card: neutral idle
clip.csv/repeat.csv  the first attempt with TNTs: TNT reactions stay below the 0.5 cheer threshold
run-report.txt    driver output for the first full run (home, level, pause, leave/reopen, win, fail)
