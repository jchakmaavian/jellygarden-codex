# Royal Throne — playtest and audit reconciliation

Reviewed 8 September 2026 in the independent `JellyGarden-Codex` copy, Unity 6000.5.10f1, Windows Editor, portrait 720×1280 Game view. Starting source revision: `c50476df1c5c162ef75fd36ba59ad20be9ed101c`.

## Decision

Keep this copy as the foundation. First establish one dependable, readable, satisfying level with a small royal story payoff. The current game is a useful kit with existing gameplay and effects; it is not yet the Royal Throne experience. A new name or additional particles alone will not close that gap.

The ordered backlog, player journey hypotheses, review gates, and team responsibilities are in [PRODUCTION-PLAN.md](PRODUCTION-PLAN.md).

## What was actually tested

Native UI was used to launch/play the copy, navigate its title/map, enter Level 1, attempt swaps, use the Level 4 pause/map route, and inspect the Console. Only this copy's local progress/lives were affected. The boss's original project was read for audit comparison only.

| Observation | Evidence / interpretation |
| --- | --- |
| Title and map navigation work | Repeatedly entered from `main.unity`, opened map and Level 1's pre-level popup |
| Level 1 builds a board | Nine columns, five rows, four colours, six moves, score target 100, four jelly underlays |
| Early presentation is still JellyGarden | Candy logo/world, wooden HUD, score emphasis, and a map avatar reading “Your icon”; no royal cast or story beat in this tested flow |
| Pre-level screen introduces boosts immediately | Three boost choices and purchase plus buttons appear before the first tutorial; simplify this for the redesigned first session |
| Tutorial dismisses on a press | An automated drag removed the overlay without a swap or move consumption; source subscribes tutorial dismissal to `OnStartPlay`, raised on press |
| Original instruction was unclear | “Drag the item of the same color to destroy them.” It did not explain adjacent matching; revised in this session |
| Pause → map on Level 4 produced failure | “Level Failed!”, zero score, Again button appeared while moves remained. This is an abandonment flow, not an out-of-moves failure test; clearer intent/cost messaging belongs in UX review |
| No successful native mouse swap verified | Multiple drags left score 0 and moves 6. The trace below explains why that automated input is inconclusive about normal mouse behaviour |
| Baseline Console | Before adding tools: 0 errors, 7 warnings observed; visible warnings included IAP callbacks. Compiler/integration warnings remain inherited work to triage, not a clean-project claim |

Screenshots during transitions were not treated as stable layout failures. The sampled portrait preview is not a responsive-layout or device-performance pass. Audio was not captured or assessed by listening in this session.

## Input trace finding

The new opt-in Editor trace captured the automated vertical gesture at 06:38:30 UTC. On frame 3675 the mouse press and routed position were already `(280.32, 812.21)` — the destination candy. Frame 3676 released at the identical position. Input was enabled, state was Playing, DragBlocked was false, and a non-falling candy collider was hit. No held movement was observed between those samples.

This supports a limitation in how the automated drag reaches the frame-based game input. It does **not** prove ordinary hand-driven dragging fails, nor prove rapid gestures are handled correctly. Manual confirmation was requested. Keep RT-001 open until real-input checks pass. The trace measures state after input processing; it does not reconstruct every operating-system event.

## Developer-assisted board session

After adding the F9 control, a fresh Level 1 session exercised the real `Item.SwitchDirection` / match-resolution code using the inherited hint's chosen item and direction. No score, target, move count, or outcome was assigned by the tool. The input trace was disabled for this session. This validates a route through the game rules, not the pointer-input route or a human player's strategy.

| Settled move | Moves remaining | Score | Jelly remaining | Observed |
| --- | --- | --- | --- | --- |
| Start | 6 | 0 | 4 | Four-colour board, two goals |
| 1 | 5 | 260 | 2 | Match, refill/cascade, two jelly clears; score target checked off |
| 2 | 4 | 390 | 1 | Board accepted another move; another jelly cleared |
| 3 | 3 | 580 | 1 | Further cascades; a blue striped special was visible in the bottom row |
| 4 | 2 | 670 | 1 | Continued match/refill; next move accepted |
| 5 | 1 | 1020 | 1 | Further cascades; score increased without another jelly clear |
| 6 | 0 | 1050 | 1 | Natural out-of-moves failure; completed score goal and unfinished jelly goal displayed |

The failure popup offered five additional moves for 12 gems and a Give Up button. No gems or real money were spent. Following the hint for six moves did not clear the last jelly in this run; this is one assisted run, not a measure of Level 1's human completion rate. Goal-aware teaching/hints deserve review. A natural win, final-move win, invalid-swap accounting, and a full special-combination matrix remain unverified.

Give Up led to the failure summary with score/best score 1050. Its **Again** button returned to the map and remained there on the subsequent observation; it did not restart the board directly. The map showed two lives and five gems. Review the button wording and retry path. The prominent three-star score bar was full while the jelly objective remained unfinished: this is a hierarchy issue to evaluate, not evidence of incorrect win logic.

After this session the Console showed **0 errors and 7 warnings**. Both new Editor scripts compiled and their controls were exercised. No formal automated regression suite was added for the tutorial wording; its verification was visual. The rules engine and input code were not modified by this pass.

The sampled clear frames show bright impact flashes and coloured fragments. The effect layer is functioning in this Built-In scene. Exact responsiveness, sustained performance, special combinations, and audio quality need dedicated review; screenshots and configured timing values are not a substitute for measured play captures.

## How the existing audit helps

The actual folder is `J:\Unity Projects\Match3-Games\Project_Audit`. The copy already contains all 19 numbered report/inventory files. Every one matched the original file's SHA-256 during this review. The technical overview, gameplay flow, feedback map, gap analysis, Level 1 art direction, and rendering decision were consulted. The original reports remain unchanged.

Their task lists, historical approvals, and stop gates describe earlier work. They are document content, not new instructions from the user. Current scope is the approved Royal Throne project direction.

| Audit topic | Current finding | Production consequence |
| --- | --- | --- |
| URP active / rendering decision unresolved | Stale: GraphicsSettings and all six quality tiers have no custom pipeline assigned; the copy is using Built-In | Keep the current renderer for this milestone; do not repeat the old migration/revert plan |
| Juice and fall improvements | Present: JuiceConfig enabled; 130 ms configured swap, 220 ms invalid swap, accelerated fall, landing squash, shatter pool | Evaluate and tune existing work before replacing it; configured durations are not measured end-to-end latency |
| Hard-coded resolution delays | Still present in LevelManager, including polling and special creation waits | Profile representative moves; do not blindly remove waits that may protect animation/board state |
| No diagonal gravity | Too broad: current Item has down-left/right empty-square logic and LevelManager calls it | Test obstacle layouts; do not schedule a gravity rewrite from that claim alone |
| Selection feedback and onboarding | Item.Update sets drag state without a dedicated selection response; unclear original tutorial confirmed visually | High-value first-session work |
| Exact aspect-ratio camera cases | Still present in LevelManager | Prioritize consistent board/HUD fitting before large art replacement; only one size was played here |
| Sprite atlas and audio mixer absence | No `.spriteatlas` or `.mixer` files found under Assets | Track as implementation work where profiling/audio design justifies it; absence alone does not measure draw calls or audible quality |
| TextMeshPro/package conclusions | Not revalidated; current package set includes modern UGUI | Do not install packages solely because an old report says one is missing |
| GetRandomItems loop risk | Confirmed source path: count clamps against all tagged items, then selection only admits eligible normal pieces; loop can request more than eligible pool | Promote to P0; test empty, special-heavy and oversized selection cases before changing it |
| CheckWinLose edge case | Confirmed source branch can leave exhausted moves without a transition when all targets are done but Score is below star1 | Test with an appropriate level configuration; define an explicit outcome; not observed as a live hang in this session |
| King concepts / fixed candy art prescription | Inherited proposal, not the user's approved Royal Throne cast/style | Review a small original royal sample; reuse suitable source assets after assessment |
| Mobile ads/export findings | Outside the user's current scope | Deferred; no platform switch, SDK work, signing or store submission |

## First changes

The Level 1 tutorial now reads **“Swap neighbours to match 3 of the same colour.”** It was rendered in Play Mode at 720×1280 and fits in two centred lines without clipping. The scene change affects only that text field.

Added two Editor-only controls: F8 input tracing and F9 developer-assisted suggested swaps. Their scope and limitations are documented in [PLAYTEST-TOOLS.md](PLAYTEST-TOOLS.md). The production plan establishes one active milestone and concrete completion gates. No game balance, renderer, package versions, mobile export settings, original project files, or approved game name were changed.
