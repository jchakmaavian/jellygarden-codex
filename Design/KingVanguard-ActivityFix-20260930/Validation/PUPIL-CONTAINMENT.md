# HUD pupil containment correction

The portrait/Area face modifier now uses **1.1× pupil geometry**, replacing 1.55×. The default in `KingVanguardFaceShapeModifier.cs`, serialized `KingVanguard_FaceShape.asset`, and `Tools/ImportVanguard.cs` agree. The importer explicitly applies 1.1 before clearing and reloading skeleton data, so repeat imports retain the correction.

Only iris/pupil attachment geometry changes. Eye-white geometry, source gaze/bone motion, facial deformation keys, activity scheduling, source skeleton binaries, texture artwork, and native full-body geometry are untouched by this change.

## Before/after evidence

The project's official Spine 3.8 runtime loaded the original King and Area binaries/atlases in an offline process. Both candidate scales were sampled at 60 Hz, including the exact final animation time. Pupil and eye-white vertices were compared in the animated `on_yuz` coordinate frame. Positive horizontal overrun means part of the pupil quad extends past an eye-white mesh's left or right extent. Negative values mean a remaining margin.

| Live activity set | Eye poses per scale | Worst horizontal overrun at 1.55× | Worst horizontal overrun at 1.1× |
| --- | ---: | ---: | ---: |
| Portrait idle and reactions | 6,872 | +3.3669 source units | −0.2556 source units |
| Area idle and clap tracks | 1,442 | +1.2877 source units | −2.3348 source units |

All 8,314 live eye poses at 1.1× have pupil quads within eye-white horizontal bounds. The tightest portrait case is the right eye in `idle_loop_1`; the sampled horizontal limit is approximately 1.132×, so 1.1× retains a conservative margin. This correction removes the added sideways overflow introduced by the earlier enlargement.

Portrait clips: `idle_loop_1`, `idle_loop_2`, `idle_loop_3`, `k-1sevinme_6`, `Happy_2`, `Heyecan_1`, `Relax_1`, `k-1_sevinme_NEW`, `k-1_kaygi`, `k-1_kaygi_loop`, `k-1_uzulme`.

Area clips: `king_drill_loop`, `king_drill_win_clap_start`, `king_drill_win_clap_loop`, `king_drill_win_clap_finish`, and additional review track `king_drill_win_clap_look_loop`.

Per-eye, per-track evidence is recorded in `pupil-containment-live-tracks.json`. The broader scan of all 42 tracks is in `pupil-containment-all-tracks.json`.

## Bounds of this verification

This is a geometric horizontal-containment check, not a pixel-compositing proof of the complete eye. Regional transparent pixels and later-drawn cheeks/lids affect the final visible silhouette. Vertical bounding-box overrun is already present in the original unmodified source at downward-looking poses: right-eye `Heyecan_1` at 0.333333 s has 2.1716 source units; `idle_loop_1` at 3.566667 s has 0.4327; `idle_loop_2` at 3.583333 s has 0.2271. At 1.1×, the largest live portrait vertical quad overrun is 3.4188 units in `Heyecan_1`; the Area idle/clap set has no vertical quad overrun (minimum margin 3.0706 units). Final visible pupil/eyelid/cheek overlap still requires the parent's Unity artwork review. Do not report the numeric bounds check as proof of all-pixel containment.

The unused extreme-gaze tracks `k-1_idle_3`, `k-1 animation`, `k-2`, and legacy `king_area 1` retain some horizontal quad overrun at 1.1×. They are included in the broader evidence and are not enabled by the HUD activity controller or Area clap sequence. This fix does not alter their authored gaze motion.

## Validation and import

The updated modifier compiles against the actual Unity and Spine reference assemblies. No Unity/editor process was invoked for this correction. Refresh and rerun `Tools/ImportVanguard.cs` to apply the serialized change to loaded skeletons before rendering. Earlier face-adaptation reports describing 1.55× pupils are superseded by this report for the HUD/Area pupil setting.
