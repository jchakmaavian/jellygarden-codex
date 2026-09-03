// The level fills the screen, top edge to bottom edge.
//
// On a 19.5:9 phone the level used to open with a band of bare backdrop above
// the HUD strip — 28 pixels of nothing at 402x874, more on a taller screen.
// Two separate things push the level's UI down, and both are released here.
//
// The first is the kit's own notch accommodation, and it is the one that
// actually bites. LevelManager.EnableMap(false) — which runs every time a level
// opens — does this:
//
//     Level/Canvas/Panel.anchoredPosition = Vector3.zero;
//     if (aspect == 2.17f)                                    // iPhone X
//     {
//         camera.orthographicSize = 8.1f;
//         Level/Canvas/Panel.anchoredPosition = Vector3.down * 50;
//     }
//
// Every piece of level UI lives under that Panel, this layer's HUD included, so
// on exactly the aspect a modern iPhone has, the whole level slides down 50
// canvas units and the top of the screen is left empty. The camera's zoom is
// left alone — that is the board's framing and it is well judged; only the
// nudge is undone.
//
// The second is the SafeArea component the kit hangs on that same Panel, which
// turns Screen.safeArea into anchors. On macOS it is a no-op (the safe area is
// the whole window) but on a device it is not, and it insets the same panel for
// the same reason.
//
// The cost of releasing both, stated plainly: on a device with a notch or a
// Dynamic Island the Target and Moves plates now sit underneath it. That is the
// intended trade.
//
// Nothing is destroyed and no kit script is edited. Both levers keep running
// and keep being put back, every frame, which is what makes this survive the
// kit re-applying the nudge on the next level. Deleting this file restores the
// kit's behaviour exactly.

using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryLevelFullBleed : MonoBehaviour
    {
        /// <summary>The whole feature, in one switch.</summary>
        public static bool Enabled = true;

        private static StoryLevelFullBleed _instance;

        /// <summary>
        /// The panels being held open. Found per scene rather than per frame:
        /// this runs on a phone, and FindObjectsByType is not something to do
        /// sixty times a second for one rect that hardly ever moves.
        /// </summary>
        private static readonly List<RectTransform> _panels = new List<RectTransform>();

        /// <summary>Logged the first time a panel is actually released, then quiet.</summary>
        private static bool _reported;

        /// <summary>Called by StorySceneHooks on every scene load.</summary>
        public static void Install()
        {
            if (!Enabled) return;

            // A new scene brings new panels, so the list is rebuilt either way.
            Rescan();

            if (_instance != null) return;

            var go = new GameObject("~StoryLevelFullBleed");
            go.hideFlags = HideFlags.HideAndDontSave;
            StoryObjects.Persist(go);
            _instance = go.AddComponent<StoryLevelFullBleed>();
        }

        /// <summary>
        /// Finds the level's panel and releases it. Called on a scene load and
        /// on a level being loaded — the two moments the panel can appear or be
        /// nudged — and by a test, which has neither.
        /// </summary>
        public static int Rescan()
        {
            _panels.Clear();

            // The panel the kit names in EnableMap, found the way the kit finds
            // it. Its SafeArea is the same object, so one lookup covers both.
            foreach (LevelManager manager in
                     Object.FindObjectsByType<LevelManager>(FindObjectsInactive.Include))
            {
                if (manager == null || manager.Level == null) continue;

                Transform panel = manager.Level.transform.Find("Canvas/Panel");
                var rect = panel == null ? null : panel.GetComponent<RectTransform>();
                if (rect != null && !_panels.Contains(rect)) _panels.Add(rect);
            }

            return Release();
        }

        /// <summary>
        /// Puts the panels found by <see cref="Rescan"/> back to the full
        /// screen: full anchors, and no downward nudge. Returns how many it had
        /// to change, which is what a test reads and what keeps the log quiet
        /// when there was nothing to do.
        /// </summary>
        public static int Release()
        {
            int released = 0;

            for (int i = 0; i < _panels.Count; i++)
            {
                RectTransform rect = _panels[i];
                if (rect == null) continue;

                bool changed = false;

                if (rect.anchorMin != Vector2.zero || rect.anchorMax != Vector2.one)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    changed = true;
                }

                // The kit sets this to zero on most aspects and to -50 on the
                // one a modern iPhone has. Zero is the value that fills the
                // screen, so zero is what it keeps.
                Vector2 pos = rect.anchoredPosition;
                if (pos.y != 0f)
                {
                    rect.anchoredPosition = new Vector2(pos.x, 0f);
                    changed = true;
                }

                if (changed) released++;
            }

            return released;
        }

        private void OnEnable()
        {
            LevelManager.OnLevelLoaded += OnLevelLoaded;
            LevelManager.OnStartPlay += OnLevelLoaded;
        }

        private void OnDisable()
        {
            LevelManager.OnLevelLoaded -= OnLevelLoaded;
            LevelManager.OnStartPlay -= OnLevelLoaded;
        }

        private static void OnLevelLoaded()
        {
            if (!Enabled) return;

            // EnableMap(false) has just nudged the panel down, so this is the
            // moment there is something to undo. Rescan because on the first
            // level of a session the panel may also be new.
            Rescan();
        }

        private void LateUpdate()
        {
            // The nudge is re-applied on every level, and the kit reaches the
            // panel by paths that raise no event — a retry, a map toggle. So
            // this is asserted per frame rather than fixed once: two compares
            // for one rect, and a write only when it drifted.
            if (!Enabled) return;

            if (Release() > 0 && !_reported)
            {
                _reported = true;
                Debug.Log("[Story] level panel released: the level draws edge to edge, " +
                          "no notch nudge and no safe-area inset.");
            }
        }
    }
}
