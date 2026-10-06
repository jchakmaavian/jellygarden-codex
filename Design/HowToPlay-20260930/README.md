# How to Play redesign (2026-09-30)

The How to Play card was redesigned. Each lesson is now a small board that plays its lesson on a loop, in the live board's
own art.

## What changed

**The card** now uses the ribbon style of the Level Failed and Quit Level cards:
- "How to Play" on the ribbon under the crown, with the red close;
- one cream panel holding the lesson;
- a row of six page dots, the current one gold;
- BACK and NEXT side by side.
  - BACK rests dimmed on the first lesson.
  - On the last lesson NEXT reads GOT IT and closes the card.
  - The card always opens on the first lesson.

**Six lessons**, each with a title, a board that is always playing, and a two-line rule:

| # | Title | What the board shows |
|---|---|---|
| 1 | MATCH THREE | The hand swaps two pieces; three books line up and clear; the pieces fall and refill |
| 2 | MAKE A ROCKET | A horizontal four becomes a Rocket where the dragged piece landed; a tap fires it both ways along its row |
| 3 | MAKE TNT | A T of five becomes TNT; a tap winds it up, then it clears the 5 x 5 around it, ring by ring |
| 4 | MAKE A PROPELLER | A square of four becomes a Propeller; a tap spins it up, it hits its four neighbours, lifts and flies an arc to the crate in the corner |
| 5 | MAKE A COLOR BALL | Five in a line becomes a Color Ball; swapped with a shield, it fires a beam at every shield one by one, and they all clear together |
| 6 | COMBINE POWER-UPS (new) | A Rocket dragged onto TNT fires three rows and three columns at once |

**Rules:** every step follows the game's code, not the reference game:
- which shape makes which power-up (`CombineManager`);
- where the power-up appears (the dragged piece's cell);
- the 5 x 5 TNT, cleared ring by ring;
- the Propeller hitting its four neighbours, then its target (a blocker first);
- the Color Ball clearing the swapped piece's colour, with beams about 0.12 s apart;
- Rocket + TNT firing three rows and three columns (`RoyalPowerupDirector`).
- One liberty: the Rocket flies at 12 cells a second, where the board uses 15, so the lesson can be read.

**Art:** only the live board's own art, as the board draws it.
- It comes from the imported catalog: the book, shield, crown and leaf; the rocket and its four half-rockets; the TNT, the
  Color Ball and the crate. Each is drawn at its own pixels per cell and pivot.
- The Propeller is assembled from its body, pin and fan, as `ReferenceGameplay/Visuals/Propeller` places them. It spins
  through its own frames: the sharp turn, then the blurred loop, then the blur disc.
- The flashes, stars, rings, glows and beams are the board's additive light sprites. A new UI shader draws them additively:
  `Resources/RoyalLessons/UIAdditive.shader`.
- The hand is the level 1 lesson's hand. The contact shadow and the gold board frame match the board.
- No new raster art was made. The old ruby and helicopter art is no longer used here.

**Names:** the Propeller and the Color Ball are called that everywhere. "MAGIC ORB" became "COLOR BALL" on the Level Start
card and in the power-up guide, and "HELICOPTER" became "PROPELLER" in the lessons.

**Motion:**
- Everything runs on the unscaled clock, so the lessons play from the pause menu while the game is stopped.
- Each loop fades out and starts over. A page starts over when it is shown.
- The layouts were checked by simulation (`sim.py` in the session scratchpad): no board holds a match before its move, the
  move makes exactly the lesson's shape, and none of the eight loops a page repeats leaves a cascade after a refill.

## Files

- New:
  - `Assets/JellyGarden/Scripts/GUI/RoyalLessonDemo.cs`: the lesson pages and their boards;
  - `Assets/JellyGarden/Resources/RoyalLessons/UIAdditive.shader`.
- Changed:
  - `Tutorial.cs`: six pages built by RoyalLessonDemo; GOT IT closes; the card opens on the first lesson;
  - `RoyalCards.cs`: `Lessons` builds the ribbon card, `LessonProgress` sets the dots and buttons, and the guide says COLOR BALL;
  - `RoyalLevelCard.cs`: COLOR BALL;
  - `RoyalMenuPresenter.cs`: six pages;
  - `Editor/RoyalPresentationChecks.cs`: details below.
- **The check changes.** The dialog review finds a title that sits on a ribbon. Its lesson check now tests what the new
  pages must do: the board plays, and keeps clear of the title and rule. The old check tested that static pieces didn't
  cover a static icon.

## Verified (editor, QA profile)

- **The redesign's own driver:** 30 of 30 checks pass, with no runtime errors and the real save unchanged.
  - Every lesson plays and loops: 2.3 to 7.1 s a loop, with no cascades.
  - The board, title and rule sit inside the panel, apart.
  - The dots, BACK and NEXT/GOT IT are right on every page. BACK works, GOT IT closes, and the card reopens on lesson 1.
  - The card fits at 1080x1920, 1440x2960, 720x1280 and 1536x2048.
  - The lesson plays from the pause menu's HELP at timeScale 0.
  - COLOR BALL shows on the Level Start card and in the guide.
- **Frame times, with nothing captured:** the lessons match Home idle.
  - Mean 17.3-18.0 ms, p95 28-32 ms, against idle Home's 17.9 and 29.5.
  - Opening a page costs at most 37-62 ms in its first half second.
  - These are editor numbers, not device measurements.
- **Existing suites:**
  - The card suite (`Design/Cards/cards_test.cs`): 788 of 788 pass, real profile unchanged.
    - Its press check was brought up to the approved press: every control goes to 0.98 and 8% darker, with no sink
      (UI polish, 2026-09-30). The old check wanted a sink and 16%. The check is stricter now: it also measures the
      scale.
    - Its lesson loop covers all six pages.
    - It presses BACK where BACK is live, and checks that it is dimmed and off on the first lesson.
    - The ten warnings are the ribbon titles, the Mod Menu chip and the Quit Level title sitting over the face's edge,
      by design.
  - The dialog review: 19 of 19 pass.
- **Real save:** the same as before these runs.
  - The dialog review turns three rewarded-video day counters when it opens the shops (see the UI polish README). They
    were put back.
- **Evidence:**
  - `screens/`: before, after, each lesson's loop at 0.7 s steps, screen sizes, the pause menu and the names;
  - `recordings/`: each lesson at normal speed.

## Not done

- Not tested on a phone.
