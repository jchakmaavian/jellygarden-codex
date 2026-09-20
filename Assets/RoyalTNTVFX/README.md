# Royal TNT VFX

The TNT explosion from *Royal Throne*, extracted as a standalone effect. The TNT artwork squashes
and warms up for a short ignition, then a white-hot ball bursts into gold rays and a broken
shockwave, a spark wave rolls out ring by ring across the blast area, and the whole thing settles
into soft glowing smoke with falling four-point stars.

It is **purely visual**. It never destroys anything and knows nothing about boards, gems, scores
or levels. As the spark wave reaches each cell you get a `CellReached` event, and you break your
own pieces there.

---

## 1. Unity version

Built and verified on **Unity 6000.5.10f1 (Unity 6.5)**, **Built-in Render Pipeline**, DX11.

- Should work on Unity 2021.3 LTS and newer; the code uses only long-stable APIs.
- **Render pipeline:** one hand-written unlit CG shader, one pass. Works as-is in Built-in. Under
  URP/HDRP an unlit transparent shader like this still renders, but it is not SRP-batched and has
  not been verified there.
- **Input system:** the demo reads keys from the IMGUI event queue, so it works with the legacy
  input manager and with the Input System package.

## 2. Installation

- **`.unitypackage`** — `Assets > Import Package > Custom Package…`, choose
  `RoyalTNTVFX.unitypackage`, import everything. It lands in `Assets/RoyalTNTVFX/`.
- **`.zip`** — unzip so the `RoyalTNTVFX` folder sits inside your `Assets/` folder.

The folder can live anywhere under `Assets/`. There are no hard-coded paths and no `Resources`
folder.

The package has its own assembly definition, `RoyalTNTVFX.asmdef`, with **zero references**. That
is a compile-time guarantee that it cannot depend on your game code or on the game it came from.
It is `autoReferenced`, so your scripts can use it with no setup.

## 3. Prefab location

```
Assets/RoyalTNTVFX/
├── Prefabs/
│   ├── TNTExplosionVFX.prefab        the player - drop this in your scene
│   ├── TNTExplosionVFX_Demo.prefab   player + on-screen test controls
│   └── TNTExplosionActor.prefab      one pooled explosion (used by the player)
├── Scripts/     TNTExplosionVFX (static API), TNTExplosionPlayer, TNTExplosionActor
├── Shaders/     TNTGlow.shader - every shape is procedural
├── Materials/   Glow, RadialRays, Shockwave, FourPointStars
├── Textures/    TNT.png - the original TNT artwork
├── Particles/   (no ParticleSystems - see the note in the folder)
├── Demo/        TNTExplosionVFX_Demo.unity + TNTExplosionDemo.cs
└── README.md
```

You need **one** `TNTExplosionVFX` in the scene. You do not place `TNTExplosionActor` yourself;
the player instantiates its pool from it.

## 4. How to trigger the VFX

1. Drag `Prefabs/TNTExplosionVFX.prefab` into your scene. Its position does not matter.
2. Call it:

```csharp
using RoyalTNTVFX;

// Standard TNT: 5x5 cells, one world unit per cell.
TNTExplosionVFX.Play(transform.position, radius: 2, cellSize: 1f);

// TNT + TNT combo: 9x9 cells. Intensity 1.45 is applied automatically for radius > 2.
TNTExplosionVFX.Play(transform.position, radius: 4, cellSize: 1f);
```

The player registers itself on `Awake`, so the static call finds it with no wiring. With no player
in the scene the call logs a warning and does nothing; it never throws.

**Hooking up destruction.** Subscribe once and break your own pieces as the wave arrives:

```csharp
var player = TNTExplosionVFX.Player;
player.CellReached += (worldPosition, offset) =>
{
    // offset is the cell relative to the centre: x right, y up. (0,0) is the TNT itself.
    myBoard.BreakCellAt(worldPosition);
};
```

`CellReached` fires ring by ring in the same order and with the same timing the game uses:
the centre 0.012 s after the blast opens, then each further ring 0.025 s later.

## 5. Public API

```csharp
// ---- static facade: RoyalTNTVFX.TNTExplosionVFX ----
static void Play(Vector3 position, int radius, float cellSize = 0f, float intensity = 0f)
static void Clear()                          // hide everything immediately
static TNTExplosionPlayer Player { get; }    // the registered player, or null
static bool IsBusy { get; }
static void Register(TNTExplosionPlayer p)   // done for you by the prefab
static void Unregister(TNTExplosionPlayer p)

// ---- component: RoyalTNTVFX.TNTExplosionPlayer ----
void Play(Vector3 position, int radius, float cellSize = 0f, float intensity = 0f)
void Clear()

event Action<Vector3, int>        Exploded;     // blast opens: position, radius
event Action<Vector3, Vector2Int> CellReached;  // wave reaches a cell: position, offset
event Action<Vector3>             Completed;    // explosion faded and returned to the pool

bool  Paused { get; set; }          // freeze without touching Time.timeScale
bool  IsBusy { get; }               // any explosion or TNT sprite still visible
int   ActiveExplosions { get; }
int   VisibleRenderers { get; }     // 0 when fully cleaned up
int   Reuses { get; }
float CellSize { get; }
```

| Parameter | Meaning |
|---|---|
| `radius` | Cells from the centre. The blast covers a square of `(2 × radius + 1)²` cells: 1 → 3×3, 2 → 5×5, 4 → 9×9. |
| `cellSize` | World units per cell. `0` uses the Inspector value. |
| `intensity` | Blast scale. `0` applies the game's rule: **1.45 when radius > 2, otherwise 1**. |

**Timeline of one explosion:** 0.12 s ignition → blast opens → 0.86 s blast, smoke and falling
stars → pooled object deactivates.

## 6. Inspector properties

On `TNTExplosionPlayer`. Defaults are the shipped production values.

| Property | Default | What it does |
|---|---|---|
| **Actor Prefab** | `TNTExplosionActor` | The pooled explosion. Leave as is. |
| **Cell Size** | 1 | World units per cell when `Play()` passes 0. |
| **Blast Wave Per Cell** | 0.025 | Delay between rings of the spark wave, in seconds. |
| **Show TNT Sprite** | on | Show the TNT artwork squashing during ignition. Turn off if your own board piece is still visible and should do that instead. |
| **TNT Texture** | `Textures/TNT` | The TNT artwork. |
| **TNT Trim Rect** | 76, 41, 360, 430 | Pixel rect of the visible art inside the texture. `0,0,0,0` uses the whole texture. |
| **TNT Pivot** | (0.5, 0.5) | Pivot inside the trim rect. |
| **TNT Sprite Cell Fraction** | 0.9 | Sprite size as a fraction of one cell. |
| **Sorting Layer** | `Default` | Must exist in your project. |
| **Sorting Order Offset** | 0 | Added to every built-in order (see §8). |
| **Pool Size** | 32 | Explosions on screen at once. Same as the game. |
| **Register As Default** | on | Whether this player answers the static calls. |

The ignition time (0.12 s) and blast length (0.86 s) are constants on `TNTExplosionActor`
(`IgnitionDuration`, `ExplosionDuration`). They are part of the approved animation curves, so they
are deliberately not exposed as sliders.

## 7. TNT artwork

`Textures/TNT.png` is the original Royal Throne TNT barrel artwork, byte-identical to the game's
copy and imported with the same settings. It is shown only during ignition: it squashes
(width +10 %, height −12 % at the midpoint) and warms toward yellow, then disappears the instant
the blast opens.

To use your own art, assign your texture, set **TNT Trim Rect** to the visible pixels and adjust
**TNT Sprite Cell Fraction**. The sprite is built at runtime, so its import pivot does not matter.

## 8. Sorting setup

Everything is a world-space renderer on one sorting layer. Built-in orders, each plus
**Sorting Order Offset**:

| Renderer | Order |
|---|---|
| SmokeGlow | 29 |
| Shockwave | 30 |
| RadialRays | 31 |
| GoldBlastGlow | 32 |
| WhiteHotCore | 33 |
| Cell impacts | 34 |
| Stars | 35 |
| TNT sprite | 36 |

To draw above your board, raise **Sorting Order Offset** until 29 + offset is above your board's
highest order, or use a dedicated sorting layer. Screen-space-overlay UI always draws on top.

## 9. External dependencies

**None.**

- No third-party plugins. The game this came from contains AllIn1VfxToolkit, but the TNT explosion
  never used it; nothing from it is included or required.
- No ParticleSystems, no Resources folder, no addressables, no packages beyond core Unity modules.
- No effect textures: every flame, ray, ring, star and smoke shape is generated in
  `TNTGlow.shader`. The only bitmap is the TNT artwork.
- The quads use Unity's built-in Quad mesh, which exists in every project.
- The demo uses IMGUI, so no Canvas, EventSystem, UI package or TextMeshPro.

Everything shipped here is original to this project and free to redistribute.

*Verified by audit: 23 assets, **0** references to anything outside `Assets/RoyalTNTVFX/`.*

### What was deliberately left out

These are part of the game's TNT but are not the explosion effect itself:

| Left out | Why | What to do instead |
|---|---|---|
| **Gem shatter fragments** | In the game the coloured chips flying out are the game's own gem-break system cutting up *the destroyed gem's sprite*. They need a real board piece to exist. | Run your own break/shatter in `CellReached`. It fires at the exact moment the game starts its shatter. |
| **3D barrel mesh** (and its `TNTBody` shader and body materials) | Present in the game's prefab but hidden during every explosion. It was never visible in the approved effect. | Nothing. |
| **Creation "reveal" pop** | Plays when a TNT is *made*, not when it explodes. A separate effect. | Nothing. |
| **Sound** | Audio is outside a VFX package. | Play your own TNT sound on `Exploded`. |

## 10. Pooling and reuse

The player builds its pool **once, on `Awake`**, and allocates nothing during play:

- **Pool Size** explosion actors (each 58 quads: 5 blast layers, 28 stars, 25 cell impacts) plus
  one TNT sprite per actor, all created inactive.
- `Play()` claims a free actor, resets it to a clean state, and plays. When the blast finishes,
  every renderer is disabled and the actor deactivates, ready for reuse.
- One `Sprite` is built from the TNT texture and destroyed with the player.

Practical notes:

- **Exhausting the pool is silent.** If all actors are busy, a further `Play()` does nothing.
  32 is generous; raise it only for very dense chains.
- **Large blasts recycle cell impacts.** Each actor has 25 cell-impact sparks. A 9×9 blast
  reaches 81 cells, so impacts are reused in order — exactly as in the game.
- **No particle leaks are possible.** There are no ParticleSystems; cleanup is disabling a fixed
  set of renderers. `VisibleRenderers` returns to 0.
- **`Clear()`** hides everything immediately. Call it on level teardown. `OnDisable` does it for you.
- **Pausing:** `player.Paused = true` freezes every explosion, including ones started while paused,
  without touching `Time.timeScale`. Unpausing resumes them.

---

## Demo

Open `Demo/TNTExplosionVFX_Demo.unity` and press Play. Buttons fire a radius-1, radius-2 (TNT)
and radius-4 (TNT + TNT) explosion and switch between 0.5× and 1.0× time. Keys: `Space` = TNT,
`C` = TNT + TNT, `1` / `2` = 1.0× / 0.5×. A faint grid marks cell borders.

## Verified

| Check | Result |
|---|---|
| Frame-by-frame parity with the game's `TNT3DAnimator` (same frames, same events) | pass — 300 frames, all 58 renderers, max difference 0.000001 |
| Structural match with the game's prefab (positions, scales, sorting, materials, meshes) | 0 differences |
| Radius 1 / 2 / 4, repeated | pass; cell events 9 / 25 / 81 |
| 5 simultaneous explosions | pass, all returned |
| 9 explosions started while paused, then resumed | pass, all returned |
| 0.5× time scale | pass |
| `Clear()` mid-blast | pass |
| Visible renderers after every run | 0 |
| Pool reuse | pass |
| External asset references / missing scripts / null references | 0 / 0 / 0 |
