# Area 1 tasks 2–4 — transition previews

**Approved and implemented in Unity for Area 1.** These are the original approval previews; see [in-game validation](../remaining-task-runtime/validation.md) for the native implementation. The already-approved vine transition remains unchanged.

[Watch all three in order](area-1-three-transitions-preview.mp4) · [Interactive gallery](index.html)

- [Build the wooden door](area-1-door-preview.mp4): the frame seats, eight oak sections rise into place, then brass hinges and handles settle with small glints. State 1 → 2; progress 1/4 → 2/4.
- [Decorate with royal flags](area-1-flags-preview.mp4): mounts seat and banners unfurl in sequence, ripple gently, then settle. State 2 → 3; progress 2/4 → 3/4.
- [Light the lanterns](area-1-lanterns-preview.mp4): left and right lamps kindle in sequence with local warm blooms, followed by a brief glint on the smiling sun. State 3 → 4; progress 3/4 → 4/4.

Each silent clip is 6.6 seconds at 720 × 1280 / 30 fps. The combined video is 19.8 seconds. The camera stays fixed. Endpoints use the current room art. Task cards are standalone preview controls, matching the earlier vine study; no gameplay screens or dialogue were modified.

`preview.template.html` contains the canvas motion. `package-preview.py` embeds the four existing room images into the self-contained `index.html`. Temporary wood and flag layers are composed inside the preview canvas; original artwork is unchanged. `render-preview.cjs` exports deterministic frames through a dedicated local Chrome instance on port 9231.

Visual review covered building, unfurling, ignition and completed states. Browser checks passed for play, pause, seek, resume and end-of-clip handling on all three previews, plus desktop/mobile layout. Video exports and replay endpoints produced no browser errors. See [render validation](validation-video.json) and [control validation](validation-controls.json).

At the preview stage, the existing game diff and 763 source/resource files were checked against the pre-preview snapshot and remained unchanged. Following approval, `StoryGateTransition` implements this motion using an offline atlas baked from the preview's temporary layers. Original room images and these approval videos remain unchanged.
