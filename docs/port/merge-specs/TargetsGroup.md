# Merge spec: `Assets/JellyGarden/Scripts/Targets/TargetsGroup.cs`

Plan: §4 "`Targets/...`", Task 3.4 Step 4, D8.

## 1. Base

`origin/royal-throne` (55 lines). `origin/main` is byte-identical to the kit ancestor `a82c4df` for this file.

```
git show origin/royal-throne:Assets/JellyGarden/Scripts/Targets/TargetsGroup.cs > Assets/JellyGarden/Scripts/Targets/TargetsGroup.cs
```

## 2. KEEP-FROM-MAIN

None. Main never changed this file.

## 3. CUT-FROM-ROYAL-THRONE

| RT line | Method | Royal Throne text (exact) | Action |
|---|---|---|---|
| 28–35 | `OnEnable` | the reference-campaign branch (below) | **Delete all 8 lines (D8).** |

```csharp
            if(RoyalThrone.Campaign.RoyalReferenceCampaign.Active)
            {
                foreach(var target in LevelManager.THIS.targetObject)
                {
                    var obj=Instantiate<TargetIcon>(prefab,transform);obj.SetTarget(target);TargetIcons.Add(obj);target.guiObj=obj;
                }
                return;
            }
```

After the cut, `OnEnable` is the kit's again: `levelNum = PlayerPrefs.GetInt("OpenLevel");` followed by `var targetObj = Resources.Load<TargetLevel>("Targets/Level" + levelNum);`.

**Keep from Royal Throne:** line 1, `using PlayerPrefs = RoyalThrone.Testing.RoyalSave;`. `RoyalSave` is imported in Task 3.5. Outside a QA session it forwards to `UnityEngine.PlayerPrefs` with the same keys (`RoyalSave.cs`: `ReadKey` returns `key` unless `Testing`). It also keeps the QA test profile isolated. Dropping the alias is equally valid and makes the file byte-identical to main's.

## 4. D3 colour changes

None.

## 5. Dropped main hooks

None.

## 6. Risks / judgement calls

- **`guiObj` lands on the asset's objects.** As in the kit, `target.guiObj = obj` is set on the `Targets/Level<n>` asset's `TargetObject`s, not on `LevelManager.targetObject`'s live copies. Royal Throne's `LevelManager.CheckCollectedTarget` prefers `RoyalGameplayPresentation.GoalIcon` and only falls back to `guiObj`. The LevelManager spec re-adds main's `guiObj` null guard on that fallback (LevelManager.md K7).

```anchors
path: Assets/JellyGarden/Scripts/Targets/TargetsGroup.cs
rt: using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
rt: if(RoyalThrone.Campaign.RoyalReferenceCampaign.Active)
rt: var obj=Instantiate<TargetIcon>(prefab,transform);obj.SetTarget(target);TargetIcons.Add(obj);target.guiObj=obj;
rt: levelNum = PlayerPrefs.GetInt("OpenLevel");
rt: var targetObj = Resources.Load<TargetLevel>("Targets/Level" + levelNum);
absent-main: RoyalThrone
```
