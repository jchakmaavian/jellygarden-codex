# Royal Rocket VFX

A standalone rocket / line-clear effect lifted out of *Royal Throne*: a directional rocket that
launches, sweeps at a constant speed and exits the screen, trailing a bounded seven-cell plume —
compact gold nozzle flame, white-hot core, white and gold four-point stars, dissolving into soft
white smoke.

It is **purely visual**. It never destroys anything, never reads a board, and has no opinion about
your game. You get a `CellReached` event as the nose passes each cell and do your own destruction
there.

---

## 1. Unity version

Built and verified on **Unity 6000.5.10f1 (Unity 6.5)**, Built-in Render Pipeline, DX11.

- Should work unchanged on Unity 2021.3 LTS and newer. The only API that is at all modern is
  `ParticleSystem.EmitParams`, which has been stable for years.
- **Render pipeline:** the shader is a hand-written unlit CG shader with a single pass. It works in
  the **Built-in** pipeline as-is. Under **URP/HDRP** it still renders (unlit transparent shaders
  are compatible), but it is not SRP-batched. If you want SRP batching, port
  `Shaders/RocketFire.shader` to HLSL with a `UnityPerMaterial` CBUFFER — no other file changes.
- **Input system:** works with either backend. The demo reads keys from the IMGUI event queue, not
  `UnityEngine.Input`, so it does not throw on Input-System-only projects.
- **Colour space:** authored in Gamma. In Linear the flame reads slightly hotter; drop
  `Flame Intensity` to about 0.85 if it blows out.

## 2. Installation

Either:

- **`.unitypackage`** — `Assets > Import Package > Custom Package…`, pick
  `RoyalRocketVFX.unitypackage`, import everything. It lands at `Assets/RoyalRocketVFX/`.
- **`.zip`** — unzip so that the `RoyalRocketVFX` folder sits inside your `Assets/` folder.

The folder can live anywhere under `Assets/`; nothing uses hard-coded paths or a `Resources` folder.

The package ships its own assembly definition (`RoyalRocketVFX.asmdef`) with **zero references**.
That is deliberate: it is a compile-time guarantee that the package cannot depend on your game code
or on the game it came from. It is `autoReferenced`, so your own scripts can use it with no setup.

## 3. Prefab location

```
Assets/RoyalRocketVFX/
├── Prefabs/
│   ├── RocketVFX_Horizontal.prefab   preset for Left/Right pairs
│   ├── RocketVFX_Vertical.prefab     preset for Up/Down pairs
│   └── RocketVFX_Demo.prefab         player + on-screen test controls
├── Scripts/        RocketVFX (static API), RocketVFXPlayer, tuning, art descriptor
├── Shaders/        RocketFire.shader — every shape is procedural
├── Materials/      8 materials, all using RoyalRocketVFX/RocketFire
├── Sprites/        RocketUp / RocketDown / RocketLeft / RocketRight
├── Textures/       (none needed — see the note in the folder)
├── Particles/      (built at runtime — see the note in the folder)
├── Profiles/       DefaultRocketProfile.asset
├── Demo/           RocketVFX_Demo.unity + RocketVFXDemo.cs
└── README.md
```

`RocketVFX_Horizontal` and `RocketVFX_Vertical` are identical except for the **Default Axis**
field, which only affects `PlayDefaultPair()`. Either one can fire all four directions. You need
**one** of them in the scene, not both.

## 4. How to trigger the VFX

1. Drag `Prefabs/RocketVFX_Horizontal.prefab` into your scene. Position does not matter — every
   call passes its own world position.
2. Call it:

```csharp
using RoyalRocketVFX;

// One rocket, eight cells to the right, one world unit per cell.
RocketVFX.Play(transform.position, RocketDirection.Right, distanceInCells: 8f, cellSize: 1f);

// The match-3 case: both directions at once.
RocketVFX.PlayPair(transform.position, RocketAxis.Horizontal, 8f, 1f);
```

The player registers itself on `Awake`, so the static call finds it with no wiring. If no player is
in the scene the call logs a warning and does nothing — it never throws.

**Hooking up destruction.** The package deliberately does not destroy anything. Subscribe to the
player and do your own work as the nose arrives:

```csharp
var player = RocketVFX.Player;                 // or a direct reference to the component
player.CellReached += (worldPos, cellIndex) => {
    // cellIndex 0 is the launch cell, 1 is the next one, and so on.
    myBoard.DestroyCellAt(worldPos);
};
```

`CellReached` fires when the **nose** of the rocket sprite reaches the cell, not its centre, so the
destruction reads as caused by the rocket. `Launched` and `Exited` are also available.

## 5. Public API

```csharp
// ---- static facade: RoyalRocketVFX.RocketVFX ----
static void Play(Vector3 position, RocketDirection direction, float distanceInCells, float cellSize = 0f)
static void PlayPair(Vector3 position, RocketAxis axis, float distanceInCells, float cellSize = 0f)
static void Clear()                       // stop everything, return all rockets and particles
static RocketVFXPlayer Player { get; }    // the registered player, or null
static bool IsBusy { get; }
static void Register(RocketVFXPlayer p)   // called for you by the prefab
static void Unregister(RocketVFXPlayer p)

// ---- component: RoyalRocketVFX.RocketVFXPlayer ----
void Play(Vector3 position, RocketDirection direction, float distanceInCells, float cellSize = 0f)
void PlayPair(Vector3 position, RocketAxis axis, float distanceInCells, float cellSize = 0f)
void PlayDefaultPair(Vector3 position, float distanceInCells, float cellSize = 0f)
void Clear()

event Action<Vector3, int> CellReached;              // world position, cell index
event Action<Vector3, RocketDirection> Launched;     // after the anticipation squash
event Action<Vector3, RocketDirection> Exited;       // left the screen, returning to pool

bool Paused { get; set; }        // freeze without touching Time.timeScale
bool IsBusy { get; }             // any rocket or trailing particle still alive
int  ParticleCount { get; }      // 0 when fully cleaned up
int  ActiveRockets { get; }
RocketAxis DefaultAxis { get; }
RocketVFXTuning Tuning { get; }

enum RocketDirection { Up, Down, Left, Right }
enum RocketAxis { Horizontal, Vertical }
```

`distanceInCells` is how far the rocket **sweeps** — you get one `CellReached` per whole cell from
0 to that value inclusive (8 gives 9 events). After that the rocket keeps flying at the same speed
until it is fully off camera, then retires. That is what makes the exit read as "it flew away"
rather than "it stopped".

`cellSize` is world units per cell. Pass `0` to use the value on the component.

## 6. Inspector properties

On `RocketVFXPlayer`, or on a `Profiles/` settings asset. Defaults below are the shipped
production values.

| Property | Default | What it does |
|---|---|---|
| **Travel Speed** | 15 | Cells per second. One constant speed for every direction and origin, so horizontal and vertical rockets read at the same pace. |
| **Cell Size** | 1 | World units per cell, when `Play()` does not pass one. |
| **Anticipation** | 0.12 | Squash-and-hold before ignition, in seconds. Keep it short or activation stops feeling instant. |
| **Rocket Scale** | 0.64 | Rocket sprite length along its travel axis, in cells. Also how far ahead of centre a cell counts as reached. |
| **Exhaust Length In Cells** | 7 | Visual plume length. Trail lifetimes are derived as `length / speed`. |
| **Flame Intensity** | 1 | Brightness of the ribbon and nozzle flame. Lower it in Linear colour space or with heavy bloom. |
| **Star Density** | 1 | Multiplier on the four-point sparkles (4 per spatial sample at 1.0). |
| **Smoke Amount** | 1 | Opacity of the soft white tail. 0 disables smoke. |
| **Sorting Layer** | `Default` | Must exist in your project. |
| **Sorting Order** | 36 | Order of the rocket sprite; everything else is offset from it (see §8). |
| **Pool Size** | 8 | Maximum rockets in flight at once. Two per cross activation. |
| **Screen Exit Padding** | 2.2 | Extra cells travelled past the screen edge before retiring. |
| **View Camera** | empty | Camera used to work out where the screen edge is. Empty uses `Camera.main`. |
| **Default Axis** | Horizontal | Only used by `PlayDefaultPair()`. |
| **Register As Default** | on | Whether this player answers the static `RocketVFX.Play` calls. |

> **Do not lengthen the plume by slowing the rocket.** Trail lifetime is computed as
> `Exhaust Length In Cells / Travel Speed`, so the plume holds its length in cells at any speed.
> Raise **Exhaust Length In Cells** instead.

**Using a profile:** create one via `Assets > Create > Royal Rocket VFX > Settings Profile`, drop it
in the **Profile** slot, and it replaces the inline values. Handy for sharing one tuning across
several players. Leave the slot empty to use the component's own values.

## 7. Directional sprites

Four sprites in `Sprites/`: `RocketUp`, `RocketDown`, `RocketLeft`, `RocketRight` (512×512 PNG).

Each is wired on the prefab through a small descriptor with three fields:

| Field | Why it exists |
|---|---|
| **Texture** | The source art. |
| **Trim Rect** | Pixel rect of the *visible* artwork inside the texture. Rocket Scale is measured against this, so transparent padding does not shrink the rocket. |
| **Nozzle Pivot** | Normalised pivot inside the trim rect, sitting **on the nozzle**. This is what welds the flame to the rocket with no gap. |

Shipped values:

| Direction | Trim Rect (x, y, w, h) | Nozzle Pivot |
|---|---|---|
| Left | 27, 132, 456, 245 | (1, 0.5) |
| Right | 28, 139, 455, 231 | (0, 0.5) |
| Down | 110, 31, 294, 450 | (0.5, 1) |
| Up | 107, 29, 300, 454 | (0.5, 0) |

**To use your own rocket art:** drop in your textures, set Trim Rect to the visible bounds, and put
Nozzle Pivot on the rear of the rocket where the flame should start. Set the trim rect to
`0,0,0,0` to use the whole texture. Sprites are built at runtime from these values, so nothing
depends on sprite import pivots.

## 8. Sorting setup

Everything is a world-space renderer on one sorting layer. Orders are derived from
**Sorting Order** (`N`, default 36):

| Renderer | Order | Notes |
|---|---|---|
| Glow trail | N − 4 | widest, softest |
| Flame ribbon | N − 3 | the main plume |
| Nozzle flame | N − 2 | the mesh quad at the nozzle |
| White-hot core | N − 1 | thin bright centre |
| Rocket sprite | **N** | the rocket itself |
| Particles | N + 2 | stars, embers, smoke, rings — on top |

To put the effect above your board, set **Sorting Order** above your board's renderers, or put it on
its own sorting layer. Screen-space-overlay UI always draws above all of this, so a HUD stays on
top with no extra work. The effect uses `SpriteMaskInteraction.None` everywhere, so sprite masks on
your board will not clip it.

## 9. External dependencies

**None.** That is the point.

- No third-party plugins. In particular **no AllIn1VfxToolkit** — the source game has it, but the
  rocket never used it, so nothing from it is included or required.
- No Resources folder, no addressables, no packages beyond the Unity modules any project has
  (`ParticleSystem`, `SpriteRenderer`, `TrailRenderer`, `LineRenderer`).
- No textures for the effect itself. Every flame, star, ring, flash and smoke shape is generated in
  `RocketFire.shader`. The only bitmaps are the four rocket sprites.
- No fonts, no TextMeshPro, no `UnityEngine.UI`. The demo uses IMGUI so it pulls in no Canvas and no
  EventSystem.

Everything shipped here is original to this project and free to redistribute.

*Verified by audit: 37 assets, **0** references to anything outside `Assets/RoyalRocketVFX/`.*

## 10. Pooling and reuse

The player builds everything **once, on `Awake`**, and never allocates during play:

- **Pool Size** rocket objects, each with its sprite, two trail renderers, the flame line renderer
  and the nozzle mesh. `Play()` claims a free one and returns it when the flight finishes.
- Six particle systems with fixed capacities (2048 exhaust stars, 2048 impact stars, 1024 embers,
  512 smoke, 128 rings, 128 flashes), emitted into directly. No system is ever created at runtime.
- Two material instances (nozzle + ribbon) so the animation clock can be driven without writing to
  the assets on disk. They are destroyed with the component.

Practical notes:

- **Exhausting the pool is silent.** If all `Pool Size` rockets are busy, a further `Play()` does
  nothing. Raise Pool Size if you fire many at once — two per cross activation is the usual need.
- **Emission is spatial, not per frame.** One sample every 0.075 cells of travel, and every lifetime
  is derived from the live speed. Plume density and length are therefore identical at 30fps, 144fps
  and at `Time.timeScale = 0.5`. If you change how the rocket moves, keep that property.
- **Cleanup is automatic.** After exit, the rocket fades its ribbon over the trail lifetime, then
  clears its trails and deactivates. `ParticleCount` returns to 0. Verified over repeated plays.
- **`Clear()`** immediately returns everything to the pool — call it on level teardown or scene
  change. `OnDisable` does it for you.
- **Pausing:** set `player.Paused = true` to freeze the effect without touching `Time.timeScale`
  (useful for a pause menu that should not freeze UI animation).
- **Determinism:** the random sequence is seeded, so the same launch looks the same every run.

---

## Demo

Open `Demo/RocketVFX_Demo.unity` and press Play. On-screen buttons fire Up / Down / Left / Right,
fire horizontal and vertical pairs, and switch between 0.5× and 1.0× time scale. Arrow keys and
`1` / `2` do the same. A faint grid marks cell boundaries so you can count the plume length —
it should measure about seven cells.

## Verified

| Check | Result |
|---|---|
| All four directions, three consecutive plays each | pass |
| Cell events per 8-cell sweep | 9 (indices 0–8) |
| Peak particles per rocket | ~530–560 |
| Particles after each flight | 0 |
| Pair firing | exactly 2 rockets, both returned |
| 0.5× time scale | same particle density as 1.0× |
| External asset references | 0 |
| Missing scripts / null material or texture slots | none |
