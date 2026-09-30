using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
// // ©2015 - 2026 Candy Smith
// // All rights reserved
// // Redistribution of this software is strictly not allowed.
// // Copy of this software can be obtained from unity asset store only.
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// // THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.MapScripts;
using JellyGarden.Scripts.System;
using JellyGarden.Scripts.Targets;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace JellyGarden.Scripts
{
    public class SquareBlocks
    {
        public SquareTypes block;
        public SquareTypes obstacle;

    }

    public enum GameState
    {
        Map,
        PrepareGame,
        Playing,
        Highscore,
        GameOver,
        Pause,
        PreWinAnimations,
        Win,
        WaitForPopup,
        WaitAfterClose,
        BlockedGame,
        Tutorial,
        PreTutorial,
        WaitForPotion,
        PreFailed,
        RegenLevel
    }


    public class LevelManager : MonoBehaviour
    {

        public static LevelManager THIS;
        public static LevelManager Instance;
        public Item itemPrefab;
        public GameObject squarePrefab;
        public Sprite squareSprite;
        public Sprite squareSprite1;
        public Sprite outline1;
        public Sprite outline2;
        public Sprite outline3;
        public GameObject blockPrefab;
        public GameObject wireBlockPrefab;
        public GameObject solidBlockPrefab;
        public GameObject undesroyableBlockPrefab;
        public GameObject thrivingBlockPrefab;
        public LifeShop lifeShop;
        public Transform GameField;
        public bool enableInApps;
        public int maxRows = 9;
        public int maxCols = 9;
        public float squareWidth = 1.2f;
        public float squareHeight = 1.2f;
        public Vector2 firstSquarePosition;
        public Square[] squaresArray;
        List<List<Item>> combinedItems = new List<List<Item>>();
        public Item lastDraggedItem;
        public Item lastSwitchedItem;
        public List<Item> destroyAnyway = new List<Item>();
        public GameObject popupScore;
        public int scoreForItem = 10;
        public int scoreForBlock = 100;
        public int scoreForWireBlock = 100;
        public int scoreForSolidBlock = 100;
        public int scoreForThrivingBlock = 100;
        public int scoreForIngredients = 100;
        public LIMIT limitType;
        public int Limit = 30;
        public int TargetScore = 1000;
        public int currentLevel = 1;
        public int FailedCost;
        public int ExtraFailedMoves = 5;
        public int ExtraFailedSecs = 30;
        public List<GemProduct> gemsProducts = new List<GemProduct>();
        public string[] InAppIDs;
        public string GoogleLicenseKey;
        LineRenderer line;
        public bool thrivingBlockDestroyed;
        List<List<Item>> newCombines;
        private bool dragBlocked;
        public int BoostColorfullBomb;
        public int BoostPackage;
        public int BoostStriped;
        public bool BoostHandActivated;
        public bool BoostBombActivated;
        public bool BoostReplacingActivated;
        public BoostIcon emptyBoostIcon;
        public BoostIcon AvctivatedBoostView;
        public BoostIcon activatedBoost;
        public string androidSharingPath;
        public string iosSharingPath;

        public BoostIcon ActivatedBoost
        {
            get
            {
                if (activatedBoost == null)
                {
                    //BoostIcon bi = new BoostIcon();
                    //bi.type = BoostType.None;
                    return emptyBoostIcon;
                }
                else
                    return activatedBoost;
            }
            set
            {
                if (value == null)
                {
                    if (activatedBoost != null && gameStatus == GameState.Playing)
                        InitScript.Instance.SpendBoost(activatedBoost.type);
                    UnLockBoosts();
                }
                //        if (activatedBoost != null) return;
                activatedBoost = value;

                if (value != null)
                {
                    LockBoosts();
                }

                if (activatedBoost != null)
                {
                    if (activatedBoost.type == BoostType.ExtraMoves || activatedBoost.type == BoostType.ExtraTime)
                    {
                        if (LevelManager.Instance.limitType == LIMIT.MOVES)
                            LevelManager.THIS.Limit += 5;
                        else
                            LevelManager.THIS.Limit += 30;

                        RoyalThrone.Characters.KingVanguardActivityEvents.NotifyExtraMovesGranted(this);

                        ActivatedBoost = null;
                    }

                }
            }
        }

        SquareBlocks[] levelSquaresFile = new SquareBlocks[81];
        public int targetBlocks;

        public ItemAnimEvents[] itemExplPool = new ItemAnimEvents[20];
        public static int Score;
        public int stars;
        /// <summary>Stars this win added to the Area balance: 1 on a level's first win, 0 on a replay (set by CommitWin).</summary>
        public int StarsGained { get; private set; }
        // A win's lasting effects are paid once per attempt (CommitWin), the moment its goals are met.
        bool winCommitted; int committedCoins, committedCoinsBefore;
        private int linePoint;
        public int star1;
        public int star2;
        public int star3;
        public bool showPopupScores;

        public GameObject stripesEffect;
        public GameObject star1Anim;
        public GameObject star2Anim;
        public GameObject star3Anim;
        public GameObject snowParticle;
        public Color[] scoresColors;
        public Color[] scoresColorsOutline;
        public int colorLimit;
        public int[] ingrCountTarget = new int[2];
        public Ingredients[] ingrTarget = new Ingredients[2];
        public CollectItems[] collectItems = new CollectItems[2];
        public Sprite[] ingrediendSprites;
        public string[] targetDiscriptions;
        public GameObject ingrObject;
        public GameObject blocksObject;
        public GameObject scoreTargetObject;
        bool ingredientFly;
        public GameObject[] gratzWords;

        public GameObject Level;
        public GameObject LevelsMap;

        public BoostIcon[] InGameBoosts;
        public int passLevelCounter;

        public Target target;

        public int TargetBlocks
        {
            get
            {
                return targetBlocks;
            }
            set
            {
                if (targetBlocks < 0)
                    targetBlocks = 0;
                targetBlocks = value;
            }
        }

        public bool DragBlocked
        {
            get
            {
                return dragBlocked;
            }
            set
            {
                if (value)
                {
                    List<Item> items = GetItems();
                    foreach (Item item in items)
                    {
                        //if (item != null)
                        //    item.anim.SetBool("stop", true);
                    }
                }
                else
                {
                    //  StartCoroutine( StartIdleCor());
                }
                dragBlocked = value;
            }
        }

        /// <summary>
        /// Resolution locks board maintenance, not untouched pieces. Item.CanAcceptInput
        /// protects individual swaps, falling pieces and already selected matches.
        /// </summary>
        public bool BoardInputAllowed =>
            !RoyalThrone.ReferenceGameplay.ReferenceBoosterBar.OwnsInput &&
            gameStatus == GameState.Playing && Limit > 0 && (!DragBlocked || royalResolving || InputSwaps > 0) &&
            (MenuManager.Instance == null || !MenuManager.Instance.BlocksBoardInput);

        public int InputSwaps { get; private set; }
        int inputRevision;
        public void BeginInputSwap() { InputSwaps++; DragBlocked = true; }
        public void EndInputSwap() { InputSwaps = Mathf.Max(0, InputSwaps - 1); if (InputSwaps == 0 && !royalResolving) DragBlocked = false; }
        public Item PreferredMatchHost(List<Item> group)
        {
            Item host = null;
            foreach (var piece in group)
                if (piece && piece.inputMoveOrder > 0 && (!host || piece.inputMoveOrder > host.inputMoveOrder)) host = piece;
            return host ? host : group.Contains(lastDraggedItem) ? lastDraggedItem : group.Contains(lastSwitchedItem) ? lastSwitchedItem : null;
        }

        private GameState GameStatus;
        public bool itemsHided;
        public int moveID;
        public int lastRandColor;
        public bool onlyFalling;
        public bool levelLoaded;
        [global::System.NonSerialized] public Hashtable countedSquares;
        public Sprite doubleBlock;
        public bool FacebookEnable;
        internal int latstMatchColor;
        [global::System.NonSerialized] public CombineManager combineManager;
        public TargetObject[] targetObject;
        Item itemTouched;
        [SerializeField]
        private ItemAnimEvents itemExplosionPrefab;

        private Transform canvasScore;
        private Transform canvasGlobal;

        private bool preWinAnimationRunning = false;

        #region EVENTS

        public delegate void GameStateEvents();

        public static event GameStateEvents OnMapState;
        public static event GameStateEvents OnEnterGame;
        public static event GameStateEvents OnLevelLoaded;
        public static event GameStateEvents OnMenuPlay;
        public static event GameStateEvents OnMenuComplete;
        public static event GameStateEvents OnStartPlay;
        public static event GameStateEvents OnWin;
        public static event GameStateEvents OnLose;

        public GameState gameStatus
        {
            get
            {
                return GameStatus;
            }
            set
            {
                // Repeated completion notifications must not pay the same win twice.
                if (value == GameState.Win && GameStatus == GameState.Win) return;
                GameStatus = value;
                InitScript.Instance.CheckAdsEvents(value);

                if (value == GameState.PrepareGame)
                {
                    passLevelCounter++;
                    MusicBase.Instance.Stop();
                    MusicBase.Instance.PlayMusic(1);
                    PrepareGame();
                }
                else if (value == GameState.WaitForPopup)
                {

                    InitLevel();
                    OnLevelLoaded();


                }
                else if (value == GameState.PreFailed)
                {
                    MenuManager.Instance.ShowPreFailed(true);

                }
                else if (value == GameState.Map)
                {
                    if (PlayerPrefs.GetInt("OpenLevelTest") <= 0)
                    {
                        MusicBase.Instance.Stop();
                        MusicBase.Instance.PlayMusic(0);
                        EnableMap(true);
                        OnMapState();
                    }
                    else
                    {
                        LevelManager.THIS.gameStatus = GameState.PrepareGame;
                        PlayerPrefs.SetInt("OpenLevelTest", 0);
                        PlayerPrefs.Save();
                    }
                    if (passLevelCounter > 0 && InitScript.Instance.ShowRateEvery > 0)
                    {
                        if (passLevelCounter % InitScript.Instance.ShowRateEvery == 0 && InitScript.Instance.ShowRateEvery > 0 && PlayerPrefs.GetInt("Rated", 0) == 0)
                            InitScript.Instance.ShowRate();
                    }
                }
                else if (value == GameState.Pause)
                {
                    Time.timeScale = 0;

                }
                else if (value == GameState.Playing)
                {
                    Time.timeScale = 1;
                    StartCoroutine(AI.THIS.CheckPossibleCombines());
                }
                else if (value == GameState.GameOver)
                {
                    MenuManager.Instance.ShowMenuFailed(true);
                    OnLose();
                }
                else if (value == GameState.PreWinAnimations)
                {
                    CommitWin();
                    StartCoroutine(PreWinAnimationsCor());
                }
                else if (value == GameState.Win)
                {
                    // The coins, the star, the life refund and the unlock were committed when the goals were met (CommitWin); a
                    // win reached another way commits here. The card and its coin flights present what was committed, and
                    // every win shows exactly one star.
                    CommitWin();
                    stars = 1;
                    MenuManager.Instance.ShowMenuComplete();
                    Juice.RoyalWinSequence.Reward(this, committedCoins, committedCoinsBefore);
                    OnMenuComplete();
                    if (Score > PlayerPrefs.GetInt("Score" + currentLevel))
                    {
                        PlayerPrefs.SetInt("Score" + currentLevel, Score);
                    }
                    PlayerPrefs.Save();

                    #if PLAYFAB || GAMESPARKS
                NetworkManager.dataManager.SetPlayerScore(currentLevel, Score);
                NetworkManager.dataManager.SetPlayerLevel(currentLevel + 1);
                NetworkManager.dataManager.SetStars();
                    #endif
                    MenuManager.Instance.ShowCompleteLabel(true);
                    SoundBase.Instance.PlaySound(SoundBase.Instance.complete[1]);
                    OnWin();
                }


            }
        }

        public void MenuPlayEvent()
        {
            OnMenuPlay();
        }


        #endregion

        void LockBoosts()
        {
            foreach (BoostIcon item in InGameBoosts)
            {
                if (item != ActivatedBoost)
                    item.LockBoost();
            }
        }

        public void UnLockBoosts()
        {
            foreach (BoostIcon item in InGameBoosts)
            {
                item.UnLockBoost();
            }
        }


        public void LoadLevel()
        {
            currentLevel = PlayerPrefs.GetInt("OpenLevel");// TargetHolder.level;
            if (currentLevel == 0)
                currentLevel = 1;
            LoadDataFromLocal(currentLevel);

        }

        public void EnableMap(bool enable)
        {
            float aspect = (float)Screen.height / (float)Screen.width;//2.1.4
            GetComponent<Camera>().orthographicSize = 5.3f;
            aspect = (float)Math.Round(aspect, 2);
            var canvasGlobal = MenuManager.Instance.GetCanvasGlobal();
            if (canvasGlobal != null)
            {
                var raycaster = canvasGlobal.GetComponent<GraphicRaycaster>();
                if (raycaster != null)
                {
                    raycaster.enabled = false;
                    raycaster.enabled = true;
                }
            }
            if (enable)
            {
                GetComponent<Camera>().GetComponent<MapCamera>().SetPosition(new Vector2(0, GetComponent<Camera>().transform.position.y));
            }
            else
            {
                InitScript.DateOfExit = DateTime.UtcNow.ToString("o", global::System.Globalization.CultureInfo.InvariantCulture);  //1.4

                LevelManager.THIS.latstMatchColor = -1;

                GetComponent<Camera>().orthographicSize = 6.5f;
                Level.transform.Find("Canvas/Panel").GetComponent<RectTransform>().anchoredPosition = Vector3.zero;//2.2
                if (aspect == 2.06f)
                    GetComponent<Camera>().orthographicSize = 7.6f;                  //2960:1440
                else if (aspect == 2.17f)
                {
                    GetComponent<Camera>().orthographicSize = 8.1f;                  //iphone x
                    Level.transform.Find("Canvas/Panel").GetComponent<RectTransform>().anchoredPosition = Vector3.down * 50;//2.2

                }

                Level.transform.Find("Canvas").GetComponent<GraphicRaycaster>().enabled = false;
                Level.transform.Find("Canvas").GetComponent<GraphicRaycaster>().enabled = true;

            }
            Camera.main.GetComponent<MapCamera>().enabled = enable;
            LevelsMap.SetActive(!enable);
            LevelsMap.SetActive(enable);
            Level.SetActive(!enable);

            if (enable)
                GameField.gameObject.SetActive(false);

            if (!enable)
                Camera.main.transform.position = new Vector3(0, 0, -10);
            foreach (Transform item in GameField.transform)
            {
                Destroy(item.gameObject);
            }
        }

        // Use this for initialization
        void Start()
        {
            #if FACEBOOK
        FacebookEnable = RoyalThrone.RoyalOfflineMode.ServicesEnabled;
        if (FacebookEnable)
            FacebookManager.THIS.CallFBInit();
            #else
            FacebookEnable = false;

            #endif
            #if UNITY_INAPPS

            enableInApps = RoyalThrone.RoyalOfflineMode.ServicesEnabled;
            if (enableInApps) gameObject.AddComponent<UnityInAppsIntegration>();
            #else
        enableInApps = false;

            #endif
            combineManager = new CombineManager();
            THIS = this;
            Instance = this;
            if (Juice.RoyalEffectsProfile.Get && !GetComponent<RoyalThrone.Presentation.RoyalGameplayPresentation>())
                gameObject.AddComponent<RoyalThrone.Presentation.RoyalGameplayPresentation>();
            //        if (!LevelManager.THIS.enableInApps)
            //            GameObject.Find("CanvasMap/Gems").gameObject.SetActive(false);//2.1.2

            gameStatus = GameState.Map;
            for (int i = 0; i < 20; i++)
            {
                itemExplPool[i] = Instantiate(itemExplosionPrefab, transform.position, Quaternion.identity);
                itemExplPool[i].SetActive(false);
            }
            passLevelCounter = 0;
        }

        void InitLevel()
        {

            GenerateLevel();
            RoyalThrone.Campaign.RoyalReferenceCampaign.Build(this);
            GenerateOutline();
            ReGenLevel();
            if (limitType == LIMIT.TIME)
            {
                StopCoroutine(TimeTick());
                StartCoroutine(TimeTick());
            }
//        InitTargets();
            GameField.gameObject.SetActive(true);

        }

        void PrepareGame()
        {
            royalResolving = false;
            InputSwaps = 0;
            inputRevision = 0;
            ActivatedBoost = null;
            Score = 0;
            stars = 0;
            winCommitted = false; StarsGained = 0;
            moveID = 0;
            // These survive a level otherwise: a win celebration interrupted by leaving the level
            // leaves preWinAnimationRunning set, and every later win then hangs before the banner.
            preWinAnimationRunning = false;
            SkipWinCelebration = false;
            ingredientFly = false;
            DragBlocked = false;


            blocksObject.SetActive(false);
            ingrObject.SetActive(false);
            scoreTargetObject.SetActive(false);

            star1Anim.SetActive(false);
            star2Anim.SetActive(false);
            star3Anim.SetActive(false);

            collectItems[0] = CollectItems.None;
            collectItems[1] = CollectItems.None;

            ingrTarget[0] = Ingredients.None;
            ingrTarget[1] = Ingredients.None;

            ingrCountTarget[0] = 0;
            ingrCountTarget[1] = 0;

            TargetBlocks = 0;
            EnableMap(false);


            GameField.transform.position = Vector3.zero;
            firstSquarePosition = GameField.transform.position;

            squaresArray = new Square[maxCols * maxRows];
            LoadLevel();
            for (int row = 0; row < maxRows; row++)
            {
                for (int col = 0; col < maxCols; col++)
                {
                    if (levelSquaresFile[row * maxCols + col].block == SquareTypes.BLOCK)
                        TargetBlocks++;
                    else if (levelSquaresFile[row * maxCols + col].block == SquareTypes.DOUBLEBLOCK)
                        TargetBlocks += 2;
                }
            }
            MenuManager.Instance.ShowPrePlay(true);
            if (limitType == LIMIT.MOVES)
            {
                InGameBoosts[0].gameObject.SetActive(true);
                InGameBoosts[1].gameObject.SetActive(false);
            }
            else
            {
                InGameBoosts[0].gameObject.SetActive(false);
                InGameBoosts[1].gameObject.SetActive(true);

            }
            OnEnterGame();
        }

        public void CheckCollectedTarget(GameObject _item)
        {
            void AnimateCollectedItem(Vector2 pos)
            {
                GameObject item = new GameObject();
                item.transform.position = _item.transform.position;
                // Keep the destruction coroutine alive while its visual clone flies to the goal.
                foreach (var renderer in _item.GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = false;
                item.transform.localScale = Vector3.one / 2f;
                SpriteRenderer spr = item.AddComponent<SpriteRenderer>();
                var spriteRenderer = _item.GetComponentInChildren<SpriteRenderer>();
                spr.sprite = spriteRenderer.sprite;
                spr.sortingLayerName = "UI";
                spr.sortingOrder = 1;

                StartCoroutine(StartAnimateIngredient(item, pos));
            }

            foreach (var objectTarget in targetObject)
            {
                if(!objectTarget.Done())
                {
                    if(objectTarget.SetOff(_item))
                        AnimateCollectedItem(objectTarget.guiObj.transform.position);
                }
            }
        }

        /// <summary>
        /// Presentation only. Fires the shared match-centre accent once the individual
        /// fruits have finished their anticipation, so the ring reads as the result of
        /// the pops rather than something that happened before them.
        /// </summary>
        IEnumerator PlayMatchCenterDelayed(Vector3 center, Color color, float intensity, float delay)
        {
            var sourceField = GameField;
            int sourceLevel = currentLevel;
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            // A paused match can be abandoned before this accent's beat arrives.
            // Do not let an old board's delayed effect appear over Home or a new level.
            if (!sourceField || sourceField != GameField || !sourceField.gameObject.activeInHierarchy ||
                currentLevel != sourceLevel || gameStatus == GameState.Map || gameStatus == GameState.PrepareGame)
                yield break;
            var vfx = Juice.MatchVFXService.Instance;
            if (vfx != null)
                vfx.PlayMatchCenter(center, color, intensity);
        }

        public ItemAnimEvents GetExplFromPool()
        {
            for (int i = 0; i < itemExplPool.Length; i++)
            {
                if (!itemExplPool[i].IsActive())
                {
                    itemExplPool[i].SetActive(true);
                    return itemExplPool[i];
                }
            }
            return null;
        }

        IEnumerator StartAnimateIngredient(GameObject item, Vector2 pos)
        {
            ingredientFly = true;
            GameObject[] ingr = new GameObject[2];
            ingr[0] = ingrObject.transform.Find("Ingr1").gameObject;
            ingr[1] = ingrObject.transform.Find("Ingr2").gameObject;
            if (targetBlocks > 0)
            {
                ingr[0] = blocksObject.transform.gameObject;
                ingr[1] = blocksObject.transform.gameObject;
            }
            AnimationCurve curveX = new AnimationCurve(new Keyframe(0, item.transform.localPosition.x), new Keyframe(0.4f, pos.x));
            AnimationCurve curveY = new AnimationCurve(new Keyframe(0, item.transform.localPosition.y), new Keyframe(0.5f, pos.y));
            curveY.AddKey(0.2f, item.transform.localPosition.y + UnityEngine.Random.Range(-2, 0.5f));
            float startTime = Time.time;
            Vector3 startPos = item.transform.localPosition;
            float speed = UnityEngine.Random.Range(0.4f, 0.6f);
            float distCovered = 0;
            while (distCovered < 0.5f)
            {
                distCovered = (Time.time - startTime) * speed;
                item.transform.localPosition = new Vector3(curveX.Evaluate(distCovered), curveY.Evaluate(distCovered), 0);
                item.transform.Rotate(Vector3.back, Time.deltaTime * 1000);
                yield return new WaitForFixedUpdate();
            }
            //     SoundBase.Instance.audio.PlayOneShot(SoundBase.Instance.getStarIngr);
            Destroy(item);
            if (gameStatus == GameState.Playing && !IsIngredientFalling())//1.6.1
                CheckWinLose();
            ingredientFly = false;
        }

        public void CheckWinLose()
        {
            if(RoyalThrone.Campaign.RoyalReferenceCampaign.CheckBonusOutcome(this))return;
            //		print ("check win lose");
            if (Limit <= 0)
            {
                bool lose = false;
                Limit = 0;
                if (targetObject.Any(i => !i.Done())) lose = true;
                if (lose)
                    gameStatus = GameState.PreFailed;
                // Goals met is a win, whatever the score (one star per win, as in the reference). With the old score
                // condition a level whose goals were met on its last move, below star1, neither failed nor won: it hung.
                else if (targetObject.All(i => i.Done()))
                {
                    gameStatus = GameState.PreWinAnimations;
                }
            }
            else
            {
                bool win = targetObject.All(i => i.Done());
                if (win)
                {
                    gameStatus = GameState.PreWinAnimations;
                }


            }
        }

        /// <summary>
        /// A win's lasting effects, once per attempt, the moment its goals are met: before the celebration and the finale,
        /// which only present them, so a kill during the finale cannot lose the win (the reference keeps it by then too).
        /// A win is worth exactly one star (the reference: one per win, whatever the score; the user's decision, 2026-09-29)
        /// and 10 coins plus 5 per star (the user's decision, 2026-09-27). It refunds the life spent on Play, saves the
        /// star that unlocks the next level and the best score, then flushes the save.
        /// </summary>
        void CommitWin()
        {
            if (winCommitted) return;
            winCommitted = true;
            stars = 1;
            string starsKey = string.Format("Level.{0:000}.StarsCount", currentLevel);
            StarsGained = PlayerPrefs.GetInt(starsKey, 0) > 0 ? 0 : 1;
            committedCoinsBefore = InitScript.Gems;
            committedCoins = Juice.RoyalWinSequence.Coins(stars);
            InitScript.Instance.AddGems(committedCoins);
            if (!InitScript.Instance.losingLifeEveryGame)
                InitScript.Instance.AddLife(1);
            if (PlayerPrefs.GetInt(starsKey, 0) < stars)
                PlayerPrefs.SetInt(starsKey, stars);
            if (Score > PlayerPrefs.GetInt("Score" + currentLevel))
                PlayerPrefs.SetInt("Score" + currentLevel, Score);
            PlayerPrefs.Save();
        }

        /// <summary>Set by a tap during the win celebration: the reward still plays out, faster.</summary>
        public bool SkipWinCelebration { get; private set; }

        public void RequestSkipWinCelebration()
        {
            if (gameStatus == GameState.PreWinAnimations) SkipWinCelebration = true;
        }

        // A celebration the player cannot leave is dead time. Each leftover move still becomes a
        // rocket and still detonates, but the gaps are short, and a tap collapses them further.
        float CelebrationWait(float normal, float skipped) => SkipWinCelebration ? skipped : normal;

        IEnumerator PreWinAnimationsCor()
        {
            if (preWinAnimationRunning)
                yield break;

            preWinAnimationRunning = true;
            SkipWinCelebration = false;
            RoyalThrone.Characters.KingVanguardActivityEvents.NotifyGoalsCompleted(this);

            // The reference order (Design/RoyalMatchReference/OBSERVATIONS.md): the celebration (the logo under pennants,
            // fireworks, confetti, the complete fanfare) as soon as the goals are done, before the moves left are used. It
            // replaces the inherited pre-complete banner; a tap shortens it.
            yield return Juice.RoyalWinSequence.Celebrate(this);
            if (gameStatus != GameState.PreWinAnimations) { preWinAnimationRunning = false; yield break; }
            SkipWinCelebration = false;

            // Then the booster session, with the goal panel turned into a Reward counter that the coins fly into.
            Juice.RoyalWinSequence.BeginFinale(this);
            yield return RoyalThrone.ReferenceGameplay.ReferenceWinFinish.Play(this);
            if (gameStatus != GameState.PreWinAnimations) { preWinAnimationRunning = false; yield break; }
            yield return Juice.RoyalWinSequence.EndFinale(this);
            if (gameStatus != GameState.PreWinAnimations) { preWinAnimationRunning = false; yield break; }

            gameStatus = GameState.Win;
            preWinAnimationRunning = false;
            SkipWinCelebration = false;
        }

        void Update()
        {
            if (InputManager.Instance != null)
            {
                if (InputManager.Instance.Debug.Skip.WasPressedThisFrame())
                {
                    NoMatches();
                }
                if (InputManager.Instance.Debug.Win.WasPressedThisFrame())
                {
                    gameStatus = GameState.PreWinAnimations;
                }
                if (InputManager.Instance.Debug.Lose.WasPressedThisFrame())
                {
                    Limit = 1;
                }

                if (InputManager.Instance.Debug.Reload.WasPressedThisFrame())
            {   //save items state
                print("Saving items...");
                int[] items = new int[99];

                for (int row = 0; row < maxRows; row++)
                {
                    for (int col = 0; col < maxCols; col++)
                    {
                        if (GetSquare(col, row, false) != null)
                        {
                            if (GetSquare(col, row, false).item != null)
                                items[row * maxCols + col] = GetSquare(col, row, false).item.color;
                        }
                        else
                            items[row * maxCols + col] = -1;

                    }
                }
                LevelDebugger.SaveMap(items, maxCols, maxRows);
            }

                if (InputManager.Instance.Debug.Quit.WasPressedThisFrame())
            {   //load items state
                print("load items...");

                int[] items = new int[99];
                items = LevelDebugger.LoadMap(maxCols, maxRows);
                for (int row = 0; row < maxRows; row++)
                {
                    for (int col = 0; col < maxCols; col++)
                    {
                        if (items[row * maxCols + col] > -1)
                        {
                            if (GetSquare(col, row).item != null)
                                GetSquare(col, row).item.SetColor(items[row * maxCols + col]);
                        }
                    }
                }
            }

                if (InputManager.Instance.Gameplay.Cancel.WasPressedThisFrame())
                    RoyalThrone.Presentation.RoyalBackButton.NotePress();
                if (InputManager.Instance.Gameplay.Cancel.WasReleasedThisFrame())
                {
                    if (LevelManager.THIS.gameStatus == GameState.Playing && !RoyalThrone.Presentation.RoyalBackButton.Consumed)
                        MenuManager.Instance.ShowMenuPause(true);
                    // Back that closed a home dialog or popup (or with a popup still open) never also exits the game.
                    else if (LevelManager.THIS.gameStatus == GameState.Map && !RoyalThrone.Presentation.RoyalBackButton.Consumed && !MenuManager.Instance.BlocksBoardInput)
                        RoyalThrone.Presentation.RoyalBackButton.Quit();

                }
            }


            if (gameStatus == GameState.PreWinAnimations && InputManager.Instance != null &&
                InputManager.Instance.WasTouchPressed)
                RequestSkipWinCelebration();

            if (LevelManager.THIS.gameStatus == GameState.Playing && InputManager.Instance != null)
            {
                // A release only ever ends the current drag. It used to fall through the same
                // branch as a press with a zero position, which probed world origin (0,0).
                if (InputManager.Instance.WasTouchReleased)
                {
                    // Item owns press/drag/release; clearing it here raced its tap handler.
                    itemTouched = null;
                }
                else if (InputManager.Instance.WasTouchPressed && LevelManager.THIS.BoardInputAllowed)
                {
                    OnStartPlay();
                    Collider2D hit = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(InputManager.Instance.TouchPosition));
                    if (hit != null)
                    {
                        itemTouched = hit.gameObject.GetComponent<Item>();
                        if (itemTouched != null && itemTouched.CanAcceptInput)
                        {
                            if (LevelManager.THIS.ActivatedBoost.type == BoostType.Bomb && itemTouched.currentType != ItemsTypes.BOMB && itemTouched.currentType != ItemsTypes.INGREDIENT)
                            {
                                SoundBase.Instance.PlaySound(SoundBase.Instance.boostBomb);
                                LevelManager.THIS.DragBlocked = true;
                                GameObject obj = Instantiate(Resources.Load("Prefabs/Effects/bomb"), itemTouched.transform.position, itemTouched.transform.rotation) as GameObject;
                                obj.GetComponent<SpriteRenderer>().sortingOrder = 4;
                                obj.GetComponent<BoostAnimation>().square = itemTouched.square;
                                LevelManager.THIS.ActivatedBoost = null;
                            }
                            else if (LevelManager.THIS.ActivatedBoost.type == BoostType.Random_color && itemTouched.currentType != ItemsTypes.BOMB)
                            {
                                SoundBase.Instance.PlaySound(SoundBase.Instance.boostColorReplace);
                                LevelManager.THIS.DragBlocked = true;
                                GameObject obj = Instantiate(Resources.Load("Prefabs/Effects/random_color_item"), itemTouched.transform.position, itemTouched.transform.rotation) as GameObject;
                                obj.GetComponent<BoostAnimation>().square = itemTouched.square;
                                obj.GetComponent<SpriteRenderer>().sortingOrder = 4;
                                LevelManager.THIS.ActivatedBoost = null;
                            }
                        }
                    }

                }
            }
        }

        IEnumerator TimeTick()
        {
            while (true)
            {
                if (gameStatus == GameState.Playing)
                {
                    if (LevelManager.Instance.limitType == LIMIT.TIME)
                    {
                        LevelManager.THIS.Limit--;
                        if (IsAllItemsFallDown())
                            CheckWinLose();
                    }
                }
                if (gameStatus == GameState.Map)
                    yield break;
                yield return new WaitForSeconds(1);
            }
        }

        private void GenerateLevel()
        {
            bool chessColor = false;
            Vector3 fieldPos = new Vector3(-maxCols / 2.75f, maxRows / 2.75f, -10);
            for (int row = 0; row < maxRows; row++)
            {
                if (maxCols % 2 == 0)
                    chessColor = !chessColor;
                for (int col = 0; col < maxCols; col++)
                {
                    CreateSquare(col, row, chessColor);
                    chessColor = !chessColor;
                }

            }
            AnimateField(fieldPos);

        }

        void AnimateField(Vector3 pos)
        {

            float yOffset = 0;
            if (target == Target.INGREDIENT)
                yOffset = 0.3f;
            Animation anim = GameField.GetComponent<Animation>();
            AnimationClip clip = new AnimationClip();
            AnimationCurve curveX = new AnimationCurve(new Keyframe(0, pos.x + 15), new Keyframe(0.7f, pos.x - 0.2f), new Keyframe(0.8f, pos.x));
            AnimationCurve curveY = new AnimationCurve(new Keyframe(0, pos.y + yOffset), new Keyframe(1, pos.y + yOffset));
            clip.legacy = true;//2.1.6
            clip.SetCurve("", typeof(Transform), "localPosition.x", curveX);
            clip.SetCurve("", typeof(Transform), "localPosition.y", curveY);
            clip.AddEvent(new AnimationEvent() { time = 1, functionName = "EndAnimGamField" });
            anim.AddClip(clip, "appear");
            anim.Play("appear");
            GameField.transform.position = new Vector2(pos.x + 15, pos.y + yOffset);

        }

        void CreateSquare(int col, int row, bool chessColor = false)
        {
            GameObject square = null;
            square = Instantiate(squarePrefab, firstSquarePosition + new Vector2(col * squareWidth, -row * squareHeight), Quaternion.identity) as GameObject;
            if (chessColor)
            {
                square.GetComponent<SpriteRenderer>().sprite = squareSprite1;
            }
            square.transform.SetParent(GameField);
            square.transform.localPosition = firstSquarePosition + new Vector2(col * squareWidth, -row * squareHeight);
            squaresArray[row * maxCols + col] = square.GetComponent<Square>();
            square.GetComponent<Square>().row = row;
            square.GetComponent<Square>().col = col;
            square.GetComponent<Square>().type = SquareTypes.EMPTY;
            if (levelSquaresFile[row * maxCols + col].block == SquareTypes.EMPTY)
            {
                CreateObstacles(col, row, square, SquareTypes.NONE);
            }
            else if (levelSquaresFile[row * maxCols + col].block == SquareTypes.NONE)
            {
                square.GetComponent<SpriteRenderer>().enabled = false;
                square.GetComponent<Square>().type = SquareTypes.NONE;

            }
            else if (levelSquaresFile[row * maxCols + col].block == SquareTypes.BLOCK)
            {
                GameObject block = Instantiate(blockPrefab, firstSquarePosition + new Vector2(col * squareWidth, -row * squareHeight), Quaternion.identity) as GameObject;
                block.transform.SetParent(square.transform);
                block.transform.localPosition = new Vector3(0, 0, -0.01f);
                square.GetComponent<Square>().block.Add(block);
                square.GetComponent<Square>().type = SquareTypes.BLOCK;

                // TargetBlocks++;
                CreateObstacles(col, row, square, SquareTypes.NONE);
            }
            else if (levelSquaresFile[row * maxCols + col].block == SquareTypes.DOUBLEBLOCK)
            {
                GameObject block = Instantiate(blockPrefab, firstSquarePosition + new Vector2(col * squareWidth, -row * squareHeight), Quaternion.identity) as GameObject;
                block.transform.SetParent(square.transform);
                block.transform.localPosition = new Vector3(0, 0, -0.01f);
                square.GetComponent<Square>().block.Add(block);
                square.GetComponent<Square>().type = SquareTypes.BLOCK;

                //  TargetBlocks++;
                block = Instantiate(blockPrefab, firstSquarePosition + new Vector2(col * squareWidth, -row * squareHeight), Quaternion.identity) as GameObject;
                block.transform.SetParent(square.transform);
                block.transform.localPosition = new Vector3(0, 0, -0.01f);
                square.GetComponent<Square>().block.Add(block);
                square.GetComponent<Square>().type = SquareTypes.BLOCK;
                block.GetComponent<SpriteRenderer>().sprite = doubleBlock;
                block.GetComponent<SpriteRenderer>().sortingOrder = 1;
                //  TargetBlocks++;
                CreateObstacles(col, row, square, SquareTypes.NONE);
            }

        }

        void GenerateOutline()
        {
            int row = 0;
            int col = 0;
            for (row = 0; row < maxRows; row++)
            { //down
                SetOutline(col, row, 0);
            }
            row = maxRows - 1;
            for (col = 0; col < maxCols; col++)
            { //right
                SetOutline(col, row, 90);
            }
            col = maxCols - 1;
            for (row = maxRows - 1; row >= 0; row--)
            { //up
                SetOutline(col, row, 180);
            }
            row = 0;
            for (col = maxCols - 1; col >= 0; col--)
            { //left
                SetOutline(col, row, 270);
            }
            col = 0;
            for (row = 1; row < maxRows - 1; row++)
            {
                for (col = 1; col < maxCols - 1; col++)
                {
                    //  if (GetSquare(col, row).type == SquareTypes.NONE)
                    SetOutline(col, row, 0);
                }
            }
        }


        void SetOutline(int col, int row, float zRot)
        {
            Square square = GetSquare(col, row, true);
            if (square.type != SquareTypes.NONE)
            {
                if (row == 0 || col == 0 || col == maxCols - 1 || row == maxRows - 1)
                {
                    GameObject outline = CreateOutline(square);
                    SpriteRenderer spr = outline.GetComponent<SpriteRenderer>();
                    outline.transform.localRotation = Quaternion.Euler(0, 0, zRot);
                    if (zRot == 0)
                        outline.transform.localPosition = Vector3.zero + Vector3.left * 0.425f;
                    if (zRot == 90)
                        outline.transform.localPosition = Vector3.zero + Vector3.down * 0.425f;
                    if (zRot == 180)
                        outline.transform.localPosition = Vector3.zero + Vector3.right * 0.425f;
                    if (zRot == 270)
                        outline.transform.localPosition = Vector3.zero + Vector3.up * 0.425f;
                    if (row == 0 && col == 0)
                    {   //top left
                        spr.sprite = outline3;
                        outline.transform.localRotation = Quaternion.Euler(0, 0, 180);
                        outline.transform.localPosition = Vector3.zero + Vector3.left * 0.015f + Vector3.up * 0.015f;
                    }
                    if (row == 0 && col == maxCols - 1)
                    {   //top right
                        spr.sprite = outline3;
                        outline.transform.localRotation = Quaternion.Euler(0, 0, 90);
                        outline.transform.localPosition = Vector3.zero + Vector3.right * 0.015f + Vector3.up * 0.015f;
                    }
                    if (row == maxRows - 1 && col == 0)
                    {   //bottom left
                        spr.sprite = outline3;
                        outline.transform.localRotation = Quaternion.Euler(0, 0, -90);
                        outline.transform.localPosition = Vector3.zero + Vector3.left * 0.015f + Vector3.down * 0.015f;
                    }
                    if (row == maxRows - 1 && col == maxCols - 1)
                    {   //bottom right
                        spr.sprite = outline3;
                        outline.transform.localRotation = Quaternion.Euler(0, 0, 0);
                        outline.transform.localPosition = Vector3.zero + Vector3.right * 0.015f + Vector3.down * 0.015f;
                    }
                }
                else
                {
                    //top left
                    if (GetSquare(col - 1, row - 1, true).type == SquareTypes.NONE && GetSquare(col, row - 1, true).type == SquareTypes.NONE && GetSquare(col - 1, row, true).type == SquareTypes.NONE)
                    {
                        GameObject outline = CreateOutline(square);
                        SpriteRenderer spr = outline.GetComponent<SpriteRenderer>();
                        spr.sprite = outline3;
                        outline.transform.localPosition = Vector3.zero + Vector3.left * 0.015f + Vector3.up * 0.015f;
                        outline.transform.localRotation = Quaternion.Euler(0, 0, 180);
                    }
                    //top right
                    if (GetSquare(col + 1, row - 1, true).type == SquareTypes.NONE && GetSquare(col, row - 1, true).type == SquareTypes.NONE && GetSquare(col + 1, row, true).type == SquareTypes.NONE)
                    {
                        GameObject outline = CreateOutline(square);
                        SpriteRenderer spr = outline.GetComponent<SpriteRenderer>();
                        spr.sprite = outline3;
                        outline.transform.localPosition = Vector3.zero + Vector3.right * 0.015f + Vector3.up * 0.015f;
                        outline.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    }
                    //bottom left
                    if (GetSquare(col - 1, row + 1, true).type == SquareTypes.NONE && GetSquare(col, row + 1, true).type == SquareTypes.NONE && GetSquare(col - 1, row, true).type == SquareTypes.NONE)
                    {
                        GameObject outline = CreateOutline(square);
                        SpriteRenderer spr = outline.GetComponent<SpriteRenderer>();
                        spr.sprite = outline3;
                        outline.transform.localPosition = Vector3.zero + Vector3.left * 0.015f + Vector3.down * 0.015f;
                        outline.transform.localRotation = Quaternion.Euler(0, 0, 270);
                    }
                    //bottom right
                    if (GetSquare(col + 1, row + 1, true).type == SquareTypes.NONE && GetSquare(col, row + 1, true).type == SquareTypes.NONE && GetSquare(col + 1, row, true).type == SquareTypes.NONE)
                    {
                        GameObject outline = CreateOutline(square);
                        SpriteRenderer spr = outline.GetComponent<SpriteRenderer>();
                        spr.sprite = outline3;
                        outline.transform.localPosition = Vector3.zero + Vector3.right * 0.015f + Vector3.down * 0.015f;
                        outline.transform.localRotation = Quaternion.Euler(0, 0, 0);
                    }


                }
            }
            else
            {
                bool corner = false;
                if (GetSquare(col - 1, row, true).type != SquareTypes.NONE && GetSquare(col, row - 1, true).type != SquareTypes.NONE)
                {
                    GameObject outline = CreateOutline(square);
                    SpriteRenderer spr = outline.GetComponent<SpriteRenderer>();
                    spr.sprite = outline2;
                    outline.transform.localPosition = Vector3.zero;
                    outline.transform.localRotation = Quaternion.Euler(0, 0, 0);
                    corner = true;
                }
                if (GetSquare(col + 1, row, true).type != SquareTypes.NONE && GetSquare(col, row + 1, true).type != SquareTypes.NONE)
                {
                    GameObject outline = CreateOutline(square);
                    SpriteRenderer spr = outline.GetComponent<SpriteRenderer>();
                    spr.sprite = outline2;
                    outline.transform.localPosition = Vector3.zero;
                    outline.transform.localRotation = Quaternion.Euler(0, 0, 180);
                    corner = true;
                }
                if (GetSquare(col + 1, row, true).type != SquareTypes.NONE && GetSquare(col, row - 1, true).type != SquareTypes.NONE)
                {
                    GameObject outline = CreateOutline(square);
                    SpriteRenderer spr = outline.GetComponent<SpriteRenderer>();
                    spr.sprite = outline2;
                    outline.transform.localPosition = Vector3.zero;
                    outline.transform.localRotation = Quaternion.Euler(0, 0, 270);
                    corner = true;
                }
                if (GetSquare(col - 1, row, true).type != SquareTypes.NONE && GetSquare(col, row + 1, true).type != SquareTypes.NONE)
                {
                    GameObject outline = CreateOutline(square);
                    SpriteRenderer spr = outline.GetComponent<SpriteRenderer>();
                    spr.sprite = outline2;
                    outline.transform.localPosition = Vector3.zero;
                    outline.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    corner = true;
                }


                if (!corner)
                {
                    if (GetSquare(col, row - 1, true).type != SquareTypes.NONE)
                    {
                        GameObject outline = CreateOutline(square);
                        SpriteRenderer spr = outline.GetComponent<SpriteRenderer>();
                        outline.transform.localPosition = Vector3.zero + Vector3.up * 0.395f;
                        outline.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    }
                    if (GetSquare(col, row + 1, true).type != SquareTypes.NONE)
                    {
                        GameObject outline = CreateOutline(square);
                        SpriteRenderer spr = outline.GetComponent<SpriteRenderer>();
                        outline.transform.localPosition = Vector3.zero + Vector3.down * 0.395f;
                        outline.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    }
                    if (GetSquare(col - 1, row, true).type != SquareTypes.NONE)
                    {
                        GameObject outline = CreateOutline(square);
                        SpriteRenderer spr = outline.GetComponent<SpriteRenderer>();
                        outline.transform.localPosition = Vector3.zero + Vector3.left * 0.395f;
                        outline.transform.localRotation = Quaternion.Euler(0, 0, 0);
                    }
                    if (GetSquare(col + 1, row, true).type != SquareTypes.NONE)
                    {
                        GameObject outline = CreateOutline(square);
                        SpriteRenderer spr = outline.GetComponent<SpriteRenderer>();
                        outline.transform.localPosition = Vector3.zero + Vector3.right * 0.395f;
                        outline.transform.localRotation = Quaternion.Euler(0, 0, 0);
                    }
                }
            }
            //Vector3 pos = GameField.transform.TransformPoint((Vector3)firstSquarePosition + new Vector3(col * squareWidth - squareWidth / 2, -row * squareHeight, 10));
            //line.SetVertexCount(linePoint + 1);
            //line.SetPosition(linePoint++, pos);

        }

        GameObject CreateOutline(Square square)
        {
            GameObject outline = new GameObject();
            outline.name = "outline";
            outline.transform.SetParent(square.transform);
            outline.transform.localPosition = Vector3.zero;
            SpriteRenderer spr = outline.AddComponent<SpriteRenderer>();
            spr.sprite = outline1;
            spr.sortingOrder = 1;
            return outline;
        }

        void CreateObstacles(int col, int row, GameObject square, SquareTypes type)
        {
            if ((levelSquaresFile[row * maxCols + col].obstacle == SquareTypes.WIREBLOCK && type == SquareTypes.NONE) || type == SquareTypes.WIREBLOCK)
            {
                GameObject block = Instantiate(wireBlockPrefab, firstSquarePosition + new Vector2(col * squareWidth, -row * squareHeight), Quaternion.identity) as GameObject;
                block.transform.SetParent(square.transform);
                block.transform.localPosition = new Vector3(0, 0, -0.5f);
                square.GetComponent<Square>().block.Add(block);
                square.GetComponent<Square>().type = SquareTypes.WIREBLOCK;
                block.GetComponent<SpriteRenderer>().sortingOrder = 3;
                //   TargetBlocks++;
            }
            else if ((levelSquaresFile[row * maxCols + col].obstacle == SquareTypes.SOLIDBLOCK && type == SquareTypes.NONE) || type == SquareTypes.SOLIDBLOCK)
            {
                GameObject block = Instantiate(solidBlockPrefab, firstSquarePosition + new Vector2(col * squareWidth, -row * squareHeight), Quaternion.identity) as GameObject;
                block.transform.SetParent(square.transform);
                block.transform.localPosition = new Vector3(0, 0, -0.5f);
                square.GetComponent<Square>().block.Add(block);
                block.GetComponent<SpriteRenderer>().sortingOrder = 3;
                square.GetComponent<Square>().type = SquareTypes.SOLIDBLOCK;

                //  TargetBlocks++;
            }
            else if ((levelSquaresFile[row * maxCols + col].obstacle == SquareTypes.UNDESTROYABLE && type == SquareTypes.NONE) || type == SquareTypes.UNDESTROYABLE)
            {
                GameObject block = Instantiate(undesroyableBlockPrefab, firstSquarePosition + new Vector2(col * squareWidth, -row * squareHeight), Quaternion.identity) as GameObject;
                block.transform.SetParent(square.transform);
                block.transform.localPosition = new Vector3(0, 0, -0.5f);
                square.GetComponent<Square>().block.Add(block);
                square.GetComponent<Square>().type = SquareTypes.UNDESTROYABLE;

                //  TargetBlocks++;
            }
            else if ((levelSquaresFile[row * maxCols + col].obstacle == SquareTypes.THRIVING && type == SquareTypes.NONE) || type == SquareTypes.THRIVING)
            {
                GameObject block = Instantiate(thrivingBlockPrefab, firstSquarePosition + new Vector2(col * squareWidth, -row * squareHeight), Quaternion.identity) as GameObject;
                block.transform.SetParent(square.transform);
                block.transform.localPosition = new Vector3(0, 0, -0.5f);
                block.GetComponent<SpriteRenderer>().sortingOrder = 3;
                if (square.GetComponent<Square>().item != null)
                    Destroy(square.GetComponent<Square>().item.gameObject);
                square.GetComponent<Square>().block.Add(block);
                square.GetComponent<Square>().type = SquareTypes.THRIVING;

                //   TargetBlocks++;
            }

        }

        void GenerateNewItems(bool falling = true)
        {
            for (int col = 0; col < maxCols; col++)
            {
                for (int row = maxRows - 1; row >= 0; row--)
                {
                    if (GetSquare(col, row) != null)
                    {
                        if (!GetSquare(col, row).IsNone() && GetSquare(col, row).CanGoInto() && GetSquare(col, row).item == null)
                        {
                            if ((GetSquare(col, row).item == null && !GetSquare(col, row).IsHaveSolidAbove()) || !falling)
                            {
                                GetSquare(col, row).GenItem(falling);
                            }
                        }
                    }
                }
            }

        }

        public void NoMatches()
        {
            StartCoroutine(NoMatchesCor());
        }

        IEnumerator NoMatchesCor()
        {
            if (gameStatus == GameState.Playing)
            {
                SoundBase.Instance.PlaySound(SoundBase.Instance.noMatch);

                MenuManager.Instance.ShowNoMoreMatches(true);
                gameStatus = GameState.RegenLevel;
                yield return new WaitForSeconds(1);
                ReGenLevel();
            }
        }

        public void ReGenLevel()
        {
            itemsHided = false;
            DragBlocked = true;
            if (gameStatus != GameState.Playing && gameStatus != GameState.RegenLevel)
                DestroyItems();
            else if (gameStatus == GameState.RegenLevel)
                DestroyItems(true);
            OnLevelLoaded();
            StartCoroutine(RegenMatches());
            OnLevelLoaded();
        }

        IEnumerator RegenMatches(bool onlyFalling = false)
        {
            // Reference refill cascades resolve in FallingDown's next pass during play.
            // Once won, retain the engine's existing stabilization below: otherwise
            // the two-colour bonus refill keeps creating new celebration boosters.
            if(onlyFalling&&RoyalThrone.Campaign.RoyalReferenceCampaign.Active&&gameStatus!=GameState.PreWinAnimations)
            {yield return new WaitForFixedUpdate();yield break;}
            if (gameStatus == GameState.RegenLevel)
            {
                //while (!itemsHided)
                //{
                yield return new WaitForSeconds(0.5f);
                //}
            }
            if (!onlyFalling)
                GenerateNewItems(false);
            else
                LevelManager.THIS.onlyFalling = true;
            //   yield return new WaitForSeconds(1f);
            yield return new WaitForFixedUpdate();

            List<List<Item>> combs = GetMatches();
            //while (!matchesGot)
            //{
            //    yield return new WaitForFixedUpdate();

            //}
            //combs = newCombines;
            //matchesGot = false;
            int i = 0;
            do
            {
                // A 2x2 is discovered from more than one cell. Recolouring the
                // same piece twice in this pass can undo its first change forever.
                var recoloured = new HashSet<Item>();
                foreach (List<Item> comb in combs)
                {
                    int colorOffset = 0;
                    foreach (Item item in comb)
                    {
                        if (item && recoloured.Add(item))
                            item.GenColor(item.color + colorOffset);
                        colorOffset++;
                    }
                }
                combs = GetMatches();
                i++;
                //while (!matchesGot)
                //{
                //    yield return new WaitForFixedUpdate();

                //}
                //combs = newCombines;
                //matchesGot = false;
                if(i>50)
                {
                    yield return new WaitForFixedUpdate();
                    i = 0;
                }
            } while (combs.Count > 0);
            yield return new WaitForFixedUpdate();
            SetPreBoosts();
            LevelManager.THIS.onlyFalling = false;
            if (gameStatus == GameState.RegenLevel)
                gameStatus = GameState.Playing;
            if (!onlyFalling)//2.1.5 prevents early move
                DragBlocked = false;
            StartCoroutine(AI.THIS.CheckPossibleCombines());//2.1.5 prevents early move

            //StartCoroutine(CheckFallingAtStart());

        }

        void SetPreBoosts()
        {
            if (BoostPackage > 0)
            {
                InitScript.Instance.SpendBoost(BoostType.Packages);
                foreach (Item item in GetRandomItems(BoostPackage))
                {
                    item.NextType = ItemsTypes.PACKAGE;
                    item.ChangeType();
                    item.boost = true;
                }
                BoostPackage = 0;
            }
            if (BoostColorfullBomb > 0)
            {
                InitScript.Instance.SpendBoost(BoostType.Colorful_bomb);
                foreach (Item item in GetRandomItems(BoostColorfullBomb))
                {
                    item.NextType = ItemsTypes.BOMB;
                    item.ChangeType();
                    item.boost = true;
                }
                BoostColorfullBomb = 0;
            }
            if (BoostStriped > 0)
            {
                InitScript.Instance.SpendBoost(BoostType.Stripes);
                foreach (Item item in GetRandomItems(BoostStriped))
                {
                    item.NextType = (ItemsTypes)UnityEngine.Random.Range(1, 3);
                    item.ChangeType();
                    item.boost = true;
                }
                BoostStriped = 0;
            }
        }


        public List<Item> GetRandomItems(int count)
        {
            if(RoyalThrone.Campaign.RoyalReferenceCampaign.Active)
            {
                var available=GetItems().Where(i=>i&&i.currentType==ItemsTypes.NONE&&i.NextType==ItemsTypes.NONE&&!i.destroying&&
                    !RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(i)).ToList();
                var selected=new List<Item>();count=Mathf.Clamp(count,0,available.Count);
                while(selected.Count<count){int pick=UnityEngine.Random.Range(0,available.Count);selected.Add(available[pick]);available.RemoveAt(pick);}
                return selected;
            }
            List<Item> list = new List<Item>();
            List<Item> list2 = new List<Item>();
            GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
            if (items.Length < count)
                count = items.Length;
            foreach (GameObject item in items)
            {
                if (item.GetComponent<Item>().currentType == ItemsTypes.NONE && item.GetComponent<Item>().NextType == ItemsTypes.NONE && !item.GetComponent<Item>().destroying)
                {
                    list.Add(item.GetComponent<Item>());
                }
            }

            // Only plain pieces can be picked: asking for more than exist never finished (with none, every
            // pass re-entered the Win state), which froze the game. Take as many as there are.
            if (list.Count < count)
                count = list.Count;
            while (list2.Count < count)
            {

                try
                {
                    Item newItem = list[UnityEngine.Random.Range(0, list.Count)];
                    if (list2.IndexOf(newItem) < 0)
                    {
                        list2.Add(newItem);
                    }
                }
                catch (Exception)
                {
                    gameStatus = GameState.Win;
                }
            }
            return list2;
        }

        List<Item> GetAllExtaItems()
        {
            List<Item> list = new List<Item>();
            GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
            foreach (GameObject item in items)
            {
                if (item.GetComponent<Item>().currentType != ItemsTypes.NONE)
                {
                    list.Add(item.GetComponent<Item>());
                }
            }

            return list;
        }

        public int GetIngredientsCount(string spritename)
        {
            if (string.IsNullOrEmpty(spritename)) return 0;
            // Count owned board pieces only. Particle renderers can have no sprite,
            // and falling fragments can retain the ingredient's sprite name.
            return GetItems().Count(item => item && !item.destroying &&
                item.currentType == ItemsTypes.INGREDIENT && item.sprRenderer &&
                item.sprRenderer.sprite && item.sprRenderer.sprite.name == spritename);
        }


        public void DestroyItems(bool withoutEffects = false)
        {

            GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
            foreach (GameObject item in items)
            {
                if (item != null)
                {
                    if(RoyalThrone.Campaign.RoyalReferenceCampaign.Active&&gameStatus==GameState.RegenLevel&&
                        RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(item.GetComponent<Item>()))continue;
                    if (item.GetComponent<Item>().currentType != ItemsTypes.INGREDIENT && item.GetComponent<Item>().currentType == ItemsTypes.NONE)
                    {
                        if (!withoutEffects)
                            item.GetComponent<Item>().DestroyItem();
                        else
                            item.GetComponent<Item>().SmoothDestroy();
                    }
                }
            }

        }

        public IEnumerator FindMatchDelay()
        {
            yield return new WaitForSeconds(0.2f);
            LevelManager.THIS.FindMatches();

        }

        bool royalResolving;
        public bool IsResolvingMatches => royalResolving;
        public void FindMatches()
        {
            inputRevision++;
            if (royalResolving) return;
            royalResolving = true;
            StartCoroutine(FallingDown());
        }

        public List<List<Item>> GetMatches(FindSeparating separating = FindSeparating.NONE, int matches = 3)
        {
            newCombines = new List<List<Item>>();
            //       List<Item> countedSquares = new List<Item>();
            countedSquares = new Hashtable();
            countedSquares.Clear();
            for (int col = 0; col < maxCols; col++)
            {
                for (int row = 0; row < maxRows; row++)
                {
                    if (GetSquare(col, row) != null)
                    {
                        if (!countedSquares.ContainsValue(GetSquare(col, row).item))
                        {
                            List<Item> newCombine = GetSquare(col, row).FindMatchesAround(separating, matches, countedSquares);
                            if (newCombine.Count >= matches)
                                newCombines.Add(newCombine);
                        }
                    }
                }
            }
            //print("global " + countedSquares.Count);
            //  Debug.Break();
            return newCombines;
        }

        IEnumerator GetMatchesCor(FindSeparating separating = FindSeparating.NONE, int matches = 3, bool Smooth = true)
        {
            Hashtable countedSquares = new Hashtable();
            for (int col = 0; col < maxCols; col++)
            {
                //if (Smooth)
                //                    yield return new WaitForFixedUpdate();
                for (int row = 0; row < maxRows; row++)
                {

                    if (GetSquare(col, row) != null)
                    {
                        if (!countedSquares.ContainsValue(GetSquare(col, row).item))
                        {
                            List<Item> newCombine = GetSquare(col, row).FindMatchesAround(separating, matches, countedSquares);
                            if (newCombine.Count >= matches)
                                newCombines.Add(newCombine);
                        }
                    }
                }
            }
            yield return new WaitForFixedUpdate();

        }

        IEnumerator CheckFallingAtStart()
        {
            yield return new WaitForSeconds(0.5f);
            while (!IsAllItemsFallDown())
            {
                yield return new WaitForSeconds(0.1f);
            }
            FindMatches();
        }

        public bool CheckExtraPackage(List<List<Item>> rowItems)
        {
            //     print("set package");
            foreach (List<Item> items in rowItems)
            {
                foreach (Item item in items)
                {
                    if (item.square.FindMatchesAround(FindSeparating.VERTICAL).Count > 2)
                    {
                        if (LevelManager.THIS.lastDraggedItem == null)
                            LevelManager.THIS.lastDraggedItem = item;
                        LevelManager.THIS.latstMatchColor = item.color;
                        //           print(LevelManager.THIS.latstMatchColor);
                        return true;
                    }
                }
            }
            return false;
        }


        IEnumerator FallingDown()
        {
            bool nearEmptySquareDetected = false;
            int combo = 0;
            AI.THIS.allowShowTip = false;
            List<Item> it = GetItems();
            for (int i = 0; i < it.Count; i++)
            {
                Item item = it[i];
                if (item != null)
                {
                    //AI.THIS.StopAllCoroutines();
                    item.anim.StopPlayback();
                }
            }
            //        combineManager.GetCombine();

            while (true)
            {

                //find matches
                yield return new WaitForSeconds(0.1f);

                while (InputSwaps > 0 || !IsAllItemsFallDown()) yield return null;
                int waveRevision = inputRevision;
                foreach (var piece in GetItems()) if (piece) piece.inputMatchPending = false;

                combinedItems.Clear();
                combinedItems = combineManager.GetCombine(); //GetMatches();  //1.6
                foreach (var group in combinedItems) foreach (var piece in group) if (piece) piece.inputMatchPending = true;
                foreach (var piece in GetItems()) if (piece) piece.inputMoveOrder = 0;
                //StartCoroutine(GetMatchesCor());
                //while (!matchesGot)
                //    yield return new WaitForFixedUpdate();
                //combinedItems = newCombines;
                //matchesGot = false;
                //   print(LevelManager.THIS.latstMatchColor);
                //			if (LevelManager.THIS.CheckExtraPackage (GetMatches (FindSeparating.HORIZONTAL)) && lastSwitchedItem != null) {
                //
                //				if (LevelManager.THIS.latstMatchColor == lastDraggedItem.color && LevelManager.THIS.lastDraggedItem.NextType == ItemsTypes.NONE)
                //					LevelManager.THIS.lastDraggedItem.NextType = ItemsTypes.PACKAGE;
                //				else if (LevelManager.THIS.latstMatchColor == lastSwitchedItem.color && LevelManager.THIS.lastDraggedItem.NextType == ItemsTypes.NONE)
                //					LevelManager.THIS.lastSwitchedItem.NextType = ItemsTypes.PACKAGE;
                //				lastDraggedItem.ChangeType ();
                //				lastSwitchedItem.ChangeType ();
                //
                //			}

                if (combinedItems.Count > 0)
                    combo++;
                if(RoyalThrone.Campaign.RoyalReferenceCampaign.Active)RoyalThrone.Campaign.RoyalCampaignBlockers.BeginMatchWave();
                foreach (List<Item> desrtoyItems in combinedItems)
                {

                    //				if (lastDraggedItem == null) {  //1.6
                    //					if (desrtoyItems.Count == 4) {
                    //						if (lastDraggedItem == null)
                    //							lastDraggedItem = desrtoyItems [UnityEngine.Random.Range (0, desrtoyItems.Count)];
                    //						lastDraggedItem.NextType = (ItemsTypes)UnityEngine.Random.Range (1, 3);
                    //						//lastDraggedItem.ChangeType();
                    //					}
                    //					if (desrtoyItems.Count >= 5) {
                    //						if (lastDraggedItem == null)
                    //							lastDraggedItem = desrtoyItems [UnityEngine.Random.Range (0, desrtoyItems.Count)];
                    //						lastDraggedItem.NextType = ItemsTypes.BOMB;
                    //						//lastDraggedItem.ChangeType();
                    //					}
                    //
                    //				}
                    // if (desrtoyItems.Count > 0) PopupScore(scoreForItem * desrtoyItems.Count, desrtoyItems[(int)desrtoyItems.Count / 2].transform.position, Color.black);
                    // --- presentation only: no match/score/target logic below ------
                    // Emphasis scales with how big the match is and how deep the
                    // cascade has gone, inside a hard ceiling from JuiceConfig.
                    var juiceCfg = Juice.JuiceConfig.Instance;
                    float groupIntensity = Juice.MatchVFXService.ComputeIntensity(desrtoyItems.Count, combo);

                    if (juiceCfg.EnableJuice)
                    {
                        Vector3 matchCenter = Vector3.zero;
                        int centerCount = 0;
                        for (int di = 0; di < desrtoyItems.Count; di++)
                        {
                            if (desrtoyItems[di] != null)
                            {
                                matchCenter += desrtoyItems[di].transform.position;
                                centerCount++;
                            }
                        }
                        if (centerCount > 0)
                        {
                            matchCenter /= centerCount;
                            Juice.RoyalPowerupDirector.Get?.Match(matchCenter, groupIntensity);
                            Item mid = desrtoyItems[Mathf.Clamp(desrtoyItems.Count / 2, 0, desrtoyItems.Count - 1)];
                            Color centerColor = mid != null ? mid.GetJuiceColor() : Color.white;
                            var vfxService = Juice.MatchVFXService.Instance;
                            if (vfxService != null)
                            {
                                // One restored group accent, aligned with the gentler sprite pop.
                                float centerDelay = .12f;
                                StartCoroutine(PlayMatchCenterDelayed(matchCenter, centerColor, groupIntensity, centerDelay));

                                // One micro-freeze for the whole group, timed to the pop.
                                // Per-fruit would read as lag in a long cascade.
                                vfxService.RequestHitStop(
                                    centerDelay,
                                    juiceCfg.HitStopDuration +
                                        juiceCfg.HitStopIntensityBonus * (groupIntensity - 1f),
                                    juiceCfg.HitStopScale);
                            }
                        }
                    }

                    int juiceIndex = 0;
                    foreach (Item item in desrtoyItems)
                    {//TODO items not destroy

                        if (!item || item.destroying) continue;

                        if (item.currentType != ItemsTypes.NONE)
                            yield return new WaitForSeconds(0.1f);
                        if (!item || item.destroying) continue;
                        // Tiny controlled offset between neighbours - still reads as
                        // simultaneous, but gives the group a visual rhythm.
                        item.juiceStagger = juiceCfg.EnableJuice ? juiceIndex * juiceCfg.MatchStagger : 0f;
                        item.juiceIntensity = groupIntensity;
                        if (item.currentType == ItemsTypes.NONE && !item.destroying)
                            item.royalMatchSize = desrtoyItems.Count;
                        juiceIndex++;
                        item.DestroyItem(true);  //destroy items safely
                        if (item.currentType != ItemsTypes.NONE)
                        {
                            //while (!item.animationFinished)
                            //{
                            //    yield return new WaitForFixedUpdate();
                            //}
                        }

                    }
                }

                foreach (Item item in destroyAnyway)
                {
                    //  if(item.sprRenderer.enabled)
                    item.DestroyItem(true, "", true);  //destroy items safely
                    //yield return new WaitForSeconds(0.2f);
                }
                //          if (destroyAnyway.Count > 0) PopupScore(scoreForItem * destroyAnyway.Count, destroyAnyway[(int)destroyAnyway.Count / 2].transform.position);
                destroyAnyway.Clear();

                if (lastDraggedItem != null)
                {
                    //
                    //				if (LevelManager.THIS.CheckExtraPackage (GetMatches (FindSeparating.HORIZONTAL))) {  //1.6
                    //					if (LevelManager.THIS.latstMatchColor == lastDraggedItem.color)
                    //						LevelManager.THIS.lastDraggedItem.NextType = ItemsTypes.PACKAGE;
                    //					else if (LevelManager.THIS.latstMatchColor == lastSwitchedItem.color)
                    //						LevelManager.THIS.lastSwitchedItem.NextType = ItemsTypes.PACKAGE;
                    //					lastDraggedItem.ChangeType ();
                    //					lastSwitchedItem.ChangeType ();
                    //
                    //				}
                    if (lastDraggedItem.NextType != ItemsTypes.NONE)
                    {
                        //lastDraggedItem.ChangeType();
                        yield return new WaitForSeconds(0.5f);

                    }
                    lastDraggedItem = null;
                }

                while (!IsAllDestoyFinished())
                {
                    if (waveRevision != inputRevision && InputSwaps == 0) break;
                    yield return new WaitForSeconds(0.1f);
                }

                // A new move during the effects needs its matches resolved before gravity.
                if (waveRevision != inputRevision) continue;

                //falling down
                for (int i = 0; i < 20; i++)
                {   //just for testing
                    for (int col = 0; col < maxCols; col++)
                    {
                        for (int row = maxRows - 1; row >= 0; row--)
                        {   //need to enumerate rows from bottom to top
                            if (GetSquare(col, row) != null)
                                GetSquare(col, row).FallOut();
                        }
                    }
                    // yield return new WaitForFixedUpdate();
                }
                if (!nearEmptySquareDetected)
                    yield return new WaitForSeconds(Juice.RoyalEffectsProfile.Get ? .035f : .2f);

                while (InputSwaps > 0) yield return null;

                CheckIngredient();
                for (int col = 0; col < maxCols; col++)
                {
                    for (int row = maxRows - 1; row >= 0; row--)
                    {
                        if (GetSquare(col, row) != null)
                        {
                            if (!GetSquare(col, row).IsNone())
                            {
                                if (GetSquare(col, row).item != null)
                                {
                                    GetSquare(col, row).item.StartFalling();
                                    //if (row == maxRows - 1 && GetSquare(col, row).item.currentType == ItemsTypes.INGREDIENT)
                                    //{
                                    //    destroyAnyway.Add(GetSquare(col, row).item);
                                    //}
                                }
                            }
                        }
                    }
                }
                yield return new WaitForSeconds(0.2f);
                GenerateNewItems();
                StartCoroutine(RegenMatches(true));
                yield return new WaitForSeconds(0.1f);
                while (!IsAllItemsFallDown())
                {
                    yield return new WaitForSeconds(0.1f);
                }

                //detect near empty squares to fall into
                nearEmptySquareDetected = false;

                for (int col = 0; col < maxCols; col++)
                {
                    for (int row = maxRows - 1; row >= 0; row--)
                    {
                        if (GetSquare(col, row) != null)
                        {
                            if (!GetSquare(col, row).IsNone())
                            {
                                if (GetSquare(col, row).item != null)
                                {
                                    if (GetSquare(col, row).item.GetNearEmptySquares())
                                        nearEmptySquareDetected = true;

                                }
                            }
                        }
                        // if (nearEmptySquareDetected) break;
                    }
                    //   if (nearEmptySquareDetected) break;
                }
                //StartCoroutine(GetMatchesCor());
                //while (!matchesGot)
                //    yield return new WaitForFixedUpdate();
                //matchesGot = false;
                //CheckIngredient();
                while (!IsAllItemsFallDown())
                {//2.0
                    yield return new WaitForSeconds(0.1f);
                }

                if (waveRevision != inputRevision || Juice.RoyalPowerupDirector.Busy) continue;

                if (destroyAnyway.Count > 0)
                    nearEmptySquareDetected = true;
                if (GetMatches().Count <= 0 && !nearEmptySquareDetected)
                    break;
            }

            List<Item> item_ = GetItems();
            for (int i = 0; i < item_.Count; i++)//2.1.5
            {
                Item item1 = item_[i];
                if (item1 != null)
                {
                    if (item1 != item1.square.item)
                    {
                        Destroy(item1.gameObject);
                    }
                }
            }

            //thrive thriving blocks
            if (!thrivingBlockDestroyed)
            {
                bool thrivingBlockSelected = false;
                for (int col = 0; col < maxCols; col++)
                {
                    if (thrivingBlockSelected)
                        break;
                    for (int row = maxRows - 1; row >= 0; row--)
                    {
                        if (thrivingBlockSelected)
                            break;
                        if (GetSquare(col, row) != null)
                        {
                            if (GetSquare(col, row).type == SquareTypes.THRIVING)
                            {
                                List<Square> sqList = GetSquare(col, row).GetAllNeghbors();
                                foreach (Square sq in sqList)
                                {
                                    if (sq.CanGoInto() && UnityEngine.Random.Range(0, 1) == 0 && sq.type == SquareTypes.EMPTY)
                                    {
                                        if (sq.item != null)
                                        {//1.6.1
                                            if (sq.item.currentType == ItemsTypes.NONE)
                                            {//1.6.1
                                                //GetSquare(col, row).GenThriveBlock(sq);
                                                CreateObstacles(sq.col, sq.row, sq.gameObject, SquareTypes.THRIVING);

                                                thrivingBlockSelected = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }


            thrivingBlockDestroyed = false;

            if (gameStatus == GameState.Playing && !ingredientFly)
                LevelManager.THIS.CheckWinLose();

            if (combo > 2 && gameStatus == GameState.Playing)
            {
                Juice.RoyalCelebration.Play(combo);
                combo = 0;
            }
            LevelManager.THIS.latstMatchColor = -1;
            CheckItemsPositions();//1.6.1
            royalResolving = false;
            DragBlocked = false;//2.1.5 prevents early move
            if (gameStatus == GameState.Playing)
                StartCoroutine(AI.THIS.CheckPossibleCombines());




        }

        void CheckItemsPositions()
        {//1.6.1
            List<Item> items = GetItems();
            foreach (var item in items)
            {
                if (item)
                    item.transform.position = item.square.transform.position + Vector3.back * 0.2f;
            }
        }


        public void DestroyDoubleBomb(int col)
        {
            StartCoroutine(DestroyDoubleBombCor(col));
            StartCoroutine(DestroyDoubleBombCorBack(col));
        }

        IEnumerator DestroyDoubleBombCor(int col)
        {
            for (int i = col; i < maxCols; i++)
            {
                List<Item> list = GetColumn(i);
                foreach (Item item in list)
                {
                    if (item != null)
                        item.DestroyItem(true, "", true);
                }
                yield return new WaitForSeconds(0.3f);
                //GenerateNewItems();
                //yield return new WaitForSeconds(0.3f);
            }
            if (col <= maxCols - col - 1)
                FindMatches();
        }

        IEnumerator DestroyDoubleBombCorBack(int col)
        {
            for (int i = col - 1; i >= 0; i--)
            {
                List<Item> list = GetColumn(i);
                foreach (Item item in list)
                {
                    if (item != null)
                        item.DestroyItem(true, "", true);
                }
                yield return new WaitForSeconds(0.3f);
                //GenerateNewItems();
                //yield return new WaitForSeconds(0.3f);
            }
            if (col > maxCols - col - 1)
                FindMatches();
        }


        public Square GetSquare(int col, int row, bool safe = false)
        {
            if (!safe)
            {
                if (row < 0 || col < 0 || row >= maxRows || col >= maxCols)
                    return null;
                return squaresArray[row * maxCols + col];
            }
            else
            {
                row = Mathf.Clamp(row, 0, maxRows - 1);
                col = Mathf.Clamp(col, 0, maxCols - 1);
                return squaresArray[row * maxCols + col];
            }
        }

        bool IsAllDestoyFinished()
        {
            if (InputSwaps > 0) return false;
            if (Juice.RoyalPowerupDirector.Busy) return false;
            GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
            foreach (GameObject item in items)
            {
                Item itemComponent = item.GetComponent<Item>();
                if (itemComponent == null)
                {
                    return false;
                }
                if (itemComponent.destroying && !itemComponent.animationFinished)
                    return false;
            }
            return true;
        }

        bool IsIngredientFalling()
        {//1.6.1
            if (gameStatus == GameState.PreWinAnimations)
                return true;
            GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
            foreach (GameObject item in items)
            {
                Item itemComponent = item.GetComponent<Item>();
                if (itemComponent != null)
                {
                    if (itemComponent.falling && itemComponent.currentType == ItemsTypes.INGREDIENT)
                        return true;
                }
            }
            return false;

        }

        bool IsAllItemsFallDown()
        {
            if (InputSwaps > 0) return false;
            if (gameStatus == GameState.PreWinAnimations)
                return true;
            GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
            foreach (GameObject item in items)
            {
                Item itemComponent = item.GetComponent<Item>();
                if (itemComponent == null)
                {
                    return false;
                }
                if (itemComponent.falling)
                    return false;
            }
            return true;
        }

        public Vector2 GetPosition(Item item)
        {
            return GetPosition(item.square);
        }

        public Vector2 GetPosition(Square square)
        {
            return new Vector2(square.col, square.row);
        }

        public List<Item> GetRow(int row)
        {
            List<Item> itemsList = new List<Item>();
            for (int col = 0; col < maxCols; col++)
            {
                itemsList.Add(GetSquare(col, row, true).item);
            }
            return itemsList;
        }

        public List<Item> GetColumn(int col)
        {
            List<Item> itemsList = new List<Item>();
            for (int row = 0; row < maxRows; row++)
            {
                itemsList.Add(GetSquare(col, row, true).item);
            }
            return itemsList;
        }

        public List<Square> GetColumnSquaresObstacles(int col)
        {
            List<Square> itemsList = new List<Square>();
            for (int row = 0; row < maxRows; row++)
            {
                if (GetSquare(col, row, true).IsHaveDestroybleObstacle())
                    itemsList.Add(GetSquare(col, row, true));
            }
            return itemsList;
        }

        public List<Square> GetRowSquaresObstacles(int row)
        {
            List<Square> itemsList = new List<Square>();
            for (int col = 0; col < maxCols; col++)
            {
                if (GetSquare(col, row, true).IsHaveDestroybleObstacle())
                    itemsList.Add(GetSquare(col, row, true));
            }
            return itemsList;
        }

        public List<Item> GetItemsAround(Square square)
        {
            int col = square.col;
            int row = square.row;
            List<Item> itemsList = new List<Item>();
            for (int r = row - 1; r <= row + 1; r++)
            {
                for (int c = col - 1; c <= col + 1; c++)
                {
                    itemsList.Add(GetSquare(c, r, true).item);
                }
            }
            return itemsList;
        }

        public List<Item> GetItemsCross(Square square, List<Item> exceptList = null, int COLOR = -1)
        {
            if (exceptList == null)
                exceptList = new List<Item>();
            int c = square.col;
            int r = square.row;
            List<Item> itemsList = new List<Item>();
            Item item = null;
            item = GetSquare(c - 1, r, true).item;
            if (exceptList.IndexOf(item) <= -1)
            {
                if (item.color == COLOR || COLOR == -1)
                    itemsList.Add(item);
            }
            item = GetSquare(c + 1, r, true).item;
            if (exceptList.IndexOf(item) <= -1)
            {
                if (item.color == COLOR || COLOR == -1)
                    itemsList.Add(item);
            }
            item = GetSquare(c, r - 1, true).item;
            if (exceptList.IndexOf(item) <= -1)
            {
                if (item.color == COLOR || COLOR == -1)
                    itemsList.Add(item);
            }
            item = GetSquare(c, r + 1, true).item;
            if (exceptList.IndexOf(item) <= -1)
            {
                if (item.color == COLOR || COLOR == -1)
                    itemsList.Add(item);
            }

            return itemsList;
        }

        public List<Item> GetItems()
        {
            List<Item> itemsList = new List<Item>();
            for (int row = 0; row < maxRows; row++)
            {
                for (int col = 0; col < maxCols; col++)
                {
                    if (GetSquare(col, row) != null)
                        itemsList.Add(GetSquare(col, row, true).item);
                }
            }
            return itemsList;
        }

        public void SetTypeByColor(int p, ItemsTypes nextType)
        {
            GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
            foreach (GameObject item in items)
            {
                if (item.GetComponent<Item>().color == p)
                {
                    if (nextType == ItemsTypes.HORIZONTAL_STRIPPED || nextType == ItemsTypes.VERTICAL_STRIPPED)
                        item.GetComponent<Item>().NextType = (ItemsTypes)UnityEngine.Random.Range(1, 3);
                    else
                        item.GetComponent<Item>().NextType = nextType;

                    item.GetComponent<Item>().ChangeType();
                    if (nextType == ItemsTypes.NONE)
                        destroyAnyway.Add(item.GetComponent<Item>());
                }
            }
        }

        public void CheckIngredient()
        {
            int row = maxRows;
            List<Square> sqList = GetBottomRow();
            foreach (Square sq in sqList)
            {
                if (sq.item != null)
                {
                    if (sq.item.currentType == ItemsTypes.INGREDIENT)
                    {
                        destroyAnyway.Add(sq.item);
                    }
                }
            }
        }

        public List<Square> GetBottomRow()
        {
            List<Square> itemsList = new List<Square>();
            int listCounter = 0;
            for (int col = 0; col < maxCols; col++)
            {
                for (int row = maxRows - 1; row >= 0; row--)
                {
                    Square square = GetSquare(col, row, true);
                    if (square.type != SquareTypes.NONE)
                    {
                        itemsList.Add(square);
                        listCounter++;
                        break;
                    }
                }
            }
            return itemsList;
        }

        IEnumerator StartIdleCor()
        {
            for (int col = 0; col < maxCols; col++)
            {
                for (int row = 0; row < maxRows; row++)
                {
                    // GetSquare(col, row, true).item.anim.SetBool("stop", false);
                    if (GetSquare(col, row, true).item != null)
                        GetSquare(col, row, true).item.StartIdleAnim();
                }

            }

            yield return new WaitForFixedUpdate();
        }

        public void StrippedShow(GameObject obj, bool horrizontal)
        {
            GameObject effect = Instantiate(stripesEffect, obj.transform.position, Quaternion.identity) as GameObject;
            if (!horrizontal)
                effect.transform.Rotate(Vector3.back, 90);
            Destroy(effect, 1);
        }

        public void PopupScore(int value, Vector3 pos, int color)
        {
            Score += value;
            if( targetObject.Any(i => i.type == Target.SCORE))
                targetObject.First(i => i.type == Target.SCORE).SetOff(null, value);
            UpdateBar();
            CheckStars();
            if (showPopupScores)
            {
                Transform parent = canvasScore;
                GameObject poptxt = Instantiate(popupScore, pos, Quaternion.identity) as GameObject;
                poptxt.transform.GetComponentInChildren<Text>().text = "" + value;
                if (color <= scoresColors.Length - 1)
                {
                    poptxt.transform.GetComponentInChildren<Text>().color = scoresColors[Mathf.Clamp(color, 0, scoresColors.Length)];
                    poptxt.transform.GetComponentInChildren<Outline>().effectColor = scoresColorsOutline[Mathf.Clamp(color, 0, scoresColorsOutline.Length)];
                }
                poptxt.transform.SetParent(parent);
                //   poptxt.transform.position += Vector3.right * 1;
                poptxt.transform.position = pos;//2.1.6
                poptxt.transform.localScale = Vector3.one / 1.5f;
                Destroy(poptxt, 0.3f);
            }
        }

        void UpdateBar()
        {
            ProgressBarScript.Instance.UpdateDisplay((float)Score * 100f / ((float)star1 / ((star1 * 100f / star3)) * 100f) / 100f);

        }

        void CheckStars()
        {
            if (Score >= star1 && stars <= 0)
            {
                stars = 1;
            }
            // A win is worth exactly one star (the reference; the user's decision, 2026-09-29): the score no longer adds
            // a second or third. The star markers below still light up with the score.

            if (Score >= star1)
            {
                if (!star1Anim.activeSelf)
                    SoundBase.Instance.PlaySound(SoundBase.Instance.getStarIngr);
                star1Anim.SetActive(true);
            }
            if (Score >= star2)
            {
                if (!star2Anim.activeSelf)
                    SoundBase.Instance.PlaySound(SoundBase.Instance.getStarIngr);
                star2Anim.SetActive(true);
            }
            if (Score >= star3)
            {
                if (!star3Anim.activeSelf)
                    SoundBase.Instance.PlaySound(SoundBase.Instance.getStarIngr);
                star3Anim.SetActive(true);
            }
        }

        public void LoadDataFromLocal(int currentLevel)
        {
            levelLoaded = false;
            if(RoyalThrone.Campaign.RoyalReferenceCampaign.TryLoad(this,currentLevel,out var referenceBoard))
            {levelSquaresFile=referenceBoard;return;}
            //Read data from text file
            TextAsset mapText = Resources.Load("Levels/" + currentLevel) as TextAsset;
            if (mapText == null)
            {
                mapText = Resources.Load("Levels/" + currentLevel) as TextAsset;
            }
            ProcessGameDataFromString(mapText.text);
        }

        void ProcessGameDataFromString(string mapText)
        {
            string[] lines = mapText.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            int mapLine = 0;
            foreach (string line in lines)
            {
                //check if line is game mode line
                if (line.StartsWith("MODE"))
                {
                    //Replace GM to get mode number,
                    string modeString = line.Replace("MODE", string.Empty).Trim();
                    //then parse it to interger
                    target = (Target)int.Parse(modeString);
                    //Assign game mode
                }
                else if (line.StartsWith("SIZE "))
                {
                    string blocksString = line.Replace("SIZE", string.Empty).Trim();
                    string[] sizes = blocksString.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                    maxCols = int.Parse(sizes[0]);
                    maxRows = int.Parse(sizes[1]);
                    squaresArray = new Square[maxCols * maxRows];
                    levelSquaresFile = new SquareBlocks[maxRows * maxCols];
                    for (int i = 0; i < levelSquaresFile.Length; i++)
                    {

                        SquareBlocks sqBlocks = new SquareBlocks();
                        sqBlocks.block = SquareTypes.EMPTY;
                        sqBlocks.obstacle = SquareTypes.NONE;

                        levelSquaresFile[i] = sqBlocks;
                    }
                }
                else if (line.StartsWith("LIMIT"))
                {
                    string blocksString = line.Replace("LIMIT", string.Empty).Trim();
                    string[] sizes = blocksString.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                    limitType = (LIMIT)int.Parse(sizes[0]);
                    Limit = int.Parse(sizes[1]);
                }
                else if (line.StartsWith("COLOR LIMIT "))
                {
                    string blocksString = line.Replace("COLOR LIMIT", string.Empty).Trim();
                    colorLimit = Mathf.Clamp(int.Parse(blocksString), 1, Juice.RoyalGameplayArt.BoardColorCount);
                }
                else if (line.StartsWith("STARS"))
                {
                    string blocksString = line.Replace("STARS", string.Empty).Trim();
                    string[] blocksNumbers = blocksString.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                    star1 = int.Parse(blocksNumbers[0]);
                    star2 = int.Parse(blocksNumbers[1]);
                    star3 = int.Parse(blocksNumbers[2]);
                    if (ProgressBarScript.Instance != null)//2.1.2
                        ProgressBarScript.Instance.InitBar();//2.1.2
                }
                else if (line.StartsWith("COLLECT COUNT "))
                {
                    string blocksString = line.Replace("COLLECT COUNT", string.Empty).Trim();
                    string[] blocksNumbers = blocksString.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < blocksNumbers.Length; i++)
                    {
                        ingrCountTarget[i] = int.Parse(blocksNumbers[i]);
                    }
                }
                else if (line.StartsWith("COLLECT ITEMS "))
                {
                    string blocksString = line.Replace("COLLECT ITEMS", string.Empty).Trim();
                    string[] blocksNumbers = blocksString.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < blocksNumbers.Length; i++)
                    {
                        if (target == Target.INGREDIENT)
                            ingrTarget[i] = (Ingredients)int.Parse(blocksNumbers[i]);
                        else if (target == Target.COLLECT)
                            collectItems[i] = (CollectItems)int.Parse(blocksNumbers[i]);

                    }
                }
                else
                { //Maps
                    //Split lines again to get map numbers
                    string[] st = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < st.Length; i++)
                    {
                        levelSquaresFile[mapLine * maxCols + i].block = (SquareTypes)int.Parse(st[i][0].ToString());
                        levelSquaresFile[mapLine * maxCols + i].obstacle = (SquareTypes)int.Parse(st[i][1].ToString());
                    }
                    mapLine++;
                }

                var tar = Resources.Load<TargetLevel>("Targets/Level" + currentLevel);
                targetObject = new TargetObject[tar.targets.Length];
                for (var index = 0; index < tar.targets.Count; index++)
                {
                    var tarTarget = tar.targets[index];
                    targetObject[index] = tarTarget.DeepCopy();
                }
            }
            levelLoaded = true;
        }

    }

    [global::System.Serializable]
    public class GemProduct
    {
        public int count;
        public float price;
    }
}
