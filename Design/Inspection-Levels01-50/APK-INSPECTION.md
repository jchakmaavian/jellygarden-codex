# Reference APK inspection — 23 September 2026

This is local reference evidence. The reference APK was opened read-only. No contents were imported into `Assets`, and no source game files were edited by this inspection.

## Package identity

- File: `Design/Backup/apk/Royal_Throne-rf.apk`.
- Size: 246,902,118 bytes; last-write timestamp remained 23 September 2026 14:13:42 local.
- SHA-256: `4493ea05602f18aa3b26ab989acaee33f652aa2a85bd9f3dc3f92360441935a1`.
- Android label: **Royal Match**, despite the reference filename.
- Package: `com.dreamgames.royalmatch`; version name/code `32984`.
- Main activity: `com.dreamgames.library.DreamUnityPlayerActivity`.
- Minimum Android SDK 24, target SDK 35, ARM64 and ARMv7 native libraries.
- Main Unity archive version: **2021.3.55f1**; IL2CPP executable and metadata are packaged.
- Area 01 bundle was authored with Unity **2019.4.38f1**.

Identity was read with the Android `aapt2` bundled with the installed Unity editor. Unity `WebExtract` unpacked the relevant archives. No new frameworks were installed.

## Levels 01–50

All fifty numeric TextAssets named `1` through `50` are present exactly once in `assets/bin/Data/datapack.unity3d`, inside `resources.assets`. Their combined raw payload size is **99,244 bytes**. They contain binary FlatBuffers data, not plain JSON.

- Exact untouched payloads: `ReferenceLevelsRaw/level01.bytes` through `level50.bytes`.
- Per-level source offsets, sizes and SHA-256: `reference-levels01-50-inventory.json`.
- Extraction procedure: `inspect_reference_payloads.py`.
- The separate level-inspection work supplies the decoded schema and manifest; raw presence alone does not establish runtime solvability or observed behavior.

The installed Unity 6 `binary2text` refused the archive's Unity 2021 version header. A separate inspection copy had only that version string replaced so the tool could emit object class/offset metadata. The original APK, unpacked original serialized files, and extracted level payloads were not patched. Main serialized files lack type trees, so this does not provide authoritative decoded component fields by itself.

## Accessible visual evidence

The following exact packaged atlases were decoded to PNG in `ReferenceArt` for visual review:

| File | Contents verified visually | Source |
| --- | --- | --- |
| `GameplayAtlas.png` and `GameplayAtlas-review.png` | Match silhouettes, specials and many blockers; visible yellow crown, blue shield, green leaf and red book, plus rocket, TNT, propeller and lightball parts | `resources.assets`, RGBA32 streamed texture |
| `HomeUiAtlas.png` and `HomeUiAtlas-review.png` | Packaged home UI atlas; individual elements rather than a composed screen | `resources.assets`, RGBA32 streamed texture |
| `UiAtlas.png` and `UiAtlas-review.png` | Packaged common UI atlas | `resources.assets`, RGBA32 streamed texture |
| `LevelWinAtlas.png` | Packaged win UI atlas | `resources.assets`, RGBA32 streamed texture |
| `King.png` | White-haired/bearded king parts, gold crown, blue sleeve and colored HUD frame parts; not a composed portrait | `sharedassets2.assets`, RGBA32 texture, 2048 × 574 |

`atlas-manifest.json` records dimensions, texture format and source stream offsets for the resources atlases. `decode_reference_astc.py` records the bounded texture decoding procedure. An additional Sapphire atlas and gameplay extension atlas were decoded with the installed Unity ASTC DLL; these were exploratory and are not needed for the early-level visual reference.

`reference-asset-names.csv` records named GameObjects, Sprite objects, TextAssets and Texture2D objects with their source byte offsets. Names are direct package data and do not establish that every asset is used in levels 01–50.

The gameplay scene contains named objects for King / King Spine, Goals, Moves / MoveCount, Boosters, Hammer, Arrow, Cannon, JesterHat, settings, music, sound, haptic and quit. The complete scene-name list is `reference-gameplay-scene-objects.txt`. Scene binary extraction alone did not verify responsive positioning, animation timing, hit areas, or a composed gameplay screenshot.

## Palace evidence and limits

The packaged `assets/android/area01.ab` contains a typed sprite atlas with castle towers/buildings, castle stairs, bushes and pool parts. The readable native dump is `ReferencePackage/area01.assets.txt`. Its main texture is a 4096 × 4096 ETC2 RGBA8 atlas (`m_TextureFormat 47`) stored in the bundle's `.resS`. That texture was **not decoded** in this inspection. A composed palace layout must be assessed from runtime screenshots or the authored hierarchy, not inferred from sprite names.

## Device availability

The first read-only device check found one offline device and one unauthorized device. After the user's authorization, ADB reported an online **Infinix X665B**, serial `088083728H004391`. The parent task owns subsequent runtime inspection and testing. This inspection agent did not install, launch or operate apps on it. The installed emulator reported no configured AVDs.

## Boundaries

The binary archives, extracted reference level bytes and competitor art atlases are local inspection material. They should not be imported as Royal Throne shipping assets or included in a public/release artifact. Reference assets establish visual and structural evidence; successful gameplay, progression, UI layout and motion must be verified separately in the target game.
