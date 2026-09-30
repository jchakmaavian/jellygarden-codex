using System.Collections.Generic;
using JellyGarden.Scripts;
using UnityEngine;
using UnityEngine.UI;
using static RoyalThrone.Presentation.RoyalCardKit;
using Art = RoyalThrone.Presentation.RoyalProfileArt;

namespace RoyalThrone.Presentation
{
    // The Edit Profile card (2026-09-30), over the Profile page, in the ribbon cards' style (Level Failed, How to Play).
    // - Its parts: "Edit Profile" on the ribbon with the red close; the preview (the avatar in its frame, the name field with
    //   its edit button); the Avatar / Frame / Name / Badge tabs over a cream panel of choices; Save.
    // - A choice shows in the preview at once; Save keeps it (RoyalProfile.Save), and is off until something changed and
    //   the name is 3 to 14 letters or numbers. The close (or Back) leaves the profile as it was.
    // - Badges come with Collections, which are not open: that tab says so.
    public sealed class RoyalProfileEditor : MonoBehaviour
    {
        const float H = 1170, CardW = 664, PanelW = 600, Tile = 150, Pitch = 184;
        static readonly string[] Tabs = { "Avatar", "Frame", "Name", "Badge" };

        RoyalProfilePage owner;
        RectTransform root, scaler, card, preview, grid;
        CanvasGroup cardGroup;
        InputField field;
        Text hint;
        Button save;
        readonly List<Transform> tabButtons = new List<Transform>();
        string avatar; int frame, style, tab = -1;
        bool built;

        public bool IsOpen => root && root.gameObject.activeSelf;
        public int Tab => tab;
        public string PendingAvatar => avatar;
        public int PendingFrame => frame;
        public int PendingStyle => style;
        public InputField NameField => field;
        public Button SaveButton => save;
        public RectTransform Grid => grid;
        public RectTransform Card => card;

        public static RoyalProfileEditor Build(RectTransform page, RoyalProfilePage owner)
        {
            var go = new GameObject("Royal edit profile", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var rt = (RectTransform)go.transform;
            rt.SetParent(page, false);
            Stretch(rt, 0);
            var canvas = go.GetComponent<Canvas>();
            canvas.overrideSorting = true; canvas.sortingOrder = 61;
            var editor = go.AddComponent<RoyalProfileEditor>();
            editor.owner = owner; editor.root = rt;
            editor.BuildCard();
            go.SetActive(false);
            return editor;
        }

        public void Open()
        {
            avatar = RoyalProfile.Avatar; frame = RoyalProfile.Frame; style = RoyalProfile.NameStyle;
            field.text = RoyalProfile.Name;
            root.gameObject.SetActive(true);
            ShowTab(0);
            Preview();
        }

        public void Close() { if (root) root.gameObject.SetActive(false); }

        public void Choose(int t) => ShowTab(t);
        public void PickAvatar(string id) { avatar = id; ShowTab(tab); Preview(); }
        public void PickFrame(int f) { frame = f; ShowTab(tab); Preview(); }
        public void PickStyle(int s) { style = s; ShowTab(tab); Preview(); }

        public void Save()
        {
            if (!save || !save.interactable) return;
            RoyalProfile.Save(field.text, avatar, frame, style);
            Close();
        }

        bool Changed => RoyalProfile.Clean(field.text) != RoyalProfile.Name || avatar != RoyalProfile.Avatar || frame != RoyalProfile.Frame || style != RoyalProfile.NameStyle;
        bool NameOk => RoyalProfile.Clean(field.text).Length >= RoyalProfile.NameMin;

        void LateUpdate()
        {
            if (!built) return;
            // The card keeps the page's scale (its 800-unit column), centred on the screen.
            float s = owner ? owner.ColumnScale : 1;
            if (Mathf.Abs(scaler.localScale.x - s) > .0005f) scaler.localScale = new Vector3(s, s, 1);
            bool ok = NameOk;
            save.interactable = ok && Changed;
            if (hint.enabled == ok) hint.enabled = !ok;
        }

        // ---- Building ----------------------------------------------------------------------------------------------------

        void BuildCard()
        {
            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(root, false);
            Stretch((RectTransform)dim.transform, 0);
            var dimImage = dim.GetComponent<Image>(); dimImage.color = DimColor; dimImage.raycastTarget = true;

            // The page's scale goes on a holder, so the card's own entrance (0.96 -> 1) plays inside it.
            scaler = (RectTransform)new GameObject("Scaler", typeof(RectTransform)).transform;
            scaler.SetParent(root, false);
            RoyalMenuSkin.Place(scaler, 0, 0, 0, 0);
            card = (RectTransform)new GameObject("Card", typeof(RectTransform), typeof(CanvasGroup)).transform;
            card.SetParent(scaler, false);
            RoyalMenuSkin.Place(card, 0, -16, CardW, H);
            cardGroup = card.GetComponent<CanvasGroup>();
            float top = H * .5f;
            var skin = Container(card, "Royal skin");
            CleanSurface(skin, CardW, H);
            var crest = skin.Find("Crest"); if (crest) crest.GetComponent<Image>().enabled = false;   // the ribbon carries the title
            Ribbon(card, "Edit Profile", H);
            var controls = new List<RoyalCardControl>();

            var close = Hit(card, "Royal close", Close);
            RedClose(close, H);
            controls.Add(Feedback(close, false));

            // The preview: the page's card look, the avatar, the name field and its edit button.
            preview = (RectTransform)Container(card, "Royal preview");
            RoyalMenuSkin.Place(preview, 0, top - 210, PanelW, 200);
            var shade = Shape(preview, "Shadow", 0, -4, PanelW + 8, 208, 30, C("000a30", .35f), C("000a30", .35f)); shade.softness = 12;
            Inset(preview, "Rim", 0, 28, C("86aaf2", .55f), C("4a76d6", .4f));
            Inset(preview, "Fill", 2.4f, 25.6f, C("12399a", .92f), C("0b2a78", .92f));
            Art.Tile(preview, "Royal preview avatar", -205, 0, Tile, RoyalProfileArt.Find(RoyalProfile.Avatar), RoyalProfile.Frame);
            BuildField(controls);
            hint = Words(preview, "Royal name hint", "3 to 14 letters or numbers", 40, -66, 330, 20, Ink.Light);
            hint.enabled = false;

            // The choices: the tabs over the cream panel, then the grid (it scrolls when there are more rows than fit).
            float panelH = 640, panelY = top - 340 - panelH * .5f;
            var panel = RoyalCardKit.Panel(card, "Royal choices", 0, panelY, PanelW, panelH, 26);
            float barY = panelH * .5f - 24 - 44;
            var bar = Art.Bar(panel, "Royal tabs", barY, 560, 88);
            float w = (560 - 16 - 18) / 4f;
            for (int i = 0; i < Tabs.Length; i++)
            {
                int index = i;
                var t = Art.Segment(bar, "Tab " + Tabs[i], Tabs[i], (i - 1.5f) * (w + 6), w, 72, 28, () => ShowTab(index));
                tabButtons.Add(t); controls.Add(Feedback(t, false));
                t.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(Click);
            }
            float lineY = barY - 44 - 16;
            Shape(panel, "Royal separator", 0, lineY, 540, 2, 1, C("c9ae7c", .75f), C("c9ae7c", .75f));
            Shape(panel, "Royal separator light", 0, lineY - 1.7f, 540, 1.4f, .7f, C("ffffff", .8f), C("ffffff", .8f));
            float viewTop = lineY - 10, viewH = viewTop + panelH * .5f - 14;
            var view = (RectTransform)Container(panel, "Royal choice view");
            RoyalMenuSkin.Place(view, 0, viewTop - viewH * .5f, 580, viewH);
            view.gameObject.AddComponent<RectMask2D>();
            var hitArea = view.gameObject.AddComponent<Image>(); hitArea.color = Color.clear;   // drags anywhere in the view scroll it
            grid = (RectTransform)new GameObject("Royal choice grid", typeof(RectTransform)).transform;
            grid.SetParent(view, false);
            grid.anchorMin = grid.anchorMax = grid.pivot = new Vector2(.5f, 1);
            grid.sizeDelta = new Vector2(580, viewH);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = grid; scroll.viewport = view; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic; scroll.scrollSensitivity = 30;

            var saveGo = Hit(card, "Royal save", Save);
            float saveY = top - 1060;
            CleanButton(saveGo, "Save", 0, saveY, 360, 112, Kind.Primary, 46);
            ButtonShadow(skin, 0, saveY, 360, 112);
            save = saveGo.GetComponent<UnityEngine.UI.Button>();
            controls.Add(Feedback(saveGo, true));

            var motion = root.gameObject.AddComponent<RoyalCardMotion>();
            motion.dim = dimImage; motion.dimColor = DimColor; motion.card = card; motion.cardGroup = cardGroup;
            motion.controls = controls.ToArray();
            motion.gloss = saveGo.Find("Royal skin/Sweep") ? saveGo.Find("Royal skin/Sweep").GetComponent<RoyalSweepGraphic>() : null;
            built = true;
        }

        // The name: a cream field with the display font, and the orange edit button beside it that opens the keyboard.
        void BuildField(List<RoyalCardControl> controls)
        {
            var box = Container(preview, "Royal name field");
            RoyalMenuSkin.Place(box, 40, 12, 330, 86);
            const float r = 22;
            Inset(box, "Edge", 0, r, C("c47a26"), C("a8641c"));
            Inset(box, "Gold", 1, r - 1, C("fde58a"), C("e9ae44"));
            Inset(box, "Cream", 4.5f, r - 4.5f, C("fff6e1"), C("f5e2bc"));
            Strip(box, "Cream shade", true, 4.5f, 16, r - 4.5f, C("7a5a2e", .16f), C("7a5a2e", 0));
            var input = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(InputField));
            input.transform.SetParent(box, false);
            Stretch((RectTransform)input.transform, 6);
            input.GetComponent<Image>().color = Color.clear;
            var text = Words(input.transform, "Text", "", 0, 0, 290, 34, Ink.Navy);
            text.alignment = TextAnchor.MiddleLeft; text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            Stretch(text.rectTransform, 0); text.rectTransform.offsetMin = new Vector2(16, 0); text.rectTransform.offsetMax = new Vector2(-10, 0);
            field = input.GetComponent<InputField>();
            field.textComponent = text;
            field.characterLimit = RoyalProfile.NameMax;
            field.lineType = InputField.LineType.SingleLine;
            field.contentType = InputField.ContentType.Standard;
            field.caretColor = C("12337c"); field.customCaretColor = true;
            field.selectionColor = C("7cb4ff", .45f);
            field.onValidateInput += (s, i, c) => char.IsLetterOrDigit(c) || c == ' ' || c == '_' || c == '-' || c == '.' ? c : '\0';
            field.onEndEdit.AddListener(v => { string clean = RoyalProfile.Clean(v); if (clean != v) field.text = clean; Preview(); });

            var edit = Hit(preview, "Royal name edit", () => { field.Select(); field.ActivateInputField(); });
            RoyalMenuSkin.Place(edit, 250, 12, 84, 84);
            var skin = Container(edit, "Royal skin");
            RoyalMenuSkin.Place(skin, 0, 0, 72, 72);
            Inset(skin, "Edge", 0, 16, C("8c4a14"), C("6a3410"));
            Inset(skin, "Gold", 1.1f, 14.9f, C("ffe27a"), C("e6a22a"));
            Inset(skin, "Dark line", 5, 11, C("7a3a0c"), C("5a2a0a"));
            Inset(skin, "Face", 6.3f, 9.7f, C("ffbe4a"), C("ea861e"));
            Strip(skin, "Gloss", true, 8, 18, 8, C("ffffff", .3f), C("ffffff", .04f));
            Art.Pencil(skin, 38, C("fffdf3"), C("fff1dc"));
            controls.Add(Feedback(edit, false));
        }

        // A tappable child with a Button and a clear Image as its tap area.
        static Transform Hit(Transform parent, string name, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = Color.clear;
            var button = go.GetComponent<UnityEngine.UI.Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);
            button.onClick.AddListener(Click);
            return go.transform;
        }

        static void Click() { if (SoundBase.Instance) SoundBase.Instance.PlaySound(SoundBase.Instance.click); }

        // ---- The preview and the tabs --------------------------------------------------------------------------------

        void Preview()
        {
            var tile = preview.Find("Royal preview avatar") as RectTransform;
            Art.SetAvatar(tile, RoyalProfileArt.Find(avatar));
            Art.SetFrame(tile, frame);
            // On the cream field the light name styles would not read: the field shows the style's dark edge colour as ink.
            var s = RoyalProfileArt.NameStyles[style];
            RoyalCardKit.Style(field.textComponent, s.edge, Color.Lerp(s.edge, Color.black, .15f), Color.clear, 0, Color.clear, 0, C("ffffff", .6f), new Vector2(0, -1.2f), 34 * .018f);
        }

        void ShowTab(int t)
        {
            tab = t;
            for (int i = 0; i < tabButtons.Count; i++) Art.Choose(tabButtons[i], i == t);
            for (int i = grid.childCount - 1; i >= 0; i--) { var c = grid.GetChild(i); c.name += " (old)"; c.gameObject.SetActive(false); Destroy(c.gameObject); }
            grid.anchoredPosition = Vector2.zero;
            int count = 0;
            switch (t)
            {
                case 0:
                    foreach (var a in RoyalProfileArt.Avatars)
                    {
                        var at = Cell(count++, 3, Pitch, Pitch);
                        var tile = Art.Tile(grid, "Avatar " + a.id, at.x, at.y, Tile, a, frame); Top(tile);
                        string id = a.id;
                        Tap(tile, () => PickAvatar(id));
                        if (a.id == avatar) Art.Chosen(tile, Tile);
                    }
                    break;
                case 1:
                    for (int i = 0; i < RoyalProfileArt.Frames.Length; i++)
                    {
                        var at = Cell(count++, 3, Pitch, Pitch);
                        var tile = Art.Tile(grid, "Frame " + RoyalProfileArt.Frames[i].name, at.x, at.y, Tile, null, i); Top(tile);
                        int f = i;
                        Tap(tile, () => PickFrame(f));
                        if (i == frame) Art.Chosen(tile, Tile);
                    }
                    break;
                case 2:
                    for (int i = 0; i < RoyalProfileArt.NameStyles.Length; i++)
                    {
                        var at = Cell(count++, 2, 280, 124);
                        var plate = Plate(grid, "Name " + RoyalProfileArt.NameStyles[i].name, at.x, at.y, i); Top(plate);
                        int s = i;
                        Tap(plate, () => PickStyle(s));
                        if (i == style) ChosenPlate(plate);
                    }
                    break;
                default:
                    Top(Art.Picture(grid, "Royal badge art", Art.HomeArt(16492), 0, -130, 170).transform);
                    var message = Words(grid, "Royal badge message", "Badges come with Collections,\nwhich are not open yet.", 0, -290, 540, 30, Ink.Navy);
                    message.lineSpacing = 1.1f; Top(message.transform);
                    count = 0;
                    break;
            }
            int columns = t == 2 ? 2 : 3;
            float rowH = t == 2 ? 124 : Pitch, rows = Mathf.Ceil(count / (float)columns);
            float need = t == 3 ? 380 : rows * rowH + 30;
            grid.sizeDelta = new Vector2(580, Mathf.Max(need, ((RectTransform)grid.parent).rect.height));
        }

        // Anchors a placed item to the grid's top edge (its position is already measured from the top).
        static void Top(Transform t)
        {
            var r = (RectTransform)t; var p = r.anchoredPosition;
            r.anchorMin = r.anchorMax = new Vector2(.5f, 1); r.anchoredPosition = p;
        }

        // The centre of item `index` in a grid of `columns`, from the grid's top.
        static Vector2 Cell(int index, int columns, float pitchX, float pitchY)
        {
            int c = index % columns, r = index / columns;
            return new Vector2((c - (columns - 1) * .5f) * pitchX, -18 - pitchY * .5f - r * pitchY);
        }

        static void Tap(Transform t, UnityEngine.Events.UnityAction action)
        {
            var hit = t.gameObject.GetComponent<Image>(); if (!hit) { hit = t.gameObject.AddComponent<Image>(); hit.color = Color.clear; }
            var button = t.gameObject.GetComponent<UnityEngine.UI.Button>(); if (!button) button = t.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action); button.onClick.AddListener(Click);
            t.gameObject.AddComponent<RoyalHudPress>().parts = new[] { (RectTransform)t };
        }

        // A name plate: the page's blue with a gold rim, the name in style `s`.
        Transform Plate(Transform parent, string name, float x, float y, int s)
        {
            var plate = Container(parent, name);
            RoyalMenuSkin.Place(plate, x, y, 262, 96);
            const float r = 24;
            var shadow = Shape(plate, "Shadow", 0, -4, 266, 100, r + 2, C("000a30", .35f), C("000a30", .35f)); shadow.softness = 10;
            Inset(plate, "Edge", 0, r, C("7a3a12"), C("5a2a0e"));
            Inset(plate, "Gold", 1.2f, r - 1.2f, C("ffe27a"), C("e3a02a"));
            Inset(plate, "Navy line", 5, r - 5, C("0a2c7c"), C("061f5c"));
            Inset(plate, "Face", 6.2f, r - 6.2f, C("2e66d4"), C("1440a2"));
            var text = Words(plate, "Name", RoyalProfile.Clean(field.text).Length > 0 ? RoyalProfile.Clean(field.text) : RoyalProfile.Name, 0, 1, 236, 34, Ink.Display);
            if (text.preferredWidth > 236) { float size = Mathf.Floor(34 * 236 / text.preferredWidth); Paint(text, Ink.Display, size); RoyalMenuSkin.Place(text.transform, 0, 1 + size * CapDrop, 236, size * 1.4f); }
            Art.StyleName(text, s, text.fontSize);
            return plate;
        }

        static void ChosenPlate(Transform plate)
        {
            var mark = Container(plate, "Royal chosen");
            var ring = Shape(mark, "Ring", 0, 0, 262 + 16, 96 + 16, 32, C("7bea4a"), C("2fa836"));
            ring.band = 7;
            var badge = Container(mark, "Badge");
            Badge(badge, true, 118, -36);
            Glyph(badge, "Royal tick", true);
            mark.SetAsLastSibling();
        }
    }
}
