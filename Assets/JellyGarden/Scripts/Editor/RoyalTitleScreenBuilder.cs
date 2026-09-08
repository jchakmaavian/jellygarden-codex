using System;
using System.IO;
using RoyalThrone.Presentation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoyalThrone.Editor
{
    // Explicit authoring command. It writes real scene objects; it never runs automatically on import.
    public static class RoyalTitleScreenBuilder
    {
        const string MainScene = "Assets/JellyGarden/Scenes/main.unity";
        const string ArtPath = "Assets/JellyGarden/Textures/menu/main_menu/royal_castle_twilight.png";
        const string LogoPath = "Assets/JellyGarden/Textures/menu/main_menu/logo.png";
        static Font displayFont;
        static Font bodyFont;

        [MenuItem("Tools/Royal Throne/Build splash and loading screens")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before authoring.");
            if (!Application.dataPath.Replace('\\', '/').EndsWith("/outputs/JellyGarden-Codex/Assets", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This authoring command is restricted to the independent Codex copy.");
            var current = SceneManager.GetActiveScene();
            if (current.isDirty) throw new InvalidOperationException("Save the current scene before rebuilding its title UI.");
            if (!File.Exists(ArtPath)) throw new FileNotFoundException("Castle illustration is not installed.", ArtPath);
            var scene = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
            ConfigureArt();
            var art = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
            displayFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/JellyGarden/Fonts/BorisBlackBloxx.ttf");
            bodyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (!art || !logo || !displayFont || !bodyFont) throw new InvalidOperationException("Required title assets are missing.");

            // The main scene contains only its opening UI, camera, input and sound roots.
            // Keep the existing camera/input/audio and replace only this scene's Canvas.
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.GetComponent<Canvas>()) UnityEngine.Object.DestroyImmediate(root);

            var rootRect = Rect("Royal Throne UI", null);
            SceneManager.MoveGameObjectToScene(rootRect.gameObject, scene);
            var canvas = rootRect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = rootRect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            rootRect.gameObject.AddComponent<GraphicRaycaster>();
            var whole = rootRect.gameObject.AddComponent<CanvasGroup>();

            var backgroundFrame = Rect("Castle background", rootRect);
            Stretch(backgroundFrame);
            var background = Picture("Twilight castle", backgroundFrame, art);
            var aspect = background.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = art.rect.width / art.rect.height;
            var shade = Surface("Lower vignette", rootRect, new Vector2(720, 480), new Vector2(.5f, 0), Vector2.zero,
                new Color(.055f, .027f, .13f, 0), new Color(.035f, .018f, .09f, .78f), 0);
            shade.rectTransform.anchorMin = Vector2.zero;
            shade.rectTransform.anchorMax = new Vector2(1, .4f);
            shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;

            var motesRoot = Rect("Floating golden lights", rootRect);
            Stretch(motesRoot);
            var motes = new RectTransform[12];
            for (int i = 0; i < motes.Length; i++)
            {
                float size = i % 3 == 0 ? 7 : 4;
                var light = Surface("Light " + (i + 1), motesRoot, Vector2.one * size, new Vector2(.1f + i % 5 * .18f, .15f + i * .065f),
                    Vector2.zero, new Color(1, .95f, .65f, .7f), new Color(1, .67f, .25f, .15f), size);
                motes[i] = light.rectTransform;
            }

            var titleRoot = Rect("Title content", rootRect);
            Stretch(titleRoot);
            var titleGroup = titleRoot.gameObject.AddComponent<CanvasGroup>();
            var safe = Rect("Safe area", titleRoot);
            Stretch(safe);
            var titleLogo = Picture("Logo", safe, logo);
            Place(titleLogo.rectTransform, Vector2.one * 550, new Vector2(.5f, 1), new Vector2(0, -28));
            titleLogo.rectTransform.pivot = new Vector2(.5f, 1);

            var play = Rect("Play", safe);
            Place(play, new Vector2(440, 112), new Vector2(.5f, .135f), Vector2.zero);
            var shadow = Surface("Soft shadow", play, Vector2.zero, Vector2.zero, Vector2.zero,
                new Color(.025f, .01f, .06f, .65f), new Color(.025f, .01f, .06f, .2f), 39);
            Inset(shadow.rectTransform, -6, -10, -6, 2);
            var edge = Surface("Gold edge", play, Vector2.zero, Vector2.zero, Vector2.zero, Hex("B87826"), Hex("784016"), 35);
            Stretch(edge.rectTransform);
            var rim = Surface("Polished gold rim", play, Vector2.zero, Vector2.zero, Vector2.zero, Hex("FFF0A7"), Hex("EFB746"), 32);
            Inset(rim.rectTransform, 3, 8, 3, 2);
            var face = Surface("Emerald face", play, Vector2.zero, Vector2.zero, Vector2.zero, Hex("6AD94A"), Hex("108B47"), 27);
            Inset(face.rectTransform, 8, 14, 8, 7);
            face.raycastTarget = true;
            var inner = Surface("Upper gloss", play, Vector2.zero, Vector2.zero, Vector2.zero,
                new Color(.86f, 1, .72f, .45f), new Color(.65f, 1, .52f, 0), 20);
            inner.rectTransform.anchorMin = new Vector2(0, .54f);
            inner.rectTransform.anchorMax = Vector2.one;
            inner.rectTransform.offsetMin = new Vector2(15, 0);
            inner.rectTransform.offsetMax = new Vector2(-15, -13);
            var playLabel = Label("Label", play, "PLAY", 51, true, Hex("FFFBE6"));
            Stretch(playLabel.rectTransform);
            playLabel.rectTransform.anchoredPosition = new Vector2(0, 5);
            var textShadow = playLabel.gameObject.AddComponent<Shadow>();
            textShadow.effectColor = new Color(.015f, .2f, .075f, .8f);
            textShadow.effectDistance = new Vector2(0, -3);
            var button = play.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1, 1, .88f);
            colors.selectedColor = new Color(1, 1, .92f);
            colors.pressedColor = new Color(.77f, .88f, .73f);
            colors.disabledColor = new Color(.62f, .72f, .61f);
            colors.fadeDuration = .08f;
            button.colors = colors;
            play.gameObject.AddComponent<RoyalButtonMotion>();
            var error = Label("Load error", safe, "", 22, false, Hex("FFE6BD"));
            Place(error.rectTransform, new Vector2(620, 64), new Vector2(.5f, .058f), Vector2.zero);

            var loadingRoot = Rect("Loading", rootRect);
            Stretch(loadingRoot);
            var loadingGroup = loadingRoot.gameObject.AddComponent<CanvasGroup>();
            loadingGroup.alpha = 0;
            var veil = Surface("Twilight veil", loadingRoot, Vector2.zero, Vector2.zero, Vector2.zero,
                new Color(.055f, .025f, .14f, .64f), new Color(.045f, .018f, .10f, .88f), 0);
            Stretch(veil.rectTransform);
            veil.raycastTarget = true;
            var loadingSafe = Rect("Safe area", loadingRoot);
            Stretch(loadingSafe);
            var loadingLogo = Picture("Royal Throne logo", loadingSafe, logo);
            Place(loadingLogo.rectTransform, Vector2.one * 440, new Vector2(.5f, .72f), Vector2.zero);
            var card = Rect("Loading card", loadingSafe);
            Place(card, new Vector2(570, 236), new Vector2(.5f, .225f), Vector2.zero);
            var cardBorder = Surface("Gold hairline", card, Vector2.zero, Vector2.zero, Vector2.zero,
                new Color(.95f, .78f, .44f, .65f), new Color(.48f, .3f, .2f, .35f), 30);
            Stretch(cardBorder.rectTransform);
            var cardFace = Surface("Purple panel", card, Vector2.zero, Vector2.zero, Vector2.zero, Hex("36265A"), Hex("19122E"), 28);
            Inset(cardFace.rectTransform, 2, 2, 2, 2);
            var jewels = new RectTransform[3];
            for (int i = 0; i < 3; i++)
            {
                var jewel = Surface("Animated jewel " + (i + 1), card, Vector2.one * 22, new Vector2(.5f, .5f),
                    new Vector2((i - 1) * 42, 113), Hex("FFF1A2"), Hex("DA8B2A"), 0);
                jewel.diamond = true;
                jewels[i] = jewel.rectTransform;
            }
            var loadingLabel = Label("Heading", card, "Loading your kingdom", 28, true, Hex("FFF3D0"));
            Place(loadingLabel.rectTransform, new Vector2(510, 48), new Vector2(.5f, .5f), new Vector2(0, 58));
            var track = Surface("Progress gold rim", card, Vector2.zero, Vector2.zero, Vector2.zero, Hex("E9C672"), Hex("A47530"), 18);
            track.rectTransform.anchorMin = new Vector2(0, .5f);
            track.rectTransform.anchorMax = new Vector2(1, .5f);
            track.rectTransform.offsetMin = new Vector2(38, -17);
            track.rectTransform.offsetMax = new Vector2(-38, 17);
            var inset = Surface("Progress track", track.transform, Vector2.zero, Vector2.zero, Vector2.zero, Hex("100C21"), Hex("2A193E"), 15);
            Inset(inset.rectTransform, 3, 3, 3, 3);
            var fillContainer = Rect("Progress fill area", inset.transform);
            Inset(fillContainer, 3, 3, 3, 3);
            var fill = Surface("Progress fill", fillContainer, Vector2.zero, Vector2.zero, Vector2.zero, Hex("FFF2A2"), Hex("ECA72C"), 10);
            Stretch(fill.rectTransform);
            fill.rectTransform.anchorMax = new Vector2(0, 1);
            fill.gameObject.AddComponent<RectMask2D>();
            var shine = Surface("Moving glint", fill.transform, new Vector2(28, 0), new Vector2(0, .5f), Vector2.zero,
                new Color(1, 1, .92f, .55f), new Color(1, 1, .92f, .06f), 12);
            shine.rectTransform.anchorMin = Vector2.zero;
            shine.rectTransform.anchorMax = new Vector2(0, 1);
            shine.rectTransform.sizeDelta = new Vector2(28, 0);
            var status = Label("Status", card, "Preparing your game...", 24, false, Hex("D6C9E7"));
            Place(status.rectTransform, new Vector2(500, 46), new Vector2(.5f, .5f), new Vector2(0, -62));

            var controller = rootRect.gameObject.AddComponent<RoyalTitleScreen>();
            controller.safeArea = safe;
            controller.loadingSafeArea = loadingSafe;
            controller.titleLogo = titleLogo.rectTransform;
            controller.loadingLogo = loadingLogo.rectTransform;
            controller.playRect = play;
            controller.loadingCard = card;
            controller.wholeScreen = whole;
            controller.titleGroup = titleGroup;
            controller.loadingGroup = loadingGroup;
            controller.playButton = button;
            controller.errorLabel = error;
            controller.progressFill = fill.rectTransform;
            controller.progressShine = shine.rectTransform;
            controller.jewels = jewels;
            controller.loadingLabel = loadingLabel;
            controller.loadingStatus = status;
            controller.motes = motes;
            controller.gameScene = "game";
            loadingRoot.gameObject.SetActive(false);
            UnityEventTools.AddPersistentListener(button.onClick, controller.BeginPlay);
            var eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem) eventSystem.firstSelectedGameObject = button.gameObject;
            Canvas.ForceUpdateCanvases();
            controller.RefreshLayout();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MainScene);
            Selection.activeGameObject = rootRect.gameObject;
            Debug.Log("Royal Throne: splash and animated loading screens authored and saved.");
            Validate();
        }

        [MenuItem("Tools/Royal Throne/Validate title screen")]
        public static void Validate()
        {
            var controller = UnityEngine.Object.FindAnyObjectByType<RoyalTitleScreen>();
            if (!controller) throw new InvalidOperationException("Open the main scene to validate its title UI.");
            if (!controller.playButton || !controller.progressFill || !controller.loadingGroup || controller.jewels.Length != 3)
                throw new InvalidOperationException("Title UI bindings are incomplete.");
            foreach (var graphic in controller.GetComponentsInChildren<Graphic>(true))
                if (!graphic.GetComponent<CanvasRenderer>())
                    throw new InvalidOperationException("Missing CanvasRenderer on " + graphic.name);
            var image = controller.titleLogo.GetComponent<Image>();
            if (AssetDatabase.GetAssetPath(image.sprite) != LogoPath || !image.preserveAspect)
                throw new InvalidOperationException("Approved logo reference or aspect was changed.");
            if (controller.playButton.onClick.GetPersistentEventCount() != 1 ||
                controller.playButton.onClick.GetPersistentMethodName(0) != nameof(RoyalTitleScreen.BeginPlay))
                throw new InvalidOperationException("Play must have exactly one async-loading callback.");
            foreach (var graphic in controller.GetComponentsInChildren<Image>(true))
                if (graphic.sprite && AssetDatabase.GetAssetPath(graphic.sprite).EndsWith("/loading.png"))
                    throw new InvalidOperationException("The flattened JellyGarden loading art is still referenced.");
            bool hasGame = false;
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.enabled && scene.path == "Assets/JellyGarden/Scenes/game.unity") hasGame = true;
            if (!hasGame) throw new InvalidOperationException("The game scene is absent from the enabled scene list.");
            Debug.Log("Royal Throne title validation PASS: approved logo, clean loading art, one Play callback, complete bindings and destination scene.");
        }

        [MenuItem("Tools/Royal Throne/Preview loading animation (Play Mode) _F7")]
        public static void PreviewLoading()
        {
            if (!EditorApplication.isPlaying) { Debug.Log("Enter Play Mode in main to preview the loading animation."); return; }
            UnityEngine.Object.FindAnyObjectByType<RoyalTitleScreen>()?.PreviewLoadingAnimation();
        }

        static void ConfigureArt()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(ArtPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }

        static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var color); return color; }
        static RectTransform Rect(string name, Transform parent)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.layer = 5;
            var rect = (RectTransform)item.transform;
            rect.SetParent(parent, false);
            return rect;
        }
        static void Place(RectTransform rect, Vector2 size, Vector2 anchor, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
        static void Stretch(RectTransform rect) => Inset(rect, 0, 0, 0, 0);
        static void Inset(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
        static Image Picture(string name, Transform parent, Sprite sprite)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }
        static RoyalPanelGraphic Surface(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 position, Color top, Color bottom, float radius)
        {
            var surface = Rect(name, parent);
            surface.gameObject.AddComponent<CanvasRenderer>();
            var graphic = surface.gameObject.AddComponent<RoyalPanelGraphic>();
            Place(graphic.rectTransform, size, anchor, position);
            graphic.topColor = top;
            graphic.bottomColor = bottom;
            graphic.cornerRadius = radius;
            graphic.raycastTarget = false;
            return graphic;
        }
        static Text Label(string name, Transform parent, string value, int size, bool display, Color color)
        {
            var text = Rect(name, parent).gameObject.AddComponent<Text>();
            text.font = display ? displayFont : bodyFont;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
    }
}
