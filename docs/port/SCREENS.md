## Royal Throne vs King Leo: screen-by-screen comparison

Status words: **live** (a player can reach it) · **unreachable** (the code or art exists but nothing opens it) · **disabled** (deliberately switched off) · **dev-only** · **legacy-only** (only with the `-legacy-story` flag). In the King Leo column, "Legacy:" describes the `-legacy-story` variant of the same screen.

| Screen | Royal Throne | King Leo (main today) | After the port (this branch) |
|---|---|---|---|
| **Boot** | | | |
| OS / engine launch splash | Live: Android 12+ system splash showing the app icon. Unity's own splash is off. | Live: the OS launch screen, then 'Made with Unity' on charcoal (`m_ShowUnitySplashScreen: 1`). | Unchanged from King Leo. This branch's ProjectSettings still have the Unity splash on. |
| Splash / title | Live **Splash**: Lottie logo animation over a violet gradient. It takes no input and cross-fades to Loading. | Live **Splash / title**: the kit menu re-dressed with a castle painting, the gold KING LEO wordmark ('Adventure of' eyebrow) and a painted Play button. | King Leo's, kept. |
| Loading | Live: art picked by aspect ratio, the Royal Throne logo with a rotating halo and 'Loading...'. It stays up until Home has drawn. | Live: the castle splash art with a centred 'Loading...' while game.unity loads. | King Leo's, kept. |
| Kit Jelly Garden title menu (Logo, Play, Privacy, Wheel) | Unreachable: removed from main.unity, and GUIEvents.Play and PrivacyPolicy have no buttons. | This is the canvas StorySplash re-dresses. The Privacy button is inactive. | King Leo's dressed version, kept. |
| Royal-blue castle title with crowned PLAY (Sept 13) | Unreachable: superseded, though SPLASH-AND-LOADING.md still describes it. | — | Not ported. |
| Twilight castle title (Sept 8) | Unreachable: the art is kept but no scene uses it. | — | Not ported. |
| First-run auto-launch into Level 1 | — | Live: a fresh save skips Home and the popup and opens Level 1 directly. The story starts after the first win. | Kept with the Story shell. OPEN: does StoryShell still launch it before RoyalHomeScreen draws? |
| **Home & navigation** | | | |
| Home | Live: the area scene with drifting clouds. Resource bar (King avatar, Coins +, Lives + timer, Stars +, gear), a green 'Level N' button and a 5-tab bar. | Live: the area painting with a wallet (coins, this area's stars, gear), title plaque, 'N/4 complete', Pip's Map card, next-task card and 'Play Level N'. Legacy: the 'Castle First' home with a floating repair marker and a NEXT PUZZLE capsule. | Royal Throne's RoyalHomeScreen. OPEN: background art (Royal Throne's RoyalAreaScene or King Leo's area painting), and what the Stars pill counts and opens. |
| Home area card / next-task card | Disabled since 2026-10-05: the orange 'Area N' card with a progress bar. The Stars pill replaced it. | Part of Home: a paper next-task card (thumbnail, label, star cost) that opens the Area page. | OPEN: how does Royal Home reach the King Leo Area page (Stars pill, a card, or only the dock)? |
| Home: area complete / end of content | — | Live: after a discovery scene, Home reopens on the finished area. It shows '4/4 complete', 'More adventures are being prepared!' and a hard-coded 'View the sun clue' card. This is the permanent home after Area 2. | OPEN: RoyalHomeScreen has no end-of-content state. Where does this message go? |
| Bottom navigation | Part of Home: 5 tabs (Areas, Leaderboard, Home, Team, Collection). The selected tab is raised. | Live dock: Home / Map / Collection, hidden in levels and on the Area page. Legacy: a green dock (Areas / Ranks / Home / Cards); Ranks and Cards only show a toast. | King Leo's dock: Home → Royal Home, Map → Kingdom Map, Collection → album. OPEN: is Royal Throne's 5-tab bar removed, or trimmed to these three? |
| Lives info dialog ('Royal lives') | Live from the Lives pill: heart, 'Lives x/5', next-life timer and 'Back to home'. It sells nothing. | — (lives are never spent) | Ported with Royal Home and kit lives. |
| Generic Home info dialogs | Unreachable fallback copy (Leagues / Team / Collection 'coming soon', coin and star text). Full pages draw over it. | — | Not ported as screens. The fallback code comes inside RoyalHomeScreen.cs and should stay unreachable. |
| Toast messages | — | Live: a short line near the bottom that fades after about 2.6 s ('Area N is still locked.', 'More adventures are being prepared!' and others). | Kept with the Story shell. |
| **Meta / progression** | | | |
| Areas list | Live **Areas tab**: royal-blue page of 10 area cards (art in a gold border, progress bar or Completed!/View, locked reason strip). | Live **Kingdom Map** (Map tab): 150 cards ordered 150 down to 1. Only areas 1-2 are ready; locked cards have a padlock. Legacy: an 'Areas' list on slate with oak frames. | King Leo's Kingdom Map, through the dock. Royal Throne's Areas tab is not ported. |
| Area task page | Live **Area task list**: blue card under the resource bar, 'Area Progress' bar with a chest, one task row with a green 'Do It ⭐N'. | Live **Area page**: the room painting with a paper sheet (name, 'N/4 complete'), unfinished task rows that are buy buttons, and 'Play Level N'. Legacy: a Royal panel with 'Do It' rows and a chest. | King Leo's Area page. OPEN: does its gear open RoyalSettingsPage, and does its wallet show the Royal coin balance? |
| Completed-area view | Live **Area view**: View on a restored card replays the area full screen (unrestored → each task → restored). | Live **chapter-complete page**: rows hidden. The hard-coded sun clue, 'Next: Outer Courtyard / Coming soon' and 'View the sun clue' replay show even on Area 2. | King Leo's, kept. |
| Task build / repair animation | Live **build sequence**: stars arc into Do It, the controls slide away, the object rises with a puff and the list returns. | Live **sparkle reveal** (all of Area 2): work-type sparkles, a flash and a crossfade, then the star flies to the counter (about 1.85 s). | King Leo's, kept. |
| Native repair transitions (Area 1) | — | Live: **Clear the vines** (VineDissolve shader, 4.55 s), then **Door / Flags / Lanterns** built from a baked parts atlas. | Kept. |
| Authored repair video | — | Unreachable: no VideoClips exist. | Kept, still unreachable. |
| Repair cutscene on a black screen | — | Unreachable: only the kit map or tests can start it. | Kept, still unreachable. |
| Not Enough Stars! | Live card: a 3×3 board → a big star, 'Beat levels to earn stars'. Continue opens the next Level Start card. | — (unaffordable rows are blocked; there is no popup) | Not ported. |
| Area chest rewards tooltip | Live: a speech bubble showing the coin and booster reward. | — | Not ported. |
| Area chest ceremony | Live: the chest moves to centre, 'Tap to Open', and the rewards fly to the counters. | — | Not ported. |
| Restored area showcase | Live: the restored area alone on screen with 'Continue'. | — | Not ported. |
| New Area! / Coming Soon / All Restored! card | Live: the next area greyed under a padlock, a green Unlock button and sparkles. | — (areas unlock silently; the map shows 'Coming soon') | Not ported. |
| Collection | Live **placeholder page**: 'Collections are not open yet' and 'PLAY LEVEL N'. | Live **paper album**: one sun-clue card, no Area 2 entry, no close button. Legacy: the 'Cards' tab only shows a toast. | King Leo's album, through the dock. Royal Throne's page is not ported. |
| Royal Journey level grid | Unreachable: a paginated grid of 12 levels that only editor checks open. | — | Not ported. |
| Kit world map (LevelsMap) | Unreachable: hidden at alpha 0. It is kept alive for the life-regeneration timer and saved-level data. | Unreachable: only if StoryShell.UseMap is true. The shell deactivates it every frame. | Unreachable. OPEN: Royal Throne's life timer relies on this map staying alive, but King Leo's shell deactivates it. |
| **Before a level** | | | |
| Level start popup | Live **Level Start card** (reskinned kit MenuPlay): LEVEL crest, LEVEL GOALS, Color Ball / Rocket / TNT boosts, a ? button, and a green PLAY that spends a life. | Live **Storybook invitation** (StoryLevelPopup): goal well, 3 booster tiles (owned count, EMPTY at 0), Play. The kit MenuPlay is unreachable. Legacy: the 'Sapphire Trim' card. | King Leo's StoryLevelPopup; Royal Throne's card is not ported. OPEN: does its Play spend a life and open the life shop at 0, and does an EMPTY tile open the booster shop? |
| Booster guide (BoostInfo) | Live from ?: 'POWER UP YOUR PLAY' with three booster rows. | Unreachable **kit Boost Info** (only from MenuPlay). | Unreachable: its only entry (the ? on Royal Throne's card) is not ported. |
| Level start cover | Live: a royal-blue cover fades in on PLAY and lifts once the level has loaded. | — | OPEN: keep it behind StoryLevelPopup's Play? |
| Pre-level plate (PrePlay) | Live **'YOUR GOALS'** reminder (about 1.1 s, a tap skips), then the board slides in. | Live **pre-play power plate** (StoryPrePlay): one plate per chosen booster, or the Rocket / Rainbow Ball recipe on levels 2-3. The **kit PrePlay target card** is disabled. | King Leo's StoryPrePlay plates. OPEN: does Royal Throne's 'YOUR GOALS' also run? Both hook PrePlay. |
| **In the level: Board** | | | |
| Board and backdrop | A gold frame that follows the board shape, on a royal-blue gradient with no scenery. 100 kit levels; 1-5 use reference layouts. | A full-bleed board on the area's current repair painting with a gold outline, centred and pixel-snapped. King Leo's own levels. | Royal Throne's board, frame and camera fit on the blue backdrop; the area painting is retired from levels. King Leo's levels are kept; the reference campaign is not ported. |
| Board behaviour | Royal Throne's swap and resolve loop with the RoyalBoardIntegrity watchdog. A best-move hint outline shows when Hint is on. | The kit loop plus StoryOriginalMotion and the StoryDeadBoard watchdog. | Royal Throne's loop, which also fixes main's powerup/refill overlap bug. |
| **In the level: Pieces & powerups** | | | |
| Gems | Royal Throne gem skins. The board is clamped to 5 colours. | Royal piece art (book, coin, gem, shield, leaf, crown), up to 6 colours. | Royal Throne's skins plus a sixth gem for King Leo's 6-colour levels. |
| Powerups and combos | Rocket, TNT, Propeller, Color Ball and their combos, with VFX and screen shake. | Imported approximations of Rocket, TNT and Color Ball with combos (StoryOriginal*). No Propeller. | Royal Throne's powerup director, Propeller included. Main's StoryOriginal* layer and Item tap/land rules are retired. |
| **In the level: HUD** | | | |
| Level HUD | Goal panel (arched gold ribbon), King Vanguard's live Spine portrait in an arched window that reacts to play, and a Moves/Time panel. | Treasure Hunt: a blue 'TARGETS' card (or SCORE bar), a MOVES/TIME crest and a round pause button top centre; no visible portrait. **Legacy Level HUD**: Target plate, King Leo medallion and Moves card. | Royal Throne's HUD with King Leo's StoryAvatar art in the arch (worry / pop / cheer / slump). OPEN: how score-only levels show and win under the goals-only win rule. |
| Kit in-level HUD (Panel + BottomPanel) | Unreachable: alpha 0. Its boost buttons are moved into the tool rail. | Faded to alpha 0 in Treasure Hunt and kept as animation anchors. Legacy draws its HUD inside it. | Hidden, as in Royal Throne. |
| **In the level: Booster bar** | | | |
| In-level booster bar | One pill: Royal Hammer, Arrow, Cannon and Jester Hat (red count, or green + when used up) plus a blue settings button. Padlocked on levels 1-5. | Four enamel buttons: Extra Moves (+5 / +30s), Hammer, Bomb, Shuffle. Disabled at 0, so no shop opens. Legacy: the kit tray with pause in the centre. | Royal Throne's rail and tools; its settings button opens Pause. Extra Moves leaves the bar. OPEN: are King Leo's free StoryFreeGame grants kept alongside coin purchases? |
| Tool selection prompt | Live: an instruction plate with a cancel X. The chosen tool gets a gold halo. | — | Ported. |
| **In the level: Banners** | | | |
| Praise words | Live **cascade celebration** (GREAT! / BRAVO! / FANTASTIC! / BRILLIANT!) with halo and sparkles. The **kit gratzWord1-3** are unreachable. | Live **praise words**: kit Good / Great / Fantastic repainted bronze, silver and gold. | Royal Throne's celebration banner. The kit words go unused. |
| Auto-shuffle ('SHUFFLE!') | Live: plain gems glide to new cells with a guaranteed move. | — | Ported. |
| No More Matches banner | A fallback only, when the shuffle fails. Kit art, not restyled, then the board regenerates. | Live: a repainted crimson wordmark, then the kit regenerates the board (kit AI or StoryDeadBoard). | A fallback only, as in Royal Throne. OPEN: keep or drop StoryDeadBoard (plan Task 4.5)? |
| 'Level Completed' label (CompleteLabel) | Live: unstyled kit art set active on Win, behind the win card. | Live: a repainted royal wordmark during the leftover-moves bonus phase. | Royal Throne's timing (on Win). |
| Pre-complete banner | Unreachable: the kit PreCompleteBanner is never shown. | Unreachable: dressed as a 'Wonderful!' ribbon, but the call was removed. | Unreachable. |
| **End of level** | | | |
| Win celebration overlay | Live (about 3 s): the board dims, the ROYAL THRONE logo builds with pennants, trumpets and fireworks, then shrinks away. | — | Ported. OPEN: it spells ROYAL THRONE. Swap in the KING LEO wordmark? |
| Leftover-moves finish | Live: the Goal panel becomes a 'Reward' coin counter, remaining moves fire as rockets, and coins pop into the counter. | Live: leftover moves fly out as Rockets and every special fires in volleys. No coins. | Royal Throne's coin finale. CommitWin pays coins and refunds the life. |
| Win card | Live **'Well Done!'** (MenuComplete): crest, rotating rays, one star onto a velvet pillow, confetti, Continue. No score and no king. | Live **Storybook Victory popup**: Pip, a gold star, '+1 Star' or 'Star already earned', 'Back to Castle'. Kit MenuComplete is disabled. **Legacy**: the painted win_panel with Continue. | Royal Throne's 'Well Done!'. The win also pays King Leo's area star. OPEN: where do the owed opening and discovery scenes play, and is the card's star King Leo's area star? |
| Home reward collection | Live: Home controls slide in, the star curves into the Stars pill and coins stream in. The task list opens if a task is affordable. | — | Ported with Royal Home. OPEN: open King Leo's Area page instead of Royal Throne's task list? |
| Out-of-moves continue offer | Live **'SO CLOSE!'** (reskinned PreFailed): goals left, CONTINUE for coins (the Shop opens if short), END LEVEL. | Disabled: **kit PreFailed** (+5 moves for 12 gems) returns early and GameOver follows. | Royal Throne's 'So Close!' coin continue. |
| Level failed | Live **Level Failed**: broken heart, goals with X, a sad King Vanguard bust, Try Again / End Level. | Live **Storybook Lose popup**: grey star, 'Out of moves/time', area repairs bar, 'Back to Castle'; no retry. Kit MenuFailed is disabled. **Legacy Lose popup**: Continue. | Royal Throne's card with King Leo's slump art. OPEN: should Try Again open StoryLevelPopup? |
| Dog star-reward animation | Unreachable: a design preview (MP4 / HTML) of a corgi offering the star. | — | Not ported. |
| **Story & tutorials** | | | |
| Opening scene | — (no story system) | Live **OPEN-01** after the first win: Pip with the six-key map strip, Leo and Pip dialogue cards, Skip. It resumes if interrupted and can be replayed from Pip's Map. **Legacy opening dialogue** (Alfred / Leo / Mimi). | Kept. OPEN: what triggers it once Continue goes to Royal Home, and where does the Pip's Map replay live? |
| Area completion beat | Live **'Area Completed!'** banner: a gold-and-blue ribbon, with King Vanguard rising and giving a thumbs-up. | Live **discovery scene** (Area 1 sun clue / Area 2 horse fountain): clue picture on the map strip, with Pip and Freya cards. | King Leo's discovery scene, kept. The Royal Throne banner is not ported. |
| Clue card | — | Legacy-only: a black scrim with the clue image and sentence after an area's last repair. | Kept, legacy-only. |
| Arc-beat cutscenes | — | Legacy-only: 30 scenes, one every fifth area, with multi-speaker cards. | Kept, legacy-only. |
| Sabotage intro (Grimm) | — | Legacy-only: shown once, at level 157. | Kept, legacy-only. |
| Task-completion dialogue card (incl. Mimi banner beat) | — | Unreachable: every Area 1/2 onComplete line is empty. | Kept, still unreachable. |
| Story cinematics (proposal) | Unreachable: a draft only (Wishlight opening, area intro and completion scenes). | — | Not ported. |
| Level 1 hand tutorial | Live: a dark layer and an animated hand, with 'Match next to a crate to smash it!'. | Live kit TutorialManager: black layer, label and a pointing hand; not restyled. | OPEN: which version? Royal Throne's caption is written for Royal Throne's level 1 layout. |
| Power lessons (levels 2-5) | Live, non-blocking: hint and hand for Rocket and TNT, then captions for grass and for combining power-ups. | — (StoryPrePlay's recipe plates on levels 2-3 cover similar ground) | Not ported. |
| How to Play lessons | Live from Settings or Pause HELP: six lessons, each looping on a live mini board. | Unreachable **kit Tutorial pages** (only from kit Settings). | Not ported. OPEN: hide Pause HELP and the Settings 'How to Play' row, or point them somewhere? |
| First-visit area guide | Live once after Level 1: a spotlight on the Stars pill with 'Tap here to open your task list.' | — | Not ported. |
| **Shops & economy** | | | |
| Shop page | Live from the coin pill and every coin shortfall. Coins tab: 5 Google Play packs. Stars tab: stars for coins plus 3 money packs. | — (coins show in the wallet, but nothing sells) | Ported. OPEN: Google Play product IDs and billing for King Leo (including when embedded in the VMP app); do bought stars feed King Leo's area-star pools? |
| Google Play purchase sheet | Live: the OS billing sheet. The editor uses a fake store. | — | Comes with RoyalStore. OPEN: same as Shop page. |
| 'Not enough coins' notice | Live: a navy pill with the balance, then the Shop opens. | — | Ported. |
| Booster shop | Live **'ROYAL BOOSTERS'** from a 0-stock pre-level booster: three coin buy rows. | **Kit BoostShop** (gems, unstyled), legacy-only, from a 0-count kit booster. | Ported. OPEN: what opens it from StoryLevelPopup's EMPTY tiles? |
| Life shop | Live **'MORE LIVES'**: heart, timer, a coin REFILL; the video button is hidden. | Disabled **kit Lives Shop** (lives are never spent). | Ported with kit lives. OPEN: should StoryLevelPopup's Play open it? |
| Tool purchase panel | Live when a 0-stock tool is tapped: tool art and 'Buy tool pack' for coins. | — (zero-count buttons are disabled) | Ported. |
| Kit coin shop (GemsShop) | Unreachable: reskinned 'ROYAL COINS', replaced by the Shop page. | Disabled: returns early (ShopsAllowed is false). | Unreachable. |
| Reward card | Unreachable **'A ROYAL REWARD'** chest card: it needs a rewarded ad. | Unreachable **kit Reward popup**. | Not ported. The kit popup stays unreachable. |
| Full-screen ads (interstitial / rewarded) | Disabled: ServicesEnabled is false, no ad defines are set, and video buttons are hidden. | Disabled: AdsAllowed is false and no ad defines are set. | Disabled. |
| **Social & profile** | | | |
| Profile page | Live from the Home avatar: player card, General Stats, Edit Profile. | — | Not ported. OPEN: what does the avatar on Royal Home's resource bar do? |
| Edit Profile card | Live: Avatar / Frame / Name / Badge tabs and Save. | — | Not ported. |
| Leaderboard | Live placeholder: Friends / Players / Teams tabs and a King Vanguard card. Connect shows 'Coming soon'. | Legacy 'Ranks' tab only shows a toast. | Not ported. |
| Team | Live placeholder: 'Teams are not open yet' and PLAY LEVEL N. | — | Not ported. |
| Facebook login / share | Disabled: Connect shows 'Coming soon' and SHARE is hidden. | Unreachable: the **kit FB / offline leaderboard strip and Share** are only inside the bypassed MenuPlay / MenuComplete and the hidden map. | Absent / disabled. |
| **Settings & system** | | | |
| Settings | Live full page from the Home gear: Music, Sound, Motion and Hint switches, volume sliders, How to Play (plus a Mod Menu row in dev builds). | Live **Storybook panel** from the wallet gear: Reset Progress and Quit / 'Exit to VMP'; no sound toggles. **Legacy**: the same logic in Royal style. | Royal Throne's Settings page. OPEN: move Reset Progress and the VMP host exit into it? |
| Reset progress confirmation | — | Live: 'This cannot be undone…' then 'Yes, wipe it' wipes the save and returns to Area 1. | OPEN (see Settings). |
| Kit Settings popup | Unreachable: reskinned 'SETTINGS', only reachable from the hidden map. | Unreachable: restyled by StorybookKitMenus, only reachable from the hidden map. | Unreachable. |
| Pause | Live **'TAKE A BREATHER'**: CONTINUE, Music / SFX with sliders, MATCHING HINTS, HOME and HELP. It also opens when the app goes to the background. | Live, Storybook-dressed: 'Paused', Sound / Music, 'Keep Playing', and 'Back to Castle' (the kit's BackToMap → Lose popup saying 'Out of moves'). **Legacy**: undressed kit art. | Royal Throne's Pause skin. The dev Mod Menu chip is not ported. |
| Quit Level? confirm | Live from HOME: 'You will lose a life!'. Red Quit goes Home; blue Cancel returns. | — (quitting goes through the Lose popup) | Ported. |
| Rate popup | Disabled: ServicesEnabled is false. | Disabled: RatePromptAllowed is false. | Disabled. |
| **Dev / QA** | | | |
| Mod Menu | Dev-only: a QA panel (Quick Test, Test Mode, categories) from a Settings row and a Pause chip. | — | Not ported. |
| Legacy QA menu (RoyalQaMenu) | Unreachable: its UI is suppressed by the Mod Menu. | — | Not ported. |
| Editor preview / check menus | Dev-only: Tools → Royal Throne (loading preview, 'Build approved homepage', Home checks and captures). | Dev-only: **JellyGarden > Story > Preview** opens the real screens in Play mode. | King Leo's Preview menu, kept. OPEN: do Royal Throne's Home checks come with RoyalHomeScreen? Many need a 'JellyGarden-Codex' folder name. |
| Editor windows | Dev-only: kit level editor, All In 1 VFX Lab, Effects Workshop, King Vanguard Animation Review. | Dev-only: kit level editor (LevelMakerEditor is on main). | Kit level editor kept. King Vanguard review is not ported. OPEN: VFX Lab and Effects Workshop. |
| VFX Lab panel (`-storyvfxlab`) | — | Dev-only: opens level 1 with buttons to compare clear and bomb effects. | Retired with StoryVfxLab (plan §5). |
| Headless review / test harnesses | — | Dev-only: flag-driven runners (`-treasure-review*`, `-storydemo`, `-storyplaytest`, `-storyselftest` and others) with no UI of their own. | Adapted (plan Task 5.1). StoryPowerupTest, StoryExplosionProbe, StorybookBoardReview and the StoryOriginal / Rocket / RoyalBoard validations are retired. |
| VFX / reference preview scenes | Dev-only: Rocket and TNT VFX demo scenes, plus an empty SampleScene. | Dev-only: the same demos plus ReferenceBoosterPreview and ReferenceTntPreview. | Kept, editor only. |
| Kit API test overlay (ApiTestGUI / TestLevelGUI) | Unreachable: inactive in game.unity. | Inactive in game.unity (dev-only). | Inactive. |

OPEN for all legacy variants: does `-legacy-story` keep its own home, popups, HUD and win/lose cards, or get the Royal screens too? The target only decides the Treasure Hunt path.

### Only in Royal Throne
- Royal-blue castle title (Sept 13)
- Twilight castle title (Sept 8)
- Lives info dialog
- Generic Home info dialogs
- Royal Journey level grid
- Not Enough Stars!
- Area chest rewards tooltip
- Area chest ceremony
- Restored area showcase
- New Area! / Coming Soon / All Restored! card
- First-visit area guide
- Level start cover
- Tool selection prompt
- Auto-shuffle 'SHUFFLE!' banner
- Win celebration overlay
- Home reward collection
- Dog star-reward preview
- Story cinematics (proposal)
- Power lessons
- How to Play lessons (King Leo's kit Tutorial pages are unreachable)
- Shop page
- Google Play purchase sheet
- 'Not enough coins' notice
- Tool purchase panel
- Profile page
- Edit Profile card
- Leaderboard
- Team
- Quit Level? confirm
- Mod Menu
- Legacy QA menu
- Royal Throne editor windows (VFX Lab, Effects Workshop, King Vanguard Animation Review)

### Only in King Leo
- First-run auto-launch into Level 1
- Home area-complete / end-of-content state
- Toast messages
- Native repair transitions (vines; door / flags / lanterns)
- Authored repair video
- Repair cutscene on a black screen
- Opening scene (OPEN-01)
- Legacy opening dialogue
- Clue card
- Arc-beat cutscenes
- Sabotage intro
- Task-completion dialogue card / Mimi banner beat
- Reset progress confirmation
- VFX Lab panel
- Headless review / test harnesses
- Reference preview scenes
- All `-legacy-story` variants: Castle First home, legacy dock, legacy Areas list, legacy Area page, Sapphire Trim popup, legacy HUD, legacy Victory / Lose / Pause / Settings

### Notable differences
- **Story vs. restoration loop.** King Leo has a narrative layer: Pip, Leo and Freya scenes, dialogue, native repair transitions, and a 150-area map with only areas 1-2 ready. Royal Throne has no story system. Its payoff is the area loop (task list, build, chest, 'Area Completed!' with King Vanguard, New Area!) over 10 areas, and none of that loop is ported.
- **Economy.** Royal Throne runs on coins: lives, a coin continue, booster / life / tool shops, Google Play purchases and coins paid on a win. King Leo is free: lives are never spent, StoryFreeGame suppresses every shop, ad, rate prompt and the +5-moves offer, and boosters are granted free. The port brings the coin economy into King Leo, which is the biggest change players will feel.
- **End-of-level flow.** King Leo goes Victory → 'Back to Castle' → owed story beat, and quitting from Pause shows the Lose popup saying 'Out of moves'. Royal Throne goes celebration → coin finale → 'Well Done!' → Home reward collection, and quitting goes through 'Quit Level?' straight Home. The port takes Royal Throne's flow, so the hook points for King Leo's story beats have to be rewired.
- **King character.** Royal Throne's King Vanguard Spine rig appears on the HUD, the Home avatar, the fail card, the Area Completed banner and the Leaderboard. King Leo's Treasure Hunt HUD shows no portrait, and Leo appears in dialogue. After the port, StoryAvatar art fills Royal Throne's HUD arch and the fail-card bust.
- **The committed plan contradicts this target.** In `docs/superpowers/plans/2026-10-12-royal-throne-level-port.md`:
  - D4 keeps StoryWinPopup and StoryLosePopup and rules out CommitWin and RoyalEconomy.
  - D5 keeps tools free with the purchase panel off.
  - Task 4.6 keeps Pause restyled by StorybookKitMenus.
  - §2 lists Royal Throne's home, win card and settings as out of scope.

  The plan needs updating before Phase 4 tasks follow it.
