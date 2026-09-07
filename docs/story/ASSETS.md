# GENERATED ASSETS

Everything in `game/unity/Assets/Resources/` was generated, not drawn or recorded
by hand. This file records what exists, how it was made, and what still needs a
human decision.

---

## ART — Magnific

Style brief used across every prompt: *painterly storybook illustration, mobile
game art, clean silhouette, no text.* Cost was ~75 credits per image.

| File | What it is |
|---|---|
| `Art/castle_reveal.jpg` | The courtyard Leo rides into. Used in the opening. |
| `Art/gate_state0_ruined.jpg` | Gate choked with ivy, rotted timbers, no banner, dark lanterns |
| `Art/gate_state1_ivycut.jpg` | Ivy cut away, bare stone |
| `Art/gate_state2_timbers.jpg` | New oak gate |
| `Art/gate_state3_banner.jpg` | Crimson banner hung — first strong colour |
| `Art/gate_state4_lanterns.jpg` | Lanterns lit — first warm light |
| `Art/mimi.jpg` | The castle cat |
| `Art/clue_logbook.jpg` | The gatekeeper's logbook, for the clue card |
| `Art/leo.jpg` | King Leo. Used at the "...Freya?" beat in the opening. |
| `Art/block_vine.png` | Ivy blocker on the board |
| `Art/block_timber.png` | Rotted timber blocker |
| `Art/block_lantern_off.png` / `_on.png` | Lantern, unlit and lit |
| `Art/powerup_rocket.png` | Rocket — board special and booster icon |
| `Art/powerup_bomb.png` | Bomb — board special and booster icon |
| `Art/powerup_hammer.png` | Hammer booster |
| `Art/powerup_shuffle.png` | Shuffle booster |
| `Art/ui_star.png`, `ui_bg.jpg` | Star reward, warm castle backdrop |
| `Art/tile_*.png` | Five match-3 gems, background removed (crown, ruby, sapphire, emerald, amethyst) |

**The gate states are the important ones.** Each state is generated from the
state before it, so the composition, stonework and lighting stay consistent while
only the newly repaired element changes. They read as one gate being fixed, not
five different gates.

> **Chain the references, do not fan them out.** The first attempt generated
> every state from state 0 — the ruin. That makes each prompt argue with its own
> reference, and the reference sometimes wins: the banner state came back with
> the *broken* gate restored underneath it, silently undoing the repair the
> player had already paid a star for. Referencing the immediately previous state
> removes the argument, because only one thing is being asked to change.
>
> **The parked Area 002 courtyard states have this same flaw** — all five were
> generated against the ruined courtyard. Check each one for regressed repairs
> before using them, and regenerate any that lost earlier work.

The progression is deliberately **decay → structure → identity → life**, which is
why the banner (colour) and the lanterns (warmth) come last.

---

## VOICE — ElevenLabs

Alfred's narration. Five lines, all from `arc-1-the-missing-queen/00-OPENING.md`.

| File | Line |
|---|---|
| `Audio/VO/alfred_open_01.mp3` | "The King had been gone eleven weeks." |
| `Audio/VO/alfred_open_02.mp3` | "He had left the kingdom in good hands. So he was told." |
| `Audio/VO/alfred_open_03.mp3` | "He expected trumpets. He got weeds." |
| `Audio/VO/alfred_open_04.mp3` | "No one had thought to tell him. That, in itself, was the answer." |
| `Audio/VO/alfred_clue_001.mp3` | "He never finished the sentence. He never got the chance." |

> **Open decision — Alfred is not cast yet.** The ElevenLabs API key in use is
> missing the `voices_read` permission, so neither listing voices nor requesting
> one by id works. Every line above was rendered with the account's *default*
> voice, whatever that currently is. To actually cast Alfred — dry, fond, a
> servant who has known Leo since he was small — add `voices_read` to the key,
> then regenerate. The wiring does not change; only the files are replaced.

---

## SOUND EFFECTS — ElevenLabs

| File | Fires when |
|---|---|
| `Audio/SFX/match_pop.mp3` | Any match clears. Pitch rises across a cascade chain. |
| `Audio/SFX/vine_cut.mp3` | A vine is destroyed |
| `Audio/SFX/timber_break.mp3` | A timber is destroyed |
| `Audio/SFX/lantern_light.mp3` | A lantern lights; also the final repair of an area |
| `Audio/SFX/star_earned.mp3` | Level won |
| `Audio/SFX/repair_done.mp3` | A repair task completes |
| `Audio/SFX/booster_hammer.mp3` | Hammer booster used |
| `Audio/SFX/invalid.mp3` | Rejected swap, or a booster you do not have |

---

## MUSIC — ElevenLabs

| File | Where |
|---|---|
| `Audio/Music/theme_ruin.mp3` | Map, area screen, and the opening. Sparse cello, distant harp, unresolved. |
| `Audio/Music/theme_level.mp3` | During levels. Light lute and strings, unobtrusive. |

Both are 45s instrumental beds. They cross-fade on screen change, and duck under
Alfred's narration rather than fighting it.

---

## CUTOUTS WITHOUT CREDITS

The board icons are generated on a flat backdrop, so `game/tools/cutout.py`
flood-fills inward from the border to remove it locally — no round trip to a
background-removal service, and no credits spent.

```bash
python3 game/tools/cutout.py in.jpg out.png [tolerance]
```

---

## PARKED — AREA 002

`game/_area002_parked/` holds a full set of Outer Courtyard art (six progressive
courtyard states plus the chiselled statue base for the clue, and a rubble
blocker icon) generated during a misread of scope. It is paid for and correct;
move it into `Resources/Art` when Area 002 is built.

---

## IMPORT

`Assets/Scripts/Editor/AssetImportRules.cs` sets the import settings on the way
in — textures under `Resources/Art` become Sprites, music streams, effects
decompress on load. Drop a new file in the right folder and it is configured
correctly without anyone touching the inspector.

`Assets/Scripts/Game/Assets.cs` is the only place asset paths are written down.
A missing file logs one warning and the game falls back to flat colour rather
than rendering blank.

---

## BEFORE YOU PUBLISH

Both services' commercial-use terms need checking against a free public release.
The game earns nothing, but a store listing is still distribution, and the terms
turn on distribution rather than revenue. This is the one item here that cannot
be settled in code.
