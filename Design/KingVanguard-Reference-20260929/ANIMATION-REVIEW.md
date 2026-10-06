# King Vanguard animation review

Open **Royal Throne → King Vanguard → Animation Review** in this project's Unity editor.

The viewer loads the new V2 imports directly: portrait, area celebration, nightmare/bedtime, and the native KingDrill prefab. Select a structure and animation, drag the time slider, or use Play and Speed. Reload Assets picks up newly imported textures or rigs. Missing imports are shown explicitly.

The viewer uses a private preview scene. It does not create or modify objects in an open game scene, save a scene, or change source animation tracks. Framing is measured over 13 samples of the selected clip and remains fixed during playback. Source decoration can be toggled; the standalone default removes portrait frames, area banners/horns, and the nightmare background while retaining the nightmare King, dog, book and bedding.

Generate labelled contact sheets from the window, or call this editor method:

```csharp
RoyalThrone.Characters.EditorTools.KingVanguardAnimationReview.RenderContactSheets(
    "Design/KingVanguard-Reference-20260929/AnimationReview");
```

It renders every nonzero animation from all three Spine imports at 0%, 33%, 66% and 95% of the original duration. It also renders every imported native KingDrill clip when that prefab is available. Pages contain up to eight animation rows; each row uses a fixed camera fitted to that clip's sampled bounds. The source portrait's empty `_3` track is available in the viewer and is explicitly skipped in contact sheets.

The output includes PNG contact sheets, `contact-sheet-manifest.json` with exact times/bounds, and `contact-sheet-report.txt`. Missing imports or rendering failures appear in the report and the manifest's `errors`; they are not silently treated as a pass.

These sheets check the actual runtime mesh, attachment swaps, deformation and redesigned atlas. They do not establish that copied IL2CPP game behavior has been reproduced. Native KingDrill unresolved source bindings and reconstruction limits remain documented in `Logic/KingDrill`.

Implementation: `Assets/Characters/KingVenguard/V2/Editor/KingVanguardAnimationReview.cs`.
