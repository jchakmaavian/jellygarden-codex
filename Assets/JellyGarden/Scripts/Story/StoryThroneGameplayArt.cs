using System.Collections.Generic;
using JellyGarden.Scripts.GUI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JellyGarden.Scripts.Story
{
    // Gameplay-only art adapter. Coin, backdrop, home and UI geometry retain their original assets.
    public static class StoryThroneGameplayArt
    {
        static readonly Dictionary<string, string> Native = new Dictionary<string, string> {
            {"red", "Items-MatchItem-book__22221"}, {"purple", "Items-MatchItem-gem__16466"},
            {"blue", "Items-MatchItem-shield__21626"}, {"green", "Items-MatchItem-leaf__16451"},
            {"yellow", "Items-MatchItem-crown__22208"},
            {"rocket", "Items-SpecialItems-RocketItem-horizontal_rocket__16419"},
            {"area-bomb", "Items-SpecialItems-TntItem-tnt__16602"},
            {"rainbow-orb", "Items-SpecialItems-LightballItem-default__16887"},
            {"crate", "Items-BoxItem-box_1__16297"}
        };
        static readonly HashSet<string> Imported = new HashSet<string> {
            "ice", "stone", "ivy-stone", "locket", "ring", "recolour", "chains", "hammer", "score", "glove"
        };
        static Sprite backdrop;
        public static bool IsGameplayBackdrop(Sprite sprite) => sprite && sprite == backdrop;
        // Exact texture generation from Royal Throne's RoyalGameplayHud.Backdrop.
        public static Sprite Backdrop()
        {
            if (backdrop) return backdrop;
            const int w = 96, h = 192;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) {
                name = "Royal HUD backdrop", wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave
            };
            Color centreTop = new Color32(43,121,218,255), centreBottom = new Color32(25,100,197,255);
            Color edgeTop = new Color32(17,65,142,255), edgeBottom = new Color32(12,53,125,255);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++) {
                float v = y / (h - 1f);
                Color centre = Color.Lerp(centreBottom, centreTop, v), edge = Color.Lerp(edgeBottom, edgeTop, v);
                for (int x = 0; x < w; x++) {
                    float d = (x + .5f) / w - .5f, f = Mathf.Exp(-(d * d) / (2 * .235f * .235f));
                    pixels[y * w + x] = Color.Lerp(edge, centre, f);
                }
            }
            texture.SetPixels(pixels); texture.Apply(false, true);
            backdrop = Sprite.Create(texture, new Rect(0,0,w,h), new Vector2(.5f,.5f), w, 0, SpriteMeshType.FullRect);
            backdrop.name = "Royal HUD backdrop"; backdrop.hideFlags = HideFlags.DontSave;
            return backdrop;
        }
        public const float CellArtScale = .78f;
        static readonly HashSet<string> CellArt = new HashSet<string> {
            "red", "orange", "coin", "purple", "blue", "green", "yellow", "rocket", "area-bomb", "rainbow-orb",
            "crate", "ice", "stone", "ivy-stone", "chains", "locket", "ring"
        };
        static readonly Dictionary<string, Rect> Painted = new Dictionary<string, Rect> {
            {"red", new Rect(0.00892857f,0.00800000f,0.98214286f,0.98400000f)},
            {"purple", new Rect(0.00813008f,0.00877193f,0.98373984f,0.98245614f)},
            {"blue", new Rect(0.00813008f,0.00787402f,0.98373984f,0.98425197f)},
            {"green", new Rect(0.00813008f,0.00813008f,0.98373984f,0.98373984f)},
            {"yellow", new Rect(0.00769231f,0.00869565f,0.98461538f,0.98260870f)},
            {"crate", new Rect(0.00704225f,0.00704225f,0.98591549f,0.98591549f)},
            {"rocket", new Rect(0.00746269f,0.00943396f,0.98507463f,0.98113208f)},
            {"area-bomb", new Rect(0.00559701f,0.00984252f,0.98507463f,0.98425197f)},
            {"rainbow-orb", new Rect(0.00763359f,0.00757576f,0.98473282f,0.98484848f)},
            {"orange", new Rect(0.09409888f,0.09170654f,0.81259968f,0.81897927f)},
            {"coin", new Rect(0.09409888f,0.09170654f,0.81259968f,0.81897927f)},
            {"ice", new Rect(0.10546875f,0.09179688f,0.79101562f,0.81640625f)},
            {"stone", new Rect(0.09960938f,0.08593750f,0.80273438f,0.82617188f)},
            {"ivy-stone", new Rect(0.08984375f,0.09179688f,0.81640625f,0.81835938f)},
            {"chains", new Rect(0.04687500f,0.05468750f,0.90625000f,0.89843750f)},
            {"locket", new Rect(0.15820312f,0.08593750f,0.67968750f,0.82812500f)},
            {"ring", new Rect(0.08984375f,0.08984375f,0.82812500f,0.82617188f)},
        };
        static readonly Dictionary<string, Sprite> Raw = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Sprite> Fitted = new Dictionary<string, Sprite>();
        public static bool Gameplay => StorybookBoardArt.Enabled && SceneManager.GetActiveScene().name == "game" &&
            LevelManager.THIS != null && LevelManager.THIS.gameStatus != GameState.Map;

        public static Sprite Source(string key)
        {
            if (!Native.ContainsKey(key) && !Imported.Contains(key)) return null;
            if (Raw.TryGetValue(key, out var cached)) return cached;
            string path = Native.TryGetValue(key, out var name) ? "ReferenceGameplay/Sprites/" + name : "Story/ThroneGameplay/" + key;
            var sprite = Resources.Load<Sprite>(path);
            Raw[key] = sprite;
            if (!sprite) Debug.LogError("[Throne gameplay art] Missing sprite: " + path);
            return sprite;
        }

        public static Sprite Resolve(string key, Sprite original, bool blocker = false, bool popup = false)
        {
            // Keep the existing coin texture; fit it to the same painted extent as the other gems.
            if ((!Gameplay && !popup) || !original) return original;
            if (key == "courtyard") return Backdrop();
            var source = key == "orange" || key == "coin" ? Resources.Load<Sprite>("Story/RoyalBoard/coin") : Source(key);
            if (!source) return original;
            string id = key + ":" + original.GetEntityId() + ":" + blocker;
            if (Fitted.TryGetValue(id, out var fitted)) return fitted;
            float extent = Mathf.Max(original.bounds.size.x, original.bounds.size.y);
            Rect rect = source.rect;
            Vector2 pivot = source.pivot / source.rect.size;
            if (CellArt.Contains(key)) {
                // Normalize painted bounds to a common cell share, instead of scaling old artwork canvases.
                if (Painted.TryGetValue(key, out var painted)) rect = new Rect(
                    source.rect.x + painted.x * source.rect.width, source.rect.y + painted.y * source.rect.height,
                    painted.width * source.rect.width, painted.height * source.rect.height);
                extent = CellArtScale * (blocker ? StoryBoardScale.KitPitch : StoryBoardScale.Pitch / StoryBoardScale.ItemScale);
                pivot = new Vector2(.5f, .5f);
            }
            fitted = Sprite.Create(source.texture, rect, pivot,
                Mathf.Max(rect.width, rect.height) / Mathf.Max(.01f, extent), 0, SpriteMeshType.FullRect);
            fitted.name = source.name;
            fitted.hideFlags = HideFlags.DontSave;
            Fitted[id] = fitted;
            return fitted;
        }

        public static Sprite PopupGoal(StoryLevelHud.HudTarget target)
        {
            if (target.IsScore) return Resolve("score", target.Icon ? target.Icon : StoryUi.StarSprite, false, true);
            foreach (var key in new[] { "red", "orange", "purple", "blue", "green", "yellow", "ice", "locket", "ring" }) {
                var current = StorybookBoardArt.Get(key);
                if (current && target.Icon && current.texture == target.Icon.texture) return Resolve(key, current, false, true);
            }
            return target.Icon;
        }
        public static Sprite PopupBooster(BoostType type, Sprite original)
        {
            string key = type == BoostType.Stripes ? "rocket" : type == BoostType.Packages ? "area-bomb" :
                type == BoostType.Colorful_bomb ? "rainbow-orb" : null;
            return key == null ? original : Resolve(key, original, false, true);
        }

        public static void FitIdleBody(GameObject body)
        {
            bool found = false; Bounds bounds = default;
            foreach (var renderer in body.GetComponentsInChildren<SpriteRenderer>()) {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || !renderer.sprite || renderer.color.a < .01f) continue;
                if (!found) { bounds = renderer.bounds; found = true; } else bounds.Encapsulate(renderer.bounds);
            }
            if (!found) return;
            float span = Mathf.Max(bounds.size.x, bounds.size.y);
            if (span > .001f) body.transform.localScale *= StoryBoardScale.Pitch * CellArtScale / span;
        }

        public static void DressBlocker(GameObject block)
        {
            if (!Gameplay || !block) return;
            foreach (var renderer in block.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var old = renderer.sprite;
                if (!old) continue;
                string name = old.name.ToLowerInvariant(), key = null;
                if (name == "block" || name == "block_02") key = "ice";
                else if (name.Contains("solidblock")) key = "crate";
                else if (name.Contains("wireblock")) key = "chains";
                else if (name.Contains("undestroyable")) key = "stone";
                else if (name.Contains("thriving")) key = "ivy-stone";
                if (key == null) continue;
                var skin = renderer.GetComponent<StoryThroneBlockerSkin>();
                if (!skin) skin = renderer.gameObject.AddComponent<StoryThroneBlockerSkin>();
                skin.Set(renderer, key, old);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            foreach (var sprite in Fitted.Values) if (sprite) Object.Destroy(sprite);
            Raw.Clear(); Fitted.Clear();
            if (backdrop) { Object.Destroy(backdrop.texture); Object.Destroy(backdrop); }
            backdrop = null;
        }
    }

    // Reapply after the legacy animation has keyed its old sprite. Preserve the name used by goal matching.
    public sealed class StoryThroneBlockerSkin : MonoBehaviour
    {
        SpriteRenderer view; Sprite original, replacement; string key;
        public void Set(SpriteRenderer renderer, string artKey, Sprite old) { view = renderer; key = artKey; original = old; Apply(); }
        void Apply()
        {
            if (!view || !StoryThroneGameplayArt.Gameplay) return;
            if (!replacement) {
                replacement = StoryThroneGameplayArt.Resolve(key, original, true);
                if (replacement != original) replacement.name = original.name;
            }
            view.sprite = replacement;
        }
        void LateUpdate() { Apply(); }
    }
}
