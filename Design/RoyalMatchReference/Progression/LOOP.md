# Progression loop log

## Iteration 1 (2026-09-29, 11:00-11:55)
**Setup:**
- The evidence folder, `README.md` (the loop definition and rules) and this log.
- The source check:
  - `Design/Backup/apk/Royal_Throne-rf.apk` is **a modified third-party build**: signed `CN=usa, O=O, L=msk`, with injected
    `UnityPIayerNativeActivity`, `hm.mod.update.*` and `com.hm.speedremoveads`. It was not run.
  - The official Play build 38174 on the reference emulator is used instead.

**Reference, from data:** the `AreaNNAreaConfig` assets decode from the official data pack (read-only).
- Areas 1-12: task star costs (`reference/area_configs_1-12.json`), which match everything seen in play.
- Stars per area are 10, 10, then 20 for Areas 3-11, then 30 for Area 12. **There is no 7-level cycle.**

**Ours:** a full code audit (`OURS.md`).

**Gaps:** `GAPS.md`, G01-G17.

**User decisions (D1-D4):**
- one star per win;
- the reference's area totals;
- placeholder scenes for Areas 2+;
- DX11 handed over.

**Reference, in play:**
- L6 won: the one-star card, 16 coins.
- A kill on the win card keeps the win.
- L7 unlocks the pre-level boosters.
- L7 lost:
  - Continue? costs 900 coins for +5 moves;
  - a kill on the offer restores the level;
  - the fail card doubles as the retry card;
  - lives drop 5 → 4 at 30 minutes per life.

**Fix G01:** a booster-shop purchase now adds to the stock instead of replacing it.
- It was verified in the QA profile through the shop button: 2 → 5 → 8, charged once each.
- Staged tree offline compile: 0 errors.
- The real save was unchanged apart from Unity's session keys.
- Commit `0849dc6b` (1 file, 2+/1-; the other sessions' three hunks in `AnimationManager.cs` stay unstaged).
- Test log: `tests/G01_booster_purchase.txt`.

## Iteration 2 (2026-09-29, 11:55-12:45): the last one; the user stopped the loop
**Fix G02 (D1), G03, G04, G11** in `LevelManager.cs`, `RoyalWinSequence.cs`, `RoyalHomeProgress.cs` and `RoyalHomeScreenChecks.cs`:
- every win is exactly one star, whatever the score;
- a win at 0 moves with the goals met now wins (before, below star1, it hung);
- the win commits once, when the goals are met, before the finale: 15 coins, the life refund, the star and the unlock, then a save flush;
- the win card presents what was committed;
- Home counts one star per won level.

**Tests (QA profile):**
- T1 (level 32, full win) passed 11/11.
- T2 (level 35, stop mid-finale) passed 6/6.
- T3 (a fresh session) kept the star and the Level 36 unlock. Its coin FAIL was the harness reading the in-memory balance, see `tests/G02_G04_T3_after_kill_L35.txt`.
- The T2/T3 reruns on levels 36 and 37 never reached the win: the board did not settle while those levels opened.

**Suites:**
- Home 177/177, after its five expectations moved to one star per won level.
- Areas 84/84.
- Coins 83 PASS, 0 FAIL.
- QA controls: 107 PASS, then "Combine selected" did not settle (see `GAPS.md`, "Found during testing").

**Build:** the staged tree compiles offline (Assembly-CSharp and Assembly-CSharp-Editor, 0 errors).

**Real save:**
- T1's `EndSession` leak was restored.
- The other runs changed no real game key.

