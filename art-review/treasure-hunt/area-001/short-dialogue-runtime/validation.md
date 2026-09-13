# Area 1 — approved short dialogue

Implemented and verified on 2026-09-12. Area 1 now has four dialogue cards across two scenes, reduced from thirteen.

**Opening, after the first win:**

- Leo: “Welcome, Puzzle Partner!”
- Pip: “I’m Pip! Six keys lead to one royal surprise!” — retains the map cue.

**Ending, after the lantern transition:**

- Pip: “Look! The sun is our first clue!” — retains the sun close-up.
- Freya: “Next, let’s restore the courtyard fountain!”

Tasks 1–3 return to their updated task panel after the existing native animation. Empty `speaker` and `onComplete` fields omit ordinary task cards. The final discovery is delivered independently of these fields. Existing scene IDs, replay, skip, save flags, star costs, and the Area 2 readiness boundary are preserved.

## Validation

- Compiler consistency and all 17 authoring regression tests pass, including optional dialogue and incomplete speaker/line rejection.
- Unity campaign checks: 0 failures. Checks cover the exact four approved lines, map/sun cues, silent repairs returning to the panel, callbacks without blank cards, final discovery recovery, skip/replay, saved progress, and content locks.
- C# typecheck: 0 errors, 96 warnings. macOS development build: 0 errors, 379 warnings.
- Protected kit VFX and launch configuration checks pass.

| Real-player run | Final wins / tasks / stars | Result |
|---|---|---|
| [Opening replay and door repair](opening-door/runtime-check.json) | 2 / 2 / 0 | Passed |
| [Flag repair](flags/runtime-check.json) | 3 / 3 / 0 | Passed |
| [Lantern repair and ending](ending/runtime-check.json) | 4 / 4 / 0 | Passed |

All three runs copied earned checkpoints into isolated saves. They used the actual repair buttons, verified animation and panel/discovery continuation, reloaded saved progress, and reported zero runtime errors. Opening and ending each displayed exactly two cards. Area 2 remains locked.

Visually inspected the approved dialogue, map cue, sun close-up, and completed task panel at 393 × 852 and 320 × 568 in the macOS player. Examples: [Leo's welcome](opening-door/opening-0.png), [Pip's introduction](opening-door/small-phone-opening-1.png), [door task panel](opening-door/completed-panel-2.png), [Pip's clue](ending/discovery-0.png), and [Freya's next objective](ending/small-phone-discovery-1.png).

The tested player replaces `Builds/Mac/KingLeo.app`. Existing player saves are not reset; already-seen scenes can be viewed through the existing replay controls. A running older player must be restarted to load the new build.
