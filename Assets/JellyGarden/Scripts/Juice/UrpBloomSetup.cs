using System;
using System.Reflection;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>
    /// Creates the bloom Volume at runtime and switches post-processing on for the
    /// game cameras.
    ///
    /// Everything here goes through reflection on purpose. The juice layer must keep
    /// compiling and running whether the project is on URP or the Built-In pipeline,
    /// so it never references a URP type directly - if URP is not the active pipeline
    /// this whole class quietly does nothing.
    ///
    /// No scene asset is touched: the Volume is a runtime-only DontDestroyOnLoad
    /// object, exactly like MatchVFXService itself.
    /// </summary>
    public static class UrpBloomSetup
    {
        private static bool _applied;
        private static GameObject _volumeGo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _applied = false;
            _volumeGo = null;
        }

        private static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName, false);
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>True when a Scriptable Render Pipeline (i.e. URP here) is active.</summary>
        public static bool SrpActive
        {
            get { return UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null; }
        }

        public static void Apply(JuiceConfig cfg)
        {
            if (_applied) return;
            if (cfg == null || !cfg.EnableJuice || !cfg.EnableBloom) return;
            if (!SrpActive) return;   // Built-In: nothing to do, and no error either

            var tBloom = FindType("UnityEngine.Rendering.Universal.Bloom");
            var tVolume = FindType("UnityEngine.Rendering.Volume");
            var tProfile = FindType("UnityEngine.Rendering.VolumeProfile");
            if (tBloom == null || tVolume == null || tProfile == null) return;

            _applied = true;

            // --- the volume -------------------------------------------------
            _volumeGo = new GameObject("~JuiceBloomVolume");
            UnityEngine.Object.DontDestroyOnLoad(_volumeGo);

            var volume = _volumeGo.AddComponent(tVolume);
            SetMember(volume, "isGlobal", true);
            SetMember(volume, "priority", 100f);
            SetMember(volume, "weight", 1f);

            var profile = ScriptableObject.CreateInstance(tProfile);
            profile.name = "JuiceBloomProfile";
            profile.hideFlags = HideFlags.HideAndDontSave;
            SetMember(volume, "sharedProfile", profile);

            // --- the bloom override ------------------------------------------
            var add = tProfile.GetMethod("Add", new[] { typeof(Type), typeof(bool) });
            if (add == null) return;
            var bloom = add.Invoke(profile, new object[] { tBloom, true });
            if (bloom == null) return;

            // Threshold at/above 1.0 means only the HDR-boosted VFX bloom; the board
            // art never exceeds 1.0, so the idle picture is unchanged.
            SetVolumeParam(bloom, "threshold", cfg.BloomThreshold);
            SetVolumeParam(bloom, "intensity", cfg.BloomIntensity);
            SetVolumeParam(bloom, "scatter", cfg.BloomScatter);
            SetVolumeParamBool(bloom, "highQualityFiltering", false);   // cheaper on mobile

            EnablePostProcessingOnCameras();
        }

        /// <summary>URP ignores volumes unless the camera opts in to post-processing.</summary>
        public static void EnablePostProcessingOnCameras()
        {
            var tData = FindType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
            if (tData == null) return;

            var cams = UnityEngine.Object.FindObjectsByType<Camera>();
            for (int i = 0; i < cams.Length; i++)
            {
                var cam = cams[i];
                if (cam == null) continue;
                var data = cam.GetComponent(tData) ?? cam.gameObject.AddComponent(tData);
                // Bloom with a threshold at/above 1.0 only works if the camera renders
                // in HDR; otherwise every value clamps to 1.0 and nothing ever exceeds
                // the threshold. The pipeline asset also has supportsHDR enabled.
                cam.allowHDR = true;
                SetMember(data, "renderPostProcessing", true);
            }
        }

        // ------------------------------------------------------------------
        private static void SetMember(object target, string name, object value)
        {
            if (target == null) return;
            var t = target.GetType();
            var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (p != null && p.CanWrite) { p.SetValue(target, value, null); return; }
            var f = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (f != null) f.SetValue(target, value);
        }

        /// <summary>
        /// A VolumeParameter only takes effect when its overrideState is set, so both
        /// the value and the flag have to be written.
        /// </summary>
        private static void SetVolumeParam(object component, string fieldName, float value)
        {
            var f = component.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (f == null) return;
            var param = f.GetValue(component);
            if (param == null) return;
            SetMember(param, "overrideState", true);
            SetMember(param, "value", value);
        }

        private static void SetVolumeParamBool(object component, string fieldName, bool value)
        {
            var f = component.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (f == null) return;
            var param = f.GetValue(component);
            if (param == null) return;
            SetMember(param, "overrideState", true);
            SetMember(param, "value", value);
        }
    }
}
