# Royal Match King source verification — rfv387

Source: `Design\Backup\apk\royal_thron_rfv387.apk` (211,664,697 bytes).
SHA-256: `a2cbee4ecf021ee464dc6f17766ba6c0b2addb2d65007f611e5c368532aa8151`.

The two core Unity packs match the requested APK byte-for-byte. Both core packs and all 22 standalone `assets/android/` bundles were inspected. The four King-named bundles were also unpacked and their visual/native data preserved. The broader bundle audit found no additional King character Spine rig; the `table_game` bundle contributes King Workshop UI/area/gameplay atlases and a tutorial hand rig. This is an offline asset/metadata inventory, not a claim that every downloaded game episode is present or playable.

## Recovered character structures

| Structure | Source | Bones / slots | Tracks | Use |
|---|---|---:|---:|---|
| `AreaCompleted.skel` (3.8.99) | `Design\Inspection-rfv387\datapack.unity3d_data\resources.assets` | 58 / 97 | 10 | Upper-body King, articulated arms/fingers, banner/horns; no legs. |
| `King.skel` (3.8.84) | `Design\Inspection-rfv387\datapack.unity3d_data\sharedassets2.assets` | 29 / 35 | 32 | Gameplay portrait with source expression/idle/reaction motion; no legs. |
| `nightmare_popup.skel` (3.8.99) | `EmbeddedKingBundles\kingnightmarecommon_v3_data\CAB-14f8b05708a6270f300861926a702e4a` | 75 / 43 | 6 | Bedtime King reading/sleeping, dog, book, blanket and bed; no leg rig. |
| Native KingDrill | `assets/android/kingdrill_v3` | Unity transform/sprite hierarchy | 6 King clips | Full-body King with independent left/right leg sprites, arms, head and expressions. |

The three Spine binaries contain **48 named tracks, 47 with nonzero duration**; `_3` in the portrait is an empty zero-duration track. All binaries parsed to the exact end of their source payloads. The native KingDrill has six King clips plus the separate drill loop. These are separate source structures, not a single universal turnaround/locomotion rig.

## Native KingDrill clips

| Clip | Seconds |
|---|---:|
| `KingFailWin` | 2.383333 |
| `KingWinAnim` | 2.766667 |
| `KingLoopAnim` | 3.083333 |
| `KingLoop2Anim` | 12.383333 |
| `KingFailAnim` | 10.016666 |
| `KingWalkAnim` | 6.616667 |

Original native controller/clip JSON, sprites, atlas and hierarchy are in `EmbeddedKingBundles/Recovered/kingdrill_v3`. The inventory preserves original data even where an intermediate decoded clip has unresolved hashed transform paths; reconstruction verification is reported separately by the implementation task.

## Original deliverables and texture handling

`Original-rfv387/` contains the three `.skel` binaries, their original `.atlas` text, decoded `.png` textures, and source material evidence. The binaries are directly readable by the installed Spine 3.8 runtime; `.skel.bytes` is a Unity import naming convention, not a conversion. The `*.spine38.json` files in this reference folder are inspection schemas, **not** standard Spine JSON imports.

- Portrait `King.png`: decoded 672 × 1872, exactly matches atlas declared dimensions.
- `AreaCompleted.png`: APK texture is 2048 × 2048, original atlas declares 2048 × 1714. This is the source player texture scaling; runtime adaptation must retain the declared source UV layout or resample the decoded texture to the declared dimensions. The reference file remains the original decoded 2048-square payload.
- `nightmare_popup.png`: decoded 2048 × 2048 and atlas declares the same.
- Source material `_StraightAlphaInput` values are recorded in `source-material-evidence.json`; they are 0 for these source Spine materials, so source textures are intended for premultiplied-alpha rendering. Adapted straight-alpha paintings require a corresponding material setup.

## Activity and logic evidence

The APK IL2CPP metadata exposes `KingUi` and `KingAnimationStateManager` fields/method names, and serialized scene references recover animation-role assignments. Verified integer constants: `StartWorryMoveCount = 3` in both classes and `LBComboExciteRayThreshold = 8` in the state manager. Method names show move/goal updates, idle cycling, worry, happy, excited, sad, relief and combo/win checks. These observations establish data and interfaces; compiled IL2CPP method bodies are not editable original C# source, so do not describe a rewritten policy as a byte-identical recovered game algorithm. See `Logic/` for the focused binding evidence.

## Framing and exclusions

The portrait export bounds are x=-215.85194397, y=-0.16087499, width=431.40197754, height=406.26498413 in source units. The AreaCompleted export bounds include large decorative banner content and must not be used as the standalone character fit rectangle. Official runtime setup-pose measured bounds are separately recorded in `import-report.txt`; they can differ slightly from export metadata and must be measured after presentation exclusions.

`standalone-slot-exclusions.json` lists exact frame/background/banner/horn slot names. Apply exclusions after animation application, because original attachment timelines can restore hidden decoration. Keep the original source binary unchanged.

## All 48 Spine animation durations

| Rig | Animation | Seconds |
|---|---|---:|
| `AreaCompleted.skel` | `king_area 1` | 2.650000 |
| `AreaCompleted.skel` | `king_area_2` | 2.083333 |
| `AreaCompleted.skel` | `king_area_3` | 6.550000 |
| `AreaCompleted.skel` | `king_area_4` | 5.033333 |
| `AreaCompleted.skel` | `king_drill_loop` | 6.516667 |
| `AreaCompleted.skel` | `king_drill_win` | 2.566667 |
| `AreaCompleted.skel` | `king_drill_win_clap_finish` | 0.816667 |
| `AreaCompleted.skel` | `king_drill_win_clap_look_loop` | 1.866667 |
| `AreaCompleted.skel` | `king_drill_win_clap_loop` | 1.200000 |
| `AreaCompleted.skel` | `king_drill_win_clap_start` | 1.466667 |
| `King.skel` | `Happy_1` | 3.333333 |
| `King.skel` | `Happy_2` | 3.766667 |
| `King.skel` | `Heyecan_1` | 5.983333 |
| `King.skel` | `Relax_1` | 1.983333 |
| `King.skel` | `_3` | 0.000000 |
| `King.skel` | `animation` | 4.583333 |
| `King.skel` | `idle_connect_1` | 4.583333 |
| `King.skel` | `idle_connect_2` | 4.583333 |
| `King.skel` | `idle_loop_1` | 8.950000 |
| `King.skel` | `idle_loop_2` | 5.216667 |
| `King.skel` | `idle_loop_3` | 4.216667 |
| `King.skel` | `k-1_dans` | 4.550000 |
| `King.skel` | `k-1_idle_3` | 5.116667 |
| `King.skel` | `k-1_idle_4` | 4.816667 |
| `King.skel` | `k-1_idle_5` | 2.483333 |
| `King.skel` | `k-1_idle_6` | 1.000000 |
| `King.skel` | `k-1_kaygi` | 5.983333 |
| `King.skel` | `k-1_kaygi_loop` | 11.650000 |
| `King.skel` | `k-1_sevinme` | 2.816667 |
| `King.skel` | `k-1_sevinme_2` | 2.250000 |
| `King.skel` | `k-1_sevinme_3` | 0.700000 |
| `King.skel` | `k-1_sevinme _4` | 4.216667 |
| `King.skel` | `k-1_sevinme_5` | 3.766667 |
| `King.skel` | `k-1_sevinme_NEW` | 2.816667 |
| `King.skel` | `k-1_sevinme_NEW_yedek` | 2.816667 |
| `King.skel` | `k-1_sevinme_NEW_yedek2` | 2.816667 |
| `King.skel` | `k-1_sevinme_NEW_yedek3` | 2.816667 |
| `King.skel` | `k-1_uzulme` | 2.816667 |
| `King.skel` | `k-1 animation` | 18.666666 |
| `King.skel` | `k-1sevinme_6` | 3.633333 |
| `King.skel` | `k-2` | 1.916667 |
| `King.skel` | `kaygı-uzulme` | 1.516667 |
| `nightmare_popup.skel` | `animation` | 10.000000 |
| `nightmare_popup.skel` | `animation2` | 10.000000 |
| `nightmare_popup.skel` | `animation3` | 11.666667 |
| `nightmare_popup.skel` | `animation4` | 10.000000 |
| `nightmare_popup.skel` | `dog_cycle` | 1.333333 |
| `nightmare_popup.skel` | `yedek_cycle` | 2.333333 |

## Scope and limitations

No source project was launched, no game server was contacted, and no source APK/package files were modified by this inspection. This task inspected packaged assets and metadata; it did not verify a running Royal Match session. Prior extracted King data was used only for comparison, never as the new import source: all portrait animation timelines happen to match the older copy exactly, while the new source atlas and skin data differ. Current V1 Royal Throne art/rig was not used in this extraction. Costume redesign and integration QA are separate from this source report.
