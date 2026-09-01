// Grows the hand-made map from the kit's 100 level nodes to the story's 150
// area nodes, without redrawing it.
//
// The new nodes are clones of the existing ones and the new background is the
// existing background sprites tiled further up, so the map that ships is still
// the map an artist made — it just runs 50 areas longer.
//
// Unity -batchmode -quit -nographics -projectPath . \
//   -executeMethod JellyGarden.Scripts.Story.Editor.StoryMapExtender.RunHeadless -logFile -

using System.Collections.Generic;
using System.Linq;
using JellyGarden.Scripts.MapScripts;
using UnityEditor;
using UnityEngine;

namespace JellyGarden.Scripts.Story.Editor
{
    public static class StoryMapExtender
    {
        public const string MapPrefabPath = "Assets/JellyGarden/Prefabs/Prefabs/LevelsMap.prefab";

        /// <summary>How many nodes the story needs. Read from the data, not typed in.</summary>
        private static int TargetNodeCount
        {
            get { return Mathf.Max(1, StoryDatabase.AreaCount); }
        }

        [MenuItem("JellyGarden/Story/Extend Map To Story Length")]
        public static void Extend()
        {
            string error;
            int count = ExtendInternal(out error);
            if (error != null)
            {
                Debug.LogError("[Story] Map extension failed: " + error);
                return;
            }
            Debug.Log(string.Format("[Story] Map now has {0} area nodes.", count));
        }

        public static void RunHeadless()
        {
            string error;
            int count = ExtendInternal(out error);
            if (error != null)
            {
                Debug.LogError("[Story] Map extension failed: " + error);
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log(string.Format("[Story] Map now has {0} area nodes.", count));
            EditorApplication.Exit(0);
        }

        private static int ExtendInternal(out string error)
        {
            error = null;
            int wanted = TargetNodeCount;

            GameObject root = PrefabUtility.LoadPrefabContents(MapPrefabPath);
            if (root == null)
            {
                error = "could not load " + MapPrefabPath;
                return 0;
            }

            try
            {
                List<MapLevel> nodes = root.GetComponentsInChildren<MapLevel>(true)
                    .OrderBy(l => l.Number).ToList();

                if (nodes.Count == 0)
                {
                    error = "the map prefab has no MapLevel nodes";
                    return 0;
                }

                if (nodes.Count >= wanted)
                {
                    // Idempotent: a second run is a no-op, not a second copy.
                    Debug.Log(string.Format(
                        "[Story] Map already has {0} nodes for {1} areas; nothing to add.",
                        nodes.Count, wanted));
                    return nodes.Count;
                }

                Transform holder = nodes[0].transform.parent;
                Path path = root.GetComponentInChildren<Path>(true);

                int added = AddNodes(nodes, holder, path, wanted);
                ExtendBackground(root, nodes[0].transform.parent);
                Debug.Log(string.Format("[Story] Added {0} area nodes.", added));

                PrefabUtility.SaveAsPrefabAsset(root, MapPrefabPath);

                float top = HighestPoint(root);
                GrowCameraBounds(top);

                return nodes.Count + added;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static float HighestPoint(GameObject root)
        {
            float top = float.MinValue;
            foreach (Transform child in root.transform)
            {
                var renderer = child.GetComponent<SpriteRenderer>();
                if (renderer != null && child.name.StartsWith("map_background"))
                    top = Mathf.Max(top, child.localPosition.y + renderer.bounds.size.y * 0.5f);
            }
            return top;
        }

        /// <summary>
        /// MapCamera clamps scrolling to a serialised Bounds that lives on the
        /// camera in the game scene, not in the map prefab. Extending the map
        /// without extending this leaves the new nodes unreachable — the player
        /// can see them coming and never scroll to them.
        /// </summary>
        private static void GrowCameraBounds(float requiredTop)
        {
            const string ScenePath = "Assets/JellyGarden/Scenes/game.unity";

            UnityEngine.SceneManagement.Scene scene =
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                    ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);

            try
            {
                MapCamera camera = null;
                foreach (GameObject go in scene.GetRootGameObjects())
                {
                    camera = go.GetComponentInChildren<MapCamera>(true);
                    if (camera != null) break;
                }

                if (camera == null)
                {
                    Debug.LogWarning("[Story] No MapCamera in " + ScenePath +
                                     "; scrolling bounds were not extended.");
                    return;
                }

                Bounds bounds = camera.Bounds;
                if (bounds.max.y >= requiredTop)
                {
                    Debug.Log("[Story] Camera bounds already cover the extended map.");
                    return;
                }

                Vector3 min = bounds.min;
                var max = new Vector3(bounds.max.x, requiredTop, bounds.max.z);
                bounds.SetMinMax(min, max);
                camera.Bounds = bounds;

                EditorUtility.SetDirty(camera);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

                Debug.Log(string.Format(
                    "[Story] Camera scroll bounds raised to y={0:0.#} so the new nodes are reachable.",
                    requiredTop));
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>
        /// Clones the last stretch of the existing path and lays it end to end
        /// after the final node, so the added run winds the way the hand-made
        /// one does instead of marching in a straight line.
        /// </summary>
        private static int AddNodes(List<MapLevel> nodes, Transform holder, Path path, int wanted)
        {
            int existing = nodes.Count;
            int needed = wanted - existing;

            // Use as long a stretch of the original as we need, capped at half the
            // map so the repeat is not obvious.
            int window = Mathf.Min(needed, Mathf.Max(2, existing / 2));
            int windowStart = existing - window;

            MapLevel last = nodes[existing - 1];
            Vector3 step = last.transform.localPosition -
                           nodes[existing - 2].transform.localPosition;
            Vector3 windowOrigin = nodes[windowStart].transform.localPosition;
            Vector3 anchor = last.transform.localPosition + step;

            var added = new List<MapLevel>();
            for (int i = 0; i < needed; i++)
            {
                MapLevel source = nodes[windowStart + (i % window)];
                int number = existing + i + 1;

                // A new lap starts where the previous one ended, so the path keeps
                // climbing instead of looping back over itself.
                int lap = i / window;
                Vector3 lapOffset = lap == 0
                    ? Vector3.zero
                    : lap * (nodes[existing - 1].transform.localPosition - windowOrigin + step);

                var clone = (GameObject)Object.Instantiate(source.gameObject, holder);
                clone.name = "Level" + number;
                clone.transform.localPosition =
                    anchor + (source.transform.localPosition - windowOrigin) + lapOffset;
                clone.transform.localRotation = source.transform.localRotation;
                clone.transform.localScale = source.transform.localScale;

                var level = clone.GetComponent<MapLevel>();
                level.Number = number;
                level.IsLocked = true;
                if (level.Lock != null) level.Lock.gameObject.SetActive(true);

                added.Add(level);
            }

            if (path != null)
            {
                // WaypointsMover walks Path.Waypoints by index, so a pivot that is
                // not registered here makes the character refuse to travel to it.
                foreach (MapLevel level in added)
                    if (level.PathPivot != null && !path.Waypoints.Contains(level.PathPivot))
                        path.Waypoints.Add(level.PathPivot);
            }
            else
            {
                Debug.LogWarning("[Story] No Path component found; the character will " +
                                 "teleport rather than walk to the new nodes.");
            }

            return added.Count;
        }

        /// <summary>
        /// Tiles the existing background sprites further up so the extended path
        /// stays on painted ground. No new art: the same five sprites repeat.
        /// </summary>
        private static void ExtendBackground(GameObject root, Transform holder)
        {
            var tiles = new List<SpriteRenderer>();
            foreach (Transform child in root.transform)
            {
                // Clones from a previous run must not become sources themselves,
                // or the repeat pattern drifts.
                if (!child.name.StartsWith("map_background")) continue;
                if (child.name.Contains("_ext")) continue;
                var renderer = child.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null) tiles.Add(renderer);
            }

            if (tiles.Count == 0)
            {
                Debug.LogWarning("[Story] No map_background tiles found; the extended " +
                                 "path will run off the painted area.");
                return;
            }

            tiles.Sort((a, b) => a.transform.localPosition.y.CompareTo(b.transform.localPosition.y));

            float tileHeight = tiles[0].bounds.size.y;
            float topY = tiles[tiles.Count - 1].transform.localPosition.y;

            float highestNode = float.MinValue;
            foreach (Transform child in holder)
                highestNode = Mathf.Max(highestNode, child.localPosition.y);

            // Cover the highest node plus one tile of headroom, so the camera never
            // reaches an unpainted edge.
            float required = highestNode + tileHeight;

            int sourceCount = tiles.Count;
            int addedTiles = 0;
            while (topY < required)
            {
                SpriteRenderer source = tiles[addedTiles % sourceCount];
                topY += tileHeight;

                var clone = (GameObject)Object.Instantiate(source.gameObject, root.transform);
                clone.name = source.name + "_ext" + (addedTiles + 1);
                clone.transform.localPosition =
                    new Vector3(source.transform.localPosition.x, topY,
                        source.transform.localPosition.z);
                addedTiles++;
            }

            if (addedTiles > 0)
                Debug.Log(string.Format(
                    "[Story] Tiled {0} more background panels to cover the extended path (up to y={1:0.#}).",
                    addedTiles, topY));
        }
    }
}
