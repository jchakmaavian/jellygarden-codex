# Royal Match, as captured

Eleven screens pulled off a real device (iPhone 6s Plus, iOS 15.8.5) over USB.
These are the reference for the map, area page and level page work in
[PLAN.md](../../PLAN.md). Everything below is what the screenshots show, not
what anyone remembers.

## How they were captured

`mobilecli` could not start its own agent, so `mobile_*` screenshots and taps
both failed. WebDriverAgent was in fact already running on the device and
answering on port 8100 — it only needed the USB port forwarded:

```bash
iproxy 8100 8100 -u <udid> &
curl -s http://127.0.0.1:8100/status
```

From there the phone is drivable over WDA's HTTP API directly. Two things cost
time and are worth writing down:

- `POST /session` **with** a `bundleId` relaunches the app and loses your place;
  **without** one it lands on SpringBoard. Make one session, keep the id, reuse it.
- This WDA build has no `/wda/tap/0` route — it 404s. Use W3C
  `POST /session/{id}/actions` with pointer events.

Screenshots come from `idevicescreenshot`, which never needed WDA at all.

## The screens

| file | what it shows |
|---|---|
| `01-home-area.jpg` | Home: the room being restored, top bar, area button, 5-tab nav |
| `02-area-tasks.jpg` | The area's task list — **"Do It ⭐1"** per task |
| `03-repair-in-place.jpg` | A repair playing **on the room itself**, gold sparkle sweep |
| `04-areas-list.jpg` | Areas: a **vertical list of room cards**, each with a progress bar |
| `05-earn-stars.jpg` | "Beat levels to earn stars" |
| `06-story-beat-popup.jpg` | A story beat: one illustration, reward, **Play / Skip** |
| `07-event-level-story-above-board.jpg` | Event level — **story art above the board** |
| `08-event-board.jpg` | That board in play |
| `09-home-level-button.jpg` | Home again, the green button now reads **"Level 16"** |
| `10-level-start-popup.jpg` | Level popup: **Goal** icons + counts, **Select Boosters**, Play |
| `11-level-hud.jpg` | In-level HUD: **Target · king portrait · Moves**. Nothing else. |

## What actually differs from the kit

**There is no score bar and no star rating in a level.** The HUD is three
panels: the target with icon counts, the king's portrait, and a moves number.
No progress bar creeping toward three stars — that idea does not exist here.
A level is passed or it is not.

**A star is a currency, not a grade.** Beat a level, get one star; spend stars
on repairs at ⭐1 or ⭐2 each. That is precisely what `StoryFlow.HandleWin` and
`StoryProgress` already do, and it is why the kit's three-star scoring had to be
collapsed rather than reskinned.

**The map is a list, not a path.** Areas is a vertical scroll of cards, each
showing the room's art and an `n/7` bar. The kit's winding candy path with a
node per level has no counterpart — Royal Match never shows a level map at all,
because the only level you can play is the next one, from a button that names it.

**Repairs animate on the room.** No separate cutscene screen: the panel closes
and the object sweeps in over the room with a gold shimmer. The story's
`RepairCutscene` already works this way, which is the one piece of this that
needs no change.

**Story beats are one illustration plus Play / Skip.** Not a dialogue sequence.

**The level start popup carries no moves count** — only the goal and the
boosters. Moves appear once you are on the board.
