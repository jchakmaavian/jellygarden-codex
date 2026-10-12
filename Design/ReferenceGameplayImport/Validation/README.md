# Native asset import validation

The isolated Unity 6000.5.10f1 import compiled and instantiated 38 prefabs, with 18 animation clips, six controllers, 324 renderers and 189 particle systems. Materials, available particle sprite dependencies, animation sampling and particle stop/clear were checked. This is asset compatibility validation, not a claim that the main gameplay scene was tested.

`Native-Unity-Render-Check.png` is a real Unity render of recovered prefabs. Every tile uses the same 3.2-native-cell camera frame at 0.18 seconds, with no exposure or bloom adjustment. Read the matching text file for tile names. The horizontal Rocket creation branch is activated for that fixture, as its native orientation branches are authored inactive. The Rocket exhaust tile is intentionally stationary and empty: its source emitters use distance-based emission (10/20/10/15 per world unit) and zero time-based emission. Its moving playback must be checked in actual gameplay. Large native blast rays extend outside this fixed tile framing.

Known source gaps remain explicit:

- Potion effect ParticleSystem 94786 contains a null source Sprite. That template's own renderer/emission are disabled; its recovered child effects are retained.
- Native cupboard-door particles require unavailable compiled ShaderGraph materials; their recovered meshes remain unconnected.
- One Propeller animation source binding could not be resolved.
- The potion rack and bottle views are native, but the source bottle placement/variant configuration is not recoverable. The grass2 selector is likewise unverified.

`Generated-Asset-Manifest.json` lists the exact files copied to the main project. Existing GUIDs were preserved and every generated YAML dependency resolves to an imported asset or built-in Unity resource. Validated texture/audio import settings accompany the assets; PNG pixels and WAV samples are unchanged. Importers protect source alpha and native sprite pivots/rectangles from project defaults.

`ImportedNativeModules.json` retains the Unity roundtrip component data. The separate media comparison report verifies native particle curves and active visual values and documents compatibility-only differences. No Android application was installed, run or built.
