// A small runtime UI builder for the story screens.
//
// The story adds several screens the kit's scenes have no prefabs for. Building
// them in code rather than editing main.unity / game.unity keeps every existing
// prefab, animation and particle system exactly where it is, and means a screen
// can be changed without a scene merge conflict.
//
// Everything here reuses the kit's own fonts and sprites, so the story screens
// sit in the same visual language as the rest of the game.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public static partial class StoryUi
    {
        // The kit's UI sprites live outside Resources, where a built player
        // cannot reach them, so tools/ui_sprites.py copies the handful the story
        // needs into Resources/Story/UI with their original import settings.
        //
        // Panels and buttons are NOT taken from there. Every panel the kit ships
        // is bespoke artwork with its labels painted in — play_menu_01 has
        // "Level", "Target:" and "Choose the boost:" baked into the image, and
        // play_button has "Play" baked into it. Borrowing one as a generic frame
        // put someone else's labels behind the story's own text, which is exactly
        // how it looked on screen. The story draws its own instead.
        private const string SpriteRoot = "Story/UI/";

        public static readonly Color Ink = new Color(0.16f, 0.11f, 0.07f, 1f);
        public static readonly Color Parchment = new Color(0.96f, 0.91f, 0.80f, 1f);
        public static readonly Color Dim = new Color(0f, 0f, 0f, 0.72f);
        public static readonly Color Gold = new Color(0.98f, 0.78f, 0.25f, 1f);
        public static readonly Color Muted = new Color(0.45f, 0.40f, 0.34f, 1f);

        private static Font _font;

        /// <summary>
        /// The kit's own UI font, found by looking at a Text the kit already has.
        /// Falls back to Unity's built-in font so a screen is never invisible.
        /// </summary>
        public static Font Font
        {
            get
            {
                if (_font != null) return _font;

                var existing = Object.FindFirstObjectByType<Text>(FindObjectsInactive.Include);
                if (existing != null && existing.font != null)
                {
                    _font = existing.font;
                    return _font;
                }

                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        private static Font _displayFont;
        private static bool _displayFontLooked;

        /// <summary>
        /// The chunky face a headline wears: the kit's BorisBlackBloxx, copied
        /// into Resources by tools/splash_build.py so a build carries it.
        ///
        /// <see cref="Font"/> above takes whichever Text happens to be in the
        /// scene, which is right for body copy and wrong for a hero button —
        /// on the splash the only Texts are the kit's loading and privacy
        /// labels, both wearing Unity's default, so a Play button built there
        /// came out in Arial next to a painted gold wordmark.
        ///
        /// Falls back to <see cref="Font"/>, so a build without the copy draws
        /// a plainer button rather than an invisible one.
        /// </summary>
        public static Font DisplayFont
        {
            get
            {
                if (_displayFontLooked) return _displayFont ?? Font;

                _displayFontLooked = true;
                _displayFont = Resources.Load<Font>("Story/Fonts/story_display");
                if (_displayFont == null)
                    Debug.LogWarning("[Story] No Story/Fonts/story_display; " +
                                     "headlines fall back to the kit's UI font.");
                return _displayFont ?? Font;
            }
        }

        private static Font _nameFont, _bodyFont;
        private static bool _nameFontLooked, _bodyFontLooked;

        /// <summary>
        /// Baloo 2 Bold — the face a character's NAME is set in. Instanced to a
        /// single weight before import: Unity's legacy Font renders one weight,
        /// so a variable TTF would come out Regular however it was asked for.
        /// </summary>
        public static Font NameFont
        {
            get
            {
                if (_nameFontLooked) return _nameFont ?? DisplayFont;

                _nameFontLooked = true;
                _nameFont = Resources.Load<Font>("Story/Fonts/story_name");
                if (_nameFont == null)
                    Debug.LogWarning("[Story] No Story/Fonts/story_name; " +
                                     "speaker names fall back to the display face.");
                return _nameFont ?? DisplayFont;
            }
        }

        /// <summary>
        /// Nunito SemiBold — the face spoken lines are set in. A body face is a
        /// different job from a display one: it is read in sentences, over
        /// artwork, at a size the display face would be shouting at.
        /// </summary>
        public static Font BodyFont
        {
            get
            {
                if (_bodyFontLooked) return _bodyFont ?? Font;

                _bodyFontLooked = true;
                _bodyFont = Resources.Load<Font>("Story/Fonts/story_body");
                if (_bodyFont == null)
                    Debug.LogWarning("[Story] No Story/Fonts/story_body; " +
                                     "spoken lines fall back to the kit's UI font.");
                return _bodyFont ?? Font;
            }
        }

        // Unlabelled kit art is still fair game — a star is a star.
        public static Sprite StarSprite { get { return LoadSprite("star"); } }
        public static Sprite LockSprite { get { return LoadSprite("lock"); } }

        private static Sprite _panelSprite;
        private static Sprite _buttonSprite;

        /// <summary>
        /// Makes a Text shrink to fit on ONE line instead of wrapping onto two.
        ///
        /// Unity's best-fit wraps before it shrinks. Given a rect too narrow for
        /// the words, it breaks the line first and only then reduces the size to
        /// fit the height — so a long name in a short box becomes two lines at
        /// the minimum size, overflowing the box, rather than one smaller line
        /// inside it.
        ///
        /// Turning horizontal wrapping off is what makes best-fit solve for
        /// WIDTH. It matters here because the 150 area names run from "The Mint"
        /// to "Grimm's Childhood Room", nearly double the length, and every one
        /// of them is drawn in a box sized for the short ones.
        /// </summary>
        public static void FitOneLine(Text text, int min, int max)
        {
            if (text == null) return;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = min;
            text.resizeTextMaxSize = max;
        }

        /// <summary>A plain rounded card, drawn rather than borrowed.</summary>
        public static Sprite PanelSprite
        {
            get
            {
                if (_panelSprite == null) _panelSprite = RoundedRect(48, Color.white);
                return _panelSprite;
            }
        }

        /// <summary>A plain rounded pill for buttons, tinted by the caller.</summary>
        public static Sprite ButtonSprite
        {
            get
            {
                if (_buttonSprite == null) _buttonSprite = RoundedRect(28, Color.white);
                return _buttonSprite;
            }
        }

        /// <summary>
        /// Builds a white rounded rectangle as a 9-sliced sprite, so one texture
        /// serves every panel size without stretching its corners. White so the
        /// Image colour decides the look.
        /// </summary>
        private static Sprite RoundedRect(int radius, Color colour)
        {
            int size = radius * 2 + 2;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Distance from the nearest corner centre, so the corners
                    // round and the edges stay straight when sliced.
                    float dx = Mathf.Max(radius - x, x - (size - 1 - radius), 0f);
                    float dy = Mathf.Max(radius - y, y - (size - 1 - radius), 0f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    // One pixel of feather so the edge is not stair-stepped.
                    float alpha = Mathf.Clamp01(radius - distance);
                    pixels[y * size + x] = new Color(colour.r, colour.g, colour.b, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static readonly Dictionary<string, Sprite> _sprites =
            new Dictionary<string, Sprite>();
        private static readonly HashSet<string> _warned = new HashSet<string>();

        public static Sprite LoadSprite(string name)
        {
            Sprite sprite;
            if (_sprites.TryGetValue(name, out sprite)) return sprite;

            sprite = Resources.Load<Sprite>(SpriteRoot + name);
            _sprites[name] = sprite;

            if (sprite == null && _warned.Add(name))
                Debug.LogWarning("[Story] Missing sprite Resources/" + SpriteRoot + name +
                                 " — run tools/ui_sprites.py. Falling back to flat colour.");
            return sprite;
        }

        /// <summary>
        /// The sprite names that were missing and fell back to flat colour. A
        /// fallback draws, so nothing else notices; StoryContentAudit reads this
        /// to fail on one rather than shipping it.
        /// </summary>
        public static IEnumerable<string> WarnedSprites
        {
            get { return _warned; }
        }

        /// <summary>Test hook — forgets what has warned, so the next run reports its own.</summary>
        public static void ClearSpriteWarnings()
        {
            _warned.Clear();
        }

        /// <summary>
        /// A full-screen canvas that sorts above the kit's UI. Story screens are
        /// modal, so they own their own canvas rather than fighting for a slot in
        /// the kit's hierarchy.
        /// </summary>
        public static Canvas CreateOverlayCanvas(string name, int sortingOrder = 500)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>A rect that fills its parent.</summary>
        public static RectTransform Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            return rect;
        }

        public static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        public static Text CreateText(string name, Transform parent, string content,
            int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            RectTransform rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.color = color;
            text.text = content;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.supportRichText = true;
            return text;
        }

        /// <summary>
        /// A button drawn with the kit's play-button sprite. The label is a child
        /// so it can be recoloured independently when the button is disabled.
        /// </summary>
        public static Button CreateButton(string name, Transform parent, string label,
            int fontSize, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rect = CreateRect(name, parent);

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = ButtonSprite;
            image.color = Gold;
            image.type = Image.Type.Sliced;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null) button.onClick.AddListener(onClick);

            Text text = CreateText("Label", rect, label, fontSize, Ink, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 8f);

            return button;
        }

        /// <summary>A tap-anywhere catcher, used to dismiss cards and advance dialogue.</summary>
        public static Button CreateFullScreenButton(string name, Transform parent,
            Color color, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rect = Stretch(CreateRect(name, parent));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = null;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            if (onClick != null) button.onClick.AddListener(onClick);
            return button;
        }

        public static void SetInteractable(Button button, bool interactable)
        {
            if (button == null) return;
            button.interactable = interactable;

            var image = button.targetGraphic as Image;
            if (image != null)
                image.color = interactable ? Gold : new Color(0.62f, 0.60f, 0.57f, 1f);

            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.color = interactable ? Ink : Muted;
        }

        /// <summary>Anchors a rect to the top of its parent with a fixed height.</summary>
        public static RectTransform TopAnchored(RectTransform rect, float top, float height,
            float sidePadding = 0f)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(sidePadding, -top - height);
            rect.offsetMax = new Vector2(-sidePadding, -top);
            return rect;
        }

        /// <summary>Anchors a rect to the bottom of its parent with a fixed height.</summary>
        public static RectTransform BottomAnchored(RectTransform rect, float bottom, float height,
            float sidePadding = 0f)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(sidePadding, bottom);
            rect.offsetMax = new Vector2(-sidePadding, bottom + height);
            return rect;
        }
    }
}
