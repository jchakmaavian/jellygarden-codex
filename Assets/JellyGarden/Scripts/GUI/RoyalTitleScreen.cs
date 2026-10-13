using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using System.Collections;
using System.Collections.Generic;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    /// <summary>
    /// The Royal Throne startup flow in the first scene (main): Splash, then Loading, then Home. No input is needed.
    /// - Splash: Splash.json (RoyalLottiePlayer) plays once over the reference's violet gradient, centred at the
    ///   reference's proportions.
    /// - Loading: the phone or tablet illustration (whichever matches the screen's aspect ratio, covering the screen without
    ///   stretching), the approved Royal Throne logo with its rotating light rays at the top, and "Loading..." at the bottom.
    ///   It starts the game scene's asynchronous load only once it is on screen, and stays until Home is genuinely ready:
    ///   the scene is activated, the level manager is on the map, the home screen is visible and has drawn a frame beneath
    ///   this opaque overlay. Then the overlay fades out over Home and is destroyed.
    /// The class keeps its name and script GUID (the scene and the editor tools refer to it); <see cref="BeginPlay"/> remains
    /// only as an input-free "start loading now" hook for those tools.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class RoyalTitleScreen : MonoBehaviour
    {
        public enum StartupPhase { Splash, Loading, OpeningHome, Done }

        [Header("Splash")]
        public CanvasGroup splashGroup;
        public AspectRatioFitter splashBackground;
        public RoyalLottiePlayer splashAnimation;
        [Tooltip("Composition width as a share of the screen width, from Splash-reference.png (0.943 on its 852 x 1847 frame).")]
        public float splashWidthShare = .943f;
        [Tooltip("Composition centre as a share of the screen height from the top, from the reference.")]
        public float splashCentreFromTop = .46f;
        [Header("Loading")]
        public CanvasGroup loadingGroup;
        public Image loadingBackground;
        public AspectRatioFitter loadingFitter;
        public Sprite loadingMobile;
        public Sprite loadingTablet;
        public RectTransform loadingSafeArea;
        public RectTransform loadingLogo;
        public RectTransform loadingHalo;
        public Text loadingLabel;
        public CanvasGroup wholeScreen;
        public string gameScene = "game";

        public StartupPhase Phase { get; private set; }
        public bool IsLoading => Application.isPlaying && Phase != StartupPhase.Splash;
        /// <summary>The loading illustration shown ("mobile" or "tablet"), for checks.</summary>
        public string LoadingArt { get; private set; }
        public float SplashSeconds { get; private set; }
        public float LoadingSeconds { get; private set; }
        public int LoadRequestCount { get; private set; }

        // "Loading..." in its four dot states. Every state lays out all three dots (the hidden ones transparent), so the text's
        // footprint never changes: only the dots' visibility animates, and the word never moves.
        static readonly string[] LoadingStates = { "Loading<color=#00000000>...</color>", "Loading.<color=#00000000>..</color>", "Loading..<color=#00000000>.</color>", "Loading..." };
        const string LoadingFull = "Loading...";

        Rect lastSafeArea; Vector2 lastSize;
        bool skipSplash; float phaseStarted; AsyncOperation loadOperation; int shownDots = -1;
        bool MotionEnabled => PlayerPrefs.GetInt("RoyalThrone.HomeMotion", 1) != 0;

        // Unity runs a newly activated scene's Awake, Start and first LateUpdate all in one frame, so every one-off build in
        // the game scene would otherwise freeze this screen together. While it covers the start, those builds are queued here
        // (QueueStartupWork) and run after that first frame, one step per frame, in the order queued, so the loading screen
        // keeps drawing between them. Home is revealed only once the queue is empty.
        static readonly Queue<KeyValuePair<Object, IEnumerator>> startupWork = new Queue<KeyValuePair<Object, IEnumerator>>();
        static RoyalTitleScreen covering;
        static bool quitting;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { startupWork.Clear(); covering = null; quitting = false; Application.quitting -= OnQuitting; Application.quitting += OnQuitting; }
        static void OnQuitting() { quitting = true; startupWork.Clear(); }

        /// <summary>
        /// While the loading screen covers the game scene's start, queues a one-off build to run under it: each part of
        /// <paramref name="steps"/> up to a yield runs in a frame of its own, after the scene's first frame, in the order queued.
        /// Returns false when no loading screen is covering the start (for example the game scene played directly): the caller
        /// then runs the work itself, at once, as before.
        /// </summary>
        public static bool QueueStartupWork(Object owner, IEnumerator steps)
        {
            if (!Application.isPlaying || quitting || !covering || covering.Phase != StartupPhase.Loading || steps == null) return false;
            startupWork.Enqueue(new KeyValuePair<Object, IEnumerator>(owner, steps));
            return true;
        }
        public static bool QueueStartupWork(Object owner, System.Action work) => work != null && QueueStartupWork(owner, Once(work));
        static IEnumerator Once(System.Action work) { work(); yield break; }

        // Runs the next queued step; false when nothing is left. A destroyed owner's work is dropped. A build that throws is
        // logged and dropped, as the exception would have ended the component's own Awake or LateUpdate, and the start goes on.
        static bool RunStartupStep()
        {
            while (startupWork.Count > 0)
            {
                var work = startupWork.Peek();
                if (!work.Key) { startupWork.Dequeue(); continue; }
                bool more;
                try { more = work.Value.MoveNext(); }
                catch (System.Exception e) { Debug.LogException(e, work.Key); more = false; }
                if (!more) startupWork.Dequeue();
                return true;
            }
            return false;
        }

        void OnDestroy()
        {
            if (covering != this) return;
            covering = null;
            // Normally empty by now. If this screen goes early, nothing queued is left unbuilt.
            if (Application.isPlaying && !quitting) while (RunStartupStep()) { }
        }

        void OnEnable()
        {
            RefreshLayout();
            if (!Application.isPlaying) return;
            covering = this;
            // The persistent audio director owns the castle theme in every scene, so the music starts with the game, as it
            // did with the old title screen, and carries on into Home.
            _ = RoyalAudioDirector.Get;
            Phase = StartupPhase.Splash;
            wholeScreen.alpha = 1; wholeScreen.blocksRaycasts = true;
            splashGroup.gameObject.SetActive(true); splashGroup.alpha = 1;
            loadingGroup.gameObject.SetActive(false); loadingGroup.alpha = 0;
            StartCoroutine(Run());
        }

        void Update()
        {
            var size = ((RectTransform)transform).rect.size;
            if (lastSafeArea != Screen.safeArea || lastSize != size) RefreshLayout();
            if (!Application.isPlaying || Phase == StartupPhase.Splash || Phase == StartupPhase.Done || !loadingLabel) return;
            // The dots fill in one by one, 0.4 s each; only a change of state touches the text.
            int dots = MotionEnabled ? (int)((Time.unscaledTime - phaseStarted) / .4f) % 4 : 3;
            if (dots != shownDots) { shownDots = dots; loadingLabel.text = LoadingStates[dots]; }
        }

        public void RefreshLayout()
        {
            var root = (RectTransform)transform;
            lastSize = root.rect.size; lastSafeArea = Screen.safeArea;
            if (root.rect.width <= 0 || root.rect.height <= 0) return;
            float w = root.rect.width, h = root.rect.height;
            // Splash: the reference's width share on phones; on wider screens as on a 9:16 phone column, so a tablet shows it
            // at a phone's proportion of the height rather than filling its width.
            if (splashAnimation)
            {
                var comp = splashAnimation.Loaded ? splashAnimation.CompositionSize : new Vector2(1200, 680);
                var rt = (RectTransform)splashAnimation.transform;
                float compWidth = splashWidthShare * Mathf.Min(w, h * 9f / 16f);
                rt.anchorMin = rt.anchorMax = new Vector2(.5f, 1 - splashCentreFromTop); rt.pivot = new Vector2(.5f, .5f);
                rt.sizeDelta = new Vector2(compWidth, compWidth * comp.y / comp.x); rt.anchoredPosition = Vector2.zero;
                if (Application.isPlaying && splashAnimation.Loaded)
                {
                    var canvas = GetComponentInParent<Canvas>();
                    float scale = canvas ? canvas.rootCanvas.scaleFactor : 1;
                    splashAnimation.Prepare(compWidth * scale / comp.x);
                }
            }
            if (splashBackground && splashBackground.TryGetComponent<Image>(out var splashImage) && splashImage.sprite)
                splashBackground.aspectRatio = splashImage.sprite.rect.width / splashImage.sprite.rect.height;
            SelectLoadingArt(w / h);
            if (!loadingSafeArea || !loadingLogo || !loadingLabel) return;
            Rect safe = lastSafeArea; Vector2 min = Vector2.zero, max = Vector2.one;
            if (Application.isPlaying && Screen.width > 0 && Screen.height > 0 && safe.width > 0)
            { min = safe.min / new Vector2(Screen.width, Screen.height); max = safe.max / new Vector2(Screen.width, Screen.height); }
            loadingSafeArea.anchorMin = min; loadingSafeArea.anchorMax = max; loadingSafeArea.offsetMin = loadingSafeArea.offsetMax = Vector2.zero;
            float sw = w * (max.x - min.x), sh = h * (max.y - min.y);
            // Top centre, clear of the notch: the approved logo and its light rays.
            float logo = Mathf.Max(1, Mathf.Min(sw * .72f, sh * .30f));
            loadingLogo.anchorMin = loadingLogo.anchorMax = new Vector2(.5f, 1); loadingLogo.pivot = new Vector2(.5f, 1);
            loadingLogo.sizeDelta = Vector2.one * logo; loadingLogo.anchoredPosition = new Vector2(0, -sh * .015f);
            if (loadingHalo)
            {
                loadingHalo.anchorMin = loadingHalo.anchorMax = new Vector2(.5f, 1); loadingHalo.pivot = new Vector2(.5f, .5f);
                loadingHalo.sizeDelta = new Vector2(logo * 1.9f, logo * 1.9f * 1015 / 1123);
                loadingHalo.anchoredPosition = loadingLogo.anchoredPosition + Vector2.down * (logo * .5f);
            }
            // Bottom centre, inside the safe area with room to spare.
            var label = loadingLabel.rectTransform;
            label.anchorMin = label.anchorMax = new Vector2(.5f, 0); label.pivot = new Vector2(.5f, 0);
            label.sizeDelta = new Vector2(Mathf.Min(sw * .8f, 560), 70);
            label.anchoredPosition = new Vector2(-FullTextInkCentre(), Mathf.Max(48, sh * .055f));
        }

        // Where the glyphs of the full "Loading..." are centred, relative to the label's centre, in its local units. Text centres
        // its advance box, and the final dot's advance leaves empty room on the right, so the ink would sit slightly left.
        float FullTextInkCentre()
        {
            if (!loadingLabel.font) return 0;
            var generator = new TextGenerator();
            if (!generator.Populate(LoadingFull, loadingLabel.GetGenerationSettings(loadingLabel.rectTransform.rect.size)) || generator.vertexCount == 0) return 0;
            float min = float.MaxValue, max = float.MinValue;
            foreach (var v in generator.verts) { min = Mathf.Min(min, v.position.x); max = Mathf.Max(max, v.position.x); }
            return (min + max) * .5f / loadingLabel.pixelsPerUnit;
        }

        // Phone art (971 x 1620) or tablet art (1536 x 2048): whichever aspect is closer to the screen's, so the cover crop is
        // smallest. The switch point is their geometric mean (about 0.67 width/height): every phone is below it, 3:4 and
        // 2:3 tablets are above it, and 16:10 tablets keep the phone art, which suits their taller shape better.
        void SelectLoadingArt(float aspect)
        {
            if (!loadingBackground || !loadingFitter || !loadingMobile || !loadingTablet) return;
            float mobile = loadingMobile.rect.width / loadingMobile.rect.height, tablet = loadingTablet.rect.width / loadingTablet.rect.height;
            bool useTablet = aspect >= Mathf.Sqrt(mobile * tablet);
            var sprite = useTablet ? loadingTablet : loadingMobile;
            if (loadingBackground.sprite != sprite) loadingBackground.sprite = sprite;
            LoadingArt = useTablet ? "tablet" : "mobile";
            loadingFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            loadingFitter.aspectRatio = sprite.rect.width / sprite.rect.height;
        }

        IEnumerator Run()
        {
            float started = Time.unscaledTime;
            if (splashAnimation && (splashAnimation.Loaded || splashAnimation.Load()))
            {
                RefreshLayout();
                splashAnimation.Play();
                // one presented frame of the gradient and frame 0 before the clock matters
                yield return null;
                while (!splashAnimation.Finished && !skipSplash) yield return null;
            }
            else Debug.LogError("Royal Throne: the splash animation is missing; continuing to the loading screen.", this);
            SplashSeconds = Time.unscaledTime - started;
            yield return ShowLoading();
            yield return LoadHome();
        }

        IEnumerator ShowLoading()
        {
            Phase = StartupPhase.Loading; phaseStarted = Time.unscaledTime;
            if (loadingLabel) { shownDots = MotionEnabled ? 0 : 3; loadingLabel.text = LoadingStates[shownDots]; }
            RefreshLayout();
            loadingGroup.gameObject.SetActive(true); loadingGroup.alpha = 0;
            // The loading screen cross-fades in over the finished splash; the splash goes only once it is covered.
            float elapsed = 0;
            while (elapsed < .35f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / .35f));
                loadingGroup.alpha = t;
                if (loadingLogo && !loadingLogo.GetComponent<RoyalLogoMotion>()) loadingLogo.localScale = Vector3.one * (MotionEnabled ? Mathf.Lerp(.94f, 1, 1 - Mathf.Pow(1 - t, 3)) : 1);
                yield return null;
            }
            loadingGroup.alpha = 1; if (loadingLogo && !loadingLogo.GetComponent<RoyalLogoMotion>()) loadingLogo.localScale = Vector3.one;
            splashGroup.gameObject.SetActive(false);
            // The loading screen is presented before any scene work starts.
            yield return new WaitForEndOfFrame();
        }

        IEnumerator LoadHome()
        {
            float started = Time.unscaledTime;
            // This overlay has to outlive the first scene: it covers the switch and Home's first frames.
            DontDestroyOnLoad(gameObject);
            while (true)
            {
                if (!Application.CanStreamedLevelBeLoaded(gameScene)) { Debug.LogError("Royal Throne: the " + gameScene + " scene is not in the build."); yield break; }
                loadOperation = SceneManager.LoadSceneAsync(gameScene, LoadSceneMode.Single);
                if (loadOperation != null) { LoadRequestCount++; break; }
                Debug.LogError("Royal Throne: the game scene load did not start; retrying.");
                float wait = Time.unscaledTime + 1; while (Time.unscaledTime < wait) yield return null;
            }
            while (!loadOperation.isDone) yield return null;
            // The scene's first frame (its Awake, Start and first LateUpdate) finishes and draws first; then the builds its
            // components queued run, one step per frame, with this screen drawn between them.
            yield return null;
            // Genuinely ready: the game scene's systems ran their Start, every queued build has run, the level manager is on
            // the map and the home screen has initialised and reports itself visible.
            float warned = Time.unscaledTime + 20;
            RoyalHomeScreen home = null;
            while (true)
            {
                if (RunStartupStep()) { yield return null; continue; }
                if (!home) home = FindAnyObjectByType<RoyalHomeScreen>();
                var lm = LevelManager.THIS;
                if (home && home.IsHomeVisible && lm && lm.gameStatus == GameState.Map && InitScript.Instance) break;
                if (Time.unscaledTime > warned) { warned = float.MaxValue; Debug.LogWarning("Royal Throne: Home is taking longer than 20 s to become ready; the loading screen stays up."); }
                yield return null;
            }
            // Let Home lay out and draw beneath the opaque loading screen before revealing it.
            yield return null; yield return null; yield return new WaitForEndOfFrame();
            LoadingSeconds = Time.unscaledTime - started;
            Phase = StartupPhase.OpeningHome;
            float elapsed = 0;
            while (elapsed < .35f)
            {
                elapsed += Time.unscaledDeltaTime;
                wholeScreen.alpha = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / .35f));
                yield return null;
            }
            Phase = StartupPhase.Done;
            Destroy(gameObject);
        }

        /// <summary>
        /// Editor and QA tools: skip the rest of the splash and start loading now. Players never need it; the flow is
        /// automatic.
        /// </summary>
        public void BeginPlay() { if (Application.isPlaying) skipSplash = true; }
    }
}
