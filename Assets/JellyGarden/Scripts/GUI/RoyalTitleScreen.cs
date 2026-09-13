using System;
using System.Collections;
using JellyGarden.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class RoyalTitleScreen : MonoBehaviour
    {
        [Header("Screen layout")]
        public RectTransform safeArea;
        public RectTransform loadingSafeArea;
        public RectTransform titleLogo;
        public RectTransform loadingLogo;
        public RectTransform titleHalo;
        public RectTransform loadingHalo;
        public RectTransform playRect;
        public RectTransform loadingCard;
        public CanvasGroup wholeScreen;
        public CanvasGroup titleGroup;
        public CanvasGroup loadingGroup;
        public Button playButton;
        public Text errorLabel;
        [Header("Loading presentation")]
        public RectTransform progressFill;
        public RectTransform progressShine;
        public RectTransform[] jewels;
        public Text loadingLabel;
        public Text loadingStatus;
        public string gameScene = "game";
        [Tooltip("Brief presentation time; scene progress still comes from Unity's asynchronous operation.")]
        [Range(.4f, 2f)] public float minimumLoadingSeconds = .9f;
        [Header("Ambient light")]
        public RectTransform[] motes;

        public bool IsLoading { get; private set; }
        public float DisplayedProgress { get; private set; }
        public int LoadRequestCount { get; private set; }
        Rect lastSafeArea;
        Vector2 lastSize;
        AsyncOperation loadOperation;
        float startedAt;
        bool preview;

        void OnEnable()
        {
            RefreshLayout();
            if (!Application.isPlaying) return;
            wholeScreen.alpha = 1;
            titleGroup.alpha = 1;
            titleGroup.interactable = titleGroup.blocksRaycasts = true;
            loadingGroup.alpha = 0;
            loadingGroup.gameObject.SetActive(false);
            playButton.interactable = true;
            errorLabel.text = "";
            StartCoroutine(Entrance());
        }

        void Update()
        {
            var size = ((RectTransform)transform).rect.size;
            if (lastSafeArea != Screen.safeArea || lastSize != size) RefreshLayout();
            if (!Application.isPlaying) return;
            float time = Time.unscaledTime;
            for (int i = 0; i < motes.Length; i++)
            {
                var mote = motes[i];
                if (!mote) continue;
                float seed = i * 2.39996f;
                mote.anchorMin = mote.anchorMax = new Vector2(.08f + Mathf.Repeat(i * .173f, .84f),
                    .12f + Mathf.Repeat(time * (.012f + i * .0007f) + i * .107f, .8f));
                mote.anchoredPosition = new Vector2(Mathf.Sin(time * .6f + seed) * 18, 0);
                float scale = .6f + .4f * Mathf.Sin(time * 1.3f + seed);
                mote.localScale = Vector3.one * scale;
            }
            if (!IsLoading) return;
            for (int i = 0; i < jewels.Length; i++)
            {
                float pulse = .5f + .5f * Mathf.Sin(time * 5 - i * .8f);
                jewels[i].localScale = Vector3.one * Mathf.Lerp(.75f, 1.12f, pulse);
                jewels[i].anchoredPosition = new Vector2((i - 1) * 42, -49 + pulse * 7);
            }
            progressShine.anchorMin = new Vector2(Mathf.Repeat(time * .65f, 1), 0);
            progressShine.anchorMax = new Vector2(progressShine.anchorMin.x, 1);
        }

        public void RefreshLayout()
        {
            if (!safeArea || !loadingSafeArea || !titleLogo || !playRect || !loadingCard) return;
            var root = (RectTransform)transform;
            lastSize = root.rect.size;
            lastSafeArea = Screen.safeArea;
            Rect safe = lastSafeArea;
            Vector2 min = Vector2.zero, max = Vector2.one;
            if (Application.isPlaying && Screen.width > 0 && Screen.height > 0 && safe.width > 0)
            {
                min = safe.min / new Vector2(Screen.width, Screen.height);
                max = safe.max / new Vector2(Screen.width, Screen.height);
            }
            safeArea.anchorMin = loadingSafeArea.anchorMin = min;
            safeArea.anchorMax = loadingSafeArea.anchorMax = max;
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            loadingSafeArea.offsetMin = loadingSafeArea.offsetMax = Vector2.zero;
            float width = root.rect.width * (max.x - min.x);
            float height = root.rect.height * (max.y - min.y);
            float logoSize = Mathf.Max(1, Mathf.Min(width * .8f, height * .45f));
            titleLogo.sizeDelta = Vector2.one * logoSize;
            titleLogo.anchoredPosition = new Vector2(0, -height * .01f);
            loadingLogo.sizeDelta = titleLogo.sizeDelta;
            loadingLogo.anchoredPosition = titleLogo.anchoredPosition;
            LayoutHalo(titleHalo, width, logoSize);
            LayoutHalo(loadingHalo, width, logoSize);
            float buttonWidth = Mathf.Max(1, Mathf.Min(width * .84f, height * .48f));
            playRect.sizeDelta = new Vector2(buttonWidth, buttonWidth / 3);
            playRect.anchoredPosition = new Vector2(0, height * .07f);
            float cardWidth = Mathf.Max(1, Mathf.Min(660, width * .84f));
            loadingCard.sizeDelta = new Vector2(cardWidth, Mathf.Max(270, cardWidth * .5f));
            loadingCard.anchoredPosition = new Vector2(0, height * .08f);
        }

        void LayoutHalo(RectTransform halo, float width, float logoSize)
        {
            if (!halo) return;
            halo.sizeDelta = new Vector2(width * 1.56f, width * 1.56f * 690 / 763);
            halo.anchoredPosition = titleLogo.anchoredPosition + Vector2.down * (logoSize * .5f);
        }

        IEnumerator Entrance()
        {
            float elapsed = 0;
            while (elapsed < .55f && !IsLoading)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / .55f);
                titleGroup.alpha = Mathf.SmoothStep(0, 1, t);
                titleLogo.localScale = Vector3.one * Mathf.Lerp(.94f, 1, 1 - Mathf.Pow(1 - t, 3));
                yield return null;
            }
            titleLogo.localScale = Vector3.one;
        }

        public void BeginPlay()
        {
            // Latch before yielding: repeated pointer/keyboard input cannot enqueue another scene load.
            if (IsLoading) return;
            if (!Application.CanStreamedLevelBeLoaded(gameScene))
            {
                errorLabel.text = "Couldn't open the game. Please try again.";
                Debug.LogError("Royal Throne: game scene is not available in the scene list.");
                return;
            }
            IsLoading = true;
            playButton.interactable = false;
            titleGroup.interactable = titleGroup.blocksRaycasts = false;
            errorLabel.text = "";
            if (SoundBase.Instance != null) SoundBase.Instance.PlaySound(SoundBase.Instance.click);
            StartCoroutine(LoadGame());
        }

        IEnumerator ShowLoading()
        {
            startedAt = Time.unscaledTime;
            DisplayedProgress = 0;
            SetProgress(0);
            loadingGroup.gameObject.SetActive(true);
            loadingGroup.blocksRaycasts = true;
            loadingLabel.text = "Loading your kingdom";
            loadingStatus.text = preview ? "Animation preview" : "Preparing your game...";
            float elapsed = 0;
            while (elapsed < .25f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / .25f));
                titleGroup.alpha = 1 - t;
                loadingGroup.alpha = t;
                yield return null;
            }
            titleGroup.alpha = 0;
            loadingGroup.alpha = 1;
            // Guarantee one presented frame before starting disk/scene work.
            yield return null;
        }

        IEnumerator LoadGame()
        {
            yield return ShowLoading();
            Exception failure = null;
            try
            {
                loadOperation = SceneManager.LoadSceneAsync(gameScene, LoadSceneMode.Single);
                if (loadOperation == null) throw new InvalidOperationException("No scene operation was created.");
                loadOperation.allowSceneActivation = false;
                LoadRequestCount++;
            }
            catch (Exception exception) { failure = exception; }
            if (failure != null)
            {
                Debug.LogException(failure);
                RestoreTitle("Couldn't open the game. Please try again.");
                yield break;
            }
            // Keep the overlay alive through activation so the map never flashes beneath an unfinished bar.
            DontDestroyOnLoad(gameObject);
            while (loadOperation.progress < .9f || Time.unscaledTime - startedAt < minimumLoadingSeconds || DisplayedProgress < .999f)
            {
                float target = Mathf.Clamp01(loadOperation.progress / .9f);
                SetProgress(Mathf.MoveTowards(DisplayedProgress, target, Time.unscaledDeltaTime * 1.8f));
                yield return null;
            }
            SetProgress(1);
            loadingStatus.text = "Ready!";
            yield return null;
            loadOperation.allowSceneActivation = true;
            while (!loadOperation.isDone) yield return null;
            yield return null;
            float elapsed = 0;
            while (elapsed < .3f)
            {
                elapsed += Time.unscaledDeltaTime;
                wholeScreen.alpha = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / .3f));
                yield return null;
            }
            Destroy(gameObject);
        }

        void SetProgress(float progress)
        {
            DisplayedProgress = Mathf.Clamp01(progress);
            progressFill.anchorMax = new Vector2(DisplayedProgress, 1);
        }

        void RestoreTitle(string message)
        {
            IsLoading = false;
            preview = false;
            titleGroup.alpha = 1;
            wholeScreen.alpha = 1;
            titleGroup.interactable = titleGroup.blocksRaycasts = true;
            loadingGroup.gameObject.SetActive(false);
            playButton.interactable = true;
            errorLabel.text = message;
        }

#if UNITY_EDITOR
        public void PreviewLoadingAnimation()
        {
            if (!Application.isPlaying || IsLoading) return;
            IsLoading = preview = true;
            playButton.interactable = false;
            titleGroup.interactable = titleGroup.blocksRaycasts = false;
            StartCoroutine(Preview());
        }
        IEnumerator Preview()
        {
            yield return ShowLoading();
            float elapsed = 0;
            while (elapsed < 15)
            {
                elapsed += Time.unscaledDeltaTime;
                SetProgress(Mathf.Clamp01(elapsed / 15));
                yield return null;
            }
            RestoreTitle("");
        }
#endif
    }
}
