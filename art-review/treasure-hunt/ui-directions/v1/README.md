# Whole-game UI — three visual directions

Created as previews on 2026-09-12. The user subsequently selected Royal Storybook and approved implementation. The original comparison boards remain unchanged; see the [Unity integration review](../../royal-storybook-runtime/README.md) for the implemented screens.

Each board shows four portrait screens in order: Home, Area Tasks, Before Level, Level Complete. The same Castle Gates setting and Level 3 example make the different styles comparable.

[Open the HTML comparison gallery](index.html). Filter by style or screen, enlarge a preview, switch styles with the arrow controls, and toggle between fit-to-window and actual size. The page runs locally without a server or external dependencies; keep the three PNGs beside `index.html`. Screen-specific views use CSS windows onto the original boards without modifying their pixels.

1. [Royal Storybook](01-royal-storybook.png): warm ivory panels, royal-blue ribbons and buttons, restrained gold edging, folded page corners.
2. [Pip's Treasure Map](02-pips-treasure-map.png): folded map sheets, turquoise paper buttons, route markers, compass motifs, and six empty key spaces.
3. [Royal Festival](03-royal-festival.png): colourful pennants, sculpted blue controls, gold medallions, and celebratory ribbons.

All three were generated with the built-in imagegen tool using the existing Castle Gates state-2 painting and Pip portrait as visual references. [Exact generation prompts](prompts.md).

Visual review: each board contains all four complete screens; the main labels, 2/4 task progress, one-star task costs, Level 3's 500-point goal, and one-star victory reward are readable. These are static art-direction mockups. Illustrated board pieces, icon details, decorative text, and material treatments are proposals, not implementation assets or gameplay changes.

Other screens discussed in the design ideas (board HUD, map, dialogue, settings, retry, and area completion) can follow the selected direction after review. They are not separately illustrated in this first comparison.
