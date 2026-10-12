# King reference - inspection notes

Inspected 24 September 2026, read-only. Nothing from the reference was copied into this character's
files or uploaded to Magnific; the reference informed age, build, style and timing only.

Sources:

- `Design/Backup/Reference-UI-Characters/20260923-184326/Characters/King/Original/` - `King.skel`, `King.atlas`, `King.png`.
- `.../Characters/King/Recovered/King.spine38.json` - the decoded skeleton.
- `.../Previews/Characters/King/` - 31 silent 60 fps MP4s, 512 x 512, labelled "Partially reconstructed" by that task.

How it was studied: decoded keyframe data for all 32 animations, plus timestamped contact sheets at
1/3 s and 1/4 s steps of nine previews. Those were idle_loop_1, Happy_2, k-1_sevinme, k-1_kaygi,
k-1_uzulme, Heyecan_1, Relax_1, k-2 and k-1_dans. The videos were not watched in real time; frame
sheets and curve data were used instead.

## What the reference is

| Aspect | Finding |
|---|---|
| Rig format | **Spine 3.8.84, 2D skeletal.** 29 bones, 35 slots, 1 transform constraint. Mesh attachments with weights and per-frame mesh deforms, plus a clipping attachment for the frame. Not 3D. |
| Framing | A **framed head-and-shoulders portrait**, not a full-body character. The rig holds a face, ears, beard, mustache, crown and a shoulder cape ("Pelerin"), inside an arched frame with six colour skins. |
| Body | Nothing below the shoulders exists. The one extra body part is `el_OK`: a thumbs-up hand with a sleeve, in the `sag`/`sol` slots, hidden by default. |
| Apparent age | Elderly, with white hair, a full white beard, a white mustache and bushy white brows. No age is stated anywhere, so none is assumed. |
| Build and proportions | Very large head against narrow framed shoulders. Full, rounded cheeks that bulge in smiles, a large bulb nose, a wide soft face and broad cape shoulders. The belly and legs are not shown, so the full-body build is an extrapolation. |
| Rendering | Painted 2.5D sprites with a 3D look. Warm key light from the upper left, soft gradients, saturated red, gold and white, near-front view. |
| Costume | Tall gold crown with a red gem, red cape over gold, white beard. |
| Motion method | The face moves through a `yuz_kontrol` control bone: features slide to look around and shift the head. Mesh deforms squash the cheeks, beard, mouth and lids. Blinks swap lid and lash attachments. The cape breathes by scale, up to 1.06, with small translations. The head bobs up to 56 px and rotates -10 to +4 degrees. |

## Animation list (actual data)

"Seen" marks the animations whose frames were inspected. The rest are described from their keyframe
channels and names: sevinme is joy, kaygı is worry, üzülme is sadness, heyecan is excitement, dans is
dance, and yedek is a backup copy.

| Animation | Duration | Seen | Content |
|---|---|---|---|
| idle_loop_1 | 8.95 s | yes | Slow breathing drift and glances around, 4 blinks, a sly squint near the end. Loops. |
| idle_loop_2 | 5.22 s | - | Idle with more face motion (27 channels). |
| idle_loop_3 | 4.22 s | - | Short idle (13 channels). |
| idle_connect_1 / _2, animation | 4.58 s each | - | Transitions into idle; identical channel sets. |
| k-1_idle_3 / _4 / _5 / _6 | 5.12 / 4.82 / 2.48 / 1.00 s | - | Idle variants. |
| Happy_1 | 3.33 s | - | Head rises up to 32 px, lids swap, open-mouth smile, cape shrug. |
| **Happy_2** | 3.77 s | yes | Head bob with open smile, then the **thumbs-up hand enters from the lower left at 1.90 s**, held with a smile and head tilt, then settles. |
| k-1_sevinme (+ _2, _3, _4, _5, 6, _NEW and 3 yedek copies) | 0.70 - 4.22 s | yes (base) | Joy: squint and laugh, head lifts with chin up and eyes closed around 1.0 s, open smile, settle. _5 and 6 also swap in the thumbs-up at 1.90 s. |
| Heyecan_1 | 5.98 s | yes | Excitement: wide eyes, big open smile, repeated head tilts and bobs. |
| Relax_1 | 1.98 s | yes | Content: eyes close, head leans back slightly, eyes reopen. |
| k-1_kaygi / k-1_kaygi_loop | 5.98 / 11.65 s | yes (base) | Worry: eyes dart sideways, brows pulled up at the centre, pursed wavy mouth, head shifts side to side. |
| k-1_uzulme | 2.82 s | yes | Sadness: brows tilt and droop, eyes lower, mouth turns down, head sinks slightly. |
| kaygı-uzulme | 1.52 s | - | Worry into sadness (34 channels). |
| k-1_dans | 4.55 s | yes | Dance: rhythmic head sway -10 to +4 degrees with happy blinks. |
| k-2 | 1.92 s | yes | Glance up and aside with a small nose and mouth shift, like thinking. |
| k-1 animation | 18.67 s | - | Long combined sequence. |
| _3 | 0 s | - | Empty track, no keys. |

Timing values taken from the data:

- **Blinks:** the lid and lash swap lasts 0.10-0.32 s.
- **Idle loops:** 4.2-9.0 s.
- **Joy reactions:** 2.8-3.8 s.
- **Thumbs-up swap:** 1.90 s in all three animations that use it.
- **Settle:** motion eases back to rest over the last 0.4-0.8 s.

## How King Venguard answers the reference

- **Same:** elderly, heavyset, full-cheeked and friendly. Same polished 3D-look casual rendering, same
  near-front view, the same Spine 2D skeletal format and the same attachment-swap approach for lids,
  mouths and hands.
- **Different:** a clean chin with silver mutton-chops and a curled mustache instead of a full beard.
  A small button nose, and blue-grey eyes. A low gold circlet with sapphires and sun points over a blue
  velvet cap. A royal-blue sun-embroidered coat, short cape, cream fur collar, belt and ivory breeches.
  Blue and gold instead of red and gold.
- **Extended:** a full-body rig. The reference has no body, so Venguard's weight shifts, belly breathing
  and leg behaviour are this design's interpretation. They cannot be checked against the reference.
- **Timing matched:** the idle loop is 4.0 s, near idle_loop_3's 4.22 s. The blink lasts 0.14 s. The
  reaction lasts 3.8 s against Happy_2's 3.77 s, with the thumbs-up swapped in at 1.90 s.
