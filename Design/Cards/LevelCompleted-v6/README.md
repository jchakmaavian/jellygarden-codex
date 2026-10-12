# Well Done lettering and royal primary buttons

The user's 6 October 2026 revision asks for the reference's Well Done animation and a more royal finish for the viridian primary button. This builds on v5 without changing the star/pillow choreography or reward accounting.

## Reference observation

Source: `G:/Game/old Backup/JellyGarden-Codex-20261005/Design/RoyalMatchReference/recordings/game completed experience.mp4`, the same SHA-256 recorded in [v5](../LevelCompleted-v5/README.md).

The title is a travelling upward wave, not a short shrink/fade. The first W appears near its final size at 10.500 s, reaches about 1.72 times its settled face height around 10.600 s, dips to about 0.88 at 10.800 s, and returns to rest around 10.900 s. Its bottom remains nearly fixed. Later glyphs overlap this motion; the word space occupies one beat. The final exclamation appears around 10.850 s and settles around 11.20-11.25 s. Native 60-fps title sheets and measured cream-face bounds are in `Reference/`.

The old implementation finished each letter in the same 35 ms that separated its onset from the next letter. That made almost its entire bounce disappear between displayed frames.

## Implementation

- Start lettering 0.18 s after star activation. Stagger characters by 0.04 s and give each glyph a full 0.40 s envelope: 1 to 1.72 over 0.10 s, then 0.88 over 0.20 s, then 1 over 0.10 s. Each glyph scales about its bottom centre.
- Composite complete outlined glyphs in sequence so overlapping pale faces cannot erase their neighbours' borders. The opt-in is limited to this win title; other text retains its normal rendering.
- Preserve the word-space beat even though Unity omits whitespace glyph quads. Give the final exclamation its entire animation before switching to the settled mesh. Motion off shows the complete still title.
- Use ivory faces, a navy inner edge, a gold outer edge and a downward shadow, preserving Royal Throne's font. Lower the title by 22 card units to row 166, reserving room below the crest for its upward growth.
- Share one primary-button finish between `Face` and `CleanFace`: emerald `#18a44e` to `#034522`, a burnished gold bezel with a reversed inner bevel, warm ivory lettering and a narrow warm reflection. This replaces the broad white lens over the former teal face. Bevels scale with button height; secondary and destructive styles retain their existing geometry and colours.

## Review and verification

Watch the [actual Unity card](Royal-Throne-v6-Unity-Card.mp4), [reference versus Unity title animation](Royal-Throne-v6-Reference-vs-Unity-Title.mp4), or [six-stage comparison](Royal-Throne-v6-Title-Wave-Comparison.png). Unity recording preserves the captured real-time cadence (about 20 frames per second, encoded at 30); motion assertions use the separate roughly 60-fps mesh pass. These silent presentation captures retain Royal Throne's own font and artwork. The reference comparison aligns card activation without retiming either animation.

The independent Unity Roslyn compilation and live Unity recompilation passed with zero errors. The [focused Unity verification](Verification/title-buttons-checks.txt) passed 159 checks with zero failures or Unity errors, using a temporary owned QA profile.

- Rendered glyph meshes at 1080x1920, 720x1280 and 1536x2048 confirm upward growth, overlap, undershoot, stable baselines, the word-space beat and the final exclamation's complete envelope. D appeared at 0.387-0.390 s and ! at 0.553-0.557 s in sampled frames. Peak outlined glyphs clear opaque crown pixels by at least 5.67 local units.
- Both primary-button builders produce the same finish at four sizes (62, 82, 116 and 120 high). The actual Continue and Level Start buttons share it. Secondary blue and danger red remain unchanged. The [button board](Verification/TitleButtons/shared-button-board.png) shows all sizes and the retained variants.
- Continue and Close remain topmost raycast targets and each route exactly once to Home. Presentation QA commits no reward. Motion off remains still; reopening reuses the hierarchy.
- Normal save keys, runtime currency/lives/level and the original Home catalog were restored exactly. The editor was returned to clean Edit Mode in main.unity and the owned reservation released.

The editable [browser preview](Preview/preview.html) is an offline rendering; it does not substitute for Unity capture. V5 review media remain preserved. Android/device verification is outside this presentation pass.
