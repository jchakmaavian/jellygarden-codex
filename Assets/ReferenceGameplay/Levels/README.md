# Imported campaign data, Levels 01–50

`Resources/ReferenceGameplay/levels01-50.json` is the usable runtime projection of the original packaged level definitions. `RoyalReferenceCampaign` loads this resource. The original resources remain recoverable at `Assets/JellyGarden/Resources/RoyalCampaign`.

All 50 authored boards, 3,933 cells, moves, goals, fixed pieces, grass layers, predefined feed entries and spawn-set values are preserved. The row order is converted from the source's bottom-up storage to the existing engine's top-down grid. No source level is invented. `Provenance.json` records each original TextAsset path ID, offset and payload hash.

The untouched native payloads, full decoded fields and schema evidence remain in `Design/Inspection-Levels01-50`. Raw compiled APK objects are not used as Unity assets. See `Design/ReferenceGameplayImport/Levels/RULE-PROVENANCE.md` for the boundary between imported data and existing reconstructed behavior.

The existing isolated QA campaign remains the entry point. It does not replace normal save data or change homepage/Settings UI. Use the existing test-profile flow, or the existing campaign verification entry points. `RoyalThrone.ReferenceGameplay.Editor.ReferenceLevelChecks.VerifyData()` checks the imported resource identity, hashes and runtime data without opening a level or changing saves.
