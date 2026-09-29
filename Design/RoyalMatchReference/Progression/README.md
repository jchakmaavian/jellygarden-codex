# Royal Throne progression validation loop

Started 2026-09-29. The goal is to audit, compare, test, fix and implement Royal Throne's gameplay and progression chain
against the reference game:

**Level → Win → Coins → Stars → Area Task → Story → Reward → Area Completion → Next Area**

Also in scope: the win, fail, retry and continue flows; coins and stars (earning, collecting, spending and persistence);
Area construction and tasks; rewards and chest rules; save and restart persistence; duplicate-reward protection; and the
connected UI, animation and VFX rules.

The number of levels per Area cycle is established by observation, not assumed. Gameplay and progression correctness and
persistence come before visual polish.

## Reference source

Royal Match by Dream Games is the reference. It is a third-party game, so behaviour is observed but its art, audio,
animation and level data are not copied into Royal Throne.

| File or build | Signer | Status |
| --- | --- | --- |
| `Design/Backup/apk/Royal_Throne-rf.apk` (`com.dreamgames.royalmatch` 32984) | `CN=usa, O=O, L=msk` (unknown, not Play or Dream Games) | **Modified, not run.** Injected classes: `com.android.unityengine.UnityPIayerNativeActivity` (capital I for l), `hm.mod.update.up*`, `com.hm.speedremoveads`. It is a repackaged mod build, and its logic may be altered. Read-only. |
| `Design/Backup/apk/royal_thron_rfv387.apk` (38174) | `CN=A1, L=Lazyland, C=RU` | **Tampered, never run** (see `../PROGRESS.md`). |
| Official Play build 38174 (`scratchpad/refapk/official_*.apk`, Google Play app signing) | Google Play | **Used.** Installed on the dedicated reference emulator `RoyalThrone_Reference_4K` (emulator-5556). |

The user's phone holds a real Royal Match account. It is not used without explicit approval, because any spend there
changes that account.

## Loop

Each iteration is one focused unit:
1. Pick the highest-priority open item in `GAPS.md`, or the next unobserved link of the chain.
2. **Observe** it in the reference (emulator, recorded) and write down what the reference does, with evidence, in
   `REFERENCE.md`.
3. **Compare** with Royal Throne (code reading plus a QA-profile run) in `OURS.md`.
4. **Fix** it:
   - patch only the exact lines needed;
   - preserve every other session's uncommitted work;
   - commit path-limited;
   - never push.
5. **Verify:**
   - compile;
   - the focused test;
   - the relevant check suites;
   - that the real save is unchanged.
6. **Record** the iteration in `LOOP.md` and update `GAPS.md`.

The loop is capped at 999 iterations, and it stops early only when every item is closed or blocked with a reason.

## Safety rules (from the user, 2026-09-29)

- The working tree holds hundreds of other sessions' changes: King Venguard, Spine, coins, the card kit and others.
  Inspect `git status` and the diff before touching any shared file. Patch exact lines only. Never use `reset --hard`,
  `clean`, destructive checkout, mass revert, blind stash or pop, or broad replacement. Do not stage unrelated work.
  Do not push.
- The protected baseline is `8ec1502a`, `bef914b2` and `dd0cef99`. Do not modify, revert, squash or recommit them.
- Leave the startup system alone unless a progression test finds a genuine regression caused by it.
- Tests run in the QA save profile; the player's real save is snapshotted before and compared after.
- Before controlling the shared Unity editor (DX11), obtain an explicit handoff.

## Files

- `REFERENCE.md`: what the reference does, link by link, with evidence paths.
- `OURS.md`: what Royal Throne does, from code and tests.
- `GAPS.md`: the differences, with priority, status, iteration and commit.
- `LOOP.md`: the iteration log.
- `reference/`: recordings and frames.
- `ours/`: captures of our build.
- `tests/`: test logs.
- Earlier evidence reused: `../OBSERVATIONS.md` (levels 1-5, the win flow, the Castle tasks) and `../Areas/REFERENCE.md`
  (the task list, the build, the last task, the chest, the new area, the Areas tab).
