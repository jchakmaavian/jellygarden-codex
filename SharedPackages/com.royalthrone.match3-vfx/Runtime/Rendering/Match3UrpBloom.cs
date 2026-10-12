using System;
using System.Reflection;
using UnityEngine;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>
    /// Optional runtime bloom for URP, done through reflection so the kit compiles and runs on the
    /// Built-in pipeline without a URP dependency. With the threshold at or above 1.0 only the
    /// HDR-boosted effects glow; board art stays unchanged. Off by default because many projects
    /// already own their post-processing.
    /// </summary>
    internal static class Match3UrpBloom
    {
        static bool applied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => applied = false;

        public static void Apply(Match3VfxSettings settings)
        {
            if (applied || !settings || !settings.enableUrpBloom) return;
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null) return;

            var bloomType = FindType("UnityEngine.Rendering.Universal.Bloom");
            var volumeType = FindType("UnityEngine.Rendering.Volume");
            var profileType = FindType("UnityEngine.Rendering.VolumeProfile");
            if (bloomType == null || volumeType == null || profileType == null) return;
            applied = true;

            var go = new GameObject("~Match3Vfx Bloom Volume");
            UnityEngine.Object.DontDestroyOnLoad(go);
            var volume = go.AddComponent(volumeType);
            SetMember(volume, "isGlobal", true);
            SetMember(volume, "priority", 100f);
            SetMember(volume, "weight", 1f);

            var profile = ScriptableObject.CreateInstance(profileType);
            profile.name = "Match3Vfx Bloom";
            profile.hideFlags = HideFlags.HideAndDontSave;
            SetMember(volume, "sharedProfile", profile);

            var add = profileType.GetMethod("Add", new[] { typeof(Type), typeof(bool) });
            var bloom = add?.Invoke(profile, new object[] { bloomType, true });
            if (bloom == null) return;
            SetParameter(bloom, "threshold", settings.bloomThreshold);
            SetParameter(bloom, "intensity", settings.bloomIntensity);
            SetParameter(bloom, "scatter", settings.bloomScatter);
            SetParameter(bloom, "highQualityFiltering", false);

            var cameraDataType = FindType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
            if (cameraDataType == null) return;
            foreach (var cam in Camera.allCameras)
            {
                var data = cam.GetComponent(cameraDataType) ?? cam.gameObject.AddComponent(cameraDataType);
                cam.allowHDR = true;
                SetMember(data, "renderPostProcessing", true);
            }
        }

        static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = assembly.GetType(fullName, false);
                if (t != null) return t;
            }
            return null;
        }

        static void SetMember(object target, string name, object value)
        {
            if (target == null) return;
            var type = target.GetType();
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanWrite) { property.SetValue(target, value, null); return; }
            type.GetField(name, BindingFlags.Public | BindingFlags.Instance)?.SetValue(target, value);
        }

        // A VolumeParameter only applies when overrideState is set too.
        static void SetParameter(object component, string field, object value)
        {
            var parameter = component.GetType().GetField(field, BindingFlags.Public | BindingFlags.Instance)?.GetValue(component);
            if (parameter == null) return;
            SetMember(parameter, "overrideState", true);
            SetMember(parameter, "value", value);
        }
    }
}
