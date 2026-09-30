# Level Start card (MenuPlay)

`reference.png` is the approved design for the Level Start popup (2026-09-27, level 23). The card in the game is
`RoyalLevelCard` (layout) plus `RoyalLevelCardMotion` (entrance, highlights, press feedback), in
`Assets/JellyGarden/Scripts/GUI/`. `RoyalMenuSkin.LevelCard` hands the card to them. The card is built from the shared
parts in `RoyalCardKit`, which every other popup now uses too (see `Design/Cards`).

## Layout

Reference pixels map to card units at 0.735 around the reference card's centre (533.5, 751.5), so the card is
664 x 862 units: 83% of the 800-unit canvas width, as in the reference. The constants at the top of
`RoyalLevelCard` (card, goals, boosters, PLAY) are those measurements.

Everything is drawn in code with `RoyalCardShape` (anti-aliased rounded or squircle panels) except one sprite:

- `Assets/JellyGarden/Resources/RoyalGameplay/level-card-crest.png`: the crown, scroll ornament, raised tab and ribbon
  ends, cut from `reference.png` by `extract_crest.py` (`python extract_crest.py reference.png level-card-crest.png`).
  The card face is transparent in it, so the face, the number's halo and the live text are the game's own. The sprite's
  ends overlap the straight top border and fade into it. Every card uses it. To replace it with final art, keep the same
  frame (605 x 290 px, crown axis 302 px from the left, card top border 131 px from the top) or adjust the crest line in
  `RoyalCardKit.Surface`.
- The goals and boosters use the game's own art: `RoyalVisualCatalog.Goal` per level goal, and the board's current
  Color Ball, Rocket and TNT (`RoyalPieceSkin.Art("orb" / "rocket" / "tnt")`, which resolve through
  `ReferenceGameplayCatalog` like the board does). The older menu art `Resources/RoyalGameplay/orb.webp` and
  `rocket.webp` was removed on 2026-09-27; with the imported art switched off, menus fall back to the built-in
  `RoyalGameplayV4` pack.

## Motion

- Entrance: dim 0 to 78%; card scale 0.96 to 1 and fade in, 0.22 s ease-out cubic, no overshoot. LEVEL, goals,
  boosters and PLAY follow 0.05 s apart (0.20 s each, 8-unit rise); all settled by 0.40 s. The inherited clip's
  squash is gone; its events (two click ticks at 0.25 / 0.33 s, `OnFinished` at 0.35 s, which resets the boosters)
  are still fired at the same times.
- Goal icons brighten once to 1.1x and back, 0.05 s apart (`Hidden/RoyalThrone/UIAdditive`).
- Boosters are still. A faint glint crosses the row every 6.5 s; the badges pulse 1.00 to 1.06 (0.9 s, every 3.2 s)
  only while nothing is chosen. A tap brightens the card's gold and lifts the art 1.00 to 1.04 in 0.18 s; a chosen
  booster keeps a soft gold rim and glow, and its badge turns into the tick.
- PLAY: a gloss crosses it every 3.4 s, and once more, brighter, just after a booster is chosen. Pressed: 86%
  brightness and 2 units down (0.06 s in, 0.10 s out). x and ?: 84% brightness and 0.98 scale. The ? has an
  80 x 68-unit tap area, clear of the TNT card below it.
- The `button_controller` Animators on Close, Play and Boost1-3 are switched off for this card only (they squashed the
  controls and pinned their sizes); the Buttons keep their callbacks.
