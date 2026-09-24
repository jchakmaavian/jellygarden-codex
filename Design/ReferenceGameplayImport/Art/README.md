# Recovered native gameplay artwork

Source: unchanged `Design/Backup/apk/Royal_Throne-rf.apk`, `resources.assets` from its Unity data pack. Existing decoded atlases were reused. Missing additive/color/ice/honey atlases were decoded from their original resource streams. No image generation, recoloring, resizing or altered artwork is used.

`Assets/ReferenceGameplay/Art/NativeSpriteCatalog.json` records every imported sprite's actual native name, source path ID, texture dependency, bottom-left pixel rectangle, custom pivot, border, PPU, resource path, stable GUID and content hash. All sprites retain native 140 PPU. The seven original atlas PNGs are retained under `Art/Atlases`; cropped Sprite assets are under `Art/Resources/ReferenceGameplay/Sprites`.

The import contains 1,243 Sprite assets: board pieces, booster parts/frames, blockers/goals, coin frames, colored fragments, additive effect shapes, and the level-completion star. This includes artwork for packaged blockers beyond early campaign usage; presence is not a claim that the corresponding rules or animation controllers are implemented.

The five packaged match-piece types are book/red, shield/blue, crown/yellow, leaf/green and gem/pink. No sixth orange match piece was found. The Color Ball has a complete `default` sphere and separately pivoted upper/lower slow, speed and blur frames. The Propeller is a multi-renderer composition: its `01` sprite is blades, not a complete booster. `runtime-key-candidates.json` identifies verified visual components; composition must use the native prefab transforms.

`cell_front` is an effect dot, not a board tile. Native border pieces need their original geometry/layout. The artwork import does not replace UI/home layouts or declare those connections complete.

Core SpriteRenderer material provenance: native resources renderers reference globalgamemanagers.assets material 2, `Sprites-Default`, linked to built-in shader 10753. Core gameplay PNGs are straight-alpha RGBA. No unpremultiplication was performed. The additive atlas is RGB24 with black backgrounds; those sprites require their original additive blend contract, not a normal alpha Sprite material.

`art-validation.json` verifies all 1,243 crops are byte-for-byte equal to their declared source-atlas rectangles, with unique asset paths and GUIDs. Unity import/runtime validation is owned by the integration checks. Pixels alone do not verify animation timing or native VFX behavior.

The six static native blocker hierarchies are generated as actual Unity prefabs under `Art/Resources/ReferenceGameplay/Blockers`. The isolated Unity import and real-render checks are recorded in `../Validation/`; main gameplay verification is separate. Mailbox uses its recovered Spine idle geometry and original premultiplied-alpha texture, with a local matching blend material. Unrecovered potion placement, grass2 state selection and other limitations remain in `blocker-import-status.json`.
