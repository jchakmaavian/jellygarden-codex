# Original Royal Throne campaign art

## Source contract

Use the new Royal Throne PNGs generated for this task. The reference-game inspection supplies level/mechanic information, not artwork for this pack. Keep generation/source images with the main task's art outputs. This document records the integration contract; the successful import writes a separate manifest of the actual supplied PNG bytes.

Destination: `Assets/JellyGarden/Resources/RoyalCampaignArt/`.

Required filenames:

- Characters: `king-idle.png`, `king-cheer.png`, `king-think.png`, `dog.png`, `steward.png`.
- Palace: `palace.png`.
- Pieces/reward: `ruby.png`, `shield.png`, `crown.png`, `leaf.png`, `lily.png`, `gem.png`, `coin.png`.
- Obstacles/goal objects: `box1.png`, `box2.png`, `box3.png`, `grass.png`, `cupboard.png`, `mailbox.png`, `egg.png`, `potion.png`.

Optional art-only booster overrides are `rocket.png`, `rocket-vertical.png`, `tnt.png`, `orb.png`, and `helicopter.png`. They do not change booster rules, rigs, timing, VFX or sound. Missing overrides retain the current booster artwork.

## Import and runtime use

`RoyalCampaignArtImporter` imports one centered full-canvas Sprite per PNG at 100 pixels per unit. Alpha is preserved. Textures use bilinear filtering, clamp wrapping, no mipmaps, no CPU read/write, no power-of-two resizing and uncompressed GPU storage. Maximum imported size is 512 for characters/pieces/objects and 1024 for the palace. Android explicitly uses RGBA32 at those same limits.

The existing `RoyalPieceSkin` art cache owns campaign Sprite lookups. `RoyalGameplayArt.Art` and `RoyalPieceSkin.Art` both prefer this pack where a replacement exists, retaining existing artwork as fallback. The sixth match-piece key `vase` maps to `gem`; `home-castle` maps to `palace`; `avatar-king` maps to `king-idle`; legacy coin, grass and crate lookups map to their corresponding new sprites. No gameplay key, color index, save key, scene GUID or matching rule changes.

The existing home screen swaps its palace, King Vanguard avatar, coin and Home crown when those new assets are available. King Vanguard's dog and steward appear beside him only in the existing Profile and Story dialog art area. The dialog keeps its original buttons, level grid and navigation. Cobalt/teal/gold native panels and a small procedural sun-rosette seal supply Royal Throne accents without a new UI flow or cinematics.

## Verification

After PNG delivery, run `RoyalThrone.Editor.RoyalCampaignArtImporter.ImportAndVerify` in Unity. It checks all 21 required assets, Sprite loading, full-canvas bounds, centered pivots, GPU/alpha/import settings, catalog preference and palace/sixth-piece aliases. On success it writes `Design/Inspection-Levels01-50/original-art-manifest.json`, including each source PNG's SHA-256, Unity GUID and imported dimensions. `Verify` performs the checks without reimporting.

The existing V4 import checker explicitly verifies its retained fallback catalog, so installing this pack does not invalidate that maintenance tool.

Static C# compilation of the complete current runtime and editor source sets against Unity 6000.5.10f1 assemblies passed after integration. No Unity import/build was triggered by the integration worker. PNG completeness, source-image visual quality, alpha edges and in-game dialog/board framing still require the main task's asset import and visual pass. Music assets, playback code and music settings were not edited.
