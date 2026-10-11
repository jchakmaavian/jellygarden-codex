// The original Royal Throne Rocket, Color Ball, combos, match break and win
// finish on the King Leo board, beside the original TNT (StoryOriginalTnt).
//
// The art, clips, particles and sounds are the ones imported from royal-throne
// under Assets/ReferenceGameplay; ReferenceTnt and ReferenceBoosters draw them by
// the rules and timings the game used there (the same code the booster preview
// plays). This layer only decides WHEN, from hooks in Item and LevelManager:
//
//   Rocket / Color Ball piece   StoryOriginalPiece: creation clip, then idle
//   Rocket fired                both halves fly out on the imported trail
//   Color Ball fired            charge, a beam to each target, final pulse
//   Color Ball + Rocket / TNT   the same beams to the gems it converts
//   Color Ball + Color Ball     the two-ball clip and board-wide blast
//   Rocket + TNT, TNT + TNT     the combo burst; the TNT+TNT burst replaces the
//                               single blasts of that frame
//   plain gem cleared           the gem explosion in the gem's colour
//   moves left at a win         each flies in from the Moves counter, and the
//                               gem turns into its Rocket when it lands
//   win card                    the star burst and idle sparkle (StoryOriginalWinStars)
//
// WHAT IT DOES NOT CHANGE
//
// Item and LevelManager still own every clear, its area, scoring and timing.
// The one timing this layer supplies is the Rocket's: cells clear when the
// imported halves' noses reach them, at the original 14.25 cells a second.
// King Leo has no Propeller, so the imported Propeller stays in the preview.
//
// THE SWITCH
//
// -originalboosters / -nooriginalboosters, or the StoryVfx.OriginalBoosters
// pref; on by default. Off, or with any imported asset missing, every call
// returns false and the board keeps its own effects exactly as before.

using System.Collections;
using System.Collections.Generic;
using RoyalThrone.ReferenceGameplay.Preview;
using UnityEngine;
using static RoyalThrone.ReferenceGameplay.Preview.ReferenceBoosters;

namespace JellyGarden.Scripts.Story
{
    public static class StoryOriginalBoosters
    {
        /// <summary>How far ahead of its pivot a flying half's nose is, in cells (126px art at 140 PPU).</summary>
        public const float RocketNoseCells = .45f;

        public static bool Enabled
        {
            get
            {
                if (_enabled.HasValue) return _enabled.Value;
                _enabled = StoryVfxFlags.On("-originalboosters", "StoryVfx.OriginalBoosters");
                return _enabled.Value;
            }
            set { _enabled = value; }
        }
        private static bool? _enabled;

        /// <summary>Enabled, and the imported pieces, effects, sprites and sounds load.</summary>
        public static bool Ready
        {
            get
            {
                if (!Enabled) return false;
                if (!_assets.HasValue)
                {
                    _assets = ReferenceTnt.Visual(RocketKey) != null && ReferenceTnt.Visual(ColorBallKey) != null &&
                        ReferenceTnt.Visual(ColorBallComboKey) != null &&
                        ReferenceTnt.VfxPrefab(RocketTrailVfx) != null && ReferenceTnt.VfxPrefab(ColorBallRayVfx) != null &&
                        ReferenceTnt.VfxPrefab(MatchVfx) != null && ReferenceTnt.VfxPrefab(MovesFlightVfx) != null &&
                        ReferenceBoosters.Sprite(RocketLeftSprite) != null && ReferenceTnt.Clip(RocketAudio) != null;
                    if (!_assets.Value)
                        Debug.LogWarning("[OriginalBoosters] Imported booster assets are missing; using the board's own effects.");
                }
                return _assets.Value;
            }
        }
        private static bool? _assets;

        /// <summary>A Rocket fired now flies as the original (StoryRocketSweep only reaches StoryRoyalVfx on RoyalStreak).</summary>
        public static bool OwnsRocket => Application.isPlaying && Ready && StoryRocketVfx.Enabled &&
            StoryRocketVariant.Current == RocketVariant.RoyalStreak;

        /// <summary>A Color Ball fired now is drawn (and heard) by the original sequence.</summary>
        public static bool OwnsColorBall => Application.isPlaying && Ready;

        /// <summary>A plain gem cleared now gets the original explosion and sound.</summary>
        public static bool OwnsMatch => Application.isPlaying && Ready;

        // Counters for the validation harness.
        public static int RocketFlights { get; private set; }
        public static int ColorBallPlays { get; private set; }
        public static int ColorBallConverts { get; private set; }
        public static int DualColorBallPlays { get; private set; }
        public static int RocketTntPlays { get; private set; }
        public static int TntTntPlays { get; private set; }
        public static int Rays { get; private set; }
        public static int MatchBreaks { get; private set; }
        public static int MovesSent { get; private set; }
        public static int MovesInFlight { get; private set; }
        public static int ActiveEffects => _host == null ? 0 : _host.Active;

        private static StoryOriginalBoostersHost _host;
        private static int _matchSound, _matchSoundFrame = -1, _colorBallFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _enabled = null;
            _assets = null;
            _host = null;
            _matchSound = 0; _matchSoundFrame = -1; _colorBallFrame = -1;
            _colorBallMade.Clear();
            RocketFlights = ColorBallPlays = ColorBallConverts = DualColorBallPlays = 0;
            RocketTntPlays = TntTntPlays = Rays = MatchBreaks = MovesSent = MovesInFlight = 0;
        }

        static bool IsPiece(ItemsTypes type) => type == ItemsTypes.HORIZONTAL_STRIPPED ||
            type == ItemsTypes.VERTICAL_STRIPPED || type == ItemsTypes.BOMB;

        /// <summary>
        /// Puts the original piece on a Rocket or Color Ball. <paramref name="reveal"/>
        /// plays the creation clip and sound. False for any other type, or when off.
        /// </summary>
        public static bool ShowPiece(Item item, bool reveal)
        {
            if (!Application.isPlaying || item == null || !IsPiece(item.currentType) || !Ready) return false;
            // Only a TNT has a separate Color Ball reveal; a Rocket or Color Ball drops the mark.
            TakeColorBallReveal(item);
            var piece = item.GetComponent<StoryOriginalPiece>();
            if (piece == null) piece = item.gameObject.AddComponent<StoryOriginalPiece>();
            if (!piece.Show(item, reveal)) return false;
            if (reveal) Sound(item.currentType == ItemsTypes.BOMB ? ColorBallCreationAudio : RocketCreationAudio);
            return true;
        }

        /// <summary>When a Rocket's nose reaches a cell <paramref name="distance"/> cells away.</summary>
        public static float RocketReachesAt(int distance) =>
            Mathf.Max(0f, Mathf.Abs(distance) - RocketNoseCells) / RocketCellsPerSecond;

        /// <summary>
        /// The original Rocket: both halves leave <paramref name="at"/> (the square) and
        /// fly off the screen. Negative and positive are the board reaches each way.
        /// </summary>
        public static bool FireRocket(Vector3 at, bool horizontal, int negative, int positive, float pitch)
        {
            if (!OwnsRocket) return false;
            RocketFlights++;
            Sound(RocketAudio);
            var host = Host();
            host.StartCoroutine(host.Rocket(StoryRoyalVfx.EffectPosition(at, pitch), horizontal, negative, positive, pitch));
            return true;
        }

        /// <summary>A Color Ball swapped with a plain gem: beams to every gem of that colour.</summary>
        public static bool ColorBall(Vector3 at, int colour, List<Vector3> targets)
        {
            if (!OwnsColorBall || targets == null) return false;
            ColorBallPlays++;
            // The gems are cleared this frame; their beams' contacts are their explosions.
            _colorBallFrame = Time.frameCount;
            var host = Host();
            host.StartCoroutine(host.ColorBall(at, colour, targets, StoryBoardScale.Pitch));
            return true;
        }

        /// <summary>A Color Ball swapped with a Rocket or TNT: the same beams to the gems it converts.</summary>
        public static bool ColorBallConvert(Vector3 at, int colour)
        {
            if (!OwnsColorBall) return false;
            var targets = new List<Vector3>();
            foreach (var go in GameObject.FindGameObjectsWithTag("Item"))
            {
                var item = go.GetComponent<Item>();
                if (item == null || item.color != colour || item.destroying) continue;
                targets.Add(item.square != null ? item.square.transform.position : item.transform.position);
                _colorBallMade.Add(item);
            }
            ColorBallConverts++;
            var host = Host();
            host.StartCoroutine(host.ColorBall(at, colour, targets, StoryBoardScale.Pitch));
            return true;
        }

        /// <summary>Two Color Balls swapped together.</summary>
        public static bool DualColorBall(Vector3 at)
        {
            if (!OwnsColorBall) return false;
            DualColorBallPlays++;
            var host = Host();
            host.StartCoroutine(host.DualColorBall(StoryRoyalVfx.EffectPosition(at, StoryBoardScale.Pitch), StoryBoardScale.Pitch));
            return true;
        }

        /// <summary>A Rocket swapped onto a TNT: the combo burst over the TNT.</summary>
        public static bool RocketTnt(Vector3 at)
        {
            if (!OwnsColorBall) return false;
            RocketTntPlays++;
            float pitch = StoryBoardScale.Pitch;
            Host().Play(TntRocketVfx, StoryRoyalVfx.EffectPosition(at, pitch), pitch);
            Sound(RocketTntAudio);
            return true;
        }

        /// <summary>Two TNTs swapped together: the big combo blast, in place of that frame's single blasts.</summary>
        public static bool TntTnt(Vector3 at)
        {
            if (!OwnsColorBall || !StoryOriginalTnt.OwnsBlast) return false;
            TntTntPlays++;
            float pitch = StoryBoardScale.Pitch;
            Host().Play(ReferenceTnt.ComboExplodeVfx, StoryRoyalVfx.EffectPosition(at, pitch), pitch);
            Sound(ReferenceTnt.ComboAudio);
            StoryOriginalTnt.HoldBlasts();
            return true;
        }

        /// <summary>A plain gem cleared: the gem explosion in its colour, and one match sound a frame.</summary>
        public static bool MatchBreak(Vector3 at, int colour)
        {
            if (!OwnsMatch) return false;
            if (Time.frameCount == _colorBallFrame) return true;
            MatchBreaks++;
            float pitch = StoryBoardScale.Pitch;
            Host().Play(MatchVfx, StoryRoyalVfx.EffectPosition(at, pitch), pitch, PartColour(colour));
            if (_matchSoundFrame != Time.frameCount)
            {
                _matchSoundFrame = Time.frameCount;
                Sound(MatchAudio[_matchSound++ % MatchAudio.Length]);
            }
            return true;
        }

        /// <summary>
        /// A move left at a win flies in from the Moves counter; the gem changes into
        /// its Rocket (NextType already set) when it lands. False when off: the caller
        /// changes it at once, as before.
        /// </summary>
        public static bool SendMove(Item item)
        {
            if (!Application.isPlaying || item == null || !Ready) return false;
            MovesSent++;
            MovesInFlight++;
            var host = Host();
            host.StartCoroutine(host.Move(item, StoryBoardScale.Pitch));
            return true;
        }

        /// <summary>
        /// Whether a Color Ball made this item (ReferencePieceVisual.RevealFromColorBall):
        /// its TNT then reveals with the dedicated clip. Asking clears the mark.
        /// </summary>
        public static bool TakeColorBallReveal(Item item) => item != null && _colorBallMade.Remove(item);
        private static readonly HashSet<Item> _colorBallMade = new HashSet<Item>();

        internal static void Landed() => MovesInFlight = Mathf.Max(0, MovesInFlight - 1);

        internal static void CountRay() => Rays++;

        public static void StateChanged(GameState state)
        {
            if (state == GameState.Map || state == GameState.PrepareGame) Clear();
        }

        public static void Clear()
        {
            if (_host != null) _host.Clear();
            MovesInFlight = 0;
            _colorBallMade.Clear();
        }

        /// <summary>
        /// King Leo's colour index (red book, orange coin, purple gem, blue shield,
        /// green leaf, yellow crown) to the blast_part_* the explosion carries
        /// (ReferenceTnt: red, blue, yellow, green, pink).
        /// </summary>
        public static int PartColour(int colour)
        {
            switch (colour)
            {
                case 0: return 0;
                case 1: return 2;
                case 2: return 4;
                case 3: return 1;
                case 4: return 3;
                case 5: return 2;
                default: return -1;
            }
        }

        // Through SoundBase, so the player's sound setting and volume apply.
        internal static void Sound(string clip)
        {
            if (SoundBase.Instance != null) SoundBase.Instance.PlaySound(ReferenceTnt.Clip(clip));
        }

        private static StoryOriginalBoostersHost Host()
        {
            if (_host != null) return _host;
            var go = new GameObject("~OriginalBoosters");
            Object.DontDestroyOnLoad(go);
            _host = go.AddComponent<StoryOriginalBoostersHost>();
            return _host;
        }
    }

    /// <summary>
    /// Runs the original sequences and owns what they spawn. The kit pauses with
    /// Time.timeScale 0, which holds these coroutines and particles with it.
    /// </summary>
    public sealed class StoryOriginalBoostersHost : MonoBehaviour
    {
        // Effects that retire themselves when their particles die; the rest belong to a sequence.
        private readonly List<GameObject> _owned = new List<GameObject>();
        private readonly List<GameObject> _auto = new List<GameObject>();

        public int Active => _owned.Count + _auto.Count;

        GameObject Own(GameObject go)
        {
            if (go != null) _owned.Add(go);
            return go;
        }

        void Drop(GameObject go)
        {
            if (go == null) return;
            _owned.Remove(go);
            Destroy(go);
        }

        /// <summary>A one-shot effect that removes itself when done.</summary>
        public GameObject Play(string key, Vector3 at, float pitch, int colour = -1, int order = ReferenceTnt.VfxOrder)
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

        private void Update()
        {
            for (int i = _auto.Count - 1; i >= 0; i--)
            {
                if (_auto[i] != null && ReferenceTnt.Alive(_auto[i])) continue;
                if (_auto[i] != null) Destroy(_auto[i]);
                _auto.RemoveAt(i);
            }
        }

        // ── Rocket ─────────────────────────────────────────────────────────

        // RoyalRocketSequence flight: constant speed off the screen, the trail at each half's fins.
        internal IEnumerator Rocket(Vector3 at, bool horizontal, int negative, int positive, float pitch)
        {
            Vector3 axis = horizontal ? Vector3.right : Vector3.up;
            var halves = new SpriteRenderer[2];
            var trails = new GameObject[2];
            var exits = new float[2];
            var nozzles = new float[2];
            for (int i = 0; i < 2; i++)
            {
                int d = i == 0 ? -1 : 1;
                string name = horizontal ? (d < 0 ? RocketLeftSprite : RocketRightSprite) : (d < 0 ? RocketBottomSprite : RocketTopSprite);
                var go = Own(new GameObject("Original rocket half"));
                go.transform.SetParent(transform, false);
                go.transform.position = at;
                go.transform.localScale = Vector3.one * pitch;
                var r = go.AddComponent<SpriteRenderer>();
                r.sprite = ReferenceBoosters.Sprite(name);
                r.sortingOrder = FlyerOrder;
                halves[i] = r;
                nozzles[i] = horizontal ? r.bounds.extents.x : r.bounds.extents.y;

                var trail = Own(ReferenceTnt.PlayVfx(RocketTrailVfx, transform, at, pitch, 0, ReferenceTnt.VfxOrder));
                if (trail != null)
                {
                    var tail = -axis * d;
                    trail.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(tail.y, tail.x) * Mathf.Rad2Deg - 90);
                    foreach (var ps in trail.GetComponentsInChildren<ParticleSystem>(true)) { var main = ps.main; main.simulationSpeed = .95f; }
                }
                trails[i] = trail;
                exits[i] = Mathf.Max((d < 0 ? negative : positive) + 1, ScreenExit(at, axis * d) / pitch + 1);
            }

            float distance = 0, longest = Mathf.Max(exits[0], exits[1]);
            while (distance < longest)
            {
                distance += RocketCellsPerSecond * Time.deltaTime;
                for (int i = 0; i < 2; i++)
                {
                    if (halves[i] == null) continue;
                    Vector3 dir = axis * (i == 0 ? -1 : 1);
                    Vector3 head = at + dir * Mathf.Min(distance, exits[i]) * pitch;
                    halves[i].transform.position = head;
                    if (trails[i] != null) trails[i].transform.position = head - dir * nozzles[i];
                    if (distance >= exits[i])
                    {
                        ReferenceTnt.StopEmission(trails[i]);
                        Drop(halves[i].gameObject);
                        halves[i] = null;
                    }
                }
                yield return null;
            }
            yield return null;
            while (ReferenceTnt.Alive(trails[0]) || ReferenceTnt.Alive(trails[1])) yield return null;
            Drop(trails[0]);
            Drop(trails[1]);
        }

        static float ScreenExit(Vector3 from, Vector3 dir)
        {
            var camera = Camera.main;
            if (camera == null || !camera.orthographic) return 12f;
            Vector3 c = camera.transform.position;
            float halfH = camera.orthographicSize, halfW = halfH * camera.aspect;
            if (dir.x > 0) return c.x + halfW - from.x;
            if (dir.x < 0) return from.x - (c.x - halfW);
            if (dir.y > 0) return c.y + halfH - from.y;
            return from.y - (c.y - halfH);
        }

        // ── Color Ball ─────────────────────────────────────────────────────

        sealed class Count { public int Value; }

        // ColorBallVisualV4 activation: charge → a beam to each target, nearest first → final pulse.
        internal IEnumerator ColorBall(Vector3 at, int colour, List<Vector3> targets, float pitch)
        {
            at = StoryRoyalVfx.EffectPosition(at, pitch);
            var ball = Own(ReferenceTnt.SpawnVisual(ColorBallKey, transform, at, pitch, 0, BoosterOrder));
            var animator = ball != null ? ball.GetComponentInChildren<Animator>(true) : null;
            ReferenceTnt.Play(animator, ColorBallUseState);
            Transform source = ball != null ? ReferenceTnt.Find(ball.transform, "Main") : null;
            Vector3 origin = source != null ? source.position : at;
            var use = Own(ReferenceTnt.PlayVfx(ColorBallUseVfx, transform, origin, pitch, 0, ReferenceTnt.VfxOrder));
            StoryOriginalBoosters.Sound(ColorBallStartAudio);
            yield return new WaitForSeconds(ColorBallChargeSeconds);

            var sorted = new List<Vector3>(targets.Count);
            foreach (var t in targets) sorted.Add(StoryRoyalVfx.EffectPosition(t, pitch));
            sorted.Sort((a, b) => (a - at).sqrMagnitude.CompareTo((b - at).sqrMagnitude));
            var flying = new Count();
            for (int i = 0; i < sorted.Count; i++)
            {
                StartCoroutine(Ray(source != null ? source.position : origin, sorted[i], colour, pitch, flying));
                StoryOriginalBoosters.Sound(ColorBallRayAudio[i % ColorBallRayAudio.Length]);
                StoryOriginalBoosters.CountRay();
                yield return new WaitForSeconds(ColorBallBeamGap);
            }
            while (flying.Value > 0) yield return null;

            yield return new WaitForSeconds(ColorBallFinalPulseAt);
            ReferenceTnt.StopEmission(use);
            Drop(ball);
            StoryOriginalBoosters.Sound(ColorBallExplodeAudio);
            Play(ColorBallExplodeVfx, origin, pitch);
            yield return null;
            while (ReferenceTnt.Alive(use)) yield return null;
            Drop(use);
        }

        // ColorBallVfxV4 native beam: the head travels out, the target flashes, the line fades.
        IEnumerator Ray(Vector3 from, Vector3 to, int colour, float pitch, Count flying)
        {
            flying.Value++;
            var ray = Own(ReferenceTnt.PlayVfx(ColorBallRayVfx, transform, from, pitch, 0, RayOrder));
            try
            {
                if (ray == null) yield break;
                var lines = ray.GetComponentsInChildren<LineRenderer>(true);
                var alphas = new GradientAlphaKey[lines.Length][];
                for (int i = 0; i < lines.Length; i++)
                {
                    alphas[i] = lines[i].colorGradient.alphaKeys;
                    lines[i].widthMultiplier *= pitch;
                    lines[i].positionCount = 2;
                }
                float duration = RayDuration(Vector3.Distance(from, to) / pitch), age = 0;
                bool hit = false;
                while (age < duration + ColorBallBeamFade)
                {
                    age += Time.deltaTime;
                    Vector3 end = Vector3.Lerp(from, to, Mathf.Clamp01(age / duration));
                    float fade = 1 - Mathf.SmoothStep(0, 1, Mathf.Max(0, age - duration) / ColorBallBeamFade);
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
                    if (!hit && age >= duration) { hit = true; Play(ColorBallContactVfx, to, pitch, StoryOriginalBoosters.PartColour(colour)); }
                    yield return null;
                }
            }
            finally
            {
                Drop(ray);
                flying.Value--;
            }
        }

        // ReferenceDualColorBallVisual: the two-ball clip with its charge, then the blast.
        internal IEnumerator DualColorBall(Vector3 at, float pitch)
        {
            var body = Own(ReferenceTnt.SpawnVisual(ColorBallComboKey, transform, at, pitch, 0, ReferenceTnt.ComboBodyOrder));
            float length = ReferenceTnt.Play(body != null ? body.GetComponentInChildren<Animator>(true) : null, ColorBallComboState);
            StoryOriginalBoosters.Sound(ColorBallComboAudio);
            var charge = Own(ReferenceTnt.PlayVfx(ColorBallComboChargeVfx, transform, at, pitch, 0, ReferenceTnt.VfxOrder));
            yield return new WaitForSeconds(length);
            ReferenceTnt.StopEmission(charge);
            Drop(body);
            Play(ColorBallComboExplodeVfx, at, pitch);
            yield return null;
            while (ReferenceTnt.Alive(charge)) yield return null;
            Drop(charge);
        }

        // ── Win finish ─────────────────────────────────────────────────────

        // ReferenceWinFinish.Send: from the Moves counter to the gem in 0.45s, a hit, then the Rocket.
        internal IEnumerator Move(Item item, float pitch)
        {
            GameObject flight = null;
            try
            {
                Vector3 end = StoryRoyalVfx.EffectPosition(item.square != null ? item.square.transform.position : item.transform.position, pitch);
                Vector3 start = MovesCounter(end);
                flight = Own(ReferenceTnt.PlayVfx(MovesFlightVfx, transform, start, pitch, 0, ReferenceTnt.VfxOrder));
                float progress = 0;
                while (progress < 1)
                {
                    progress = Mathf.Min(1, progress + Time.deltaTime / .45f);
                    if (flight != null)
                        flight.transform.position = Vector3.Lerp(start, end, progress) + Vector3.right * Mathf.Sin(progress * Mathf.PI) * .5f * pitch;
                    yield return null;
                }
                ReferenceTnt.StopEmission(flight);
                Play(MovesHitVfx, end, pitch);
                if (item != null && !item.destroying && item.NextType != ItemsTypes.NONE) item.ChangeType();
            }
            finally
            {
                StoryOriginalBoosters.Landed();
                if (flight != null) _auto.Add(flight);
                _owned.Remove(flight);
            }
        }

        // The Moves number on the HUD, in board space; the top of the screen when it is not there.
        static Vector3 MovesCounter(Vector3 board)
        {
            var camera = Camera.main;
            var label = GameObject.Find("MovesValue");
            if (camera != null && label != null)
            {
                var canvas = label.GetComponentInParent<Canvas>();
                Camera ui = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(ui, label.transform.position);
                Vector3 world = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, camera.WorldToScreenPoint(board).z));
                world.z = board.z;
                return world;
            }
            if (camera != null && camera.orthographic)
                return new Vector3(board.x, camera.transform.position.y + camera.orthographicSize * .8f, board.z);
            return board + Vector3.up * 6f;
        }
    }
}
