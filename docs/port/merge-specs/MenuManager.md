# Merge spec: `Assets/JellyGarden/Scripts/GUI/MenuManager.cs`

Plan: §4 "`InitScript.cs`, `GUI/AnimationManager.cs`, `GUI/MenuManager.cs`", Task 3.4 Step 2, Appendix B row `GUI/MenuManager.cs`.

## 1. Base

**`origin/main`** (331 lines). Keep main's file and add one member from Royal Throne. Main's version is already in the working tree on `port/royal-throne-level`, so there is nothing to restore.

## 2. KEEP-FROM-MAIN

All of main's file stays unchanged, including the `StoryFreeGame` hooks from Appendix B:
- `ShowGemsShop`: `if (show && !JellyGarden.Scripts.Story.StoryFreeGame.ShopsAllowed) return;`
- `ShowLiveShop`: `if (show && !JellyGarden.Scripts.Story.StoryFreeGame.ShopsAllowed) return;`
- `ShowRate`: `if (show && !JellyGarden.Scripts.Story.StoryFreeGame.RatePromptAllowed) return;`
- `ShowPreFailed`: `if (show && !JellyGarden.Scripts.Story.StoryFreeGame.PreFailOfferAllowed) { StartCoroutine(EndTheLostLevel()); return; }`, plus the `EndTheLostLevel()` coroutine.

## 3. ADD-FROM-ROYAL-THRONE (the only change)

Royal Throne `MenuManager.cs` RT:45–70 adds `Showing` and `BlocksBoardInput`. Royal Throne's `LevelManager.BoardInputAllowed` and `TimerHeld` read `BlocksBoardInput`. So do `ReferenceBoosterBar` and other imported code (7 references in the import set).

**Anchor in main's file:** after the last serialized field, `[SerializeField] private GameObject[] stars;` (main:44), and before `void Awake()` (main:46). Insert:

```csharp

        static bool Showing(GameObject popup) => popup != null && popup.activeInHierarchy;

        /// <summary>
        /// True while a popup owns the screen. The board sits behind these panels and its
        /// colliders are still hit by a touch, so it has to ignore input while one is open.
        /// Every popup is a child of the global canvas, including ones this manager holds no
        /// reference to (the in-level booster shop), so the whole canvas is checked. The
        /// tutorial and the goal banners on the level canvas are excluded: play continues under them.
        /// </summary>
        public bool BlocksBoardInput
        {
            get
            {
                if (Showing(prePlay)) return true;
                if (canvasGlobal == null) return false;
                foreach (Transform child in canvasGlobal)
                {
                    if (!child.gameObject.activeSelf) continue;
                    if (tutorial != null && child.gameObject == tutorial) continue;
                    return true;
                }
                return false;
            }
        }
```

### CUT inside the added member

Royal Throne line RT:60, `if (RoyalThrone.Presentation.RoyalShopPage.IsOpen) return true;   // the shop, over a card or the board`, is **not** copied. `RoyalShopPage` is Royal Throne's coin shop page: a home-screen type, out of scope under D5, and not imported. Every other line is copied verbatim from Royal Throne. Main's file already declares the fields it uses: `canvasGlobal` (main:22), `prePlay` (main:25) and `tutorial` (main:33).

## 4. D3 colour changes

None.

## 5. Dropped main hooks

None. Every main hook stays.

## 6. Risks / judgement calls

- **Main's Story popups are not on `CanvasGlobal`.** `StoryPrePlay`, `StoryWinPopup` and `StoryLosePopup` build their own `Canvas` under their own transform (`StoryPrePlay.cs:436`, `StoryWinPopup.cs:89`, `StoryLosePopup.cs:213` on `origin/main`). So `BlocksBoardInput` does not see them. They show while `gameStatus != Playing`, and `BoardInputAllowed` already requires `Playing`, so no board input leaks through. UNVERIFIED at runtime; Task 3.8 Step 3 covers it.
- **Scene check (verified).** In both `game.unity` files, every authored child of `CanvasGlobal` is inactive by default: MenuPlay, MenuComplete, MenuPause, MenuFailed, PreFailed, BoostShop, LiveShop, GemsShop, BoostInfo, Settings, Reward and Tutorial, plus one prefab-instance child that could not be resolved (UNVERIFIED). So `BlocksBoardInput` is false during normal play on main's scene. If any main Story script later parents an always-on object under `CanvasGlobal`, all board input stops. `StoryFlow.ReportOpenMenus` (main) only reads the canvas.
- **Pause sets `Time.timeScale = 0` and shows `MenuPause` under `CanvasGlobal`.** `BlocksBoardInput` is then true, which is intended.

```anchors
path: Assets/JellyGarden/Scripts/GUI/MenuManager.cs
main: [SerializeField] private GameObject[] stars;
main: void Awake()
main: [SerializeField] private Transform canvasGlobal;
main: [SerializeField] private GameObject prePlay;
main: [SerializeField] private GameObject tutorial;
main: if (show && !JellyGarden.Scripts.Story.StoryFreeGame.ShopsAllowed)
main: if (show && !JellyGarden.Scripts.Story.StoryFreeGame.RatePromptAllowed)
main: if (show && !JellyGarden.Scripts.Story.StoryFreeGame.PreFailOfferAllowed)
absent-main: BlocksBoardInput
rt: public bool BlocksBoardInput
rt: static bool Showing(GameObject popup) => popup != null && popup.activeInHierarchy;
rt: if (RoyalThrone.Presentation.RoyalShopPage.IsOpen) return true;   // the shop, over a card or the board
rt: if (tutorial != null && child.gameObject == tutorial) continue;
```
