# Reference gameplay integration

Checkpoint: `Design/Backup/ReferenceGameplay-Checkpoint-20260924-121611`.
Source APK retained unchanged. All work is offline; no APK is installed, launched or built.

## Runtime connections

| Imported content | Main-game connection |
| --- | --- |
| Five normal pieces and booster sprites | `RoyalGameplayArt.For` → `ReferenceGameplayCatalog`; native PPU/pivots through `RoyalPieceSkin` |
| Rocket, TNT, Propeller and Color Ball render hierarchies/creation clips | `Item.ChangeTypeCor` → pooled `ReferencePieceVisual` |
| Propeller/Color Ball active native clips | Existing actor → `ReferenceActivationVisual`; existing gameplay callbacks own targets/hits |
| Match particle/fragments | `CandyShatterService.Shatter`; old duplicate match presets suppressed |
| TNT ignition/blast and Rocket exhaust | Existing `RoyalPowerupDirector`/`RoyalRocketSequence` → `ReferenceVfxPool` |
| Color Ball ray and target particles | Existing `ColorBallVfxV4` leases/arrival events → native LineRenderers and contact particles |
| Blocker composites, goal icons, hit/clear feedback | Existing `RoyalCampaignBlockers`; native source groups selected at accepted hit events |
| Gameplay SFX | Existing capped audio director and actor-owned voices → recovered PCM clips; BGM ownership unchanged |
| Levels 01–50 | Main `LevelManager`/spawner/blocker hooks via existing isolated reference QA campaign; data resource `ReferenceGameplay/levels01-50` |

Previous assets are retained. The imported level campaign uses separate QA progress; normal campaign/progress is not replaced. Native renderer/particle data is distinct from reconstructed game code. Existing targeting, match resolution and booster choreography have not been established as exact reference behavior.

## Incomplete reference equivalence

- Serialized board data is recoverable; IL2CPP gameplay source and runtime timing overrides are not editable Unity source. Matching priority, refill policy, blocker damage cadence, combo targeting/order, bonus completion and rewards remain the documented existing adaptation. See `Levels/RULE-PROVENANCE.md`.
- Potion rack is recovered, but runtime bottle placement/colour state is not. Common bottle fragments are connected; unknown coloured splash selection and the source null-Sprite template are disabled.
- Cupboard-door debris needs unavailable custom shader behavior; mailbox activity playback, exact Grass2 composite state and coin reward particle choreography remain incomplete.
- One Propeller active-object curve points to a child absent from its packaged hierarchy. Source identity is retained in the native animation notes.
- Board background/border generation, ordinary swap/fall motion and code-controlled effect staging are not claimed to be imported native behavior.
- Six-colour legacy practice boards can still use the retained sixth-piece fallback. The recovered first 50 levels use only the five packaged normal piece colours.

## Imported assets

1,243 exact sprite crops; 7 shared source atlases; 38 prefabs (6 booster hierarchies, 6 blocker composites, 26 VFX hierarchies); 18 editable clips and 6 controllers; 44 gameplay WAVs. The VFX contain 189 ParticleSystems and two LineRenderers. Per-effect runtime connections and remaining unconnected native sequence hooks are listed in `vfx-connections.csv`.

## Verification boundaries

See `Compile/` for static C# results, `Art/art-validation.json` for pixel/pivot import checks, `Media/AudioValidation.json` for decoded audio checks, `Levels/source-parity.json` for data provenance, and `protected-file-checks.json` plus `apk-integrity.json` for protected files/source integrity.

Isolated Unity import validation passed with zero errors. The actual render check is `Validation/Native-Unity-Render-Check.png`; this is asset rendering, not a main-game recording. The particle comparison checks 50,256 recovered values with zero unexpected differences; native runtime-assigned null-template data is explicitly excluded/disabled. Runtime and Editor compilation also pass using the exact staged source, excluding unrelated local changes (`Validation/Staged-Source-Compile.json`).

Main Unity Play Mode checks passed all five level batches (3,356 assertions). Each level loaded and resolved a real action; representative blocker, fail/retry, win, reward-once and progression checks passed. All batches preserved normal saves. Levels 01–30 passed before the completion fix; 31–50 passed afterward. These are gameplay checks, not a proof that every level is solvable or that reconstructed rules equal the APK. Fresh evidence is in `Validation/Campaign-validation-20260924.md` and its dated archive.

Two issues found during testing were fixed: native pooled visuals defer reparenting until parent deactivation finishes; the reference campaign now retains the existing engine's stable refill during completion, preventing Level 40 from continually producing post-win boosters. Active gameplay cascades and authored palettes are unchanged.

The actual game recorded 13 successful cases at 60 captured frames per simulation second, 1.0× playback, with Unity audio: Match-3/4/5, cascade, both Rockets, TNT, Propeller, Color Ball and four combinations. `Validation/Gameplay-capture-report.tsv` lists them. Source frames/audio remain in `Gameplay-20260924-133720/`; encoded clips and the compact montage are under `Delivery/`. Reviewed screenshots are in `Validation/Gameplay-Screenshots/`. Existing UI/character art is visible because it was intentionally preserved.

Final tracking has zero new console errors. Session cleanup reports zero native effects, particles, activation/board visuals or owned voices; the normal profile is restored and Unity returned to Edit Mode. Captured audio is non-silent without sample clipping. The 16 protected file hashes still match the checkpoint. These checks do not substitute for the incomplete native sequence/state work listed above.
