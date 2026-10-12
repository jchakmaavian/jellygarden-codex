# Royal Throne — Editor playtest controls

These tools live under `Assets/JellyGarden/Scripts/Editor`, so they are Editor-only. They operate only in this independent copy. Normal play changes this copy's local progress/lives; Git backs up project source, not PlayerPrefs.

## Mouse input trace: F8

Enter Play Mode, focus the Game view, and press **F8**, or use **Tools → Royal Throne → Playtest → Toggle input trace**. Perform a swap, then press F8 again. Leaving Play Mode or reloading scripts also stops recording. Each session replaces `Temp/RoyalThroneInputTrace.jsonl`; the file is ignored by Git. Recording stops at 2,000 samples.

Samples include mouse press/release, routed input position, focused Editor window, hit collider, level state, drag gate, score, and moves. Samples are taken after an Input System update, before normal component Update processing. Interpret frame order accordingly. The tool does not inject input or alter game state. It performs diagnostic queries and file writes, so disable it for timing/performance comparisons.

## Developer-assisted suggested swap: F9

After the tutorial overlay is dismissed and the board has settled, press **F9**, or use **Tools → Royal Throne → Playtest → Play suggested swap (bypasses pointer)**. This passes the current hint's item/direction into the existing `Item.SwitchDirection` path. It does not directly assign score, moves, targets, or win/fail state. Those results come from the game's normal swap and resolution code.

This is a developer control, not a solver or proof that mouse input works. It depends on the inherited hint system and its limitations. Wait for resolution and a fresh hint before the next invocation. It refuses to act outside an active, settled board. Console messages explicitly identify assisted swaps.

## Baseline checklist

Record level, starting state and input method. Keep each result as passed, failed, or unverified.

- Launch from `main.unity`; enter the map and Level 1.
- Understand the goal and first move; confirm tutorial dismissal is appropriate.
- **Real mouse:** horizontal and vertical valid swap; invalid swap; rapid drag; release outside board; taps on UI; pause/continue.
- **Board rules:** exactly one move per accepted swap; none for invalid swap; match, fall/refill, cascade and next-move readiness.
- **Targets:** jelly clearing and HUD updates agree; normal/special actions count correctly.
- **Outcomes:** win before exhaustion, win on final move, exhausted moves with unfinished goals, retry and map navigation.
- **Progress:** finish and reopen; confirm the copy's progression behaves as intended.
- **Presentation:** board and counters readable in the chosen portrait sizes; new Console errors absent; timing judged with trace off.

Do not count F9, the inherited debug win/lose hotkeys, or manually assigned state as real-input validation. Do not use a forced result as evidence of a natural win or failure.
