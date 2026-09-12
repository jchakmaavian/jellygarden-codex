# Curved victory headline

“Level Complete!” is now a transparent painted sprite that follows the blue
ribbon's arch. It replaces the straight Unity Text component and scales with
the ribbon while preserving its aspect ratio. The level number and reward copy
remain dynamic; the star animation and Continue behavior are unchanged.

- Production sprite: `Assets/JellyGarden/Resources/Story/Art/win_victory_headline.png`.
- Prepared master: `tools/art/win/victory_headline.png` (1950 × 329, RGBA).
- Built-in imagegen prompts: [headline-prompts.md](../../tools/art/win/reference/headline-prompts.md).
- Import: `python3 tools/win_art.py`.

The generated source contained a neutral checkerboard preview rather than alpha.
`tools/win_headline.swift` removes that matte and trims transparent padding;
the original generated source is preserved beside its prompts.

Validation: C# typecheck has zero errors; Unity victory tests pass for the
headline image, replay, reward isolation, duplicate Continue, reopening, and
star animation while paused. The native release build has zero errors (381
existing project warnings). Animation/VFX and launch guards pass.

Native captures use an isolated Unity project and separate save profile.
First-win previews at 393 × 852, 329 × 646 and 768 × 1024 show the headline
inside the ribbon, clear of the crown and gold trim. The 393 × 852 replay
preview uses the same image; the native duplicate-Continue check also passes.

[Phone preview](first-win-393x852.png) · [Small phone](first-win-329x646.png) ·
[Tablet](first-win-768x1024.png) · [Replay](replay-393x852.png)
