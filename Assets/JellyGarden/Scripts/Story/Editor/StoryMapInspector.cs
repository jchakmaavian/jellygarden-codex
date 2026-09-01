// Read-only reconnaissance on the map prefab. Prints what is actually in
// LevelsMap.prefab so the extender does not have to guess at YAML.
//
// Unity -batchmode -quit -nographics -projectPath . \
//   -executeMethod JellyGarden.Scripts.Story.Editor.StoryMapInspector.Dump -logFile -

using System.Collections.Generic;
using System.Linq;
using System.Text;
using JellyGarden.Scripts.MapScripts;
using UnityEditor;
using UnityEngine;

namespace JellyGarden.Scripts.Story.Editor
{
    public static class StoryMapInspector
    {
        public const string MapPrefabPath = "Assets/JellyGarden/Prefabs/Prefabs/LevelsMap.prefab";

        [MenuItem("JellyGarden/Story/Inspect Map Prefab")]
        public static void Dump()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(MapPrefabPath);
            if (root == null)
            {
                Debug.LogError("[Story] Could not load " + MapPrefabPath);
                return;
            }

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("[Story] Map prefab: " + root.name);

                foreach (Transform child in root.transform)
                {
                    var renderer = child.GetComponent<SpriteRenderer>();
                    sb.AppendFormat(
                        "  child '{0}' pos={1} scale={2} sprite={3} bounds={4}\n",
                        child.name,
                        child.localPosition,
                        child.localScale,
                        renderer != null && renderer.sprite != null ? renderer.sprite.name : "-",
                        renderer != null ? renderer.bounds.size.ToString() : "-");
                }

                List<MapLevel> levels = root.GetComponentsInChildren<MapLevel>(true)
                    .OrderBy(l => l.Number).ToList();
                sb.AppendFormat("  MapLevel count: {0}\n", levels.Count);

                if (levels.Count > 0)
                {
                    Transform holder = levels[0].transform.parent;
                    sb.AppendFormat("  levels parent: '{0}' pos={1}\n",
                        holder != null ? holder.name : "<none>",
                        holder != null ? holder.localPosition.ToString() : "-");

                    foreach (int index in new[] { 0, 1, 2, levels.Count - 3, levels.Count - 2, levels.Count - 1 })
                    {
                        if (index < 0 || index >= levels.Count) continue;
                        MapLevel level = levels[index];
                        sb.AppendFormat(
                            "    [{0}] name='{1}' number={2} pos={3} pivot={4} scene='{5}'\n",
                            index, level.name, level.Number, level.transform.localPosition,
                            level.PathPivot != null ? level.PathPivot.localPosition.ToString() : "-",
                            level.SceneName);
                    }

                    // The spacing between consecutive nodes is what the extender
                    // has to continue, so measure it rather than assume it.
                    Vector3 min = levels[0].transform.localPosition;
                    Vector3 max = min;
                    float totalStep = 0f;
                    for (int i = 1; i < levels.Count; i++)
                    {
                        Vector3 p = levels[i].transform.localPosition;
                        totalStep += Vector3.Distance(p, levels[i - 1].transform.localPosition);
                        min = Vector3.Min(min, p);
                        max = Vector3.Max(max, p);
                    }
                    sb.AppendFormat("  node bounds: min={0} max={1} avgStep={2:0.###}\n",
                        min, max, totalStep / (levels.Count - 1));

                    sb.AppendLine("  first node hierarchy:");
                    DumpHierarchy(sb, levels[0].transform, "    ");
                }

                var map = root.GetComponent<LevelsMap>();
                if (map != null)
                {
                    sb.AppendFormat("  LevelsMap: Count={0} IsGenerated={1} translation={2} stars={3}\n",
                        map.Count, map.IsGenerated, map.TranslationType, map.StarsEnabled);
                    sb.AppendFormat("  WaypointsMover: {0}\n",
                        map.WaypointsMover != null ? map.WaypointsMover.name : "<none>");
                }

                var path = root.GetComponentInChildren<PathMap>(true);
                if (path != null)
                    sb.AppendFormat("  PathMap '{0}' waypoints={1}\n", path.name, path.Waypoints.Count);

                Debug.Log(sb.ToString());
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void DumpHierarchy(StringBuilder sb, Transform node, string indent, int depth = 0)
        {
            if (depth > 3) return;
            foreach (Transform child in node)
            {
                var renderer = child.GetComponent<SpriteRenderer>();
                sb.AppendFormat("{0}{1}{2}\n", indent, child.name,
                    renderer != null && renderer.sprite != null ? " [" + renderer.sprite.name + "]" : "");
                DumpHierarchy(sb, child, indent + "  ", depth + 1);
            }
        }
    }
}
