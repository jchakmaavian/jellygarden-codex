# Native gameplay media recovery

Source: unchanged `Design/Backup/apk/Royal_Throne-rf.apk`, Unity 2021.3.55f1. Reused the existing `resources.assets` extraction and recovered its missing `resources.resource` audio stream directly from the APK. No reference code, services or music are imported.

## Imported media

- Six render-only booster hierarchies: Rocket, TNT, Propeller, Color Ball, TNT+TNT and Color Ball+Color Ball. Original child names, transforms, artwork dependencies, sorting order and alpha values are retained. Native game scripts are excluded.
- Eighteen editable animation clips. Compiled streamed cubic coefficients are decoded into Unity curves with their original key times, values and tangents. Constant curves, object-reference sprite frames, source state names, state speeds and the two Propeller flight transitions are retained. No gameplay animation callbacks are imported.
- Twenty-six particle/beam hierarchies, containing 189 particle systems and two line renderers, with original module settings, sprite lists, colors, velocity/gravity, emission and lifetime curves. Particle sprites share their original atlas textures. Original Unity built-in shader names and material properties are used.
- Forty-four decoded gameplay SFX, including match, fall, swap, booster creation/activation/combinations coins and first-50-level blockers. Native names and path IDs are in `AudioManifest.json`. All decoded durations, channel counts and sample rates match the native AudioClip descriptors; see `AudioValidation.json`.

Runtime resource contracts:

- `ReferenceGameplay/Visuals/{Rocket,Propeller,TNT,ColorBall,TNTTNTCombo,ColorBallColorBallCombo}`
- `ReferenceGameplay/VFX/<original prefab name>`
- `ReferenceGameplay/Audio/<original cue name>__<native path ID>`

`ReferenceGameplayMediaImporter.Build()` and `ReferenceGameplayVfxImporter.Build()` create the editable Unity assets from the scoped native JSON. `ReferenceGameplayVfxImporter.DumpImportedModules()` exports actual Unity 6 module values for source comparison. These tools do not alter gameplay rules or invoke reference game code.

## Exact limitations

- The normal match-piece prefab has no packaged Animator or Animation component. Swap, fall and pop motion driven by native custom code is not recovered as an animation clip. Existing Royal Throne motion remains reconstructed code.
- One active-object curve in `PropellerTapStartFlyAnimation` names path hash `3985679667`, absent from the packaged Propeller hierarchy. It is recorded in `NativeVisuals.json` and `ImportNotes.txt`; no imaginary child is substituted.
- `CupboardItemDoorExplodeParticles` is not connected: its two exact door meshes were recovered, but Android-compiled `Shader Graphs/Cupboard_Door_Particle` and `Royal/Object3DShadowShader` have no recoverable compatible shader source. No replacement shader is invented. Other recovered cupboard particles are available.
- `PotionItemSingleExplodeParticles` includes a native template emitter (path ID 94786) with a null sprite awaiting original runtime subtype configuration. Only that incomplete emitter is disabled; its valid child effects remain. See `VfxIncomplete.txt`. Potion-color, normal-piece-color and box-tier selection must use the owner gameplay event, not activate every source variant together.
- No separate native mailbox particle prefab was recovered. Its custom Spine activity playback is outside this particle recovery.
- Source clip durations and controller state speeds are recovered. Additional IL2CPP gameplay playback-speed overrides, custom sequence scheduling and custom component logic are not recovered as editable source.
- The separate `superlightball_v4` and `gameplayitemsfx4` bundles were inspected offline. Their extra feature assets are not used to replace the ordinary Color Ball or first-50-level presentation.
- Scene and pool integrations are handled by the existing Royal Throne systems. Visual import alone does not establish reference-equivalent gameplay behavior.

Only source-driven compatibility changes are applied: local sorting-layer IDs are mapped to the existing project, and ParticleSystem stop actions use owner-managed cleanup so a pooled object cannot delete itself. Original source dictionaries remain separately recoverable under `Design`, never presented as playable raw APK imports.

## Native value verification

`NativeModuleValidation.json` compares the generated Unity 6 assets against the source Unity 2021 particle and line-renderer data, including active burst/rate, lifetime, size, color/gradient, gravity, shape, velocity, texture-sheet and line-width settings. Recursive source serialization versions are retained to prevent Unity from interpreting curves/colors as older formats. Persistent references are remapped to actual imported material and atlas-sprite assets.

The isolated import fixture separately checks shaders, material references and sprite-sheet dependencies and renders the recovered objects. Obsolete lightmap fields on the two unlit beams, project sorting-layer remapping, and the explicitly disabled null-sprite template are listed as compatibility differences. These checks establish data fidelity and rendering compatibility; they do not establish exact reference gameplay scheduling or visual equivalence by themselves.
