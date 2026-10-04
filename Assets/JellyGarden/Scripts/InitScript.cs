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
using System.Collections.Generic;
using System.Globalization;
using JellyGarden.Scripts.GUI;
using UnityEngine.Events;
using JellyGarden.Scripts.Integrations;
using JellyGarden.Scripts.MapScripts;
using JellyGarden.Scripts.System;
using UnityEngine;
#if UNITY_ADS
using UnityEngine.Advertisements;
#endif

#if CHARTBOOST_ADS
using ChartboostSDK;
#endif
#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif
using UnityEngine.EventSystems;

namespace JellyGarden.Scripts
{
    #if FACEBOOK
using Facebook.Unity;
    #endif


    public enum Target
    {
        SCORE,
        COLLECT,
        INGREDIENT,
        BLOCKS
    }

    public enum LIMIT
    {
        MOVES,
        TIME
    }

    public enum Ingredients
    {
        None = 0,
        Ingredient1,
        Ingredient2
    }

    public enum CollectItems
    {
        None = 0,
        Item1,
        Item2,
        Item3,
        Item4,
        Item5,
        Item6
    }

    public enum RewardedAdsType
    {
        NONE,
        GetLifes,
        GetGems,
        GetGoOn
    }

    public class InitScript : MonoBehaviour
    {
        public static InitScript Instance;
        public static int openLevel;


        public static float RestLifeTimer;
        public static string DateOfExit;
        // The refill schedule is a single absolute UTC deadline so it survives scene loads,
        // backgrounding and quitting, and so elapsed time is never counted twice or lost.
        public const string NextLifeKey = "RoyalThrone.NextLifeUtc";
        static DateTime nextLifeUtc;
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !ROYAL_DISABLE_QA
        internal DateTime LifeRefillDeadline => nextLifeUtc;
        // Restoring a QA snapshot must not persist a new deadline to the player's save.
        internal void RestoreLifeScheduleForTesting(DateTime deadline, float remaining)
        {
            nextLifeUtc = deadline;
            RestLifeTimer = remaining;
        }
#endif
        public static DateTime today;
        public static DateTime DateOfRestLife;
        public static string timeForReps;
        private static int Lifes;

        bool loginForSharing;

        public RewardedAdsType currentReward;

        public static int lifes
        {
            get { return InitScript.Lifes; }
            set { InitScript.Lifes = value; }
        }

        public int CapOfLife = 5;
        public float TotalTimeForRestLifeHours = 0;
        public float TotalTimeForRestLifeMin = 5;
        public float TotalTimeForRestLifeSec = 0;
        public int FirstGems = 20;
        public static int Gems;
        public static int waitedPurchaseGems;
        private int BoostExtraMoves;
        private int BoostPackages;
        private int BoostStripes;
        private int BoostExtraTime;
        private int BoostBomb;
        private int BoostColorful_bomb;
        private int BoostHand;
        private int BoostRandom_color;
        public List<AdEvents> adsEvents = new List<AdEvents>();

        public static bool sound = false;
        public static bool music = false;
        private bool adsReady;
        public bool enableUnityAds;
        public bool enableGoogleMobileAds;
        public bool enableChartboostAds;
        public string rewardedVideoZone;
        public string nonRewardedVideoZone;
        public int ShowChartboostAdsEveryLevel;
        public int ShowAdmobAdsEveryLevel;
        public int dailyRewardedFrequency; //2.2.2
        public RewardedAdsTime dailyRewardedFrequencyTime; //2.2.3
        public int[] dailyRewardedShown;
        public DateTime[] dailyRewardedShownDate;
        #if GOOGLE_MOBILE_ADS
    private InterstitialAd interstitial;
    private AdRequest requestAdmob;
        #endif
        public string admobUIDAndroid;
        public string admobUIDIOS;
        public string admobRewardedUIDAndroid;
        public string admobRewardedUIDIOS;
        public bool LoginEnable;

        public int ShowRateEvery;
        public string RateURL;
        public int rewardedGems = 5;
        public bool losingLifeEveryGame;
        public static Sprite profilePic;
        public GameObject facebookButton;
        private string lastResponse = string.Empty;

        public UnityEvent OnAdLoadedEvent;
        public UnityEvent OnAdFailedToLoadEvent;
        public UnityEvent OnAdOpeningEvent;
        public UnityEvent OnAdFailedToShowEvent;
        public UnityEvent OnUserEarnedRewardEvent;
        public UnityEvent OnAdClosedEvent;

        protected string LastResponse
        {
            get { return this.lastResponse; }

            set { this.lastResponse = value; }
        }

        private string status = "Ready";
        public string RateURLIOS; //2.1.5

        protected string Status
        {
            get { return this.status; }

            set { this.status = value; }
        }

        public UnityAdsID unityAds;

        // Use this for initialization
        void Awake()
        {
            Application.targetFrameRate = 60;
            Instance = this;
            RestLifeTimer = PlayerPrefs.GetFloat("RestLifeTimer");
            DateOfExit = PlayerPrefs.GetString("DateOfExit", "");
            Gems = PlayerPrefs.GetInt("Gems");
            lifes = PlayerPrefs.GetInt("Lifes");
            {
                //2.2.2 rewarded limit
                dailyRewardedShown = new int[Enum.GetValues(typeof(RewardedAdsType)).Length];
                dailyRewardedShownDate = new DateTime[Enum.GetValues(typeof(RewardedAdsType)).Length];
                for (int i = 0; i < dailyRewardedShown.Length; i++)
                {
                    dailyRewardedShown[i] = PlayerPrefs.GetInt(((RewardedAdsType)i).ToString());
                    dailyRewardedShownDate[i] = DateTimeManager.GetLastDateTime(((RewardedAdsType)i).ToString());
                }
            }
            if (PlayerPrefs.GetInt("Lauched") == 0)
            {
                //First lauching
                lifes = CapOfLife;
                PlayerPrefs.SetInt("Lifes", lifes);
                Gems = Juice.RoyalEconomy.StartingCoins;   // the coin rules' starting purse, not the inspector's FirstGems
                PlayerPrefs.SetInt("Gems", Gems);
                PlayerPrefs.SetInt("Music", 1);
                PlayerPrefs.SetInt("Sound", 1);

                PlayerPrefs.SetInt("Lauched", 1);
                PlayerPrefs.Save();
            }

            LoadLifeSchedule();

            if (gameObject.GetComponent<AspectCamera>() == null)
                gameObject.AddComponent<AspectCamera>().map = FindAnyObjectByType<LevelsMap>().transform
                    .Find("map_background_01").GetComponent<SpriteRenderer>()
                    .sprite;

            MusicBase.Instance.SetVolume(PlayerPrefs.GetInt("Music"));
            SoundBase.Instance.SetVolume(PlayerPrefs.GetInt("Sound"));

            if (!RoyalThrone.RoyalOfflineMode.ServicesEnabled)
            {
                enableUnityAds = enableGoogleMobileAds = enableChartboostAds = false;
                if (facebookButton) facebookButton.SetActive(false);
                return;
            }

            #if UNITY_ADS //2.1.1
        enableUnityAds = true;
        unityAds = Resources.Load<UnityAdsID>("UnityAdsID");
        gameObject.AddComponent<UnityAdsController>();
        UnityAdsController.Instance.InitAds(unityAds.androidID, unityAds.iOSID);
            #else
            enableUnityAds = false;
            #endif
            #if CHARTBOOST_ADS
        enableChartboostAds = true;
            #else
            enableChartboostAds = false;
            #endif

            #if FACEBOOK
        FacebookManager fbManager = gameObject.AddComponent<FacebookManager>();
        fbManager.facebookButton = facebookButton;
            #endif

            #if GOOGLE_MOBILE_ADS
        enableGoogleMobileAds = true;
#if UNITY_ANDROID
        MobileAds.Initialize(initStatus => { });
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        LoadInterstitialAd(admobUIDAndroid);
#elif UNITY_IOS
        MobileAds.Initialize(initStatus => { });
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        LoadInterstitialAd(admobUIDIOS);
#else
        MobileAds.Initialize(initStatus => { });
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        LoadInterstitialAd(admobUIDAndroid);
#endif
        requestAdmob = new AdRequest();
            #else
            enableGoogleMobileAds = false;
            #endif
        }

        #if GOOGLE_MOBILE_ADS

    /*private AdRequest CreateAdRequest()
    {
        return new AdRequest.Builder().Build();
    }*/

    public void LoadInterstitialAd(string _adUnitId)
    {
        if (!RoyalThrone.RoyalOfflineMode.ServicesEnabled) return;
        // Clean up the old ad before loading a new one.
        if (interstitial != null)
        {
            interstitial.Destroy();
            interstitial = null;
        }

        Debug.Log("Loading the interstitial ad.");

        // send the request to load the ad.
        // Load an interstitial ad
        InterstitialAd.Load(_adUnitId, new AdRequest(),
            (InterstitialAd ad, LoadAdError loadError) =>
            {
                if (loadError != null)
                {
                    Debug.Log("Interstitial ad failed to load with error: " +
                              loadError.GetMessage());
                    return;
                }
                else if (ad == null)
                {
                    Debug.Log("Interstitial ad failed to load.");
                    return;
                }

                Debug.Log("Interstitial ad loaded.");
                interstitial = ad;

                ad.OnAdFullScreenContentOpened += () =>
                {
                    Debug.Log("Interstitial ad opening.");
                    OnAdOpeningEvent.Invoke();
                };
                ad.OnAdFullScreenContentClosed += () =>
                {
                    Debug.Log("Interstitial ad closed.");
                    OnAdClosedEvent.Invoke();
                };
                ad.OnAdImpressionRecorded += () => { Debug.Log("Interstitial ad recorded an impression."); };
                ad.OnAdClicked += () => { Debug.Log("Interstitial ad recorded a click."); };
                ad.OnAdFullScreenContentFailed += (AdError error) =>
                {
                    Debug.Log("Interstitial ad failed to show with error: " +
                              error.GetMessage());
                };
                ad.OnAdPaid += (AdValue adValue) =>
                {
                    string msg = string.Format("{0} (currency: {1}, value: {2}",
                        "Interstitial ad received a paid event.",
                        adValue.CurrencyCode,
                        adValue.Value);
                    Debug.Log(msg);
                };
            });
    }

        #endif


        // The inherited kit starred every level when Left Ctrl (or U) was released, so an
        // ordinary Ctrl+S while the game had focus rewrote the player's whole progress.
        // Star control now lives in Settings -> Mod Menu, which is save-isolated.
        // Update() then only tracked that key in a field nothing read, so it was removed.

        public void SaveLevelStarsCount(int level, int starsCount)
        {
            Debug.Log(string.Format("Stars count {0} of level {1} saved.", starsCount, level));
            PlayerPrefs.SetInt(GetLevelKey(level), starsCount);
        }

        private string GetLevelKey(int number)
        {
            return string.Format("Level.{0:000}.StarsCount", number);
        }

        public bool GetRewardedUnityAdsReady()
        {
            if (!RoyalThrone.RoyalOfflineMode.ServicesEnabled) return false;
            #if UNITY_ADS
        return UnityAdsController.Instance.isLoaded;
            #endif

            return false;
        }

        public void ShowRewardedAds()
        {
            if (!RoyalThrone.RoyalOfflineMode.ServicesEnabled) return;
            #if UNITY_ADS
        Debug.Log("show Unity Rewarded ads video in " + LevelManager.THIS.gameStatus);

        if (GetRewardedUnityAdsReady())
        {
#if UNITY_ANDROID
            UnityAdsController.Instance.ShowAds(unityAds.unityRewardedAndroid);
#elif UNITY_IOS
                        UnityAdsController.Instance.ShowAds(unityAds.unityRewardedIOS);
#endif
        }

            #elif GOOGLE_MOBILE_ADS //2.2
        bool stillShow = true;
#if UNITY_ADS
        stillShow = !GetRewardedUnityAdsReady ();
#endif
        if(stillShow)
        {
            Debug.Log("show Admob Rewarded ads video in " + LevelManager.THIS.gameStatus);
            RewAdmobManager.THIS.ShowRewardedAd(CheckRewardedAds);
        }
            #endif
        }

        public bool RewardedReachedLimit(RewardedAdsType type) //2.2.2
        {
            if (dailyRewardedFrequency == 0) return false;
            dailyRewardedShown[(int)type] = PlayerPrefs.GetInt(type.ToString());
            if (!DateTimeManager.IsPeriodPassed(type.ToString())) return true;
            if (dailyRewardedFrequency > 0 && dailyRewardedShown[(int)type] >= dailyRewardedFrequency) return true;
            // if (Random.Range(0, 5) > 0) return true;
            dailyRewardedShown[(int)type]++;
            PlayerPrefs.SetInt(type.ToString(), dailyRewardedShown[(int)type]);
            if (dailyRewardedShown[(int)type] >= dailyRewardedFrequency) DateTimeManager.SetDateTimeNow(type.ToString());
            PlayerPrefs.Save();

            return false;
        }

        public void CheckAdsEvents(GameState state)
        {
            if (!RoyalThrone.RoyalOfflineMode.ServicesEnabled) return;
            //1.4 added
            foreach (AdEvents item in adsEvents)
            {
                if (item.gameEvent == state)
                {
                    //1.5   1.6.1
                    //				if ((LevelManager.THIS.gameStatus == GameState.GameOver || LevelManager.THIS.gameStatus == GameState.Pause ||
                    //				    LevelManager.THIS.gameStatus == GameState.Playing || LevelManager.THIS.gameStatus == GameState.PrepareGame || LevelManager.THIS.gameStatus == GameState.PreWinAnimations ||
                    //				    LevelManager.THIS.gameStatus == GameState.RegenLevel || LevelManager.THIS.gameStatus == GameState.Win)) {

                    item.calls++; //1.6
                    if (item.calls % item.everyLevel == 0)
                        ShowAdByType(item.adType);
                    //				} else {
                    //					ShowAdByType (item.adType);
                    //
                    //				}
                }
            }
        }

        void ShowAdByType(AdType adType)
        {
            //1.4 added
            if (adType == AdType.AdmobInterstitial && enableGoogleMobileAds)
                ShowAds(false);
            else if (adType == AdType.UnityAdsVideo && enableUnityAds)
                ShowVideo();
            else if (adType == AdType.UnityAdsInterstitial && enableUnityAds)
                ShowInterstitial();
            else if (adType == AdType.ChartboostInterstitial && enableChartboostAds)
                ShowAds(true);
        }

        public void ShowInterstitial()
        {
            if (!RoyalThrone.RoyalOfflineMode.ServicesEnabled) return;
            Debug.Log("show Unity ads video on " + LevelManager.THIS.gameStatus + "  for Reward ==> " +
                      InitScript.Instance.currentReward);
            #if UNITY_ADS
#if UNITY_ANDROID
        UnityAdsController.Instance.ShowAds(unityAds.unityInterstitialAndroid);

#elif UNITY_IOS
            UnityAdsController.Instance.ShowAds(unityAds.unityInterstitialIOS);
#endif
            #endif
        }

        public void ShowVideo()
        {
            if (!RoyalThrone.RoyalOfflineMode.ServicesEnabled) return;
            //1.4 added
            #if UNITY_ADS
        Debug.Log("show Unity Rewarded ads video in " + LevelManager.THIS.gameStatus + "  for Reward ==> " +
                  InitScript.Instance.currentReward);

        if (GetRewardedUnityAdsReady())
        {
#if UNITY_ANDROID
            UnityAdsController.Instance.ShowAds(unityAds.unityRewardedAndroid);
#elif UNITY_IOS
                        UnityAdsController.Instance.ShowAds(unityAds.unityRewardedIOS);
#endif
        }
            #endif
        }


        public void ShowAds(bool chartboost = true)
        {
            if (!RoyalThrone.RoyalOfflineMode.ServicesEnabled) return;
            if (chartboost)
            {
                #if CHARTBOOST_ADS
            Debug.Log("show Chartboost Interstitial in " + LevelManager.THIS.gameStatus);

            Chartboost.showInterstitial(CBLocation.Default);
            Chartboost.cacheInterstitial(CBLocation.Default);
                #endif
            }
            else
            {
                #if GOOGLE_MOBILE_ADS
            Debug.Log("show Google mobile ads Interstitial in " + LevelManager.THIS.gameStatus);
            if (interstitial != null && interstitial.CanShowAd())
            {
                Debug.Log("Showing interstitial ad.");

                // show ads in mobile device ONLY
                #if !UNITY_EDITOR
                interstitial.Show();
                #endif
#if UNITY_ANDROID
                //interstitial = new InterstitialAd(admobUIDAndroid);
                LoadInterstitialAd(admobUIDAndroid);
#elif UNITY_IOS
               // interstitial = new InterstitialAd(admobUIDIOS);
                     LoadInterstitialAd(admobUIDIOS);
#else
			// interstitial = new InterstitialAd (admobUIDAndroid);
                    LoadInterstitialAd(admobUIDAndroid);
#endif

                // show ads in unity editor ONLY
#if UNITY_EDITOR
                interstitial.Show();
#endif
            }
            else
            {
                Debug.LogError("Interstitial ad is not ready yet.");
            }
                #endif
            }
        }

        public void ShowRate()
        {
            if (!RoyalThrone.RoyalOfflineMode.ServicesEnabled) return;
            if (MenuManager.Instance != null)
                MenuManager.Instance.ShowRate(true);
        }


        public void CheckRewardedAds()
        {
            RewardIcon reward = MenuManager.Instance.ShowReward().GetComponent<RewardIcon>();
            if (currentReward == RewardedAdsType.GetGems)
            {
                reward.SetIconSprite(0);

                reward.gameObject.SetActive(true);
                AddGems(rewardedGems);
                MenuManager.Instance.ShowGemsShop(false);
            }
            else if (currentReward == RewardedAdsType.GetLifes)
            {
                reward.SetIconSprite(1);
                reward.gameObject.SetActive(true);
                RestoreLifes();
                MenuManager.Instance.ShowLiveShop(false);
            }
            else if (currentReward == RewardedAdsType.GetGoOn)
            {
                MenuManager.Instance.ShowPreFailed(false);
            }
        }

        public void SetGems(int count)
        {
            Gems = count;
            PlayerPrefs.SetInt("Gems", Gems);
            PlayerPrefs.Save();
        }


        public void AddGems(int count)
        {
            Gems += count;
            PlayerPrefs.SetInt("Gems", Gems);
            PlayerPrefs.Save();
            #if PLAYFAB || GAMESPARKS
        NetworkManager.currencyManager.IncBalance(count);
            #endif
        }

        public void SpendGems(int count)
        {
            SoundBase.Instance.PlaySound(SoundBase.Instance.cash);
            Gems -= count;
            PlayerPrefs.SetInt("Gems", Gems);
            PlayerPrefs.Save();
            #if PLAYFAB || GAMESPARKS
        NetworkManager.currencyManager.DecBalance(count);
            #endif
        }


        /// <summary>Seconds one life takes to refill, as configured on this component.</summary>
        public float LifeIntervalSeconds =>
            Mathf.Max(1f, TotalTimeForRestLifeHours * 3600f + TotalTimeForRestLifeMin * 60f + TotalTimeForRestLifeSec);

        /// <summary>
        /// Grants every life whose interval has already elapsed and returns the seconds left
        /// until the next one. Safe to call every frame: it only writes when something changed.
        /// </summary>
        public float UpdateLives()
        {
            float interval = LifeIntervalSeconds;
            DateTime now = DateTime.UtcNow;

            if (lifes >= CapOfLife)
            {
                if (lifes > CapOfLife) lifes = CapOfLife;
                if (nextLifeUtc != DateTime.MinValue)
                {
                    nextLifeUtc = DateTime.MinValue;
                    SaveLifeSchedule();
                    PlayerPrefs.Save();
                }
                RestLifeTimer = 0;
                return 0;
            }

            bool scheduleChanged = false;
            // A missing deadline (or one further away than a full interval, which means the
            // device clock moved backwards) restarts the countdown from now.
            if (nextLifeUtc == DateTime.MinValue || (nextLifeUtc - now).TotalSeconds > interval)
            {
                nextLifeUtc = now.AddSeconds(interval);
                scheduleChanged = true;
            }

            int granted = 0;
            while (lifes < CapOfLife && now >= nextLifeUtc)
            {
                lifes++;
                granted++;
                nextLifeUtc = nextLifeUtc.AddSeconds(interval);
            }
            if (lifes >= CapOfLife) nextLifeUtc = DateTime.MinValue;

            RestLifeTimer = nextLifeUtc == DateTime.MinValue
                ? 0
                : Mathf.Max(0f, (float)(nextLifeUtc - now).TotalSeconds);

            if (granted > 0)
            {
                PlayerPrefs.SetInt("Lifes", lifes);
                SaveLifeSchedule();
                PlayerPrefs.Save();
            }
            else if (scheduleChanged)
            {
                SaveLifeSchedule();
                PlayerPrefs.Save();
            }
            return RestLifeTimer;
        }

        /// <summary>Test hook: makes the next life arrive in <paramref name="seconds"/>.</summary>
        public void SetRefillSeconds(float seconds)
        {
            if (lifes >= CapOfLife)
            {
                nextLifeUtc = DateTime.MinValue;
                RestLifeTimer = 0;
            }
            else
            {
                RestLifeTimer = Mathf.Max(0, seconds);
                nextLifeUtc = DateTime.UtcNow.AddSeconds(RestLifeTimer);
            }
            SaveLifeSchedule();
            PlayerPrefs.Save();
        }

        public void SaveLifeSchedule()
        {
            PlayerPrefs.SetString(NextLifeKey, nextLifeUtc == DateTime.MinValue
                ? string.Empty
                : nextLifeUtc.ToString("o", CultureInfo.InvariantCulture));
            PlayerPrefs.SetFloat("RestLifeTimer", RestLifeTimer);
        }

        /// <summary>Reads the stored deadline, migrating saves written before this schedule existed.</summary>
        void LoadLifeSchedule()
        {
            nextLifeUtc = DateTime.MinValue;
            string saved = PlayerPrefs.GetString(NextLifeKey, string.Empty);
            if (!string.IsNullOrEmpty(saved) &&
                DateTime.TryParse(saved, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed))
            {
                nextLifeUtc = parsed.ToUniversalTime();
            }
            else if (lifes < CapOfLife)
            {
                // Old saves kept "seconds left" plus a local-time exit stamp. Fold both into a deadline.
                float remaining = RestLifeTimer > 0 ? RestLifeTimer : LifeIntervalSeconds;
                double away = 0;
                if (!string.IsNullOrEmpty(DateOfExit) && DateTime.TryParse(DateOfExit, out DateTime exit))
                    away = Math.Max(0, (DateTime.Now - exit).TotalSeconds);
                nextLifeUtc = DateTime.UtcNow.AddSeconds(remaining - away);
            }
            UpdateLives();
        }

        public void RestoreLifes()
        {
            lifes = CapOfLife;
            nextLifeUtc = DateTime.MinValue;
            RestLifeTimer = 0;
            PlayerPrefs.SetInt("Lifes", lifes);
            SaveLifeSchedule();
            PlayerPrefs.Save();
        }

        public void AddLife(int count)
        {
            lifes += count;
            if (lifes > CapOfLife)
                lifes = CapOfLife;
            if (lifes >= CapOfLife)
            {
                nextLifeUtc = DateTime.MinValue;
                RestLifeTimer = 0;
            }
            PlayerPrefs.SetInt("Lifes", lifes);
            SaveLifeSchedule();
            PlayerPrefs.Save();
        }

        public int GetLife()
        {
            if (lifes > CapOfLife)
            {
                lifes = CapOfLife;
                PlayerPrefs.SetInt("Lifes", lifes);
                PlayerPrefs.Save();
            }

            return lifes;
        }

        public void PurchaseSucceded()
        {
            AddGems(waitedPurchaseGems);
            waitedPurchaseGems = 0;
        }

        public void SpendLife(int count)
        {
            if (lifes > 0)
            {
                bool wasFull = lifes >= CapOfLife;
                lifes -= count;
                PlayerPrefs.SetInt("Lifes", lifes);
                // Only the drop from a full set starts a countdown. Spending a life while one is
                // already pending must never push its deadline back, or the timer could be reset at will.
                if (wasFull)
                {
                    nextLifeUtc = DateTime.UtcNow.AddSeconds(LifeIntervalSeconds);
                    RestLifeTimer = LifeIntervalSeconds;
                }
                SaveLifeSchedule();
                PlayerPrefs.Save();
            }
            //else
            //{
            //    GameObject.Find("Canvas").transform.Find("RestoreLifes").gameObject.SetActive(true);
            //}
        }

        public void BuyBoost(BoostType boostType, int price, int count)
        {
            PlayerPrefs.SetInt("" + boostType, count);
            PlayerPrefs.Save();
            #if PLAYFAB || GAMESPARKS
        NetworkManager.dataManager.SetBoosterData();
            #endif

            //   ReloadBoosts();
        }

        public void SpendBoost(BoostType boostType)
        {
            PlayerPrefs.SetInt("" + boostType, PlayerPrefs.GetInt("" + boostType) - 1);
            PlayerPrefs.Save();
            #if PLAYFAB || GAMESPARKS
        NetworkManager.dataManager.SetBoosterData();
            #endif
        }
        //void ReloadBoosts()
        //{
        //    BoostExtraMoves = PlayerPrefs.GetInt("" + BoostType.ExtraMoves);
        //    BoostPackages = PlayerPrefs.GetInt("" + BoostType.Packages);
        //    BoostStripes = PlayerPrefs.GetInt("" + BoostType.Stripes);
        //    BoostExtraTime = PlayerPrefs.GetInt("" + BoostType.ExtraTime);
        //    BoostBomb = PlayerPrefs.GetInt("" + BoostType.Bomb);
        //    BoostColorful_bomb = PlayerPrefs.GetInt("" + BoostType.Colorful_bomb);
        //    BoostHand = PlayerPrefs.GetInt("" + BoostType.Hand);
        //    BoostRandom_color = PlayerPrefs.GetInt("" + BoostType.Random_color);

        //}
        //public void onMarketPurchase(PurchasableVirtualItem pvi, string payload, Dictionary<string, string> extra)
        //{
        //    PurchaseSucceded();
        //}

        //private void OnApplicationFocus(bool focus)//2.1.5 need to test music on
        //{
        //	var music = GameObject.Find("Music");
        //	if (music != null) music.GetComponent<AudioSource>().Play();
        //}

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                UpdateLives();
                PlayerPrefs.SetInt("Lifes", lifes);
                SaveLifeSchedule();
                PlayerPrefs.SetString("DateOfExit", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
                PlayerPrefs.Save();
            }
        }

        void OnApplicationQuit()
        {
            //1.4  added
            UpdateLives();
            PlayerPrefs.SetInt("Lifes", lifes);
            SaveLifeSchedule();
            PlayerPrefs.SetString("DateOfExit", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
            //print(RestLifeTimer)
        }

        public void OnLevelClicked(object sender, LevelReachedEventArgs args)
        {
            if (EventSystem.current.IsPointerOverGameObject(-1))
                return;
            TryOpenLevel(args.Number);
        }

        // Shared entry for world-map selection and the Royal Throne homepage UI.
        public bool TryOpenLevel(int number)
        {
            if (number < 1 || LevelsMap.IsLevelLocked(number) || LevelManager.THIS == null ||
                LevelManager.THIS.gameStatus != GameState.Map || MenuManager.Instance == null)
                return false;
            if (!MenuManager.Instance.menuPlay.activeSelf &&
                !MenuManager.Instance.gemsShop.activeSelf &&
                !MenuManager.Instance.liveShop.activeSelf &&
                !MenuManager.Instance.settings.activeSelf)
            {
                PlayerPrefs.SetInt("OpenLevel", number);
                PlayerPrefs.Save();
                LevelManager.THIS.MenuPlayEvent();
                LevelManager.THIS.LoadLevel();
                openLevel = number;
                MenuManager.Instance.ShowMenuPlay(true);
                return true;
            }
            return false;
        }

        void OnEnable()
        {
            LevelsMap.LevelSelected += OnLevelClicked;
        }

        void OnDisable()
        {
            LevelsMap.LevelSelected -= OnLevelClicked;

            PlayerPrefs.SetInt("Lifes", lifes);
            SaveLifeSchedule();
            PlayerPrefs.SetString("DateOfExit", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            PlayerPrefs.Save();

/*#if GOOGLE_MOBILE_ADS
        interstitial.OnAdLoaded -= HandleInterstitialLoaded;
        interstitial.OnAdFailedToLoad -= HandleInterstitialFailedToLoad;
#endif*/
        }

        #region FaceBook

        #if FACEBOOK
    //	public void CallFBInit () {
    //		FB.Init (OnInitComplete, OnHideUnity);
    //
    //	}
    //
    //	private void OnInitComplete () {
    //		Debug.Log ("FB.Init completed: Is user logged in? " + FB.IsLoggedIn);
    //
    //	}
    //
    //	private void OnHideUnity (bool isGameShown) {
    //		Debug.Log ("Is game showing? " + isGameShown);
    //	}
    //
    //	void OnGUI () {
    //		if (LoginEnable) {
    //			InitScript.Instance.CallFBLogin ();
    //			LoginEnable = false;
    //		}
    //	}
    //
    //
    //	public void CallFBLogin () {
    //		FB.LogInWithReadPermissions (new List<string> () { "public_profile", "email", "user_friends" }, this.HandleResult);
    //	}
    //
    //	public void CallFBLoginForPublish () {
    //		// It is generally good behavior to split asking for read and publish
    //		// permissions rather than ask for them all at once.
    //		//
    //		// In your own game, consider postponing this call until the moment
    //		// you actually need it.
    //		FB.LogInWithPublishPermissions (new List<string> () { "publish_actions" }, this.HandleResult);
    //	}
    //
    //	void LoginCallback (IPayResult result) {
    //
    //		if (result.Error != null)
    //			lastResponse = "Error Response:\n" + result.Error;
    //		else if (!FB.IsLoggedIn) {
    //			lastResponse = "Login cancelled by Player";
    //		} else {
    //			lastResponse = "Login was successful!";
    //			if (loginForSharing) {
    //				loginForSharing = false;
    //				Share ();
    //			}
    //		}
    //		Debug.Log (lastResponse);
    //	}
    //
    //	private void CallFBLogout () {
    //		FB.LogOut ();
    //	}
    //
    //	public void Share () {
    //		if (!FB.IsLoggedIn) {
    //			loginForSharing = true;
    //			LoginEnable = true;
    //			Debug.Log ("not logged, logging");
    //		} else {
    //			FB.FeedShare (
    //				link: new Uri ("http://apps.facebook.com/" + FB.AppId + "/?challenge_brag=" + (FB.IsLoggedIn ? AccessToken.CurrentAccessToken.UserId : "guest")),
    //				linkName: FacebookSettings.AppLabels [0],
    //				linkCaption: "I just scored " + LevelManager.Score + " points! Try to beat me!"
    //            //picture: "https://fbexternal-a.akamaihd.net/safe_image.php?d=AQCzlvjob906zmGv&w=128&h=128&url=https%3A%2F%2Ffbcdn-photos-h-a.akamaihd.net%2Fhphotos-ak-xtp1%2Ft39.2081-0%2F11891368_513258735497916_1832270581_n.png&cfs=1"
    //			);
    //		}
    //	}
    //
    //	protected void HandleResult (IResult result) {
    //		if (result == null) {
    //			this.LastResponse = "Null Response\n";
    //			Debug.Log (this.LastResponse);
    //			return;
    //		}
    //
    //		//     this.LastResponseTexture = null;
    //
    //		// Some platforms return the empty string instead of null.
    //		if (!string.IsNullOrEmpty (result.Error)) {
    //			this.Status = "Error - Check log for details";
    //			this.LastResponse = "Error Response:\n" + result.Error;
    //			Debug.Log (result.Error);
    //		} else if (result.Cancelled) {
    //			this.Status = "Cancelled - Check log for details";
    //			this.LastResponse = "Cancelled Response:\n" + result.RawResult;
    //			Debug.Log (result.RawResult);
    //		} else if (!string.IsNullOrEmpty (result.RawResult)) {
    //			this.Status = "Success - Check log for details";
    //			this.LastResponse = "Success Response:\n" + result.RawResult;
    //			if (loginForSharing) {
    //				loginForSharing = false;
    //				Share ();
    //			}
    //
    //			Debug.Log (result.RawResult);
    //		} else {
    //			this.LastResponse = "Empty Response\n";
    //			Debug.Log (this.LastResponse);
    //		}
    //	}
        #endif

        #endregion
    }
}
