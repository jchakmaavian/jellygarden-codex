# Coin VFX and audio extracted from rfv387

Source: `Design/Backup/apk/royal_thron_rfv387.apk`. Particle records come from its `resources.assets`; audio bytes come directly from its `assets/bin/Data/resources.resource`. This extraction writes only under `Design/Coin-Reference`.

## Import

1. Copy each `NativeCoinParticles.json` texture's `extractedPath` to its `assetPath`.
2. Run `ReferenceGameplayVfxImporter.BuildFile("Design/Coin-Reference/NativeCoinParticles.json")` in Unity.
3. Integrate the decoded WAVs listed in `CoinAudioManifest.json` where required.

All integration IDs are original source IDs plus **387000000**, avoiding collisions with existing assets imported from the older APK. Original IDs are retained in source metadata. For the importer's existing sprite schema, `sourcePathId` is the integration ID and `originalSourcePathId` is the APK ID.

## Extracted effects

| Export name | APK GameObject ID | Particle systems | Role |
|---|---:|---:|---|
| CoinItemExplodeParticles | 54592 | 2 | Board coin collection: a burst of 10 sparks and one light ring |
| RemainingMovesCoinTracer | 58260 | 2 | Trail under RemainingMovesSingleCoinPilot: star trail emits 10/unit travelled, sparks emit 2/unit |
| CoinHitParticles | 25684 | 3 | Standalone coin hit hierarchy |
| CoinCollectHitParticles | 33370 | 3 | CoinCollectAnimation child hit hierarchy, renamed for a distinct export |

`NativeVFXOriginal` preserves all 33 Transform/ParticleSystem/ParticleSystemRenderer component dictionaries with serialized version tags, source materials, and the two excluded MonoBehaviour raw records. `NativeCoinParticleSummary.json` provides native lifetimes, size/speed curves and emission data. The import document preserves all native particle modules and renderer settings, with local references remapped.

The 1024x1024 AdditiveParticleAtlas is pixel-identical to the current imported atlas and its PNG SHA256 is also identical (`94094726c217e59ebad88aa7128950b3f271f1d38ecca32f075b9802a580107f`). The export still uses its own pathname. Six original sprite rects/pivots/PPUs and two materials are included. Materials use `Mobile/Particles/Additive` and `Particles/Standard Unlit`.

## Audio identity

All four clips decoded successfully to mono 44.1 kHz WAV. The `.fsb` files preserve exact encoded payloads; the manifest records offsets, lengths and SHA256 values.

| Name | APK ID | Decoded duration | Existing project comparison |
|---|---:|---:|---|
| coin_counter | 17638 | 2.003379s | Encoded payload differs from the older source; new WAV extracted |
| coin_appear | 17675 | 1.846009s | Encoded payload differs from the older source; new WAV extracted |
| coin_collect | 17679 | 0.317347s | WAV and encoded payload identical to existing `coin_collect__15714.wav` |
| coin_collect | 17782 | 0.270975s | WAV and encoded payload identical to existing `coin_collect__15802.wav` |

## Boundaries and validation

- Native particle JSON and audio decode were verified. Unity import and visual playback are separate integration checks.
- All non-Transform local object references resolve within each exported prefab or its dependency set. Root Transform parent pointers are retained as provenance; the importer creates detached prefab roots and skips Transform JSON.
- The built-in mesh reference `(fileID 4, pathID 10202)` is retained in the source renderer JSON. The only renderer referencing it has billboard render mode, so the mesh slot is inactive; the existing importer resolves this slot to null.
- MonoBehaviour code does not become C# source when extracting an IL2CPP APK. The two native records are preserved, but the importer excludes scripts. Moving/starting/stopping the remaining-moves tracer must be driven by the project runtime.
- The existing importer deliberately sets stopAction to None, normalizes sorting layer IDs, and activates prefab roots. These are integration adaptations; original settings remain in the source dictionaries.
- Particle randomness is retained as configured by the source; matching modules does not guarantee identical random particles in each run.
