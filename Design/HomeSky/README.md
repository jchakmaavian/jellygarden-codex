# Homepage background and clouds

Imported the supplied Homepage_bg.png and three transparent cloud PNGs unchanged. SHA-256 provenance is recorded in imports.json. Originals remain under Design/Backup.

ReferenceHomeSky replaces the live homepage background and places three clouds behind the existing controls. Each cloud has bounded, continuous horizontal drift with a different period (170 / 205 / 240 seconds), plus a tiny vertical drift. Motion respects HomeMotion and pauses while the homepage is hidden. Gameplay, music and player data are unchanged.

Verified in Unity Play Mode: live imported background, exactly three clouds, all three moving, input/layering, reduced motion, hidden-home pause, Settings opening, and restored normal player data. All checks passed with zero tracked errors. Static compilation passed. Before/after-12-second screenshots show the normal-speed drift. No full-game QA was performed.

Checkpoint: see checkpoint.txt. Editor Play Mode options are temporarily changed by the fresh-state QA helper and restored upon exiting Play Mode. No scene is saved by these checks.
