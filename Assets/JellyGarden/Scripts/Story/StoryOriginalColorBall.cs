// The Royal Throne Color Ball on the King Leo board: its rules and its sequence,
// as royal-throne played them (RoyalPowerupDirector.Orb / DualColorBall,
// ColorBallV4Sequence.AnimateColorBallV4Targets, ColorBallVisualV4, ColorBallVfxV4,
// ColorBallChargedTargetV4), with the imported art, clips, particles and sounds.
//
// THE RULES
//
//   swapped with a gem   targets every PLAIN gem of that gem's colour; specials are
//                        not targets (EligibleColorBallTarget)
//   tapped               fires at the colour most plain gems share (first on a tie)
//   + Rocket / + TNT     the most common colour; each target turns into the partner's
//                        exact type as its beam lands, and they all fire together once
//                        the ball is gone. The partner is spent.
//   + Color Ball         the two-ball clip, then every cell on the board is hit at once
//
// THE SEQUENCE
//
//   the ball plays its use clip with its use particles and start sound and charges
//   for 0.32s; then one beam per target in board order, every 0.09s (a 1/30s aim, then
//   the launch; 0.095-0.085s for more than 28), each crossing at 17 cells a second
//   (0.14-0.37s) with a ray sound. A beam's contact bursts on its target, which is held
//   charged (StoryOriginalChargedTarget) or, with a payload, squashes and converts
//   2/30s later. Each link stays lit at 76%, fading over 0.46s. When the last beam has
//   landed (+0.14s), the ball finishes: every charged gem clears at once, the links go
//   out within 0.1s, the final pulse comes at 0.18s and the ball is gone at 0.46s; the
//   converted pieces then fire.
//
// The board is held for the whole sequence (LevelManager.IsAllDestoyFinished), as
// RoyalPowerupDirector.Busy held royal-throne's.
//
// THE SWITCH
//
// -originalcolorball / -nooriginalcolorball, or the StoryVfx.OriginalColorBall pref;
// on by default, and only with the imported assets (StoryOriginalBoosters.Ready). Off,
// the kit's colour bomb and its combos play exactly as before.

using System.Collections;
using System.Collections.Generic;
using RoyalThrone.ReferenceGameplay.Preview;
using UnityEngine;
using static RoyalThrone.ReferenceGameplay.Preview.ReferenceBoosters;

namespace JellyGarden.Scripts.Story
{
    public static class StoryOriginalColorBall
    {
        // ColorBallVisualV4 / ColorBallV4Sequence / ColorBallVfxV4.
        public const float ChargeSeconds = .32f, AimSeconds = 1f / 30f, ConvertAfter = 2f / 30f;
        public const float BeamGap = .09f, LogicalTail = .14f, LinkResidue = .46f, LinkOpacity = .76f;
        public const float FinishSeconds = .46f, FinalPulseAt = .18f, LinksOutAfterFinish = .10f;
        const int BoardColours = 6;

        public static bool Enabled
        {
            get
            {
                if (_enabled.HasValue) return _enabled.Value;
                _enabled = StoryVfxFlags.On("-originalcolorball", "StoryVfx.OriginalColorBall");
                return _enabled.Value;
            }
            set { _enabled = value; }
        }
        private static bool? _enabled;

        public static bool Ready => Application.isPlaying && Enabled && StoryOriginalBoosters.Ready;

        /// <summary>A Color Ball sequence is running; LevelManager holds the cascade for it.</summary>
        public static bool Busy => _running > 0;

        // Counters for the validation harness.
        public static int Plays { get; private set; }
        public static int Combos { get; private set; }
        public static int Duals { get; private set; }
        public static int Beams { get; private set; }
        public static int Held { get; private set; }
        public static int Cleared { get; private set; }
        public static int Converted { get; private set; }
        public static int Fired { get; private set; }
        public static int LastColour { get; private set; } = -1;
        public static int LastTargets { get; private set; }
        public static float LastFinishAt { get; private set; }

        private static int _running;
        private static StoryOriginalColorBallHost _host;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _enabled = null;
            _running = 0;
            _host = null;
            Plays = Combos = Duals = Beams = Held = Cleared = Converted = Fired = LastTargets = 0;
            LastColour = -1;
            LastFinishAt = 0f;
        }

        /// <summary>RoyalPowerupDirector.EligibleColorBallTarget: a plain gem in a real cell.</summary>
        public static bool Eligible(Item item) =>
            item != null && item.isActiveAndEnabled && !item.destroying && item.currentType == ItemsTypes.NONE &&
            item.color >= 0 && item.color < BoardColours && item.square != null && item.square.item == item &&
            !item.square.IsNone() && item.square.type != SquareTypes.UNDESTROYABLE;

        /// <summary>RoyalPowerupDirector.MostCommonColorBallColour: the colour most plain gems share, first on a tie.</summary>
        public static int MostCommonColour()
        {
            var counts = new int[BoardColours];
            foreach (var item in Object.FindObjectsByType<Item>(FindObjectsSortMode.None))
                if (Eligible(item)) counts[item.color]++;
            int best = 0;
            for (int c = 1; c < counts.Length; c++) if (counts[c] > counts[best]) best = c;
            return best;
        }

        /// <summary>
        /// RoyalPowerupDirector.Orb from <paramref name="ball"/>'s cell. <paramref name="colour"/> below 0
        /// takes the most common; <paramref name="payload"/> NONE clears, a Rocket or TNT type converts;
        /// <paramref name="all"/> is the Color Ball + Color Ball board clear. The <paramref name="partner"/>
        /// of a combo is spent. False when off, so the caller plays the kit's own.
        /// </summary>
        public static bool Fire(Item ball, int colour, ItemsTypes payload, bool all, Item partner = null)
        {
            var lm = LevelManager.THIS;
            if (!Ready || ball == null || lm == null) return false;
            Vector3 at = ball.square != null ? ball.square.transform.position : ball.transform.position;
            if (colour < 0 || colour >= BoardColours) colour = MostCommonColour();
            var targets = new List<Square>();
            for (int r = 0; r < lm.maxRows; r++)
                for (int c = 0; c < lm.maxCols; c++)
                {
                    var sq = lm.GetSquare(c, r);
                    if (sq != null && (all || Eligible(sq.item) && sq.item.color == colour)) targets.Add(sq);
                }
            if (partner != null && partner != ball) Spend(partner);
            LastColour = all ? -1 : colour;
            LastTargets = targets.Count;
            _running++;
            var host = Host();
            if (all) { Duals++; host.StartCoroutine(host.Dual(at, targets)); }
            else
            {
                if (payload == ItemsTypes.NONE) Plays++; else Combos++;
                host.StartCoroutine(host.Sequence(at, colour, targets, payload));
            }
            return true;
        }

        /// <summary>A Color Ball is tappable under royal-throne's rules (RoyalPowerupDirector.Tap).</summary>
        public static bool Tappable(Item item) => Ready && item != null && item.currentType == ItemsTypes.BOMB;

        // RoyalPowerupDirector.Consume: the partner of a combo leaves the board with no effect of its own.
        static void Spend(Item partner)
        {
            if (partner.destroying) return;
            StoryOriginalMotion.Silence(partner);
            partner.NextType = ItemsTypes.NONE;
            partner.currentType = ItemsTypes.NONE;
            partner.DestroyItem(true);
        }

        internal static void Done() => _running = Mathf.Max(0, _running - 1);
        internal static void CountBeam() => Beams++;
        internal static void CountHeld() => Held++;
        internal static void CountCleared() => Cleared++;
        internal static void CountConverted() => Converted++;
        internal static void CountFired() => Fired++;
        internal static void MarkFinish() => LastFinishAt = Time.time;

        public static void StateChanged(GameState state)
        {
            if (state == GameState.Map || state == GameState.PrepareGame) Clear();
        }

        public static void Clear()
        {
            if (_host != null) _host.Clear();
            StoryOriginalChargedTarget.ClearAll();
            _running = 0;
        }

        internal static void Sound(string clip, float gain)
        {
            if (SoundBase.Instance != null) SoundBase.Instance.PlaySound(ReferenceTnt.Clip(clip), gain);
        }

        private static StoryOriginalColorBallHost Host()
        {
            if (_host != null) return _host;
            var go = new GameObject("~OriginalColorBall");
            Object.DontDestroyOnLoad(go);
            _host = go.AddComponent<StoryOriginalColorBallHost>();
            return _host;
        }
    }

    /// <summary>Runs the Color Ball sequences and owns what they spawn.</summary>
    public sealed class StoryOriginalColorBallHost : MonoBehaviour
    {
        private readonly List<GameObject> _owned = new List<GameObject>();
        private readonly List<GameObject> _auto = new List<GameObject>();

        sealed class Run
        {
            public Transform Source;      // the ball's Animator/Main: where the beams leave from
            public Vector3 Origin;
            public float FinishAge = -1f;  // >= 0 once the ball is finishing
            public readonly List<Beam> Beams = new List<Beam>();
        }

        sealed class Beam
        {
            public Square Square; public Item Item; public Vector3 To; public int Colour;
            public float Age, Duration, ContactAge; public bool Hit;
            public StoryOriginalChargedTarget Charge; public int ChargeLease; public Vector3 Rest;
        }

        GameObject Own(GameObject go) { if (go != null) _owned.Add(go); return go; }

        void Drop(GameObject go)
        {
            if (go == null) return;
            _owned.Remove(go);
            Destroy(go);
        }

        GameObject Play(string key, Vector3 at, float pitch, int colour = -1, int order = ReferenceTnt.VfxOrder)
        {
            var go = ReferenceTnt.PlayVfx(key, transform, at, pitch, 0, order, colour);
            if (go != null) _auto.Add(go);
            return go;
        }

        public void Clear()
        {
            StopAllCoroutines();
            foreach (var go in _owned) if (go != null) Destroy(go);
            foreach (var go in _auto) if (go != null) Destroy(go);
            _owned.Clear();
            _auto.Clear();
        }

        void Update()
        {
            for (int i = _auto.Count - 1; i >= 0; i--)
            {
                if (_auto[i] != null && ReferenceTnt.Alive(_auto[i])) continue;
                if (_auto[i] != null) Destroy(_auto[i]);
                _auto.RemoveAt(i);
            }
        }

        static bool Paused => LevelManager.THIS != null && LevelManager.THIS.gameStatus == GameState.Pause;

        // ── One Color Ball ─────────────────────────────────────────────────

        internal IEnumerator Sequence(Vector3 at, int colour, List<Square> targets, ItemsTypes payload)
        {
            float pitch = StoryBoardScale.Pitch;
            var run = new Run { Origin = StoryRoyalVfx.EffectPosition(at, pitch) };
            bool convert = payload != ItemsTypes.NONE;
            var charged = new List<Beam>();
            var converted = new List<Square>();
            GameObject ball = null, use = null;
            AudioSource start = null;
            try
            {
                // Activate: the use clip, its particles at the ball's centre, the start sound; 0.32s of charge.
                ball = Own(ReferenceTnt.SpawnVisual(ColorBallKey, transform, run.Origin, pitch, 0, BoosterOrder));
                var animator = ball != null ? ball.GetComponentInChildren<Animator>(true) : null;
                ReferenceTnt.Play(animator, ColorBallUseState);
                run.Source = ball != null ? ReferenceTnt.Find(ball.transform, "Main") : null;
                use = Own(ReferenceTnt.PlayVfx(ColorBallUseVfx, transform, SourceAt(run), pitch, 0, ReferenceTnt.VfxOrder));
                start = Voice(ball, ColorBallStartAudio, StartGain);
                yield return Hold(ChargeSeconds, run, use);

                // Beams, one per target in board order; contacts hold or convert their gems.
                float clock = 0f, nextBeam = 0f;
                int next = 0, rays = 0;
                bool aimed = false;
                var flying = new List<Beam>();
                while (next < targets.Count || flying.Count > 0)
                {
                    if (Paused) { yield return null; continue; }
                    if (next < targets.Count && clock >= nextBeam)
                    {
                        var sq = targets[next];
                        if (sq == null || sq.item == null || sq.item.destroying || sq.item.currentType != ItemsTypes.NONE)
                        { next++; aimed = false; continue; }
                        if (!aimed) { aimed = true; nextBeam += AimSeconds; }
                        else
                        {
                            var beam = new Beam
                            {
                                Square = sq, Item = sq.item, Colour = sq.item.color, Rest = sq.item.transform.localScale,
                                To = StoryRoyalVfx.EffectPosition(sq.transform.position, pitch)
                            };
                            beam.Duration = RayDuration(Vector3.Distance(SourceAt(run), beam.To) / pitch);
                            run.Beams.Add(beam);
                            flying.Add(beam);
                            StartCoroutine(Ray(run, beam, pitch));
                            StoryOriginalColorBall.Sound(ColorBallRayAudio[rays++ % ColorBallRayAudio.Length], RayGain);
                            StoryOriginalColorBall.CountBeam();
                            next++; aimed = false;
                            float fraction = next / (float)Mathf.Max(1, targets.Count);
                            nextBeam += (targets.Count > 28 ? Mathf.Lerp(.095f, .085f, Mathf.Sin(fraction * Mathf.PI)) : BeamGap) - AimSeconds;
                        }
                    }
                    for (int i = 0; i < flying.Count;)
                    {
                        var f = flying[i];
                        if (f.Square == null || f.Item == null || f.Item.destroying || f.Square.item != f.Item)
                        {
                            if (f.Item != null) f.Item.transform.localScale = f.Rest;
                            if (f.Charge != null) f.Charge.Release(f.ChargeLease);
                            flying.RemoveAt(i); continue;
                        }
                        if (!f.Hit && f.Age >= f.Duration)
                        {
                            f.Hit = true; f.ContactAge = 0f;
                            Contact(f.To, f.Colour, !convert, pitch);
                            if (!convert)
                            {
                                f.Charge = StoryOriginalChargedTarget.Hold(f.Item);
                                if (f.Charge != null) { f.ChargeLease = f.Charge.Lease; StoryOriginalColorBall.CountHeld(); }
                            }
                            i++; continue;
                        }
                        if (f.Hit)
                        {
                            f.ContactAge += Time.deltaTime;
                            float t = Mathf.Clamp01(f.ContactAge / ConvertAfter);
                            if (convert)
                                f.Item.transform.localScale = Vector3.Scale(f.Rest,
                                    new Vector3(1 + .055f * Mathf.Sin(t * Mathf.PI), 1 - .11f * Mathf.Sin(t * Mathf.PI), 1));
                            if (f.ContactAge + 1e-5f >= ConvertAfter)
                            {
                                f.Item.transform.localScale = f.Rest;
                                if (convert && f.Item.currentType == ItemsTypes.NONE)
                                {
                                    StoryOriginalBoosters.MarkColorBallMade(f.Item);
                                    f.Item.NextType = payload;
                                    f.Item.ChangeType();
                                    converted.Add(f.Square);
                                    StoryOriginalColorBall.CountConverted();
                                }
                                else if (!convert) charged.Add(f);
                                flying.RemoveAt(i); continue;
                            }
                        }
                        i++;
                    }
                    yield return null;
                    clock += Time.deltaTime;
                }
                // The last link must actually finish its logical life first; no other pause.
                while (run.Beams.Exists(b => b.Age < b.Duration + LogicalTail)) yield return null;

                // Finish: every charged gem clears at once; the links go out; the final pulse; the ball goes.
                run.FinishAge = 0f;
                StoryOriginalColorBall.MarkFinish();
                foreach (var f in charged)
                {
                    if (f.Charge != null) f.Charge.Release(f.ChargeLease);
                    if (f.Square != null && f.Item != null && !f.Item.destroying && f.Square.item == f.Item)
                    { Hit(f.Square); StoryOriginalColorBall.CountCleared(); }
                }
                bool pulsed = false;
                while (run.FinishAge < FinishSeconds)
                {
                    if (!pulsed && run.FinishAge >= FinalPulseAt)
                    {
                        pulsed = true;
                        if (start != null) start.Stop();
                        ReferenceTnt.StopEmission(use);
                        Play(ColorBallExplodeVfx, SourceAt(run), pitch);
                        StoryOriginalColorBall.Sound(ColorBallExplodeAudio, ExplodeGain);
                    }
                    if (use != null) use.transform.position = SourceAt(run);
                    yield return null;
                    if (!Paused) run.FinishAge += Time.deltaTime;
                }
                Drop(ball); ball = null;

                // A Color Ball's TNTs settle their dedicated reveal first; then every converted piece fires.
                float wait = 3f;
                while (wait > 0f && converted.Exists(sq => sq != null && sq.item != null && !sq.item.destroying &&
                    (sq.item.currentType != payload || TntRevealing(sq.item))))
                { yield return null; wait -= Time.deltaTime; }
                foreach (var sq in converted)
                    if (sq != null && sq.item != null && !sq.item.destroying && sq.item.currentType == payload)
                    { Hit(sq); StoryOriginalColorBall.CountFired(); }
            }
            finally
            {
                foreach (var f in charged) if (f.Charge != null) f.Charge.Release(f.ChargeLease);
                foreach (var b in run.Beams) if (b.Item != null && !b.Item.destroying) b.Item.transform.localScale = b.Rest;
                if (start != null) start.Stop();
                Drop(ball);
                if (use != null) { _owned.Remove(use); _auto.Add(use); ReferenceTnt.StopEmission(use); }
                StoryOriginalColorBall.Done();
            }
        }

        // ReferenceGameplayAudio gains: the start cue is owned at 0.45, the rays and the pulse play at 0.72.
        const float StartGain = .45f, RayGain = .72f, ExplodeGain = .72f;
        const float ChargeSeconds = StoryOriginalColorBall.ChargeSeconds, AimSeconds = StoryOriginalColorBall.AimSeconds;
        const float ConvertAfter = StoryOriginalColorBall.ConvertAfter, BeamGap = StoryOriginalColorBall.BeamGap;
        const float LogicalTail = StoryOriginalColorBall.LogicalTail, LinkResidue = StoryOriginalColorBall.LinkResidue;
        const float LinkOpacity = StoryOriginalColorBall.LinkOpacity, FinishSeconds = StoryOriginalColorBall.FinishSeconds;
        const float FinalPulseAt = StoryOriginalColorBall.FinalPulseAt, LinksOut = StoryOriginalColorBall.LinksOutAfterFinish;

        static Vector3 SourceAt(Run run) => run.Source != null ? run.Source.position : run.Origin;

        // Waits out a phase while the use particles ride the ball's centre.
        IEnumerator Hold(float seconds, Run run, GameObject use)
        {
            float t = 0f;
            while (t < seconds)
            {
                if (use != null) use.transform.position = SourceAt(run);
                yield return null;
                if (!Paused) t += Time.deltaTime;
            }
        }

        static bool TntRevealing(Item item)
        {
            var piece = item.GetComponent<StoryOriginalTntPiece>();
            return piece != null && piece.Revealing;
        }

        // RoyalPowerupDirector.Hit: the cell's item clears (a special fires), or its block breaks.
        static void Hit(Square sq)
        {
            if (sq == null || sq.IsNone() || sq.type == SquareTypes.UNDESTROYABLE) return;
            if (sq.item != null && sq.item.currentType != ItemsTypes.INGREDIENT) sq.item.DestroyItem(true);
            else if (sq.block.Count > 0) sq.DestroyBlock();
        }

        // ColorBallVfxV4.Contact: the imported burst in the gem's colour; a gem that is held keeps its
        // own shaped flash, so the burst's round blast and ring are left out.
        void Contact(Vector3 at, int colour, bool shaped, float pitch)
        {
            var burst = Play(ColorBallContactVfx, at, pitch, StoryOriginalBoosters.PartColour(colour));
            if (burst == null || !shaped) return;
            foreach (var ps in burst.GetComponentsInChildren<ParticleSystem>())
                if (ps.name == "bang_blast" || ps.name == "ring") ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // ColorBallVfxV4 native link: the head runs out from the ball's centre; once it lands the line
        // stays lit at 76%, fading over 0.46s, and goes out within 0.1s of the ball's finish.
        IEnumerator Ray(Run run, Beam beam, float pitch)
        {
            var ray = Own(ReferenceTnt.PlayVfx(ColorBallRayVfx, transform, SourceAt(run), pitch, 0, RayOrder));
            try
            {
                var lines = ray != null ? ray.GetComponentsInChildren<LineRenderer>(true) : new LineRenderer[0];
                var alphas = new GradientAlphaKey[lines.Length][];
                for (int i = 0; i < lines.Length; i++)
                {
                    alphas[i] = lines[i].colorGradient.alphaKeys;
                    lines[i].widthMultiplier *= pitch;
                    lines[i].positionCount = 2;
                }
                while (true)
                {
                    Vector3 from = SourceAt(run);
                    float progress = Mathf.Clamp01(beam.Age / beam.Duration), after = Mathf.Max(0f, beam.Age - beam.Duration);
                    float fade = after <= 0f ? 1f : Mathf.Lerp(LinkOpacity, 0f, Mathf.SmoothStep(0, 1, after / LinkResidue));
                    if (run.FinishAge >= 0f) fade *= 1 - Mathf.SmoothStep(0, 1, run.FinishAge / LinksOut);
                    Vector3 end = Vector3.Lerp(from, beam.To, progress);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        line.SetPosition(0, line.useWorldSpace ? from : line.transform.InverseTransformPoint(from));
                        line.SetPosition(1, line.useWorldSpace ? end : line.transform.InverseTransformPoint(end));
                        var gradient = line.colorGradient;
                        var faded = new GradientAlphaKey[alphas[i].Length];
                        for (int k = 0; k < faded.Length; k++) faded[k] = new GradientAlphaKey(alphas[i][k].alpha * fade, alphas[i][k].time);
                        gradient.SetKeys(gradient.colorKeys, faded);
                        line.colorGradient = gradient;
                    }
                    if (beam.Age >= beam.Duration + LinkResidue || run.FinishAge >= LinksOut) break;
                    yield return null;
                    if (!Paused) beam.Age += Time.deltaTime;
                }
            }
            finally
            {
                // A link that outlived its ray still counts its logical life out for the finish.
                beam.Age = Mathf.Max(beam.Age, beam.Duration + LogicalTail);
                Drop(ray);
            }
        }

        // A sustained cue that stops with its owner (ReferenceActivationVisual.PlayOwned).
        static AudioSource Voice(GameObject owner, string clipName, float gain)
        {
            var clip = ReferenceTnt.Clip(clipName);
            if (owner == null || clip == null || SoundBase.Instance == null) return null;
            var source = owner.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0; source.clip = clip;
            source.volume = gain * SoundBase.Instance.GetVolume();
            source.Play();
            return source;
        }

        // ── Color Ball + Color Ball ─────────────────────────────────────────

        // ReferenceDualColorBallVisual + RoyalPowerupDirector.DualColorBall: the two-ball clip with its
        // charge, then the blast, and every cell on the board is hit on that frame.
        internal IEnumerator Dual(Vector3 at, List<Square> targets)
        {
            float pitch = StoryBoardScale.Pitch;
            at = StoryRoyalVfx.EffectPosition(at, pitch);
            GameObject body = null, charge = null;
            try
            {
                body = Own(ReferenceTnt.SpawnVisual(ColorBallComboKey, transform, at, pitch, 0, ReferenceTnt.ComboBodyOrder));
                float length = ReferenceTnt.Play(body != null ? body.GetComponentInChildren<Animator>(true) : null, ColorBallComboState);
                StoryOriginalColorBall.Sound(ColorBallComboAudio, .45f);
                charge = Own(ReferenceTnt.PlayVfx(ColorBallComboChargeVfx, transform, at, pitch, 0, ReferenceTnt.VfxOrder));
                float t = 0f;
                while (t < length) { yield return null; if (!Paused) t += Time.deltaTime; }
                Drop(body); body = null;
                ReferenceTnt.StopEmission(charge);
                Play(ColorBallComboExplodeVfx, at, pitch);
                StoryOriginalColorBall.MarkFinish();
                foreach (var sq in targets) { Hit(sq); StoryOriginalColorBall.CountCleared(); }
            }
            finally
            {
                Drop(body);
                if (charge != null) { _owned.Remove(charge); _auto.Add(charge); ReferenceTnt.StopEmission(charge); }
                StoryOriginalColorBall.Done();
            }
        }
    }
}
