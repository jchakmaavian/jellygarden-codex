# Royal Throne — production plan

Decision: build on this independent copy. The existing kit gives us a useful board, level format, objectives, map, and effects. Replace weak parts in measured steps. A full rewrite would discard those advantages before we have a tested replacement.

Status: first playable milestone in progress, 8 September 2026. This is the working backlog created for the user's current request, not an execution of the inherited audit's old phases. Android/iOS exports remain deferred.

## The product we are testing

**A satisfying match-3 game where helping a king and queen produces visible, playful changes in their castle.** Royal Throne is the approved name. The exact cast, visual style, and plot are still creative proposals.

Our proposed distinction is an active king-and-queen partnership and small interactive story payoffs: solve a board, repair something meaningful, see the characters react, and make a simple decorative choice. Test that promise in one castle location before writing a large story or producing cinematics.

Royal Match already connects its cast, castle, puzzle pieces, and power-ups into a coherent world. Its official guide also describes tap-or-swap activation and several distinct power-ups. Our benchmark therefore includes readable rules and consistent world design, alongside attractive art. These sources are a reference review, not a hands-on timing benchmark: [Dream Games world and character design](https://www.dreamgames.com/games/royal-match), [official power-up rules](https://dreamgames.helpshift.com/hc/en/3-royal-match/faq/6-creating-and-using-the-power-ups/).

“Better than Royal Match” remains our ambition. We will test individual qualities with players rather than award ourselves an overall superiority score.

## One active milestone: a complete first-level experience

Build a short, coherent slice: a brief castle situation → understandable goal → first successful swap → obstacle progress → one special-piece demonstration → clear win/fail and retry → a visible castle payoff.

The existing Level 1 is our technical test bed. Its six moves and four jelly tiles are a baseline to evaluate, not a difficulty target we must preserve for the redesigned tutorial.

| Gate | Deliverable | Acceptance evidence |
| --- | --- | --- |
| 1. Reliable play | Verified input, swaps, cascades, goals, win/fail/retry | Real mouse interaction passes; a valid swap spends one move, an invalid swap spends none; settled board accepts the next move; both outcomes and replay work |
| 2. Understandable first session | Simplified goal hierarchy, guided first match, clear jelly/blocker explanation | In a small formative test, players can point to the goal and remaining moves and perform the first match without verbal coaching |
| 3. Distinct visual and story sample | One board frame, six-piece readability sheet, one king/queen reaction pair, one castle before/after | User reviews the sample at gameplay size; pieces remain distinguishable by silhouette; effects do not hide goals; the reward changes the world |
| 4. Polished slice | Integrated motion, VFX, sound, readable results, saved progress | Replayable in Unity; no new Console errors; regression checklist passes; design review completed; commit is pushed |

Gate 1 must pass before we call the slice playable. Visual exploration can run alongside diagnosis; broad scene or asset replacement waits until we have a reviewed sample. The first session should not require spending gems or choosing unfamiliar paid boosts.

## Player journey hypotheses

These are design hypotheses and proposed acceptance targets, not research findings about our players.

| Moment | Player's question | Intended response | What we observe |
| --- | --- | --- | --- |
| Arrival | Why should I care? | Show the royal pair and one small, understandable castle problem | Can the player explain who needs help? |
| Goal | What am I doing? | Emphasize one goal with the same symbol used on the board | Can they point to the relevant obstacle? |
| First action | Did that work? | A clear guided swap and immediate selection/acceptance feedback | Successful first action; mistaken taps; hesitation |
| Progress | Am I getting closer? | Obstacle response, readable counter change, restrained effects | Does the player notice progress without reading an explanation? |
| Mastery | What clever move can I make? | Teach one special piece through play | Can they predict its direction or affected area? |
| Failure | Why did I lose? | Show unfinished goals and a direct retry | Can they describe a different next attempt? |
| Reward | Why play another level? | A short character reaction and a visible castle change | Do they recall the payoff and choose to continue? |

Initial research session: invite five match-3 players with varied experience when the slice is ready. Observe silently, then ask about clarity, control, satisfaction, and character recall. Record completions and problems; a five-person qualitative session does not establish retention or market success. A proposed first-action target is at least four of five succeeding without help; revise the design and repeat when a pattern of confusion appears.

## Ordered production backlog

P0 blocks reliable testing or can freeze play. P1 affects the first-level experience. P2 follows the slice. These priorities supersede the historical audit's ordering for our current scope.

| ID | Priority / state | Work and player benefit | Done when |
| --- | --- | --- | --- |
| RT-001 | P0 — open | Verify actual mouse swaps; automated drags currently reach Unity at the end position | Repeated horizontal/vertical swaps, rapid gestures, invalid swaps, and UI cancellation are checked with real input; trace explains failures |
| RT-002 | P0 — source risk | Bound random selection in `GetRandomItems`; requested count can exceed eligible normal pieces | Empty, all-special, partially eligible, and oversized requests terminate without duplicates or a forced win; integration test covers booster allocation/end celebration |
| RT-003 | P1 — source risk | Make every exhausted-move outcome explicit in `CheckWinLose` | Targets incomplete, final-move completion, and completed targets below `star1` all produce an intentional result; proposed rule agreed before changing success criteria |
| RT-004 | P1 — started | Teach adjacent matching and obstacle goals; remove premature complexity from the first session | Revised instruction visible; tutorial completion tied to learning, not an arbitrary tap; goal and first action understood in player test |
| RT-005 | P1 — next | Fit board, HUD, and goals consistently across portrait shapes | Check 720×1280, 1080×2400, and 1536×2048 Editor previews; no clipped counters/buttons; board readable at gameplay size |
| RT-006 | P1 — next | Improve selection, accepted/invalid swap feedback and cascade rhythm | Compare short before/after play captures; readable actions and no input lock left after resolution; existing juice settings reviewed before adding effects |
| RT-007 | P1 — design sample | Original royal board, piece family, king and queen | One coherent style sheet and one in-game sample reviewed by the user; silhouettes, value hierarchy, and expression range work at small size |
| RT-008 | P1 — prototype | First castle payoff and character relationship | One short scene tied to an actual completed level; skip/replay works; decorative choice visibly persists |
| RT-009 | P1 — after core flow | Align audio, VFX and goal feedback | Representative normal match, special creation, activation and win read distinctly; no masking of objectives; compare with trace disabled |
| RT-010 | P2 — later | Create a small chapter of levels with one mechanic introduced at a time | Playtest results inform move budgets and difficulty; no copied difficulty assumptions from the kit |
| RT-011 | P2 — later | Trailer and in-game animated storytelling | Reviewed storyboard → rough animatic → one finished shot → full edit; reuse approved game assets and show implemented gameplay |
| RT-012 | P1 — observed | Make failure/retry wording match the action; Again currently returns to map | A clearly labelled retry restarts the same level, or the map action is accurately named; unfinished goals remain understandable |

RT-002 and RT-003 are verified code-path risks, not claimed live reproductions in the reviewed session. Diagnose and test before declaring them fixed. Mobile SDK/export findings from the audit are deferred.

## How we work together

**You:** creative/product direction, UX/UI decisions, visual review, and feedback from player sessions. You do not need to operate build pipelines.

**Codex:** inspect and implement the game, maintain the backlog, prepare concrete design alternatives, run checks, document remaining uncertainty, and commit/push completed work. We share game-design decisions; unfamiliar plot or art proposals are presented in a small reviewable sample before large production work.

Use one active ticket at a time: player problem → small acceptance brief → focused implementation → Unity playtest → visual review → regression checks → Git backup. Larger experiments use a branch in this copy. Avoid simultaneous edits to the same Unity scene; prefer reusable prefabs and data for new systems. Preserve `.meta` files and editable asset sources.

A ticket is done only when its acceptance evidence exists, Console changes are understood, accidental file changes are excluded, and its commit has reached the private backup. An imported project is not a gameplay pass. A developer-assisted swap is not a mouse-input pass. A locally saved change is not an online backup.

## Animation and asset production

Approve a compact visual sample before a full asset set. For each character: role/personality → silhouette and colour exploration → expressions and views needed by the shot → rig/prototype → in-game reaction. Existing king concept art is reference material to assess, not an automatically approved final cast.

For cinematics: write the playable story beat first, then storyboard and rough animatic. Keep the first payoff short and skippable, with replay available. Test readable emotions without dialogue before adding expensive voice or video production. Timeline is already in the project; evaluate a small scene with reusable character assets before committing to a full video pipeline.

Do not ship reference-game screenshots as Royal Throne artwork. Keep an asset register with source, author/tool, rights information, editable source, export dimensions, and implementation status. The audit's inventories are a starting point for that register.

## Review and backup

The active baseline and test limitations are in [PLAYTEST-AND-AUDIT-2026-09-08.md](PLAYTEST-AND-AUDIT-2026-09-08.md). Developer controls are described in [PLAYTEST-TOOLS.md](PLAYTEST-TOOLS.md). Save and verify each completed ticket, then push to the existing private repository. Follow [BACKUP-AND-RESTORE.md](BACKUP-AND-RESTORE.md) for recovery. No recurring automation or separate external project-management service is required to start.
