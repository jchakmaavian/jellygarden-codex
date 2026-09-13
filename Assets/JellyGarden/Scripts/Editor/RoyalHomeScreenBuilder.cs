using System;
using System.IO;
using System.Linq;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.MapScripts;
using RoyalThrone.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoyalThrone.Editor
{
    public static class RoyalHomeScreenBuilder
    {
        public const string ScenePath = "Assets/JellyGarden/Scenes/game.unity";
        public const string ArtPath = "Assets/JellyGarden/Textures/RoyalHome/";
        static Font displayFont, bodyFont;
        static Color Blue => Hex("1763dc");
        static Color Navy => Hex("102966");
        static Color Ivory => Hex("fff3d1");

        [MenuItem("Tools/Royal Throne/Home/Build approved homepage _F6")]
        public static void Build()
        {
            RequireCopy();
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before building the homepage.");
            if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("The open scene has unsaved changes. Save it before building the homepage.");
            AssetDatabase.Refresh();
            foreach (string file in Directory.GetFiles(ArtPath, "*.png", SearchOption.AllDirectories)) ConfigureArt(file.Replace('\\', '/'));
            displayFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/JellyGarden/Fonts/BorisBlackBloxx.ttf");
            bodyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var existing in scene.GetRootGameObjects().Where(go => go.name == "Royal Throne Home").ToArray()) UnityEngine.Object.DestroyImmediate(existing);
            var root = Rect("Royal Throne Home", null);
            var canvas = root.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var scaler = root.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root.gameObject.AddComponent<GraphicRaycaster>();
            var home = root.gameObject.AddComponent<RoyalHomeScreen>();
            home.presentation = root.gameObject.AddComponent<CanvasGroup>();
            var frame = Rect("Castle framing", root); Stretch(frame);
            var background = Image("Royal blue courtyard", frame, Art("home-castle"));
            background.raycastTarget = true;
            var aspect = background.gameObject.AddComponent<AspectRatioFitter>(); aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = background.sprite.rect.width / background.sprite.rect.height;
            var glow = Rect("Fountain glow", background.transform).gameObject.AddComponent<RoyalWaterGlow>();
            glow.color = new Color(.8f, .95f, 1, .12f); glow.raycastTarget = false;
            Bounds(glow.rectTransform, .34f, .38f, .66f, .42f); home.waterGlow = glow;
            home.motes = new RoyalPanelGraphic[3];
            for (int i = 0; i < home.motes.Length; i++)
            {
                var mote = Surface("Golden mote " + i, root, Hex("fff1b5"), Hex("ffe6a0"), 5);
                Center(mote.rectTransform, 5, 5, .22f + .27f * i, .45f + .04f * i); home.motes[i] = mote;
            }
            home.safeArea = Rect("Safe area", root); Stretch(home.safeArea);
            home.layout = Rect("Responsive content", home.safeArea); home.layout.anchorMin = new Vector2(.5f, 0); home.layout.anchorMax = new Vector2(.5f, 1); home.layout.sizeDelta = new Vector2(720, 0);
            home.logo = Image("Approved Royal Throne logo", home.layout, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JellyGarden/Textures/menu/main_menu/logo.png")).rectTransform;
            var controls = Rect("Home controls", home.layout); Stretch(controls); home.controls = controls.gameObject.AddComponent<CanvasGroup>();
            home.hud = Rect("Resource bar", controls);
            home.resourceButtons = new Button[5];
            var profile = FramedButton("Profile", home.hud, Blue, Hex("063584"));
            Bounds(profile.GetComponent<RectTransform>(), 0, 0, .145f, 1);
            var portrait = Image("King portrait", profile.transform, Art("avatar-king")); Bounds(portrait.rectTransform, -.05f, .03f, 1.05f, .97f);
            home.resourceButtons[0] = profile;
            home.resourceButtons[1] = Resource("Coins", home.hud, "icon-coin", .155f, .43f, out home.coinLabel);
            var plus = FramedButton("Add coins", home.resourceButtons[1].transform, Hex("92d847"), Hex("2b8316"));
            Bounds(plus.GetComponent<RectTransform>(), .27f, .02f, .48f, .32f);
            // The plus is decoration within the same generous resource hit target.
            UnityEngine.Object.DestroyImmediate(plus.GetComponent<RoyalButtonMotion>()); UnityEngine.Object.DestroyImmediate(plus);
            Label("Plus", home.resourceButtons[1].transform.GetChild(home.resourceButtons[1].transform.childCount - 1), "+", 27, false, Color.white);
            home.resourceButtons[2] = Resource("Lives", home.hud, "icon-heart", .44f, .68f, out home.lifeStatus);
            home.lifeStatus.fontSize = 25;
            var lives = Label("Life count", home.resourceButtons[2].transform, "5", 33, true, Ivory); Bounds(lives.rectTransform, -.02f, .19f, .46f, .80f); home.lifeLabel = lives;
            home.resourceButtons[3] = Resource("Stars", home.hud, "icon-star", .69f, .85f, out home.starLabel);
            var settings = ArtButton("Settings", home.hud, Art("icon-settings")); Bounds(settings.GetComponent<RectTransform>(), .86f, .03f, 1, .97f); home.resourceButtons[4] = settings;

            home.actions = Rect("Level and area actions", controls);
            home.levelButton = ArtButton("Next level", home.actions, Art("button-crowned-blank")); home.levelRect = home.levelButton.GetComponent<RectTransform>();
            home.levelLabel = Label("Level label", home.levelButton.transform, "Level 1", 45, true, Ivory); Bounds(home.levelLabel.rectTransform, .12f, .05f, .88f, .52f);
            Shadow(home.levelLabel, new Vector2(1.5f, -2));
            home.areaButton = FramedButton("Area progress", home.actions, Hex("fff1a5"), Hex("eea72c")); home.areaRect = home.areaButton.GetComponent<RectTransform>();
            home.areaLabel = Label("Area number", home.areaButton.transform, "Area 1", 36, true, Hex("753d13")); Bounds(home.areaLabel.rectTransform, .08f, .52f, .66f, .91f);
            var track = Surface("Area progress track", home.areaButton.transform, Navy, Navy, 10); Bounds(track.rectTransform, .08f, .17f, .62f, .44f);
            var fill = Surface("Area progress fill", track.transform, Hex("66c3ff"), Hex("1763dc"), 8); Stretch(fill.rectTransform); home.areaFill = fill.rectTransform;
            home.areaProgress = Label("Completed levels", track.transform, "0/6", 29, false, Ivory); home.areaProgress.fontStyle = FontStyle.Bold;
            var chest = Image("Royal chest", home.areaButton.transform, Art("icon-chest")); Bounds(chest.rectTransform, .64f, .10f, .99f, .92f);

            var navBackground = Surface("Navigation blue", root, Hex("248afa"), Hex("0849aa"), 0); home.navigationBackground = navBackground.rectTransform;
            home.navigationBackground.anchorMin = Vector2.zero; home.navigationBackground.anchorMax = new Vector2(1, 0); home.navigationBackground.pivot = new Vector2(.5f, 0);
            var trim = Surface("Navigation gold trim", navBackground.transform, Hex("fff1a9"), Hex("d89929"), 0); Bounds(trim.rectTransform, 0, .95f, 1, 1);
            // Background must render below the safe-area controls.
            navBackground.transform.SetSiblingIndex(home.safeArea.GetSiblingIndex());
            home.navigation = Rect("Main navigation", controls); home.tabButtons = new Button[5];
            string[] tabNames = { "Story", "Leagues", "Home", "Team", "Collection" };
            string[] tabIcons = { "icon-story", "icon-trophy", "icon-home", "icon-team", "icon-cards" };
            float[] edges = { 0, .1923f, .3846f, .6154f, .8077f, 1 };
            for (int i = 0; i < 5; i++)
            {
                var button = PlainButton(tabNames[i], home.navigation); Bounds(button.GetComponent<RectTransform>(), edges[i], 0, edges[i + 1], 1);
                button.GetComponent<RoyalButtonMotion>().hoverScale = 1; // Edge tabs must stay within the safe area when focused.
                if (i == 2) button.GetComponent<Image>().color = new Color(.45f, .72f, 1, .24f);
                var icon = Image(tabNames[i] + " icon", button.transform, Art(tabIcons[i]));
                Bounds(icon.rectTransform, i == 2 ? .02f : .12f, .26f, i == 2 ? .98f : .88f, i == 2 ? 1.36f : .94f);
                var label = Label(tabNames[i] + " label", button.transform, tabNames[i], i == 2 ? 29 : 23, true, Ivory); Bounds(label.rectTransform, 0, .07f, 1, .31f); Shadow(label, new Vector2(0, -1.5f));
                home.tabButtons[i] = button;
            }

            var veil = Image("Home dialog", home.safeArea, null); Stretch(veil.rectTransform); veil.color = new Color(.02f, .075f, .2f, .64f); veil.raycastTarget = true; home.dialog = veil.gameObject;
            var dialog = Panel("Royal dialog panel", veil.transform, Ivory, Hex("f4e5c7")); home.dialogRect = dialog;
            var close = FramedButton("Close", dialog, Hex("367eea"), Hex("0640a1")); Center(close.GetComponent<RectTransform>(), 96, 96, 1, 1); Label("Close symbol", close.transform, "×", 42, false, Ivory); home.closeButton = close;
            close.GetComponent<RectTransform>().anchoredPosition = new Vector2(-16, -16);
            home.dialogIcon = Image("Dialog icon", dialog, Art("icon-chest")); Bounds(home.dialogIcon.rectTransform, .37f, .72f, .63f, .96f);
            home.dialogTitle = Label("Dialog title", dialog, "Royal Throne", 43, true, Navy); Bounds(home.dialogTitle.rectTransform, .06f, .58f, .94f, .74f);
            home.dialogCopy = Label("Dialog subtitle", dialog, "Welcome home", 28, false, Navy); Bounds(home.dialogCopy.rectTransform, .08f, .48f, .92f, .58f);
            home.dialogDetail = Label("Dialog detail", dialog, "", 26, false, Hex("294571")); Bounds(home.dialogDetail.rectTransform, .09f, .23f, .91f, .47f);
            home.dialogButton = FramedButton("Back to home", dialog, Hex("53a7ff"), Hex("06449f")); Bounds(home.dialogButton.GetComponent<RectTransform>(), .12f, .07f, .88f, .21f);
            home.dialogButtonLabel = Label("Back label", home.dialogButton.transform, "Back to home", 30, true, Ivory);
            home.icons = new[] { Art("avatar-king"), Art("icon-coin"), Art("icon-heart"), Art("icon-star"), Art("icon-settings"), Art("icon-chest"), Art("icon-story"), Art("icon-trophy"), Art("icon-home"), Art("icon-team"), Art("icon-cards") };
            var settingsGroup = Rect("Audio and motion", dialog); Bounds(settingsGroup, .12f, .24f, .88f, .61f); home.settingsGroup = settingsGroup.gameObject;
            home.settingButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                var button = FramedButton("Setting " + i, settingsGroup, Hex("367eea"), Hex("0640a1")); Bounds(button.GetComponent<RectTransform>(), 0, .68f - i * .34f, 1, 1 - i * .34f);
                Label("Setting label", button.transform, "On", 25, false, Ivory); home.settingButtons[i] = button;
            }
            var grid = Rect("Chapter levels", dialog); Bounds(grid, .10f, .27f, .90f, .61f); home.levelGrid = grid.gameObject; home.gridRect = grid;
            home.levelChoices = new Button[12];
            for (int i = 0; i < 12; i++)
            {
                int col = i % 4, row = i / 4; var choice = FramedButton("Choose level " + i, grid, Hex("5ca7ff"), Hex("1253ba"));
                Bounds(choice.GetComponent<RectTransform>(), col * .25f + .01f, (2 - row) / 3f + .025f, (col + 1) * .25f - .01f, (3 - row) / 3f - .025f);
                Label("Level stars", choice.transform, "1\n0/3", 23, false, Ivory); home.levelChoices[i] = choice;
            }
            home.previousPage = FramedButton("Previous levels", dialog, Blue, Hex("0640a1")); Bounds(home.previousPage.GetComponent<RectTransform>(), .10f, .155f, .28f, .255f); Label("Previous arrow", home.previousPage.transform, "<", 26, false, Ivory);
            home.nextPage = FramedButton("Next levels", dialog, Blue, Hex("0640a1")); Bounds(home.nextPage.GetComponent<RectTransform>(), .72f, .155f, .90f, .255f); Label("Next arrow", home.nextPage.transform, ">", 26, false, Ivory);
            home.pageLabel = Label("Page", dialog, "1 / 9", 23, false, Navy); Bounds(home.pageLabel.rectTransform, .3f, .155f, .7f, .255f);
            home.levelNumbers = Directory.GetFiles("Assets/JellyGarden/Resources/Levels", "*.txt").Select(System.IO.Path.GetFileNameWithoutExtension).Where(s => int.TryParse(s, out _)).Select(int.Parse).OrderBy(n => n).ToArray();
            var menu = UnityEngine.Object.FindAnyObjectByType<MenuManager>(FindObjectsInactive.Include);
            home.globalMenus = menu.GetCanvasGlobal();
            // Keep the board's original camera-space presentation. Home temporarily raises
            // this canvas at runtime so its pre-level panel is visible over the new art.
            var globalCanvas = home.globalMenus.GetComponent<Canvas>(); globalCanvas.renderMode = RenderMode.ScreenSpaceCamera; globalCanvas.sortingOrder = 2;
            var oldMap = GameObject.Find("CanvasMap");
            if (!oldMap) throw new InvalidOperationException("Existing map HUD was not found; cannot safely preserve the life timer.");
            if (!oldMap.TryGetComponent<CanvasGroup>(out var legacyGroup)) legacyGroup = oldMap.AddComponent<CanvasGroup>();
            home.legacyMapUI = legacyGroup;
            home.legacyMapUI.alpha = 0; home.legacyMapUI.interactable = home.legacyMapUI.blocksRaycasts = false;
            home.mapCamera = UnityEngine.Object.FindAnyObjectByType<MapCamera>();
            home.levelGrid.SetActive(false); home.settingsGroup.SetActive(false); home.dialog.SetActive(false);
            Canvas.ForceUpdateCanvases(); home.RefreshProgress(); home.RefreshLayout();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root.gameObject;
            File.WriteAllText("Library/RoyalHome-authoring.txt", "PASS: homepage authored in game scene, " + home.levelNumbers.Length + " levels, live map HUD timer preserved.\n" +
                "Navigation hover scale: " + home.tabButtons[4].GetComponent<RoyalButtonMotion>().hoverScale + "\nOriginal board canvas order: " + globalCanvas.sortingOrder + "\nDialog close offset: " + close.GetComponent<RectTransform>().anchoredPosition);
            Debug.Log("Royal Throne homepage authored and saved. Live progression, level entry, resource HUD and settings connected.");
        }

        public static void RequireCopy()
        {
            if (!Application.dataPath.Replace('\\', '/').EndsWith("/outputs/JellyGarden-Codex/Assets", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Restricted to the independent Codex copy.");
        }
        static void ConfigureArt(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = !path.EndsWith("home-castle.png");
            importer.npotScale = TextureImporterNPOTScale.None; importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = path.EndsWith("home-castle.png") ? 2048 : 1024; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        static Sprite Art(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + name + ".png") ?? throw new FileNotFoundException("Missing home art: " + name);
        static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var color); return color; }
        static RectTransform Rect(string name, Transform parent)
        { var go = new GameObject(name, typeof(RectTransform)); go.layer = 5; var rect = (RectTransform)go.transform; rect.SetParent(parent, false); return rect; }
        static void Bounds(RectTransform rect, float left, float bottom, float right, float top)
        { rect.anchorMin = new Vector2(left, bottom); rect.anchorMax = new Vector2(right, top); rect.offsetMin = rect.offsetMax = Vector2.zero; }
        static void Stretch(RectTransform rect) => Bounds(rect, 0, 0, 1, 1);
        static void Center(RectTransform rect, float width, float height, float x, float y)
        { rect.anchorMin = rect.anchorMax = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height); rect.anchoredPosition = Vector2.zero; }
        static Image Image(string name, Transform parent, Sprite sprite)
        { var image = Rect(name, parent).gameObject.AddComponent<Image>(); image.sprite = sprite; image.preserveAspect = sprite; image.raycastTarget = false; Stretch(image.rectTransform); return image; }
        static RoyalPanelGraphic Surface(string name, Transform parent, Color top, Color bottom, float radius)
        { var rect = Rect(name, parent); var g = rect.gameObject.AddComponent<RoyalPanelGraphic>(); g.topColor = top; g.bottomColor = bottom; g.cornerRadius = radius; g.raycastTarget = false; Stretch(rect); return g; }
        static RectTransform Panel(string name, Transform parent, Color top, Color bottom)
        {
            var outer = Surface(name, parent, Hex("ffe08a"), Hex("b87a24"), 28); outer.raycastTarget = true;
            var inner = Surface("Ivory trim", outer.transform, Hex("fff5bb"), Hex("dda64a"), 25); Stretch(inner.rectTransform); inner.rectTransform.offsetMin = new Vector2(3, 5); inner.rectTransform.offsetMax = new Vector2(-3, -3);
            var face = Surface("Panel face", inner.transform, top, bottom, 22); Stretch(face.rectTransform); face.rectTransform.offsetMin = new Vector2(3, 5); face.rectTransform.offsetMax = new Vector2(-3, -3);
            return outer.rectTransform;
        }
        static Button FramedButton(string name, Transform parent, Color top, Color bottom)
        { var panel = Panel(name, parent, top, bottom); var button = panel.gameObject.AddComponent<Button>(); button.targetGraphic = panel.GetComponent<RoyalPanelGraphic>(); panel.gameObject.AddComponent<RoyalButtonMotion>(); return button; }
        static Button PlainButton(string name, Transform parent)
        { var image = Image(name, parent, null); image.color = Color.clear; image.raycastTarget = true; var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; image.gameObject.AddComponent<RoyalButtonMotion>(); return button; }
        static Button ArtButton(string name, Transform parent, Sprite art)
        { var button = PlainButton(name, parent); var picture = Image("Artwork", button.transform, art); return button; }
        static Button Resource(string name, Transform parent, string icon, float left, float right, out Text value)
        {
            var button = PlainButton(name, parent); Bounds(button.GetComponent<RectTransform>(), left, 0, right, 1);
            var plate = Panel("Ivory balance", button.transform, Hex("fff8e3"), Hex("efdbad")); Bounds(plate, .13f, .20f, 1, .84f);
            plate.GetComponent<RoyalPanelGraphic>().raycastTarget = false;
            var picture = Image(name + " icon", button.transform, Art(icon)); Bounds(picture.rectTransform, -.05f, .08f, .46f, .95f);
            value = Label(name + " balance", button.transform, "0", 32, true, Navy); Bounds(value.rectTransform, .42f, .20f, .96f, .84f); return button;
        }
        static Text Label(string name, Transform parent, string value, int size, bool display, Color color)
        {
            var text = Rect(name, parent).gameObject.AddComponent<Text>(); Stretch(text.rectTransform);
            text.font = display ? displayFont : bodyFont; text.text = value; text.fontSize = size; text.color = color; text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false; text.resizeTextForBestFit = true; text.resizeTextMinSize = Mathf.Max(18, size - 8); text.resizeTextMaxSize = size;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; return text;
        }
        static void Shadow(Text text, Vector2 offset)
        { var shadow = text.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(.18f, .09f, .035f, .85f); shadow.effectDistance = offset; }
    }
}
