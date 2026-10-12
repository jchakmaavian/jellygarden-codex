# Main-game activity adapter

The animation names below are the original rfv387 KingUi serialized role bindings. The event connections and priority rules are explicit Royal Throne adapter choices. They do not claim reconstruction of the APK's compiled method bodies.

| Main-game action | Committed source | Original clip | Handling |
|---|---|---|---|
| In-level extra-moves booster | `LevelManager.ActivatedBoost`, after adding moves | `k-1_sevinme_NEW` | Emit only for a live move-limited board. |
| Continue after purchasing or obtaining extra moves | `AnimationManager.GoOnFailed`, after increasing the limit and restoring Playing | `k-1_sevinme_NEW` | Wait for the leaving continue banner to stop covering the HUD. |
| Goals completed | `LevelManager.PreWinAnimationsCor`, after its once-per-win guard | `Relax_1` | Play relief during the real pre-win celebration, before remaining-move conversion and the result card. |
| Power-up positive feedback | Existing `RoyalPowerupDirector.Reaction` threshold | `k-1sevinme_6` | Does not restart an active reaction. |
| Three moves or fewer | Active Playing move-limited board | `k-1_kaygi` then `k-1_kaygi_loop` | Entrance once per low-move episode; timed levels excluded. |
| Win card / fail card | Result card activation | `k-1sevinme_6` / `k-1_uzulme` | Restart on each opening; failure holds the last sad pose. |
| Leaderboard full-body illustration | Page activation | Native `KingWinAnim`, then `KingLoopAnim` | Original full-body rig through a transparent 448x512 UI view; no old still fallback. |
| Area completed | Existing completed-area banner | `king_drill_win_clap_start`, two `king_drill_win_clap_loop` passes, `king_drill_win_clap_finish` | Uses the independently imported AreaCompleted rig, then its idle. |

`KingVanguardActivityEvents` emits only after actual game actions. The HUD queues at most one delayed action, scoped to the emitting LevelManager, GameField and `passLevelCounter`. A different attempt, hidden/destroyed board, incompatible state or disabled presenter clears it. Goal completion supersedes pending extra-move presentation. Extra-move and relief actions replace generic cheers so confirmed player actions stay responsive.

The HUD explicitly closes its character slot when hidden through CanvasGroup. Reopening resets the rig and reads the current move context; old animation callbacks cannot restore a previous episode. Character presentation does not change moves, score, currency, goals or rewards.

Validation is split: `source-consistency.json` checks live source/dependency isolation; `KingVanguardRuntimeChecks.Run()` exercises the real imported Spine tracks and priority transitions. Real purchase/continue and goal-completion UI validation must also run in Unity's isolated QA profile.
