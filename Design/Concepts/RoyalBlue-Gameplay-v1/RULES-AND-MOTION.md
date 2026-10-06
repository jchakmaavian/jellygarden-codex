# Proposed rules and motion

These are the rules implemented in the standalone browser prototype. They are a proposed Royal Throne design, not a claim about the current Unity game's rules.

## Core loop

The board is 8 × 8. The sample challenge starts with 24 moves, twelve crates (mixed one/two layers) and four one-layer vases. Five royal piece types are matchable. Crates and vases are fixed blockers; matching orthogonally beside them removes one layer. Blasts damage blockers only inside their footprint. A blocker counts toward its goal only when its final layer is removed. A single resolution wave cannot double-count one blocker.

Only neighboring, non-blocker swaps are allowed. Invalid normal swaps return with no move charge. A valid swap, power-up tap, or power-up combination costs one move. All matched groups resolve before gravity/refill, followed by automatic cascades. Input is locked during this resolution. Columns refill independently in segments above/below fixed blockers. There are no holes/portals in this prototype arrangement. A board without legal moves reshuffles automatically without charging a move; ordinary shuffles preserve piece counts and blocker positions.

Completion is checked after settling. Finishing the last goal on the last move wins. With unfinished goals and no moves left, the level fails. Retry opens the same design with a fresh seeded opening. The browser session is disposable and never writes Unity PlayerPrefs or game progression.

## Formations

| Match | Result | Activation |
| --- | --- | --- |
| Three in a line | Normal match | Clears matched pieces and chips adjacent blockers |
| Four in a line | Rocket | Full row or column; orientation follows the line that created it |
| Four in a 2 × 2 square | Helicopter | Origin and orthogonal neighbors, then one target |
| Five in a T/L intersection | TNT | Square radius two: up to 25 cells |
| Five or more in a line | Royal orb | All normal pieces of one color |

Overlapping detections merge into one group. Precedence is orb, TNT, rocket, helicopter, normal match. The created power-up stays at the swapped-to cell when it belongs to the group, otherwise the swapped-from cell or a stable center cell. It does not immediately destroy itself. A horizontal rocket sprite is rotated for the vertical variant.

The helicopter prioritizes unfinished one-layer blockers, then thicker blockers, then normal pieces. Ties are deterministic. This target selection is our design choice; it differs from the random-element wording in Royal Match's help pages. A tap on an orb chooses the most common normal color; swapping an orb with a normal piece chooses that piece's color.

## Combination matrix

| Pair | Prototype result |
| --- | --- |
| Rocket + Rocket | One full row plus one full column |
| Rocket + TNT | Three rows plus three columns |
| TNT + TNT | Square radius four, clipped at board edges |
| Helicopter + Helicopter | Launch neighbors plus three distinct prioritized targets |
| Helicopter + Rocket | Carries a rocket to a prioritized target, then clears its indicated line |
| Helicopter + TNT | Carries TNT to a prioritized target and detonates there |
| Orb + Rocket | Converts the most common normal color to rockets, then fires them |
| Orb + TNT | Converts the most common normal color to TNT, then fires it |
| Orb + Helicopter | Converts the most common normal color to helicopters, then launches them |
| Orb + Orb | Whole-board hit; removes one blocker layer per cell |

Existing power-ups inside a blast trigger once each. In a single chain wave, hit cells are deduplicated; blocker layers take one hit per wave. This explicit prototype rule should be retained in tests or intentionally revised during balancing. Goal collection is clamped at zero.

## Timing targets in the prototype

These are authored timing values, not frame-exact measurements copied from the reference videos.

| Beat | Timing | Visual treatment |
| --- | --- | --- |
| Swap | 145 ms | Cell-to-cell translation; invalid swap returns |
| Match anticipation | 145–180 ms | Small expansion and compression before removal |
| Power-up charge | 130–170 ms | Scale-up cue at the source |
| Rocket | 340–550 ms | Fast directional gold streak with narrow bright core |
| TNT | 340–550 ms | Expanding warm ring, brief soft glow, clipped cell damage |
| Helicopter | About 900 ms | Rotor cue, curved travel, visible payload, target impact |
| Orb | 450–600 ms | Fine connecting rays, color conversion, chained activation |
| Match fragments | 350–550 ms | Color sparks; wood/ceramic chips for blockers |
| Gravity/refill | 240–270 ms | Consistent piece IDs and vertical easing |
| Goal flight | 620 ms | Small target object travels toward its counter |
| King celebration | 650 ms | New expression, shoulder rise, restrained rebound |
| Victory sequence | About 2.2 s, skippable | Four staged blasts, then the win panel |
| Win star | 700 ms | Scale overshoot with rotation and settle |
| Home reward flight | 1.1 s | Star lifts and travels to the home counter |

The browser VFX pool is capped at 160 particles; canvas resolution is capped at 2× display scale. Rocket rays, rings, trails and chips are procedural geometry. The idle halo is a gradient transform, not a video. Pausing freezes existing animations, timers and the VFX loop. Restart invalidates pending sequences. Reduced motion removes particles, shake and transform animations while preserving the rules and concise feedback.

## Before Unity approval and implementation

Review the king's scale/acting, object silhouettes at phone size, the board surface, power-up readability and dialog family. Then implement a vertical slice with an independent rules model feeding presentation events. Validate formation precedence, exact blast footprints, target selection, input locking, save integrity, animation interruption and zero-allocation hot loops in Unity. Profile on agreed target hardware before setting performance or package-size claims. Actual 3D animation rigs, production audio and new level migration require their own acceptance work.
