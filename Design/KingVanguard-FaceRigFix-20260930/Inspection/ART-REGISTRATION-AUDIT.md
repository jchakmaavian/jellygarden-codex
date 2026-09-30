# Facial artwork registration audit

This is a read-only audit of the installed gameplay portrait atlas and its source workboard. It also records the installed Area atlas hash because the two rigs use the same repainted facial components. No Unity session was opened and no runtime asset was changed.

The artwork alone does **not** establish that the source mesh topology is incompatible. The new pieces remain separate attachments in their original UV rectangles. There are some visible contour and shading differences that can make a motion-registration problem more noticeable. Fix and review the motion mapping first; a complete artwork regeneration is not justified by these crops.

- [Mouth, moustache and teeth comparison](mouth-and-moustache-registration.png)
- [Face base, nose, brows and eyelids comparison](face-brows-and-lids-registration.png)
- [Exact input hashes, coverage measurements and source topology](art-registration-audit.json)
- [Reproducible technical extraction script](inspect_art_registration.py)

Each sheet shows the original decoded APK component, the generated paint sampled back into the same logical region, then the actual installed texels. Components are enlarged with nearest-neighbor filtering to expose registration and clipping. The middle column's simple green removal is diagnostic; it is not replacement artwork or a byte-exact reproduction of the C# packer.

## Specific findings

| Component | Installed/source alpha sum | Finding and smallest useful follow-up |
| --- | ---: | --- |
| Moustache, cell 03 `Bıyık` | 84.03% | The new paint is a largely symmetric pair of lobes, whereas the original component is asymmetric, with unequal curl-tip heights. Its new paint is clipped by the original alpha island. The actual installed crop loses part of the intended tip/lobe outline. After the motion fix, check nose-to-centre contact and both curl tips in idle, worry and celebration. If still visible, register only this painted cell to the original asymmetry while retaining its new white hair; do not move the entire rig to compensate for a texture contour. |
| Right/left brows, cells 25/27 | 63.92% / 62.88% | The new arched hair shapes leave substantially more transparent area inside the original broad wedge-shaped regions. Their visible mass is narrower and its apparent centre differs from the mesh region centre. This can make the old brow pivot look misplaced. If the corrected motion still looks wrong, reshape/register these two cells against their original alpha contours; changing all head artwork is unnecessary. |
| Lower teeth, cell 19 `Alt Diş` | 99.25% | Coverage is close to source. The new teeth introduce much brighter enamel and strong individual divisions where the source is a subdued shaded band. This raises the visibility of any separation from the mouth. There is no evidence here of a duplicated lower-teeth layer. Judge placement after geometry is corrected; optionally soften/darken only the gum/root edge if the teeth still appear detached. |
| Upper teeth, cell 21 `Ust Diş` | 94.77% | The generated teeth are more symmetric and horizontal than the slanted source band; packing clips the top/ends into the original region. Strong, square tooth divisions can stretch visibly in an eight-vertex deform mesh. A small cell-only alignment and softer divisions may help if needed after motion QA. Do not offset upper and lower teeth independently without checking the full mouth cycle. |
| Mouth, cell 20 `Ağız` | 99.83% | The installed texture matches the prior mouth-only repair. It contains the dark cavity and lip border, with **no baked tongue or teeth**. The source's small transparent opening near the upper middle remains because the new paint is clipped to the source alpha. The tongue is still the separate cell 18 attachment. |
| Eyelids, cells 10/14 | 99.47% / 98.50% | Outline coverage is close to source. The new paint adds a pronounced crease absent from the nearly plain original lid. An attachment visibility change can reveal this crease abruptly even when bones interpolate smoothly. If that remains distracting after transition QA, soften only the crease; this is not evidence that the lid pivot itself is wrong. |
| Face/beard base, cell 01 | 98.93% | No eyes, brows, nose, teeth, tongue or separate moustache are baked into the underlay. The larger-looking white beard comes from new texture detail and geometry adaptation, not a duplicate mouth assembly. |

The final pack policy is **new keyed paint alpha clipped to the source UV island alpha**, not forced original coverage. This is confirmed by both the final pack report and exact installed alpha comparisons. The packer also fills RGB under transparent texels from nearby **new** paint. Stripes visible when viewing raw atlas RGB outside the visible alpha are not, by themselves, a rendered defect. An earlier atlas-wide impression of forced visible edge fill was corrected after checking the actual packed alpha.

## Original attachment behavior relevant to placement

The original portrait data registers these parts independently:

| Attachment | Source topology | Motion relationship |
| --- | --- | --- |
| `Bıyık` | 56 vertices, 73 triangles, weighted | Seven moustache bones; five clips also contain deform timelines. |
| `Sağ Kaş` / `Sol Kaş` | 45/31 vertices, weighted | Two brow bones per side; no explicit deform timeline in the source inventory. |
| `Ağız` | 62 vertices, weighted | Upper/lower lip bones; 26 clips contain deform timelines. |
| `Alt Diş` | 16 vertices, unweighted | Attached to `yuz`; 23 clips contain deform timelines. |
| `Ust Diş` | 8 vertices, unweighted | Attached to `yuz`; 21 clips contain deform timelines. |
| `Dil` | 8 vertices, unweighted | Attached to `yuz`; 24 clips contain deform timelines. |

This mixture means teeth follow keyed deform coordinates while the mouth/moustache also depend on weighted animated bones. A setup-pose screenshot cannot establish correct registration throughout a clip or blend. The runtime review should evaluate both clip interiors and transition boundaries before artwork offsets are introduced.

## Exact assets and rectangles

Installed runtime textures are `Assets/Characters/KingVenguard/V2/Spine/King_Vanguard.png` and `AreaCompleted_Vanguard.png`. The original portrait is `Design/KingVanguard-Reference-20260929/Original-rfv387/King.png`; the original logical region definitions and masks are in `FaceRedesign/Portrait/Portrait-layout.json` and its `Parts` directory.

Portrait physical PNG rectangles, top-left origin, are:

| Cell | Region | x, y, width, height |
| --- | --- | --- |
| 03 | Moustache | 407, 100, 187, 87 |
| 18 | Tongue | 382, 1538, 67, 29 |
| 19 | Lower teeth | 407, 12, 69, 32 |
| 20 | Mouth | 505, 973, 154, 72 |
| 21 | Upper teeth | 602, 8, 67, 26 |
| 25 | Right brow | 382, 1842, 34, 30 |
| 27 | Left brow | 477, 9, 37, 31 |

The final painted mouth workboard is `Design/KingVanguard-ActivityFix-20260930/FaceRedesign/Portrait-mouth-repaired-workboard.png`; it retains the other painted cells. The mouth-only final hashes remain `e769a63f610e82b39a325ef8e9573d2664b8c3f424c36a761d5dbb3e2b96fa2e` for portrait and `09e909b4016fd87a7450fa2a8ebf75753caf071b280d59370f2b4af0e534ad40` for Area.

This audit does not review the unrelated full-body/native or bedtime rigs. Its conclusions concern the shared portrait/Area facial art used for the reported gameplay portrait.
