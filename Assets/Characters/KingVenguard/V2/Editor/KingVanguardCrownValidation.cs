#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace RoyalThrone.Characters.EditorTools
{
    /// <summary>Regression checks for complete crown UVs, skin registration and animated weights.</summary>
    public static class KingVanguardCrownValidation
    {
        [MenuItem("Royal Throne/Characters/Validate Vanguard Crown")]
        public static void ValidateMenu() { Debug.Log(Run()); }

        public static string Run()
        {
            int poses = 0;
            foreach (string name in new[] { "King", "AreaCompleted" })
            {
                var asset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(
                    "Assets/Characters/KingVenguard/V2/Spine/" + name + "_Vanguard_SkeletonData.asset");
                Require(asset, "Missing skeleton: " + name);
                foreach (var atlas in asset.atlasAssets) atlas.Clear();
                asset.Clear(); var data = asset.GetSkeletonData(false);
                int slotIndex = data.FindSlotIndex("Taç");
                Require(slotIndex >= 0, "Missing crown slot");
                var crown = data.DefaultSkin.GetAttachment(slotIndex, "Taç") as MeshAttachment;
                var region = crown == null ? null : crown.RendererObject as AtlasRegion;
                Require(region != null && region.name == "Vanguard Crown", "Crown replacement missing");
                Require(crown.WorldVerticesLength == 40 && crown.Triangles.Length == 72, "Incomplete crown grid");
                int row = name == "King" ? 1880 : 1722;
                Require(Mathf.Abs(region.v-(1f-(float)row/region.page.height))<.00001f &&
                    Mathf.Abs(region.v2-(1f-(float)(row+288)/region.page.height))<.00001f,
                    "Crown samples the wrong atlas rows");
                foreach (var entry in data.DefaultSkin.GetAttachments())
                    if (entry.SlotIndex == slotIndex && entry.Name == "Taç")
                        Require(ReferenceEquals(entry.Attachment, crown), "Skin lookup and enumeration disagree");
                foreach (var modifier in asset.skeletonDataModifiers) modifier.Apply(data);
                Require(ReferenceEquals(data.DefaultSkin.GetAttachment(slotIndex, "Taç"), crown), "Crown registration is not idempotent");
                var corners = new HashSet<Vector2>();
                for (int i = 0; i < crown.RegionUVs.Length; i += 2)
                    corners.Add(new Vector2(crown.RegionUVs[i], crown.RegionUVs[i + 1]));
                foreach (var corner in new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one })
                    Require(corners.Contains(corner), "Crown silhouette can be clipped at a texture corner");
                int b = 0, v = 0;
                while (b < crown.Bones.Length)
                {
                    int count = crown.Bones[b++]; float sum = 0;
                    for (int i = 0; i < count; i++)
                    {
                        Require(crown.Bones[b++] < data.Bones.Count, "Invalid crown bone");
                        float weight = crown.Vertices[v + 2]; v += 3;
                        Require(weight > 0 && !float.IsNaN(weight), "Invalid crown weight"); sum += weight;
                    }
                    Require(Mathf.Abs(sum - 1) < .0001f, "Crown weights are not normalized");
                }
                var skeleton = new Skeleton(data); var world = new float[crown.WorldVerticesLength];
                foreach (var clip in data.Animations)
                    for (int phase = 0; phase < 5; phase++)
                    {
                        skeleton.SetToSetupPose();
                        clip.Apply(skeleton, 0, clip.Duration * phase / 4, false, null, 1, MixBlend.Setup, MixDirection.In);
                        skeleton.UpdateWorldTransform(); crown.ComputeWorldVertices(skeleton.Slots.Items[slotIndex], world);
                        foreach (float coordinate in world)
                            Require(!float.IsNaN(coordinate) && !float.IsInfinity(coordinate), "Nonfinite crown pose: " + clip.Name);
                        poses++;
                    }
            }
            return "PASS Vanguard crown: complete UV coverage, normalized native weights, idempotent skin replacement; " + poses + " animated poses.";
        }
        static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    }
}
#endif
