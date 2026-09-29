# Royal Throne area redesign v1

Implemented 29 September 2026: original blue-and-gold environments for Areas 1-5.

| Area | Environment | Build tasks | Stars |
| --- | --- | ---: | ---: |
| 1 | Sapphire Castle | 6 | 10 |
| 2 | Crown Throne Room | 6 | 12 |
| 3 | Royal Dining Salon | 7 | 15 |
| 4 | Queen's Crown Garden | 8 | 18 |
| 5 | Grand Royal Library | 8 | 19 |

The runtime catalogue now points to unique art in `Assets/JellyGarden/Resources/RoyalAreas/RedesignV1`. Each area has an empty architectural/landscape base, separate transparent restoration layers, matching task icons, a completed composite, and two thumbnails. This is 90 runtime WebP assets: 35 layers, 35 icons, ten scenes and ten thumbnails. The existing rise animation and replay use those layers directly.

The first five areas retain their original task counts, index order, individual star costs, and reward amounts. The campaign remains ten areas, 87 tasks and 208 stars. Areas 6-10 retain their previous catalogue entries. Existing player progression is not reset or migrated.

## Runtime changes

- `RoyalAreaTask.icon` lets task rows use clear isolated icons rather than wide groups of scene objects. Old tasks fall back to their existing patch.
- `RoyalAreaInfo.bakedAtmosphere` suppresses the legacy foreground clouds and blue sky veil for the newly authored backgrounds. Other backgrounds keep their existing behavior.
- The static area catalogue resets at subsystem registration, so entering Play Mode with domain reload disabled uses the current imported catalogue.
- Imported WebPs retain the project's existing sprite format, desktop compression and ASTC mobile settings; transparent task edges use the existing color-bleed importer.

## Editable sources and reproduction

Each `AreaNN` folder contains the generated background, original transparent object atlas, completed composition, measured crop information and placement manifest. Area 3 uses `cabinet-replacement.png` to retain the complete cabinet crown. Artwork was created using the built-in imagegen tool for this redesign; the previously extracted Royal Match sprites were not copied into these runtime assets.

`build_redesign.py` performs only slicing, resizing, placing and lossless compositing. Its `SPECS` define the task names, measured atlas cells and placement bounds. It supports a workspace-level source folder or this project-local folder. It uses the already installed Pillow dependencies under `Design/ReferenceGameplayImport/Media/Tools/Python`. `baseline-areas.json` preserves the pre-integration catalogue for cost comparisons. Run with the project's Python 3.12 executable; optional integer arguments select individual areas. After generation, refresh Unity and run **Tools > Royal Throne > Areas > Apply area art import settings**.

Source layers reconstruct the completed image exactly, avoiding a different composition appearing on the last task. Scene size is 940x1672; thumbnails are 240x428 and task icons 256x256. The main home and full-scene viewer remain responsive through the existing layout.

## Verified

- Unity 6000.5.10f1 compilation completed with no compile errors.
- All five asset contracts passed, including independent scene keys, expected dimensions, transparent task sprites, icon loading, bounds and import settings: `Unity/asset-checks.txt`.
- At 1080x1920, all 35 task rows showed their matching names and icons; all 35 purchases and independent build layers passed.
- Invalid order, insufficient stars, repeated purchases, chest rewards, area unlocks and all five replays passed. Completing the five areas spent exactly 74 stars and reached Area 6.
- Real player saves and the original QA progression/currency/life schedule were checked and restored; no runtime errors occurred: `Unity/play-checks-1080x1920.txt`.
- Home, task-panel and full-scene screenshots were captured for all five areas. `UNITY-REVIEW.jpg` shows the final Home and full-scene results.

Validation used the connected main project in Unity Play Mode. No new Android APK or physical-device run is claimed for this update.
