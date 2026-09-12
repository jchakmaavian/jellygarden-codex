using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    /// <summary>The floating controls from home concept 03, Castle First.</summary>
    public static class StoryCastleUi
    {
        public static readonly Color Cream = new Color32(255, 250, 222, 255);
        public static readonly Color Ink = new Color32(66, 86, 62, 255);
        public static readonly Color Gold = new Color32(255, 219, 114, 255);

        public static void Place(RectTransform rect, float x, float y, float width, float height,
            float anchorX = 0f, float anchorY = 1f)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(anchorX, anchorY);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        public static Image Plate(string name, Transform parent, float radius, Color color)
        {
            Image image = StoryUi.CreateImage(name, parent, StoryUi.RoundedSprite(Mathf.CeilToInt(radius)), color);
            image.type = Image.Type.Sliced;
            return image;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color,
            bool display = false, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            Text label = StoryUi.CreateText(name, parent, text, size, color, alignment);
            label.font = display ? StoryUi.DisplayFont : StoryUi.BodyFont;
            StoryUi.FitOneLine(label, size, size);
            return label;
        }

        public static Image Icon(string name, Transform parent, string key)
        {
            Image image = StoryUi.CreateImage(name, parent, StoryArt.Load(key), Color.white);
            image.preserveAspect = true;
            return image;
        }

        public static Button Clickable(Image image, UnityAction onClick)
        {
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.96f);
            colors.pressedColor = new Color(0.79f, 0.87f, 0.77f, 1f);
            colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.65f);
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            button.onClick.AddListener(onClick);
            return button;
        }

        public static void Gradient(Image image, Color top, Color bottom)
        {
            var gradient = image.gameObject.AddComponent<StoryCastleGradient>();
            gradient.Top = top;
            gradient.Bottom = bottom;
            image.SetVerticesDirty();
        }

        public static StoryUi.TopBarParts TopBar(Transform parent, UnityAction settings)
        {
            var parts = new StoryUi.TopBarParts();
            parts.Root = StoryUi.CreateRect("FloatingWallet", parent);
            Place(parts.Root, 17, -23, 326, 44);
            parts.Coins = Currency(parts.Root, "Coins", "icon_coin", 0);
            parts.Stars = Currency(parts.Root, "Stars", "icon_star", 91);

            Image hit = StoryUi.CreateImage("Settings", parts.Root, null, Color.clear);
            Place(hit.rectTransform, 287, 5, 44, 44);
            parts.Settings = Clickable(hit, settings);
            Image circle = Plate("Face", hit.transform, 18, new Color(1f, 0.985f, 0.9f, 0.78f));
            Place(circle.rectTransform, 4.5f, -4.5f, 35, 35);
            parts.Settings.targetGraphic = circle;
            Image gear = Icon("Gear", hit.transform, "icon_gear");
            Place(gear.rectTransform, 10, -10, 24, 24);
            return parts;
        }

        private static Text Currency(Transform parent, string name, string icon, float x)
        {
            Image border = Plate(name, parent, 18, new Color(1f, 1f, 0.96f, 0.8f));
            Place(border.rectTransform, x, -4, 79, 34);
            Image fill = Plate("Cream", border.transform, 17, new Color32(247, 247, 227, 230));
            StoryUi.Stretch(fill.rectTransform, 1);
            Text label = Label("Value", border.transform, "0", 19, Ink, true, TextAnchor.MiddleCenter);
            Place(label.rectTransform, 31, -1, 43, 31);
            StoryUi.FitOneLine(label, 11, 19);
            Image art = Icon("Icon", border.transform, icon);
            Place(art.rectTransform, -6, 3, 37, 37);
            return label;
        }

        public static StoryUi.NavBarParts NavBar(Transform parent, StoryUi.NavIcon[] icons,
            string[] labels, UnityAction<int> onTab)
        {
            var parts = new StoryUi.NavBarParts();
            RectTransform frame = StoryCastleFrame.Create(parent);
            Image rim = Plate("NavigationDock", frame, 23, new Color(0.85f, 0.94f, 0.83f, 0.46f));
            Place(rim.rectTransform, 16, 88, 328, 70, 0, 0);
            parts.Root = rim.rectTransform;
            parts.Rim = rim.rectTransform;
            parts.Body = Plate("Glass", rim.transform, 22, new Color32(23, 59, 60, 239));
            StoryUi.Stretch(parts.Body.rectTransform, 1);
            parts.Body.raycastTarget = true;
            parts.Tabs = new StoryUi.NavTabParts[icons.Length];
            string[] keys = { "nav_areas", "nav_leaderboard", "nav_home", "nav_cards" };
            for (int i = 0; i < icons.Length; i++)
            {
                int slot = i;
                RectTransform tabRoot = StoryUi.CreateRect(labels[i] + "Tab", rim.transform);
                Place(tabRoot, 5 + i * 79.5f, -5, 79.5f, 58);
                // Keep changing selection backgrounds separate from the artwork.
                // Icons always draw above the face, including after returning
                // from another screen and rebuilding the canvas batches.
                Image face = Plate(labels[i], tabRoot, 17, Color.clear);
                StoryUi.Stretch(face.rectTransform);
                Button button = Clickable(face, () => onTab(slot));
                Text label = Label("Label", tabRoot, labels[i], 10, Cream, true, TextAnchor.MiddleCenter);
                Place(label.rectTransform, 0, -34, 79.5f, 19);
                Image glyph = Icon("Icon", tabRoot, keys[i]);
                Place(glyph.rectTransform, 25.25f, -5, 29, 30);
                parts.Tabs[i] = new StoryUi.NavTabParts
                {
                    Icon = icons[i], Button = button, Face = face,
                    Board = face.rectTransform, Glyph = glyph.rectTransform, Label = label
                };
            }
            return parts;
        }

        public static void SelectNav(StoryUi.NavBarParts parts, int selected)
        {
            if (parts == null) return;
            for (int i = 0; i < parts.Tabs.Length; i++)
            {
                StoryUi.NavTabParts tab = parts.Tabs[i];
                bool active = i == selected;
                tab.Raised = active;
                tab.Face.color = active ? new Color32(240, 240, 205, 255) : new Color(1f, 1f, 1f, 0f);
                tab.Label.color = active ? new Color32(62, 99, 79, 255) : Cream;
                Place(tab.Glyph, active ? 21.75f : 25.25f, active ? 1 : -5,
                    active ? 36 : 29, active ? 37 : 30);
            }
        }

        // A small UI glyph, built at its final size so no new bitmap is needed.
        public static void Hammer(Transform parent)
        {
            RectTransform root = StoryUi.CreateRect("Hammer", parent);
            Place(root, 13, -13, 19, 19);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = new Vector2(22.5f, -22.5f);
            root.localRotation = Quaternion.Euler(0, 0, -42);
            Image handle = Plate("Handle", root, 2, Ink);
            Place(handle.rectTransform, 7.5f, -5, 4, 16);
            Image head = Plate("Head", root, 1, Ink);
            Place(head.rectTransform, 2, 0, 15, 6);
            Image cap = Plate("Cap", root, 1, Ink);
            Place(cap.rectTransform, 14, 1, 3, 8);
        }
    }
}
