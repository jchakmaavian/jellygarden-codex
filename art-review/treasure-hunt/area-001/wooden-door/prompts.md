# Area 001 wooden-door art revision — 2026-09-12

Generated and edited with the built-in `image_gen` tool. No CLI/API fallback or pixel post-processing. The selected masters replace runtime states 2–4; the previous arch-only masters are retained in `previous-art/`. State 0, state 1, and Pip are unchanged.

## State 2

Use case: precise-object-edit. Asset type: production repair-state background for a bright cartoon mobile castle game. Input image 1 is the EDIT TARGET (the gates after clearing the vines). Create state 2 for the task 'Build the wooden door'. Add a beautifully built CLOSED arched double wooden castle door filling the central archway from its curved underside down to the threshold. Warm honey oak vertical planks, rounded substantial frame, two elegant brass horizontal hinge straps per leaf, a neat center seam and two large gold ring handles. It must unmistakably read as an actual installed wooden door, not a beam or empty frame. Keep the existing wooden side brackets. Door is inset inside the stone arch and covers the distant castle and fountain within that opening only. Preserve the smiling gold sun ABOVE the arch completely. Keep everything outside the opening precisely unchanged: cream stone towers and arch, blue roofs, empty front banner poles, unlit lanterns, cart, cupboard, flowers, path, sky, sunlight, camera, framing, object positions and image dimensions. No front flags yet, no climbing vines, no lantern light, no new props, no lettering, no people. Retain the exact cheerful polished 3D storybook art and composition. Single full portrait image at the same aspect ratio as input, not a comparison or a sheet.

## State 3

Use case: precise-object-edit. Asset type: production repair state 3 background for a bright cartoon castle mobile game. Image 1 is the EDIT TARGET with the newly installed closed oak double door. Image 2 is a SUPPORTING STYLE REFERENCE FOR THE TWO BLUE FLAGS ONLY; do not copy its open passage or wooden top beam. On image 1, add exactly two royal-blue hanging pennant banners, one on each existing empty front brass pole. Copy the attractive gold-bordered, gold sun-emblem, forked-tail banner design from image 2. The poles must remain at image 1's exact current positions; hang the banners downward with hems just above the lanterns, not covering lamps. Preserve image 1's entire closed wooden double door, arch frame, gold straps, handles and threshold EXACTLY. Preserve every other feature in image 1: camera, cream towers, blue roofs, sun medallion, unlit lamps, cupboard, cart, flowers, paving, sky, lighting, dimensions and composition. Only the two cloth flags are new. No vines, no lantern glow yet, no text, no characters. Same polished cheerful storybook style. Single full portrait image, not a comparison or sheet.

## State 4

undefined

## Inputs and outputs

- State 2 edits `area_001_state1.png`; generated master: `exec-8801fbee-3621-470a-a362-5ab28c65b263.png`.
- State 3 edits the new state 2 and uses the previous state 3 solely as the flag design reference; generated master: `exec-14a77a8c-9b1b-4b4e-a7d5-ac92e6f2c41d.png`.
- State 4 edits the new state 3; its generated master is recorded in `assets.json`.

Exports: `Assets/JellyGarden/Resources/Story/TreasureHunt/Art/area_001_state2.png`, `area_001_state3.png`, and `area_001_state4.png`. Retained full-resolution PNGs are also the Unity import sources, with existing sprite GUIDs preserved.

The door icon is a Unity sprite crop of state 2, authored at `[0.31, 0.335, 0.365, 0.31]` in normalized bottom-left coordinates. Its repair effect covers `[0.30, 0.335, 0.39, 0.31]` so the star and sparkles reach the complete doorway.
