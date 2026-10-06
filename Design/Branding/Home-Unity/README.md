# Royal Throne homepage — Unity verification

Verified on 13 September 2026 in Unity 6000.5.10f1, using the independent `JellyGarden-Codex` copy. These PNGs are actual Unity Game-view renders captured with `ScreenCapture.CaptureScreenshot`.

| Game-view size | Result | Report |
| --- | --- | --- |
| 720 × 1280 | 86 assertions passed, zero failures/errors during checks | [Portrait](checks-720x1280.txt) |
| 720 × 1600 | 86 assertions passed, zero failures/errors during checks | [Tall phone](checks-720x1600.txt) |
| 768 × 1024 | 86 assertions passed, zero failures/errors during checks | [Tablet](checks-768x1024.txt) |

Checks cover fresh, partial, non-contiguous and completed progress; the four-level final area; all 100 existing level assets; live resources; preservation of the original life counter; on-screen hit targets; separation of HUD/actions/navigation; Home and settings dialogs; Story locks and pagination; a saved motion toggle; duplicate level presses; pre-level selection; its visible Close button; return to Home; and absence of runtime errors within each check window.

Interactive review confirmed entry from the main scene into the game scene's Home, the Level button opening the existing booster menu, entry to the board, and updated progress on returning Home. The user also played during review: the final captures reflect Level 5, 10 earned stars and 4/6 completed levels in Area 1. The life display was observed counting down from four lives and naturally returning to five/Full. No debug reward or progress override was used by the homepage checks. The splash/loading implementation itself was unchanged; this run verified that its scene transition reaches the new Home.

The original logo's runtime bytes match the approved master. All 24 imported home PNGs have Unity metadata; [runtime-assets.json](runtime-assets.json) records paths, sizes and hashes. [scene-preservation.json](scene-preservation.json) compares scene objects against the pre-home commit: existing objects remain present. Most differences in pre-existing blocks are Unity 6 serialization fields; the map prefab gains a CanvasGroup to hide its graphics while keeping its life counter active.

## Review captures

- Home: [portrait](home-720x1280.png), [tall phone](home-720x1600.png), [tablet](home-768x1024.png).
- Area: [portrait](area-720x1280.png), [tall phone](area-720x1600.png), [tablet](area-768x1024.png).
- Story/replay: [portrait](story-720x1280.png), [tall phone](story-720x1600.png), [tablet](story-768x1024.png).
- Settings: [portrait](settings-720x1280.png), [tall phone](settings-720x1600.png), [tablet](settings-768x1024.png).
- Existing pre-level flow: [portrait](pre-level-720x1280.png), [tall phone](pre-level-720x1600.png), [tablet](pre-level-768x1024.png).

This is Editor verification, not device testing, mobile export, multiplayer validation, a full campaign balance test or a performance benchmark. Restoration rewards, Leagues, Team and Collection are clearly marked coming soon. Existing board and booster artwork retain their original style. Implementation notes are in [HOME-SCREEN.md](../HOME-SCREEN.md).
