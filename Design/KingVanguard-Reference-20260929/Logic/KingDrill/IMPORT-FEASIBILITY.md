# Native KingDrill rig recovery

The packaged `kingdrill_v3` bundle contains a genuine full-body 2D character rig, not one baked full-body sprite. It has separately rendered arms, legs, torso, head, mouth, moustache, eyelids, shadow and swapped limb poses. This rig is independent of both the old V1 character and the newly imported portrait Spine rig.

The exact `KingAnimator` subtree was recovered to `native-king-drill-import-data.json`:

- 77 transforms with native local position, quaternion, scale, active state, parent and child links.
- 28 SpriteRenderer objects and their 28 referenced sprite assets.
- 11 SpriteSkin components, including original root-bone and bone-transform references.
- Complete vertex positions, triangle indices, UVs, four bone indices/weights per vertex, bind-pose matrices, and sprite bone definitions.
- All 11 skin checks pass: every bone resolves inside the recovered hierarchy, bind-pose count matches referenced bones, and weight count matches mesh vertices. No sprite decoding errors occurred.
- Original Animator data, controller data and six decoded native clips. The raw native clip/controller JSON remains in the reference agent's `EmbeddedKingBundles/Recovered/kingdrill_v3/Serialized` folder.

| Clip | Seconds | Loop | Recovered scalar/object curves |
|---|---:|---|---:|
| KingWalkAnim | 6.616667 | false | 600 |
| KingLoopAnim | 3.083333 | true | 563 |
| KingFailAnim | 10.016666 | false | 652 |
| KingLoop2Anim | 12.383333 | true | 612 |
| KingWinAnim | 2.766667 | false | 649 |
| KingFailWin | 2.383333 | false | 658 |

All six clips run at 60 FPS with controller state speed 1.0. They contain streamed Hermite curves and constants; they have zero dense sample channels. Original stream coefficients remain available in the native JSON. The normalized curves use the project's existing `export_visuals.decode_curves` decoder, so Unity visual/parity checks remain necessary before claiming numerically identical playback.

The native controller default state is `KingLoopAnim`. It has one automatic transition: `KingWalkAnim` -> `KingLoop2Anim`, exit time 1.0, fixed transition duration 0, no conditions. The other states have no automatic outgoing transitions in this controller. External game code chooses win/failure and walking events; those choices are not established by the Animator asset alone.

## Exact remaining limitation

Path hash **2299782333** in walk/idle/idle2 has no matching path in the packaged transform hierarchy. Twenty-seven scalar curves are preserved under the explicit diagnostic path `__UNRESOLVED_HASH_2299782333`; they were not dropped or assigned to a guessed node. Most are constants; the walking clip changes that target's position and scale. This may be a stale authoring target, but that interpretation is unverified. Win, fail and fail-win clips resolve all their transform bindings.

The complete-rig-and-bindings flag therefore remains **false**. This inspection did not create a falsely complete runtime prefab or alter Assets. `native-king-drill-feasibility.json` records every unresolved key and all successful skin checks.

## Unity 6 import route

The project's current package manifest does not include Unity 2D Animation/SpriteSkin. Importing just PNGs and transform animation would lose authored mesh deformation in the face, torso, hands and legs.

A package-independent reconstruction can preserve the recovered hierarchy and create native Mesh assets with the original bind poses/weights. A SkinnedMeshRenderer or an explicit mesh-skinning adapter must retain each source sprite's sorting, color, visibility and sprite-swap animation. SpriteRenderer bindings must either drive an adapter that mirrors these values to the mesh or be translated explicitly and validated. Adding the 2D Animation package is a separate project-level dependency choice; it was not done here.

The source atlas UV layout must remain stable while redrawing parts. Use the recovered atlas/mesh data as the paint template, preserve each sprite's dimensions/pivot/mesh, and update the texture art. Redrawing isolated sprites at different proportions without adjusting their skins would break the original joint alignment.

Before using the native full-body rig in the main game, validate idle, the walk-to-idle2 transition, win, fail and fail-win frame-by-frame in Unity. Compare source and redesign with identical clip time, rig geometry and framing. Check mouth/eyelid/moustache deformation and all swapped limb states. No Unity process was launched by this inspection agent.
