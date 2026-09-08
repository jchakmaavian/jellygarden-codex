# CLAUDE.md — JellyGarden Match-3 Project Rules

## Project
- Project root: `J:/Unity Projects/Match3-Games`
- Game: JellyGarden / Match 3 Jelly Garden Kit v2.5.2
- Unity: `6000.5.10f1`
- Primary benchmark scene: **Level 1**
- Target: premium mobile Match-3 quality comparable to Royal Match in responsiveness, readability, polish, material quality, animation timing, and VFX layering — **without copying Royal Match assets, code, or proprietary designs**.

## Permanent Working Rules
1. **Inspect before editing.**
   - Read the relevant scripts, prefabs, materials, scenes, and audit notes first.
   - Identify the real runtime path before changing anything.

2. **Execute only the explicitly assigned CURRENT TASK.**
   - Never automatically continue to the next phase.
   - Stop after the requested task is implemented/tested or after the requested plan/audit is produced.

3. **Keep diffs narrow.**
   - Do not redesign, refactor, rename, reorganize, or “clean up” unrelated systems.
   - Do not modify unrelated UI, gameplay rules, levels, boosters, VFX, audio, scenes, or project settings.

4. **Preserve game rules unless the task explicitly targets game-feel timing.**
   - Do not change match rules, objectives, scoring, special-piece rules, level data, or board geometry unless specifically requested.

5. **Level 1 is the vertical-slice quality benchmark.**
   - Prototype, test, and visually tune there first.
   - Do not expand changes across all levels until Level 1 reaches the approved quality bar.

6. **Mobile-first implementation.**
   - Prefer pooling, reuse, low allocations, limited overdraw, lightweight fake physics, and bounded particle counts.
   - Avoid repeated `Instantiate/Destroy` in hot match/cascade paths when a reusable approach exists.
   - Avoid unnecessary third-party packages.

7. **Use existing architecture when suitable.**
   - Reuse current pooling, VFX, tuning, sorting, sprite, animation, and utility systems where they are appropriate.
   - Do not replace working infrastructure merely because another architecture is cleaner.

8. **Do not use Royal Match as an asset-copy target.**
   - Use it only as a quality reference for:
     - responsiveness
     - visual hierarchy
     - animation timing
     - physical weight
     - readable debris
     - VFX layering
     - polish
   - All final game art must remain original to this project.

## Confirmed Project Architecture

### Board / Pieces
- The board is **world-space**, not UGUI.
- Normal game pieces use `SpriteRenderer`.
- Main piece prefab: `Assets/JellyGarden/Resources/Prefabs/Item.prefab`
- `Item` root contains gameplay/code components.
- Child `Sprite` contains the rendered artwork and is targeted by authored animation clips.
- This root/child split is useful for layered animation and should be preserved.
- Normal fruit/candy art is currently flattened 2D PNG sprite art, primarily 128×128.
- Special pieces reuse the same `Item` prefab and swap sprite references rather than using separate special-piece prefabs.

### UI
- HUD uses legacy Unity UGUI.
- Board and HUD scale by different systems.
- Board: orthographic world-space camera.
- HUD: width-matched CanvasScaler.
- Existing UI uses many flattened PNG assets.
- Existing text is largely legacy `UnityEngine.UI.Text`.
- TextMeshPro is not currently used.
- Existing project artwork has effectively no useful 9-slicing.
- There are no modern Sprite Atlas assets yet.

### Animation
Multiple animation systems coexist:
- Animator / Animation Clips
- Legacy Animation
- Coroutines + `Vector3.Lerp`
- Runtime-generated AnimationClips
- AI-added code-driven juice animation

There is **no DOTween, LeanTween, PrimeTween, or iTween** installed.

Important:
- Some animation clip events control gameplay timing.
- Do not retime critical destruction clips blindly.
- Understand whether a visual animation releases a gameplay gate before editing duration.

### Match / Cascade Timing
- The current cascade pipeline contains several hard-coded waits.
- The project audit estimates roughly **0.8 s minimum waiting per cascade wave** before visual timing improvements can fully pay off.
- The destroy gate is polled at ~100 ms granularity.
- Gameplay responsiveness work must distinguish:
  - gameplay resolution timing
  - visual-only debris/VFX lifetime

Visual debris should not block board continuation unless the current task explicitly requires it.

### VFX / Juice
Current AI-added systems include:
- `Assets/JellyGarden/Scripts/Juice/`
- `Assets/JellyGarden/Resources/JuiceConfig.asset`
- `Assets/JellyGarden/Resources/JuiceAdditive.mat`
- `Assets/JellyGarden/Shaders/JuiceAdditive.shader`
- `MatchVFXService`

`MatchVFXService` is a useful pooled/allocation-conscious foundation.

`JuiceConfig.asset` contains many live tuning values and should be preferred for iteration when appropriate instead of hard-coding every feel value.

### Rendering
- The project originally used Built-In rendering.
- It is currently switched to URP 2D by prior AI changes.
- The audit reports the current bloom setup as non-functional.
- The added shader has known technical issues in the audit.
- Before making rendering-dependent final art/VFX decisions, confirm whether the current URP setup is being kept and repaired.

### Existing Art
- Existing live gameplay art is mostly flattened PNG sprite art.
- There are no PSD/SVG/AI layered source files for the shipped kit.
- Any serious visual redesign should budget for rebuilding editable source art.
- `Assets/GameRedesign/` contains unused prior concept work including:
  - king/mascot expressions
  - HUD concepts
  - reward/coin concepts
  - reference imagery
- Treat `Assets/GameRedesign/` as prior design exploration, not as active game content.

## Quality Dependency
Use this dependency order:

**Stable runtime → Responsive gameplay timing → Final assets → Primary animation → VFX → Audio/Haptics → Final polish**

Do not compensate for weak assets or motion by simply adding more particles.

## Testing Rule
For any gameplay-feel change:
- test in Level 1
- test the narrow case requested
- check Unity Console for new errors
- confirm no unrelated behavior changed
- report exact files modified
- stop after the task

## Reference Documents
Before major architecture, asset, animation, or VFX work, consult the relevant files under:

`Project_Audit/`

Primary audit:
`02_Unity_Match3_Technical_Visual_Audit.pdf`

Use the audit as evidence of the current project, not as permission to modify every issue it lists.
