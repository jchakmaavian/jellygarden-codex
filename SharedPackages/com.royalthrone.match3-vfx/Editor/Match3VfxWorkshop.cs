using System;
using UnityEditor;
using UnityEngine;

namespace RoyalThrone.Match3Vfx.Editor
{
    /// <summary>Tune settings and preview every effect in Play Mode without touching a board or save data.</summary>
    public sealed class Match3VfxWorkshop : EditorWindow
    {
        Match3VfxSettings settings;
        UnityEditor.Editor settingsEditor;
        Vector2 scroll;

        public static void Open() => GetWindow<Match3VfxWorkshop>("Match-3 VFX");

        void OnEnable()
        {
            if (!settings) settings = AssetDatabase.LoadAssetAtPath<Match3VfxSettings>(Match3VfxMenu.SettingsAssetPath);
        }

        void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying) Repaint();
        }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Label("ROYAL THRONE • MATCH-3 VFX KIT", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Previews use the same pools as gameplay. They never call HitCell, so no board, move or reward changes.", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                settings = (Match3VfxSettings)EditorGUILayout.ObjectField("Settings", settings, typeof(Match3VfxSettings), false);
                if (GUILayout.Button("Create", GUILayout.Width(60))) { Match3VfxMenu.CreateSettingsAsset(); OnEnable(); }
            }

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                if (EditorApplication.isPlaying && settings && GUILayout.Button("Use these settings now (rebuilds pools)"))
                    Match3Vfx.Settings = settings;

                GUILayout.Space(4);
                GUILayout.Label("Preview at screen centre", EditorStyles.miniBoldLabel);
                int column = 0;
                EditorGUILayout.BeginHorizontal();
                foreach (Match3PreviewEffect effect in Enum.GetValues(typeof(Match3PreviewEffect)))
                {
                    if (GUILayout.Button(ObjectNames.NicifyVariableName(effect.ToString()))) Match3Vfx.Preview(effect, PreviewPoint());
                    if (++column % 3 == 0) { EditorGUILayout.EndHorizontal(); EditorGUILayout.BeginHorizontal(); }
                }
                EditorGUILayout.EndHorizontal();
                if (GUILayout.Button("Clear all effects")) Match3Vfx.Clear();
            }
            if (!EditorApplication.isPlaying) EditorGUILayout.HelpBox("Enter Play Mode to preview.", MessageType.None);

            DrawStats();

            if (settings)
            {
                GUILayout.Space(8);
                UnityEditor.Editor.CreateCachedEditor(settings, null, ref settingsEditor);
                settingsEditor.OnInspectorGUI();
            }
            EditorGUILayout.EndScrollView();
        }

        static Vector3 PreviewPoint()
        {
            var camera = Camera.main;
            if (!camera) return Vector3.zero;
            var p = camera.ViewportToWorldPoint(new Vector3(0.5f, 0.52f, Mathf.Abs(camera.transform.position.z)));
            p.z = 0f;
            return p;
        }

        static void DrawStats()
        {
            var rig = Match3Vfx.ExistingRig;
            if (!EditorApplication.isPlaying || !rig) return;
            GUILayout.Space(6);
            GUILayout.Label("Live pools", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("Sequences busy", Match3Vfx.Busy ? "yes (" + rig.Sequences.PendingOperations + ")" : "no");
            EditorGUILayout.LabelField("Projectiles", rig.Powerups.ActiveProjectiles + " / " + rig.Powerups.ProjectileCapacity + " (peak " + rig.Powerups.PeakProjectiles + ")");
            EditorGUILayout.LabelField("Debris chips", rig.Shatter.ActiveFragments + " / " + rig.Shatter.Capacity + " (peak " + rig.Shatter.PeakFragments + ")");
            EditorGUILayout.LabelField("Overlays", rig.Burst.ActiveOverlays + " / " + MatchBurstFx.OverlayPoolSize);
            EditorGUILayout.LabelField("Particles", (rig.Burst.ParticleCount + rig.Powerups.ParticleCount).ToString());
            EditorGUILayout.LabelField("SFX voices", rig.Sfx.ActiveVoices + " / " + rig.Sfx.Capacity + " (peak " + rig.Sfx.PeakVoices + ")");
        }
    }
}
