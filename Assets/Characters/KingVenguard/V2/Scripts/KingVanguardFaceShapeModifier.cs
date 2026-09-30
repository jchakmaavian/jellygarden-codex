using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace RoyalThrone.Characters
{
    /// <summary>Registers proportions without changing recovered geometry, weights or
    /// animation keys. KingVanguardFaceRig applies them after skinning in the animated head frame.</summary>
    [CreateAssetMenu(menuName = "Royal Throne/King Vanguard/Face Shape Modifier")]
    public sealed class KingVanguardFaceShapeModifier : SkeletonDataModifierAsset
    {
        [SerializeField] Vector2 faceScale = new Vector2(1.15f, .92f);
        [SerializeField] Vector2 noseScale = new Vector2(1.05f, .85f);
        [SerializeField] float pupilScale = 1.1f;
        [SerializeField] float mouthScale = 1f;
        [SerializeField] bool hideObsoleteUpperCrownShadow = true;
        [SerializeField] bool hideObsoleteBrowShadows;
        [SerializeField] bool hideObsoleteMoustacheShadow;
        [SerializeField, Range(0, 1)] float browShadowOpacity = .3f;
        [SerializeField, Range(0, 1)] float moustacheShadowOpacity = .32f;
        [SerializeField, Range(0, 1)] float beardShadowOpacity = .45f;
        [SerializeField, Range(0, 1)] float crownShadowOpacity = .42f;
        static readonly ConditionalWeakTable<SkeletonData, KingVanguardFaceShapeModifier> Registered =
            new ConditionalWeakTable<SkeletonData, KingVanguardFaceShapeModifier>();
        static readonly HashSet<string> FacialAttachments = new HashSet<string>(StringComparer.Ordinal)
        {
            "Alt Diş", "Ağız", "Burun", "Bıyık", "Bıyık Gölge", "Dil",
            "Sakal Gölge", "Sakal Surat", "Sağ Göz", "Sağ Göz Bebeği",
            "Sağ Göz Kapağı", "Sağ Kaş", "Sağ Kaş Gölge", "Sağ Kirpik",
            "Sağ Kulak", "Sağ Yanak", "Sol  Göz Bebeği", "Sol Göz",
            "Sol Göz kapağı", "Sol Kaş", "Sol Kaş Gölge", "Sol Kirpik",
            "Sol Kulak", "Sol Yanak", "Taç", "Taç Alt Gölge", "Taç Gölge Ust", "Ust Diş"
        };
        public Vector2 FaceScale { get { return faceScale; } }
        public Vector2 NoseScale { get { return noseScale; } }
        public float PupilScale { get { return pupilScale; } }
        public float MouthScale { get { return mouthScale; } }
        internal enum Feature { None, Face, Mouth, Nose, Pupil }
        internal static Feature Classify(string name)
        {
            if (!FacialAttachments.Contains(name)) return Feature.None;
            if (name == "Ağız" || name == "Alt Diş" || name == "Ust Diş" || name == "Dil") return Feature.Mouth;
            if (name == "Burun") return Feature.Nose;
            if (name == "Sağ Göz Bebeği" || name == "Sol  Göz Bebeği") return Feature.Pupil;
            return Feature.Face;
        }
        public static bool TryGet(SkeletonData data, out KingVanguardFaceShapeModifier configuration)
        {
            configuration = null;
            return data != null && Registered.TryGetValue(data, out configuration) && configuration != null;
        }
        public override void Apply(SkeletonData data)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (faceScale.x <= 0 || faceScale.y <= 0 || noseScale.x <= 0 || noseScale.y <= 0 || pupilScale <= 0 || mouthScale <= 0)
                throw new InvalidOperationException("King Vanguard face scales must be positive.");
            if (data.FindBone("yuz") == null || data.FindBone("on_yuz") == null)
                throw new InvalidOperationException("Face modifier needs the King/Area yuz + on_yuz frame.");
            foreach (var skin in data.Skins)
                foreach (var entry in skin.GetAttachments())
                {
                    float opacity = -1;
                    // Reuse the native animated contact-shadow masks. Their RGB is
                    // now warm neutral; restrained opacity suits Vanguard's lighter
                    // beard and brows without opaque ribbons across the skin.
                    if (entry.Name == "Sağ Kaş Gölge" || entry.Name == "Sol Kaş Gölge") opacity = browShadowOpacity;
                    else if (entry.Name == "Bıyık Gölge") opacity = moustacheShadowOpacity;
                    else if (entry.Name == "Sakal Gölge") opacity = beardShadowOpacity;
                    else if (entry.Name == "Taç Alt Gölge") opacity = crownShadowOpacity;
                    if ((hideObsoleteUpperCrownShadow && entry.Name == "Taç Gölge Ust") ||
                        (hideObsoleteBrowShadows && (entry.Name == "Sağ Kaş Gölge" || entry.Name == "Sol Kaş Gölge")) ||
                        (hideObsoleteMoustacheShadow && entry.Name == "Bıyık Gölge"))
                        opacity = 0;
                    if (opacity >= 0)
                    {
                        var region = entry.Attachment as RegionAttachment;
                        var mesh = entry.Attachment as MeshAttachment;
                        if (region != null) region.A = Mathf.Clamp01(opacity);
                        if (mesh != null) mesh.A = Mathf.Clamp01(opacity);
                    }
                }
            // Re-applying is idempotent. Never write geometry, deformation or bone data.
            Registered.Remove(data);
            Registered.Add(data, this);
        }
    }
}
