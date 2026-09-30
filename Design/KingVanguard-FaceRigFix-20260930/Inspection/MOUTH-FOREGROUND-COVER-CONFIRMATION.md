# Cell 20 is a foreground mouth surround

The post-mapping renders confirm a separate artwork-semantics problem: `Ağız` was redesigned as a large dark open cavity even though the source attachment reads primarily as a foreground skin/lip surround with a small transparent mouth opening.

Evidence inspected:

- [Original isolated source cell 20](../../KingVanguard-Reference-20260929/FaceRedesign/Portrait/Parts/20.png): orange/flesh shading across the opaque island, with a small transparent slit near the upper middle.
- [Earlier portrait render retaining original facial artwork](../../KingVanguard-Reference-20260929/King_Vanguard-first-render.png): the same broad area reads as flesh/lip shading under the moustache. This is an earlier integration render, not a pristine APK screenshot.
- [Current corrected-mapping idle](../After/idle_loop_2-close.png), [sad](../After/k-1_uzulme-close.png), and [happy](../After/k-1sevinme_6-close.png): the large burgundy slab remains after the motion mapping correction.
- [Source draw order, alpha/color and setup bounds](mouth-shadow-semantics.json): `Dil` and `Alt Diş` draw before `Ağız`; `Ust Diş` and the moustache draw afterward. The foreground opaque portion of `Ağız` covers earlier layers, while the source slit reveals them.

The repaired workboard removed a duplicated painted tongue, but retained the incorrect full-cavity interpretation across the foreground cover. Preserving the source alpha cannot correct that interior RGB meaning. The median visible source color is (171, 79, 46), compared with current (97, 21, 16); this is a large, intentional-looking dark cavity where the reference used flesh/lip shading.

The smallest appropriate repair is **cell 20 only**, shared by portrait and Area:

- Paint warm peach skin and softly shaded lower-lip/chin-surround color, matching the new Vanguard face and cheeks.
- Preserve its original registered outline, narrow opening, UV rectangle and deformation behavior. Keep the opening transparent through the existing source-alpha clipping.
- Do not paint a full open dark mouth, teeth, gums, tongue, moustache or beard into this foreground layer.
- Keep the new identity and texture style; do not restore the source character's orange facial RGB.
- Review the same idle/sad/worry/happy samples before changing tooth positions or repainting further cells.

This corrects the earlier workboard label **OPEN MOUTH INTERIOR**, which encouraged the wrong painting interpretation. A more accurate prompt label is **FOREGROUND MOUTH / LOWER-LIP SKIN SURROUND — separate teeth and tongue behind the opening**. The source name `Ağız` alone was not sufficient to determine the component's visual role.

No artwork, atlas, runtime code or Unity state was changed during this confirmation.
