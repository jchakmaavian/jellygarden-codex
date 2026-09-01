// Turns the map from a level list into a story.
//
// The first node of each arc gets a chapter banner, and the area the player is
// actually on gets its name. Labels are clones of the node's own number label,
// so they inherit the kit's font, canvas, material and sorting without this
// file knowing anything about them — and without the map prefab being edited.
//
// Naming every node was the first attempt and it did not survive being looked
// at: the path winds tightly enough that 150 names overlapped each other and
// sat under the node sprites, so the map read as broken text rather than as a
// story. One name — the room you are standing in — plus six chapter banners
// says the same thing and can be read.

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

        /// <summary>
        /// Local offset of the area name below its node. Far enough clear that
        /// the node sprite does not sit on top of the text — the first attempt
        /// tucked it under the stone and only fragments were readable.
        /// </summary>
        private static readonly Vector3 NameOffset = new Vector3(0f, -0.78f, 0f);

        /// <summary>Local offset of an arc banner above the arc's first node.</summary>
        private static readonly Vector3 BannerOffset = new Vector3(0f, 1.15f, 0f);

        private int _labelledArea = -1;

        /// <summary>
        /// Adds the labels to whichever map is in the loaded scene. Called by
        /// StorySceneHooks on every scene load, and a no-op when this scene has
        /// no map in it.
        /// </summary>
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

        private void Update()
        {
            // The name follows the player. Cheap: an int compare per frame, and
            // a rebuild only when they actually move on.
            if (StoryProgress.FurthestUnlockedArea != _labelledArea)
                Apply();
        }

        /// <summary>
        /// Rebuilds the labels. Called on Start and again whenever progress moves
        /// the player to a new area, so the name follows them up the map.
        /// </summary>
        public void Apply()
        {

            var map = GetComponent<LevelsMap>();
            if (map == null) return;

            // First node of each arc, so a banner lands exactly once per arc.
            var arcFirstNode = new Dictionary<int, int>();
            foreach (StoryArcInfo arc in StoryDatabase.Arcs)
                arcFirstNode[arc.FirstArea] = arc.Arc;

            // The area the player is on. Only this one is named, so the name is
            // legible and always tells you where you are.
            int currentArea = StoryProgress.FurthestUnlockedArea;

            int labelled = 0;
            int banners = 0;

            foreach (MapLevel node in map.GetMapLevels())
            {
                StoryArea area = StoryDatabase.GetArea(node.Number);
                if (area == null) continue;

                // Clear a name left on the node the player has moved off.
                if (node.Number != currentArea)
                {
                    Transform stale = FindDeep(node.transform, NameLabelObject);
                    if (stale != null) Destroy(stale.gameObject);
                }

                Text source = FindNumberLabel(node);
                if (source == null) continue;

                if (node.Number == currentArea &&
                    AddLabel(node.transform, source, NameLabelObject, area.Name,
                        NameOffset, 1.15f, Color.white))
                    labelled++;

                int arcNumber;
                if (arcFirstNode.TryGetValue(node.Number, out arcNumber))
                {
                    StoryArcInfo arc = StoryDatabase.ArcOf(node.Number);
                    string title = arc != null
                        ? string.Format("Arc {0} — {1}", arc.Arc, arc.Title)
                        : "Arc " + arcNumber;
                    if (AddLabel(node.transform, source, BannerObject, title,
                            BannerOffset, 1.3f, new Color(1f, 0.88f, 0.45f, 1f)))
                        banners++;
                }
            }

            _labelledArea = currentArea;

            Debug.Log(string.Format(
                "[Story] Map labelled: area {0} named, {1} arc banners.",
                currentArea, banners));

            ReportMapState(map);
        }

        /// <summary>
        /// Prints what the map is actually showing. A screenshot cannot tell an
        /// unlocked node from a locked one at a glance, and guessing from pixels
        /// is how a "the game will not start" bug ships.
        /// </summary>
        private static void ReportMapState(LevelsMap map)
        {
            var sb = new global::System.Text.StringBuilder("[Story] Map state: ");
            int locked = 0;
            int total = 0;

            foreach (MapLevel node in map.GetMapLevels())
            {
                total++;
                if (node.IsLocked) locked++;
                if (node.Number <= 3)
                    sb.AppendFormat("node {0} locked={1}; ", node.Number, node.IsLocked);
            }

            sb.AppendFormat("{0} of {1} nodes locked. ", locked, total);
            sb.AppendFormat("StoryProgress says area 1 unlocked={0}, area 2 unlocked={1}. ",
                StoryProgress.IsAreaUnlocked(1), StoryProgress.IsAreaUnlocked(2));

            // Where the playable node actually is, so a click can be aimed at it
            // instead of guessed at from a screenshot.
            MapLevel first = map.GetLevel(StoryProgress.FurthestUnlockedArea);
            if (first != null && Camera.main != null)
            {
                Vector3 screen = Camera.main.WorldToScreenPoint(first.transform.position);
                sb.AppendFormat(
                    "Node {0} is at world {1}, screen ({2:0},{3:0}) in a {4}x{5} view.",
                    first.Number, first.transform.position, screen.x, screen.y,
                    Screen.width, Screen.height);
            }

            Debug.Log(sb.ToString());
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
            string content, Vector3 offset, float scale, Color colour)
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
            text.color = colour;
            text.fontStyle = FontStyle.Bold;

            // The map is a bright green field, and the node numbers are dark by
            // design because they sit on a pale stone. A label out on the grass
            // needs its own contrast, so it borrows the kit's own outline effect
            // rather than inventing a second look.
            var outline = clone.GetComponent<JellyGarden.Scripts.GUI.NicerOutline>();
            if (outline == null)
                outline = clone.AddComponent<JellyGarden.Scripts.GUI.NicerOutline>();
            outline.effectColor = new Color(0.05f, 0.12f, 0.03f, 0.95f);
            outline.effectDistance = new Vector2(2.2f, -2.2f);

            // Above the node sprites: a name half-hidden behind a stone reads as
            // a rendering bug.
            clone.transform.SetAsLastSibling();

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
