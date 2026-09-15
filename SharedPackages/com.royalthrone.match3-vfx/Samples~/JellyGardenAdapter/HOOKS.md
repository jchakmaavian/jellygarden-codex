# JellyGarden adapter: where to hook the kit

For projects built on the JellyGarden match-3 code base (`Item`, `Square`, `LevelManager` in
`JellyGarden.Scripts`). The adapter uses only members that exist in the original JellyGarden
source, so it compiles in copies that other people have edited. It has no assembly definition,
so it compiles into `Assembly-CSharp` beside the game code.

## 1. Install

1. **Package Manager → + → Add package from disk…** and choose the kit's `package.json`.
2. In the kit's **Samples** tab, import **JellyGarden Adapter**. Also import **Royal Throne Art and SFX Pack** if you want the Royal Throne look.
3. Optional: **Tools → Royal Throne → Match-3 VFX Kit → Create Settings Asset**, then assign the art pack values to it. You can also drag the `RoyalThroneArtSettings` asset onto the bridge.

`JellyGardenVfxBridge` adds itself to the `LevelManager` GameObject after scene load. It
configures the host and board, and calls `Match3Vfx.Clear()` when the game returns to the map or
loads another level. If you add it yourself, define `MATCH3VFX_NO_AUTO_BRIDGE`.

Nothing changes visually until you add the hooks below. Add them one at a time and play a level
after each one.

## 2. Hooks

Add `using RoyalThrone.Match3Vfx;` and `using RoyalThrone.Match3Vfx.Adapters;` to each edited file.

### Match pop: `Item.JuiceDestroyCor()`

Replace the body of the coroutine (the anticipation, squash, rebound and break code) with:

```csharp
yield return Match3PieceMotion.Pop(transform, sprRenderer, new Match3PopOptions
{
    kind = JellyGardenVfxBoard.TakePopKind(this),    // Rocket / PowerClear when a kit power-up hit it
    burstColor = GetJuiceColor(),
    colorIndex = color,
    intensity = juiceIntensity,
    stagger = juiceStagger,
    restScale = juiceBaseScale,
    onFinished = SetAnimationDestroyingFinished         // unblocks the cascade exactly as before
});
```

On TIME levels, pass `renderers` without the "5sec" bonus flyer so it is not hidden.

### Group accent: `LevelManager`, where each destroyed group is handled

Put this where the old code computed `groupIntensity` and called `MatchVFXService`:

```csharp
var positions = desrtoyItems.Where(i => i).Select(i => i.transform.position).ToList();
Item mid = desrtoyItems[desrtoyItems.Count / 2];
float groupIntensity = Match3Vfx.PlayMatchGroup(positions, mid ? mid.GetJuiceColor() : Color.white, desrtoyItems.Count, combo);
// then for each item, before DestroyItem:
item.juiceIntensity = groupIntensity;
item.juiceStagger = Match3Vfx.StaggerFor(juiceIndex++);
```

### Swap and rejected swap: `Item.Switching()`

```csharp
// valid
yield return Match3PieceMotion.Swap(transform, switchItem ? switchItem.transform : null, myTarget, startPos);
// rejected: evaluate the rule first, then
yield return Match3PieceMotion.InvalidSwap(transform, switchItem.transform, startPos, otherStart);
```

### Fall and landing: `Item.FallingCor()`

```csharp
bool landed = false;
yield return Match3PieceMotion.Fall(transform, endPos, () => landed = true);
if (landed && animate)
{
    StartCoroutine(Match3PieceMotion.LandingSquash(transform, juiceBaseScale, () => destroying));
    SoundBase.Instance.PlaySound(SoundBase.Instance.drop[Random.Range(0, SoundBase.Instance.drop.Length)]);
}
```

### Blocker chip: `Square.DestroyBlock()`

```csharp
var visual = currentBlock.GetComponent<SpriteRenderer>();
Match3Vfx.PlayChip(transform.position);
Match3Vfx.Shatter(visual, 0.75f, Match3ShatterStyle.PowerClear);
visual.enabled = false;
```

### Power-ups: `Item.DestroyCor()` and the swap

Power-ups clear cells over time through `JellyGardenVfxBoard.HitCell`, which calls the game's
own `DestroyItem` / `DestroyBlock`. Route the legacy immediate clears to the kit instead of running
both:

```csharp
// a striped / package / bomb (or HELICOPTER) item is destroyed:
if (JellyGardenVfxBridge.IsPower(currentType)) JellyGardenVfxBridge.Activate(this, orbColor);
// instead of DestroyHorizontal(), DestroyVertical(), DestroyPackage(), DestroyColor(...)

// two power-ups swapped together:
JellyGardenVfxBridge.Combine(this, switchItem, (cell, power) => { /* optional: convert orb targets */ });
```

A power-up destroyed by another power-up's hit activates in turn, so chains work like Royal Throne.

**Before refilling**, wait for delayed hits in the resolve loop:

```csharp
while (Match3Vfx.Busy) yield return null;
```

### Power-up created: `Item.ChangeTypeCor()`

```csharp
StartCoroutine(Match3PieceMotion.Creation(transform, juiceBaseScale));
```

### Combo banner: end of the resolve loop

```csharp
if (combo > 2 && gameStatus == GameState.Playing) Match3Vfx.ShowComboBanner(combo);
```

## 3. Project-specific flags

If your copy has extra fields (for example `royalRocketClear`), set them without editing the adapter:

```csharp
JellyGardenVfxBoard.BeforeItemHit += (item, source) => { /* item.royalRocketClear = source == Match3HitSource.Rocket; */ };
```

## 4. Character reactions and music

```csharp
Match3Vfx.Reaction += strength => king.React(strength);          // 0.2-1.5
Match3Vfx.MusicDuckRequested += seconds => music.Duck(seconds);  // TNT, rocket, banner
```
