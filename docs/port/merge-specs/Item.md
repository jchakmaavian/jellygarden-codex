# Merge spec: `Assets/JellyGarden/Scripts/Item.cs`

Plan: §4 "`Item.cs`", Task 3.2 (Steps 1–5), Appendix A, D3, D4, D8, Task 4.7 (`AvatarEvents`).

## 1. Base

`origin/royal-throne` (1,889 lines).

```
git show origin/royal-throne:Assets/JellyGarden/Scripts/Item.cs > Assets/JellyGarden/Scripts/Item.cs
```

## 2. KEEP-FROM-MAIN

**Nothing** (plan §4: "Keep from main: nothing in-level"). Main's 1,679-line file is discarded. Main's removal of the `OnMouse*` handlers does not matter. Royal Throne's `Item.Update` (RT:1808–1852) drives press, drag and release from `InputManager` (`WasTouchPressed` / `IsTouching` / `WasTouchReleased`). Its leftover `OnMouseDown/Up/Drag` (RT:1854–1880) are never called under `activeInputHandler: 1`, as main's own comment notes (main Item.cs:1666–1670), so they are harmless dead code. Keep them as Royal Throne has them.

Main-only public members that disappear (verified by a member diff of `origin/main` against `origin/royal-throne`):
- `BeginDragFromCurrentTouch`, `PopScale`, `PopRelease`, `PopClaimForKit`;
- `IsUniversalPowerup`, `CanTapActivate`, `TapActivate`, `LandActivate`;
- `DestroyByRocket`.

Their callers on `origin/main` are all deleted or replaced by this port:
- `LevelManager.cs:932` (`BeginDragFromCurrentTouch`; dropped in LevelManager.md);
- `Square.cs:165` (`IsUniversalPowerup`; dropped in Square.md);
- `StoryOriginalBoostersValidation`, `StoryOriginalMotion`, `StoryOriginalTntValidation`, `StoryPopVfx`, `StoryPunchVfx`, `StoryPowerupTest`, `StoryRocketSweep`, `StoryRocketValidation`. All of these are on the §5 "Retired" list (Task 3.7).

## 3. CUT-FROM-ROYAL-THRONE

Royal Throne line numbers. Apply bottom-up if you edit by line number.

### 3a. Campaign (D8). `IsSpecialItem` is always false, because `RoyalCampaignItem` is not imported.

| RT line | Method | Royal Throne text (exact) | Action |
|---|---|---|---|
| 194–195 | `GenColor` | `if(RoyalThrone.Campaign.RoyalReferenceCampaign.TryColor(this,exceptColor))` / `{StartCoroutine(FallingCor(square,true));return;}` | delete both lines |
| 435 | `SwitchDirection` | `if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(this)) { ResetDrag(); return; }` | delete the line |
| 470 | `SwitchDirection` | `if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(switchItem)) { ResetDrag(); return; }` | delete the line |
| 1301–1302 | `DestroyItem` | `royalCampaignHitWave = RoyalThrone.Campaign.RoyalCampaignBlockers.CurrentWave;` / `RoyalThrone.Campaign.RoyalCampaignBlockers.OnItemDestroying(this);` | delete both lines. The field `royalCampaignHitWave` (RT:1501) stays and is always 0. |
| 1328–1330 | `DestroyCor` | `// An accepted campaign coin hit already owns its recovered particles and SFX.` / `// Release the board through the same completion path without a second fruit pop.` / `bool royalCoin = RoyalThrone.Campaign.RoyalCampaignBlockers.HasCoinPresentation(this);` | delete the 3 lines, then remove `royalCoin` from its 3 uses (next rows) |
| 1331 | `DestroyCor` | `if (royalSpecial \|\| royalConsumed \|\| royalCoin)` | `if (royalSpecial \|\| royalConsumed)` |
| 1334 | `DestroyCor` | `if (!royalConsumed && !royalCoin) RoyalPowerupDirector.Get.Queue(this);` | `if (!royalConsumed) RoyalPowerupDirector.Get.Queue(this);` |
| 1340 | `DestroyCor` | `else if (royalSpecial \|\| royalConsumed \|\| royalCoin) { }` | `else if (royalSpecial \|\| royalConsumed) { }` |
| 1417 | `DestroyCor` | `currentType == ItemsTypes.NONE && !RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(this);` | `currentType == ItemsTypes.NONE;` The call `square.DestroyBlock(royalRocketClear, royalCampaignHitWave, campaignAdjacentMatch);` (RT:1418) stays; Square keeps that signature (Square.md §3). |

### 3b. Win sequence and profile (D4; profile out of scope)

| RT line | Method | Royal Throne text (exact) | Action |
|---|---|---|---|
| 1333 | `DestroyCor` | `if (royalSpecial) RoyalWinSequence.FinaleBoosterActivated(this);` | delete. This fed coins to `RoyalWinSequence`'s Reward counter, and D4 does not port `RoyalWinSequence`. |
| 1441–1442 | `DestroyCor` | `// The profile counts the power-ups matches make while the level is played (RoyalProfile).` / `if (LevelManager.THIS && LevelManager.THIS.gameStatus == GameState.Playing && Juice.RoyalPowerupDirector.IsPower(i.currentType)) RoyalThrone.RoyalProfile.PowerupMade();` | delete both lines (`RoyalProfile` is the profile page, out of scope) |

### 3c. Retired booster versions (Task 3.2 Step 5: `Boosters.*.V2|V3|V4`)

Only **Propeller V3/V4** appear in this file. **ColorBall V4 (`ColorBallSeatV4`, RT:1190) and Propeller V5 (`PropellerSeatV5` RT:1191, `PropellerVfxV5` RT:1540) are imported (§5) and stay.**

| RT line | Royal Throne text (exact) | Action |
|---|---|---|
| 1495 | `[HideInInspector] public bool royalPropellerV3Clear;` | delete |
| 1496 | `[HideInInspector] public bool royalPropellerV4Clear;` | delete |
| 1547–1554 | the block from `if (royalPropellerV4Clear)` to its closing `}` (contains `RoyalThrone.Boosters.Propeller.V4.PropellerVfxV4.Break(sprRenderer, color);`) | delete the 8 lines |
| 1556–1564 | `// V3's precise air hit uses its own tiny chip pool. TNT/Rocket branches above are unchanged.` plus the block from `if (royalPropellerV3Clear)` to its `}` (contains `RoyalThrone.Boosters.Propeller.V3.PropellerVfxV3.Break(sprRenderer, color);`) | delete the 9 lines, and the blank line before them |

Only the non-imported `Boosters/Propeller/V3/Scripts/PropellerGameplayV3.cs` and `.../V4/Scripts/PropellerGameplayV4.cs` set those two flags (verified with grep on `origin/royal-throne`), so nothing else breaks.

### 3d. Avatar events (Task 3.2 Step 4; plan §4: `KingVanguardActivityEvents` is retargeted)

| RT line | Method | Royal Throne text (exact) | Replace with |
|---|---|---|---|
| 1184 | `ChangeTypeCor` (royal seat branch, after `if (previousType != currentType)`) | `RoyalThrone.Characters.KingVanguardActivityEvents.NotifyPowerupCreated(LevelManager.THIS, this);` | `JellyGarden.Scripts.Juice.AvatarEvents.NotifyPowerupCreated(LevelManager.THIS, this);` |
| 1243 | `ChangeTypeCor` (legacy branch, after `if (previousLegacyType != currentType)`) | same text as RT:1184 | same replacement |

The `AvatarEvents` API contract is in INDEX.md §3. Record both sites in `COUPLINGS.md` (Task 3.2 Step 4).

After §3, this must return nothing:

```
grep -nE "RoyalThrone\.(Campaign|Characters|RoyalProfile)|RoyalWinSequence|Propeller\.V[34]|royalPropellerV[34]Clear|royalCoin|BoardColor" Assets/JellyGarden/Scripts/Item.cs
```

### Keep from Royal Throne (plan §4)

- **Swap pipeline:** `SwitchDirection`, `Switching`, `SwitchingCore`, `TrySwap`, `CanAcceptInput`, and tap → `RoyalPowerupDirector.Tap`.
- **`DestroyItem`:** `square.Reserve(this)` (RT:1304) and `DestroyAfterInputSwap` deferral.
- **`DestroyCor`:** `RoyalPowerupDirector.Get.Queue`, `RoyalSpecialFormation.Gather`, `square.Release(this)` + `GenItem(..., campaignRefill: false)` (RT:1435–1436) and `RoyalSpecialCreationFlash.Attach`.
- **`ChangeTypeCor` royal seat:** `ReferencePieceVisual.Attach`, `TNT3DAnimator.RevealSprite`, `ColorBallSeatV4`, `PropellerSeatV5`, `RoyalRocketCreation`.
- **Motion:** `FallingCor`, `FallDistanceAt`, `LandingSquashCor`, `JuiceDestroyCor`, `StopIdleAnim`.
- **Input:** `Item.Update`'s press/drag/release path (RT:1808–1852, gated by `CanAcceptInput` and `MenuManager.BlocksBoardInput`). `RoyalSave.BlockGameplayInput` guards the dead `OnMouse*` handlers (RT:1856, 1866, 1875) and, through `InputManager`, the live path. `RoyalSave` is imported.
- **The enum value `ItemsTypes.HELICOPTER`** at the end of `ItemsTypes` (RT:33, Fact 4).

## 4. D3 colour changes (Task 3.2 Step 3)

| RT line | Where | Royal Throne text (exact) | Replace with |
|---|---|---|---|
| 86 | `color` setter | `COLOR = RoyalGameplayArt.BoardColor(value);` | `COLOR = value;` |
| 200 | `GenColor` | `int paletteSize = Mathf.Clamp(LevelManager.Instance.colorLimit, 1, RoyalGameplayArt.BoardColorCount);` | `int paletteSize = Mathf.Clamp(LevelManager.Instance.colorLimit, 1, items.Length);` |

Notes:
- The upper bound `items.Length` matches what `GenColor` indexes later: `sprRenderer.sprite = items[randColor];` (RT:270). The kit prefab ships 6 gem sprites. A level's `COLOR LIMIT` of 3–6 therefore passes through unchanged. LevelManager.md §4 removes the matching clamp at parse time.
- After these two edits the file has no `RoyalGameplayArt.BoardColor*` reference. The constant `RoyalGameplayArt.BoardColorCount` and `BoardColor()` stay in `Juice/RoyalGameplayArt.cs` until Task 4.2 makes them level-driven and identity.

## 5. Dropped main hooks

Main's `Item.cs` is not taken, so all of these simply do not exist after the merge:
- **Appendix A:** `StoryOriginalColorBall.Fire/.Tappable`, `StoryOriginalTnt.*` (`OwnsBlast`, `ClearArea`, `ComboAnticipation`, `Show`, `Ignite`, `BeginWave`, `EndWave`, `WaveDelay`, `SingleRadius`, `DualRadius`, `Ready`, `InCombo`), `StoryOriginalBoosters.*` (`Ready`, `ShowPiece`, `OwnsRocket`, `OwnsColorBall`, `ColorBallConvert`, `MatchBreak`, `TntTnt`, `RocketTnt`, `DualColorBall`, `ColorBall`), `StoryOriginalMotion.Ready/.Fall/.Break/.Silence`, `StoryRocketVfx.Enabled/.Contact`, `StoryRocketSweep.Show/.Hit/.HitObstacle`, `StoryBombVfx.Blow/.TintFor`, `StoryColorBombVfx.Fire`, `StoryRoyalVfx.TntReady`, `StoryPunchVfx.Punch`, `StorybookBoardArt.Apply` (in `Awake`), `Match3PieceMotion.Pop`.
- **Not in Appendix A but also in main's `Item.cs`:** `StoryBoardScale.Pitch` (verified by grep on `origin/main`).
- **Universal-powerup rules:** `IsUniversalPowerup`, `LandingPowerup`, `CanLandFire`, `CanTapActivate`, `TapActivate`, `LandActivate`.
- **Other:** `DestroyByRocket`, `PopScale`/`PopRelease`/`PopClaimForKit`, and the drag threshold (`pressScreenPos`, `DragThresholdPixels`, `BeginDragFromCurrentTouch`).

## 6. Risks / judgement calls

- **The sixth colour shares art until Task 4.2.** Gameplay keeps colour 5 distinct after the §4 edits. But `RoyalPieceSkin.For` and `RoyalGameplayArt.For` (Juice; RT:1167 `appeared = true; sprRenderer.sprite = RoyalPieceSkin.For(currentType, color);`) still map colour 5 to colour 1's art through `RoyalGameplayArt.BoardColor`. So from Task 4.1 (switch on) until Task 4.2, colour 5 gems look like colour 1 gems while matching separately. Do Task 4.2 right after 4.1, or do not play `COLOR LIMIT 6` levels in between.
- **`RoyalPieceSkin` at runtime (Task 3.6 Step 2, forward note).** Royal Throne's prefab carries the skin component; main's prefab does not, and must not be edited. `royalSkin = GetComponent<Juice.RoyalPieceSkin>();` (RT:152, `Awake`) is therefore null and every `royalSkin?.ApplyNow()` is skipped. That is correct for Phase 3. In Task 3.6, change RT:152 to:

  ```csharp
              royalSkin = GetComponent<Juice.RoyalPieceSkin>();
              if (!royalSkin && Juice.RoyalEffectsProfile.Get) royalSkin = gameObject.AddComponent<Juice.RoyalPieceSkin>();
  ```

  The component has no serialized fields on Royal Throne's `Item.prefab` (it is `m_EditorClassIdentifier: Assembly-CSharp::JellyGarden.Scripts.Juice.RoyalPieceSkin` with no data), so adding it at runtime is equivalent.
- **Compile unit.** This file uses Royal Throne's `LevelManager` members (`InputSwaps`, `BeginInputSwap`/`EndInputSwap`, `PreferredMatchHost`, `royalSpecialGather` consumers), `Square.Reserve`/`Release`/`Reserved`, and many `Juice/**`, `ReferenceGameplay/**` and `Boosters/*/V4|V5/**` types. It only compiles after Tasks 3.1, 3.3 and 3.5 (Phase 3 is one compile unit).
- **`AvatarEvents` must exist before this compiles** (Task 3.2 Step 4). See INDEX.md §3 for the contract.

```anchors
path: Assets/JellyGarden/Scripts/Item.cs
rt: COLOR = RoyalGameplayArt.BoardColor(value);
rt: if(RoyalThrone.Campaign.RoyalReferenceCampaign.TryColor(this,exceptColor))
rt: {StartCoroutine(FallingCor(square,true));return;}
rt: int paletteSize = Mathf.Clamp(LevelManager.Instance.colorLimit, 1, RoyalGameplayArt.BoardColorCount);
rt: sprRenderer.sprite = items[randColor];
rt: if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(this)) { ResetDrag(); return; }
rt: if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(switchItem)) { ResetDrag(); return; }
rt: if (previousType != currentType)
rt: if (previousLegacyType != currentType)
rt: RoyalThrone.Characters.KingVanguardActivityEvents.NotifyPowerupCreated(LevelManager.THIS, this);
rt: if (orb) RoyalThrone.Boosters.ColorBall.V4.ColorBallSeatV4.Attach(this, true);
rt: if (createdType == ItemsTypes.HELICOPTER) RoyalThrone.Boosters.Propeller.V5.PropellerSeatV5.Attach(this, true);
rt: royalCampaignHitWave = RoyalThrone.Campaign.RoyalCampaignBlockers.CurrentWave;
rt: RoyalThrone.Campaign.RoyalCampaignBlockers.OnItemDestroying(this);
rt: if (NextType != ItemsTypes.NONE) square.Reserve(this);   // a power-up will appear here: see Square.reservedFor
rt: bool royalCoin = RoyalThrone.Campaign.RoyalCampaignBlockers.HasCoinPresentation(this);
rt: if (royalSpecial || royalConsumed || royalCoin)
rt: if (royalSpecial) RoyalWinSequence.FinaleBoosterActivated(this);
rt: if (!royalConsumed && !royalCoin) RoyalPowerupDirector.Get.Queue(this);
rt: else if (royalSpecial || royalConsumed || royalCoin) { }
rt: currentType == ItemsTypes.NONE && !RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(this);
rt: square.DestroyBlock(royalRocketClear, royalCampaignHitWave, campaignAdjacentMatch);
rt: Item i = square.GenItem(!rocket && !(RoyalEffectsProfile.Get && NextType == ItemsTypes.PACKAGE), campaignRefill: false);
rt: // The profile counts the power-ups matches make while the level is played (RoyalProfile).
rt: if (LevelManager.THIS && LevelManager.THIS.gameStatus == GameState.Playing && Juice.RoyalPowerupDirector.IsPower(i.currentType)) RoyalThrone.RoyalProfile.PowerupMade();
rt: [HideInInspector] public bool royalPropellerV3Clear;
rt: [HideInInspector] public bool royalPropellerV4Clear;
rt: [HideInInspector] public int royalCampaignHitWave;
rt: if (royalPropellerV4Clear)
rt: RoyalThrone.Boosters.Propeller.V4.PropellerVfxV4.Break(sprRenderer, color);
rt: // V3's precise air hit uses its own tiny chip pool. TNT/Rocket branches above are unchanged.
rt: if (royalPropellerV3Clear)
rt: RoyalThrone.Boosters.Propeller.V3.PropellerVfxV3.Break(sprRenderer, color);
rt: RoyalThrone.Boosters.Propeller.V5.PropellerVfxV5.Break(sprRenderer, color);
rt: royalSkin = GetComponent<Juice.RoyalPieceSkin>();
rt: appeared = true; sprRenderer.sprite = RoyalPieceSkin.For(currentType, color);
rt: HELICOPTER
rt: using JellyGarden.Scripts.Juice;
rt: private void Update()
rt: public void OnMouseDown()
rt: if (InputManager.Instance.WasTouchPressed)
path: Assets/JellyGarden/Scripts/Item.cs
main: // OnMouseDown/OnMouseUp/OnMouseDrag used to live here and are gone.
path: Assets/JellyGarden/Scripts/LevelManager.cs
main: itemTouched.BeginDragFromCurrentTouch();
```
