# Merge spec: `Assets/JellyGarden/Scripts/InputManager.cs`

Plan: §4 "`InputManager.cs`", Task 3.4 Step 1.

## 1. Base

`origin/royal-throne` (304 lines).

```
git show origin/royal-throne:Assets/JellyGarden/Scripts/InputManager.cs > Assets/JellyGarden/Scripts/InputManager.cs
```

## 2. KEEP-FROM-MAIN

Main's only change against `a82c4df` is the press-position fix (`git diff a82c4df origin/main -- Assets/JellyGarden/Scripts/InputManager.cs`):

```csharp
    private void OnTouchPerformed(InputAction.CallbackContext context)
    {
        isTouching = true;
        wasTouchPressed = true;
        // The press and the position are separate actions and the Input System
        // does not promise which one it dispatches first. When the press lands
        // first, lastPosition is still the previous touch's — so whatever reads
        // TouchPosition on the press frame anchors to the wrong point. Reading
        // the position action here makes the press frame correct either way.
        if (positionAction != null)
            lastPosition = positionAction.ReadValue<Vector2>();
    }
...
    private void OnPanPerformed(InputAction.CallbackContext context)
    {
        isTouching = true;
        wasTouchPressed = true;
        // Same ordering problem as OnTouchPerformed, on the map's pan action.
        if (panPositionAction != null)
            lastPosition = panPositionAction.ReadValue<Vector2>();
    }
```

**Coverage check (Task 3.4 Step 1).** Royal Throne covers this.
- Both handlers call `TrackPressPointer(context)` (RT:264 and RT:291).
- `TrackPressPointer` writes `lastPosition = pointer.position.ReadValue()` from the device that pressed (RT:267–274).
- `TouchPosition` now reads that pointer directly: `public Vector2 TouchPosition => pressPointer != null && pressPointer.added ? pressPointer.position.ReadValue() : lastPosition;` (RT:42).

So the press frame never reads a stale position. **Default: do not re-apply main's lines.**

One case is not covered: a press whose `context.control.device` is not a `Pointer`. In that case `TrackPressPointer` writes nothing. To make the file a strict superset, append this `else` branch to `TrackPressPointer`. It is optional and low value, because every binding in the shipped action asset is a pointer or touch control (UNVERIFIED for any custom bindings):

```csharp
    private void TrackPressPointer(InputAction.CallbackContext context)
    {
        if (context.control?.device is Pointer pointer)
        {
            pressPointer = pointer;
            lastPosition = pointer.position.ReadValue();
        }
        else if (positionAction != null)                       // main's press-position fix, non-pointer devices
            lastPosition = positionAction.ReadValue<Vector2>();
    }
```

Anchor: method `TrackPressPointer`, after the RT line `lastPosition = pointer.position.ReadValue();` and its closing `}`.

## 3. CUT-FROM-ROYAL-THRONE

None. The only external type is `RoyalThrone.Testing.RoyalSave.BlockGameplayInput` (RT:41–44). `RoyalSave.cs` is imported in Task 3.5, and outside a QA session `BlockGameplayInput` is `false`.

**Keep from Royal Throne:**
- `ownedActions` (a per-instance copy of the action asset, RT:22, RT:140–143, RT:251);
- `pressPointer` and `TrackPressPointer`;
- `DebugKeysEnabled`: debug map only in the Editor or debug builds (RT:201, RT:209–213).

## 4. D3 colour changes

None.

## 5. Dropped main hooks

Main's two `lastPosition = ...ReadValue<Vector2>()` lines in `OnTouchPerformed` and `OnPanPerformed`. Royal Throne's `TrackPressPointer` replaces them; see §2.

## 6. Risks / judgement calls

- `IsTouching`, `WasTouchPressed` and `WasTouchReleased` are now false while a QA panel is visible (`RoyalSave.BlockGameplayInput`). The code is behind `#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !ROYAL_DISABLE_QA` inside `RoyalSave`, and no effect is expected outside QA.
- `Instantiate(inputActionAsset)` means each `InputManager` owns a copy. Main code that subscribes to `InputManager.Instance.inputActionAsset` actions keeps working, because the field is reassigned to the copy before the maps are looked up (RT:142). UNVERIFIED: whether any main Story script cached the original asset reference before `Awake` ran. A search of `origin/main` Story code for `inputActionAsset` is advised during the merge.
- Debug keys stop working in non-development player builds. That is intentional (Royal Throne's comment at RT:196–200).

```anchors
path: Assets/JellyGarden/Scripts/InputManager.cs
rt: private void TrackPressPointer(InputAction.CallbackContext context)
rt: lastPosition = pointer.position.ReadValue();
rt: TrackPressPointer(context);
rt: public Vector2 TouchPosition => pressPointer != null && pressPointer.added ? pressPointer.position.ReadValue() : lastPosition;
rt: public static bool DebugKeysEnabled => Application.isEditor || UnityEngine.Debug.isDebugBuild;
rt: ownedActions=Instantiate(inputActionAsset);
main: lastPosition = positionAction.ReadValue<Vector2>();
main: lastPosition = panPositionAction.ReadValue<Vector2>();
```
