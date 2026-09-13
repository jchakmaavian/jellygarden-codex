# Royal Throne homepage

Implemented in the independent Codex copy on 13 September 2026 after the user approved the RoyalBlue-Home-v1 concept with “impleament.”

## Player experience

The opening Play button loads the existing game scene and its new royal-blue castle home. The screen uses the approved ivory castle, sapphire roofs, gold trim, king portrait, crown coin, heart, star, chest and navigation artwork. The locked Royal Throne logo is unchanged. A soft fountain glow and drifting gold motes use unscaled time; Settings can turn this ambient motion off.

The main Level button opens the highest available unlocked level through the existing pre-level booster and life-check flow. Story provides paginated access to all 100 supplied levels, including completed levels for replay. Locked levels follow the original map's rule: the previous level must have at least one star. Repeated presses are latched while the pre-level panel is open.

The resource bar shows the player's existing currency balance, lives and regeneration timer, and total earned stars. Currency is presented with the approved crown-coin icon; it uses the existing Gems balance and does not rebalance prices or grant currency. Stars are the sum of best saved level stars, not a new spendable restoration currency. The original life-regeneration component remains active under a hidden map HUD, so the homepage does not create a second timer.

Area progress groups the existing levels into sets of six; the final area has four levels. It reports actual completed levels. The Area dialog clearly identifies restoration and chest rewards as coming soon. Leagues, Team and Collection open matching informational panels for future features. Profile reports completed levels and stars. Music and Sound use the existing saved settings and audio managers; Motion is saved as `RoyalThrone.HomeMotion`.

## Scope and artwork

This implementation covers the homepage and its dialogs. Existing pre-level/booster, board, win/loss and shop presentations retain their current artwork. They remain the next candidates for visual consistency work. There is no new multiplayer service, restoration economy, reward claim, character story, trailer, 3D model or mobile export in this change.

Approved masters and provenance remain in `Design/Concepts/RoyalBlue-Home-v1`. Runtime artwork is in `Assets/JellyGarden/Textures/RoyalHome`: all 14 artwork PNGs and 10 UI surface exports are included with Unity metadata. Runtime icon copies are sized for the UI, and the crowned button's transparent margins are trimmed. The castle retains its original pixels. Editable native Unity text, gradient panels and progress fill compose the UI surfaces; equivalent PNG exports remain available in `Surfaces`. The existing locked logo sprite is referenced directly, rather than replaced by a new design.

The castle and fountain are a single rendered background plate with a 3D appearance. Their animation is an overlay effect, not simulated 3D water. The background covers the viewport without stretching. HUD, logo, actions, navigation and dialogs adapt to the available safe area. The old map and its saved-level data remain available behind the presentation.

## Editing and verification

Open `Assets/JellyGarden/Scenes/game.unity` and select **Royal Throne Home**. Its artwork, controls and dialog content are ordinary scene objects. `RoyalHomeScreen` owns presentation and navigation; `RoyalHomeProgress` reads existing saves. `InitScript.TryOpenLevel` is the shared entry used by Home and the original map.

While Home is visible, the existing global menu canvas temporarily uses overlay rendering so its pre-level and life panels appear above the castle. The original camera rendering and sorting order 2 are restored on entering the board. `KeepUIInScreen` handles overlay coordinates directly and keeps legacy Close controls inside the safe area; camera-space behavior is retained. Edge navigation tabs keep their normal scale on focus to prevent their targets extending off-screen.

**Tools → Royal Throne → Home → Build approved homepage** (F6) rebuilds only the homepage root in the copied game scene. It refuses Play Mode, unsaved scenes and the original project path. Adjust the builder as well as the scene if a manual layout change must survive regeneration. It never runs automatically on import.

**Check and capture homepage (Play Mode)** (Shift+F8) runs explicit progress, resource, hit-testing, dialog, pagination, motion, level-entry and return checks while Home is visible. It writes reports and actual Unity game captures to `Design/Branding/Home-Unity`. The check does not award progress or spend currency/lives; it opens and closes the pre-level menu without starting a level. Real level play remains a separate manual verification.

Existing Unity 6 serialization fields may be updated when the game scene is saved. No pre-existing scene objects are removed. Package versions, renderer, player identity, original project and the boss's workspace remain unchanged.
