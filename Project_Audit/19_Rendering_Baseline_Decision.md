# Phase 0A — Rendering Baseline Decision

**Project:** `J:/Unity Projects/Match3-Games` · JellyGarden / Match 3 Jelly Garden Kit v2.5.2 · Unity `6000.5.10f1`
**Task:** `PLAN.md` Phase 0A — Rendering Baseline Decision (READ ONLY)
**Status:** ✅ **APPROVED 2026-09-07.** Phase 0B (implementation) is unlocked but **NOT yet executed**. No project file has been modified.
**Companion documents:** `Project_Audit/01–17` (technical & visual audit set) · `Project_Audit/18_Level1_Art_Direction.md` (approved art direction)

---

## Context

`PLAN.md` CURRENT TASK asks for one decision before any art is produced: **KEEP + REPAIR URP 2D**, or **REVERT TO BUILT-IN**.

The project shipped on Built-In. A prior AI session migrated it to URP 2D on 09-07 to enable bloom. The approved art direction (`Project_Audit/18_Level1_Art_Direction.md`, ratified in `PLAN.md`) is **premium baked 2D / 2.5D sprites** — base candy/board/blocker art must look correct with no bloom, no `Light2D`, no normal maps, no runtime post-processing. Phases 3–5 (board, 4 candies, jelly blocker) begin immediately after this gate, so whichever renderer is live becomes the surface every new asset is judged against.

Per the governing rule: URP is not recommended merely because bloom exists, and Built-In is not recommended merely because the project started there. This judges the project as it exists now.

**This document is DECISION + PLAN only. Nothing is implemented.**

---

## RECOMMENDATION

# REVERT TO BUILT-IN

Restore the Built-In Render Pipeline as the Level 1 rendering baseline. Leave the two URP assets on disk, unreferenced, so the migration can be re-taken in ~8 lines if Phase 9/10 VFX later justifies it.

---

## CONFIDENCE

**HIGH.**

The evidence is unusually unambiguous: URP currently delivers **zero** realized rendering benefit, is **structurally incapable** of delivering the one benefit it was installed for, carries an **active hazard** that will damage the art phase starting next, and reverting requires **no changes to any script, shader, scene, prefab, or material** while producing a **pixel-identical frame**.

Confidence is High rather than absolute because two things are genuinely uncertain, and neither is resolvable from a read-only inspection:

- **No profiler or Frame Debugger data was collected.** Every mobile-cost statement below is inference from serialized config plus URP package source, not device measurement.
- **`JuiceConfig.VfxHdrBoost = 1.55`** may currently be producing above-1.0 additive output at runtime (the scenes serialize `m_HDR: 0`, but `UrpBloomSetup.cs:105` forces `allowHDR = true`). Under Built-In LDR that clamps. This could cause a small reduction in additive sparkle peak brightness. It needs a visual A/B, and it is the only plausible visible delta in the entire revert.

**What would flip this to KEEP:** a decision at Phase 9/10 that the premium match VFX layer requires **URP-2D-exclusive techniques** — specifically the Camera Sorting Layers Texture (`JG_2DRenderer.asset:52`, currently `0`) for screen-warp/shockwave effects, or a true post-process bloom pass. That is a real capability and the only one URP uniquely offers here. It is also a *late-phase optional*, and re-migrating costs the same ~8 lines this revert costs. Deferring it is nearly free; keeping it is not.

---

## WHY

**1. The sole reason the migration happened — bloom — cannot render. Not "disabled": structurally impossible.**
`Assets/Settings/JG_2DRenderer.asset:57` → `m_PostProcessData: {fileID: 0}`. Confirmed in URP 17.5 source, four links:
- `Renderer2DRendergraph.cs:136` — `if (data.postProcessData != null)` gates construction of `m_PostProcess` and `m_ColorGradingLutPassRenderGraph`. Both stay **null**.
- `:156` — `supportedRenderingFeatures.postProcessing = m_Renderer2DData.postProcessData != null` → **false** (renderer-feature list at `:29` is `[]`, so nothing ORs in).
- `:970` — `applyPostProcessing = cameraData.postProcessEnabled && m_PostProcess != null` → false forever.
- `:1013` — `RenderPostProcessing(...)` is **unreachable**.

URP 17.5 has no non-rendergraph 2D path, so there is no alternate route. `UrpBloomSetup` setting `renderPostProcessing = true` flips only the left operand. No tonemapping, no colour grading, no bloom is being applied to this game today.

**2. Nothing else in the project uses URP. Not "little" — nothing.**
- **Zero `Light2D`.** Confirmed by GUID (`073797afb82c5a1438f328866b10b3f0`) and by name across every `.unity` and `.prefab`. The 2D lighting subsystem — the entire reason to select a 2D Renderer — is unused, and `m_LightBlendStyles` are the four stock defaults untouched.
- **Zero renderer features**, zero `ScriptableRendererFeature` / `ScriptableRenderPass` anywhere in `Assets/**/*.cs`.
- **Zero `UniversalAdditionalCameraData`** serialized in any scene or prefab (GUID `a79441f348de89743a2939f4d699eac1`, 0 hits). It is added purely at runtime.
- **Zero URP materials on any renderer.** All **1041** renderer material slots point at Built-In `Sprites-Default` (`fileID: 10754`). `Sprite-Lit-Default` and `Sprite-Unlit-Default` appear only in three settings files and on no renderer at all.
- `JG_URP_2D.asset:94` assigns no volume profile.

**3. The SRP Batcher — URP's headline performance argument — is architecturally unavailable on this content.**
- Both custom shaders are `CGPROGRAM` + `UnityCG.cginc` with bare uniforms and **no `CBUFFER_START(UnityPerMaterial)`** (`JuiceAdditive.shader:47,52,55-58`; `CandyFragment.shader:34,37,39-41`) → definitively SRP-Batcher-incompatible.
- `CandyShatterService` drives all 96 fragment `MeshRenderer`s with `MaterialPropertyBlock` (`:142, :389-390, :463`), which **unconditionally disables SRP batching** — so even rewriting both shaders buys nothing without also re-architecting per-fragment tinting.
- `ParticleSystemRenderer` draws are never SRP-batched under any pipeline.
- `Sprites-Default` is likewise not SRP-Batcher-compatible.
- Meanwhile `JG_URP_2D.asset:73` sets `m_SupportsDynamicBatching: 0` — Built-In's dynamic batcher was switched off alongside, so the project gave up one batching path and cannot use the other.

**4. URP is actively hazardous for the art phase that starts next.**
`JG_2DRenderer.asset:58` → `m_DefaultMaterialType: 0` = **Lit** (`Runtime/2D/Renderer2DData.cs:21-26`), resolving via `Renderer2DDataAuthoring.cs:36-45` and `UniversalRenderPipelineGlobalSettings.asset:348` to **`Sprite-Lit-Default.mat`**. With **zero `Light2D`** present, `Renderer2DRendergraph.cs:625-626` fails `Light2DManager.GetGlobalColor` and clears the shape-light textures to **black**.

> **Every new SpriteRenderer created in this project renders BLACK.**

This does not bite today only because all 1041 existing slots predate the migration and every runtime-created renderer assigns an explicit material. Phases 3–5 produce a new board, four new candies and a new blocker. The failure mode presents as *"the new art is broken"*, not *"the pipeline is misconfigured"* — the worst possible signal to inject into an art-approval loop. This class of bug cannot occur under Built-In, whose default sprite material is the unlit `Sprites-Default`.

**5. URP is costing mobile bandwidth right now, for zero pixels.**
`JG_URP_2D.asset:26` `m_SupportsHDR: 1` combined with `UrpBloomSetup.cs:105` forcing `cam.allowHDR = true` requests a **32-bit float intermediate colour buffer** instead of RGBA8 — roughly doubling colour-buffer bandwidth on tile-based mobile GPUs, which is typically the dominant cost in a fill-heavy 2D match-3. That is being paid for a bloom that never executes. *(Estimated from config, not profiled.)* Add the `Light2DManager` init and per-frame `Light2DCullResult` culling running against an empty light set every frame.

Critically: **repairing bloom does not recover this cost — it increases it.** Assigning `postProcessData` constructs a colour-grading LUT pass, an intermediate colour target (`:222`), a bloom mip pyramid, and a final full-screen blit. The current state is URP overhead without URP benefit; the repaired state is more overhead for one effect the approved art direction says the base art must not depend on.

**6. Reverting breaks nothing — the juice layer was explicitly written to survive it.**
`UrpBloomSetup.cs` is **reflection-only** (`FindType()` at `:31-39`, no URP `using` at all) and early-returns on Built-In by design: `:51` `if (!SrpActive) return;   // Built-In: nothing to do, and no error either`. Three independent layers prevent it throwing. Both custom shaders are natively Built-In (`.meta` dated **08-31**, days before URP was configured on **09-07**) and are *more* correct there — the `SPRITE_RENDERER_COLOR` path depends on the legacy `UnityPerDrawSprite`/`_RendererColor` binding, which is guaranteed on Built-In and only *probably* populated under URP (URP's own sprite shaders use `unity_SpriteColor`, `ShaderLibrary/UnityInput.hlsl:169`). `MatchVFXService` and `CandyShatterService` reference no URP type; both fall back to `Shader.Find("Sprites/Default")` — a Built-In name. `MatchVFXService.cs:179-180` already carries the comment *"Harmless on Built-In, where values simply clamp."*

**Revert requires zero script, shader, scene, prefab, or material changes.**

**7. The rendered frame is pixel-identical.**
`ProjectSettings.asset:49` → `m_ActiveColorSpace: 0` = **Gamma** (unchanged by the migration — confirmed by `git diff`). URP grading is `m_ColorGradingMode: 0` (LDR); `DefaultVolumeProfile` tonemapping is **None** and all `ColorAdjustments` are neutral; post-processing is structurally dead (finding 1); all sprites are unlit `Sprites-Default`; `GraphicsSettings.asset:47-48` still holds `m_SpritesDefaultMaterial: {fileID: 10754}`, already correct for Built-In. Existing art renders the same either way.

**8. The migration footprint is 8 lines, and it is the only untracked work in the changed set.**
`git ls-files` vs `git status`: **`JG_URP_2D.asset` and `JG_2DRenderer.asset` are UNTRACKED (AI-added)**. The six other `Assets/Settings/` assets (`Mobile_RPAsset`, `PC_RPAsset`, `Mobile_Renderer`, `PC_Renderer`, `DefaultVolumeProfile`, `SampleSceneProfile`, `UniversalRenderPipelineGlobalSettings`) are **TRACKED URP-template leftovers that were never wired into `GraphicsSettings`** — the committed value was `m_CustomRenderPipeline: {fileID: 0}`. Both template renderers are 3D `UniversalRendererData` (unusable for a 2D game as-is) but both correctly carry `postProcessData` — the AI's renderer was created without it, and did not inherit the template's mobile tuning either (`Mobile_RPAsset:29` `m_RenderScale: 0.8` vs JG's `1`).

The whole change is: 2 lines in `GraphicsSettings`, a 3-line globals map block, and 2 lines × 6 quality tiers. **Both directions cost the same ~8 lines — this decision is cheap to reverse in either direction.**

**9. Committing to URP *properly* is a large, low-payoff conversion.**
2 shader rewrites (`CGPROGRAM`→`HLSLPROGRAM`, `UnityCG.cginc`→`Core.hlsl`, add `UnityPerMaterial` CBUFFER, add `RenderPipeline`/`LightMode` tags, swap `_RendererColor`→`unity_SpriteColor`) · **9 materials** converted, 7 of them legacy particle shaders (`Mobile/Particles/Alpha Blended`, `Legacy Shaders/Particles/Additive`, `Additive (Soft)`) whose blend and `_TintColor` semantics differ from URP's, so **15 particle emitters need hand re-tuning**, not a material swap · **1041 sprite renderer slots** migrated (1007 of them in `LevelsMap.prefab` alone) plus a full visual regression pass · and dropping `MaterialPropertyBlock` from `CandyShatterService` to unlock any batching at all.

Payoff: **no visual change** (both shaders unlit, Gamma colour space) and **no batching change** (finding 3). This work would land directly on top of Phases 3–5 asset production.

**10. The approved art direction needs nothing URP provides.**
Baked 2D / 2.5D, no bloom / `Light2D` / normal-map / post-processing dependency — ratified in `PLAN.md`. The optional enhancement layer the direction permits (§2.12: *"glow must be painted into the sprite or delivered as an additive pooled sprite through the existing `MatchVFXService`"*) is **already implemented that way today**, on pipeline-agnostic `ParticleSystem` and additive-material code. The one thing URP could add on top — a real bloom pass — is exactly what the governing rule says must not drive this decision.

---

## WHAT WE KEEP

Nothing built in this project is lost. Specifically retained, **unmodified**:

| Kept | Why it survives |
|---|---|
| `Assets/JellyGarden/Scripts/Juice/MatchVFXService.cs` | No URP type referenced. `Resources.Load` + `Shader.Find("Sprites/Default")` fallback are pipeline-agnostic. Pooling, scheduler, hit-stop all unaffected. |
| `Assets/JellyGarden/Scripts/Juice/CandyShatterService.cs` | `MeshRenderer` + `MaterialPropertyBlock` + core `UnityEngine.Rendering` enums only. Zero URP types. |
| `Assets/JellyGarden/Shaders/JuiceAdditive.shader` + `.mat` | Natively Built-In CG. Returns to its home path; the `SPRITE_RENDERER_COLOR` binding becomes *guaranteed* rather than *probable*. |
| `Assets/JellyGarden/Shaders/CandyFragment.shader` + `.mat` | Same. |
| `Assets/JellyGarden/Scripts/Juice/UrpBloomSetup.cs` | Kept **as-is**. Returns at `:51` before allocating anything. No throw, no log, no cost. It is the re-migration hook if Phase 9 wants URP back. |
| `JuiceConfig.asset` / `JuiceConfig.cs` | All tuning preserved. Bloom fields become inert (they already are). |
| `Assets/Settings/JG_URP_2D.asset`, `JG_2DRenderer.asset` | **Left on disk, unreferenced.** Not built (not in `Resources`, not referenced by settings). Zero cost, and makes re-migration a 2-file re-point. |
| All 1041 sprite renderer material assignments | Already Built-In `Sprites-Default`. Untouched. |
| All 9 materials, 15 particle emitters, every scene, prefab and clip | Untouched. |
| The `Item.prefab` root/child rig, sorting layers, camera setup | Pipeline-neutral. |

**Regained:** the six `QualitySettings` tiers' `antiAliasing` / `pixelLightCount` / `shadows` / `softParticles` fields become authoritative again instead of being overridden. No practical effect at the shipping default (`m_CurrentQuality: 0` = Fastest, `antiAliasing: 0`), but it removes a second source of truth.

---

## WHAT MUST BE FIXED

**Required for the revert (2 files, ~8 lines):**

1. `ProjectSettings/GraphicsSettings.asset` — `m_CustomRenderPipeline` → `{fileID: 0}`.
2. `ProjectSettings/GraphicsSettings.asset` — remove the `m_RenderPipelineGlobalSettingsMap` `UniversalRenderPipeline` entry (guid `18dc0cd2c080841dea60987a38ce93fa`).
3. `ProjectSettings/QualitySettings.asset` — remove `customRenderPipeline` from all **6** tiers (Fastest, Fast, Simple, Good, Beautiful, Fantastic).

> **Do NOT use `git checkout` on these two files.** They also contain legitimate Unity 6.5 upgrade migration (`serializedVersion 12→16` / `2→5`, `m_GraphicsStateCollection`, `m_ShaderBuildSettings`, `skinWeights`, `globalTextureMipmapLimit`, `terrain*` blocks, `Nintendo Switch 2` quality entry, etc.) that is unrelated to the pipeline and must be preserved. **Targeted line edits only.**

**Verify-then-leave (already correct for Built-In):**

4. `GraphicsSettings.asset:47-48` — `m_SpritesDefaultMaterial: {fileID: 10754}` must remain. Confirm, do not change.
5. `m_AlwaysIncludedShaders` (`:32-43`) — neither custom shader is listed; both survive stripping via their `Resources/*.mat`, which is the correct mechanism and is documented in `MatchVFXService.cs:149-152` and `CandyShatterService.cs:102-103`. Confirm, do not change.

**Flagged, decided separately — NOT part of this revert:**

6. **`JuiceAdditive.shader` applies alpha twice.** `:106` `col.rgb *= col.a * _Boost;` premultiplies in the fragment shader, then `:40` `Blend SrcAlpha One` multiplies by alpha again in hardware. Net output is `tex.rgb * color.rgb * _Boost * alpha²` — particles are dimmer than authored and fade quadratically. Correct premultiplied additive needs `Blend One One`. **This bug is pipeline-independent and exists identically under both.** It belongs to Phase 9 (Match VFX), not Phase 0B.
7. **`_Tint` is dead** (`JuiceAdditive.shader:23,56,90`) — declared, multiplied in, never written by any code. Cosmetic. Phase 9.
8. **`JuiceConfig.VfxHdrBoost = 1.55`** — may need re-tuning after the LDR-clamp A/B in the validation plan. Only if the A/B shows a visible dimming.

---

## WHAT MUST NOT CHANGE

- **Any scene** — `game.unity`, `main.unity`. No `UniversalAdditionalCameraData` is serialized in either, so no scene edit is needed or permitted. Camera settings (`orthographic size: 8.61`, `m_ClearFlags: 1`, `m_HDR: 0`, culling masks) stay exactly as authored.
- **Any prefab** — including all 1041 renderer material slots.
- **Any material** — all 9 `.mat` files, including the 7 legacy particle materials.
- **Either custom shader** — no rewrite, no retag, no pipeline conversion. (The two defects in items 6–7 above are deferred to Phase 9, not fixed here.)
- **Any script** — `MatchVFXService`, `CandyShatterService`, `UrpBloomSetup`, `Item.cs`, `LevelManager.cs`, `Easing`, `SparkleTextureFactory`, `JuiceConfig.cs`.
- **`JuiceConfig.asset`** — no tuning value changed as part of the revert.
- **`Assets/Settings/`** — delete nothing. Both JG assets and all six template assets stay on disk.
- **Colour space** — `m_ActiveColorSpace: 0` (Gamma) stays. The migration did not change it; the revert must not either.
- **The Unity 6.5 upgrade migration** inside `GraphicsSettings.asset` / `QualitySettings.asset` — preserve every line that is not one of the 8 pipeline lines.
- **Game rules, board geometry, match logic, scoring, objectives, level data, timing gates, UI, HUD** — entirely out of scope for Phase 0B.
- **Sorting layers** (`Default`, `UI`) and the established board order (cells 0 → jelly 0/1 → pieces 2 → blockers 3 → shatter 20 → sparkle 25).
- **`Assets/GameRedesign/`** — untouched (its `Resources/` build-size issue is a separate, already-logged finding).

---

## MOBILE IMPACT

**Measured from serialized config:**

| | Current (URP 2D) | After revert (Built-In) |
|---|---|---|
| Intermediate colour buffer | **32-bit float** (`m_SupportsHDR: 1` + runtime `allowHDR = true`) | RGBA8 |
| Post-process chain | None constructed (`postProcessData` null) | None |
| MSAA | Off (`m_MSAA: 1`) | Off at tier 0 (`antiAliasing: 0`) |
| Render scale | 1.0 (template's mobile `0.8` was not inherited) | n/a |
| Depth / opaque texture | Off / Off | Off / Off |
| SRP Batcher | On, but unusable on this content | n/a |
| Dynamic batching | **Off** (`m_SupportsDynamicBatching: 0`) | Available again |
| `Light2DManager` per-frame culling | Runs against an empty set | Gone |

**Net expected effect of reverting: small improvement, zero regression.** The HDR intermediate buffer is the meaningful item — roughly halving colour-buffer bandwidth on tile-based mobile GPUs, which is typically the dominant cost in a fill-heavy 2D match-3. *(Estimated from config and package source. No profiler, Frame Debugger, or device timing was collected — this is the main unmeasured claim in this document.)*

**Android / iOS build path:** unaffected and already healthy. Both platforms are on **IL2CPP**, bundle identifiers are set (`com.DefaultCompany.JellyGarden` / `com.best2dgamesstudio.jellygarden`). Built-In is fully supported in Unity 6000.5.10f1. Shader inclusion is unchanged — both custom shaders ship via their `Resources/*.mat` under either pipeline.

**Deprecation risk, stated plainly:** Unity marks the Built-In pipeline deprecated in 6.5. That is a real long-term factor and the strongest argument against this recommendation. It is outweighed here because (a) Built-In is fully supported for this Unity version and this project's shipping horizon is a Level 1 vertical slice, and (b) the migration costs the same 8 lines whenever it is actually needed. Deferring is nearly free; carrying an unused pipeline through asset production is not.

**Counterfactual — if we KEEP and repair instead:** assigning `postProcessData` adds, per frame, a colour-grading LUT pass, an intermediate colour target, a bloom mip pyramid (6 iterations default), and a full-screen blit — on top of the HDR buffer already being paid. That is the most expensive of the three possible states, in service of an effect the approved art direction forbids the base art from depending on.

---

## SMALLEST IMPLEMENTATION PLAN

1. Capture a Level 1 reference screenshot from the running editor (board with pieces, one match in flight) as the pixel-identity baseline.
2. Close the Unity Editor completely — it caches `GraphicsSettings` / `QualitySettings` in memory and rewrites them on quit, which would undo external edits.
3. In `ProjectSettings/GraphicsSettings.asset`, set `m_CustomRenderPipeline: {fileID: 0}`.
4. In `ProjectSettings/GraphicsSettings.asset`, delete the `m_RenderPipelineGlobalSettingsMap` block's `UniversalRenderPipeline` entry (guid `18dc0cd2c080841dea60987a38ce93fa`).
5. In `ProjectSettings/QualitySettings.asset`, delete the `customRenderPipeline` line pair from each of the 6 quality tiers.
6. Change nothing else in either file — leave all Unity 6.5 serialization migration intact.
7. Leave `Assets/Settings/JG_URP_2D.asset` and `JG_2DRenderer.asset` on disk, now unreferenced.
8. Reopen the Unity Editor and let the asset database reimport.
9. Run the VALIDATION PLAN below.
10. Re-tune `JuiceConfig.VfxHdrBoost` **only if** validation step 6 shows a visible dimming.

**Files touched: 2. Lines changed: ~8. Scripts, shaders, scenes, prefabs, materials, art: 0.**

---

## VALIDATION PLAN

1. **Pipeline is off.** Project Settings → Graphics shows no Scriptable Render Pipeline asset; Quality shows none on any of the 6 tiers.
2. **Sprite default is correct.** `GraphicsSettings.asset` still holds `m_SpritesDefaultMaterial: {fileID: 10754}`.
3. **Console is clean.** Enter Play on Level 1 — zero new errors or warnings. Specifically confirm `UrpBloomSetup` logs nothing and throws nothing (expected: silent return at `:51`).
4. **Level 1 renders identically.** A/B the new frame against the step-1 baseline: board cells, checkerboard, yellow border, all four candy colours, jelly blocker, HUD. Expected delta: **none** (Gamma colour space, unlit sprites both sides).
5. **The black-sprite hazard is gone.** Create a throwaway `GameObject` with a `SpriteRenderer` and any project sprite, assign no material. It must render correctly, **not black**. This is the single most important check — it is the failure this revert exists to prevent, and it must be verified *before* Phase 3 art production begins.
6. **Additive VFX brightness A/B.** Trigger a 3-match and a 5-match. Compare sparkle peak brightness against the baseline. If the `VfxHdrBoost = 1.55` LDR clamp visibly dims the effect, note the delta and re-tune in `JuiceConfig.asset` (remembering the C# default in `JuiceConfig.cs:231` must be updated to match). Expected: negligible, since bloom never ran and the scene cameras already serialize `m_HDR: 0`.
7. **Shatter is intact.** Confirm 4–7 chunks per candy, correct per-fragment tint via `MaterialPropertyBlock`, correct UV mapping from the source candy sprite, correct tumble and fade.
8. **Both custom shaders compile.** Inspect `JellyGarden/JuiceAdditive` and `JellyGarden/CandyFragment` — no compile errors, no pink materials anywhere.
9. **No pool leaks or timing regressions.** Play ~20 moves including cascades and a simultaneous clear. Verify pooled overlay/fragment counts return to rest, move accounting stays at −1 per match, and destruction gate timing is unchanged.
10. **Build path viable.** Switch platform to Android, confirm the build settings resolve with no pipeline errors. (A full player build is out of scope for Phase 0B.)
11. **Working tree is clean of collateral.** `git status` / `git diff` shows exactly two modified `ProjectSettings` files, and the diff contains only the pipeline lines — no reverted 6.5 migration, no touched `Assets/`.

---

## STOP GATE

**This task is DECISION + PLAN only. Nothing has been implemented.** `git status` is unchanged; no `Assets/`, `ProjectSettings/`, scene, prefab, material, shader or script was modified.

Phase 0B (Rendering Baseline Implementation) stays **LOCKED** until this recommendation is reviewed and approved.

On approval, the next action is Phase 0B — execute the 10 numbered steps above and report against the 11 validation checks. **Do not proceed to Phase 1 or Phase 3 in the same task.**

Recommend also recording the outcome in `CLAUDE.md` § Rendering, which currently reads *"currently switched to URP 2D by prior AI changes… confirm whether the current URP setup is being kept and repaired"* — that open question is what this document closes.

---

*Prepared by direct read-only inspection of serialized Unity YAML (`ProjectSettings/`, `Assets/Settings/`, scenes, prefabs, materials), URP 17.5 package source under `Library/PackageCache/com.unity.render-pipelines.universal@d7774c218bdf/`, project shader and script sources, `.meta` importer data, and git tracking state. No file inside `Assets/`, `Packages/`, `ProjectSettings/` or `UserSettings/` was modified to produce this document. Approved 2026-09-07; implementation deferred to Phase 0B.*
