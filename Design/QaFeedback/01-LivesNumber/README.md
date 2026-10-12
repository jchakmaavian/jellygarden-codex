# QA feedback 1: lives number inside the heart

The user clarified that this item concerns the Home resource bar's lives number and supplied a visual reference. Only the number's position changed.

`ReferenceHomeLayout.LifeCountPx` moves the text box 1.5 reference pixels left and 4 pixels up at a 1080-wide layout. Its dimensions, font, size and color are unchanged. The offset compensates for the display font's visible digit sitting below its centered line box.

Verification on 7 October 2026: inspected the actual 720x1280 Unity homepage; previewed the same anchor displacement in Play Mode without changing player resources; visually reviewed `before-after.png`. Runtime source compilation passed with one existing unused-field warning. Unity's requested editor recompile remains deferred during the existing Play Mode session. No Android rebuild or physical-device check was performed.

`before-portrait.png` and `after-portrait.png` are full Unity captures retained locally. The comparison is a crop of those captures. Other QA items were left untouched.

Git origin currently points to `avianit/king-leo`, which differs from the authorized `jchakmaavian/jellygarden-codex` backup. No push to that origin was performed.
