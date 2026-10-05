using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // Motion for the cards built with RoyalCardKit, matching the Level Start card (RoyalLevelCardMotion) without its goal and
    // booster effects: the backdrop dims while the card scales 0.96 -> 1 and fades in (0.22 s, ease-out cubic, no overshoot),
    // its content follows top to bottom 0.05 s apart; pressed buttons darken and sink 2 units (primary) or shrink to 0.98; the
    // primary action's gloss crosses it every few seconds. The inherited entrance clip no longer plays; its events (click
    // ticks, OnFinished) still fire at the clip's own times. Unscaled time, so a card over the paused game animates too.
    public sealed class RoyalCardMotion : MonoBehaviour
    {
        public Image dim;
        public Color dimColor;
        public RectTransform card;
        public CanvasGroup cardGroup;
        // Off for cards whose own clip carries their timing (PreFailed, PrePlay): press feedback and gloss only.
        public bool entrance = true;
        // Opt-in for the win card; existing cards keep their current motion behaviour.
        public bool respectMotionSetting;
        // A question shown over its own card (the pause menu's leave question) moves above anything added to that card later.
        public bool front;
        public RoyalCardReveal[] reveals;
        public RoyalCardControl[] controls;
        public RoyalSweepGraphic gloss;
        public float[] eventTimes;
        public string[] eventNames;

        const float CardIn = .22f, RevealIn = .20f, Rise = 8, EntranceEnd = .45f;
        const float GlossFirst = 1.4f, GlossPeriod = 3.4f, GlossTime = .75f, GlossStrength = .26f;
        float t, glossAt = -10, nextGloss;
        int nextEvent;
        bool settled, reduced;
        // Seconds since the card opened, on the card's own clock (for diagnostics and checks).
        public float Elapsed => t;

        // Takes over the menu's inherited entrance clip: it stops playing, and its events are fired from here instead.
        public void AdoptClip(Animation animation)
        {
            if (!animation || !animation.clip) return;
            animation.playAutomatically = false;
            animation.Stop();
            var events = animation.clip.events;
            eventTimes = new float[events.Length];
            eventNames = new string[events.Length];
            for (int i = 0; i < events.Length; i++) { eventTimes[i] = events[i].time; eventNames[i] = events[i].functionName; }
        }

        void OnEnable()
        {
            t = 0; nextEvent = 0; settled = !entrance; glossAt = -10; nextGloss = GlossFirst;
            reduced = respectMotionSetting && RoyalThrone.Testing.RoyalSave.GetInt("RoyalThrone.HomeMotion", 1) == 0;
            if (front) transform.SetAsLastSibling();
            if (controls != null) foreach (var c in controls) if (c != null) { c.blend = 0; RoyalLevelCardMotion.ApplyPress(c, 0); }
            if (entrance) Pose();
        }

        void OnDisable()
        {
            // Leave everything at rest, so nothing reads a half-played pose while the card is closed.
            if (entrance) { t = EntranceEnd + 10; settled = false; Pose(); }
            if (controls != null) foreach (var c in controls) if (c != null) { c.blend = 0; RoyalLevelCardMotion.ApplyPress(c, 0); }
            if (gloss) { gloss.strength = 0; gloss.SetVerticesDirty(); }
        }

        void Update()
        {
            // At most 50 ms a frame: a hitch while the card opens must not skip its entrance (above 20 fps this is real time).
            t += Mathf.Min(Time.unscaledDeltaTime, .05f);
            if (entrance) Pose();
            if (eventNames != null)
                while (nextEvent < eventNames.Length && t >= eventTimes[nextEvent])
                    SendMessage(eventNames[nextEvent++], SendMessageOptions.DontRequireReceiver);
            Gloss();
            if (controls != null) foreach (var c in controls) RoyalLevelCardMotion.Press(c);
        }

        void Pose()
        {
            if (settled) return;
            float e = RoyalLevelCardMotion.EaseOutCubic(t / CardIn);
            if (dim) { var c = dimColor; c.a *= e; dim.color = c; }
            if (card) card.localScale = Vector3.one * (reduced ? 1 : Mathf.Lerp(.96f, 1, e));
            if (cardGroup) cardGroup.alpha = e;
            if (reveals != null)
                foreach (var r in reveals)
                {
                    float p = RoyalLevelCardMotion.EaseOutCubic((t - r.delay) / RevealIn);
                    for (int i = 0; i < r.rects.Length; i++)
                    {
                        if (!r.rects[i]) continue;
                        r.groups[i].alpha = p;
                        r.rects[i].anchoredPosition = r.rest[i] + new Vector2(0, reduced ? 0 : -Rise * (1 - p));
                    }
                }
            settled = t >= EntranceEnd;
        }

        void Gloss()
        {
            if (!gloss) return;
            // No idle sweep (UI polish, 2026-09-30): the reference's primary buttons are still while the card waits.
            float p = (t - glossAt) / GlossTime;
            float strength = p >= 0 && p <= 1 ? GlossStrength : 0;
            if (strength > 0 || gloss.strength > 0)
            {
                gloss.strength = strength;
                gloss.position = Mathf.Lerp(-.35f, 1.35f, -(Mathf.Cos(Mathf.PI * Mathf.Clamp01(p)) - 1) * .5f);
                gloss.SetVerticesDirty();
            }
        }

        // Reveal groups for a card: its elements (not the skin, the close control or hidden parts) in up to four bands
        // from the title down, 0.05 s apart, as the Level Start card reveals LEVEL, goals, boosters and PLAY.
        public static RoyalCardReveal[] Bands(Transform card, float height, params Transform[] extra)
        {
            float top = height * .5f;
            var bands = new List<RectTransform>[4];
            for (int b = 0; b < 4; b++) bands[b] = new List<RectTransform>();
            void Add(Transform child)
            {
                if (!child || !child.gameObject.activeSelf || child.name == "Royal skin" || child.name == "Close") return;
                var group = child.GetComponent<CanvasGroup>();
                if (group && group.alpha <= 0) return;   // hidden on purpose (RoyalCardKit.Hide)
                if (child.GetComponent<RoyalCalmAppear>() || child.GetComponent<RoyalAppearsLater>()) return;   // shows by itself later (the win card's star, title and lights)
                var r = child as RectTransform;
                if (!r) return;
                int band = Mathf.Clamp(Mathf.FloorToInt((top - r.anchoredPosition.y - 60) / 150), 0, 3);
                bands[band].Add(r);
            }
            foreach (Transform child in card) Add(child);
            foreach (var e in extra) Add(e);
            var result = new List<RoyalCardReveal>();
            for (int b = 0; b < 4; b++) if (bands[b].Count > 0) result.Add(RoyalCardKit.Reveal(.05f + .05f * b, bands[b]));
            return result.ToArray();
        }
    }
}
