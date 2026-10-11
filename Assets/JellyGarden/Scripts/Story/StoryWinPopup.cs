// A royal victory card. StoryFlow pays the reward before Open; presentation
// and repeated taps must never award another star.
using System;
using System.Collections;
using JellyGarden.Scripts.GUI;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryWinPopup : MonoBehaviour
    {
        public static bool Enabled = true;
        public const string HostName = "StoryWinPopup";
        private static StoryWinPopup _instance;
        private static readonly Color Navy = new Color32(19, 43, 91, 255);
        private static readonly Color Gold = new Color32(235, 184, 66, 255);
        private static Color ScrimColor => StorybookUi.Enabled ? new Color(.06f,.04f,.025f,.48f) : new Color(0.025f,0.065f,0.16f,0.68f);
        private static readonly Vector2 DesignSize = new Vector2(1000f, 1400f);

        private Canvas _canvas;
        private RectTransform _safeArea, _layout, _card;
        private CanvasGroup _cardGroup, _footer;
        private Image _scrim;
        private RawImage _backdrop;
        private Text _title, _caption;
        private Button _continue;
        private StoryWinStarVfx _starVfx;
        // The original Royal Throne stars, in place of _starVfx while they are on.
        private StoryOriginalWinStars _originalStars;
        private Action _onContinue;
        private Material _blurMaterial;
        private RenderTexture _blurred;
        private float _openedAt;
        private bool _presenting, _continuing;
        private Rect _lastSafeArea;
        private Vector2 _lastCanvasSize;

        public static bool IsOpen => _instance != null && _instance.gameObject.activeSelf;
        // The level label retains its existing public contract.
        public static string TitleText => _instance == null ? null : _instance._title.text;
        public static string CaptionText => _instance == null ? null : _instance._caption.text;

        public static void Open(int levelNumber, int areaId, bool paidStar, Action onContinue)
        {
            if (!Enabled) return;
            HideKitPanel();
            if (_instance == null)
            {
                var go = new GameObject(HostName);
                StoryObjects.Persist(go);
                _instance = go.AddComponent<StoryWinPopup>();
                _instance.Build();
            }
            // Reopening during the entrance cancels the previous capture too.
            _instance.StopAllCoroutines();
            _instance.ReleaseBackdrop();
            _instance.gameObject.SetActive(true);
            _instance._onContinue = onContinue;
            _instance._continuing = false;
            _instance._title.text = "Level " + levelNumber;
            _instance._caption.text = paidStar ? (StorybookUi.Enabled ? "+1 Star" : "+1 Star earned") : "Star already earned";
            _instance.FitToScreen();
            _instance._continue.interactable = true;
            if (Application.isPlaying) _instance.StartCoroutine(_instance.Present());
            else _instance.ShowSettled();
            Debug.Log(string.Format("[Story] win panel: level {0}, area {1}, {2}.",
                levelNumber, areaId, paidStar ? "one star paid" : "replay, star already earned"));
        }

        public static void Close()
        {
            if (_instance == null) return;
            _instance._onContinue = null;
            _instance.gameObject.SetActive(false);
        }

        private static void HideKitPanel()
        {
            foreach (AnimationManager menu in
                     UnityEngine.Object.FindObjectsByType<AnimationManager>(FindObjectsInactive.Include))
                if (menu != null && menu.gameObject.name == "MenuComplete")
                    menu.gameObject.SetActive(false);
        }

        private void Build()
        {
            _canvas = StoryUi.CreateOverlayCanvas("WinPopupCanvas", 620);
            _canvas.transform.SetParent(transform, false);
            _backdrop = StoryUi.CreateRect("BlurredBoard", _canvas.transform).gameObject.AddComponent<RawImage>();
            StoryUi.Stretch(_backdrop.rectTransform);
            _backdrop.raycastTarget = false;
            _backdrop.enabled = false;
            _scrim = StoryUi.CreateImage("Scrim", _canvas.transform, null, ScrimColor);
            StoryUi.Stretch(_scrim.rectTransform);
            _scrim.raycastTarget = true;
            _safeArea = StoryUi.CreateRect("SafeArea", _canvas.transform);
            _layout = StoryUi.CreateRect("Layout", _safeArea);
            Place(_layout, Vector2.zero, DesignSize);
            _card = StoryUi.CreateRect("Card", _layout);
            StoryUi.Stretch(_card);
            _cardGroup = _card.gameObject.AddComponent<CanvasGroup>();

            if (StorybookUi.Enabled) { BuildStorybook(); return; }
            Image plate = Art("Plate", _card, "win_panel", StoryUi.RoundedSprite(48));
            plate.type = Image.Type.Sliced;
            Place(plate.rectTransform, new Vector2(0f, -70f), new Vector2(920f, 1160f));
            RectTransform effects = StoryUi.CreateRect("StarEffects", _card);
            Place(effects, new Vector2(0f, 36f), new Vector2(660f, 660f));
            _starVfx = effects.gameObject.AddComponent<StoryWinStarVfx>();
            _starVfx.raycastTarget = false;
            Image star = Art("Star", _card, "win_victory_star", StoryUi.StarSprite);
            star.preserveAspect = true;
            Place(star.rectTransform, new Vector2(0f, 36f), new Vector2(465f, 465f));
            _starVfx.SetStar(star.rectTransform);
            _originalStars = StoryOriginalWinStars.Attach(effects, _starVfx);

            Image banner = Art("VictoryBanner", _card, "win_victory_banner", StoryArt.Load("win_caption"));
            banner.preserveAspect = true;
            Place(banner.rectTransform, new Vector2(0f, 500f), new Vector2(1000f, 357f));
            // The painted lettering follows the ribbon's arch; draw it whole
            // so resizing the card cannot flatten or stretch the curve.
            Image headline = Art("Headline", banner.transform, "win_victory_headline", null);
            headline.preserveAspect = true;
            Place(headline.rectTransform, new Vector2(0f, -4f), new Vector2(700f, 140f));
            _title = Label("Level", _card, "", 63, Navy,
                new Vector2(0f, 313f), new Vector2(410f, 88f));
            Divider(-285f);
            Divider(285f);

            RectTransform footer = StoryUi.CreateRect("Footer", _card);
            StoryUi.Stretch(footer);
            _footer = footer.gameObject.AddComponent<CanvasGroup>();
            Image badge = StoryUi.CreateImage("RewardBadge", footer, StoryUi.RoundedSprite(40), Gold);
            badge.type = Image.Type.Sliced;
            Place(badge.rectTransform, new Vector2(0f, -260f), new Vector2(602f, 88f));
            Image badgeFill = StoryUi.CreateImage("Fill", badge.transform,
                StoryUi.RoundedSprite(38), new Color32(255, 234, 163, 255));
            badgeFill.type = Image.Type.Sliced;
            StoryUi.Stretch(badgeFill.rectTransform, 3f);
            Image checkDisc = StoryUi.CreateImage("CheckDisc", badge.transform,
                StoryUi.CircleSprite, new Color32(181, 133, 38, 255));
            Place(checkDisc.rectTransform, new Vector2(-247f, 0f), new Vector2(58f, 58f));
            // Geometry avoids a dependency on check/arrow font glyphs.
            Stroke(checkDisc.transform, "CheckShort", new Vector2(-10f, -2f), 18f, -45f, 8f);
            Stroke(checkDisc.transform, "CheckLong", new Vector2(6f, 3f), 32f, 45f, 8f);
            _caption = Label("RewardLabel", badge.transform, "", 40,
                new Color32(135, 93, 27, 255), new Vector2(35f, 0f), new Vector2(480f, 68f));
            Label("Encouragement", footer, "Nicely done!", 58, Navy,
                new Vector2(0f, -355f), new Vector2(700f, 85f));
            Image buttonFace = Art("Continue", footer, "win_victory_button", StoryArt.Load("win_button"));
            Place(buttonFace.rectTransform, new Vector2(0f, -520f), new Vector2(780f, 242f));
            buttonFace.preserveAspect = true;
            buttonFace.raycastTarget = true;
            _continue = buttonFace.gameObject.AddComponent<Button>();
            _continue.targetGraphic = buttonFace;
            ColorBlock colors = _continue.colors;
            colors.highlightedColor = new Color(1f, 1f, 0.94f);
            colors.pressedColor = new Color(0.78f, 0.9f, 0.7f);
            colors.fadeDuration = 0.1f;
            _continue.colors = colors;
            _continue.onClick.AddListener(OnContinue);
            Text buttonLabel = Label("Label", buttonFace.transform, "Continue", 78, Color.white,
                new Vector2(-30f, 1f), new Vector2(570f, 130f));
            ShadowText(buttonLabel, new Color32(27, 91, 19, 255), new Vector2(0f, -4f));
            Stroke(buttonFace.transform, "ArrowTop", new Vector2(251f, 13f), 36f, -45f, 12f);
            Stroke(buttonFace.transform, "ArrowBottom", new Vector2(251f, -13f), 36f, 45f, 12f);
        }

        private void BuildStorybook()
        {
            // Coordinates follow the fourth phone in the approved HTML board:
            // 386-wide ribbon, 327-wide Pip and a 370-wide parchment sheet.
            Image paper = StorybookLayers.Image("StorybookVictory", _card, "victory-paper");
            Place(paper.rectTransform, new Vector2(1, -175), new Vector2(971, 1002));
            Image pip = StorybookArt.Image("Pip", _card, StorybookArt.PipVictory);
            Place(pip.rectTransform, new Vector2(-35, 679), new Vector2(847, 498));
            Image ribbon = StorybookArt.Image("HeadlineArtwork", _card, StorybookArt.VictoryRibbon);
            Place(ribbon.rectTransform, new Vector2(0, 313), new Vector2(1000, 295));
            _title = Label("Level", _card, "", 73, Navy, new Vector2(0, 207), new Vector2(710, 98));
            RectTransform effects = StoryUi.CreateRect("StarEffects", _card);
            Place(effects, new Vector2(0, -56), new Vector2(835, 750));
            _starVfx = effects.gameObject.AddComponent<StoryWinStarVfx>();
            _starVfx.raycastTarget = false;
            Image star = StorybookArt.Image("Star", _card, StorybookArt.VictoryStar);
            Place(star.rectTransform, new Vector2(0, -56), new Vector2(422, 401));
            _starVfx.SetStar(star.rectTransform);
            _originalStars = StoryOriginalWinStars.Attach(effects, _starVfx);
            RectTransform footer = StoryUi.Stretch(StoryUi.CreateRect("Footer", _card));
            _footer = footer.gameObject.AddComponent<CanvasGroup>();
            _caption = Label("RewardLabel", footer, "", 102, Navy, new Vector2(0, -326), new Vector2(840, 125));
            StoryUi.FitOneLine(_caption, 52, 102);
            Image action = StorybookArt.Image("Continue", footer, StorybookArt.VictoryButton);
            _continue = StoryCastleUi.Clickable(action, OnContinue);
            Place(action.rectTransform, new Vector2(0, -505), new Vector2(803, 213));
        }

        private void Divider(float x)
        {
            Image line = StoryUi.CreateImage("LevelDivider", _card, null, Gold);
            Place(line.rectTransform, new Vector2(x, 313f), new Vector2(132f, 3f));
            Image diamond = StoryUi.CreateImage("Diamond", _card, null, Gold);
            Place(diamond.rectTransform, new Vector2(x - Mathf.Sign(x) * 68f, 313f), new Vector2(12f, 12f));
            diamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        private static Image Art(string name, Transform parent, string key, Sprite fallback)
        {
            Sprite sprite = StoryArt.Load(key);
            if (sprite == null) Debug.LogWarning("[Story] victory art missing: " + key + "; run tools/win_art.py.");
            return StoryUi.CreateImage(name, parent, sprite != null ? sprite : fallback, Color.white);
        }

        private static Text Label(string name, Transform parent, string content, int size,
            Color color, Vector2 at, Vector2 dimensions)
        {
            Text label = StoryUi.CreateText(name, parent, content, size, color, TextAnchor.MiddleCenter);
            label.font = StorybookUi.Enabled ? StorybookArt.Font : StoryUi.DisplayFont;
            label.fontStyle = FontStyle.Normal;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.RoundToInt(size * 0.75f);
            label.resizeTextMaxSize = size;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            Place(label.rectTransform, at, dimensions);
            return label;
        }

        private static void ShadowText(Text label, Color color, Vector2 distance)
        {
            Shadow shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }

        private static void Stroke(Transform parent, string name, Vector2 at, float length,
            float angle, float width)
        {
            Image stroke = StoryUi.CreateImage(name, parent, StoryUi.RoundedSprite(6), Color.white);
            stroke.type = Image.Type.Sliced;
            Place(stroke.rectTransform, at, new Vector2(length, width));
            stroke.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private static void Place(RectTransform rect, Vector2 at, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = at;
            rect.sizeDelta = size;
        }

        private void FitToScreen()
        {
            Rect safe = Screen.safeArea;
            Vector2 canvasSize = ((RectTransform)_canvas.transform).rect.size;
            if (canvasSize.x <= 0f || canvasSize.y <= 0f) return;
            if (safe.width <= 0f || safe.height <= 0f) safe = new Rect(0f, 0f, Screen.width, Screen.height);
            float width = Mathf.Max(1f, Screen.width), height = Mathf.Max(1f, Screen.height);
            _safeArea.anchorMin = new Vector2(safe.xMin / width, safe.yMin / height);
            _safeArea.anchorMax = new Vector2(safe.xMax / width, safe.yMax / height);
            _safeArea.offsetMin = _safeArea.offsetMax = Vector2.zero;
            Vector2 available = Vector2.Scale(canvasSize, _safeArea.anchorMax - _safeArea.anchorMin);
            // Uniform scaling preserves the whole layout on phones and tablets.
            float designHeight = StorybookUi.Enabled ? 1620 : DesignSize.y;
            float scale = Mathf.Min((available.x - 44f) / DesignSize.x,
                (available.y - 90f) / designHeight, 1.08f);
            _layout.localScale = Vector3.one * Mathf.Max(0.1f, scale);
            if (StorybookUi.Enabled) _layout.anchoredPosition = new Vector2(0, -90 * scale);
            _lastSafeArea = safe;
            _lastCanvasSize = canvasSize;
        }

        private IEnumerator Present()
        {
            _presenting = false;
            _cardGroup.alpha = _footer.alpha = 0f;
            _footer.blocksRaycasts = false;
            _scrim.color = Color.clear;
            // The invisible scrim still catches taps while we capture the board,
            // including its Screen Space - Camera HUD, before showing the card.
            yield return new WaitForEndOfFrame();
            CaptureBackdrop();
            _openedAt = StoryClock.Now;
            _presenting = true;
            _starVfx.Play(_openedAt);
            if (_originalStars != null) _originalStars.Play();
            Animate(0f);
        }

        private void Update()
        {
            if (_canvas == null) return;
            if (_lastSafeArea != Screen.safeArea ||
                _lastCanvasSize != ((RectTransform)_canvas.transform).rect.size) FitToScreen();
            if (_presenting) Animate(Mathf.Max(0f, StoryClock.Now - _openedAt));
        }

        private void Animate(float elapsed)
        {
            float t = Mathf.Clamp01(elapsed / 0.38f);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            _card.localScale = Vector3.one * Mathf.Lerp(0.88f, 1f, ease);
            _card.anchoredPosition = new Vector2(0f, Mathf.Lerp(-32f, 0f, ease));
            _cardGroup.alpha = Mathf.Clamp01(elapsed / 0.18f);
            _scrim.color = new Color(ScrimColor.r, ScrimColor.g, ScrimColor.b,
                ScrimColor.a * Mathf.Clamp01(elapsed / 0.22f));
            _footer.alpha = Mathf.Clamp01((elapsed - 0.52f) / 0.25f);
            _footer.blocksRaycasts = elapsed >= 0.62f;
        }

        private void ShowSettled()
        {
            _presenting = false;
            _card.localScale = Vector3.one;
            _card.anchoredPosition = Vector2.zero;
            _cardGroup.alpha = _footer.alpha = 1f;
            _footer.blocksRaycasts = true;
            _scrim.color = ScrimColor;
            _starVfx.Settle();
        }

        private void CaptureBackdrop()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            Shader shader = Resources.Load<Shader>("Story/UI/WinBackdropBlur");
            if (shader == null || !shader.isSupported) return;
            if (_blurMaterial == null) _blurMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            int width = Mathf.Max(1, Screen.width), height = Mathf.Max(1, Screen.height);
            RenderTexture capture = RenderTexture.GetTemporary(width, height, 0);
            RenderTexture scratch = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                StoryWindow.CaptureInto(capture);
                int downsample = StorybookUi.Enabled ? 2 : 4;
                int smallWidth = Mathf.Max(1, width / downsample), smallHeight = Mathf.Max(1, height / downsample);
                _blurred = RenderTexture.GetTemporary(smallWidth, smallHeight, 0);
                _blurred.filterMode = FilterMode.Bilinear;
                _blurred.wrapMode = TextureWrapMode.Clamp;
                scratch = RenderTexture.GetTemporary(smallWidth, smallHeight, 0);
                scratch.filterMode = FilterMode.Bilinear;
                scratch.wrapMode = TextureWrapMode.Clamp;
                Graphics.Blit(capture, _blurred);
                for (int i = 0; i < (StorybookUi.Enabled ? 1 : 2); i++)
                {
                    _blurMaterial.SetVector("_Direction", new Vector4(1f / smallWidth, 0f, 0f, 0f));
                    Graphics.Blit(_blurred, scratch, _blurMaterial);
                    _blurMaterial.SetVector("_Direction", new Vector4(0f, 1f / smallHeight, 0f, 0f));
                    Graphics.Blit(scratch, _blurred, _blurMaterial);
                }
                _backdrop.texture = _blurred;
                _backdrop.uvRect = SystemInfo.graphicsUVStartsAtTop ? new Rect(0,1,1,-1) : new Rect(0,0,1,1);
                _backdrop.enabled = true;
            }
            catch (Exception e)
            {
                // An unavailable capture must never leave an invisible modal
                // blocking the player. The navy scrim is sufficient on its own.
                ReleaseBackdrop();
                Debug.LogWarning("[Story] victory backdrop unavailable: " + e.Message);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(capture);
                if (scratch != null) RenderTexture.ReleaseTemporary(scratch);
            }
        }

        private void ReleaseBackdrop()
        {
            if (_backdrop != null) { _backdrop.texture = null; _backdrop.enabled = false; }
            if (_blurred != null) { RenderTexture.ReleaseTemporary(_blurred); _blurred = null; }
        }

        private void OnContinue()
        {
            if (_continuing || !gameObject.activeSelf) return;
            _continuing = true;
            _continue.interactable = false;
            Action go = _onContinue;
            Close();
            go?.Invoke();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _presenting = false;
            _onContinue = null;
            ReleaseBackdrop();
        }

        private void OnDestroy()
        {
            ReleaseBackdrop();
            if (_blurMaterial != null) Destroy(_blurMaterial);
            if (_instance == this) _instance = null;
        }
    }
}
