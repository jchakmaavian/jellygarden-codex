# Native KingDrill implementation

The native activity now has an independent Unity 6 importer and runtime adapter under `Assets/Characters/KingVenguard/V2/Activities/KingDrill`. It uses the exact rfv387 native hierarchy, sprites, meshes, skin weights, bind poses and animation curves. It does not use the V1 character and does not add or change Unity packages.

The source's SpriteRenderer components remain as animation-property targets with `forceRenderingOff = true`. Native MeshRenderer and SkinnedMeshRenderer children draw the actual recovered geometry. `NativeKingDrillPart` mirrors source color, enabled state, sorting and flip state, and switches meshes when the SpriteRenderer's sprite reference changes. All six recovered clips currently use GameObject active changes and alpha changes for alternate poses; they contain zero sprite object-reference curves. The importer supports object-reference curves without substituting invented motion.

## Execute in Unity

Call `RoyalThrone.Characters.Activities.Editor.NativeKingDrillImporter.BuildWithTexture(texturePath)` with the redesigned atlas path under Assets. Native dimensions and packing must remain **1024x2048**. `Build()` uses `Assets/Characters/KingVenguard/V2/Activities/KingDrill/Textures/KingDrill_Vanguard.png`; if missing, it copies the original atlas for initial source preview and explicitly records that fact.

The original atlas is `Design/KingVanguard-Reference-20260929/EmbeddedKingBundles/Recovered/kingdrill_v3/Textures/sactx-0-1024x2048-ETC2-KingDrillAtlas-89d7723c__4934485327719105542.png`. The torso `01800_govde` region has the source atlas's vertical packing flip, applied to its exported UVs. The other 27 selected regions are unrotated.

Generated prefab: `Assets/Characters/KingVenguard/V2/Activities/KingDrill/Resources/KingVanguard/KingVanguardDrill.prefab`.

Resource key: `KingVanguard/KingVanguardDrill`.

`NativeKingDrillActivity` exposes `Idle(bool alternate)`, `Walk()`, `Win()`, `Fail()`, `RecoverFromFail()`, `Play(string)`, `FindClip(string)`, and `Sample(string, float)`. `Sample` evaluates an exact clip time and refreshes all visible native parts; use it for deterministic review frames. `Clips`, `Parts` and `Animator` are exposed as read-only properties. The native automatic walk-to-idle2 transition remains at exit time 1.0 with zero transition duration.

The importer applies 3,707 matching curves and leaves the source's 27 unmatched transform curves unapplied. Their full values remain in the evidence JSON under hash 2299782333. It does not fabricate a target transform. All matching float curves retain decoded times and tangents. Raw stream coefficients remain in the original clip serialization for further parity checks.

## Standalone shadow correction

Visual review identified a floating brown/orange vertical shape left of the character. Source inspection confirms it is the original `NewKing/00190_body_G` room-shadow sprite, not a material conversion error. The source position is (-1.99, 1.88, 0), native renderer alpha is approximately 0.392157, and all six clips animate its alpha. The original sprite already contains the brown/orange shape. All 28 source renderers use the same `Sprites-Default` material, with white color, no external alpha and no special shader keywords.

The standalone prefab now hides only this scene-overlay mesh output. The native source SpriteRenderer, mesh, texture, transform and animation curves remain imported unchanged. `NativeKingDrillActivity.SetSceneOverlaysVisible(true)` explicitly restores it for a future full-room reconstruction. Other shadows and character parts remain visible. Animation poses/timing were not changed to address the overlay.

## Verification status

The three implementation C# files compile successfully against the installed Unity 6000.5.10f1 editor/runtime references using an isolated compiler invocation. Final invocation exit code: 0. The preparation script completes with 77 nodes, 28 sprites and 6 clips. Static checks confirm every referenced skin bone and weight/bind-pose count resolves. No Unity process was launched by this implementation agent.

Unity asset creation, repeated import, animation sampling, skinned mesh rendering, atlas appearance and gameplay placement still require the root agent's Unity execution and visual checks. The importer writes `unity-import-report.txt` after successfully creating and structurally checking the generated prefab; that report is not a substitute for visual QA.
