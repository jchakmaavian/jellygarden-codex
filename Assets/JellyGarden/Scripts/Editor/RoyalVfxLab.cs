using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.IO;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEditor;
using UnityEngine;

namespace RoyalThrone.Editor
{
    // Play Mode lab for trying the All In 1 VFX Toolkit demo effects on the live Royal Throne board.
    // Editor-only and temporary: spawned copies are adjusted for the orthographic board and removed again.
    // Prefab assets, gameplay, moves, lives, coins and saved progress are not changed.
    // Plugin components are matched by name so this window still compiles if the plugin is removed.
    public sealed class RoyalVfxLab : EditorWindow
    {
        const string PrefabFolder = "Assets/Plugins/AllIn1VfxToolkit/Demo & Assets/Demo/Prefabs";
        const string FavouritesKey = "RoyalThrone.VfxLab.Favourites";
        // The royal effect rig draws at sorting orders 26–38; lab effects sit just above it.
        const int SortingOrder = 40;

        GameObject[] prefabs = new GameObject[0];
        readonly List<(GameObject instance, double removeAt)> active = new List<(GameObject, double)>();
        HashSet<string> favourites = new HashSet<string>();
        string filter = "";
        bool favouritesOnly, tapToFire, autoCycle, wasTouching;
        bool faceCamera = true, softParticles = true, integratedImpacts = true;
        readonly System.Random previewRandom = new System.Random(8153);
        static bool captureRunning;
        int selected;
        float size = 1f, tilt, lifetime = 2.5f, gameSpeed = 1f;
        float? originalTimeScale;
        Camera depthCamera;
        DepthTextureMode originalDepthMode;
        double nextCycleAt;
        Vector2 scroll;

        [MenuItem("Tools/Royal Throne/Effects/All In 1 VFX Lab")]
        public static void Open() => GetWindow<RoyalVfxLab>("VFX Lab");

        [MenuItem("Tools/Royal Throne/Effects/Capture integrated Fire in Lab at half speed %#F9")]
        public static void CaptureHalfSpeed()
        {
            if (!EditorApplication.isPlaying || !LevelManager.THIS || LevelManager.THIS.gameStatus != GameState.Playing || captureRunning)
                throw new InvalidOperationException("Open a gameplay board before the Lab capture.");
            var lab = GetWindow<RoyalVfxLab>("VFX Lab");
            RoyalRegressionRunner.Run(CaptureHalfSpeedRoutine(lab));
        }
        static IEnumerator CaptureHalfSpeedRoutine(RoyalVfxLab lab)
        {
            captureRunning = true;
            float oldSpeed = lab.gameSpeed, oldScale = Time.timeScale, oldSize = lab.size;
            float? oldOwner = lab.originalTimeScale;
            bool oldIntegrated = lab.integratedImpacts, oldAuto = lab.autoCycle;
            string folder = "Library/RoyalToolkitLabCapture";
            Directory.CreateDirectory(folder);
            int frame = 0;
            try
            {
                lab.autoCycle = false; lab.integratedImpacts = true; lab.size = 1;
                lab.SetGameSpeed(.5f); lab.ClearEffects();
                var fire = lab.prefabs.First(p => p.name == "Fire Impact");
                lab.Fire(fire, BoardCentre());
                float start = Time.realtimeSinceStartup, next = start;
                while (Time.realtimeSinceStartup - start < 3.6f)
                {
                    yield return new WaitForEndOfFrame();
                    if (Time.realtimeSinceStartup < next) continue;
                    ScreenCapture.CaptureScreenshot(folder + "/frame-" + frame.ToString("D4") + ".png");
                    frame++; next = Time.realtimeSinceStartup + .08f;
                }
                File.WriteAllText(folder + "/capture.json", "{\"effect\":\"Fire Impact\",\"path\":\"RoyalVfxLab.Fire\",\"gameSpeed\":0.5,\"sampleIntervalSeconds\":0.08,\"durationSeconds\":3.6,\"frames\":" + frame + ",\"performanceBenchmark\":false}");
                Debug.Log("[Royal Toolkit] Lab Fire capture at 0.5 complete: " + folder);
            }
            finally
            {
                if (lab) { lab.ClearEffects(); lab.gameSpeed = oldSpeed; lab.originalTimeScale = oldOwner; lab.size = oldSize; lab.integratedImpacts = oldIntegrated; lab.autoCycle = oldAuto; }
                // A pause menu or another review control can take ownership mid-shot.
                if (Mathf.Approximately(Time.timeScale, .5f)) Time.timeScale = oldScale;
                captureRunning = false;
            }
        }

        // Review the production rocket at the Lab's actual speed control, with restoration.
        public static IEnumerator ReviewRocketSpeeds(Func<float,IEnumerator> review)
        {
            var lab=GetWindow<RoyalVfxLab>("VFX Lab");
            float oldSpeed=lab.gameSpeed,oldScale=Time.timeScale;float? oldOwner=lab.originalTimeScale;
            bool oldAuto=lab.autoCycle;
            try
            {
                lab.autoCycle=false;
                foreach(float speed in new[]{.5f,1f})
                {lab.SetGameSpeed(speed);yield return review(speed);}
            }
            finally
            {if(lab){lab.gameSpeed=oldSpeed;lab.originalTimeScale=oldOwner;lab.autoCycle=oldAuto;lab.Repaint();}Time.timeScale=oldScale;}
        }

        void OnEnable()
        {
            prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(prefab => prefab && !prefab.name.StartsWith("z")) // zProjectileBase is an empty template
                .OrderBy(prefab => prefab.name)
                .ToArray();
            favourites = new HashSet<string>(EditorPrefs.GetString(FavouritesKey, "").Split('\n').Where(name => name.Length > 0));
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += PlayModeChanged;
        }

        void OnDisable()
        {
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            SetTapToFire(false);
            ClearEffects();
            SetGameSpeed(1f);
        }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Label("ROYAL THRONE • ALL IN 1 VFX LAB", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Fires the plugin's demo effects on the live board in Play Mode. Copies are temporary: prefab assets, moves, lives, coins and saved progress do not change.", MessageType.Info);
            if (prefabs.Length == 0)
            {
                EditorGUILayout.HelpBox("No demo prefabs found in " + PrefabFolder + ".", MessageType.Error);
                EditorGUILayout.EndScrollView();
                return;
            }

            filter = EditorGUILayout.TextField("Search", filter);
            favouritesOnly = EditorGUILayout.Toggle("Favourites only", favouritesOnly);
            var shown = Shown();
            if (shown.Length == 0) EditorGUILayout.HelpBox("No effects match.", MessageType.None);
            else
            {
                selected = Mathf.Clamp(selected, 0, shown.Length - 1);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("◀", GUILayout.Width(28))) selected = (selected + shown.Length - 1) % shown.Length;
                    selected = EditorGUILayout.Popup(selected, shown.Select(prefab => (favourites.Contains(prefab.name) ? "★ " : "") + prefab.name).ToArray());
                    if (GUILayout.Button("▶", GUILayout.Width(28))) selected = (selected + 1) % shown.Length;
                    if (GUILayout.Button(favourites.Contains(shown[selected].name) ? "★" : "☆", GUILayout.Width(28))) ToggleFavourite(shown[selected].name);
                }
                EditorGUILayout.LabelField($"{selected + 1} of {shown.Length}", EditorStyles.miniLabel);
            }

            size = EditorGUILayout.Slider(new GUIContent("Size (board cells)", "Scale relative to one board cell."), size, .25f, 4f);
            tilt = EditorGUILayout.Slider(new GUIContent("Tilt X", "−90 stands flat ground effects, such as areas and portals, upright for the 2D camera."), tilt, -90f, 90f);
            lifetime = EditorGUILayout.Slider(new GUIContent("Remove after (s)", "Looping effects are removed after this time."), lifetime, .5f, 10f);
            faceCamera = EditorGUILayout.Toggle(new GUIContent("Face 2D camera", "Turns flat horizontal-billboard layers (glows, shockwaves, burn marks) toward the board camera. The demo views them from above; the board camera would see them edge-on."), faceCamera);
            integratedImpacts = EditorGUILayout.Toggle(new GUIContent("Integrated Ice / Fire", "Uses the production pooled board effects for Ice Impact and Fire Impact. Turn off to compare the untouched vendor demos."), integratedImpacts);
            bool soft = EditorGUILayout.Toggle(new GUIContent("Soft particles", "Many smoke layers use soft particles, which need the camera depth texture in the Built-in pipeline. The lab enables it only while its effects are live; in a phone build it costs an extra render pass."), softParticles);
            if (soft != softParticles)
            {
                softParticles = soft;
                if (!soft) RestoreDepth();
                else if (active.Count > 0) EnableDepth();
            }

            bool playing = EditorApplication.isPlaying;
            using (new EditorGUI.DisabledScope(!playing))
                SetGameSpeed(EditorGUILayout.Slider(new GUIContent("Game speed", "Slows the whole game to review fast bursts. Restored when you close the lab or leave Play Mode."), gameSpeed, .05f, 1f));

            var level = playing ? LevelManager.THIS : null;
            if (!playing) EditorGUILayout.HelpBox("Enter Play Mode and open a level to fire effects.", MessageType.Warning);
            else if (!level || level.gameStatus != GameState.Playing)
            {
                EditorGUILayout.HelpBox("Open a level to see effects on the board. Effects still fire over other screens.", MessageType.Warning);
                using (new EditorGUI.DisabledScope(!level || level.gameStatus != GameState.Map))
                    if (GUILayout.Button("Open current level without life cost")) RoyalEffectsChecks.OpenBoard();
            }

            using (new EditorGUI.DisabledScope(!playing || shown.Length == 0))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Fire at board centre")) Fire(shown[selected], BoardCentre());
                    if (GUILayout.Button("Fire on random cell")) Fire(shown[selected], RandomCell());
                }
                SetTapToFire(EditorGUILayout.ToggleLeft(new GUIContent("Tap board to fire", "Board swaps are paused while this is on."), tapToFire));
                bool cycle = EditorGUILayout.ToggleLeft(new GUIContent("Play through list", "Fires each shown effect at the board centre in turn."), autoCycle);
                if (cycle && !autoCycle) nextCycleAt = 0;
                autoCycle = cycle;
                if (GUILayout.Button($"Clear effects ({active.Count})")) ClearEffects();
            }

            if (favourites.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Favourites", string.Join(", ", favourites.OrderBy(name => name)), EditorStyles.wordWrappedLabel);
                if (GUILayout.Button("Copy favourites")) EditorGUIUtility.systemCopyBuffer = string.Join("\n", favourites.OrderBy(name => name));
            }
            EditorGUILayout.EndScrollView();
        }

        void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            double now = Time.timeAsDouble;
            int before = active.Count;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i].instance && now < active[i].removeAt) continue;
                if (active[i].instance) Destroy(active[i].instance);
                active.RemoveAt(i);
            }
            if (active.Count != before)
            {
                if (active.Count == 0) RestoreDepth();
                Repaint();
            }
            if (!autoCycle && !tapToFire) return;

            var shown = Shown();
            if (shown.Length == 0) return;
            if (autoCycle && now >= nextCycleAt)
            {
                selected = nextCycleAt == 0 ? Mathf.Clamp(selected, 0, shown.Length - 1) : (selected + 1) % shown.Length;
                ClearEffects();
                Fire(shown[selected], BoardCentre());
                nextCycleAt = now + lifetime;
                Repaint();
            }

            var level = LevelManager.THIS;
            if (!tapToFire || !level) return;
            if (level.gameStatus == GameState.Playing) level.DragBlocked = true;
            bool touching = InputManager.Instance.IsTouching;
            if (touching && !wasTouching && Camera.main)
                Fire(shown[Mathf.Clamp(selected, 0, shown.Length - 1)], Camera.main.ScreenToWorldPoint(InputManager.Instance.TouchPosition));
            wasTouching = touching;
        }

        void Fire(GameObject prefab, Vector3 point)
        {
            var level = LevelManager.THIS;
            float cell = level && level.GameField ? level.squareWidth * level.GameField.lossyScale.x : 1.2f;
            if (integratedImpacts && (prefab.name == "Ice Impact" || prefab.name == "Fire Impact"))
            {
                point.z = level && level.GameField ? level.GameField.position.z : 0;
                bool played = prefab.name == "Fire Impact" ? RoyalToolkitImpact.TryPlayFire(point, cell * size) : RoyalToolkitImpact.TryPlayIce(point, cell * size);
                if (!played) Debug.LogWarning("[Royal Toolkit] Integrated effects unavailable or reduced motion enabled. Build the toolkit library in Edit Mode first.");
                Repaint(); return;
            }
            point.z = (level && level.GameField ? level.GameField.position.z : 0f) - 1.2f;

            bool projectile = prefab.GetComponentInChildren<Rigidbody>(true);
            var rotation = projectile ? Quaternion.LookRotation(Vector3.right, Vector3.up) : Quaternion.Euler(tilt, 0f, 0f) * prefab.transform.rotation;
            var copy = Instantiate(prefab, point, rotation);
            copy.name = "VFX Lab — " + prefab.name;
            copy.transform.localScale = prefab.transform.localScale * cell * size;

            // The demo camera shake needs the demo scene's shaker; in the game it would only log errors.
            foreach (var behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour && behaviour.GetType().Name == "AllIn1DoShake") behaviour.enabled = false;
            // Demo projectiles are launched by the demo controller; send them across the board instead.
            foreach (var body in copy.GetComponentsInChildren<Rigidbody>(true))
            {
                body.useGravity = false;
                body.linearVelocity = Vector3.right * cell * 6f;
            }
            foreach (var system in copy.GetComponentsInChildren<ParticleSystem>(true))
            {
                // Demo effects are authored at world scale; follow the copy's scale so they fit a board cell.
                var main = system.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                var particleRenderer = system.GetComponent<ParticleSystemRenderer>();
                if (faceCamera && particleRenderer && particleRenderer.renderMode == ParticleSystemRenderMode.HorizontalBillboard)
                    particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            }
            foreach (var renderer in copy.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sortingLayerID = SortingLayer.NameToID("Default");
                renderer.sortingOrder = SortingOrder + Mathf.Clamp(renderer.sortingOrder, -5, 5);
            }
            EnableDepth();
            active.Add((copy, Time.timeAsDouble + lifetime));
            Repaint();
        }

        void EnableDepth()
        {
            var camera = Camera.main;
            if (!softParticles || !camera || depthCamera == camera) return;
            RestoreDepth();
            depthCamera = camera;
            originalDepthMode = camera.depthTextureMode;
            camera.depthTextureMode |= DepthTextureMode.Depth;
        }

        void RestoreDepth()
        {
            if (depthCamera) depthCamera.depthTextureMode = originalDepthMode;
            depthCamera = null;
        }

        void SetGameSpeed(float speed)
        {
            if (Mathf.Approximately(speed, gameSpeed)) return;
            gameSpeed = speed;
            if (!EditorApplication.isPlaying) { originalTimeScale = null; return; }
            if (originalTimeScale == null) originalTimeScale = Time.timeScale;
            Time.timeScale = Mathf.Approximately(speed, 1f) ? originalTimeScale.Value : speed;
            if (Mathf.Approximately(speed, 1f)) originalTimeScale = null;
        }

        void SetTapToFire(bool on)
        {
            if (on == tapToFire) return;
            tapToFire = on;
            wasTouching = true; // ignore a touch that is already held when the mode changes
            var level = EditorApplication.isPlaying ? LevelManager.THIS : null;
            if (!on && level && level.gameStatus == GameState.Playing && !RoyalPowerupDirector.Busy) level.DragBlocked = false;
        }

        void ClearEffects()
        {
            if (EditorApplication.isPlaying)
                foreach (var entry in active)
                    if (entry.instance) Destroy(entry.instance);
            active.Clear();
            RoyalToolkitImpact.Existing?.Clear();
            RestoreDepth();
        }

        void PlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingPlayMode) return;
            tapToFire = autoCycle = false;
            active.Clear();
            RestoreDepth();
            SetGameSpeed(1f);
        }

        void ToggleFavourite(string name)
        {
            if (!favourites.Remove(name)) favourites.Add(name);
            EditorPrefs.SetString(FavouritesKey, string.Join("\n", favourites));
        }

        GameObject[] Shown() => prefabs
            .Where(prefab => prefab && (!favouritesOnly || favourites.Contains(prefab.name)) &&
                             (filter.Length == 0 || prefab.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0))
            .ToArray();

        static Vector3 BoardCentre()
        {
            var cells = BoardCells();
            if (cells.Count > 0)
            {
                var bounds = new Bounds(cells[0].transform.position, Vector3.zero);
                foreach (var cell in cells) bounds.Encapsulate(cell.transform.position);
                return bounds.center;
            }
            var camera = Camera.main;
            return camera ? camera.ViewportToWorldPoint(new Vector3(.5f, .52f, 10f)) : Vector3.zero;
        }

        Vector3 RandomCell()
        {
            var cells = BoardCells();
            return cells.Count > 0 ? cells[previewRandom.Next(cells.Count)].transform.position : BoardCentre();
        }

        static List<Square> BoardCells()
        {
            var cells = new List<Square>();
            var level = LevelManager.THIS;
            if (!level || level.gameStatus != GameState.Playing) return cells;
            for (int row = 0; row < level.maxRows; row++)
                for (int col = 0; col < level.maxCols; col++)
                {
                    var square = level.GetSquare(col, row);
                    if (square && !square.IsNone()) cells.Add(square);
                }
            return cells;
        }
    }
}
