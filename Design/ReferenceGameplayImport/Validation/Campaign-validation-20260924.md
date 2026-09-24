# Campaign validation — 24 September 2026

All 50 levels passed the existing Unity Editor campaign checks in five batches: **3,356 assertions**, zero failed final batches. The final fresh tracking reports zero console errors.

| Levels | Passed assertions | Result | Finished |
|---|---:|---|---|
| 01-10 | 1128 | PASS | 07:17:55 UTC |
| 11-20 | 805 | PASS | 07:19:26 UTC |
| 21-30 | 504 | PASS | 07:20:52 UTC |
| 31-40 | 413 | PASS | 07:36:03 UTC |
| 41-50 | 506 | PASS | 07:37:16 UTC |

Levels 01–30 passed before the completion fix; 31–50 passed afterward. These were successive isolated QA sessions, not one uninterrupted final run.

Each level opened with its imported layout, holes, moves, objectives and authored pieces; a real swap/power action resolved with valid ownership. Relevant blockers were exercised. Representative fail/retry, win/reward and progression checks ran per batch, including both bonus levels. All five batches verified the normal save snapshot was unchanged and restored the player profile.

Source validation separately passed all **50 levels / 3,933 cells**, original payload hashes, moves/objectives, resource identity and the active runtime loader. Imported data is exact to the available extracted projection; blocker rules, refill probabilities, booster targeting/combinations and rewards still contain documented Royal Throne reconstruction. These checks do not prove APK runtime equivalence or reference-identical visuals/SFX.

Two genuine issues found during validation were repaired: native piece pooling now defers reparenting outside parent deactivation callbacks; and the campaign cascade shortcut now retains the existing engine's completion-only refill stabilization. The latter stopped Level 40's self-extending post-win chain. Its unchanged 45-second check reached Win in **31.911 seconds**, with no active booster operations or native VFX left. Active gameplay refill, source palette, rewards and native VFX timing were not changed.

Evidence: `Campaign-20260924-073716/` contains the five fresh batch reports, data checks and final Level 40 diagnostic. Earlier failures remain in `20260924-first-campaign-run/`. Visual capture and booster/combo review are separate validation work; this report does not substitute for them.
