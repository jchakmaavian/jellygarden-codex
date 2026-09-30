#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace RoyalThrone.Characters.Activities.Editor
{
    /// <summary>Adapts only newly created V2 facial meshes, before asset save. Original hierarchy, bindings and clips remain intact.</summary>
    public static class NativeKingDrillFaceShape
    {
        public const string HeadPath = "NewKing/01900_kafa";
        public const float HorizontalScale = 1.15f;
        public const float VerticalScale = .92f;
        public const float PupilScale = 1.55f;
        const float MaxAllowedError = .0001f;

        [Serializable]
        public sealed class Validation
        {
            public string path, bindingHashBefore, bindingHashAfter;
            public int vertexCount, boneCount;
            public bool skinned, bindingsUnchanged;
            public Vector3 pivot, sourceCenter, sourceSize, shapedCenter, shapedSize, pupilPivot;
            public float horizontalScale, verticalScale, pupilScale, maxSetupWorldError, maxSizeError, pupilCentroidError;
        }

        [Serializable]
        public sealed class ValidationSet
        {
            public string source = "Native rfv387 KingDrill; only V2 cloned mesh vertex positions are adapted.";
            public string method = "Common setup-world face affine followed by 1.55x XY scale around each pupil's post-affine centroid; inverse of each vertex's weighted skin matrix, static renderer inverse for unskinned parts.";
            public float horizontalScale = HorizontalScale, verticalScale = VerticalScale, pupilScale = PupilScale;
            public List<Validation> meshes = new List<Validation>();
        }

        public static bool IsFacialPath(string path)
        { return path == HeadPath || path.StartsWith(HeadPath + "/", StringComparison.Ordinal); }

        public static bool IsPupilPath(string path)
        { return path == HeadPath + "/01920_sag_goz_bebek" || path == HeadPath + "/01940_sol_goz_bebek"; }

        public static Validation Apply(Mesh clonedMesh, Matrix4x4 rendererSetupWorld, Matrix4x4[] boneSetupWorld, Vector3 pivot, string path)
        {
            if (!clonedMesh || !IsFacialPath(path)) throw new ArgumentException("Face shape requires a newly created facial mesh.");
            var vertices = clonedMesh.vertices;
            var weights = clonedMesh.boneWeights;
            var bindPoses = clonedMesh.bindposes;
            bool skinned = boneSetupWorld != null && boneSetupWorld.Length > 0;
            if (skinned && (weights.Length != vertices.Length || bindPoses.Length != boneSetupWorld.Length))
                throw new InvalidDataException("Face skin input count mismatch: " + path);
            string beforeHash = BindingHash(clonedMesh);
            var affine = Matrix4x4.Translate(pivot) * Matrix4x4.Scale(new Vector3(HorizontalScale, VerticalScale, 1)) * Matrix4x4.Translate(-pivot);
            bool isPupil = IsPupilPath(path);
            Vector3 pupilPivot = Vector3.zero;
            if (isPupil)
            {
                if (vertices.Length == 0) throw new InvalidDataException("Pupil mesh has no vertices: " + path);
                // Use one common setup-world center for the whole part, including weighted meshes.
                // This preserves the eye placement while increasing the artwork around its center.
                for (int i = 0; i < vertices.Length; i++)
                {
                    Matrix4x4 skinWorld = skinned ? WeightedSetupWorld(weights[i], boneSetupWorld, bindPoses) : rendererSetupWorld;
                    pupilPivot += affine.MultiplyPoint3x4(skinWorld.MultiplyPoint3x4(vertices[i]));
                }
                pupilPivot /= vertices.Length;
            }
            var changed = new Vector3[vertices.Length];
            Bounds before = default, after = default;
            float largestError = 0;
            Vector3 shapedCentroid = Vector3.zero;
            for (int i = 0; i < vertices.Length; i++)
            {
                // Native Unity meshes store one position per vertex, shared by all influences. Inverting each
                // influence independently and averaging would be incorrect. Invert the weighted affine map itself.
                Matrix4x4 skinWorld = skinned ? WeightedSetupWorld(weights[i], boneSetupWorld, bindPoses) : rendererSetupWorld;
                float determinant = skinWorld.determinant;
                if (!Finite(determinant) || Mathf.Abs(determinant) < .00000001f)
                    throw new InvalidDataException("Singular setup skin matrix: " + path + " vertex " + i);
                Vector3 sourceWorld = skinWorld.MultiplyPoint3x4(vertices[i]);
                Vector3 expectedWorld = affine.MultiplyPoint3x4(sourceWorld);
                if (isPupil)
                {
                    expectedWorld.x = pupilPivot.x + (expectedWorld.x - pupilPivot.x) * PupilScale;
                    expectedWorld.y = pupilPivot.y + (expectedWorld.y - pupilPivot.y) * PupilScale;
                }
                changed[i] = skinWorld.inverse.MultiplyPoint3x4(expectedWorld);
                Vector3 actualWorld = skinWorld.MultiplyPoint3x4(changed[i]);
                float error = Vector3.Distance(actualWorld, expectedWorld);
                if (!Finite(changed[i]) || !Finite(actualWorld) || error > MaxAllowedError)
                    throw new InvalidDataException("Face setup-world mapping failed: " + path + " vertex " + i + "; error=" + error);
                largestError = Mathf.Max(largestError, error);
                shapedCentroid += actualWorld;
                if (i == 0) { before = new Bounds(sourceWorld, Vector3.zero); after = new Bounds(actualWorld, Vector3.zero); }
                else { before.Encapsulate(sourceWorld); after.Encapsulate(actualWorld); }
            }
            float pupilScale = isPupil ? PupilScale : 1;
            Vector3 expectedSize = Vector3.Scale(before.size, new Vector3(HorizontalScale * pupilScale, VerticalScale * pupilScale, 1));
            Vector3 sizeError = after.size - expectedSize;
            float maxSizeError = Mathf.Max(Mathf.Abs(sizeError.x), Mathf.Abs(sizeError.y), Mathf.Abs(sizeError.z));
            float centroidError = isPupil ? Vector3.Distance(shapedCentroid / vertices.Length, pupilPivot) : 0;
            if (maxSizeError > MaxAllowedError || centroidError > MaxAllowedError)
                throw new InvalidDataException("Face combined affine bounds or pupil centroid validation failed: " + path);
            clonedMesh.vertices = changed;
            clonedMesh.RecalculateBounds();
            string afterHash = BindingHash(clonedMesh);
            if (beforeHash != afterHash) throw new InvalidDataException("Face adaptation changed source mesh bindings: " + path);
            return new Validation
            {
                path = path, vertexCount = vertices.Length, boneCount = skinned ? boneSetupWorld.Length : 0,
                skinned = skinned, bindingsUnchanged = true, bindingHashBefore = beforeHash, bindingHashAfter = afterHash,
                pivot = pivot, sourceCenter = before.center, sourceSize = before.size, shapedCenter = after.center, shapedSize = after.size,
                horizontalScale = HorizontalScale, verticalScale = VerticalScale, pupilScale = pupilScale, pupilPivot = pupilPivot,
                maxSetupWorldError = largestError, maxSizeError = maxSizeError, pupilCentroidError = centroidError
            };
        }

        static Matrix4x4 WeightedSetupWorld(BoneWeight weight, Matrix4x4[] bones, Matrix4x4[] bindPoses)
        {
            var result = new Matrix4x4();
            AddInfluence(ref result, weight.boneIndex0, weight.weight0, bones, bindPoses);
            AddInfluence(ref result, weight.boneIndex1, weight.weight1, bones, bindPoses);
            AddInfluence(ref result, weight.boneIndex2, weight.weight2, bones, bindPoses);
            AddInfluence(ref result, weight.boneIndex3, weight.weight3, bones, bindPoses);
            // Native skinning computes xyz directly. Keep the affine row canonical rather than normalizing source weights.
            result.m30 = result.m31 = result.m32 = 0; result.m33 = 1;
            return result;
        }

        static void AddInfluence(ref Matrix4x4 result, int index, float weight, Matrix4x4[] bones, Matrix4x4[] bindPoses)
        {
            if (weight == 0) return;
            if (weight < 0 || !Finite(weight) || index < 0 || index >= bones.Length)
                throw new InvalidDataException("Invalid native face bone influence.");
            var influence = bones[index] * bindPoses[index];
            for (int i = 0; i < 16; i++) result[i] += influence[i] * weight;
        }

        // Hash everything that the adapter is forbidden to change. Vertex positions are deliberately excluded.
        static string BindingHash(Mesh mesh)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(mesh.vertexCount);
                foreach (int index in mesh.triangles) writer.Write(index);
                foreach (var uv in mesh.uv) { writer.Write(uv.x); writer.Write(uv.y); }
                foreach (var weight in mesh.boneWeights)
                {
                    writer.Write(weight.boneIndex0); writer.Write(weight.boneIndex1); writer.Write(weight.boneIndex2); writer.Write(weight.boneIndex3);
                    writer.Write(weight.weight0); writer.Write(weight.weight1); writer.Write(weight.weight2); writer.Write(weight.weight3);
                }
                foreach (var matrix in mesh.bindposes) for (int i = 0; i < 16; i++) writer.Write(matrix[i]);
                writer.Flush();
                using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
            }
        }

        static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        static bool Finite(Vector3 value) { return Finite(value.x) && Finite(value.y) && Finite(value.z); }
    }
}
#endif
