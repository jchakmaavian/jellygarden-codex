// The Royal Throne gem break, fall and refill on the King Leo board, beside the
// original boosters (StoryOriginalBoosters) and TNT (StoryOriginalTnt).
//
// royal-throne drove these in code (Item.JuiceDestroyCor, FallingCor,
// LandingSquashCor, LevelManager.GenerateNewItems) with the values in its
// JuiceConfig asset. This is that motion, with those values, on King Leo's items;
// hooks in Item and LevelManager hand it the work:
//
//   gem cleared   a 3-match breaks at once; a bigger match pulls in, squashes and
//                 rebounds first, 12ms apart along the group; then the gem's sprite
//                 is swapped for the imported explosion in its colour
//                 (StoryOriginalBoosters.MatchBreak). A rocket or TNT hit breaks at once.
//   gem falls     closed-form gravity (4 units/s, +32/s², at most 19/s), then a
//                 squash and two hops scaled by how hard it landed, with the imported
//                 fall_hit sound. Boosters hop without squashing.
//   refill        new gems queue above their column's top cell, fade in as they
//                 cross it, and the columns start in a wave from the centre out.
//
// WHAT IT DOES NOT CHANGE
//
// Which gems clear, where they fall, scoring, and the animationFinished and
// falling flags the cascade waits on. It changes how long and how it looks: while
// on, it replaces the board's own pop (Match3PieceMotion), the neighbour punch
// and the kit's linear fall.
//
// THE SWITCH
//
// -originalmotion / -nooriginalmotion, or the StoryVfx.OriginalMotion pref; on by
// default, and only with the imported assets (StoryOriginalBoosters.Ready).

using System;
using System.Collections;
using System.Collections.Generic;
using RoyalThrone.ReferenceGameplay.Preview;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryOriginalMotion
    {
        // JuiceConfig.asset on royal-throne.
        const float FallSpeed = 4f, FallAcceleration = 32f, FallMaxSpeed = 19f;
        const float LandingSquash = .05f, LandingBounce = .06f, LandingDuration = .21f;
        const float MatchAnticipation = .015f, MatchAnticipationScale = .05f, MatchSquashDuration = .03f, MatchSquashAmount = .07f;
        const float MatchReboundDuration = .02f, MatchReboundAmount = .12f, MatchDestroyDuration = .05f, MatchStagger = .012f;
        // RoyalAudioDirector's landing cue: fall_hit_1 at 0.16, at most every 0.09s.
        const string LandAudio = "fall_hit_1__15531";
        const float LandGain = .16f, LandInterval = .09f;
        // LevelManager.GenerateNewItems: each column starts 22ms later per column from the centre.
        const float ColumnDelay = .022f;

        public static bool Enabled
        {
            get
            {
                if (_enabled.HasValue) return _enabled.Value;
                _enabled = StoryVfxFlags.On("-originalmotion", "StoryVfx.OriginalMotion");
                return _enabled.Value;
            }
            set { _enabled = value; }
        }
        private static bool? _enabled;

        public static bool Ready => Application.isPlaying && Enabled && StoryOriginalBoosters.Ready;

        // Counters for the validation harness.
        public static int Falls { get; private set; }
        public static int Landings { get; private set; }
        public static int Entries { get; private set; }
        public static int ImmediateBreaks { get; private set; }
        public static int PoppedBreaks { get; private set; }
        public static int HitBreaks { get; private set; }
        public static int Breaking { get; private set; }

        struct Match { public int Size, Index; }
        private static readonly Dictionary<Item, Match> _marks = new Dictionary<Item, Match>();
        private static readonly Dictionary<int, int> _stacked = new Dictionary<int, int>();
        private static int _stackedColumn = -1;
        private static float _lastLand = -10f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _enabled = null;
            _marks.Clear();
            _stacked.Clear();
            _stackedColumn = -1;
            _lastLand = -10f;
            Falls = Landings = Entries = ImmediateBreaks = PoppedBreaks = HitBreaks = Breaking = 0;
        }

        // ── Break ──────────────────────────────────────────────────────────

        /// <summary>LevelManager's match loop: how big this gem's group is and where in it the gem sits.</summary>
        public static void MarkMatch(Item item, int size, int index)
        {
            if (item == null || !Ready || item.currentType != ItemsTypes.NONE) return;
            _marks[item] = new Match { Size = size, Index = Mathf.Max(0, index) };
        }

        /// <summary>
        /// Item.JuiceDestroyCor for a plain gem, run on LevelManager like the board's own
        /// pop. <paramref name="rest"/> is the gem's resting scale (Item.PopClaimForKit).
        /// <paramref name="finished"/> raises animationFinished, which the cascade waits on.
        /// </summary>
        public static IEnumerator Break(Item item, Vector3 rest, bool rocketHit, string animName, Action finished)
        {
            Breaking++;
            try
            {
                if (item == null) yield break;
                // A Color Ball's beams draw the burst for the gems it clears this frame.
                bool quiet = StoryOriginalBoosters.ColorBallClearing;
                Vector3 at = item.square != null ? item.square.transform.position : item.transform.position;
                int colour = item.color;
                // A spent combo partner just goes; the Color Ball's own visual has taken its place.
                if (_silent.Remove(item))
                {
                    foreach (var r in item.GetComponentsInChildren<SpriteRenderer>(true))
                        if (r != null && !IsBonus(r.transform, item.transform)) r.enabled = false;
                    yield break;
                }
                bool matched = _marks.TryGetValue(item, out var mark);
                _marks.Remove(item);

                // A rocket has already arrived. A TNT blast reaches this gem's ring (read now, while
                // DestroyPackage's wave is open) and shows it 0.018s more before it shatters.
                if (rocketHit || animName == "destroy_package")
                {
                    HitBreaks++;
                    if (!rocketHit)
                        yield return new WaitForSeconds(StoryOriginalTnt.WaveDelay(item) + StoryOriginalTnt.ShatterAfterHit);
                    Shatter(item, at, colour, quiet);
                    yield return new WaitForSeconds(rocketHit ? .065f : .035f);
                    yield break;
                }

                bool smooth = matched && mark.Size >= 3;
                bool immediate = smooth && mark.Size == 3 && item.NextType == ItemsTypes.NONE;
                float anticipationTime = immediate ? 0f : smooth ? .03f : MatchAnticipation;
                float squashTime = immediate ? 0f : smooth ? .045f : MatchSquashDuration;
                float reboundTime = immediate ? 0f : smooth ? .045f : MatchReboundDuration;
                float destroyTime = smooth ? .09f : MatchDestroyDuration;
                float squash = immediate ? 0f : smooth ? .045f : MatchSquashAmount;
                float rebound = immediate ? 0f : smooth ? .065f : MatchReboundAmount;
                // Neighbours never pop on the same frame, except a plain three which breaks as one.
                float stagger = smooth && mark.Size != 3 ? mark.Index * MatchStagger : 0f;
                if (immediate) ImmediateBreaks++; else PoppedBreaks++;
                if (stagger > 0f) yield return new WaitForSeconds(stagger);

                if (!immediate)
                {
                    // 1. anticipation: a small pull inward.
                    float t = 0f;
                    while (t < 1f && item != null)
                    {
                        t += Time.deltaTime / Mathf.Max(.0001f, anticipationTime);
                        float e = Ease(t, smooth, 2);
                        float s = 1f - MatchAnticipationScale * e;
                        Pose(item, rest, s, s);
                        yield return null;
                    }
                    // 2. squash: wide and short.
                    float from = 1f - MatchAnticipationScale;
                    t = 0f;
                    while (t < 1f && item != null)
                    {
                        t += Time.deltaTime / Mathf.Max(.0001f, squashTime);
                        float e = Ease(t, smooth, 3);
                        Pose(item, rest, Mathf.Lerp(from, 1f + squash, e), Mathf.Lerp(from, 1f - squash * 1.15f, e));
                        yield return null;
                    }
                    // 3. rebound: a short stretch back up.
                    t = 0f;
                    while (t < 1f && item != null)
                    {
                        t += Time.deltaTime / Mathf.Max(.0001f, reboundTime);
                        float e = Ease(t, smooth, 2);
                        Pose(item, rest, Mathf.Lerp(1f + squash, 1f - rebound * .35f, e),
                            Mathf.Lerp(1f - squash * 1.15f, 1f + rebound, e));
                        yield return null;
                    }
                }

                // 4. break apart: the explosion takes over the gem's silhouette on this frame.
                if (item != null) Shatter(item, at, colour, quiet);
                yield return new WaitForSeconds(destroyTime);
            }
            finally
            {
                Breaking = Mathf.Max(0, Breaking - 1);
                finished?.Invoke();
            }
        }

        static float Ease(float t, bool smooth, int power)
        {
            t = Mathf.Clamp01(t);
            return smooth ? Mathf.SmoothStep(0f, 1f, t) : 1f - Mathf.Pow(1f - t, power);
        }

        static void Pose(Item item, Vector3 rest, float x, float y) =>
            item.transform.localScale = new Vector3(rest.x * x, rest.y * y, rest.z);

        // The gem's renderers go off as the imported explosion starts, so it reads as breaking.
        // The TIME levels' "5sec" bonus is left alone: it flies off on its own.
        static void Shatter(Item item, Vector3 at, int colour, bool quiet)
        {
            if (!quiet) StoryOriginalBoosters.MatchBreak(at, colour);
            foreach (var r in item.GetComponentsInChildren<SpriteRenderer>(true))
                if (r != null && !IsBonus(r.transform, item.transform)) r.enabled = false;
        }

        static bool IsBonus(Transform t, Transform root)
        {
            for (; t != null && t != root; t = t.parent) if (t.name.StartsWith("5sec")) return true;
            return false;
        }

        // ── Fall ───────────────────────────────────────────────────────────

        /// <summary>Item.FallDistanceAt: closed-form, so the speed is the same at 30 and 60 FPS.</summary>
        public static float FallDistanceAt(float elapsed, float initialSpeed, float acceleration, float maxSpeed)
        {
            elapsed = Mathf.Max(0, elapsed);
            maxSpeed = Mathf.Max(.01f, maxSpeed);
            initialSpeed = Mathf.Clamp(initialSpeed, .01f, maxSpeed);
            acceleration = Mathf.Max(0, acceleration);
            if (acceleration <= .0001f) return initialSpeed * elapsed;
            float accelerating = Mathf.Min(elapsed, (maxSpeed - initialSpeed) / acceleration);
            return initialSpeed * accelerating + .5f * acceleration * accelerating * accelerating +
                maxSpeed * (elapsed - accelerating);
        }

        /// <summary>
        /// Item.FallingCor: the whole fall of <paramref name="item"/> into <paramref name="target"/>,
        /// run inside the item's own FallingCor. A piece whose square changes mid-fall restarts
        /// from where it is; a newer fall of the same piece takes over.
        /// </summary>
        public static IEnumerator Fall(Item item, Square target, bool animate)
        {
            if (item == null || target == null || item.destroying) yield break;
            var state = StoryOriginalFall.Of(item);
            int version = ++state.Version;
            state.StopLanding(item);
            item.falling = true;
            Falls++;
            bool entering = !float.IsNaN(state.EntryEdge);
            if (entering) { state.BeginFade(item); state.ApplyFade(item); }
            if (state.Delay > 0f)
            {
                float wait = state.Delay; state.Delay = 0f;
                while (wait > 0f)
                {
                    if (version != state.Version || item.destroying) yield break;
                    if (!Paused) wait -= Time.deltaTime;
                    yield return null;
                }
            }

            Vector3 start = item.transform.position;
            Vector3 end = target.transform.position + Vector3.back * .2f;
            // Z is depth, not travel: short and diagonal steps animate too.
            float distance = Vector2.Distance(start, end), elapsed = 0f, travelled = 0f;
            while (distance > .0001f && travelled < distance)
            {
                if (version != state.Version || item.destroying) yield break;
                if (item.square != target) { item.falling = false; item.StartFalling(); yield break; }
                if (Paused) { yield return null; continue; }
                elapsed += Time.deltaTime;
                travelled = FallDistanceAt(elapsed, FallSpeed, FallAcceleration, FallMaxSpeed);
                item.transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(travelled / distance));
                if (entering) state.ApplyFade(item);
                yield return null;
            }
            if (version != state.Version || item.destroying) yield break;
            if (item.square != target) { item.falling = false; item.StartFalling(); yield break; }
            item.transform.position = end;
            if (entering) state.EndFade();
            item.falling = false;
            item.justCreatedItem = false;
            if (distance > .0001f && animate)
            {
                // How hard it lands: its speed against the top speed, and how far it fell.
                float cell = StoryBoardScale.Pitch;
                float impact = Mathf.Min(FallMaxSpeed, FallSpeed + FallAcceleration * elapsed);
                float strength = Mathf.Clamp01(impact / FallMaxSpeed) * Mathf.Clamp01(distance / Mathf.Max(.01f, cell));
                state.Landing = item.StartCoroutine(Land(item, state, strength));
                Landings++;
                if (Time.unscaledTime - _lastLand >= LandInterval && SoundBase.Instance != null)
                {
                    _lastLand = Time.unscaledTime;
                    SoundBase.Instance.PlaySound(ReferenceTnt.Clip(LandAudio), LandGain);
                }
            }
        }

        static bool Paused => LevelManager.THIS != null && LevelManager.THIS.gameStatus == GameState.Pause;

        // Item.LandingSquashCor: contact squash 0-16%, a hop to 68%, a second hop at 30% of it.
        static IEnumerator Land(Item item, StoryOriginalFall state, float strength)
        {
            strength = Mathf.Clamp01(strength);
            if (item.destroying || strength <= 0f) { state.Landing = null; yield break; }
            int version = state.Version;
            var landedSquare = item.square;
            var landedType = item.currentType;
            bool rigid = landedType != ItemsTypes.NONE;
            Vector3 rest = item.transform.position, applied = rest;
            float hop1 = StoryBoardScale.Pitch * LandingBounce * strength, hop2 = hop1 * .3f;
            bool moved = false;
            float t = 0f;
            while (t < 1f && !item.destroying && !item.falling && version == state.Version &&
                item.square == landedSquare && item.currentType == landedType)
            {
                // A swap took the piece: leave its position to the swap.
                if ((item.transform.position - applied).sqrMagnitude > .000001f) { moved = true; break; }
                if (Paused) { yield return null; continue; }
                t += Time.deltaTime / LandingDuration;
                float k = Mathf.Clamp01(t), lift = 0f, squash;
                if (k < .16f) squash = Mathf.Sin(k / .16f * Mathf.PI) * strength;
                else if (k < .68f)
                {
                    float u = (k - .16f) / .52f;
                    lift = hop1 * 4f * u * (1f - u);
                    squash = -.3f * strength * Mathf.Sin(Mathf.Min(u / .5f, 1f) * Mathf.PI);
                }
                else
                {
                    float u = (k - .68f) / .32f;
                    lift = hop2 * 4f * u * (1f - u);
                    squash = .35f * strength * Mathf.Sin(Mathf.Min(u / .3f, 1f) * Mathf.PI);
                }
                applied = rest + Vector3.up * lift;
                item.transform.position = applied;
                if (!rigid) item.PopScale(1f + squash * LandingSquash * .75f, 1f - squash * LandingSquash);
                yield return null;
            }
            if (item == null || item.destroying || version != state.Version) yield break;
            if (!moved && !item.falling && item.square == landedSquare &&
                (item.transform.position - applied).sqrMagnitude < .000001f)
                item.transform.position = rest;
            if (!rigid) item.PopRelease();
            state.Landing = null;
        }

        // ── Refill ─────────────────────────────────────────────────────────

        /// <summary>LevelManager.GenerateNewItems, before its columns: a new refill pass.</summary>
        public static void BeginRefill()
        {
            _stacked.Clear();
            _stackedColumn = -1;
        }

        /// <summary>
        /// A refill gem: queued above its column's top cell (the lowest first), fading in as it
        /// crosses that edge, and held back a little further from the centre column.
        /// </summary>
        public static void Enter(Item created, bool falling, int col, int row)
        {
            var lm = LevelManager.THIS;
            if (created == null || !falling || lm == null || !Ready) return;
            if (col != _stackedColumn) { _stacked.Clear(); _stackedColumn = col; }
            int top = row;
            while (top > 0 && lm.GetSquare(col, top - 1) != null && lm.GetSquare(col, top - 1).CanGoInto()) top--;
            _stacked.TryGetValue(top, out int k);
            _stacked[top] = k + 1;
            float cell = StoryBoardScale.Pitch;
            Vector3 topCell = lm.GetSquare(col, top).transform.position;
            Vector3 target = lm.GetSquare(col, row).transform.position;
            created.transform.position = new Vector3(target.x, topCell.y + cell * (k + 1), target.z - .2f);
            var state = StoryOriginalFall.Of(created);
            state.EntryEdge = topCell.y + cell * .5f;
            state.Delay = ColumnDelay * Mathf.Abs(col - (lm.maxCols - 1) * .5f);
            Entries++;
        }

        public static void Clear() { _marks.Clear(); _silent.Clear(); }

        /// <summary>A piece leaving with no effect of its own (a Color Ball combo's spent partner).</summary>
        public static void Silence(Item item) { if (item != null) _silent.Add(item); }
        private static readonly HashSet<Item> _silent = new HashSet<Item>();
    }

    /// <summary>Per-piece fall state: which fall owns it, its landing, and its refill entry.</summary>
    [DisallowMultipleComponent]
    public sealed class StoryOriginalFall : MonoBehaviour
    {
        public int Version;
        public Coroutine Landing;
        public float EntryEdge = float.NaN, Delay;
        SpriteRenderer[] _fading;
        float[] _alphas;

        public static StoryOriginalFall Of(Item item)
        {
            var state = item.GetComponent<StoryOriginalFall>();
            return state != null ? state : item.gameObject.AddComponent<StoryOriginalFall>();
        }

        public void StopLanding(Item item)
        {
            if (Landing == null) return;
            item.StopCoroutine(Landing);
            Landing = null;
            if (!item.destroying) item.PopRelease();
        }

        public void BeginFade(Item item)
        {
            if (_fading != null) return;   // a restarted fall keeps the alphas it captured first
            _fading = item.GetComponentsInChildren<SpriteRenderer>(true);
            _alphas = new float[_fading.Length];
            for (int i = 0; i < _fading.Length; i++) _alphas[i] = _fading[i].color.a;
        }

        // Hidden while wholly above the edge; whole once its centre is a fifth of a cell inside.
        public void ApplyFade(Item item)
        {
            if (_fading == null || float.IsNaN(EntryEdge)) return;
            float cell = StoryBoardScale.Pitch;
            float a = Mathf.Clamp01((EntryEdge + cell * .5f - item.transform.position.y) / (cell * .7f));
            a = a * a * (3f - 2f * a);
            for (int i = 0; i < _fading.Length; i++)
                if (_fading[i] != null) { var c = _fading[i].color; c.a = _alphas[i] * a; _fading[i].color = c; }
        }

        public void EndFade()
        {
            if (_fading != null)
                for (int i = 0; i < _fading.Length; i++)
                    if (_fading[i] != null) { var c = _fading[i].color; c.a = _alphas[i]; _fading[i].color = c; }
            _fading = null; _alphas = null; EntryEdge = float.NaN;
        }
    }
}
