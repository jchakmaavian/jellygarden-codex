using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;
using Feature = RoyalThrone.Characters.KingVanguardFaceShapeModifier.Feature;

namespace RoyalThrone.Characters
{
    /// <summary>
    /// Maps final, originally skinned facial vertices in the CURRENT animated face
    /// frame. Never changes Spine attachments, Slot.Deform, bones or animation state.
    /// UV plus source-position ownership also identifies interpolated clipped vertices.
    /// </summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class KingVanguardFaceRig : MonoBehaviour
    {
        SkeletonGraphic graphic;
        SkeletonAnimation animation;
        MeshFilter meshFilter;
        Skeleton skeleton;
        Skeleton setupSkeleton;
        KingVanguardFaceShapeModifier configuration;
        KingVanguardAttention attention;
        Bone head, face;
        Bone chest;
        [SerializeField] bool stabilizeTeeth = true;
        bool subscribed;
        Dictionary<Attachment, Part>[] partsBySlot;
        readonly List<Part> activeParts = new List<Part>(100);
        readonly Dictionary<Vector2, List<VertexOwner>> uvOwners = new Dictionary<Vector2, List<VertexOwner>>(2048);
        readonly List<Bounds> pendingBounds = new List<Bounds>(8);
        int mapped, interpolated, ambiguous, unmatched;

        public int LastMappedVertices { get; private set; }
        public int LastInterpolatedVertices { get; private set; }
        public int LastAmbiguousVertices { get; private set; }
        /// <summary>Includes non-facial clipped vertices; this alone is not a registration failure.</summary>
        public int LastUnattributedVertices { get; private set; }
        public int ActiveFaceAttachments { get; private set; }
        public bool IsRegistered { get { return skeleton != null && configuration != null; } }
        public bool StabilizeTeeth { get { return stabilizeTeeth; } set { stabilizeTeeth = value; } }
        /// <summary>Maximum nonrigid tooth correction, in skeleton units, for the current pose.</summary>
        public float MaximumToothCorrection { get; private set; }
        public float GazeInfluence { get; private set; }
        public float BodyLeanDegrees { get; private set; }

        public static KingVanguardFaceRig Ensure(SkeletonGraphic target)
        {
            if (!target) return null;
            return EnsureOn(target.gameObject);
        }

        public static KingVanguardFaceRig Ensure(SkeletonAnimation target)
        {
            if (!target) return null;
            return EnsureOn(target.gameObject);
        }

        static KingVanguardFaceRig EnsureOn(GameObject target)
        {
            var component = target.GetComponent<KingVanguardFaceRig>();
            if (!component) component = target.AddComponent<KingVanguardFaceRig>();
            if (component.isActiveAndEnabled) component.Bind();
            return component;
        }

        void OnEnable() { Bind(); }
        void OnDisable() { Unbind(); }
        void OnDestroy() { Unbind(); }

        void Bind()
        {
            if (subscribed) return;
            graphic = GetComponent<SkeletonGraphic>();
            animation = graphic ? null : GetComponent<SkeletonAnimation>();
            if (graphic)
            {
                attention = KingVanguardAttention.Ensure(graphic);
                graphic.OnRebuild += GraphicRebuilt;
                graphic.OnPostProcessVertices += MapVertices;
                graphic.OnMeshAndMaterialsUpdated += GraphicMeshUpdated;
            }
            else if (animation)
            {
                meshFilter = animation.GetComponent<MeshFilter>();
                animation.OnRebuild += AnimationRebuilt;
                animation.OnPostProcessVertices += MapVertices;
                animation.OnMeshAndMaterialsUpdated += AnimationMeshUpdated;
            }
            else return;
            subscribed = true;
            ResetRegistration();
        }

        void Unbind()
        {
            if (subscribed && graphic)
            {
                graphic.OnRebuild -= GraphicRebuilt;
                graphic.OnPostProcessVertices -= MapVertices;
                graphic.OnMeshAndMaterialsUpdated -= GraphicMeshUpdated;
            }
            if (subscribed && animation)
            {
                animation.OnRebuild -= AnimationRebuilt;
                animation.OnPostProcessVertices -= MapVertices;
                animation.OnMeshAndMaterialsUpdated -= AnimationMeshUpdated;
            }
            subscribed = false;
            ResetRegistration();
        }

        void GraphicRebuilt(SkeletonGraphic _) { ResetRegistration(); }
        void AnimationRebuilt(SkeletonRenderer _) { ResetRegistration(); }

        void ResetRegistration()
        {
            skeleton = null;
            setupSkeleton = null;
            configuration = null;
            partsBySlot = null;
            activeParts.Clear();
            uvOwners.Clear();
            pendingBounds.Clear();
            head = face = chest = null;
            MaximumToothCorrection = GazeInfluence = BodyLeanDegrees = 0;
            mapped = interpolated = ambiguous = unmatched = 0;
            LastMappedVertices = LastInterpolatedVertices = LastAmbiguousVertices = LastUnattributedVertices = ActiveFaceAttachments = 0;
        }

        bool Register(Skeleton current)
        {
            if (current == null) return false;
            if (ReferenceEquals(current, skeleton)) return configuration != null;
            ResetRegistration();
            if (!KingVanguardFaceShapeModifier.TryGet(current.Data, out configuration)) return false;
            skeleton = current;
            head = current.FindBone("yuz");
            face = current.FindBone("on_yuz");
            chest = current.FindBone("pelerin") ?? current.RootBone;
            if (head == null || face == null) { configuration = null; return false; }
            setupSkeleton = new Skeleton(current.Data);
            setupSkeleton.SetToSetupPose();
            setupSkeleton.UpdateWorldTransform();
            if (graphic && !attention) attention = KingVanguardAttention.Ensure(graphic);
            partsBySlot = new Dictionary<Attachment, Part>[current.Slots.Count];
            for (int i = 0; i < partsBySlot.Length; i++) partsBySlot[i] = new Dictionary<Attachment, Part>();
            foreach (var skin in current.Data.Skins)
                foreach (var entry in skin.GetAttachments()) AddPart(entry.SlotIndex, entry.Attachment, entry.Name);
            return true;
        }

        Part AddPart(int slotIndex, Attachment attachment, string name)
        {
            if (!(attachment is RegionAttachment) && !(attachment is MeshAttachment)) return null;
            var slotParts = partsBySlot[slotIndex];
            Part part;
            if (slotParts.TryGetValue(attachment, out part)) return part;
            part = new Part(skeleton.Slots.Items[slotIndex], attachment, name, setupSkeleton);
            slotParts.Add(attachment, part);
            for (int i = 0; i < part.Uvs.Length; i += 2)
            {
                var uv = new Vector2(part.Uvs[i], part.Uvs[i + 1]);
                List<VertexOwner> owners;
                if (!uvOwners.TryGetValue(uv, out owners)) { owners = new List<VertexOwner>(1); uvOwners.Add(uv, owners); }
                owners.Add(new VertexOwner { Part = part, Coordinate = i });
            }
            return part;
        }

        bool PrepareParts()
        {
            if (Mathf.Abs(face.A * face.D - face.B * face.C) < 1e-8f) return false;
            foreach (var part in activeParts) part.Active = false;
            activeParts.Clear();
            Part mouth = null;
            Part leftEye = null, rightEye = null;
            ActiveFaceAttachments = 0;
            MaximumToothCorrection = 0;
            for (int i = 0; i < skeleton.Slots.Count; i++)
            {
                var slot = skeleton.Slots.Items[i];
                if (slot.Attachment == null || !slot.Bone.Active) continue;
                var part = AddPart(i, slot.Attachment, slot.Attachment.Name);
                if (part == null) continue;
                part.UpdateWorld();
                part.Active = true;
                activeParts.Add(part);
                if (part.Feature != Feature.None) ActiveFaceAttachments++;
                if (part.Name == "Ağız") mouth = part;
                else if (part.Name == "Sol Göz") leftEye = part;
                else if (part.Name == "Sağ Göz") rightEye = part;
                MaximumToothCorrection = Mathf.Max(MaximumToothCorrection, part.PrepareTeeth(face, stabilizeTeeth));
            }
            var scale = configuration.FaceScale;
            var global = Affine.InFrame(face, head.WorldX, head.WorldY, scale.x, scale.y);
            var mouthMap = mouth == null ? global : global.ScaleAroundMappedPoint(mouth.Center,
                configuration.MouthScale, configuration.MouthScale, face);
            foreach (var part in activeParts)
            {
                part.Map = global;
                if (part.Feature == Feature.Mouth) part.Map = mouthMap;
                else if (part.Feature == Feature.Nose)
                {
                    var nose = configuration.NoseScale;
                    part.Map = global.ScaleAroundMappedPoint(part.Center, nose.x, nose.y, face);
                }
                else if (part.Feature == Feature.Pupil)
                {
                    part.Map = global.ScaleAroundMappedPoint(part.Center, configuration.PupilScale, configuration.PupilScale, face);
                    var eye = part.Name == "Sol  Göz Bebeği" ? leftEye : rightEye;
                    if (attention && attention.isActiveAndEnabled && eye != null && attention.Influence > 0)
                    {
                        var target = PupilTarget(part, eye, attention.Gaze);
                        var delta = (global.Point(target) - global.Point(part.Center)) * attention.Influence;
                        part.Map = part.Map.Translated(delta);
                    }
                }
            }
            GazeInfluence = attention && attention.isActiveAndEnabled ? attention.Influence : 0;
            BodyLeanDegrees = attention && attention.isActiveAndEnabled ? -attention.BodyLean * 2.25f : 0;
            return true;
        }

        Vector2 PupilTarget(Part pupil, Part white, Vector2 gaze)
        {
            // Fit the whole pupil quad into a conservative ellipse INSIDE the eye
            // white, then blend toward it. Source lids still draw above the iris,
            // retaining their blink occlusion rather than opening a closed eye.
            var eye = white.BoundsInFrame(face);
            var iris = pupil.BoundsInFrame(face);
            float rx = eye.width * .46f, ry = eye.height * .46f;
            float hx = iris.width * .5f * configuration.PupilScale;
            float hy = iris.height * .5f * configuration.PupilScale;
            float dx = gaze.x * rx * .6f, dy = gaze.y * ry * .6f;
            float low = 0, high = 1;
            for (int i = 0; i < 12; i++)
            {
                float amount = (low + high) * .5f;
                float x = (Mathf.Abs(dx * amount) + hx) / Mathf.Max(rx, 1e-6f);
                float y = (Mathf.Abs(dy * amount) + hy) / Mathf.Max(ry, 1e-6f);
                if (x * x + y * y <= 1) low = amount; else high = amount;
            }
            var local = eye.center + new Vector2(dx, dy) * low;
            float wx, wy;
            face.LocalToWorld(local.x, local.y, out wx, out wy);
            return new Vector2(wx, wy);
        }

        void MapVertices(MeshGeneratorBuffers buffers)
        {
            var current = graphic ? graphic.Skeleton : animation ? animation.Skeleton : null;
            if (!Register(current) || !PrepareParts()) return;
            float scale = 1;
            if (graphic)
                scale = graphic.canvas ? graphic.canvas.referencePixelsPerUnit : graphic.allowMultipleCanvasRenderers ? 100 : 1;
            if (Mathf.Abs(scale) < 1e-8f) return;
            float inverseScale = 1 / scale;
            var vertices = buffers.vertexBuffer;
            var uvs = buffers.uvBuffer;
            bool bodyActive = attention && attention.isActiveAndEnabled && attention.HasAttention;
            float angle = BodyLeanDegrees * Mathf.Deg2Rad;
            float cosine = Mathf.Cos(angle), sine = Mathf.Sin(angle);
            float tap = bodyActive ? attention.TapResponse : 0;
            for (int i = 0; i < buffers.vertexCount; i++)
            {
                var point = new Vector2(vertices[i].x * inverseScale, vertices[i].y * inverseScale);
                bool conflict;
                var owner = ExactOwner(uvs[i], point, out conflict);
                bool fallback = false;
                if (owner == null && !conflict)
                {
                    owner = TriangleOwner(uvs[i], point, out conflict);
                    fallback = true;
                }
                if (conflict) ambiguous++;
                else if (owner == null) unmatched++;
                else if (owner.Feature != Feature.None)
                {
                    Vector2 toothPoint;
                    if (owner.TryToothPoint(uvs[i], out toothPoint)) point = toothPoint;
                    point = owner.Map.Point(point);
                    mapped++;
                    if (fallback) interpolated++;
                }
                else if (!bodyActive) continue;
                // A tiny complete-character response follows attention, after all
                // face maps, without changing the source root/bones or draw order.
                if (bodyActive)
                {
                    float x = (point.x - chest.WorldX) * (1 + tap * .004f);
                    float y = (point.y - chest.WorldY) * (1 - tap * .008f);
                    point = new Vector2(chest.WorldX + cosine * x - sine * y, chest.WorldY + sine * x + cosine * y);
                }
                vertices[i].x = point.x * scale;
                vertices[i].y = point.y * scale;
            }
            var bounds = new Bounds();
            if (buffers.vertexCount > 0)
            {
                var min = vertices[0];
                var max = min;
                for (int i = 1; i < buffers.vertexCount; i++) { min = Vector3.Min(min, vertices[i]); max = Vector3.Max(max, vertices[i]); }
                bounds.SetMinMax(min, max);
            }
            pendingBounds.Add(bounds);
        }

        Part ExactOwner(Vector2 uv, Vector2 point, out bool conflict)
        {
            conflict = false;
            List<VertexOwner> candidates;
            if (!uvOwners.TryGetValue(uv, out candidates)) return null;
            Part found = null;
            foreach (var candidate in candidates)
            {
                var part = candidate.Part;
                int i = candidate.Coordinate;
                if (!part.Active || !Matches(point, part.World[i], part.World[i + 1])) continue;
                if (DifferentMapping(found, part)) { conflict = true; return null; }
                found = part;
            }
            return found;
        }

        Part TriangleOwner(Vector2 uv, Vector2 point, out bool conflict)
        {
            conflict = false;
            Part found = null;
            foreach (var part in activeParts)
            {
                if (!part.Contains(uv, point)) continue;
                if (DifferentMapping(found, part)) { conflict = true; return null; }
                found = part;
            }
            return found;
        }

        static bool DifferentMapping(Part first, Part second)
        {
            if (first == null || ReferenceEquals(first, second)) return false;
            if (first.Feature != second.Feature) return true;
            // Both pupils have the same scale but different animated centers.
            return first.Feature == Feature.Pupil || first.Feature == Feature.Nose;
        }

        static bool Matches(Vector2 point, float x, float y)
        {
            float tolerance = 1e-5f + Mathf.Max(Mathf.Abs(point.x), Mathf.Abs(point.y)) * 2e-6f;
            return Mathf.Abs(point.x - x) <= tolerance && Mathf.Abs(point.y - y) <= tolerance;
        }

        void GraphicMeshUpdated(SkeletonGraphic source)
        {
            if (pendingBounds.Count > 0)
            {
                if (!source.allowMultipleCanvasRenderers)
                {
                    var mesh = source.GetLastMesh();
                    if (mesh) { mesh.bounds = pendingBounds[0]; source.canvasRenderer.SetMesh(mesh); }
                }
                else
                {
                    // This Spine version does not expose its multi-renderer meshes.
                    // Unity 6 exposes the assigned mesh through CanvasRenderer.GetMesh;
                    // never reflect private fields or change the UI RectTransform.
                    int count = Mathf.Min(pendingBounds.Count, source.canvasRenderers.Count);
                    for (int i = 0; i < count; i++)
                    {
                        var renderer = source.canvasRenderers[i];
                        var mesh = renderer.GetMesh();
                        if (!mesh) continue;
                        mesh.bounds = pendingBounds[i];
                        renderer.SetMesh(mesh);
                    }
                }
            }
            CompleteMesh();
        }

        void AnimationMeshUpdated(SkeletonRenderer source)
        {
            if (pendingBounds.Count > 0 && meshFilter && meshFilter.sharedMesh)
                meshFilter.sharedMesh.bounds = pendingBounds[0];
            CompleteMesh();
        }

        void CompleteMesh()
        {
            LastMappedVertices = mapped;
            LastInterpolatedVertices = interpolated;
            LastAmbiguousVertices = ambiguous;
            LastUnattributedVertices = unmatched;
            mapped = interpolated = ambiguous = unmatched = 0;
            pendingBounds.Clear();
        }

        struct VertexOwner { public Part Part; public int Coordinate; }

        sealed class Part
        {
            static readonly int[] RegionTriangles = { 0, 1, 2, 2, 3, 0 };
            public readonly string Name;
            public readonly Feature Feature;
            public readonly float[] Uvs, World;
            readonly int[] triangles;
            readonly Slot slot;
            readonly RegionAttachment region;
            readonly MeshAttachment mesh;
            readonly Rect uvBounds;
            readonly float[] restTeeth, currentTeeth, stableTeeth;
            readonly Vector2 restCenter;
            readonly float restDenominator;
            bool teethPrepared;
            public bool Active;
            public Vector2 Center;
            public Affine Map;

            public Part(Slot slot, Attachment attachment, string name, Skeleton setup)
            {
                this.slot = slot;
                Name = name;
                Feature = KingVanguardFaceShapeModifier.Classify(name);
                region = attachment as RegionAttachment;
                mesh = attachment as MeshAttachment;
                Uvs = region != null ? region.UVs : mesh.UVs;
                World = new float[region != null ? 8 : mesh.WorldVerticesLength];
                triangles = region != null ? RegionTriangles : mesh.Triangles;
                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                for (int i = 0; i < Uvs.Length; i += 2)
                {
                    minX = Mathf.Min(minX, Uvs[i]); maxX = Mathf.Max(maxX, Uvs[i]);
                    minY = Mathf.Min(minY, Uvs[i + 1]); maxY = Mathf.Max(maxY, Uvs[i + 1]);
                }
                uvBounds = Rect.MinMaxRect(minX, minY, maxX, maxY);
                if (mesh != null && (name == "Alt Diş" || name == "Ust Diş"))
                {
                    restTeeth = new float[World.Length];
                    currentTeeth = new float[World.Length];
                    stableTeeth = new float[World.Length];
                    mesh.ComputeWorldVertices(setup.Slots.Items[slot.Data.Index], restTeeth);
                    var setupFace = setup.FindBone("on_yuz");
                    float x = 0, y = 0;
                    for (int i = 0; i < restTeeth.Length; i += 2)
                    {
                        float fx, fy;
                        setupFace.WorldToLocal(restTeeth[i], restTeeth[i + 1], out fx, out fy);
                        restTeeth[i] = fx; restTeeth[i + 1] = fy; x += fx; y += fy;
                    }
                    restCenter = new Vector2(x * 2 / restTeeth.Length, y * 2 / restTeeth.Length);
                    for (int i = 0; i < restTeeth.Length; i += 2)
                    {
                        x = restTeeth[i] - restCenter.x; y = restTeeth[i + 1] - restCenter.y;
                        restDenominator += x * x + y * y;
                    }
                }
            }

            public void UpdateWorld()
            {
                if (region != null) region.ComputeWorldVertices(slot.Bone, World, 0);
                else mesh.ComputeWorldVertices(slot, World);
                float x = 0, y = 0;
                for (int i = 0; i < World.Length; i += 2) { x += World[i]; y += World[i + 1]; }
                Center = new Vector2(x * 2 / World.Length, y * 2 / World.Length);
            }

            public Rect BoundsInFrame(Bone frame)
            {
                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                for (int i = 0; i < World.Length; i += 2)
                {
                    float x, y; frame.WorldToLocal(World[i], World[i + 1], out x, out y);
                    minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
                }
                return Rect.MinMaxRect(minX, minY, maxX, maxY);
            }

            public float PrepareTeeth(Bone frame, bool enabled)
            {
                teethPrepared = enabled && restTeeth != null && restDenominator > 1e-12f;
                if (!teethPrepared) return 0;
                float cx = 0, cy = 0;
                for (int i = 0; i < World.Length; i += 2)
                {
                    float x, y; frame.WorldToLocal(World[i], World[i + 1], out x, out y);
                    currentTeeth[i] = x; currentTeeth[i + 1] = y; cx += x; cy += y;
                }
                cx *= 2f / World.Length; cy *= 2f / World.Length;
                float a = 0, b = 0;
                for (int i = 0; i < World.Length; i += 2)
                {
                    float x = restTeeth[i] - restCenter.x, y = restTeeth[i + 1] - restCenter.y;
                    a += x * (currentTeeth[i] - cx) + y * (currentTeeth[i + 1] - cy);
                    b += x * (currentTeeth[i + 1] - cy) - y * (currentTeeth[i] - cx);
                }
                a /= restDenominator; b /= restDenominator;
                float maximum = 0;
                for (int i = 0; i < World.Length; i += 2)
                {
                    float x = restTeeth[i] - restCenter.x, y = restTeeth[i + 1] - restCenter.y;
                    frame.LocalToWorld(a * x - b * y + cx, b * x + a * y + cy, out stableTeeth[i], out stableTeeth[i + 1]);
                    x = stableTeeth[i] - World[i]; y = stableTeeth[i + 1] - World[i + 1];
                    maximum = Mathf.Max(maximum, Mathf.Sqrt(x * x + y * y));
                }
                return maximum;
            }

            public bool TryToothPoint(Vector2 uv, out Vector2 point)
            {
                point = Vector2.zero;
                if (!teethPrepared) return false;
                for (int i = 0; i < Uvs.Length; i += 2)
                    if (uv.x == Uvs[i] && uv.y == Uvs[i + 1])
                    { point = new Vector2(stableTeeth[i], stableTeeth[i + 1]); return true; }
                // Interpolated UVs (e.g. clipping) use the same stable triangle.
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int a = triangles[i] * 2, b = triangles[i + 1] * 2, c = triangles[i + 2] * 2;
                    float bx = Uvs[b] - Uvs[a], by = Uvs[b + 1] - Uvs[a + 1];
                    float cx = Uvs[c] - Uvs[a], cy = Uvs[c + 1] - Uvs[a + 1];
                    float determinant = bx * cy - by * cx;
                    if (Mathf.Abs(determinant) < 1e-12f) continue;
                    float px = uv.x - Uvs[a], py = uv.y - Uvs[a + 1];
                    float wb = (px * cy - py * cx) / determinant;
                    float wc = (bx * py - by * px) / determinant;
                    float wa = 1 - wb - wc;
                    if (wa < -1e-5f || wb < -1e-5f || wc < -1e-5f) continue;
                    point = new Vector2(wa * stableTeeth[a] + wb * stableTeeth[b] + wc * stableTeeth[c],
                        wa * stableTeeth[a + 1] + wb * stableTeeth[b + 1] + wc * stableTeeth[c + 1]);
                    return true;
                }
                return false;
            }

            public bool Contains(Vector2 uv, Vector2 point)
            {
                const float epsilon = 1e-5f;
                if (uv.x < uvBounds.xMin - epsilon || uv.x > uvBounds.xMax + epsilon ||
                    uv.y < uvBounds.yMin - epsilon || uv.y > uvBounds.yMax + epsilon) return false;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int a = triangles[i] * 2, b = triangles[i + 1] * 2, c = triangles[i + 2] * 2;
                    float bx = Uvs[b] - Uvs[a], by = Uvs[b + 1] - Uvs[a + 1];
                    float cx = Uvs[c] - Uvs[a], cy = Uvs[c + 1] - Uvs[a + 1];
                    float determinant = bx * cy - by * cx;
                    if (Mathf.Abs(determinant) < 1e-12f) continue;
                    float px = uv.x - Uvs[a], py = uv.y - Uvs[a + 1];
                    float wb = (px * cy - py * cx) / determinant;
                    float wc = (bx * py - by * px) / determinant;
                    float wa = 1 - wb - wc;
                    if (wa < -epsilon || wb < -epsilon || wc < -epsilon) continue;
                    if (Matches(point, wa * World[a] + wb * World[b] + wc * World[c],
                        wa * World[a + 1] + wb * World[b + 1] + wc * World[c + 1])) return true;
                }
                return false;
            }
        }

        struct Affine
        {
            float a, b, c, d, x, y;
            public Vector2 Point(Vector2 p) { return new Vector2(a * p.x + b * p.y + x, c * p.x + d * p.y + y); }
            public Affine Translated(Vector2 offset) { var result = this; result.x += offset.x; result.y += offset.y; return result; }
            public static Affine InFrame(Bone frame, float anchorX, float anchorY, float sx, float sy)
            {
                float inverse = 1 / (frame.A * frame.D - frame.B * frame.C);
                var result = new Affine {
                    a = (frame.A * sx * frame.D - frame.B * sy * frame.C) * inverse,
                    b = (frame.B * sy * frame.A - frame.A * sx * frame.B) * inverse,
                    c = (frame.C * sx * frame.D - frame.D * sy * frame.C) * inverse,
                    d = (frame.D * sy * frame.A - frame.C * sx * frame.B) * inverse
                };
                result.x = anchorX - result.a * anchorX - result.b * anchorY;
                result.y = anchorY - result.c * anchorX - result.d * anchorY;
                return result;
            }
            public Affine ScaleAroundMappedPoint(Vector2 point, float sx, float sy, Bone frame)
            {
                var center = Point(point);
                var next = InFrame(frame, center.x, center.y, sx, sy);
                return new Affine {
                    a = next.a * a + next.b * c, b = next.a * b + next.b * d,
                    c = next.c * a + next.d * c, d = next.c * b + next.d * d,
                    x = next.a * x + next.b * y + next.x, y = next.c * x + next.d * y + next.y
                };
            }
        }
    }
}
