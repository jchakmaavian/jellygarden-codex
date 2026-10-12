# External Best-Practice References

External research was performed **after** the project was understood, and is kept strictly separate from project findings.

**Rule applied throughout:** no external recommendation below is presented as something this project already does. Where the project does something well, it is labelled explicitly.

---

## 1. Text rendering

**PROJECT FACT.** This project uses legacy `UnityEngine.UI.Text` exclusively — 57 components in the gameplay scene, roughly 167 across the project including prefabs. There is no TextMeshPro package and no TMP font asset. The candy-style stroke is produced by the stock `Outline` component, which duplicates the text mesh four times, applied 53 times in the gameplay scene. All 57 Texts have `m_BestFit: 0`.

**EXTERNAL BEST-PRACTICE REFERENCE.** Unity's own documentation describes TextMeshPro as "the ultimate text solution for Unity and the perfect replacement for Unity's UI Text and the legacy Text Mesh", noting that it "uses Advanced Text Rendering techniques along with custom shaders, delivering substantial visual quality improvements". Critically for a mobile title, Unity states the improvement "comes at no additional performance cost, since the geometry created by TextMeshPro uses two triangles per character just like Unity's text components".

- [TextMesh Pro Documentation, uGUI 2.0](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/TextMeshPro/index.html)
- [Unity Manual: TextMeshPro](https://docs.unity3d.com/2020.1/Documentation/Manual/com.unity.textmeshpro.html)

**Relevance.** TMP would replace the faked 4x-geometry outline with a real shader-driven one, and provide the auto-sizing this project lacks.

---

## 2. Sprite atlasing and draw calls

**PROJECT FACT.** There are **zero** Sprite Atlas assets in this project. `m_SpritePackerMode: 2` is enabled and legacy packing tags exist (51 `Pack1`, 24 `Pack3`, 15 `Pack2`, 6 `Game`, 115 untagged), but no atlas asset consumes them. There are 225 sprites and 156 Image components in the gameplay scene.

**EXTERNAL BEST-PRACTICE REFERENCE.** Unity's guidance on sprite atlasing is that objects drawn together should share one atlas, because "using multiple atlases per text object results in multiple draw calls for that object, which consumes more system resources, so as a rule, try to stick to one atlas per object".

- [Sprites, uGUI 2.0 documentation](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/TextMeshPro/Sprites.html)
- [Optimizing Unity UI, Unity Learn](https://learn.unity.com/course/doozyui-related-tutorials/tutorial/optimizing-unity-ui)

**Relevance.** This is the project's cheapest performance win — an import-settings change requiring no art rework.

---

## 3. URP post-processing configuration

**PROJECT FACT.** The 2D Renderer asset created during the recent migration (`Assets/Settings/JG_2DRenderer.asset:57`) has `m_PostProcessData: {fileID: 0}`. URP 17.5's `Renderer2DRendergraph.cs:156` sets `supportedRenderingFeatures.postProcessing` from exactly that null check, so the Bloom volume created at runtime never renders.

**EXTERNAL BEST-PRACTICE REFERENCE.** Unity documents that URP "includes its own implementation of post-processing, so you do not need to install any other package", and that when a renderer is created through the normal editor path, "Unity automatically assigns a default Asset to the post-processing data property that contains the resources (such as Textures and Shaders) that the post-processing effects require."

- [2D Renderer Data Asset, Universal RP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@7.1/manual/2DRendererData_overview.html)
- [Post-processing in the Universal Render Pipeline](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/integration-with-post-processing.html)

**Relevance.** This explains the defect precisely: the renderer was created programmatically, which bypassed the automatic resource assignment the editor performs.

---

## 4. Impact feel and hit-stop

**PROJECT FACT.** The original kit has **no** hit-stop, no camera shake, no screen flash and no impact pause of any kind. A 45 ms `Time.timeScale` micro-freeze was recently added.

**EXTERNAL BEST-PRACTICE REFERENCE.** Writing on game feel identifies a micro-pause on impact as disproportionately effective: "hit-stop, a micro-pause that can be as short as 40 to 80 milliseconds, sells weight better than any animation." The same body of work traces squash-and-stretch, anticipation and follow-through directly to Disney's twelve principles of animation.

- [3 Basic Skills for Game Juice in Unity](https://nucleartide.com/juice-techniques/)
- [How to Make Your Game Feel Good: Game Feel and Juice](https://egmatic.com/blog/how-to-make-your-game-feel-good)

**Relevance.** The added 45 ms freeze sits inside the cited band. Note the project-specific caveat in the main report: because this game's entire pipeline runs on scaled time, a hit-stop slightly lengthens both the cascade and the countdown on timed levels.

---

## 5. Match-3 pacing and responsiveness

**PROJECT FACT.** `LevelManager.FallingDown()` imposes roughly 0.8 s of hard-coded `WaitForSeconds` per cascade wave before any animation plays, plus a flat 500 ms whenever the player earns a special piece and 100 ms per special in a destroyed group. The destroy gate polls at 100 ms granularity, so every animation duration effectively rounds up.

**EXTERNAL BEST-PRACTICE REFERENCE.** Analyses of Royal Match — the current commercial benchmark — describe it as "the fastest switcher with quick and controlled animations, with power-ups and combos' animations being short and fluidic just enough to satisfy players", and specifically note that "players can continue progressing through the level without needing to wait for special effects to complete, ensuring smooth gameplay flow."

- [Design Deep Dive: Royal Match, Unity LevelUp](https://medium.com/ironsource-levelup/design-deep-dive-02-royal-match-948f7af96f04)
- [Game Analysis of Royal Match](https://medium.com/@ekinmelissezer/game-analysis-for-royal-match-and-toon-blast-9c4bff8ef48b)

**Relevance.** The benchmark's defining property is that *gameplay does not wait on presentation*. This project does the opposite: the cascade is gated on every dying piece reporting its animation complete. That is a structural difference, not a tuning difference.

---

## 6. Match-3 visual effects

**PROJECT FACT.** The original core destruction effect is a 4-frame sprite flip-book (`ItemExpl`), 163 x 163 px, about 267 ms, drawn at sorting order 0 — *behind* the fruit at order 2. It has six colour variants and no variation in size, rotation, intensity or count; a 3-match and a 5-match produced identical effects. There are no custom shaders and no post-processing in the original.

**EXTERNAL BEST-PRACTICE REFERENCE.** Unity's official 2D match-3 sample, Gem Hunter Match, demonstrates the modern approach: it uses a Sprite Custom Lit shader to create "shimmery effects that move over pieces", and for impact "the Radial Warp shader uses the URP 2D Camera Sorting Layer Texture to apply a distortion that simulates a shockwave, adding extra visual appeal when you make a match". Broader VFX writing emphasises hierarchy — "the main symbol earns an aura, light rays, and a spark burst, while low-value symbols stay quiet".

- [Gem Hunter Match: lighting and visual effects in a 2D match-3 sample](https://unity.com/blog/engine-platform/2d-puzzle-match-3-sample-gem-hunter-match)
- [VFX for Games: How Studios Build and Outsource Effects](https://sunstrikestudios.com/en/blog/vfx-for-games/)

**Relevance.** Two techniques in the Unity sample — shader-driven shimmer and a radial-warp shockwave — are unavailable in the project's original Built-In configuration and become possible under the URP migration, provided the post-processing defect (section 3) is resolved.

---

## 7. Audio layering

**PROJECT FACT.** One `AudioSource`, one `PlayOneShot` method, no AudioMixer asset anywhere, no pitch variation (`.pitch` is never referenced in any script), no volume slider (two binary on/off toggles), and zero stereo width because both sources weld to the camera at zero distance. Random variation exists in exactly two places (two destroy clips, two drop clips). `m_DSPBufferSize: 0` is the highest-latency preset.

**EXTERNAL BEST-PRACTICE REFERENCE.** Standard practice for this genre is an AudioMixer with separate SFX, Music and UI groups to permit ducking and bus limiting, and pitch variation on frequently repeated effects to avoid ear fatigue. Commentary on match-3 juice specifically calls out escalating feedback — "button glows, coin bursts, star ratings that slam in, progress bars that fill with sparkles" — as the mechanism that makes wins feel earned.

- [Effects in the best Match 3 games](https://www.youtube.com/watch?v=hKlaNTtiyvw)
- [Design Deep Dive: Royal Match](https://medium.com/ironsource-levelup/design-deep-dive-02-royal-match-948f7af96f04)

**Relevance.** This project already contains a working model for escalation — the win sequence plays three star sounds in ascending order 0.5 s apart — and an unused `Combo 01.wav` asset. The pattern and the asset both exist; only the wiring does not.

---

## 8. Object pooling

**PROJECT FACT.** Exactly one pool exists: 20 destroy-effect objects, and `GetExplFromPool()` returns `null` when exhausted, silently dropping the effect. Every other effect — fireworks, colour-bomb clear, booster overlays, stripe beams, score popups, blocker debris — is `Instantiate`/`Destroy` per use.

**EXTERNAL BEST-PRACTICE REFERENCE.** Pooling frequently spawned short-lived objects rather than instantiating and destroying them is long-standing Unity mobile guidance, because each Instantiate/Destroy pair produces garbage that must eventually be collected, which manifests as intermittent frame hitches.

- [Optimizing Unity UI, Unity Learn](https://learn.unity.com/course/doozyui-related-tutorials/tutorial/optimizing-unity-ui)

**Relevance and a fair note.** The recently added `MatchVFXService` **does** follow this guidance — shared persistent particle systems, fixed-size overlay and scheduler pools, and no per-match allocation. It is the one system in the project that independently matches current practice.

---

## Method note

Searches were restricted to `docs.unity3d.com`, `unity.com` and `discussions.unity.com` where the question concerned Unity engine behaviour, and to credible game-development sources for game-feel and genre-benchmark questions. No proprietary artwork, code or visual design from any commercial title was copied or reproduced. Royal Match is referenced only as a published quality benchmark, via third-party written analysis.
