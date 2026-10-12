using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // Areas 1-3 render their native layers and reveal clips; the remaining catalog keeps its existing patch renderer.
    // Save state and star spending stay in RoyalAreas, independent of presentation and restoration replays.
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
        RoyalHomeScreen home;
        AreaMotion.RoyalAreaMotionPlayer native;
        bool wasHomeVisible;
        public AreaMotion.RoyalAreaMotionPlayer NativePlayer => native;

        int shownState = int.MinValue;
        /// <summary>While set (a restored area is being viewed from the list), saved-state changes do not redraw the scene.</summary>
        public bool Hold { get; set; }
        public RoyalAreaInfo Shown => shown;
        public bool ShowingRestored => restored;
        public bool Revealing { get { if (rising > 0 || replay != null || (native && native.Revealing)) return true; foreach (float t in fadeStart) if (t >= 0) return true; return false; } }
        public Image Patch(int task) => task >= 0 && task < patches.Count ? patches[task] : null;

        public static RoyalAreaScene Attach(RoyalHomeScreen home)
        {
            var background = home.transform.Find("Castle framing/Royal blue courtyard");
            if (!background) return null;
            var view = background.GetComponent<RoyalAreaScene>();
            if (!view) view = background.gameObject.AddComponent<RoyalAreaScene>();
            view.scene = background.GetComponent<Image>();
            view.home = home;
            return view;
        }

        /// <summary>The player's current area; while it has no art yet, or waits behind the Area card's "New Area", the last
        /// area restored before it.</summary>
        public void ShowCurrent()
        {
            var area = RoyalAreas.CurrentArea;
            if (area != null && area.Ready && !RoyalAreas.AwaitingEntry) Show(area, RoyalAreas.Complete);
            else Show(RoyalAreas.LastReadyBefore(RoyalAreas.Current - 1), true);
        }

        public void Show(RoyalAreaInfo area, bool asRestored)
        {
            if (area == null || !area.Ready || !scene) return;
            if (area.nativeMotion && ShowNative(area, asRestored)) return;
            if (native) { native.enabled = false; if (native.Surface) native.Surface.enabled = false; }
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
            if (native && native.enabled) { StartCoroutine(Rise(task, home && !home.MotionEnabled ? .3f : 1.3f)); return; }
            if (task < 0 || task >= patches.Count) return;
            Place(patches[task], area.tasks[task], true);
            patches[task].color = new Color(1, 1, 1, 0);
            fadeStart[task] = Time.unscaledTime;
            shownState = SavedState();
        }

        /// <summary>
        /// A task just paid for rises into its place, as the reference builds it: a puff at its foot, then the object grows up
        /// out of the ground from its base (70% wide, 45% tall, a little low), overshoots slightly and settles. The reference
        /// assembles each object from its parts; ours is one picture, so the whole object rises.
        /// </summary>
        public IEnumerator Rise(int task, float duration)
        {
            var area = RoyalAreas.CurrentArea;
            if (area == null || !area.Ready || task < 0 || task >= area.tasks.Length) yield break;
            if (shown != area || restored) Show(area, false);
            if (native && native.enabled)
            {
                rising++;
                try { yield return native.Reveal(task, duration > .3f && (!home || home.MotionEnabled)); }
                finally { rising--; shownState = SavedState(); if (RoyalAreas.Complete && shown == RoyalAreas.CurrentArea) Show(shown, true); }
                yield break;
            }
            if (task >= patches.Count) yield break;
            var patch = patches[task]; Place(patch, area.tasks[task], true);
            var r = patch.rectTransform; r.pivot = new Vector2(.5f, 0);
            rising++;
            try
            {
                JellyGarden.Scripts.Juice.RoyalWinSequence.AreaPuff(patch.transform, r.rect.width);
                float t = 0;
                while (t < duration)
                {
                    t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / duration);
                    // Up to 1.04 at 70% of the time, then back to 1; it is fully opaque by a third of the way.
                    float grow = u < .7f ? Mathf.Lerp(0, 1.04f, 1 - (1 - u / .7f) * (1 - u / .7f)) : Mathf.Lerp(1.04f, 1, (u - .7f) / .3f);
                    r.localScale = new Vector3(Mathf.Lerp(.7f, 1, Mathf.Min(1, grow)) * (grow > 1 ? grow : 1), Mathf.Lerp(.45f, 1, Mathf.Min(1, grow)) * (grow > 1 ? grow : 1), 1);
                    r.anchoredPosition = new Vector2(0, -r.rect.height * .06f * (1 - Mathf.Min(1, grow)));
                    patch.color = new Color(1, 1, 1, Mathf.Clamp01(u * 3));
                    yield return null;
                }
            }
            finally
            {
                r.localScale = Vector3.one; r.anchoredPosition = Vector2.zero; patch.color = Color.white; rising--;
                shownState = SavedState();
                if (RoyalAreas.Complete && shown == RoyalAreas.CurrentArea) Show(shown, true);
            }
        }
        int rising;

        Coroutine replay;
        public bool Replaying => replay != null;
        /// <summary>A restored area's restoration played again (the Areas tab's View): its unrestored scene, then each task's
        /// object rising in order, then the restored scene.</summary>
        public void Replay(RoyalAreaInfo area, bool motion)
        {
            StopReplay();
            if (area == null || !area.Ready) return;
            replay = StartCoroutine(RunReplay(area, motion));
        }
        public void StopReplay() { if (replay != null) { StopCoroutine(replay); replay = null; if (native && native.enabled && shown != null) native.SetProgress((1 << shown.tasks.Length) - 1); } }
        IEnumerator RunReplay(RoyalAreaInfo area, bool motion)
        {
            if (area.nativeMotion)
            {
                Show(area, true);
                if (native && native.enabled)
                {
                    if (!motion) { replay = null; yield break; }
                    native.SetProgress(0);
                    yield return new WaitForSecondsRealtime(.6f);
                    for (int i = 0; i < area.tasks.Length; i++) { yield return native.Reveal(i, true); yield return new WaitForSecondsRealtime(.3f); }
                    Show(area, true); replay = null; yield break;
                }
            }
            var sprite = RoyalAreas.Art(area.before);
            if (!sprite || !motion) { Show(area, true); replay = null; yield break; }
            Show(area, false);
            foreach (var p in patches) p.enabled = false;
            yield return new WaitForSecondsRealtime(.6f);
            for (int i = 0; i < area.tasks.Length && i < patches.Count; i++)
            {
                var patch = patches[i]; Place(patch, area.tasks[i], true);
                var r = patch.rectTransform; r.pivot = new Vector2(.5f, 0);
                float t = 0;
                while (t < .55f)
                {
                    t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / .55f), e = 1 - (1 - u) * (1 - u);
                    r.localScale = new Vector3(Mathf.Lerp(.75f, 1, e), Mathf.Lerp(.5f, 1, e), 1); patch.color = new Color(1, 1, 1, Mathf.Clamp01(u * 3));
                    yield return null;
                }
                r.localScale = Vector3.one; patch.color = Color.white;
                yield return new WaitForSecondsRealtime(.3f);
            }
            Show(area, true);
            replay = null;
        }

        // The area and its finished tasks as saved, including which profile (QA or player) is active: when any of them changes
        // outside the area dialogs (a QA session starting or ending), the scene is redrawn from the save.
        static int SavedState() => RoyalAreas.Current * 65536 ^ RoyalThrone.Testing.RoyalSave.GetInt(RoyalAreas.DoneKey, 0) ^ (RoyalAreas.Entered << 20) ^ (RoyalThrone.Testing.RoyalSave.Testing ? 1 << 30 : 0);

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
            // A replay stopped mid-rise left its scale behind on an image the current area shares (bug review 2026-10-05).
            patch.rectTransform.localScale = Vector3.one;
            patch.enabled = visible;
            patch.sprite = visible ? RoyalAreas.Art(task.patch) : null;
            var r = patch.rectTransform;
            r.anchorMin = new Vector2(task.x0, 1 - task.y1); r.anchorMax = new Vector2(task.x1, 1 - task.y0);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        void Update()
        {
            if (native && native.enabled)
            {
                bool visible = !home || home.IsHomeVisible;
                native.SetVisible(visible); native.MotionEnabled = !home || home.MotionEnabled; native.SoundEnabled = RoyalThrone.Testing.RoyalSave.GetInt("Sound", 1) != 0;
                if (visible && !wasHomeVisible && !Hold && !Revealing) ShowCurrent();
                wasHomeVisible = visible;
            }
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

        bool ShowNative(RoyalAreaInfo area, bool asRestored)
        {
            if (!AreaMotion.RoyalAreaMotionPlayer.Available(area.number)) return false;
            if (!native) native = gameObject.AddComponent<AreaMotion.RoyalAreaMotionPlayer>();
            native.enabled = true; native.MotionEnabled = !home || home.MotionEnabled;
            native.SoundEnabled = RoyalThrone.Testing.RoyalSave.GetInt("Sound", 1) != 0;
            int mask = asRestored ? (1 << area.tasks.Length) - 1 : area.number == RoyalAreas.Current ? RoyalThrone.Testing.RoyalSave.GetInt(RoyalAreas.DoneKey, 0) : 0;
            if (!native.Load(area.number, mask)) return false;
            native.Surface.enabled = true;
            shown = area; restored = asRestored; shownState = SavedState(); scene.color = Color.clear;
            if (!art) { art = new GameObject("Area task art", typeof(RectTransform)).GetComponent<RectTransform>(); art.gameObject.layer = 5; art.SetParent(transform, false); art.anchorMin = Vector2.zero; art.anchorMax = Vector2.one; art.offsetMin = art.offsetMax = Vector2.zero; }
            for (int i = 0; i < Mathf.Max(patches.Count, area.tasks.Length); i++)
            {
                var patch = i < patches.Count ? patches[i] : NewPatch(i);
                if (i < area.tasks.Length) Place(patch, area.tasks[i], false);
                patch.enabled = false; fadeStart[i] = -1;
            }
            // Native clouds are already part of the area, including their authored animation.
            foreach (Transform child in transform) if (child.name.StartsWith("Sky cloud ") || child.name.Contains("Sky veil")) child.gameObject.SetActive(false);
            return true;
        }
        void OnEnable() { shownState = int.MinValue; }
        void OnDisable() { StopReplay(); rising = 0; for (int i = 0; i < fadeStart.Count; i++) fadeStart[i] = -1; }
    }
}
