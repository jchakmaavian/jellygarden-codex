// Turns the map from a level list into a story.
//
// Every node gets the name of the area it is, and the first node of each arc
// gets a chapter banner. Both labels are clones of the node's own number label,
// so they inherit the kit's font, canvas, material and sorting without this
// file knowing anything about them — and without the map prefab being edited.

using System.Collections.Generic;
using JellyGarden.Scripts.MapScripts;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryMapLabels : MonoBehaviour
    {
        private const string NameLabelObject = "StoryAreaName";
        private const string BannerObject = "StoryArcBanner";

        /// <summary>Local offset of the area name under its node, in world units.</summary>
        private static readonly Vector3 NameOffset = new Vector3(0f, -0.52f, 0f);

        /// <summary>Local offset of an arc banner above the arc's first node.</summary>
        private static readonly Vector3 BannerOffset = new Vector3(0f, 0.95f, 0f);

        private bool _applied;

        /// <summary>
        /// Adds the labels to whichever map is in the loaded scene. Runs after
        /// the scene is up so LevelsMap has built its node list, and does nothing
        /// at all if there is no map in this scene.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Install()
        {
            var map = Object.FindFirstObjectByType<LevelsMap>();
            if (map == null) return;
            if (map.GetComponent<StoryMapLabels>() != null) return;
            map.gameObject.AddComponent<StoryMapLabels>();
        }

        private void Start()
        {
            Apply();
        }

        public void Apply()
        {
            if (_applied) return;
            _applied = true;

            var map = GetComponent<LevelsMap>();
            if (map == null) return;

            // First node of each arc, so a banner lands exactly once per arc.
            var arcFirstNode = new Dictionary<int, int>();
            foreach (StoryArcInfo arc in StoryDatabase.Arcs)
                arcFirstNode[arc.FirstArea] = arc.Arc;

            int labelled = 0;
            int banners = 0;

            foreach (MapLevel node in map.GetMapLevels())
            {
                StoryArea area = StoryDatabase.GetArea(node.Number);
                if (area == null) continue;

                Text source = FindNumberLabel(node);
                if (source == null) continue;

                if (AddLabel(node.transform, source, NameLabelObject, area.Name,
                        NameOffset, 0.72f))
                    labelled++;

                int arcNumber;
                if (arcFirstNode.TryGetValue(node.Number, out arcNumber))
                {
                    StoryArcInfo arc = StoryDatabase.ArcOf(node.Number);
                    string title = arc != null
                        ? string.Format("Arc {0} — {1}", arc.Arc, arc.Title)
                        : "Arc " + arcNumber;
                    if (AddLabel(node.transform, source, BannerObject, title,
                            BannerOffset, 0.9f))
                        banners++;
                }
            }

            Debug.Log(string.Format("[Story] Map labelled: {0} area names, {1} arc banners.",
                labelled, banners));
        }

        /// <summary>
        /// The kit's own number label, which MapLevelNumber fills in from the
        /// node's name. Used as the template for everything added here.
        /// </summary>
        private static Text FindNumberLabel(MapLevel node)
        {
            return node.GetComponentInChildren<Text>(true);
        }

        private static bool AddLabel(Transform parent, Text source, string objectName,
            string content, Vector3 offset, float scale)
        {
            if (string.IsNullOrEmpty(content)) return false;

            // Idempotent: re-applying updates the text instead of stacking labels.
            Transform existing = FindDeep(parent, objectName);
            if (existing != null)
            {
                var existingText = existing.GetComponent<Text>();
                if (existingText != null) existingText.text = content;
                return false;
            }

            var clone = Instantiate(source.gameObject, source.transform.parent);
            clone.name = objectName;

            // MapLevelNumber overwrites the text in its own Start with the node
            // number, so it must not come along for the ride.
            var number = clone.GetComponent<JellyGarden.Scripts.GUI.MapLevelNumber>();
            if (number != null) Destroy(number);

            var text = clone.GetComponent<Text>();
            if (text == null) return false;

            text.text = content;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var rect = clone.GetComponent<RectTransform>();
            if (rect != null)
            {
                // Wide enough for a name like "Freya's Dressing Room" without the
                // node's own tight number box clipping it.
                rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x * 6f, 240f),
                    Mathf.Max(rect.sizeDelta.y, 40f));
            }

            clone.transform.localScale = source.transform.localScale * scale;
            clone.transform.localPosition = source.transform.localPosition + offset;

            return true;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                Transform found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
