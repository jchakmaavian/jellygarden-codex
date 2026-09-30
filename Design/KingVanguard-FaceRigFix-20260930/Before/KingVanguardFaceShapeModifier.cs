using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace RoyalThrone.Characters
{
    /// <summary>
    /// Adapts the recovered King/Area head bind geometry to Vanguard's broader face.
    /// Each point is mapped through its setup bone into the common head setup space,
    /// then back through that same bone. Animation bones, UVs, weights, indices and
    /// key times stay unchanged. Matching deform coordinates use the same map.
    /// </summary>
    [CreateAssetMenu(menuName = "Royal Throne/King Vanguard/Face Shape Modifier")]
    public sealed class KingVanguardFaceShapeModifier : SkeletonDataModifierAsset
    {
        [SerializeField] Vector2 faceScale = new Vector2(1.15f, .92f);
        [SerializeField] Vector2 noseScale = new Vector2(1.05f, .85f);
        // 1.55 extended the irises beyond the eye whites during the source gaze
        // motion. 1.1 retains a small enlargement while preserving side margins
        // throughout the portrait activities and Area idle/clap tracks.
        [SerializeField] float pupilScale = 1.1f;
        [SerializeField] float mouthScale = .82f;
        [SerializeField] bool hideObsoleteUpperCrownShadow = true;
        [NonSerialized] ConditionalWeakTable<SkeletonData, object> applied;

        static readonly HashSet<string> FacialAttachments = new HashSet<string>(StringComparer.Ordinal)
        {
            "Alt Diş", "Ağız", "Burun", "Bıyık", "Bıyık Gölge", "Dil",
            "Sakal Gölge", "Sakal Surat", "Sağ Göz", "Sağ Göz Bebeği",
            "Sağ Göz Kapağı", "Sağ Kaş", "Sağ Kaş Gölge", "Sağ Kirpik",
            "Sağ Kulak", "Sağ Yanak", "Sol  Göz Bebeği", "Sol Göz",
            "Sol Göz kapağı", "Sol Kaş", "Sol Kaş Gölge", "Sol Kirpik",
            "Sol Kulak", "Sol Yanak", "Taç", "Taç Alt Gölge", "Taç Gölge Ust", "Ust Diş"
        };

        public override void Apply(SkeletonData data)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (applied == null) applied = new ConditionalWeakTable<SkeletonData, object>();
            object previous;
            if (applied.TryGetValue(data, out previous)) return;
            if (faceScale.x <= 0 || faceScale.y <= 0 || noseScale.x <= 0 || noseScale.y <= 0 || pupilScale <= 0 || mouthScale <= 0)
                throw new InvalidOperationException("King Vanguard face scales must be positive.");

            // These two imported rigs share this head setup. Do not silently adapt
            // a different head (for example the unrelated Nightmare rig).
            var setup = new Skeleton(data);
            setup.SetToSetupPose();
            setup.UpdateWorldTransform();
            var head = setup.FindBone("yuz");
            var face = setup.FindBone("on_yuz");
            if (head == null || face == null)
                throw new InvalidOperationException("Face modifier needs the King/Area yuz + on_yuz setup.");

            // on_yuz supplies the face's horizontal/vertical axes. The head joint
            // supplies the anchor, retaining the source neck/head connection.
            var worldMap = Affine.InFrame(face, head.WorldX, head.WorldY, faceScale.x, faceScale.y);
            // Teeth and tongue must use the mouth's pivot, rather than each part's
            // center, so the new smaller mouth remains one registered assembly.
            Vector2 mouthCenter = FindCenter(setup, "Ağız");
            var mouthMap = worldMap.ScaleAroundMappedPoint(mouthCenter.x, mouthCenter.y, mouthScale, mouthScale, face);
            var edits = new Dictionary<Attachment, Edit>();
            foreach (var skin in data.Skins)
                foreach (var entry in skin.GetAttachments())
                {
                    if (!FacialAttachments.Contains(entry.Name) || edits.ContainsKey(entry.Attachment)) continue;
                    var slot = setup.Slots.Items[entry.SlotIndex];
                    var map = worldMap;
                    if (entry.Name == "Ağız" || entry.Name == "Alt Diş" || entry.Name == "Ust Diş" || entry.Name == "Dil")
                        map = mouthMap;
                    else if (entry.Name == "Burun" || entry.Name == "Sağ Göz Bebeği" || entry.Name == "Sol  Göz Bebeği")
                    {
                        var center = Center(entry.Attachment, slot);
                        bool nose = entry.Name == "Burun";
                        map = map.ScaleAroundMappedPoint(center.x, center.y,
                            nose ? noseScale.x : pupilScale, nose ? noseScale.y : pupilScale, face);
                    }
                    edits.Add(entry.Attachment, new Edit(entry.Attachment, slot.Bone, setup, map));
                }
            if (edits.Count < 27)
                throw new InvalidOperationException("King face registration is incomplete: " + edits.Count + " attachments.");

            // Spine 3.8 unweighted deform arrays contain absolute local positions;
            // weighted arrays contain offsets per influence. Clone before changing
            // geometry: zero-deform frames may alias the attachment setup vertices.
            foreach (var animation in data.Animations)
                foreach (var timeline in animation.Timelines)
                {
                    var deform = timeline as DeformTimeline;
                    Edit edit;
                    if (deform == null || !edits.TryGetValue(deform.Attachment, out edit)) continue;
                    var frames = new float[deform.Vertices.Length][];
                    for (int i = 0; i < frames.Length; i++)
                        frames[i] = edit.TransformDeform(deform.Vertices[i]);
                    deform.Vertices = frames;
                }
            foreach (var edit in edits.Values) edit.ApplySetup();
            if (hideObsoleteUpperCrownShadow)
                foreach (var skin in data.Skins)
                    foreach (var entry in skin.GetAttachments())
                        if (entry.Name == "Taç Gölge Ust")
                        {
                            // The redesigned lower crown no longer covers this
                            // original upper oval. Attachment alpha is multiplied
                            // by slot alpha; slot color keys cannot restore it.
                            var region = entry.Attachment as RegionAttachment;
                            var mesh = entry.Attachment as MeshAttachment;
                            if (region != null) region.A = 0;
                            if (mesh != null) mesh.A = 0;
                        }
            applied.Add(data, new object());
        }

        static Vector2 FindCenter(Skeleton setup, string name)
        {
            foreach (var skin in setup.Data.Skins)
                foreach (var entry in skin.GetAttachments())
                    if (entry.Name == name) return Center(entry.Attachment, setup.Slots.Items[entry.SlotIndex]);
            throw new InvalidOperationException("Missing registered facial attachment: " + name);
        }

        static Vector2 Center(Attachment attachment, Slot slot)
        {
            float[] vertices;
            var region = attachment as RegionAttachment;
            var mesh = attachment as VertexAttachment;
            if (region != null)
            {
                vertices = new float[8];
                region.ComputeWorldVertices(slot.Bone, vertices, 0);
            }
            else if (mesh != null)
            {
                vertices = new float[mesh.WorldVerticesLength];
                mesh.ComputeWorldVertices(slot, vertices);
            }
            else throw new InvalidOperationException("Unsupported facial center: " + attachment.Name);
            float cx = 0, cy = 0;
            for (int i = 0; i < vertices.Length; i += 2) { cx += vertices[i]; cy += vertices[i + 1]; }
            return new Vector2(cx * 2 / vertices.Length, cy * 2 / vertices.Length);
        }

        sealed class Edit
        {
            readonly Attachment attachment;
            readonly Affine slotMap;
            readonly Affine[] influences;

            public Edit(Attachment attachment, Bone slotBone, Skeleton setup, Affine worldMap)
            {
                this.attachment = attachment;
                slotMap = worldMap.InBone(slotBone);
                var vertex = attachment as VertexAttachment;
                if (vertex == null || vertex.Bones == null) return;
                var maps = new List<Affine>();
                var bones = vertex.Bones;
                for (int i = 0; i < bones.Length;)
                {
                    int count = bones[i++];
                    for (int j = 0; j < count; j++) maps.Add(worldMap.InBone(setup.Bones.Items[bones[i++]]));
                }
                influences = maps.ToArray();
                if (vertex.Vertices.Length != influences.Length * 3)
                    throw new InvalidOperationException("Invalid weighted face geometry: " + attachment.Name);
            }

            public float[] TransformDeform(float[] input)
            {
                var output = (float[])input.Clone();
                if (influences == null)
                {
                    for (int i = 0; i < output.Length; i += 2) slotMap.Point(output, i);
                }
                else
                {
                    if (output.Length != influences.Length * 2)
                        throw new InvalidOperationException("Invalid weighted face deformation: " + attachment.Name);
                    for (int i = 0; i < influences.Length; i++) influences[i].Vector(output, i * 2);
                }
                return output;
            }

            public void ApplySetup()
            {
                var region = attachment as RegionAttachment;
                if (region != null)
                {
                    // A rotated region can become a parallelogram; directly retain
                    // its four affine-mapped corners instead of losing shear by
                    // reducing the result to RegionAttachment scale + rotation.
                    for (int i = 0; i < region.Offset.Length; i += 2) slotMap.Point(region.Offset, i);
                    return;
                }
                var vertex = attachment as VertexAttachment;
                if (vertex == null) throw new InvalidOperationException("Unsupported facial attachment: " + attachment.Name);
                var output = (float[])vertex.Vertices.Clone();
                if (influences == null)
                    for (int i = 0; i < output.Length; i += 2) slotMap.Point(output, i);
                else
                    for (int i = 0; i < influences.Length; i++) influences[i].Point(output, i * 3);
                vertex.Vertices = output;
            }
        }

        struct Affine
        {
            public float a, b, c, d, x, y;

            public static Affine InFrame(Bone frame, float anchorX, float anchorY, float sx, float sy)
            {
                float det = frame.A * frame.D - frame.B * frame.C;
                if (Math.Abs(det) < 1e-8f) throw new InvalidOperationException("Degenerate face setup frame.");
                float inv = 1f / det;
                var result = new Affine
                {
                    a = (frame.A * sx * frame.D - frame.B * sy * frame.C) * inv,
                    b = (frame.B * sy * frame.A - frame.A * sx * frame.B) * inv,
                    c = (frame.C * sx * frame.D - frame.D * sy * frame.C) * inv,
                    d = (frame.D * sy * frame.A - frame.C * sx * frame.B) * inv
                };
                result.x = anchorX - result.a * anchorX - result.b * anchorY;
                result.y = anchorY - result.c * anchorX - result.d * anchorY;
                return result;
            }

            public Affine ScaleAroundMappedPoint(float px, float py, float sx, float sy, Bone frame)
            {
                float cx = a * px + b * py + x, cy = c * px + d * py + y;
                var next = InFrame(frame, cx, cy, sx, sy);
                return new Affine
                {
                    a = next.a * a + next.b * c, b = next.a * b + next.b * d,
                    c = next.c * a + next.d * c, d = next.c * b + next.d * d,
                    x = next.a * x + next.b * y + next.x, y = next.c * x + next.d * y + next.y
                };
            }

            public Affine InBone(Bone bone)
            {
                float det = bone.A * bone.D - bone.B * bone.C;
                if (Math.Abs(det) < 1e-8f) throw new InvalidOperationException("Degenerate facial setup bone: " + bone.Data.Name);
                float inv = 1f / det;
                float la = a * bone.A + b * bone.C, lb = a * bone.B + b * bone.D;
                float lc = c * bone.A + d * bone.C, ld = c * bone.B + d * bone.D;
                float tx = a * bone.WorldX + b * bone.WorldY + x - bone.WorldX;
                float ty = c * bone.WorldX + d * bone.WorldY + y - bone.WorldY;
                return new Affine
                {
                    a = (bone.D * la - bone.B * lc) * inv, b = (bone.D * lb - bone.B * ld) * inv,
                    c = (bone.A * lc - bone.C * la) * inv, d = (bone.A * ld - bone.C * lb) * inv,
                    x = (bone.D * tx - bone.B * ty) * inv, y = (bone.A * ty - bone.C * tx) * inv
                };
            }

            public void Point(float[] v, int i)
            {
                float px = v[i], py = v[i + 1];
                v[i] = a * px + b * py + x; v[i + 1] = c * px + d * py + y;
            }

            public void Vector(float[] v, int i)
            {
                float px = v[i], py = v[i + 1];
                v[i] = a * px + b * py; v[i + 1] = c * px + d * py;
            }
        }
    }
}
