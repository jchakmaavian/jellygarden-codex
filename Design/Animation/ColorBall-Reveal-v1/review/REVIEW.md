# Color Ball revision — 20 September 2026

Reference found at the supplied project path with the additional `animation-VFX` subfolder. Inspected consecutive distinct reveal frames 43–99 and activation frames 469–527, plus charged targeting and cleanup samples. Reviewed all three pre-existing contact sheets and the beam, target-glow and target-impact clips.

The source video has a small banded birth, a bright reveal swell, a flattened sideways spin, a static idle, and a fast activation followed by a long charged state. This revision follows the user's more specific 3-cell disc / 1-cell idle direction and avoids the old round white reveal swell. It is not a frame-identical copy of the reference.

## Changes

- New active renderer uses horizontal shutter samples of the original dot cutout. It does not use the previous rig's generated back-face dots or recolored decals. Exact original pixels return at rest.
- Reveal: tiny flattened birth above the target, prominent 3-cell disc, deceleration, short authored descent and clean return to the 1-cell sphere. One second, no bounce.
- Idle: completely static, preserving individual dots and silhouette.
- Boosting: smooth 0.30-second spin-up, 1-second repeatable charged section, then 0.50-second finish. Dark-blue body remains visible beneath saturated bands; compact white core, soft halo and small stars.
- Separate beam, contact, charged-target glow and impact assets are retained. The new separate-beam preview demonstrates target conversion and the handoff point without baking targets or Rocket exhaust into the Color Ball frames.

## Validation

288 rendered source frames: Reveal 60, Idle 120, Boosting 108. Each is 512×512 with transparent edges and a centered fixed pivot. All WebPs decode exactly to their PNG masters, including alpha. Maximum alpha in the outer two-pixel border is zero.

Reveal-to-Idle and Idle-to-Boosting handoffs are pixel-identical. Every Idle frame is identical. The charged-loop seam difference is 3.438 versus a median adjacent step of 3.401 and largest step of 3.509; its wrap is an ordinary motion step. The last finish frame is fully transparent. Measured saturated disc width is 2.973 cells across all six sampled spin phases; the idle alpha footprint is 112×113 pixels at 112 pixels per cell.

Previews and numbered contact sheets were regenerated. Checked the combined state sequence and separate-beam review frames against the reference and original artwork. C# authoring code compiled and rendered successfully. These are animation-source checks, not a Unity Play Mode integration test. No live gameplay scripts or unrelated booster assets changed.

`quality-checks.json` holds numeric results. `AnimationContract.json` in each group defines timing and scale for integration. Renderers must keep the full padded texture at 4.5714 cells wide; the ball itself is 1 or 3 cells depending on state. Do not independently fit each texture to a single cell or trim its transparent border.

Local `masters`, `samples` and `before` directories are working/review material. The source, lossless final frames, ZIP packages, previews and contracts are the handoff deliverables.
