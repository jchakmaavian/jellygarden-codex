# Merge spec: `Assets/JellyGarden/Scripts/Targets/TargetLevel.cs`

Plan: §4 "`Targets/...`", Task 3.4 Step 4, Appendix B row `Targets/TargetLevel.cs, Targets/TargetIcon.cs`, D3.

## 1. Base

`origin/royal-throne` (111 lines).

```
git show origin/royal-throne:Assets/JellyGarden/Scripts/Targets/TargetLevel.cs > Assets/JellyGarden/Scripts/Targets/TargetLevel.cs
```

## 2. KEEP-FROM-MAIN

### K1. Goal-art call site in `TargetObject.DeepCopy`

Main's block (`origin/main` TargetLevel.cs:48–53):

```csharp
        public TargetObject DeepCopy()
        {
            TargetObject other = (TargetObject) MemberwiseClone();
            other.icon = JellyGarden.Scripts.Story.StorybookBoardArt.Goal(other);
            return other;
        }
```

**Anchor in Royal Throne:** method `DeepCopy` (RT:50–60). Put main's `other.icon = ...` line between RT `TargetObject other = (TargetObject) MemberwiseClone();` (RT:52) and `return other;` (RT:59), replacing the D3 block below. The resulting method is main's method, verbatim, as quoted above.

Per plan Task 3.4 Step 4 and Appendix B, the call site stays. Task 4.6 reroutes `StorybookBoardArt.Goal` to `RoyalVisualCatalog.Goal`.

## 3. CUT-FROM-ROYAL-THRONE

None, apart from the D3 block in §4.

**Keep from Royal Throne:**
- the field `[NonSerialized] public string referenceGoal;` (RT:43–44: `// Set only on runtime copies loaded by the isolated reference campaign.`);
- the guard `if(!string.IsNullOrEmpty(referenceGoal))return false;` at the top of `SetOff` (RT:67).

Plan §4 says "Royal Throne's `TargetObject.referenceGoal`". `RoyalVisualCatalog.Goal` reads it (`if(!string.IsNullOrEmpty(target.referenceGoal))return target.icon;`). With the campaign cut (D8) it is always null, so `SetOff` behaves as the kit.

## 4. D3 colour changes

Royal Throne remaps colour 5 to colour 1 in runtime copies. **Delete RT:53–58:**

```csharp
            // Migrate runtime copies only; retain authored assets and player progress.
            if (other.type == Target.COLLECT && other.color == 5)
            {
                other.color = Juice.RoyalGameplayArt.BoardColor(other.color);
                other.icon = Juice.RoyalGameplayArt.For(ItemsTypes.NONE, other.color) ?? other.icon;
            }
```

Replace it with main's single line (K1). A `COLLECT` goal for colour 5 now stays colour 5, which matches the sixth gem the board generates under D3 (see Item.md §4).

## 5. Dropped main hooks

None. Main's only hook here (K1) is kept. Appendix B lists "`StorybookBoardArt.Goal` as the art source" as dropped; that happens in Task 4.6, not in this merge.

## 6. Risks / judgement calls

- **Icon names drive goal matching.** `SetOff` for `INGREDIENT` compares `item.sprRenderer.sprite.name == icon.name`, using the live copy's icon. `Item.GenColor` paints ingredients with `targetObject.icon` from the same live copy, so the names agree whatever art `StorybookBoardArt.Goal` returns. This is main's existing behaviour.
- **TargetIcon finds the live copy by icon name.** With K1, live copies carry main's goal art, not the authored icon. Royal Throne's `TargetIcon.SetTarget` lookup (`t.icon.name==target.icon.name`) would then miss the copy. TargetIcon.md adjusts that lookup; merge the two files together.
- **Task 4.6 must change `DeepCopy` and `TargetIcon` together.** When it switches to `RoyalVisualCatalog.Goal`, the same consistency is needed. `RoyalVisualCatalog.Replace` already keeps `sprite.name=original.name` for this reason.

```anchors
path: Assets/JellyGarden/Scripts/Targets/TargetLevel.cs
rt: public TargetObject DeepCopy()
rt: TargetObject other = (TargetObject) MemberwiseClone();
rt: // Migrate runtime copies only; retain authored assets and player progress.
rt: if (other.type == Target.COLLECT && other.color == 5)
rt: other.color = Juice.RoyalGameplayArt.BoardColor(other.color);
rt: other.icon = Juice.RoyalGameplayArt.For(ItemsTypes.NONE, other.color) ?? other.icon;
rt: [NonSerialized] public string referenceGoal;
rt: if(!string.IsNullOrEmpty(referenceGoal))return false;
main: other.icon = JellyGarden.Scripts.Story.StorybookBoardArt.Goal(other);
```
