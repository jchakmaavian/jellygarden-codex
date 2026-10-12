# Merge spec: `Assets/JellyGarden/Scripts/Targets/TargetIcon.cs`

Plan: §4 "`Targets/...`", Task 3.4 Step 4, Appendix B row `Targets/TargetLevel.cs, Targets/TargetIcon.cs`.

## 1. Base

`origin/royal-throne` (108 lines).

```
git show origin/royal-throne:Assets/JellyGarden/Scripts/Targets/TargetIcon.cs > Assets/JellyGarden/Scripts/Targets/TargetIcon.cs
```

## 2. KEEP-FROM-MAIN

### K1. Goal-art call site in `SetTarget`

Main's block (`origin/main` TargetIcon.cs:43–50):

```csharp
        public void SetTarget(TargetObject target)
        {
            checkObject.gameObject.SetActive(false);
            imageRenderer.sprite = target.icon;
            // Match the live copy's art; the lazy target lookup below uses its sprite name.
            imageRenderer.sprite = JellyGarden.Scripts.Story.StorybookBoardArt.Goal(target);
            count = target.targetCount;
```

**Anchor in Royal Throne:** method `SetTarget` (RT:43). In Royal Throne, the start of the method is RT:45–50:

```csharp
            // Cache the data reference before replacing the visual sprite.
            targetObject=LevelManager.THIS&&LevelManager.THIS.targetObject!=null
                ? LevelManager.THIS.targetObject.FirstOrDefault(t=>t.type==target.type&&t.color==target.color&&t.icon&&target.icon&&t.icon.name==target.icon.name)??target
                : target;
            checkObject.gameObject.SetActive(false);
            imageRenderer.sprite = RoyalThrone.Presentation.RoyalVisualCatalog.Goal(target);
```

**Replace RT:45–50 with:**

```csharp
            // Main's goal art (Task 4.6 reroutes StorybookBoardArt.Goal to RoyalVisualCatalog.Goal).
            // TargetObject.DeepCopy gives the live copies the same art, so the live copy is found by
            // that art's name as well as by the authored icon's name.
            var goalArt = JellyGarden.Scripts.Story.StorybookBoardArt.Goal(target);
            // Cache the data reference before replacing the visual sprite.
            targetObject=LevelManager.THIS&&LevelManager.THIS.targetObject!=null
                ? LevelManager.THIS.targetObject.FirstOrDefault(t=>t.type==target.type&&t.color==target.color&&t.icon&&target.icon&&
                    (t.icon.name==target.icon.name||goalArt&&t.icon.name==goalArt.name))??target
                : target;
            checkObject.gameObject.SetActive(false);
            // Match the live copy's art; the lazy target lookup below uses its sprite name.
            imageRenderer.sprite = goalArt;
```

Royal Throne's lookup is kept, with one extra disjunct. Royal Throne's `RoyalVisualCatalog.Goal(target)` is replaced by main's `StorybookBoardArt.Goal(target)` (Task 3.4 Step 4: "Keep main's goal-art call site; it is rerouted in Task 4.6").

## 3. CUT-FROM-ROYAL-THRONE

| RT line | Royal Throne text | Action |
|---|---|---|
| 50 | `imageRenderer.sprite = RoyalThrone.Presentation.RoyalVisualCatalog.Goal(target);` | Replaced by main's call (K1). `RoyalVisualCatalog` is still imported (§5 GUI subset) and used by the HUD; only this call site changes. |

## 4. D3 colour changes

None here. The colour-5 remap lives in `TargetLevel.DeepCopy` (see TargetLevel.md §4).

## 5. Dropped main hooks

None in this merge.

## 6. Risks / judgement calls

- **Why the lookup needs the extra disjunct.**
  - Main's `TargetObject.DeepCopy` (kept, TargetLevel.md K1) sets the live copy's `icon = StorybookBoardArt.Goal(copy)`.
  - `SetTarget` receives the *asset's* object from `Resources.Load<TargetLevel>("Targets/Level"+n)` in `TargetsGroup.OnEnable`, and that object still has the authored icon.
  - Royal Throne's original lookup compares the copy's icon name with the authored icon name. When `StorybookBoardArt.Enabled` is true these never match, so `targetObject` fell back to the asset object. The badge would then count the asset's `countCollected` (always 0), not the live goal.
  - `StorybookBoardArt.Goal` is a pure function of `type`, `color` and `icon.name`, so `Goal(asset).name == copy.icon.name`. When the art is disabled, `Goal` returns `target.icon` and the new term repeats the old one.
- **Order dependence (inherited from Royal Throne).** If `TargetsGroup.OnEnable` runs before `LevelManager.LoadDataFromLocal` has filled `targetObject`, the lookup falls back to the asset object (`??target`) and never retries. Main's comment in `LevelManager.CheckCollectedTarget` documents the same ordering issue. Task 4.6 should verify the badge counts on level 1 and an ingredient level. UNVERIFIED.
- **Alternative (not default):** keep Royal Throne's `RoyalVisualCatalog.Goal(target)` now and skip Task 4.6's TargetIcon step. That is safe only if `TargetLevel.DeepCopy` also drops main's `StorybookBoardArt.Goal` call. Otherwise the popup and HUD art differ until Task 4.6.

```anchors
path: Assets/JellyGarden/Scripts/Targets/TargetIcon.cs
rt: public void SetTarget(TargetObject target)
rt: // Cache the data reference before replacing the visual sprite.
rt: targetObject=LevelManager.THIS&&LevelManager.THIS.targetObject!=null
rt: ? LevelManager.THIS.targetObject.FirstOrDefault(t=>t.type==target.type&&t.color==target.color&&t.icon&&target.icon&&t.icon.name==target.icon.name)??target
rt: imageRenderer.sprite = RoyalThrone.Presentation.RoyalVisualCatalog.Goal(target);
rt: count = target.targetCount;
rt: targetObject = LevelManager.THIS.targetObject.First(i => i.icon.name == imageRenderer.sprite.name);
main: imageRenderer.sprite = JellyGarden.Scripts.Story.StorybookBoardArt.Goal(target);
main: // Match the live copy's art; the lazy target lookup below uses its sprite name.
```
