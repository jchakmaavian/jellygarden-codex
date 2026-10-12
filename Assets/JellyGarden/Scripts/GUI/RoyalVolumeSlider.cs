using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using static RoyalThrone.Presentation.RoyalCardKit;

namespace RoyalThrone.Presentation
{
    /// <summary>
    /// A Music or SFX volume slider in the royal style (the user's request, 2026-10-07): a dark track filled in gold up to a
    /// cream knob, with an optional percentage. It writes RoyalAudioSettings; turning a switched-off sound up switches it
    /// back on, the SFX slider plays a click at the new level, and the level is saved when the finger lifts. Used by the
    /// Home settings page and the in-level Pause and Settings cards.
    /// </summary>
    public sealed class RoyalVolumeSlider : MonoBehaviour, IPointerUpHandler
    {
        public bool music;
        Slider slider; Text percent; CanvasGroup dim; int shownPercent = -1; float nextPreview; bool changed;

        string Key => music ? "Music" : "Sound";
        float Level => music ? RoyalAudioSettings.Music : RoyalAudioSettings.Sound;
        bool On => PlayerPrefs.GetInt(Key, 1) != 0;

        /// <summary>Builds the slider centred at (x, y) in its parent, `track` units long, and returns it.</summary>
        public static RoyalVolumeSlider Build(Transform parent, string name, bool music, float x, float y, float track, Text percent = null, CanvasGroup dim = null)
        {
            const float trackH = 18, knob = 46, pad = knob * .5f;
            var holder = Container(parent, name);
            RoyalMenuSkin.Place(holder, x, y, track + knob, 64);
            holder.gameObject.AddComponent<Image>().color = Color.clear;   // the whole strip takes the drag
            Shape(holder, "Track rim", 0, 0, track + 5, trackH + 5, (trackH + 5) * .5f, C("86aaf2", .55f), C("4a76d6", .4f));
            Shape(holder, "Track", 0, 0, track, trackH, trackH * .5f, C("061a4c"), C("0b2a78"));
            var fillArea = (RectTransform)Container(holder, "Fill area");
            fillArea.anchorMin = new Vector2(0, .5f); fillArea.anchorMax = new Vector2(1, .5f);
            fillArea.offsetMin = new Vector2(pad, -trackH * .5f + 1.5f); fillArea.offsetMax = new Vector2(-pad, trackH * .5f - 1.5f);
            var fill = (RectTransform)Container(fillArea, "Fill");
            Inset(fill, "Gold", 0, trackH * .5f - 1.5f, C("ffe27a"), C("e3a02a"));
            var handleArea = (RectTransform)Container(holder, "Knob area");
            handleArea.offsetMin = new Vector2(pad, 0); handleArea.offsetMax = new Vector2(-pad, 0);
            var handle = (RectTransform)Container(handleArea, "Knob");
            handle.sizeDelta = new Vector2(knob, knob - 64);
            var shadow = Shape(handle, "Shadow", 0, -3, knob + 2, knob + 2, knob * .5f + 1, C("00103a", .5f), C("00103a", .5f));
            shadow.softness = 6;
            Inset(handle, "Edge", 0, knob * .5f, C("e3a02a"), C("7a3a12"));
            Inset(handle, "Face", 3, knob * .5f - 3, C("fffcf2"), C("e8dcc4"));
            Inset(handle, "Shine", 6, knob * .5f - 6, C("ffffff", .7f), C("ffffff", 0));

            var v = holder.gameObject.AddComponent<RoyalVolumeSlider>();
            v.music = music; v.percent = percent; v.dim = dim ? dim : holder.gameObject.AddComponent<CanvasGroup>();
            v.slider = holder.gameObject.AddComponent<Slider>();
            v.slider.transition = Selectable.Transition.None;
            v.slider.navigation = new Navigation { mode = Navigation.Mode.None };
            v.slider.fillRect = fill; v.slider.handleRect = handle; v.slider.direction = Slider.Direction.LeftToRight;
            v.slider.minValue = 0; v.slider.maxValue = 1;
            v.slider.SetValueWithoutNotify(v.Level);
            v.slider.onValueChanged.AddListener(v.Changed);
            return v;
        }

        void Changed(float value)
        {
            if (music) RoyalAudioSettings.Music = value; else RoyalAudioSettings.Sound = value;
            changed = true;
            if (value > .005f && !On)
            {
                // Turning a switched-off sound up switches it back on, as its own switch would.
                PlayerPrefs.SetInt(Key, 1);
                if (music && MusicBase.Instance) MusicBase.Instance.SetVolume(1);
                if (!music && SoundBase.Instance) SoundBase.Instance.SetVolume(1);
            }
            if (!music && Time.unscaledTime >= nextPreview && SoundBase.Instance)
            {
                nextPreview = Time.unscaledTime + .2f;
                SoundBase.Instance.PlaySound(SoundBase.Instance.click);
            }
        }

        public void OnPointerUp(PointerEventData e) => Save();

        void Save() { if (!changed) return; changed = false; PlayerPrefs.Save(); }

        void OnDisable() => Save();

        void LateUpdate()
        {
            float level = Level;
            if (Mathf.Abs(slider.value - level) > .001f) slider.SetValueWithoutNotify(level);
            if (percent)
            {
                int p = Mathf.RoundToInt(level * 100);
                if (p != shownPercent) { shownPercent = p; percent.text = p + "%"; }
            }
            // A switched-off sound's slider stays usable but dims.
            if (dim) dim.alpha = On ? 1 : .45f;
        }
    }
}
