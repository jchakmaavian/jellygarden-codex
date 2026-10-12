# Royal Throne production polish — 14 September 2026

The board is the visual priority. Gold/blue branding stays recognizable, while clear
piece silhouettes, a consistent response to input, and a short anticipation → impact
→ fall sequence make each action easier to understand. This is a design hypothesis
for satisfying play, not a measured claim about retention or superiority to another game.

## Implemented scope

| Request | Implementation | Verification |
| --- | --- | --- |
| Larger pieces | Cell-fit art at 94%; board framed from actual occupied squares into the safe gameplay viewport | Live Level 12 inspection; disposable-board sizing checks |
| Mobile hierarchy | Compact cream goal/move cards, smaller king, wider board, booster safe area | Portrait visual inspection and layout checks |
| Optional hints | Shared persisted setting in Home, Pause and Settings; immediately cancels current hint while preserving legal-move discovery | Idle On/Off/re-enable/pause tests |
| Material sound | Two gem fractures, deep TNT, rocket exhaust, rotor, firework shower and celebration; restrained pitch variation and mix headroom | Imported audio checks and captured in-game mix |
| Physical breakup | Original textured chunks remain solid, accelerate downward, recycle below the board; capped pool | Motion, alpha, pause, lifetime and pool tests |
| Power effects | Staged warm TNT core, expanding corona, rays and falling spark tails; stronger group-match sparkles | Powers, all ten pairs, chain and pause tests |
| Celebrations | Gold text entrance, halo, 32 pooled particles and sound; brief duration/cooldown, no input blocking | Lifetime, cooldown, raycast and reduced-motion checks |
| App icon | Original ruby crown/throne icon, separate safe-area adaptive foreground | Default + 18 Android icon slots verified |
| Splash/loading | Existing castle music begins at splash; three crowns gather, burst and settle over real asynchronous progress | Live splash → loading → home inspection |
| Home | Strong Level CTA; selected tab lifts/enlarges and alone shows its label; larger nav silhouettes on simple blue bar | Home pointer/layout/settings regression |

The ingredient counter now counts owned board ingredient pieces, excluding empty
VFX renderers and decorative fragments. This fixes the missing-sprite exception
that could interrupt board generation on ingredient levels.

## Review boundaries

- The independent Codex project only; original boss project unchanged.
- Existing castle music and approved logo remain byte-identical.
- Existing levels, costs, moves, win/lose conditions and future-service availability remain authoritative.
- Reduced motion suppresses shake and reduces decorative particles.
- Editor tests are not a physical-phone frame-time, battery, accessibility, or retention study.

## Reproduction and sources

`master-sfx-v3.cjs` masters only sound effects. `generation-record-v3.json` records
Magnific prompts and creation links. The sounds are AI-designed realistic effects,
not field recordings. Generated source downloads stay outside Assets; signed URLs
are excluded from version control.

The input regression uses virtual devices on a disposable board, routed through the
production InputActions → InputManager → collider hit test → Item.Update path.
See [Unity input-event documentation](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/manual/Events.html).
Android icon slots use [Unity's platform icon API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PlayerSettings.SetPlatformIcons.html).

Run the authored rig builder (Shift+F7) outside Play Mode after editing its source.
Home checks: Shift+F8. Dialog checks: Ctrl+Shift+F8. Open a level without life cost:
Shift+F12. Disposable gameplay, hints and input suite: Shift+F10. App icon setup:
Shift+F6 outside Play Mode. Keep the Game view focused and idle during checks.
