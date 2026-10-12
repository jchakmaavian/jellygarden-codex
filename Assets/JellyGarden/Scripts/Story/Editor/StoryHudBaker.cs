// Bakes the level HUD into game.unity, so nothing builds it at runtime.
//
// Retired by the royal-throne port: Bake now refuses (see RetiredReason), and
// Verify still checks the strip the scene already holds.
//
// The HUD used to be constructed every time a level opened, which is what made
// it race the kit's own header: whichever ran first won, and when Attach did
// not run the player saw Jelly Garden's score bar. A header that is IN the
// scene cannot lose that race, because there is no race — the scene already
// holds the only header there is.
//
// The design is not retyped here. StoryLevelHud.Build is still the single
// definition of the strip, its metrics and its colours; this runs it ONCE, in
// the editor, against the real scene and saves the result. Change the design in
// that file and re-bake.
//
// The one thing that cannot survive the trip is a procedural sprite.
// StoryUi.RoundedSprite and StoryUi.BannerSprite draw a Texture2D in memory,
// and a scene can only reference a sprite that is an ASSET with a guid. So each
// one is written out as a PNG first — border included, read off the generated
// sprite rather than retyped — and every Image the builder made is repointed at
// the saved copy before the scene is written.

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story.Editor
{
    public static class StoryHudBaker
    {
        private const string ScenePath = "Assets/JellyGarden/Scenes/game.unity";
        private const string StripPath = "Level/Canvas/Panel/Panel";
        private const string SpriteDir = "Assets/JellyGarden/Resources/Story/UI";

        /// <summary>The kit children that stay: the icons the board animates to, and Pause.</summary>
        private static readonly HashSet<string> Keep = new HashSet<string> { "Targets", "Pause" };

        /// <summary>
        /// Retired by the royal-throne port: RoyalGameplayPresentation draws the
        /// level HUD and hides Canvas/Panel, so a re-bake would only rewrite a
        /// hidden strip in game.unity (docs/port/RETIRE.md). Run is kept until
        /// the strip itself is removed in Task 4.6.
        /// </summary>
        public const string RetiredReason =
            "The level HUD is RoyalGameplayPresentation's now; baking the StoryLevelHud strip into " +
            ScenePath + " is retired (docs/port/RETIRE.md).";

        [MenuItem("JellyGarden/Story/Bake Level HUD into the scene")]
        public static void Bake()
        {
            Debug.LogError("[Story] " + RetiredReason);
        }

        public static void BakeHeadless()
        {
            Debug.LogError("[Story] " + RetiredReason);
            EditorApplication.Exit(1);
        }

        /// <summary>
        /// Opens the baked scene, binds the HUD the way a level does, pushes two
        /// targets through it and reports what came out.
        ///
        /// This is the check the screen tests cannot make: they call
        /// StoryLevelHud.Build and drive the hierarchy it returns, so a binder
        /// pointed at names the builder no longer makes passes them and still
        /// ships an empty Targets panel. This drives the SCENE.
        /// </summary>
        [MenuItem("JellyGarden/Story/Verify the baked HUD")]
        public static void Verify()
        {
            List<string> problems = Check();
            for (int i = 0; i < problems.Count; i++) Debug.LogError("[Story] " + problems[i]);
            if (problems.Count == 0) Debug.Log("[Story] baked HUD verified.");
        }

        public static void VerifyHeadless()
        {
            List<string> problems = Check();
            for (int i = 0; i < problems.Count; i++) Debug.LogError("[Story] " + problems[i]);
            EditorApplication.Exit(problems.Count == 0 ? 0 : 1);
        }

        private static List<string> Check()
        {
            var problems = new List<string>();
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(ScenePath);

            Transform strip = FindStrip(scene);
            if (strip == null)
            {
                problems.Add("VERIFY: no " + StripPath + " in the scene");
                return problems;
            }

            StoryLevelHud hud = strip.GetComponentInChildren<StoryLevelHud>(true);
            if (hud == null)
            {
                problems.Add("VERIFY: the scene's strip carries no StoryLevelHud — re-bake");
                return problems;
            }

            global::System.Func<StoryLevelHud.HudModel> previous = StoryLevelHud.Source;
            try
            {
                var model = new StoryLevelHud.HudModel
                {
                    LimitTitle = "Moves", LimitValue = "17", LimitLeft = 17
                };
                model.Targets.Add(new StoryLevelHud.HudTarget { Remaining = 100 });
                model.Targets.Add(new StoryLevelHud.HudTarget { Remaining = 4 });
                StoryLevelHud.Source = () => model;

                hud.EnsureBound();
                hud.Refresh();

                if (hud.VisibleTargetCount != 2)
                    problems.Add("VERIFY: the Targets panel drew " + hud.VisibleTargetCount +
                                 " chip(s) for a 2-target level — the panel comes up empty");

                List<string> texts = hud.TargetTexts();
                string joined = texts.Count == 0 ? "(none)" : string.Join(", ", texts.ToArray());
                if (!texts.Contains("100") || !texts.Contains("4"))
                    problems.Add("VERIFY: the chips read [" + joined + "], wanted 100 and 4");

                if (hud.LimitText != "17")
                    problems.Add("VERIFY: Moves reads '" + hud.LimitText + "', wanted 17 — " +
                                 "the number is not bound to the board");

                Debug.Log("[Story] baked HUD: " + hud.VisibleTargetCount + " chip(s) [" +
                          joined + "], " + hud.LimitTitleText + " " + hud.LimitText + ".");
            }
            finally
            {
                StoryLevelHud.Source = previous;
            }

            return problems;
        }

        private static List<string> Run()
        {
            var problems = new List<string>();

            // The sprites have to exist as assets BEFORE the scene references
            // them, or the scene saves a null and the strip bakes flat.
            // Only the sprites the SCENE serialises. The target chips are built
            // per level — a level carries one to four of them — so their pill
            // sprite stays procedural and never needs an asset.
            var saved = new Dictionary<Sprite, Sprite>();
            SaveSprite(StoryUi.CircleSprite, "hud_circle", saved, problems);
            if (problems.Count > 0) return problems;

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(ScenePath);
            Transform strip = FindStrip(scene);
            if (strip == null)
            {
                problems.Add("could not find " + StripPath + " in " + ScenePath);
                return problems;
            }

            StripTheKitHeader(strip);

            // Anything baked by an earlier run goes first, so re-baking is not
            // additive and the scene never carries two strips.
            Transform previous = strip.Find(StoryLevelHud.HostName);
            if (previous != null)
            {
                // A previous bake adopted the live TargetsGroup. Rescue it
                // before replacing the art or collection flights lose their
                // destination after the second bake.
                Transform targets = previous.Find("Targets");
                if (targets != null) targets.SetParent(strip, false);
                Object.DestroyImmediate(previous.gameObject);
            }

            StoryLevelHud hud = StoryLevelHud.Build(strip, ScaleOf(strip));
            Repoint(hud.transform, saved);
            AdoptKitParts(strip, hud);
            FillTheStrip(strip);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Story] HUD baked: " + CountImages(hud.transform) + " image(s), " +
                      saved.Count + " sprite(s) saved as assets, kit chrome removed from the scene.");
            return problems;
        }

        /// <summary>
        /// Writes one generated sprite out as a PNG asset and imports it with the
        /// SAME 9-slice border the generated one carried, read off the sprite
        /// rather than retyped — a border typed twice is a border that drifts.
        /// </summary>
        private static void SaveSprite(Sprite source, string name,
            Dictionary<Sprite, Sprite> saved, List<string> problems)
        {
            if (source == null)
            {
                problems.Add("sprite '" + name + "' generated as null");
                return;
            }

            Directory.CreateDirectory(SpriteDir);
            string path = SpriteDir + "/" + name + ".png";

            // Encoded straight off the generated texture. It was made in script
            // with SetPixels32, so it is already CPU-readable — and the
            // RenderTexture round trip this used to do came back with a flat
            // alpha and a gamma-shifted white, which baked square grey cards
            // where the design draws rounded cream ones.
            File.WriteAllBytes(path, ((Texture2D)source.texture).EncodeToPNG());

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                problems.Add("could not import " + path);
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = source.border;
            importer.spritePixelsPerUnit = source.pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            // A 82x82 mask whose whole job is a clean rounded edge: block
            // compression puts stair-steps on exactly that.
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            var asset = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (asset == null) problems.Add("saved " + path + " but could not load it back");
            else saved[source] = asset;
        }

        /// <summary>
        /// Deletes the kit's header content from the SCENE, permanently.
        ///
        /// The runtime already destroyed every one of these on the way in, so
        /// nothing is lost that the player ever saw — it is the same removal,
        /// done once in the editor instead of on every level load. Two children
        /// stay: `Targets`, because LevelManager flies matched pieces to the
        /// icons TargetsGroup makes and TargetsGroup kills them in OnDisable,
        /// and `Pause`, which is still the kit's button running the kit's action.
        /// </summary>
        private static void StripTheKitHeader(Transform strip)
        {
            var doomed = new List<Transform>();
            foreach (Transform child in strip)
            {
                if (child == null) continue;
                if (Keep.Contains(child.name)) continue;
                if (child.name == StoryLevelHud.HostName) continue;
                doomed.Add(child);
            }

            var removed = new List<string>();
            var switchedOff = new List<string>();
            for (int i = 0; i < doomed.Count; i++)
            {
                // A prefab instance is NOT destroyed: check_vfx_intact counts the
                // scene's PrefabInstance components and a lost one is a visual
                // regression, which is the project's one hard rule. TargetBlocks
                // is the header's only one. Switched off it draws nothing and
                // runs nothing — the same thing the player saw when the runtime
                // destroyed it — while the scene keeps the asset link.
                if (PrefabUtility.IsPartOfPrefabInstance(doomed[i].gameObject))
                {
                    doomed[i].gameObject.SetActive(false);
                    switchedOff.Add(doomed[i].name);
                    continue;
                }

                removed.Add(doomed[i].name);
                Object.DestroyImmediate(doomed[i].gameObject);
            }

            if (removed.Count > 0)
                Debug.Log("[Story] HUD bake: removed the kit's header from the scene (" +
                          string.Join(", ", removed.ToArray()) + ").");
            if (switchedOff.Count > 0)
                Debug.Log("[Story] HUD bake: switched off, not destroyed, because they are " +
                          "prefab instances (" + string.Join(", ", switchedOff.ToArray()) + ").");
        }

        /// <summary>
        /// Does at bake time what the runtime used to do to the two kit children
        /// that survive.
        ///
        /// Both have to MOVE, and the reason is the stretch below. The strip is
        /// resized from the kit's top band to the whole panel, and anything left
        /// anchored inside it moves with it — which is how the kit's target
        /// icons ended up floating over the middle of the board, drawing a
        /// shield, a book and a gem across the gems.
        ///
        ///   Targets — reparented into the HUD and made invisible with a
        ///   CanvasGroup. It cannot be destroyed: LevelManager flies every
        ///   matched piece to the icons TargetsGroup makes, and TargetsGroup
        ///   destroys them in OnDisable, so it stays as an animation anchor that
        ///   StoryLevelHud.AimKitIconsAtThisHud parks on the visible chips.
        ///
        ///   Pause — moved to the empty middle slot of the booster row, which is
        ///   where Royal Match keeps its settings button. Still the kit's button
        ///   running the kit's action.
        ///
        /// The kit's "Level" plate goes with it, for the same reason the runtime
        /// took it: the pause button now sits on top of it.
        /// </summary>
        private static void AdoptKitParts(Transform strip, StoryLevelHud hud)
        {
            Transform targets = strip.Find("Targets");
            if (targets != null)
            {
                targets.SetParent(hud.transform, false);
                var group = targets.GetComponent<CanvasGroup>();
                if (group == null) group = targets.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }

            hud.MovePauseOutOfTheHudsWay(strip);

            RectTransform row = StoryLevelHud.FindBoosterRow(strip);
            if (row == null) return;

            var children = new List<Transform>();
            foreach (Transform child in row) children.Add(child);

            // FindBoosterRow falls back to the whole bottom panel when it cannot
            // find the row, and everything on the panel would then look like the
            // kit's level plate. Only a row that actually holds boosters is one
            // whose other children are that plate.
            bool isBoosterRow = false;
            for (int i = 0; i < children.Count; i++)
                if (children[i] != null && children[i].name.StartsWith("Boost")) isBoosterRow = true;
            if (!isBoosterRow) return;

            var plate = new List<string>();
            for (int i = 0; i < children.Count; i++)
            {
                Transform child = children[i];
                if (child == null || child.name == "Pause") continue;
                if (child.name.StartsWith("Boost")) continue;
                if (PrefabUtility.IsPartOfPrefabInstance(child.gameObject))
                {
                    child.gameObject.SetActive(false);
                    continue;
                }
                plate.Add(child.name);
                Object.DestroyImmediate(child.gameObject);
            }

            if (plate.Count > 0)
                Debug.Log("[Story] HUD bake: removed the kit's level plate from the booster row (" +
                          string.Join(", ", plate.ToArray()) + ").");
        }

        /// <summary>Repoints every Image at the saved copy of the sprite it was drawn with.</summary>
        private static void Repoint(Transform root, Dictionary<Sprite, Sprite> saved)
        {
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Sprite asset;
                if (images[i].sprite != null && saved.TryGetValue(images[i].sprite, out asset))
                    images[i].sprite = asset;
            }
        }

        private static int CountImages(Transform root)
        {
            return root.GetComponentsInChildren<Image>(true).Length;
        }

        /// <summary>Same stretch the runtime did: the metrics want the whole panel.</summary>
        private static void FillTheStrip(Transform strip)
        {
            var rect = strip as RectTransform;
            if (rect == null) return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Graphic background = rect.GetComponent<Graphic>();
            if (background != null) background.enabled = false;
        }

        private static Transform FindStrip(UnityEngine.SceneManagement.Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name != "Level") continue;
                return roots[i].transform.Find("Canvas/Panel/Panel");
            }
            return null;
        }

        private static float ScaleOf(Transform parent)
        {
            // The level's Canvas is inactive when the scene is opened to bake.
            var scaler = parent.GetComponentInParent<CanvasScaler>(true);
            if (scaler == null || scaler.referenceResolution.x <= 0f) return 1f;
            return scaler.referenceResolution.x / 1080f;
        }
    }
}
