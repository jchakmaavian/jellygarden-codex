# Area King standalone presentation QA

Reviewed both rendered `AreaCompleted_Vanguard` contact sheets, their 13-sample-per-clip framing manifest, the original parsed rfv387 skeleton, and the installed binary. The installed binary is identical to the source (`cf1ec7671e91b0496ef104f6f0d619f043139c952d46af5066ba12dfab699012`). No Unity session was launched during this analysis.

## Stable celebration framing

The imported asset scale is `0.01`. The following bounds convert the runtime contact-sheet measurements back to source Spine units:

| Track | X | Y | Width | Height |
|---|---:|---:|---:|---:|
| `king_drill_loop` | -548.166 | -35.921 | 1080.507 | 1197.862 |
| `king_drill_win_clap_loop` | -402.953 | -36.189 | 846.162 | 1188.115 |
| `king_drill_win_clap_look_loop` | -369.614 | -34.904 | 794.483 | 1187.691 |

Recommended persistent wrapper focus: **`new Rect(-565, -55, 1130, 1240)`** when the thumbs-up idle and both clap loops share a slot. For clap-only presentation, **`new Rect(-450, -55, 920, 1240)`** gives a tighter frame. These include margins around measured bounds; final UI mask/canvas framing still needs its normal in-game visual check.

The clap start/finish tracks deliberately translate the `beden` body bone roughly 1,535 source units down from the stable pose. The full-trajectory contact sheets therefore display a smaller figure. Keep a stable UI focus and treat these tracks as entrance/exit, or start the persistent presentation directly on `king_drill_win_clap_loop`. Do not enlarge the persistent focus to the union of the entrance/exit trajectory. `king_area_3`, `king_area_4`, and `king_drill_win` similarly contain large source horizontal body translations.

## Floating sleeves and duplicate arms in two legacy tracks

`king_area 1` and `king_area_2` are not self-contained when sampled from the binary's default setup pose. That setup has three old arm variants attached simultaneously. These two tracks do not key off these nine visible setup attachments:

- `sol_kol_1`, `sol_kol_bilek_1`, `sag_kol_1`, `sag_kol_bilek_1`, `sol_el_1`, `sag_el_1`
- `sol_kol_bilek_2`, `sag_kol_2`, `sag_kol_bilek_2`

They hide `sol_kol_2`, then swap the second/third hand variants later in the clip. Unused sleeve/arm variants therefore remain visibly attached while the body moves, matching the floating sleeves and duplicate hands in the rendered sheets. The stable drill/clap loops explicitly hide the old variants at time zero and render cleanly.

This is evidence about the packaged track/setup combination. It does **not** establish a bug in the original Royal Match game: its surrounding code may initialize those attachments or may never select these legacy tracks. There is no alternate Spine skin in the asset (`default` is its only skin), so a missing named skin is not the explanation.

Keep both legacy tracks intact in the archive and direct review. The viewer now explains their setup limitation. Use the complete stable loops for the live standalone character; avoid inventing a destructive attachment filter for these old clips without original initialization evidence.

Exact active-slot and body-translation evidence: `area-standalone-evidence.json`. The texture integration script preserves source alpha silhouettes for costume islands and can remove matte pixels; it does not add opaque coverage outside the source atlas silhouettes. Combined with the unchanged binary and source slot evidence, the duplicated geometry is attributable to the retained source arm variants, rather than a new mesh/rig introduced during redesign.
