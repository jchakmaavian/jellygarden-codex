using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // The Home background is the current area: its unrestored scene with the restored object of every finished task laid
    // over it, and the whole restored scene once the area is complete. A new task's object fades in; build animations are
    // meant to replace that fade later (RoyalAreas.TaskBuilt).
    [DisallowMultipleComponent]
    public sealed class RoyalAreaScene : MonoBehaviour
    {
        const float FadeSeconds = .35f;
        Image scene;
        RectTransform art;
        readonly List<Image> patches = new List<Image>();
        readonly List<float> fadeStart = new List<float>();
        RoyalAreaInfo shown;
        bool restored;

        int shownState = int.MinValue;
        /// <summary>While set (a restored area is being viewed from the list), saved-state changes do not redraw the scene.</summary>
        public bool Hold { get; set; }
        public RoyalAreaInfo Shown => shown;
        public bool ShowingRestored => restored;
        public bool Revealing { get { foreach (float t in fadeStart) if (t >= 0) return true; return false; } }
        public Image Patch(int task) => task >= 0 && task < patches.Count ? patches[task] : null;

        public static RoyalAreaScene Attach(RoyalHomeScreen home)
        {
            var background = home.transform.Find("Castle framing/Royal blue courtyard");
            if (!background) return null;
            var view = background.GetComponent<RoyalAreaScene>();
            if (!view) view = background.gameObject.AddComponent<RoyalAreaScene>();
            view.scene = background.GetComponent<Image>();
            return view;
        }

        /// <summary>The player's current area; while it has no art yet, the last area restored before it.</summary>
        public void ShowCurrent()
        {
            var area = RoyalAreas.CurrentArea;
            if (area != null && area.Ready) Show(area, RoyalAreas.Complete);
            else Show(RoyalAreas.LastReadyBefore(RoyalAreas.Current - 1), true);
        }

        public void Show(RoyalAreaInfo area, bool asRestored)
        {
            if (area == null || !area.Ready || !scene) return;
            var sprite = RoyalAreas.Art(asRestored ? area.after : area.before);
            if (!sprite) return;
            shown = area; restored = asRestored; shownState = SavedState();
            scene.sprite = sprite; scene.color = Color.white;
            if (!art)
            {
                art = new GameObject("Area task art", typeof(RectTransform)).GetComponent<RectTransform>();
                art.gameObject.layer = 5; art.SetParent(transform, false);
                art.anchorMin = Vector2.zero; art.anchorMax = Vector2.one; art.offsetMin = art.offsetMax = Vector2.zero;
            }
            // Under the sky veil and the clouds, which the reference layout adds as later children.
            art.SetSiblingIndex(0);
            bool current = area.number == RoyalAreas.Current;
            for (int i = 0; i < Mathf.Max(patches.Count, area.tasks.Length); i++)
            {
                if (i >= area.tasks.Length) { patches[i].enabled = false; patches[i].sprite = null; fadeStart[i] = -1; continue; }
                bool visible = !asRestored && current && RoyalAreas.IsDone(i);
                var patch = i < patches.Count ? patches[i] : NewPatch(i);
                Place(patch, area.tasks[i], visible);
                fadeStart[i] = -1; patch.color = Color.white;
            }
            foreach (Transform child in transform)
                if (child.name.StartsWith("Sky cloud ")) child.gameObject.SetActive(area.outdoor);
        }

        /// <summary>Fades in a task that was just paid for; a completed area then settles on its restored scene.</summary>
        public void Reveal(int task)
        {
            var area = RoyalAreas.CurrentArea;
            if (area == null || !area.Ready) return;
            if (shown != area || restored) Show(area, false);
            if (task < 0 || task >= patches.Count) return;
            Place(patches[task], area.tasks[task], true);
            patches[task].color = new Color(1, 1, 1, 0);
            fadeStart[task] = Time.unscaledTime;
            shownState = SavedState();
        }

        // The area and its finished tasks as saved, including which profile (QA or player) is active: when any of them changes
        // outside the area dialogs (a QA session starting or ending), the scene is redrawn from the save.
        static int SavedState() => RoyalAreas.Current * 65536 ^ RoyalThrone.Testing.RoyalSave.GetInt(RoyalAreas.DoneKey, 0) ^ (RoyalThrone.Testing.RoyalSave.Testing ? 1 << 30 : 0);

        Image NewPatch(int index)
        {
            var go = new GameObject("Area task " + (index + 1), typeof(RectTransform));
            go.layer = 5; go.transform.SetParent(art, false);
            var image = go.AddComponent<Image>(); image.raycastTarget = false; image.enabled = false;
            patches.Add(image); fadeStart.Add(-1);
            return image;
        }

        static void Place(Image patch, RoyalAreaTask task, bool visible)
        {
            patch.enabled = visible;
            patch.sprite = visible ? RoyalAreas.Art(task.patch) : null;
            var r = patch.rectTransform;
            r.anchorMin = new Vector2(task.x0, 1 - task.y1); r.anchorMax = new Vector2(task.x1, 1 - task.y0);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        void Update()
        {
            bool finished = false;
            for (int i = 0; i < fadeStart.Count; i++)
            {
                if (fadeStart[i] < 0) continue;
                float t = Mathf.Clamp01((Time.unscaledTime - fadeStart[i]) / FadeSeconds);
                patches[i].color = new Color(1, 1, 1, t * t * (3 - 2 * t));
                if (t >= 1) { fadeStart[i] = -1; finished = true; }
            }
            if (finished && !Revealing && RoyalAreas.Complete && shown == RoyalAreas.CurrentArea) Show(shown, true);
            if (!Hold && !Revealing && SavedState() != shownState) ShowCurrent();
        }
    }
}
