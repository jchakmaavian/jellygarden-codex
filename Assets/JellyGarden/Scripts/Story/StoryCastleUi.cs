using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    /// <summary>Safe-area home controls, with the selected Royal Storybook styling.</summary>
    public static class StoryCastleUi
    {
        public static readonly Color Cream = new Color32(255, 250, 222, 255);
        public static Color Ink => StorybookUi.Enabled ? StorybookUi.Ink : new Color32(66, 86, 62, 255);
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
            label.font = display ? (StorybookUi.Enabled ? StorybookArt.Font : StoryUi.DisplayFont) : StoryUi.BodyFont;
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
            if (onClick != null) button.onClick.AddListener(onClick);
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
            parts.Stars = Currency(parts.Root, "Stars", "icon_star", StorybookUi.Enabled ? 198 : 91);

            Image hit = StoryUi.CreateImage("Settings", parts.Root, null, Color.clear);
            Place(hit.rectTransform, 287, 5, 44, 44);
            parts.Settings = Clickable(hit, settings);
            Image circle = Plate("Face", hit.transform, 18, StorybookUi.Enabled ? StorybookUi.Blue : new Color(1f, 0.985f, 0.9f, 0.78f));
            Place(circle.rectTransform, 4.5f, -4.5f, 35, 35);
            parts.Settings.targetGraphic = circle;
            Image gear = StorybookUi.Enabled ? StorybookArt.Image("Gear", hit.transform, StorybookArt.Settings) : Icon("Gear", hit.transform, "icon_gear");
            Place(gear.rectTransform, StorybookUi.Enabled ? 2 : 10, StorybookUi.Enabled ? -2 : -10, StorybookUi.Enabled ? 40 : 24, StorybookUi.Enabled ? 40 : 24);
            if (StorybookUi.Enabled) circle.enabled = false;
            return parts;
        }

        private static Text Currency(Transform parent, string name, string icon, float x)
        {
            if (StorybookUi.Enabled)
            {
                bool coin = icon == "icon_coin";
                Image wallet = StorybookLayers.Image(name, parent, coin ? "wallet-coin" : "wallet-star");
                Place(wallet.rectTransform, x-10, 7, coin ? 98 : 86, 47);
                Text amount = Label("Value", wallet.transform, "0", 19, Ink, true, TextAnchor.MiddleCenter);
                Place(amount.rectTransform, coin ? 47 : 41, -8, 39, 33);
                StoryUi.FitOneLine(amount,11,19);
                return amount;
            }
            Image border = Plate(name, parent, 18, StorybookUi.Enabled ? StorybookUi.Gold : new Color(1f, 1f, 0.96f, 0.8f));
            Place(border.rectTransform, x, -4, 79, 34);
            Image fill = Plate("Cream", border.transform, 17, StorybookUi.Enabled ? StorybookUi.Paper : new Color32(247, 247, 227, 230));
            StoryUi.Stretch(fill.rectTransform, 1);
            Text label = Label("Value", border.transform, "0", 19, Ink, true, TextAnchor.MiddleCenter);
            Place(label.rectTransform, 31, -1, 43, 31);
            StoryUi.FitOneLine(label, 11, 19);
            Image art = StorybookUi.Enabled ? StorybookArt.Image("Icon", border.transform, icon == "icon_coin" ? StorybookArt.Coin : StorybookArt.SmallStar) : Icon("Icon", border.transform, icon);
            Place(art.rectTransform, -10, 7, 44, 44);
            return label;
        }

        public static StoryUi.NavBarParts NavBar(Transform parent, StoryUi.NavIcon[] icons,
            string[] labels, UnityAction<int> onTab)
        {
            var parts = new StoryUi.NavBarParts();
            RectTransform frame = StoryCastleFrame.Create(parent);
            Image rim = StorybookUi.Enabled ? StorybookLayers.Image("NavigationDock", frame, "navigation-dock")
                : Plate("NavigationDock", frame, 23, new Color(0.85f, 0.94f, 0.83f, 0.46f));
            Place(rim.rectTransform, StorybookUi.Enabled ? 3 : 16, StorybookUi.Enabled ? 84 : 88, StorybookUi.Enabled ? 354 : 328, StorybookUi.Enabled ? 81 : 70, 0, 0);
            parts.Root = rim.rectTransform;
            parts.Rim = rim.rectTransform;
            parts.Body = Plate("Glass", rim.transform, 22, StorybookUi.Enabled ? StorybookUi.Paper : new Color32(23, 59, 60, 239));
            StoryUi.Stretch(parts.Body.rectTransform, 1);
            parts.Body.raycastTarget = true;
            parts.Tabs = new StoryUi.NavTabParts[icons.Length];
            string[] keys = StorybookUi.Enabled
                ? new[] { "nav_home", "nav_areas", "nav_cards" }
                : new[] { "nav_areas", "nav_leaderboard", "nav_home", "nav_cards" };
            if (StorybookUi.Enabled) parts.Body.color = Color.clear;
            float tabWidth = (StorybookUi.Enabled ? 344f : 318f) / icons.Length;
            for (int i = 0; i < icons.Length; i++)
            {
                int slot = i;
                RectTransform tabRoot = StoryUi.CreateRect(labels[i] + "Tab", rim.transform);
                Place(tabRoot, 5 + i * tabWidth, -5, tabWidth, StorybookUi.Enabled ? 70 : 58);
                // Keep changing selection backgrounds separate from the artwork.
                // Icons always draw above the face, including after returning
                // from another screen and rebuilding the canvas batches.
                Image face = Plate(labels[i], tabRoot, 17, Color.clear);
                StoryUi.Stretch(face.rectTransform);
                Button button = Clickable(face, () => onTab(slot));
                Text label = Label("Label", tabRoot, labels[i], 11, StorybookUi.Enabled ? Ink : Cream, true, TextAnchor.MiddleCenter);
                Place(label.rectTransform, 0, -34, tabWidth, 19);
                Image glyph = StorybookUi.Enabled ? StorybookArt.Image("Icon", tabRoot, StorybookArt.Nav(i)) : Icon("Icon", tabRoot, keys[i]);
                glyph.preserveAspect = true;
                if (StorybookUi.Enabled)
                {
                    label.font = StoryUi.BodyFont; label.fontStyle = FontStyle.Normal;
                    StoryUi.FitOneLine(label,14,14);
                    Place(label.rectTransform, 0, -40, tabWidth, 22);
                    Image underline = Plate("SelectedLine", tabRoot, 2, StorybookUi.Blue);
                    Place(underline.rectTransform, 26, -65, tabWidth - 52, 4);
                }
                Place(glyph.rectTransform, (tabWidth - 29) / 2, -5, 29, 30);
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
                tab.Face.color = StorybookUi.Enabled ? Color.clear : active ? new Color32(240, 240, 205, 255) : new Color(1f, 1f, 1f, 0f);
                tab.Label.color = StorybookUi.Enabled ? StorybookUi.Ink : active ? new Color32(62, 99, 79, 255) : Cream;
                if (StorybookUi.Enabled)
                {
                    tab.Board.parent.Find("SelectedLine").gameObject.SetActive(active);
                    Place(tab.Glyph, (tab.Board.rect.width - 46) / 2, 0, 46, 39);
                    continue;
                }
                Place(tab.Glyph, (tab.Board.rect.width - (active ? 36 : 29)) / 2, active ? 1 : -5,
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
