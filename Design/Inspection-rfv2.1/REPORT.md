# Royal_Throne_rfv2.1.apk inspection

Inspected offline on 2026-09-27. Source file unchanged. Nothing installed or launched on Android; no main Unity assets changed.

The 233,483,381-byte file is an APKMirror-style APKM ZIP container renamed `.apk`, not a standalone Android APK. Inner manifest identifies Royal Match, package `com.dreamgames.royalmatch`, version 38174. It is not a Royal Throne build or version 2.1 internally.

Contains base.apk, arm64-v8a split, armeabi-v7a split, and UnityDataAssetPack split. Android minimum API 24 (7.0), target API 35. All archive CRC checks passed. Android apksigner verified all four inner APKs; signer SHA-256 digests match. This validates package integrity/signature consistency, not runtime behavior or independent publisher provenance.

Playability: not runtime-tested. The archive itself cannot be installed as one ordinary APK. A compatible ARM Android installation needs the base, matching architecture split and asset-pack split together. No x86 native split is included. Startup, network/update requirements and gameplay availability remain unverified.

Unity 2021.3.55f1 main data and asset-pack bundles decoded successfully. Relevant named asset objects were readable with no parser errors. Main asset-pack inventory: 6,573 sprites, 585 textures, 391 materials, 75 shaders, 1,341 animation clips, 213 animator controllers, 617 audio clips, 68 meshes, 105 sprite atlases, 7,683 particle-system objects and 14,967 text assets. These counts do not imply every object is a unique effect, every text asset is a level, or every recovered shader can render unchanged in another Unity project.

Not all game content is available offline. Four bundle configurations list 222 distinct bundle entries; only one has an exact standalone filename match in this asset pack. Missing standalone names include hard_level_backgrounds, hard_level_music_v3, portalitem, tabletitem, grassbombitem and league-room/event bundles. Some content may be embedded in the main data or available under another bundled variant; absence of a standalone filename is not proof of absence of every equivalent asset. The archive is therefore insufficient to claim a complete game-content recovery.

Packaged asset data can be inspected/extracted. Gameplay code is compiled IL2CPP plus metadata, not editable C# source. Packaged shader data, rigs, VFX and platform textures require compatibility work before reuse. No claim of exact editable project reconstruction is made.

`inspection.json` contains the asset names, object counts, configuration comparison and source SHA-256; `asset-files.txt` lists packaged asset entries. Inner APK copies are retained in this inspection directory.
