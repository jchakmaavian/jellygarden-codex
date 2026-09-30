# Mouth bands and detached shadows

The original files do not prove that `Alt Diş` and `Ust Diş` are lip covers despite their tooth names. They are independent upper/lower mouth meshes with many expression-specific deform timelines. However, their original **visible artwork really is brown**, and replacing it with complete, bright white dental arches changes how the source rig reads.

[mouth-shadow-semantics.json](mouth-shadow-semantics.json) records the setup geometry, source draw order, tints and color measurements. [inspect_mouth_shadow_semantics.py](inspect_mouth_shadow_semantics.py) reproduces those measurements without Unity or asset changes.

| Attachment | Original visible median RGB | Installed visible median RGB |
| --- | --- | --- |
| Lower teeth | 139, 92, 76 | 210, 183, 161 |
| Upper teeth | 144, 89, 74 | 220.5, 197, 176 |
| Moustache shadow | 108, 37, 22 | 43, 43, 8 |
| Right brow shadow | 136, 61, 30 | 47, 47, 10 |
| Left brow shadow | 136, 61, 30 | 47, 47, 12 |

These measurements use visible texels with source alpha at least 128 and image alpha at least 64. They exclude fully transparent RGB. Thus the detached olive shadow color seen in the current idle close-up is a real visible color, not merely a raw-atlas preview artifact. Both tooth attachments have white slot and attachment tint (`FFFFFFFF`) and Normal blending in the source data. The source material does not contain a saved tooth-specific whitening parameter.

The original setup draw order is tongue → lower teeth → mouth → upper teeth → moustache shadow → moustache → nose. Original setup-world bounds put both tooth bands within the mouth's bounds:

- Lower teeth: x −20.00…49.00, y 117.00…149.00.
- Upper teeth: x −17.00…49.06, y 123.94…148.74.
- Mouth: x −65.00…88.33, y 90.00…161.65.
- Moustache: x −90.04…95.03, y 119.00…206.00.

This is consistent with heavily occluded mouth bands. Lower teeth are behind the mouth attachment, while upper teeth are in front of the mouth but behind the moustache. The bright floating rows in the current sad close-up can therefore be amplified by both the registration drift and the new highly visible enamel. Setup bounds alone do not establish behavior at every animated frame.

The bounded recommendation is:

1. Complete the coherent animated geometry correction, then inspect sad, worry, idle, and happy poses plus the blends between them.
2. Keep the source mouth assembly and occlusion order. If the bright rows still dominate, revise only cells 19 and 21 into softer, subdued warm mouth/tooth bands, with restrained enamel and fewer strong tooth divisions. Preserve their registered contours; do not reinterpret the entire UV rectangle as a complete dental arch or offset each row independently.
3. Hide the obsolete `Sağ Kaş Gölge` and `Sol Kaş Gölge` attachments. The new brow art already has self-shading. The old standalone shadows are attached to `on_yuz`, while each actual brow is a separately weighted two-bone mesh, and their old silhouettes no longer match the thinner new arches. Removing these two external layers is more predictable than recreating new ones now. Use an attachment-level suppression that animated slot color keys cannot reverse.
4. Review `Bıyık Gölge` after the geometry correction. It also contains an olive shift and is very visible under the new bright moustache. If still detached, either suppress only that external layer or retint it to a restrained warm-neutral shadow while keeping its existing alpha and motion. Prefer the former if the moustache's painted self-shading already supplies enough separation.

No artwork or runtime changes were made by this audit. The recommendation does not require a new full-face generation.
