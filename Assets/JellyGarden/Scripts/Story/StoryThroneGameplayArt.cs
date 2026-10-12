using System.Collections.Generic;
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
        public const float CellArtScale = .90f;
        static readonly HashSet<string> CellArt = new HashSet<string> {
            "red", "purple", "blue", "green", "yellow", "rocket", "area-bomb", "rainbow-orb",
            "crate", "ice", "stone", "ivy-stone", "chains", "locket", "ring"
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

        public static Sprite Resolve(string key, Sprite original)
        {
            // Orange is King Leo's coin. Unmapped keys include the courtyard and every UI surface.
            if (!Gameplay || key == "orange" || key == "coin" || !original) return original;
            var source = Source(key);
            if (!source) return original;
            string id = key + ":" + original.GetEntityId();
            if (Fitted.TryGetValue(id, out var fitted)) return fitted;
            float extent = Mathf.Max(original.bounds.size.x, original.bounds.size.y);
            if (CellArt.Contains(key)) extent *= CellArtScale;
            Vector2 pivot = source.pivot / source.rect.size;
            fitted = Sprite.Create(source.texture, source.rect, pivot,
                Mathf.Max(source.rect.width, source.rect.height) / Mathf.Max(.01f, extent), 0, SpriteMeshType.FullRect);
            fitted.name = source.name;
            fitted.hideFlags = HideFlags.DontSave;
            Fitted[id] = fitted;
            return fitted;
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
                replacement = StoryThroneGameplayArt.Resolve(key, original);
                if (replacement != original) replacement.name = original.name;
            }
            view.sprite = replacement;
        }
        void LateUpdate() { Apply(); }
    }
}
