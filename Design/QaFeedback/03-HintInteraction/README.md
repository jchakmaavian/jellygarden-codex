# QA feedback 3: matching-area hint interaction

Implemented the user's final revised specification on 7 October 2026. The existing marking line, glow and sparkles remain. Every gem in the matching area, plus both actual exchange partners, pulses and brightens. Only the exchange partners move inward.

After the existing one-second idle threshold reveals the line, the interaction waits 0.4 seconds. All affected gems grow from 1.0x to 1.06x using a smooth sine-squared approach and return over 1.2 seconds. A 0.35-second rest separates successive cycles. Pooled additive sprite copies brighten the artwork by approximately 23 percent at peak, slightly brighter than the earlier 16 percent version. Both exchange partners move an equal distance, capped by the expanded artwork's available spacing to preserve a gap from neighbouring gems. Cell owners, Item root transforms and colliders are unchanged. Touch, invalidation and cancellation immediately restore all affected artwork. Reduced motion retains the static line.

Unity 6000.5.10f1 compiled both runtime and editor code with no compilation errors. The focused `RoyalHintPresentationChecks` suite and QA-session restoration check passed 141 checks with zero failures. Coverage includes six match shapes across five gem designs, horizontal and vertical swaps, actual idle hint discovery, two full interaction cycles, no artwork overlap throughout either cycle, exact root/collider/ownership preservation, unaffected neighbouring gems, glow reuse, immediate input cancellation, pause, invalidation, preferences and reduced motion. Unity reported no runtime errors during the run. Peak checks sample actual rendered frames rather than assuming fixed scheduling.

Testing used the isolated QA save profile and disposable practice board. Normal balance and lives were restored, QA routing was disabled, the test runner retired, and the scene remained clean. No Android rebuild or physical-device verification was performed.

Local visual evidence:

- `hint-interaction.gif`: 3.66 seconds of actual Unity frames at their measured capture intervals, cropped to the board.
- `hint-interaction-frames.png`: pause, approach, return/rest and second-approach frames.
- `raw/`: original 720x1280 Unity screenshots and capture timestamps.
- `checks.txt`: the complete final 141-check result.

The Excel tracker records the revised specification and marks only feedback 3 newly fixed. Feedback 4's line-selection concern remains open. Other QA items were not changed.

Git origin points to `avianit/king-leo`, rather than the authorized `jchakmaavian/jellygarden-codex` backup. No push to the mismatched origin was made.
