# King Vanguard reference behavior evidence

This inspection reads the exact `Design/Backup/apk/royal_thron_rfv387.apk`, its existing `Design/Inspection-rfv387` extraction, and the newly extracted Spine data beside this folder. No V1 artwork or V1 rig is a source for this evidence. No original project, APK, or extracted source file was changed.

The original portrait animation curves, animation roles, and two metadata constants are recoverable. The compiled IL2CPP method bodies and exact runtime conditions are not recovered by this inspection. The new Royal Throne adapter must be described as reconstructed behavior using original animation data, not imported original C# game logic.

## Verified sources

- `king-metadata-evidence.json`: fresh global-metadata.dat read directly from the requested APK; type/method/field names, literal defaults, animation names and durations.
- `king-serialized-bindings.json`: original `level2` KingUi object 892 (GameObject 62, King), script 5287, the exact payload bytes, external file table, and decoded AnimationReferenceAsset payloads.
- `level2-892-KingUi.unitybin`: original 276-byte KingUi payload, SHA-256 `32d8b44b851aac268e288341e0922f757bd4a2c02a792150f91c485d55e62666`.
- `../King.skel.spine38.json`: direct rfv387 portrait skeleton decode. There are 32 animation records: 31 non-empty and the empty `_3` record.
- `../AreaCompleted.skel.spine38.json`: separately recovered area-completion/full-body activity skeleton. Its animations are distinct from the portrait's roles.

Both inspection scripts write only to this Logic folder. Run them with the workspace's portable Python. `inspect_king_bindings.py` parses and asserts complete consumption of all 13 referenced AnimationReferenceAsset payloads and verifies external file ID 3 resolves to `sharedassets2.assets`.

## Exact portrait role bindings

These are serialized KingUi fields, not guessed semantic translations of animation names. All references below are to `sharedassets2.assets`.

| KingUi field | Object ID | Actual animationName | Duration (seconds) |
|---|---:|---|---:|
| idleConnects[0] | 85 | idle_connect_1 | 4.583333 |
| idleConnects[1] | 86 | idle_connect_2 | 4.583333 |
| idleLoops[0] | 87 | idle_loop_2 | 5.216667 |
| idleLoops[1] | 89 | idle_loop_2 | 5.216667 |
| idleLoops[2] | 90 | idle_loop_3 | 4.216667 |
| tableGameFirstIdleLoop | 88 | idle_loop_1 | 8.950000 |
| tableGameAlternateIdleLoop | 90 | idle_loop_3 | 4.216667 |
| sad | 94 | k-1_uzulme | 2.816667 |
| happy | 95 | k-1sevinme_6 | 3.633333 |
| worryStart | 91 | k-1_kaygi | 5.983333 |
| worryLoop | 92 | k-1_kaygi_loop | 11.650000 |
| extraMoveHappy | 93 | k-1_sevinme_NEW | 2.816667 |
| excited | 83 | Heyecan_1 | 5.983333 |
| relief | 84 | Relax_1 | 1.983333 |

Object 87 has asset name `idle_loop_1` but its actual `animationName` is `idle_loop_2`. Object 88 has asset name `idle_loop_1_real` and actual `animationName` `idle_loop_1`. The duplicate `_2` bindings are source data; they do not by themselves prove a 2:1 runtime probability. `Happy_1` and `Happy_2` exist in the skeleton, but neither is the serialized `happy` role on the recovered KingUi.

## Verified controller evidence and its limits

`Royal.Scenes.Game.Ui.Top.King.KingUi` and `Royal.Scenes.Game.Levels.Units.KingAnimation.KingAnimationStateManager` both declare the integer constant `StartWorryMoveCount = 3`. The state manager also declares `LBComboExciteRayThreshold = 8`. These literal values are decoded from IL2CPP metadata; the comparison operator, additional guards, and exact uses of these constants require method-body recovery or runtime verification.

KingUi methods include `StartIdleAnimationCycle(shouldSetFirstAnimation)`, `MoveChanged(moves, moveChangeType)`, `ShouldBeWorried()`, `GetNextIdleLoop(shuffledIndex)`, `AnimationsCycle(shouldSetFirstAnimation)`, `BeWorry()`, `BeHappyAgain(shouldDecideLoopFromMove = false)`, `StopWorry()`, `BeHappy()`, `EnableHappyAnimDelayed()`, `BeExcited()`, and `BeSad()`. Fields include `movesLeft`, `stopAnimationsCycle`, `randomManager`, `isBonusLevel`, `shouldKingBeHappy`, and `animationCycle`.

The state manager names show a richer decision system than simply cheering on every booster. It declares `OnGoalUpdated`, `OnMoveChanged`, `OnMoveCompleted`, `SimulateComboWin`, `SetPropellerWinningCombo`, `SetSinglePropeller`, `CanLightballsOnBoardExciteKing`, `IsWinPossible`, `TryBeWorry`, `TryStopWorry`, `TryKingRelief`, `ShouldKingBeWorried`, and `IsKingHappyBlocked`. Its fields include goal/grid/move managers, `Last3MoveLayerCaps`, `lastExcitedTime`, `worryStartMove`, `reliefMove`, and an animation-improvement enable flag. Names establish these capabilities in the compiled program; they do not reveal the actual implementation, thresholds in arrays, exact timing, or whether a feature flag enables a path for this game session.

Other metadata contains King nightmare/drill/dragon/dynamite/kitchen/parachute/train/water-tank room and bundle classes. A named bundle or method is not evidence that its full animation content is packaged, available offline, or integrated into Royal Throne. This report does not claim to have recreated those minigames.

## Royal Throne adapter mapping

The following is the intended reconstructed runtime behavior using the verified roles. Event hooks, priority, cooldown, queue handling and boundary behavior are adapter choices unless supported by further evidence; do not label them as exact original logic.

| Royal Throne event/state | Animation role | Intended adapter result |
|---|---|---|
| Gameplay/home portrait becomes visible | idleLoops and optional idleConnects | Play original idle data at native speed, choose another idle naturally without per-frame resets. |
| Move-limited active level crosses to three or fewer moves | worryStart, then worryLoop | Enter worry once; continue looping without restarting each frame. The `<= 3` comparator is the adapter's interpretation of the verified constant. |
| Additional moves remove the low-move condition | extraMoveHappy or relief, then idle | Leave worry with a source-authored recovery reaction; return to idle. Exact original selection/call sequence is unknown. |
| Successful combo/goal progress | happy or excited | One reaction on the base track, then return to the current idle/worry base state. Exact source combo-win simulation is not copied. |
| Win result | happy | Play the verified happy role. Area celebration uses its independent area-completion rig. |
| Failed result | sad | Play the verified sad role, hold or return according to the result-screen adapter policy. |
| Screen disabled/reopened or level restarted | reset base state | Clear obsolete queued reactions and reconstruct state from the current game. |

Timed levels must not interpret seconds remaining as the move count. Hidden UI should suspend unnecessary updates, and character aspect ratio should stay uniform. These are Royal Throne integration requirements, not recovered original behavior claims.

## Concrete validation scenarios

1. **Source integrity and independence:** V2 portrait and area assets load directly from their rfv387 imports. Assert no V1 SkeletonDataAsset, V1 texture, V1 prefab, `idle`, or `happy_thumbs_up` animation dependency. Preserve original source payload hashes.
2. **Animation lookup:** Resolve every role in the table against the imported skeleton. Assert `happy` resolves to `k-1sevinme_6`, and object 87 resolves to `idle_loop_2`. Empty `_3` must not be selected as a meaningful action.
3. **Idle playback:** Keep a portrait visible for at least 30 seconds. Confirm native animation speed, visible variation, no frame-by-frame time reset, no stretched portrait, and no accumulating queue.
4. **Worry boundary:** With goals remaining, step moves 5 -> 4 -> 3 -> 2 -> 1. Verify only one worry-start entry, then the loop; no restart on each refresh. Restart at a low count and confirm deterministic state initialization.
5. **Worry recovery:** Enter worry at 3, add moves to 8, and confirm the selected original recovery/happy track followed by idle. Add moves while worry-start or an unrelated reaction is in progress to check priority and queue cancellation.
6. **Timed level:** Use 3 seconds remaining with a time limit. Confirm the move-count adapter does not automatically enter worry from the numeric limit alone.
7. **Reaction overlap:** Trigger a happy/excited event twice rapidly and then lower moves. Verify bounded track/queue use and the correct base state after the reaction ends. Observe pending event policy rather than claiming source behavior.
8. **Win/failure:** Win from an ordinary level and from worry; verify joy. Fail from idle and from worry; verify sadness on the visible result slot. Reopen the result card and restart to ensure no stale reaction resumes.
9. **Visibility and pause:** Hide/reveal home, gameplay and shared dialog slots. Confirm suspended hidden animation, correct first visible pose, and clean reset when a scene/screen closes. Pause/resume must preserve intended current state without runaway queues.
10. **Area activity:** Play the separate area completion rig using its original native animations. Check the full-body framing independently from portrait framing and verify the redrawn texture on every animated mesh.

These are validation scenarios, not a claim that these runtime checks have already passed. The implementation owner's Unity checks and visual inspection must provide the final execution evidence.
