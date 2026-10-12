# PLAN.md — JellyGarden Premium Match-3 Vertical Slice

## Goal
Bring **Level 1** to a premium Match-3 quality bar comparable to Royal Match in:
- responsiveness
- visual clarity
- asset quality
- animation quality
- VFX layering
- physical weight
- audio feedback
- overall polish

The visual identity, assets, shaders, code, and game design must remain original to this project.

---

# IMPORTANT EXECUTION RULE

## Execute ONLY the task marked `CURRENT TASK`.

Do not automatically continue to the next phase.

Each phase has a stop gate.  
After completing the current task, report results and stop for review.

---

# APPROVED BASELINE DECISIONS

The read-only **Asset Audit + Art Direction Foundation** is approved.

Reference:
`Project_Audit/18_Level1_Art_Direction.md`

Approved decisions:

- Core game art uses **premium baked 2D / 2.5D sprites**.
- Base candy/board/blocker art must look correct without bloom, Light2D, normal maps, or runtime post-processing.
- Runtime glow/bloom may be used later only as an optional enhancement layer.
- Specify the full **6-colour candy system**, but use **Level 1 as the implementation benchmark**.
- Level 1 uses:
  - `item_01` red bean
  - `item_02` orange disc
  - `item_03` purple berry
  - `item_04` blue star
  - jelly blocker `block.png`
- `item_05` green jelly and `item_06` yellow are later production.
- Final candy sprites must not contain baked contact/drop shadows that can be torn into shatter debris.
- Preserve the existing `Item.prefab` root/child animation structure.

---

# CURRENT TASK

## Phase 0 — Rendering Baseline Decision — READ ONLY

Determine whether the current renderer foundation should be:

- **KEEP + REPAIR URP 2D**
or
- **REVERT TO BUILT-IN**

This task is inspection + technical recommendation only.

### Inspect
Evaluate:

- `Assets/Settings/JG_URP_2D.asset`
- `Assets/Settings/JG_2DRenderer.asset`
- older URP template assets in `Assets/Settings/`
- Graphics/Quality render-pipeline references
- current post-processing configuration
- why bloom cannot render
- the AI-added `JuiceAdditive` shader/material
- `MatchVFXService`
- `CandyShatterService`
- current SpriteRenderer materials
- particle materials/shaders
- camera configuration
- rendering regressions caused by the migration
- Android/iOS implications
- mobile performance implications
- whether reverting removes useful current work
- whether keeping URP requires broad conversion work

### Governing Art Rule
Do **not** recommend URP merely because bloom exists.

Core candy/board/blocker art remains premium baked 2D / 2.5D and must not depend on runtime lighting or post-processing.

URP features, if retained, are optional enhancement layers for selected special-piece/reward/VFX effects.

### Required Output
Return:

1. `RECOMMENDATION`
2. `CONFIDENCE`
3. `WHY` — maximum 10 findings
4. `WHAT WE KEEP`
5. `WHAT MUST BE FIXED`
6. `WHAT MUST NOT CHANGE`
7. `MOBILE IMPACT`
8. `SMALLEST IMPLEMENTATION PLAN`
9. `VALIDATION PLAN`
10. `STOP GATE`

### DO NOT
- modify GraphicsSettings
- modify QualitySettings
- modify render-pipeline assets
- modify shaders
- modify materials
- modify scenes
- modify prefabs
- modify scripts
- generate or replace art

### STOP GATE
Stop after the rendering recommendation and smallest implementation plan are complete.

Do not implement the recommendation until reviewed and approved.

---

# Production Roadmap

## Phase 0A — Rendering Baseline Decision
### Status
**CURRENT**

Decide:
- KEEP + REPAIR URP 2D
- or REVERT TO BUILT-IN

### Stop Gate
Approved renderer decision + approved repair/revert plan.

---

## Phase 0B — Rendering Baseline Implementation
### Status
**LOCKED UNTIL PHASE 0A IS APPROVED**

Implement only the approved rendering-baseline changes.

### Validation
- Level 1 renders correctly.
- Existing sparkle/shatter systems remain functional.
- No new Console errors.
- Android/iOS target build path remains viable.
- Optional enhancement features do not become dependencies for base art.

### Stop Gate
Known-good Level 1 rendering baseline.

---

## Phase 1 — Gameplay Responsiveness Foundation
### Purpose
Remove dead time that would otherwise limit animation polish.

### Tasks
- Inspect `LevelManager.FallingDown()`.
- Identify hard-coded cascade waits.
- Separate gameplay progression from visual-only VFX/debris lifetime where safe.
- Reduce unnecessary dead air without changing match rules.
- Preserve special-piece correctness and cascade correctness.
- Keep the board responsive while visual debris finishes independently where possible.

### Quality Target
Fast, controlled Match-3 pacing with no unnecessary waiting.

### Stop Gate
Normal 3-match, 4-match, simultaneous clear, and cascade resolve correctly and feel responsive.

---

## Phase 2 — Lock Art Direction
### Status
**COMPLETE**

### Purpose
Establish one consistent visual language before mass asset production.

### Required Specification
- premium soft 2D/2.5D look
- clear silhouettes
- strong but controlled saturation
- consistent top/side lighting
- readable specular highlights
- clean bevel/edge treatment
- soft contact shadows
- restrained board/background contrast
- special pieces visually stronger than normal pieces
- VFX-friendly artwork with clear color/material identity

### Stop Gate
Approved art-direction specification and approved asset production list.

---

## Phase 3 — Level 1 Board Foundation Assets
### Priority: P0

Produce first:
- `square.png`
- `square_2.png`
- board border/frame assets

### Direction
Replace the current flat dark fills + yellow line with a quiet premium recessed-tray system.

### Principle
The board is the evaluation surface for all future candy art.

### Stop Gate
Level 1 board foundation approved before final candy production begins.

---

## Phase 4 — Level 1 Normal Candy Assets
### Priority: P0

Produce and approve:
1. `item_01` red bean
2. `item_02` orange disc
3. `item_03` purple berry
4. `item_04` blue star

### Requirements
Each piece must:
- read instantly at real board size
- have a unique silhouette
- share the same material/lighting language
- remain readable during motion
- preserve identity when fragmented
- work with SpriteRenderer architecture
- keep a centered pivot
- avoid baked contact/drop shadows
- preserve consistent world footprint

### Initial Authoring Target
- 256×256 source
- PPU 200
- preserve current world footprint unless technical validation proves otherwise

### Stop Gate
All 4 Level 1 candies approved inside the real Level 1 board.

---

## Phase 5 — Level 1 Jelly Blocker
### Priority: P0

Redesign:
- `block.png`
- its reused target-icon treatment

### Stop Gate
Level 1 board + candies + jelly blocker form one coherent visual system.

---

# LEVEL 1 VISUAL GATE

Do not continue to broader asset production until:
- board foundation is approved
- 4 Level 1 candies are approved
- Level 1 jelly blocker is approved
- readability works at real gameplay scale
- shatter compatibility is visually validated
- no asset depends on bloom/Light2D to look correct

---

## Phase 5B — Complete the 6-Colour Candy + Special-Piece Asset System

After the Level 1 visual gate:
- `item_05` green jelly
- `item_06` yellow
- striped variants
- wrapped/package variants
- colour bomb
- booster visuals when relevant

Use the same material/lighting system as the approved Level 1 pieces.

### Stop Gate
All 6 normal colours and special-piece families form one coherent approved system.

---

## Phase 6 — Selection + Swap Animation
Refine:
- touch/select response
- lift/scale/highlight
- valid swap
- invalid swap
- swap-back
- interaction responsiveness

### Important
The audit identifies missing piece-selection feedback as a critical experience gap.

### Stop Gate
The very first touch feels responsive and intentional.

---

## Phase 7 — Normal Match Destruction Animation
Refine:
- micro impact
- squash/compression
- breakup cue
- fragment release
- cleanup timing

Primary motion must work before adding heavier VFX.

### Stop Gate
A normal 3-match clearly reads:
**impact → break → debris → board continuation**

---

## Phase 8 — Fall / Refill / Landing / Cascade Motion
Refine:
- gravity/fall timing
- refill entry
- landing squash/bounce
- cascade pacing
- stagger rules

### Stop Gate
Cascades feel fast, readable, and physically coherent.

---

## Phase 9 — Match VFX
Refine:
- sparkle overlap
- candy/jelly shatter debris
- controlled fragment count
- gravity/tumble
- match-size differentiation
- simultaneous-clear clutter control

### Principle
VFX amplifies good assets and motion; it does not replace them.

### Stop Gate
Normal match VFX is premium, readable, mobile-safe, and non-blocking.

---

## Phase 10 — Special-Piece VFX
Refine individually:
- striped/rocket effect
- package/bomb effect
- color bomb effect
- special combinations
- blocker destruction
- objective feedback

### Stop Gate
Each special has a distinct, readable visual signature.

---

## Phase 11 — UI / HUD Modernization
Only after core board gameplay reaches the visual benchmark.

Review:
- HUD composition
- Target / Moves presentation
- King/mascot usage
- objective row
- booster bar
- pause/settings
- typography

Technical modernization may include, if separately approved:
- TextMeshPro
- 9-sliced UI chrome
- Sprite Atlases
- unified reference resolution
- improved safe-area handling

Do not mix broad HUD modernization into board-animation tasks.

### Stop Gate
HUD supports gameplay without competing with the board.

---

## Phase 12 — Audio + Haptics
Add/refine:
- selection SFX
- swap SFX
- match impact
- cascade escalation
- special-piece audio
- landing accents
- reward feedback
- haptics where appropriate

### Stop Gate
Audio intensity supports visual intensity without becoming repetitive.

---

## Phase 13 — Final Level 1 Polish
Perform a focused final pass on:
- input response
- swap
- match
- destruction
- fall
- landing
- cascade
- special creation
- special activation
- blocker response
- HUD feedback
- audio/haptics
- mobile performance

### Final Gate
Do not expand to other levels until Level 1 is approved as the quality benchmark.

---

# Asset Review Order

Use this exact order unless a later approved audit changes a dependency:

1. Board cells + checkerboard + frame
2. Level 1 normal candies: red, orange, purple, blue
3. Level 1 jelly blocker + target reuse
4. **LEVEL 1 VISUAL GATE**
5. Remaining normal colours: green + yellow
6. Striped variants
7. Wrapped/package variants
8. Colour bomb
9. Destruction fragment/material validation
10. Level 1 HUD planks + dedicated target icon
11. Remaining blockers
12. Booster art
13. King/mascot integration
14. Reward/celebration art

---

# Decision Framework

For every existing asset, use:

### KEEP
Already meets the target and integrates cleanly.

### REFINE
Core asset is usable; improve lighting, material, edges, resolution, or polish.

### REDESIGN
Role remains, but silhouette/composition/material direction needs major change.

### REPLACE
Existing asset should not be used in the final Level 1 visual benchmark.

### CREATE
A required asset does not currently exist.

---

# Quality Principle

**Stable Runtime  
→ Stable Rendering  
→ Responsive Logic  
→ Strong Board Foundation  
→ Strong Assets  
→ Strong Motion  
→ VFX  
→ Audio/Haptics  
→ Final Polish**

Never solve weak visual fundamentals by adding excessive particles, glow, debris, or screen clutter.
