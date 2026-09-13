# Royal Match-grade board VFX — design

Date: 2026-09-13
Status: awaiting review

## The problem, measured

The board's blast feel is one pooled sprite animator and one sound. That is the
whole of it.

- Ordinary clear: `LevelManager.GetExplFromPool()` leases one of 20 `ItemExpl`
  objects, sets an animator int, plays one clip. `Item.cs:1017-1024`.
- **Rocket, package and colorbomb spawn no particle of their own.** They are a
  sound plus the ordinary clear, repeated per affected item.
  `Item.cs:1085`, `:1106`, `:1128`, `:1147`. Royal Match's signature is that
  these are enormous; here they are the ordinary clear N times.
- Combo is one line: at `combo > 2`, switch on a random `gratzWords` object.
  `LevelManager.cs:1922-1925`. It is never switched off in code.
- No camera shake, no screen flash, no hit-stop. `Time.timeScale` is used only
  as a hard pause, never ramped.
- No tween library. All motion is hand-rolled coroutine `Vector3.Lerp`.

Royal Match stacks four to six simultaneous layers per blast. This stacks one.
That gap, not the quality of any single effect, is why it reads flat.

## Constraints that shape everything

**1. The guard is a line-count floor, not just a method-set check.**
`tools/check_vfx_intact.py` check 3 fails on `new_lines < old_lines` for the six
board scripts. Reformatting fails it at identical behaviour. Every change to
those files must be a pure insertion.

**2. The kit pool starves silently.** `GetExplFromPool()` is a linear scan over
20 slots returning `null` when exhausted, and the caller at `Item.cs:1018`
no-ops on null. Full-intensity juice multiplies effects per blast; on the
current pool they would silently stop appearing.

**3. One AudioSource.** `SoundBase.PlaySound` is a single shared
`PlayOneShot`. Overlapping blasts already contend for one channel.

**4. ParticleSystem does not render in a ScreenSpaceOverlay canvas.** Documented
in `RepairVfx.cs:8-13`. The board is world-space so particles work there; any
full-screen layer must be UI quads instead. One technique does not cover both.

## Decisions taken

- **Event bus, zero VFX logic in kit files.** Six one-line insertions fire
  events; all behaviour lives in new Story-layer files.
- **A separate growable pool.** The kit's 20 slots keep doing exactly what they
  do today. Untouched, so unregressable.
- **Full Royal Match intensity**, tuned per event class rather than uniformly.
- **Verification is a playable in-engine demo**, built and run by the owner.

## Architecture

```
Item.cs / LevelManager.cs        (6 inserted lines, no logic)
        │  BoardVfx.Blast(kind, worldPos, colour, depth)
        ▼
BoardVfx                         static router; no-op if no director
        ▼
BoardVfxDirector  (MonoBehaviour, world-space, board layer)
        ├── BoardVfxPool         growable, RepairVfx idiom
        ├── BoardVfxCamera       shake + hit-stop
        └── BoardVfxOverlay      full-screen flash (UI quads, overlay canvas)
```

`BoardVfx` is a static façade holding a nullable director reference. When no
director is present every call is an early return, so the kit runs exactly as it
does today and the demo scene is the only thing that installs one. This is what
makes the work reversible: deleting the director restores current behaviour with
no edit to the six insertions.

### The six insertion sites

Every one is an addition between existing statements. None removes, reorders or
reformats anything.

| Site | Event |
|---|---|
| `Item.cs:1018` `DestroyCor` | `Clear` — origin, colour |
| `Item.cs:1086` `DestroyHorizontal` | `RocketH` — origin, row |
| `Item.cs:1107` `DestroyVertical` | `RocketV` — origin, column |
| `Item.cs:1129` `DestroyPackage` | `Package` — origin |
| `Item.cs:1148` `DestroyColor` | `ColorBomb` — origin, colour |
| `LevelManager.cs:1921` | `Combo` — depth, read before the reset to 0 |

The combo site is notable: `combo` is read and reset to `0` on adjacent lines,
so cascade depth already exists and is currently discarded. The escalation input
Royal Match needs is free.

### Why a separate pool, and how big

`RepairVfx.cs:22-25` already establishes the house pattern and its rationale: a
pool sized to measured peak concurrency, because "a pool that runs dry does not
look sparser, it looks like the effect stuttering." `BoardVfxPool` follows it —
same idiom, sized to the worst case, which is a colorbomb clearing a full colour
across an 81-cell board while a cascade is already running. It grows rather than
returning null, so the kit's silent-starvation failure mode cannot recur in the
new layer.

### The layers per event

Ordinary clears stay restrained; the spectacle is spent on power-ups and
combos, so the board stays readable across 1208 levels.

| Event | Layers |
|---|---|
| Clear | existing kit animator (untouched) + gem-tinted spark burst + faint bloom |
| Clear, cascade ≥2 | above, plus scale and pitch rising with depth |
| RocketH/V | directional streak along the line, speed-lined trail, shake along the axis, brief hit-stop |
| Package | expanding shockwave ring, debris burst, radial shake, hit-stop |
| ColorBomb | full-screen colour-tinted flash (overlay quads), beams to each cleared gem, largest shake, longest hit-stop |
| Combo | escalating flourish keyed to depth; the existing `gratzWords` object is left exactly as-is |

Colour comes from `LevelManager.scoresColors[color]`, the same source the kit
already uses for the package Firework tint, so new effects match the gems
without new palette data.

### Where each technique applies

- **Board-space** (sparks, shockwaves, streaks, debris): world-space, so
  ParticleSystem and the already-installed AllIn1VfxToolkit are usable.
- **Full-screen** (flash, vignette pulse): UI quads on the overlay canvas, in
  the `RepairVfx` idiom, because ParticleSystem will not render there at all.

`Assets/Plugins/AllIn1VfxToolkit` is imported and referenced by zero game
scripts or assets. Its shaders and particle presets are the board-space source,
so the board-space layers need no new art.

### Audio

The single shared `AudioSource` cannot carry layered blasts. `BoardVfxAudio`
adds a small voice pool alongside `SoundBase` — additive, and `SoundBase` keeps
serving every existing call unchanged.

### Intensity

One `BoardVfxProfile` ScriptableObject holds per-layer magnitudes and a 0–1
master. Ships at full Royal Match. The dial exists so the demo can be judged at
several intensities without a rebuild, not to defer the decision.

## The demo

A new scene plus new scripts. It calls into the kit; it does not modify it.

- Same board, same blast, fired on command.
- `A` — current VFX. `B` — enhanced. Toggle live for direct comparison.
- Keys to fire each event class: clear, rocket, package, colorbomb, deep combo.
- An on-screen intensity slider bound to the profile master.

The owner builds and runs it and judges. No sweep is run to verify this.

## Verification

- `python3 tools/check_vfx_intact.py` — must stay green at every commit. The
  design's whole shape is chosen to make this trivially true.
- `python3 tools/make_typecheck_csproj.py && dotnet build Story.Typecheck.csproj`
  — new scripts are not in Unity's generated csproj.
- The demo, judged by eye.

## Explicitly out of scope

- Win/goal moments and UI/map polish. Not chosen.
- The dark near-photographic backdrops that `check_themes.py` NOTEs against the
  bright-cartoon rule. Real, unrelated, untouched here.
- Rewriting the kit's 20-slot pool. The new layer routes around it; the kit's
  own starvation bug is left as found.
