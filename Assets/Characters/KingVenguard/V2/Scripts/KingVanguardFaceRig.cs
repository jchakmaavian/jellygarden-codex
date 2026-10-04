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
        public int ActivePaintedEyeContours { get; private set; }
        public float MinimumIrisScale { get; private set; }
        /// <summary>Current left/right iris scale, for temporal and visual diagnostics.</summary>
        public Vector2 IrisScales { get; private set; }

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
            MinimumIrisScale = 0;
            IrisScales = Vector2.zero;
            ActivePaintedEyeContours = 0;
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
            ActivePaintedEyeContours = 0;
            MinimumIrisScale = float.MaxValue;
            IrisScales = Vector2.zero;
            MaximumToothCorrection = 0;
            for (int i = 0; i < skeleton.Slots.Count; i++)
            {
                var slot = skeleton.Slots.Items[i];
                if (slot.Attachment == null || !slot.Bone.Active) continue;
                var part = AddPart(i, slot.Attachment, slot.Attachment.Name);
                if (part == null) continue;
                part.UpdateWorld();
                if (part.UpdatePaintedContour(face)) ActivePaintedEyeContours++;
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
                Part eye = part.EyeSide < 0 ? leftEye : part.EyeSide > 0 ? rightEye : null;
                if (eye != null)
                {
                    // White, lids, lashes, brows and their shadows share one map.
                    // Its optical center follows the original animated white.
                    var center = eye.PaintedCenterWorld(face);
                    part.Map = global.ScaleAroundMappedPoint(center, configuration.EyeScale.x, configuration.EyeScale.y, face);
                }
                if (part.Feature == Feature.Mouth) part.Map = mouthMap;
                else if (part.Feature == Feature.Nose)
                {
                    var nose = configuration.NoseScale;
                    part.Map = global.ScaleAroundMappedPoint(part.Center, nose.x, nose.y, face);
                }
                else if (part.Feature == Feature.Pupil)
                {
                    if (eye != null)
                    {
                        float irisScale;
                        var target = ContainedPupilCenter(part, eye, out irisScale);
                        MinimumIrisScale = Mathf.Min(MinimumIrisScale, irisScale);
                        var scales = IrisScales;
                        if (part.EyeSide < 0) scales.x = irisScale; else scales.y = irisScale;
                        IrisScales = scales;
                        var eyeMap = part.Map;
                        part.Map = eyeMap.ScaleAroundMappedPoint(part.Center, irisScale, irisScale, face);
                        part.Map = part.Map.Translated(eyeMap.Point(target) - eyeMap.Point(part.Center));
                    }
                }
            }
            GazeInfluence = attention && attention.isActiveAndEnabled ? attention.Influence : 0;
            BodyLeanDegrees = attention && attention.isActiveAndEnabled ? -attention.BodyLean * 2.25f : 0;
            return true;
        }

        Vector2 ContainedPupilCenter(Part pupil, Part white, out float irisScale)
        {
            // The painted iris is oval; transparent quad corners do not define
            // containment. Registered alpha contours are transformed by the same
            // UV triangles as the visible mesh. This runs for SOURCE gaze too.
            irisScale = configuration.PupilScale;
            if (pupil.PaintedHull == null || white.PaintedHull == null)
            {
                // Explicit conservative fallback for an unregistered replacement
                // skin; no texture readability or synchronous GPU readback needed.
                irisScale = Mathf.Min(irisScale, 1.1f);
                return white.Center;
            }
            float px, py;
            face.WorldToLocal(pupil.Center.x, pupil.Center.y, out px, out py);
            var sourceCenter = new Vector2(px, py);
            var neutral = white.PaintedBounds.center;
            var boundary = white.PaintedHull;
            var iris = pupil.PaintedHull;
            float sign = white.PaintedArea >= 0 ? 1 : -1;
            float scaleLimit = float.MaxValue;
            for (int i = 0; i < boundary.Length; i++)
            {
                var a = boundary[i]; var edge = boundary[(i + 1) % boundary.Length] - a;
                var normal = new Vector2(edge.y * sign, -edge.x * sign);
                float support = float.MinValue;
                for (int j = 0; j < iris.Length; j++) support = Mathf.Max(support, Vector2.Dot(normal, iris[j] - sourceCenter));
                if (support > 1e-10f)
                    scaleLimit = Mathf.Min(scaleLimit, Vector2.Dot(normal, a - neutral) / support);
            }
            if (scaleLimit < irisScale) irisScale = Mathf.Max(.01f, scaleLimit * .995f);
            var desired = sourceCenter;
            if (attention && attention.isActiveAndEnabled && attention.Influence > 0)
            {
                var gaze = attention.Gaze;
                var target = neutral + new Vector2(gaze.x * white.PaintedBounds.width * .5f, gaze.y * white.PaintedBounds.height * .5f);
                desired = Vector2.Lerp(sourceCenter, target, attention.Influence);
            }
            // Intersect the center's ray with the eroded white half-planes. The
            // support term accounts for the complete painted iris at this pose.
            var direction = desired - neutral;
            float fraction = 1;
            for (int i = 0; i < boundary.Length; i++)
            {
                var a = boundary[i]; var edge = boundary[(i + 1) % boundary.Length] - a;
                var normal = new Vector2(edge.y * sign, -edge.x * sign);
                float travel = Vector2.Dot(normal, direction);
                if (travel <= 1e-10f) continue;
                float support = float.MinValue;
                for (int j = 0; j < iris.Length; j++) support = Mathf.Max(support, Vector2.Dot(normal, iris[j] - sourceCenter));
                float available = Vector2.Dot(normal, a - neutral) - support * irisScale;
                fraction = Mathf.Min(fraction, available / travel);
            }
            var local = neutral + direction * Mathf.Clamp01(fraction);
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
            // The paired eyes now have independent animated optical centers;
            // their Face feature tag alone no longer means an identical map.
            if (first.EyeSide != second.EyeSide) return true;
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

        static int EyeSideFor(string name)
        {
            switch (name)
            {
                case "Sol Göz": case "Sol  Göz Bebeği": case "Sol Göz kapağı":
                case "Sol Kirpik": case "Sol Kaş": case "Sol Kaş Gölge": return -1;
                case "Sağ Göz": case "Sağ Göz Bebeği": case "Sağ Göz Kapağı":
                case "Sağ Kirpik": case "Sağ Kaş": case "Sağ Kaş Gölge": return 1;
                default: return 0;
            }
        }

        // Registered painted contours in upright cropped-region UV coordinates.
        // Generated from the installed alpha masks; see eye-alpha-registration.json.
        static readonly Dictionary<string, Vector2[]> PaintedEyeContours = new Dictionary<string, Vector2[]>
        {
            { "Sol Göz", new[] { new Vector2(0.067567568f, 0.438775510f), new Vector2(0.121621622f, 0.316326531f), new Vector2(0.175675676f, 0.234693878f), new Vector2(0.229729730f, 0.173469388f), new Vector2(0.256756757f, 0.153061224f), new Vector2(0.337837838f, 0.112244898f), new Vector2(0.391891892f, 0.091836735f), new Vector2(0.608108108f, 0.091836735f), new Vector2(0.662162162f, 0.112244898f), new Vector2(0.743243243f, 0.153061224f), new Vector2(0.797297297f, 0.214285714f), new Vector2(0.851351351f, 0.295918367f), new Vector2(0.878378378f, 0.357142857f), new Vector2(0.905405405f, 0.438775510f), new Vector2(0.905405405f, 0.704081633f), new Vector2(0.878378378f, 0.785714286f), new Vector2(0.851351351f, 0.826530612f), new Vector2(0.770270270f, 0.887755102f), new Vector2(0.662162162f, 0.928571429f), new Vector2(0.364864865f, 0.928571429f), new Vector2(0.283783784f, 0.908163265f), new Vector2(0.202702703f, 0.867346939f), new Vector2(0.148648649f, 0.826530612f), new Vector2(0.094594595f, 0.744897959f), new Vector2(0.067567568f, 0.622448980f) } },
            { "Sağ Göz", new[] { new Vector2(0.071428571f, 0.447916667f), new Vector2(0.128571429f, 0.281250000f), new Vector2(0.242857143f, 0.156250000f), new Vector2(0.271428571f, 0.135416667f), new Vector2(0.385714286f, 0.093750000f), new Vector2(0.614285714f, 0.093750000f), new Vector2(0.671428571f, 0.114583333f), new Vector2(0.814285714f, 0.218750000f), new Vector2(0.842857143f, 0.260416667f), new Vector2(0.900000000f, 0.385416667f), new Vector2(0.928571429f, 0.468750000f), new Vector2(0.928571429f, 0.718750000f), new Vector2(0.900000000f, 0.781250000f), new Vector2(0.842857143f, 0.843750000f), new Vector2(0.814285714f, 0.864583333f), new Vector2(0.700000000f, 0.906250000f), new Vector2(0.614285714f, 0.927083333f), new Vector2(0.385714286f, 0.927083333f), new Vector2(0.300000000f, 0.906250000f), new Vector2(0.242857143f, 0.885416667f), new Vector2(0.128571429f, 0.802083333f), new Vector2(0.100000000f, 0.718750000f), new Vector2(0.071428571f, 0.614583333f) } },
            { "Sol  Göz Bebeği", new[] { new Vector2(0.000000000f, 0.421052632f), new Vector2(0.071428571f, 0.263157895f), new Vector2(0.285714286f, 0.105263158f), new Vector2(0.428571429f, 0.052631579f), new Vector2(0.571428571f, 0.052631579f), new Vector2(0.714285714f, 0.105263158f), new Vector2(0.928571429f, 0.263157895f), new Vector2(0.928571429f, 0.736842105f), new Vector2(0.857142857f, 0.842105263f), new Vector2(0.714285714f, 0.947368421f), new Vector2(0.285714286f, 0.947368421f), new Vector2(0.071428571f, 0.789473684f), new Vector2(0.000000000f, 0.631578947f) } },
            { "Sağ Göz Bebeği", new[] { new Vector2(0.071428571f, 0.277777778f), new Vector2(0.142857143f, 0.166666667f), new Vector2(0.214285714f, 0.111111111f), new Vector2(0.357142857f, 0.055555556f), new Vector2(0.642857143f, 0.055555556f), new Vector2(0.785714286f, 0.111111111f), new Vector2(0.857142857f, 0.166666667f), new Vector2(0.928571429f, 0.277777778f), new Vector2(0.928571429f, 0.666666667f), new Vector2(0.857142857f, 0.833333333f), new Vector2(0.785714286f, 0.888888889f), new Vector2(0.642857143f, 0.944444444f), new Vector2(0.428571429f, 0.944444444f), new Vector2(0.285714286f, 0.888888889f), new Vector2(0.142857143f, 0.777777778f), new Vector2(0.071428571f, 0.666666667f) } },
        };

        sealed class Part
        {
            static readonly int[] RegionTriangles = { 0, 1, 2, 2, 3, 0 };
            public readonly string Name;
            public readonly Feature Feature;
            public readonly int EyeSide;
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
            PaintedBinding[] paintedBindings;
            public Vector2[] PaintedHull;
            public Rect PaintedBounds;
            public float PaintedArea;
            public bool Active;
            public Vector2 Center;
            public Affine Map;

            public Part(Slot slot, Attachment attachment, string name, Skeleton setup)
            {
                this.slot = slot;
                Name = name;
                Feature = KingVanguardFaceShapeModifier.Classify(name);
                EyeSide = EyeSideFor(name);
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
                RegisterPaintedContour(attachment);
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

            void RegisterPaintedContour(Attachment attachment)
            {
                Vector2[] contour;
                if (!PaintedEyeContours.TryGetValue(Name, out contour)) return;
                var hasRegion = attachment as IHasRendererObject;
                var atlas = hasRegion == null ? null : hasRegion.RendererObject as AtlasRegion;
                // Both recovered rigs pack these four islands upright. Any new
                // rotated registration must supply an explicitly checked profile.
                if (atlas == null || atlas.degrees != 0) return;
                var bindings = new PaintedBinding[contour.Length];
                for (int p = 0; p < contour.Length; p++)
                {
                    var uv = new Vector2(Mathf.Lerp(atlas.u, atlas.u2, contour[p].x), Mathf.Lerp(atlas.v, atlas.v2, contour[p].y));
                    bool found = false;
                    for (int t = 0; t < triangles.Length; t += 3)
                    {
                        int a = triangles[t] * 2, b = triangles[t + 1] * 2, c = triangles[t + 2] * 2;
                        float bx = Uvs[b] - Uvs[a], by = Uvs[b + 1] - Uvs[a + 1];
                        float cx = Uvs[c] - Uvs[a], cy = Uvs[c + 1] - Uvs[a + 1];
                        float determinant = bx * cy - by * cx;
                        if (Mathf.Abs(determinant) < 1e-12f) continue;
                        float x = uv.x - Uvs[a], y = uv.y - Uvs[a + 1];
                        float wb = (x * cy - y * cx) / determinant, wc = (bx * y - by * x) / determinant, wa = 1 - wb - wc;
                        if (wa < -1e-5f || wb < -1e-5f || wc < -1e-5f) continue;
                        bindings[p] = new PaintedBinding { A = a, B = b, C = c, WA = wa, WB = wb, WC = wc };
                        found = true; break;
                    }
                    if (!found) return;
                }
                paintedBindings = bindings;
                PaintedHull = new Vector2[bindings.Length];
            }

            public bool UpdatePaintedContour(Bone frame)
            {
                if (paintedBindings == null) return false;
                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                for (int i = 0; i < paintedBindings.Length; i++)
                {
                    var p = paintedBindings[i];
                    float wx = p.WA * World[p.A] + p.WB * World[p.B] + p.WC * World[p.C];
                    float wy = p.WA * World[p.A + 1] + p.WB * World[p.B + 1] + p.WC * World[p.C + 1];
                    float x, y; frame.WorldToLocal(wx, wy, out x, out y);
                    PaintedHull[i] = new Vector2(x, y);
                    minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y); maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
                }
                PaintedBounds = Rect.MinMaxRect(minX, minY, maxX, maxY);
                PaintedArea = 0;
                for (int i = 0; i < PaintedHull.Length; i++)
                { var a = PaintedHull[i]; var b = PaintedHull[(i + 1) % PaintedHull.Length]; PaintedArea += a.x * b.y - a.y * b.x; }
                return true;
            }

            public Vector2 PaintedCenterWorld(Bone frame)
            {
                if (PaintedHull == null) return Center;
                var point = PaintedBounds.center;
                float x, y; frame.LocalToWorld(point.x, point.y, out x, out y);
                return new Vector2(x, y);
            }

            struct PaintedBinding { public int A, B, C; public float WA, WB, WC; }

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
