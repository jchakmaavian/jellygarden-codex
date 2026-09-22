# Color Ball impact proof — awaiting visual approval

This is the short charged-spin → discharge → contact proof. Further final-release/full-combo work is paused for the requested visual approval.

Changes:
- Continuous shared-pivot tremble, varying depth and band thickness; charged artwork has a safe deformation margin.
- Moving white inner edges, gold falloff and short outward streaks, driven by the same local motion clock and discharge pulse.
- Beam front follows actual travel progress. Warm shoulders use local alpha blending so the blue board cannot wash them white; the additive white core, width, targeting, overlap and travel timing are preserved.
- Contact briefly expands into a compact warm collar before fading. Existing conversion events and approved audio cues remain in use.

The three-panel comparison uses the original `Colorball+finishing up.mp4`, preserved V5.1 gameplay, and the revised live capture. All panels are at 1× and approximately 72 pixels per cell. No exposure boost or speed manipulation. Audio is the revised Unity capture only.

Focused final checks: actual mean 29.99/60.05 FPS, seven unique conversions per short run, fixed logical anchor, attached beam origin, unchanged charge timing, pooled reuse/cancellation, no remaining local VFX/audio and no new console errors. Unity compiled. The fixture restores the player profile and exits Play Mode; it deliberately stops before Rocket handoff. The initial repeated-fixture reset failure was corrected using the existing level reset before each short run.

Remaining: the procedural bands and aura are still more regular than the reference's irregular luminous smear. This proof does not establish that the result is superior, nor validate the full ending. Judge the normal-speed moving comparison before extending the changes.

Changed runtime/editor files: `ColorBallV5Presentation.cs`, `ColorBallSphere.shader`, `ColorBallLight.shader`, `Connected beam glow.mat`, `ColorBallVfxV4.cs`, `ColorBallV5Builder.cs`, `ColorBallV5Checks.cs`. No Rocket, TNT, Propeller, UI, targeting or game-rule files were edited.

V5.1 checkpoint: `colorball-impact-work/V5.1-before-impact-proof.zip` with SHA-256 manifest, outside the Unity project. Existing edits were retained. Baseline project commit: `173c198`.
