# JellyGarden game direction

Working direction recorded on 8 September 2026 from the user's request.

## Ambition

Create a distinctive match-3 game with exceptional feel, clarity, personality, and presentation. The user's ambition is to exceed the quality of Royal Match and other leading match-3 games. This is a design target to test through prototypes and player feedback, not a claim about the current game.

The user and Codex will collaborate across game design, game development, and project management. UX/UI design is central to the process. Review mechanics in playable form and discuss visual decisions through concrete examples.

## Current focus

| Area | What we will improve | Evidence of progress |
| --- | --- | --- |
| Game logic and feel | Swaps, matches, cascades, boosters, combinations, and clear outcomes | Valid moves resolve reliably; input and feedback feel responsive |
| Level design | Goals, obstacles, teaching, difficulty, and pacing | Players understand each goal and can explain meaningful choices |
| Story and characters | A memorable world, motivations, character personalities, and progression | The player understands who they help, why it matters, and what changes |
| UX/UI and visual direction | Board readability, hierarchy, silhouettes, color, HUD, menus, and transitions | Pieces, goals, moves, and available actions are understandable at a glance |
| Animation | Anticipation, movement, impacts, reactions, and celebrations | Motion communicates cause and effect with consistent timing |
| VFX and sound feedback | Match effects, booster effects, combo feedback, and rewards | Effects make actions satisfying while keeping the board and goals readable |

The user has selected a royal setting with a castle, king, and queen, and has requested a game trailer plus animated storytelling inside the game. [NAMING-RESEARCH.md](NAMING-RESEARCH.md) recommends **Castle Charm**, with **Castle Charm: Match 3** as the store title, following public store research and a proposed player journey. This supersedes the initial Castlewish recommendation. [NAME-STORY-AND-CINEMATICS.md](NAME-STORY-AND-CINEMATICS.md) develops the initial story/cinematic treatment. The exact name, cast, plot, and final art direction remain proposals for creative review. The existing game's name and assets are the starting material, not a requirement to keep every current design choice.

## First working milestone: one polished playable level

Use Level 1 as the initial place to establish the quality bar before applying changes across the game.

1. **Verify the interaction baseline.** Confirm real mouse swaps, match resolution, falling/refill, move consumption, win/fail, and restart behavior. The previous readiness check saw Level 4 running with no Console errors, but did not confirm a successful automated candy swap.
2. **Define the player's experience.** Make the level goal, first action, character motivation, and success moment clear. Review the proposed visual direction with the user before a broad art replacement.
3. **Implement the core feel.** Improve the most noticeable gameplay/feedback issue first, then add one representative booster interaction when the baseline is reliable.
4. **Polish the presentation.** Align piece shapes, HUD hierarchy, motion timing, VFX, and sound around the chosen direction.
5. **Play, review, and revise.** Compare the result against the criteria above. Record specific problems and the next improvement. Commit and push the reviewed changes.

This milestone does not require a complete game redesign or a mobile export. The current build can be evaluated in Unity Play Mode.

## Working rhythm

Keep one active milestone. For each change: state the intended player benefit, implement a focused prototype, test it in Unity, review the visible result together, then commit and push completed work. Record uncertain behavior as unverified rather than passed. Keep experiments separate from established work when useful.

The user has prioritized selecting a better name and establishing the royal story direction first. Follow that creative pass with character/visual exploration and the first-level interaction baseline. The trailer and story-animation plans should use the same established game assets and show gameplay that is actually implemented.

## Delivery boundary

Development takes place only in this independent copy. The boss's original project and VS Code setup remain untouched. Back up completed work to the private `jchakmaavian/jellygarden-codex` repository.

Android/iOS exporting, platform SDKs, signing, and store delivery are future work for when the boss requests them. They are not readiness requirements for the current game design work.
