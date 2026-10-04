using System.Collections.Generic;
using JellyGarden.Scripts.Targets;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // Motion for the Level Start card (built by RoyalLevelCard), replacing the inherited squash and stretch with solid,
    // quiet feedback that leads the eye from LEVEL to the goals, the optional boosters and PLAY:
    // - Entrance: the dim eases in (0.3 s) while the card scales 0.96 -> 1 and fades in (0.22 s, ease-out cubic, no overshoot);
    //   LEVEL, the goals, the boosters and PLAY follow 0.05 s apart, all settled by 0.40 s.
    // - The goal icons brighten once to 1.1x and back, 0.05 s apart.
    // - Boosters hold still; a faint glint crosses the row now and then, and the badges pulse gently (1.06x) only while
    //   no booster is chosen. A tap brightens the card's gold and lifts the art to 1.04x for 0.18 s; a chosen booster
    //   keeps a soft gold rim and glow, and its badge turns into the tick.
    // - PLAY: a gloss crosses it every few seconds, and once more, brighter, just after a booster is chosen. Pressing
    //   darkens it slightly and sinks it 2 units; x and ? darken slightly and shrink to 0.98.
    // The inherited entrance clip's events (two click ticks and OnFinished, which resets the boosters) still fire at the
    // clip's own times. Everything runs on unscaled time.
    public sealed class RoyalLevelCardMotion : MonoBehaviour
    {
        public sealed class Booster
        {
            public RectTransform root, art, plus, plate, check;
            public RoyalCardShape glow, rim, rimOuter, plateFace;
            public Color rimFrom, rimTo;
            public RoyalSweepGraphic sweep;
            public RoyalPressTracker press;
            internal float selected, pressed, swapAt = -10, shownRim = -1;
            internal int state = -1;
        }

        public Image dim;
        public Color dimColor;
        public RectTransform card, halo;
        public CanvasGroup cardGroup;
        public Text number;
        public RoyalCardReveal[] reveals;
        public Transform targets;
        public Booster[] boosters;
        public RoyalCardControl play, close, help;
        public RoyalSweepGraphic playSweep;
        public RoyalCardShape playGold;

        // The inherited entrance clip ("Menu", 0.35 s): its events and their times.
        static readonly float[] clipEventTimes = { .25f, .33f, .35f };
        static readonly string[] clipEvents = { "PlaySoundButton", "PlaySoundButton", "OnFinished" };
        const float CardIn = .22f, CardFade = .26f, DimIn = .30f, RevealIn = .20f, Rise = 8, EntranceEnd = .45f;
        const float GoalGlowAt = .30f, GoalGlowStep = .05f, GoalGlowTime = .36f, GoalGlowPeak = .10f;
        // A pressed booster eases down to 0.98 (0.08 s in, 0.14 s out) like every card control; no pop, and no idle badge pulse.
        const float PressIn = .08f, PressOut = .14f, PressScale = .98f, SwapTime = .16f, SelectTime = .15f;
        const float GlossFirst = 1.4f, GlossPeriod = 3.4f, GlossTime = .75f, GlossStrength = .30f, EmphasisStrength = .45f;
        static readonly Color RimFlash = new Color(1, .98f, .86f), GreenTop = new Color(.235f, .784f, .588f), GreenBottom = new Color(.02f, .353f, .27f);
        static readonly Color NavyTop = new Color(.149f, .337f, .722f), NavyBottom = new Color(.039f, .141f, .4f);

        static Material additive;
        float t;
        // Seconds since the card opened, on the card's own clock (for diagnostics and checks).
        public float Elapsed => t;
        int nextEvent;
        bool entranceDone;
        float glossAt = -10, glossStrength, nextGloss, emphasisAt = -10;
        Color playGoldFrom, playGoldTo;
        bool goldCaptured;
        string shownNumber;
        readonly List<Image> goalGlows = new List<Image>();

        static Material Additive
        {
            get
            {
                if (!additive)
                {
                    var shader = Resources.Load<Shader>("RoyalGameplay/UIAdditive");
                    if (shader && shader.isSupported) additive = new Material(shader) { name = "Royal UI additive", hideFlags = HideFlags.DontSave };
                }
                return additive;
            }
        }

        void OnEnable()
        {
            t = 0; nextEvent = 0; entranceDone = false;
            glossAt = -10; emphasisAt = -10; nextGloss = GlossFirst;
            if (playGold && !goldCaptured) { playGoldFrom = playGold.from; playGoldTo = playGold.to; goldCaptured = true; }
            if (boosters != null)
                foreach (var b in boosters) { b.state = -1; b.swapAt = -10; b.pressed = 0; b.selected = 0; b.shownRim = -1; }
            ClearGoalGlows();
            // Only the entrance's first pose here: the boosters' states are read from the first Update on, once every
            // BoostIcon.OnEnable has run (it clears a tick left over from an earlier opening).
            ApplyEntrance();
        }

        void OnDisable()
        {
            // Leave everything at rest, so nothing reads a half-played pose while the card is closed.
            t = EntranceEnd + 10; entranceDone = false;
            ApplyEntrance();
            ClearGoalGlows();
            if (boosters != null)
                foreach (var b in boosters)
                {
                    if (b.art) b.art.localScale = Vector3.one;
                    if (b.plus) b.plus.localScale = Vector3.one;
                    if (b.plate) b.plate.localScale = Vector3.one;
                    if (b.sweep) b.sweep.strength = 0;
                }
            foreach (var c in new[] { play, close, help }) if (c != null) { c.blend = 0; ApplyPress(c, 0); }
            if (playSweep) { playSweep.strength = 0; playSweep.SetVerticesDirty(); }
            if (playGold && goldCaptured) { playGold.from = playGoldFrom; playGold.to = playGoldTo; playGold.SetVerticesDirty(); }
        }

        void Update()
        {
            // At most 50 ms a frame: a hitch while the card opens must not skip its entrance (above 20 fps this is real time).
            t += Mathf.Min(Time.unscaledDeltaTime, .05f);
            Pose();
            while (nextEvent < clipEvents.Length && t >= clipEventTimes[nextEvent])
                SendMessage(clipEvents[nextEvent++], SendMessageOptions.DontRequireReceiver);
        }

        void LateUpdate()
        {
            // The dark halo behind the level number follows the number's width.
            if (number && halo && number.text != shownNumber)
            {
                shownNumber = number.text;
                halo.sizeDelta = new Vector2(Mathf.Clamp(number.preferredWidth + 34, 120, 300), halo.sizeDelta.y);
            }
        }

        void Pose()
        {
            ApplyEntrance();
            GoalGlow();
            Boosters();
            Gloss();
            foreach (var c in new[] { play, close, help }) Press(c);
        }

        void ApplyEntrance()
        {
            if (entranceDone) return;
            float e = EaseOutCubic(t / CardIn);
            // The dim eases in and out (smoothstep over 0.3 s; 2026-10-04): with the card's ease-out it reached half its
            // darkness in the first frame, which read as a cut.
            if (dim) { var c = dimColor; c.a *= Mathf.SmoothStep(0, 1, t / DimIn); dim.color = c; }
            if (card) card.localScale = Vector3.one * Mathf.Lerp(.96f, 1, e);
            // Its opacity eases in (smoothstep over 0.26 s): the first frames are long while the card is built, and with the
            // ease-out it was half there in the first one.
            if (cardGroup) cardGroup.alpha = Mathf.SmoothStep(0, 1, t / CardFade);
            if (reveals != null)
                foreach (var r in reveals)
                {
                    float p = EaseOutCubic((t - r.delay) / RevealIn);
                    for (int i = 0; i < r.rects.Length; i++)
                    {
                        if (!r.rects[i]) continue;
                        r.groups[i].alpha = p;
                        r.rects[i].anchoredPosition = r.rest[i] + new Vector2(0, -Rise * (1 - p));
                    }
                }
            entranceDone = t >= EntranceEnd;
        }

        // The goal icons brighten once, one after another: an additive copy of each icon at up to a tenth of its colour.
        void GoalGlow()
        {
            if (t < GoalGlowAt || !targets) return;
            if (goalGlows.Count == 0 && t < GoalGlowAt + GoalGlowTime && Additive)
                foreach (var icon in targets.GetComponentsInChildren<TargetIcon>())
                {
                    var source = icon.imageRenderer;
                    if (!source || !source.sprite) continue;
                    var go = new GameObject("Royal goal glow", typeof(RectTransform));
                    go.transform.SetParent(source.transform, false);
                    var r = (RectTransform)go.transform;
                    r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
                    var glow = go.AddComponent<Image>();
                    glow.sprite = source.sprite; glow.preserveAspect = source.preserveAspect; glow.raycastTarget = false;
                    glow.material = Additive; glow.color = new Color(1, 1, 1, 0);
                    goalGlows.Add(glow);
                }
            bool active = false;
            for (int i = 0; i < goalGlows.Count; i++)
            {
                var glow = goalGlows[i];
                if (!glow) continue;
                float p = (t - GoalGlowAt - i * GoalGlowStep) / GoalGlowTime;
                float a = p > 0 && p < 1 ? Mathf.Sin(p * Mathf.PI) * GoalGlowPeak : 0;
                glow.color = new Color(1, 1, 1, a);
                active |= p < 1;
            }
            if (!active && goalGlows.Count > 0) ClearGoalGlows();
        }

        void ClearGoalGlows()
        {
            foreach (var glow in goalGlows) if (glow) Destroy(glow.gameObject);
            goalGlows.Clear();
        }

        void Boosters()
        {
            if (boosters == null) return;
            float anySelected = 0;
            foreach (var b in boosters) anySelected = Mathf.Max(anySelected, b.selected);
            for (int i = 0; i < boosters.Length; i++)
            {
                var b = boosters[i];
                if (!b.root) continue;
                // The badge follows BoostIcon's own state: the + (none owned), the count, or the tick (chosen).
                int state = b.check && b.check.gameObject.activeInHierarchy ? 2 : b.plus && b.plus.gameObject.activeSelf ? 0 : 1;
                if (state != b.state)
                {
                    if (b.state >= 0) b.swapAt = t;
                    if (state == 2 && b.state >= 0 && b.state != 2) emphasisAt = t + .12f;
                    b.state = state;
                    if (b.plateFace)
                    {
                        b.plateFace.from = state == 2 ? GreenTop : NavyTop;
                        b.plateFace.to = state == 2 ? GreenBottom : NavyBottom;
                        b.plateFace.SetVerticesDirty();
                    }
                }
                b.selected = Mathf.MoveTowards(b.selected, state == 2 ? 1 : 0, Time.unscaledDeltaTime / SelectTime);
                float held = b.press && b.press.Held ? 1 : 0;
                b.pressed = Mathf.MoveTowards(b.pressed, held, Time.unscaledDeltaTime / (held > b.pressed ? PressIn : PressOut));
                float pop = EaseOutCubic(b.pressed);
                if (b.art) b.art.localScale = Vector3.one * Mathf.Lerp(1, PressScale, pop);
                float rim = Mathf.Clamp01(.55f * pop + .35f * b.selected);
                if (b.rim && !Mathf.Approximately(rim, b.shownRim))
                {
                    b.shownRim = rim;
                    b.rim.from = Color.Lerp(b.rimFrom, RimFlash, rim);
                    b.rim.to = Color.Lerp(b.rimTo, RimFlash, rim * .7f);
                    b.rim.SetVerticesDirty();
                }
                if (b.glow) b.glow.color = new Color(1, 1, 1, .6f * b.selected + .15f * pop);

                // Badge: still, with a short settle when it changes state.
                float swap = (t - b.swapAt) / SwapTime;
                float settle = swap >= 0 && swap < 1 ? Mathf.Lerp(.82f, 1, EaseOutCubic(swap)) : 1;
                var badgeScale = Vector3.one * settle;
                if (b.plus) b.plus.localScale = badgeScale;
                if (b.plate) b.plate.localScale = badgeScale;

                // No idle glint across the boosters (the reference's controls are still).
                if (b.sweep && b.sweep.strength > 0) { b.sweep.strength = 0; b.sweep.SetVerticesDirty(); }
            }
        }

        // PLAY's gloss: once (brighter, with a warmer rim) just after a booster is chosen, the card's answer to the choice.
        void Gloss()
        {
            if (!playSweep) return;
            if (emphasisAt > 0 && t >= emphasisAt) { glossAt = t; glossStrength = EmphasisStrength; nextGloss = t + GlossPeriod; emphasisAt = -10; }
            // Only that answer to a choice: no idle sweep every few seconds (the reference's PLAY is still while the card waits).
            float p = (t - glossAt) / GlossTime;
            float strength = p >= 0 && p <= 1 ? glossStrength : 0;
            if (strength > 0 || playSweep.strength > 0)
            {
                playSweep.strength = strength;
                playSweep.position = Mathf.Lerp(-.35f, 1.35f, EaseInOutSine(p));
                playSweep.SetVerticesDirty();
            }
            if (playGold && goldCaptured)
            {
                float warm = glossStrength > GlossStrength && p >= 0 && p <= 1 ? Mathf.Sin(p * Mathf.PI) * .5f : 0;
                var from = Color.Lerp(playGoldFrom, RimFlash, warm);
                if (from != playGold.from) { playGold.from = from; playGold.to = Color.Lerp(playGoldTo, RimFlash, warm * .6f); playGold.SetVerticesDirty(); }
            }
        }

        // Pressed controls ease down to 0.98 and darken slightly (8%), in over 0.08 s and back over 0.14 s, ease-out, no overshoot.
        // The same press on every card control, PLAY and the primary buttons included.
        public static void Press(RoyalCardControl c)
        {
            if (c == null || !c.press) return;
            bool off = c.button && !c.button.IsInteractable();
            float target = !off && c.press.Held ? 1 : 0;
            if (c.blend == target && off == c.off) return;
            c.off = off;
            c.blend = Mathf.MoveTowards(c.blend, target, Time.unscaledDeltaTime / (target > c.blend ? .08f : .14f));
            ApplyPress(c, EaseOutCubic(c.blend));
        }

        public static void ApplyPress(RoyalCardControl c, float e)
        {
            float shade = Mathf.Lerp(1, .92f, e) * (c.off ? .6f : 1);
            var tint = new Color(shade, shade, shade, 1);
            foreach (var g in c.graphics) if (g) g.canvasRenderer.SetColor(tint);
            // Each part scales about its own centre; a part away from the control's centre is also drawn in towards it, so the
            // control shrinks as one piece.
            for (int i = 0; i < c.parts.Length; i++)
            {
                if (!c.parts[i]) continue;
                c.parts[i].localScale = Vector3.one * Mathf.Lerp(1, .98f, e);
                c.parts[i].anchoredPosition = c.rest[i] * Mathf.Lerp(1, .98f, e);
            }
        }

        public static float EaseOutCubic(float x) { x = Mathf.Clamp01(x); float u = 1 - x; return 1 - u * u * u; }
        public static float EaseInOutSine(float x) => -(Mathf.Cos(Mathf.PI * Mathf.Clamp01(x)) - 1) * .5f;
    }
}
