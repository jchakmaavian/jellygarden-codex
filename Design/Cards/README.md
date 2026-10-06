# Popup cards

Every popup card uses the Level Start card's design (`Design/LevelStart`): the same frame and crest, lettering, panels,
buttons, close control and calm motion. `cards-1080x1920.png` shows all of them as they are in the game (2026-09-27).

Code, in `Assets/JellyGarden/Scripts/GUI/`:

- `RoyalCardKit`: the parts. `Card` (backdrop, frame, crest, title), `Close`, the lettering inks, `Cream` panels, `Tile`,
  goal columns, `Button` (Primary / Secondary), `Price`, badges, and the motion wiring.
- `RoyalCards`: one builder per popup. BoostShop, GemsShop, LiveShop, BoostInfo, Tutorial (and its lesson pages),
  MenuPause and Settings (and the leave question), MenuComplete and MenuFailed, Reward, PreFailed and PrePlay.
- `RoyalCardMotion`: the entrance, press feedback and gloss, shared with the Level Start card.
- `RoyalLevelCard` / `RoyalLevelCardMotion`: the Level Start card (MenuPlay), built from the same kit.
- `RoyalMenuSkin.Apply` calls the builders. `RoyalMenuPresenter` lays out the goals, styles each lesson page the frame it
  is built, and gives PreFailed and PrePlay their calm clips.

Each popup keeps its original objects, names and callbacks; only their look and place change.

## Layout rules

- **Card:** 664 units wide (83% of the 800-unit popup canvas). Its centre sits 33 units below the screen's centre on
  every card; the scene's per-popup root offsets (up to 95 units) are removed. The backdrop dims to 78%.
- **Height fits the content.** The last row ends 52.5 units above the bottom edge, where the small diamond ornament sits.
- **Header:** the crest sits on the top edge. The title goes in the hero lettering, 126 units below the top edge, fitted
  to 560 units. Where a level number belongs (Level Start, the result cards), LEVEL and the number sit on the ribbon.
- **Close:** a 73.5-unit circle with a 96-unit tap area, 44.2 / 34.8 units in from the top-right corner.
- **Content:** goals and lessons sit on cream panels with a gold border. Items (boosters, coin packs) sit on dark-blue
  tiles with a gold rim: square tiles are full squircles, oblong ones use a 56-unit corner.
- **Buttons:** one Primary per card (green face, gold rim, soft shadow, a gloss every 3.4 s) for the main action; every
  other action is Secondary (blue face). Captions are sized from the button height. A button that is not interactable
  (the offline coin packs) is dimmed to 60% and gives no press feedback.
- **Inks** (`RoyalCardKit.Ink`):
  - Hero: titles and big numbers.
  - Heading: gold headings on the blue face. HeadingOnCream: the same on cream.
  - Value: numbers on cream.
  - Body and BodyOnCream: copy.
  - Caption (on green) and CaptionOnBlue: button text.
  - Label: booster names.
  - Accent: status lines such as the next-life timer and the best score.

| Card | Popup | Height | Primary action |
|---|---|---|---|
| Level Start | MenuPlay | 862 | PLAY |
| Booster shop | BoostShop | 780 | each offer (x3 / x5 / x10) |
| Coin shop | GemsShop | 760 (840 with video offers) | each pack |
| Lives | LiveShop | 612 (720 with video offers) | REFILL |
| Booster guide | BoostInfo | 722 | (none; close) |
| How to play | Tutorial | 800 | NEXT |
| Pause | MenuPause | 670 | CONTINUE |
| Settings | Settings | 520 | (none; close) |
| Leave question | over MenuPause | 386 | STAY |
| Level won | MenuComplete | 722 | CONTINUE |
| Level failed | MenuFailed | 764 | TRY AGAIN |
| Reward | Reward | 600 | COLLECT |
| So close | PreFailed | 622 (710 with video offers) | CONTINUE |
| Goals before a level | PrePlay | 430 | (none; timed) |

## Motion

No squash or jelly anywhere. The inherited `Menu.anim` no longer plays; its events (two click ticks at 0.25 / 0.33 s,
`OnFinished` at 0.35 s) still fire at the same times, on the unscaled clock, so a card over the paused game animates too.

- **Entrance:** the backdrop dims from 0; the card scales 0.96 to 1 and fades in over 0.22 s (ease-out cubic, no
  overshoot). The content follows in up to four bands from the top, 0.05 s apart (0.20 s each, 8-unit rise). Everything
  is settled by 0.45 s.
- **Press:** Primary buttons darken to 86% and sink 2 units. Secondary buttons, the close control and ? darken to 84% and
  shrink to 0.98. Both take 0.06 s in and 0.10 s out.
- **Win card stars:** each star fades in and settles from 0.9 to 1 in 0.22 s, as `AnimationManager` shows it.
- **Clips:** PreFailed and PrePlay keep their own clips, which carry their events.
  - PreFailed holds at `WaitForGiveUp` until the player chooses, then fades out, with its backdrop, as soon as the
    choice is made; `OnFinished` stays at 3.03 s.
  - PrePlay enters the same way, holds, and fades out from 1.70 s.
  - The legacy clips' curves are built by `RoyalMenuPresenter.CalmCard`.

## What the game does at run time

The layouts account for these:

- **Video offers:** without an ads SDK (`UNITY_ADS` / `GOOGLE_MOBILE_ADS`), `AnimationManager` hides the rewarded-video
  buttons on every open, so the shops and the so-close card leave no room for them. In a build with an SDK they get a
  row (the heights in brackets).
- **SHARE** shows only with online services (`GUIEvents`); otherwise CONTINUE stays centred.
- **The win card** has no goals group, and its best score is switched off.
- **Offline:** the coin packs read "Unavailable" and are dimmed.
- **The goals card before a level** lives on the board's 720-unit canvas; its clip scales it by 720 / 800 to match the
  other cards on screen.
- **The Mod Menu's development entry** hangs just below the Settings and Pause cards (`RoyalModMenu`). The leave
  question moves in front of it when shown.

## Checks

`cards_test.cs` and `cards_sizes.cs` are editor eval scripts. They are not compiled with the project. Run them in Play
Mode on the map, for example `unity command eval_file --file Design/Cards/cards_test.cs`. Both use the QA test profile and
check that the real profile is unchanged.

- `cards_test.cs` covers the following, then plays level 23 (the goals card, Pause at timeScale 0 with How to play over
  it, the leave question) and leaves:
  - every card's entrance: no overshoot or squash, settles at rest, the old clip idle;
  - text fit and placement inside the face;
  - controls: each takes its own tap, stays on screen, and no two overlap;
  - press feedback;
  - the dimmed offline packs;
  - the lesson pages;
  - the so-close hold and exit;
  - the real close paths.

  Results go to `Temp/RoyalCards/run/log.txt`; the SUMMARY line gives the failures. The only expected warnings are the
  Mod Menu entry's two.
- `cards_sizes.cs` opens every card at the existing 1536x2048, 1440x2960 and 720x1280 Game view presets and checks the
  crest, card and controls stay on screen. It restores the 1080x1920 preset. Results go to `Temp/RoyalCards/sizes/log.txt`.

2026-09-27 results:
- `cards_test.cs`: 771 checks, 0 failures, no console errors.
- `cards_sizes.cs`: all sizes pass.
- The dialog review suite (`RoyalPresentationChecks`): 18/18.
- The Level Start card's own interaction test: unchanged.
