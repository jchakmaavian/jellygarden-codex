# GAME TESTING MODE — architecture and verification

## Existing systems inspected

- Settings: `RoyalHomeScreen` creates the home settings dialog; `MenuManager`, `AnimationManager`, `RoyalMenuSkin` and `RoyalMenuPresenter` own legacy Settings and gameplay pause. QA adds runtime child buttons only. Existing serialized bindings remain intact.
- Economy: `InitScript.Gems` is the single spendable balance, presented as **coins** on Home. There is no second gem-currency wallet. Both QA currency sections operate on that same balance through `InitScript.SetGems`/`AddGems`.
- Stars: `Level.NNN.StarsCount` stores 0–3 stars per level. `RoyalHomeProgress` sums those records. Star controls address the level selector, not an invented global star wallet.
- Lives: `InitScript` owns the cap and resource mutations. `LIFESAddCounter` owns countdown/regeneration, with `RestLifeTimer` and `DateOfExit` persistence. QA sets a three-second countdown to exercise the actual timer.
- Boosters: `BoostType` preference keys store consumable stocks. `ItemsTypes` represents board powers. `RoyalPowerupDirector` owns taps, combinations, projectiles, chain reactions and impact timing.
- Board: `LevelManager`, `Square`, `Item`, `CombineManager` and `RoyalPatterns` own matching, swapping, special creation and refill. QA scenario setup changes runtime cells, then calls their normal execution paths. Authored level text and TargetLevel assets are never saved or edited.
- Targets: runtime `TargetObject` copies and `TargetIcon` displays; level assets remain the source for restart.
- Motion/VFX: `RoyalPieceSkin`, `RoyalHintSolver`/`RoyalHintPresenter`, `MatchVFXService`, `CandyShatterService`, `RoyalToolkitImpact`, `RoyalCelebration`, and the existing complete-menu Animation component. Toolkit Ice/Fire and fracture art use the preceding gameplay integration's fixed pools.
- Inventory: `inspected-asset-inventory.txt` indexes gameplay/toolkit prefabs, materials, animation clips/controllers and settings assets reviewed for integration. Detailed artwork and toolkit dependency audits are in `Design/GameplayArt-v4` and `Design/Effects/Toolkit-review`.

## Isolation boundary

Game scripts alias the inherited PlayerPrefs API to `RoyalSave`. Outside a test session this forwards to the original keys. Within QA, reads fall back to normal data until overridden; every write, removal and save uses the `RoyalThrone.QA.v1/` namespace. Delete tombstones prevent a removed QA key from accidentally exposing the normal value again. Reset removes only indexed QA keys.

Normal runtime resources and random state are captured at session entry and restored at exit. Gameplay returns to Home before switching profiles, retiring the test board and reward animations. The test profile can be reused, but entering QA is always explicit. Starting the application normally never activates it.

QA service, UI and controls compile only for `UNITY_EDITOR` or `DEVELOPMENT_BUILD`, and are omitted when `ROYAL_DISABLE_QA` is defined. Normal release players have no QA bootstrap, menu or action handlers. The save forwarding API remains as a no-op routing layer. QA entry refuses builds with the inherited backend, advertising, Facebook or purchase integration symbols enabled.

## Usage

Open Settings → GAME TESTING MODE → Start test profile. Choose a category, enter an amount/level and zero-based column/row, then select a control. Gem IDs 0–5 follow the existing color array. Power A and B select combination operands. Visual previews expose a small replay toolbar; the main menu contains loop and 0.25× / 0.5× / 1× / 1.5× controls. End test profile returns to normal player data.

Animation fixtures use a reusable 8×8 practice board and actual Item prefabs. Replaying them does not reload the level. Open/restart loads the selected authored level using the normal LevelManager flow. A visual reward replay does not enter the Win state. Instant win enters it once; another outcome requires a restart.

## Verification status

Verified in Unity 6000.5.10f1 Play Mode on 2026-09-15. All 323 QA checks passed, including every registered control, 15 power combinations, replay/loop at all four speeds, live refill timing, single-award outcome handling and exact preservation of normal save keys. See [the executed audit](PlayMode/checks.txt). Ctrl+Shift+F6 reruns it from a normal session in the game scene.

Direct UI verification covered both Settings entries, starting and ending the test profile, typing and applying a balance of 17, category switching, 0.5x Fire Impact and replay without restarting the level. Ending the session restored the normal balance of 3, five lives and 29 stars. [Menu capture](qa-menu.png).

The separate gameplay regression passed 495 checks with no runtime errors, including mouse/touch input, complete-shape idle hints, 3/4/5 matches, powers, gravity and pool caps. Fire Impact was captured through the existing VFX Lab at game speed 0.5 for a 3.6-second review. See `../Effects/Unity-review-v4` for the report, video and contact sheet.

Release and explicit `ROYAL_DISABLE_QA` C# configurations compile successfully with both QA types excluded; see [release-compilation.txt](release-compilation.txt). These are Editor and compilation checks, not an Android device performance certification.
