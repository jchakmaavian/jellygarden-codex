using System;
using System.IO;
using System.Linq;
using RoyalThrone.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoyalThrone.Editor
{
    // Explicit authoring command. It writes real scene objects; it never runs automatically on import.
    // It replaces the main scene's startup Canvas with the Splash -> Loading -> Home flow (RoyalTitleScreen).
    public static class RoyalTitleScreenBuilder
    {
        const string MainScene = "Assets/JellyGarden/Scenes/main.unity";
        const string Folder = "Assets/JellyGarden/Textures/menu/startup/";
        const string SplashJson = Folder + "Splash.json";
        const string SplashBackground = Folder + "splash_background.png";
        const string LoadingMobile = Folder + "Loading_mobile.webp";
        const string LoadingTablet = Folder + "Loading_Tab.webp";
        const string HaloPath = "Assets/JellyGarden/Textures/menu/main_menu/wheel.png";
        const string LogoPath = "Assets/JellyGarden/Textures/menu/main_menu/logo.png";
        const string FontPath = "Assets/JellyGarden/Resources/RoyalGameplay/Display.ttf";
        // The reference's corner colour: the first frame and anything outside the gradient are never black or white.
        static readonly Color SplashEdge = new Color32(0x27, 0x0a, 0x88, 0xff);

        [MenuItem("Tools/Royal Throne/Build splash and loading screens")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before authoring.");
            if (!Application.dataPath.Replace('\\', '/').EndsWith("/outputs/JellyGarden-Codex/Assets", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This authoring command is restricted to the independent Codex copy.");
            var current = SceneManager.GetActiveScene();
            if (current.isDirty) throw new InvalidOperationException("Save the current scene before rebuilding its title UI.");
            foreach (var path in new[] { SplashJson, SplashBackground, LoadingMobile, LoadingTablet })
                if (!File.Exists(path)) throw new FileNotFoundException("Startup asset is not installed.", path);
            ConfigureArt(SplashBackground, 1024);
            ConfigureWebP(LoadingMobile); ConfigureWebP(LoadingTablet);
            var scene = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(SplashJson);
            var gradient = AssetDatabase.LoadAssetAtPath<Sprite>(SplashBackground);
            var mobile = AssetDatabase.LoadAllAssetsAtPath(LoadingMobile).OfType<Sprite>().FirstOrDefault();
            var tablet = AssetDatabase.LoadAllAssetsAtPath(LoadingTablet).OfType<Sprite>().FirstOrDefault();
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
            var haloArt = AssetDatabase.LoadAssetAtPath<Sprite>(HaloPath);
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (!json || !gradient || !mobile || !tablet || !logo || !haloArt || !font) throw new InvalidOperationException("Required startup assets are missing.");

            // The main scene holds only its startup UI, camera, input and sound roots. Keep camera/input/audio and replace
            // only the Canvas: the old castle title, its Play button and the old loading card go with it.
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.GetComponent<Canvas>()) UnityEngine.Object.DestroyImmediate(root);
            foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
                if (camera.gameObject.scene == scene) { camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = SplashEdge; }

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

            // Splash: the reference gradient covering the screen, and Splash.json on it.
            var splash = Rect("Splash", rootRect); Stretch(splash);
            var splashGroup = splash.gameObject.AddComponent<CanvasGroup>();
            var splashFrame = Rect("Splash background", splash); Stretch(splashFrame);
            var splashImage = Picture("Reference gradient", splashFrame, gradient, true);
            var splashFit = splashImage.gameObject.AddComponent<AspectRatioFitter>();
            splashFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            splashFit.aspectRatio = gradient.rect.width / gradient.rect.height;
            var animation = Rect("Splash animation (Splash.json)", splash);
            var player = animation.gameObject.AddComponent<RoyalLottiePlayer>();
            player.animationJson = json;

            // Loading: the illustration covering the screen, the logo at the top and the text at the bottom.
            var loading = Rect("Loading", rootRect); Stretch(loading);
            var loadingGroup = loading.gameObject.AddComponent<CanvasGroup>();
            var loadingFrame = Rect("Loading background", loading); Stretch(loadingFrame);
            var loadingImage = Picture("Loading illustration", loadingFrame, mobile, true);
            var loadingFit = loadingImage.gameObject.AddComponent<AspectRatioFitter>();
            loadingFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            loadingFit.aspectRatio = mobile.rect.width / mobile.rect.height;
            var safe = Rect("Safe area", loading); Stretch(safe);
            var halo = Rect("Rotating logo halo", safe);
            var haloGraphic = halo.gameObject.AddComponent<RoyalLogoHalo>();
            haloGraphic.sprite = haloArt; haloGraphic.color = new Color(1, .99f, .96f, .3f); haloGraphic.raycastTarget = false; haloGraphic.SetAllDirty();
            var logoImage = Picture("Royal Throne logo", safe, logo, false);
            var label = Rect("Loading text", safe).gameObject.AddComponent<Text>();
            label.font = font; label.fontSize = 46; label.text = "Loading..."; label.alignment = TextAnchor.MiddleCenter; label.color = new Color(1, .98f, .92f);
            label.supportRichText = true; label.raycastTarget = false; label.horizontalOverflow = HorizontalWrapMode.Overflow; label.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = label.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(.06f, .10f, .30f, .85f); outline.effectDistance = new Vector2(2.2f, -2.2f);
            var shadow = label.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(.03f, .06f, .2f, .55f); shadow.effectDistance = new Vector2(0, -4);

            var controller = rootRect.gameObject.AddComponent<RoyalTitleScreen>();
            controller.splashGroup = splashGroup; controller.splashBackground = splashFit; controller.splashAnimation = player;
            controller.loadingGroup = loadingGroup; controller.loadingBackground = loadingImage; controller.loadingFitter = loadingFit;
            controller.loadingMobile = mobile; controller.loadingTablet = tablet;
            controller.loadingSafeArea = safe; controller.loadingLogo = logoImage.rectTransform; controller.loadingHalo = halo; controller.loadingLabel = label;
            controller.wholeScreen = whole; controller.gameScene = "game";
            loading.gameObject.SetActive(false);
            var eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem) eventSystem.firstSelectedGameObject = null;

            // Unity's own splash (the grey "Made with Unity" screen) came before the old title; the game's splash replaces it.
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.backgroundColor = SplashEdge;

            Canvas.ForceUpdateCanvases();
            controller.RefreshLayout();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MainScene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = rootRect.gameObject;
            Debug.Log("Royal Throne: startup Splash and Loading screens authored and saved.");
            Validate();
        }

        [MenuItem("Tools/Royal Throne/Validate title screen")]
        public static void Validate()
        {
            var controller = UnityEngine.Object.FindAnyObjectByType<RoyalTitleScreen>(FindObjectsInactive.Include);
            if (!controller) throw new InvalidOperationException("Open the main scene to validate its startup UI.");
            if (!controller.splashGroup || !controller.splashBackground || !controller.splashAnimation || !controller.loadingGroup || !controller.loadingBackground ||
                !controller.loadingFitter || !controller.loadingMobile || !controller.loadingTablet || !controller.loadingSafeArea || !controller.loadingLogo ||
                !controller.loadingHalo || !controller.loadingLabel || !controller.wholeScreen)
                throw new InvalidOperationException("Startup UI bindings are incomplete.");
            if (AssetDatabase.GetAssetPath(controller.splashAnimation.animationJson) != SplashJson) throw new InvalidOperationException("The splash must play Splash.json.");
            if (AssetDatabase.GetAssetPath(controller.loadingMobile) != LoadingMobile || AssetDatabase.GetAssetPath(controller.loadingTablet) != LoadingTablet)
                throw new InvalidOperationException("The loading illustrations are not the phone and tablet art.");
            var logo = controller.loadingLogo.GetComponent<Image>();
            if (!logo || AssetDatabase.GetAssetPath(logo.sprite) != LogoPath || !logo.preserveAspect) throw new InvalidOperationException("Approved logo reference or aspect was changed.");
            var halo = controller.loadingHalo.GetComponent<RoyalLogoHalo>();
            if (!halo || AssetDatabase.GetAssetPath(halo.sprite) != HaloPath || halo.raycastTarget || halo.rotationSeconds != 6)
                throw new InvalidOperationException("The approved six-second logo halo is not configured.");
            if (controller.GetComponentsInChildren<Button>(true).Length > 0 || controller.GetComponentsInChildren<Selectable>(true).Length > 0)
                throw new InvalidOperationException("The startup screens must have no buttons.");
            if (controller.GetComponentsInChildren<EventSystem>(true).Length > 0) throw new InvalidOperationException("The persistent startup Canvas must not carry an EventSystem.");
            foreach (var image in controller.GetComponentsInChildren<Image>(true))
            {
                string path = AssetDatabase.GetAssetPath(image.sprite);
                if (path.EndsWith("/loading.png") || path.EndsWith("royal_castle_blue.png") || path.EndsWith("royal_play_crown.png"))
                    throw new InvalidOperationException("Old title or loading art is still referenced: " + path);
            }
            foreach (var text in controller.GetComponentsInChildren<Text>(true))
                if (text.text.IndexOf("Match", StringComparison.OrdinalIgnoreCase) >= 0) throw new InvalidOperationException("Startup text must not name another game.");
            foreach (var graphic in controller.GetComponentsInChildren<Graphic>(true))
                if (!graphic.GetComponent<CanvasRenderer>()) throw new InvalidOperationException("Missing CanvasRenderer on " + graphic.name);
            if (!controller.loadingBackground.raycastTarget) throw new InvalidOperationException("The loading screen must block input to the scene beneath.");
            bool hasGame = false;
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.enabled && scene.path == "Assets/JellyGarden/Scenes/game.unity") hasGame = true;
            if (!hasGame) throw new InvalidOperationException("The game scene is absent from the enabled scene list.");
            if (EditorBuildSettings.scenes.Length == 0 || EditorBuildSettings.scenes[0].path != MainScene) throw new InvalidOperationException("The main scene must be the first scene in the build.");
            if (PlayerSettings.SplashScreen.show) throw new InvalidOperationException("Unity's own splash would show before the game's splash.");
            Debug.Log("Royal Throne startup validation PASS: Splash.json splash, phone and tablet loading art, approved logo and halo, no buttons, no EventSystem, destination scene.");
        }

        static void ConfigureArt(string path, int maxSize)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (!importer) throw new FileNotFoundException("Required artwork has not been imported.", path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = maxSize;
            // A smooth gradient: uncompressed, so block compression cannot band it.
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }

        static void ConfigureWebP(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as RoyalWebPImporter;
            if (!importer) throw new InvalidOperationException("The WebP importer is not handling " + path);
            if (importer.astcOnMobile && !importer.bleedTransparentEdges) return;
            // Full-screen art: ASTC 6x6 on phones like the area art, instead of desktop BC3.
            importer.astcOnMobile = true; importer.bleedTransparentEdges = false;
            EditorUtility.SetDirty(importer); importer.SaveAndReimport();
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.layer = 5;
            var rect = (RectTransform)item.transform;
            rect.SetParent(parent, false);
            return rect;
        }
        static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        static Image Picture(string name, Transform parent, Sprite sprite, bool blocksInput)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = blocksInput;
            return image;
        }
    }
}
