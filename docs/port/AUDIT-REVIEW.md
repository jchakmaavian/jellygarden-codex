# Port audit review (completeness critic)

This review checks the five Phase 1 audits against each other and against the plan, `docs/superpowers/plans/2026-10-12-royal-throne-level-port.md`. Decisions D1–D8 are at their default.

The five audits are:
- `GUID-AUDIT.md`
- `IMPORT-MANIFEST.md` + `PROVENANCE.md` + `import-paths.txt` + `import-closure.json`
- `COUPLINGS.md`
- `merge-specs/*`
- `RETIRE.md` + `UNUSED-ASSETS.md`

Written 2026-10-12. Refs:

| Ref | Commit |
|---|---|
| `origin/main` | `c38b2246` |
| `origin/royal-throne` | `1164c261` |
| kit ancestor | `a82c4df` |
| `port/royal-throne-level` HEAD | `5f71ea88` |

**HEAD `5f71ea88` already contains the port.** Another agent committed it during this review, with the message "feat: swap in royal-throne's level layer". §4 checks it against the docs, using read-only git and temp-dir builds only.

---

## 1. What was run

| Check | Command | Result |
|---|---|---|
| Every tool runs with `--help` | `python3 tools/<tool>.py --help`, run for all 8 `port_*` tools | All 8 pass. Two needed fixes first; see §2, fix F1. |
| GUID audit self-test | `python3 tools/test_port_guid_audit.py` | 4/4 OK |
| Closure self-test | `python3 tools/test_port_closure.py` | 14/14 OK |
| Coupling scanner self-test | `python3 tools/port_coupling_scan.py --self-test` | OK, both before and after the edits in §2 |
| Merge-spec anchors | `python3 tools/port_merge_spec_check.py` | 295 anchors, 0 missing |
| Merge dry run | `python3 tools/port_merge_dryrun.py <scratch>` | PASS: 16 files, 0 parse errors (Roslyn 8.0.424). Re-run after fix F5. |
| Generators are deterministic | `port_import_manifest.py --out <scratch>`, and `port_coupling_scan.py --format md` before editing | `import-paths.txt` and `import-closure.json` regenerate byte-identical. The scanner output equals `COUPLINGS.md` Appendix A exactly. |
| Anchor spot-check: 10 random anchors (8 `rt:`, 2 `main:`), seed 20261012, checked against the extract | script in the scratchpad | 10 of 10 found exactly once. See the table below. |
| Line-cite spot-check: 20 `RT:n` / `file:line` cites in `INDEX.md`, `COUPLINGS.md` and the specs | `sed -n` against the extract | 20 of 20 show the quoted code |
| Plan Appendix B kept hooks | grep in the dry-run output and in HEAD | All 14 hooks are present in both. No dropped hook survives, apart from one comment that says "was `StorybookBoardArt.Enabled`". |
| D4 | method diff | `CheckWinLose` in HEAD is identical to main's |
| Folder restores in `import-paths.txt` would delete main-only files | git tree comparison | None. All 282 entries exist on royal-throne, each with its `.meta`. |
| GUID (c)/(d) resolutions | git tree comparison | Neither (c) duplicate is in `import-paths.txt` or HEAD. Every (d) folder `.meta` keeps main's GUID in HEAD. None of the 31 `folder_metas` is a (d) path. |
| Type-level gap scan | `review/gap.py` in the scratchpad, over 269 imported `.cs`, 16 dry-run kit files and 245 main-kept files | Every royal-throne-only type referenced is either in the import set or listed as a cut in `COUPLINGS.md` Appendix A, with two exceptions that the closure catches but `COUPLINGS.md` missed: `RoyalReviewAudioCapture` and `RoyalGameplayArtImporter`'s `RequireCopy`. See fixes F2 and F3. The other hits are false positives: nested or common names such as `Key`, `Data`, `Status`, `Goal` and `Preview`. |
| HEAD compiles | `TMPDIR=<scratch> python3 tools/port_retire_check.py --typecheck --source HEAD` (temp dir only) | **550 files, 0 errors** |
| Retirement on the working tree | `python3 tools/port_retire_check.py` | 0 failures, 0 retired files present |
| Couplings left on the working tree | `python3 tools/port_coupling_scan.py --worktree` | rc=1. 17 lines left; see §4. |

Anchor spot-check (the extract is `scratchpad/rt` and `scratchpad/main_cs`):

| Spec | Side | File | Line | Anchor (truncated) |
|---|---|---|---|---|
| LevelManager.md | rt | LevelManager.cs | 739 | `IEnumerator StartAnimateIngredient(GameObject item, Vector2 pos)` |
| AI.md | rt | AI.cs | 209 | `if (square.type == SquareTypes.WIREBLOCK \|\| square.item == null \|\| RoyalThrone.Campaign.Ro…` |
| TargetLevel.md | rt | TargetLevel.cs | 54 | `if (other.type == Target.COLLECT && other.color == 5)` |
| BoostIcon.md | rt | BoostIcon.cs | 1 | `using PlayerPrefs = RoyalThrone.Testing.RoyalSave;` |
| AI.md | rt | AI.cs | 125 | `if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item)) return;` |
| AnimationManager.md | rt | RoyalQaService.cs | 114 | `foreach(var animation in MenuManager.Instance.GetComponentsInChildren<AnimationManager>(tr…` |
| LevelManager.md | rt | LevelManager.cs | 956 | `RoyalThrone.Presentation.RoyalBackButton.Quit();` |
| TargetsGroup.md | rt | TargetsGroup.cs | 32 | `var obj=Instantiate<TargetIcon>(prefab,transform);obj.SetTarget(target);…` |
| TargetIcon.md | main | TargetIcon.cs | 47 | `// Match the live copy's art; the lazy target lookup below uses its sprite name.` |
| InitScript.md | main | InitScript.cs | 419 | `if (!JellyGarden.Scripts.Story.StoryFreeGame.AdsAllowed)` |

---

## 2. Fixed in this review

Each fix brings the docs in line with the majority position: the plan, the closure and what HEAD actually did. Generated sections were regenerated from the fixed tools, not edited by hand.

| # | Contradiction or defect | Fix |
|---|---|---|
| F1 | `port_merge_spec_check.py --help` crashed: it treated `--help` as a spec path. `port_merge_dryrun.py --help` **wrote 16 files into a `./--help/` folder at the repo root**, because it treated `--help` as `OUT_DIR`. | Both tools now print their docstring on `-h`/`--help`. The dry run also refuses any other `-option`. The stray `./--help/` folder that the first `--help` run created was deleted. |
| F2 | **`RoyalReviewAudioCapture`.** The closure cuts `Juice/RoyalReviewAudioCapture.cs`, mislabelled "music/ambience". `COUPLINGS.md` §3.2 said to keep it, and Appendix A had no cut for its uses at `RoyalEffectsChecks.cs:55,145`. The result was an undocumented compile break. HEAD follows the closure. | Closure kept. In `tools/port_coupling_scan.py`, the file is in `NOT_IMPORTED`, rule `qaui` matches it, and an override says to drop the `capture` field and its uses in `RoyalEffectsChecks`. Updated `COUPLINGS.md` §3.2, §3.4 and finding 6, and `IMPORT-MANIFEST.md` §7. |
| F3 | **Coupling-scan scope missed three files the closure imports:** `Editor/RoyalGameplayArtImporter.cs`, `Editor/RoyalWebPImporter.cs` and `Editor/RoyalVfxLab.cs`. So `RoyalGameplayArtImporter.cs:44` `RoyalHomeScreenBuilder.RequireCopy()` was missing from the cut list, and `COUPLINGS.md` said that "no other imported file calls it". | The three files are added to the scanner's `IMPORT_FILES`. Appendix A now lists `RoyalGameplayArtImporter.cs:44`, and the token table in §2 counts 2 imported `RequireCopy` sites. |
| F4 | **Dead `RoyalPropellerV2*`.** `COUPLINGS.md` said not to import them and to cut `RoyalPowerupDirector.cs:91`. The closure, plan §5 (`Juice/**`), plan Task 6.1 ("delete … `RoyalPropellerV2*`") and HEAD all import them. `--worktree` would have failed with "NOT-IMPORTED FILE PRESENT" ×3 and `LEFT …:91`. | Aligned to the plan. The V2 files were removed from `NOT_IMPORTED` and from rule `oldboosters`, and the `:91` override was removed. `COUPLINGS.md` §2, §3.2 and §3.4 now say to import them and delete them in Task 6.1 together with the `:91` call. |
| F5 | **`InitScript` QA members.** `merge-specs/InitScript.md` defaulted to adding three QA shims to main's `InitScript`, and `port_merge_dryrun.py` inserted them. `COUPLINGS.md` §4 and the scanner (rule `initscript`, which must be cut) cut the callers. Plan §4 says main's `InitScript` gains nothing. The spec also missed `NextLifeKey` (`RoyalQaChecks.cs:34`) and `TryOpenLevel` (`RoyalEffectsChecks.cs:23`). | Default flipped to **cut the callers**; the shims are now the alternative. The dry run adds only the `RoyalSave` alias, and the shims can be reproduced with `PORT_DRYRUN_INITSCRIPT_SHIMS=1`. Updated `InitScript.md` §3, `INDEX.md` (rows 15, §1, §4.4 and §5) and the `IMPORT-MANIFEST.md` kit-manual rows. |
| F6 | **`RoyalVfxLab`.** `COUPLINGS.md` finding 6 and the `RoyalRocketChecks.cs:29` override said to inline it because it was "not imported". The closure imports it, and HEAD keeps the call. `--worktree` would have flagged `:29`. | `RoyalVfxLab` was removed from rule `qaui` and its override removed. `RoyalVfxLab` has no couplings and writes only to `Library/`. Finding 6 was rewritten. |
| F7 | **`Display.ttf`.** `COUPLINGS.md` required the three `RoyalGameplay/Display` loads to be replaced, for licence reasons, and the scanner flagged them as must-cut. The closure imports `Display.ttf`. `PROVENANCE.md` §4 shows it is byte-identical to the font main already ships, so it adds no licence exposure. HEAD keeps the loads. | Rule `font` is now `import`, which is allowed, and the three per-file overrides were removed. Replacing the loads with `StoryUi.DisplayFont` is now optional de-duplication. `COUPLINGS.md` §2 was updated. |
| F8 | **`Item` campaign arguments.** `merge-specs/INDEX.md` §5 and `Square.md` keep the inert `royalCampaignHitWave` field and the three-argument `square.DestroyBlock(...)` call, matching plan §4's signature. `COUPLINGS.md` and the scanner said to remove them. | Aligned to the plan and the specs. Two scanner overrides now mark them `import`. `COUPLINGS.md` §2 has a note. |
| F9 | **`RoyalOfflineMode.cs`.** `COUPLINGS.md` finding 4 and `INDEX.md` §5 C7 said it "is not imported". `import-paths.txt` imports it (as does HEAD). `IMPORT-MANIFEST.md` §4 justified it with a reference that C7 removes. | All three now say: imported, harmless, and unused once C7 is applied. |
| F10 | **`AnimationManager.IsOpen`.** `IMPORT-MANIFEST.md`'s kit-manual table said imported code needs it. That was a false positive: the hit at `ReferenceBoosterBar.cs:290` is `RoyalShopPage.IsOpen`. Royal-throne's `AnimationManager.IsOpen(string)` (`:409`) is private and has no imported caller. | The row was corrected. It agrees with `COUPLINGS.md` §4 and `AnimationManager.md`. |
| F11 | **`MenuManager.BlocksBoardInput` and the Story popups.** `COUPLINGS.md` finding 11 said to replace the `RoyalShopPage` line with four Story `IsOpen` checks, and called the parenting UNVERIFIED. `MenuManager.md` just drops the line. | **Verified statically.** The four popups are root objects (`new GameObject(HostName)` + `StoryObjects.Persist`), not children of `CanvasGlobal`. They show only outside `GameState.Playing`. Every reader of `BlocksBoardInput` also requires `Playing`: `BoardInputAllowed`, `TimeTick` and `ReferenceBoosterBar.Stable()`. Finding 11 now matches `MenuManager.md`: the checks are optional hardening. |
| F12 | `IMPORT-MANIFEST.md` §7 said there are "10 Resources roots"; §1 says 13. | Changed to "13 roots, 10 used". |
| F13 | `IMPORT-MANIFEST.md` said to "keep main's `MusicBase` unchanged"; `MusicBase.md` takes royal-throne's file minus the gate. | The row now names both. The difference is RT's singleton/`SetParent(null)` fix, and neither version merges the gate. |
| F14 | **`AvatarEvents` names.** The plan, `INDEX.md` §3 and the committed `Juice/AvatarEvents.cs` disagree. The committed file has `WorriedChanged`, `MatchResolved` and `ExtraMovesGranted`, and `Action<int>` for `ToolUsed`/`ToolStruck`. | `INDEX.md` §3 now records this and recommends that Task 4.7 use the committed names. The `Notify*` methods the kit calls are identical, so compilation is unaffected. |
| F15 | **`BoardColor` timing.** `COUPLINGS.md` removes `RoyalGameplayArt.BoardColor` at import. `Item.md` §4 leaves it until Task 4.2. | `COUPLINGS.md` §7 notes that `--worktree` lists those lines as LEFT until Task 4.2. |
| F16 | Scanner counts were stale after the fixes. | `COUPLINGS.md` header and table were regenerated: 165 files, 520 code hits; imported 64 files / 334 hits / 221 cut / 113 kept; not imported 22 files / 186 hits. Appendix A was regenerated by `port_coupling_scan.py --format md`. |

Files edited: `docs/port/COUPLINGS.md`, `docs/port/IMPORT-MANIFEST.md`, `docs/port/merge-specs/INDEX.md`, `docs/port/merge-specs/InitScript.md`, `tools/port_coupling_scan.py`, `tools/port_merge_dryrun.py` and `tools/port_merge_spec_check.py`.

Not edited:
- `GUID-AUDIT.md`: its header says to re-run the tool, not edit it. Its content is consistent; its InitScript "CHECK (Task 1.3)" note is now answered by fix F5.
- `RETIRE.md`, `UNUSED-ASSETS.md`, `PROVENANCE.md`, `import-paths.txt`, `import-closure.json`: no contradictions found. The HEAD typecheck confirms the retirement map: no retired class is needed by a kept class.

---

## 3. Still open in the docs and plan (decisions)

| # | Item | Where |
|---|---|---|
| O1 | **D4 owner question.** Main's `CheckWinLose` keeps a hang when goals are met on the last move with `Score < star1`: neither win nor lose fires. Royal Throne fixed it. | `merge-specs/LevelManager.md` K6, `INDEX.md` §5 |
| O2 | **Test-only assets.** `RoyalTNTChecks` and `RoyalColorBallChecks` (which `RoyalEffectsChecks` calls) need `Resources/RoyalTNT` (26.9 MB, not imported) and `Resources/RoyalColorBall/Frames`. HEAD has imported `Frames` (217 files incl. `.meta`) but not `RoyalTNT`. Decide: import them for the tests, or trim those cases. | `IMPORT-MANIFEST.md` §4 |
| O3 | **Sixth gem skin (D3).** `runtime-catalog.json` has no `vase`. Pick one: a catalog entry, `RoyalCampaignArt/gem.png` (UNVERIFIED provenance; add it to the `PROVENANCE.md` UNVERIFIED block if chosen), or the V4 porcelain vase. | `COUPLINGS.md` finding 8, Task 4.2 |
| O4 | **Plan text to update.** <ul><li>§5 Retired and Reduced: add `KingLeoVfxHost`, `StoryVfxFlags`, `StorybookBoardReview` and `StoryBoardBorderTests`; `StoryBoot` needs no change.</li><li>Task 3.5 Step 3: do not import `RoyalOfflineBuildPolicy.cs`.</li><li>Task 3.6: no `RoyalTNTBody`/`RoyalTNTGlow`.</li><li>Task 3.6: ReferenceGameplay Resources live in 13 roots, not one.</li><li>Task 4.0: add the 8 suites `RoyalEffectsChecks` calls, plus a `RoyalQaService` installer in `PortChecks`.</li><li>Task 5.1 Step 3: the `StorybookBoardLayout` loop cannot be kept.</li><li>Appendix A: add `StoryBoardScale.Pitch` (`Item.cs:1271`).</li></ul> | `RETIRE.md` §3, §6, §8.2; `COUPLINGS.md` §1; `IMPORT-MANIFEST.md` §7 |
| O5 | **Licences, not cleared.** <ul><li>Spine: a Spine Editor licence per user.</li><li>AllIn1: Asset Store EULA per seat.</li><li>BorisBlackBloxx: unknown, but main already ships it.</li><li>Propeller V5 audio: ElevenLabs via Magnific, plan terms UNVERIFIED.</li></ul> | `PROVENANCE.md` §4 |
| O6 | **FFmpeg.** `RoyalWebPImporter` needs FFmpeg on every editor machine and on CI. | `IMPORT-MANIFEST.md` §4 |
| O7 | **Runtime checks still UNVERIFIED.** <ul><li>Back-key script order between `ReferenceBoosterBar` and `LevelManager`.</li><li>Treasure Hunt level numbers versus the level 1–5 tool lock.</li><li>The `TNTFrameAnimator` null-guard.</li><li>`StoryLevelHud` gating with the switch on.</li></ul> | `COUPLINGS.md` findings 10 and 12, §3.2; `RETIRE.md` §2.4 |
| O8 | **Optional suites.** `COUPLINGS.md` §3.3 also recommends `TNTSunArt`, `ReferenceFallColorChecks` and `ReferenceRfv387Checks`. They are not in `import-paths.txt`; add them there if wanted. | `COUPLINGS.md` §3.3 |

---

## 4. HEAD `5f71ea88` against the docs (implementation must handle)

### Already consistent with the docs

- HEAD compiles: 0 errors in a temp-dir typecheck.
- Retirement scan: 0 failures.
- Appendix B kept hooks: all present.
- `CheckWinLose` is main's (D4).
- All four (d) folder `.meta` files keep main's GUID.
- Neither (c) duplicate prefab was imported.
- None of these was imported: `Blockers`, `levels01-50`, `Home/Resources`, `RoyalTNT`, `RoyalCampaignArt`, `Characters`.
- `ReferenceGameplay/Preview` is kept.
- These match the dry run exactly: `AI.cs`, `CombineManager.cs`, `MenuManager.cs`, `SoundBase.cs`, `MusicBase.cs`, `TargetsGroup.cs`.
- The other differences from the dry run are deliberate Task 4.x progress: `TargetIcon` using `RoyalVisualCatalog.Goal`, the `RoyalBlockSkin`/`RoyalPieceSkin` runtime add, `NotifyLost`, the `BoostShop` `ShopsAllowed` guard, and `InputManager`'s optional non-pointer fallback.

### Gaps (blockers)

| # | Gap in HEAD | Evidence | Fix |
|---|---|---|---|
| B1 | **AllIn1 demo scripts are missing.** `Demo & Assets/Demo/Scripts/AllIn1VfxAutoDestroy.cs` and `AllIn1VfxFadeLight.cs` (+`.meta`) are in `import-paths.txt` but in neither HEAD nor the working tree. HEAD's `Fire Impact.prefab` and `Ice Impact.prefab` reference script GUID `83d354433035fb44ca1436010e6839c1`. | `git grep 83d3544… HEAD` | Restore both files with their `.meta`. Without them the Toolkit impacts have Missing Script components, and `AutoDestroy` never cleans them up. |
| B2 | **19 `RoyalGameplay/*.webp` sprites and `Editor/RoyalWebPImporter.cs` are not committed.** They are untracked in the working tree. | `git status` | Add them, or drop them from the manifest. Do not `git add -A`: the untracked set also holds out-of-closure files, see B5. |
| B3 | **`LevelManager` C7 not applied.** HEAD keeps `FacebookEnable = RoyalOfflineMode.ServicesEnabled` (`:503`) and `enableInApps = RoyalOfflineMode.ServicesEnabled` (`:512`). That turns off main's Facebook and IAP integration, a behaviour change from main. | `port_coupling_scan.py --worktree` | Apply `LevelManager.md` C7a/b (main's lines), or record an owner decision to go offline. |
| B4 | **`InitScript` gains 5 members that nothing calls.** They are `NextLifeKey`, `LifeRefillDeadline`, `RestoreLifeScheduleForTesting`, `SetRefillSeconds` and `TryOpenLevel`. HEAD also cuts their callers. Plan §4 and the corrected `InitScript.md` default add none. | `git diff origin/main HEAD -- …/InitScript.cs` | Remove them; keep the `RoyalSave` alias. Or record a deviation. Harmless for compiling. |
| B5 | **Over-import against `import-paths.txt`.** HEAD holds about 2,578 royal-throne-only files outside the manifest, counting `.meta`: about 1,059 extra `ReferenceGameplay/Art/.../Sprites` (APK-derived), `RoyalColorBall/Frames` (O2), `Assets/Editor/SpineSettings.asset`, `RoyalGameplay/UIAdditive.shader` and 4 extra `RoyalGameplay/Audio` files. The working tree also has untracked out-of-closure files: `coin.webp`, `king-*.webp`, `home-castle.webp`, `reward-rays.webp`, `settings.webp`, `win-cushion*.webp` and `RoyalColorBall/VFX/`. | tree comparison | Prune them, or regenerate the manifest and `PROVENANCE.md` to match. The `APK-DERIVED` prefix `Assets/ReferenceGameplay/Art/` already covers the sprites for the Task 6.4 gate. |
| B6 | **Master switch committed.** `Resources/RoyalGameplay/EffectsProfile.asset` is in HEAD. Plan Task 3.6 Step 3 holds it back until Task 4.1, so Task 3.8's "kit fallback" checkpoint (switch off) was skipped. | `git show --stat 5f71ea88` | Run the Task 3.8 checks with the asset moved aside, or record the deviation. |
| B7 | **`--worktree` reports 17 lines left.** <ul><li>`Design/` output paths in `PropellerReferenceChecks.cs:19`, `RoyalAllDualComboChecks.cs:19` and `RoyalDualColorBallChecks.cs:19` (Task 4.0).</li><li>The presentation-side king driver (`kingFeedback`, `KingToolUsed`/`KingToolStruck` subscriptions) is still in `RoyalGameplayPresentation.cs:23,129-153,258-283` (Task 4.7: move it to an avatar driver on `StoryAvatar`).</li><li>`RoyalGameplayArt.cs:35` `BoardColor` is now identity; remove it in Task 4.2.</li><li>`LevelManager.cs:99,569` `ContinuesBought` (`LevelManager.md` C1/C9).</li><li>B3's two lines.</li></ul> | `python3 tools/port_coupling_scan.py --worktree` | Finish Tasks 4.0, 4.2 and 4.7, plus C1/C9 and C7. |
| B8 | **The 6 recommended `Reference*Checks` suites are not imported.** They are `ReferenceArcherChecks`, `ReferenceBoosterChecks`, `ReferenceCannonChecks`, `ReferenceHammerChecks`, `ReferenceLayoutChecks` and `ResponsiveInputChecks`, which cover tool, layout and input. They are in `import-paths.txt`. | tree comparison | Import them for Tasks 4.5, 4.6 and 4.8, or drop them from the manifest. Optional. |
| B9 | **The working tree is not read-only.** Another process keeps writing under `Assets/` and committing; it swept this review's early `tools/` fix into `5f71ea88`. | `git log`, `git status` | Coordinate before the next commit, so that review-only doc and tool edits are not mixed into feature commits by accident. |
