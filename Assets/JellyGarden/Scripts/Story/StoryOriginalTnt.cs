// The original Royal Throne TNT on the King Leo board, in place of RoyalTNTVFX.
//
// The art, clips, particles and sounds are the ones imported from royal-throne
// under Assets/ReferenceGameplay; ReferenceTnt draws them by the rules the game
// used there. This layer decides WHEN, with royal-throne's own timings
// (RoyalPowerupDirector.Explosion, NativeTntComboAnticipation, ReferencePieceVisual):
//
//   made         the creation clip (TntLightballCreationAnimation when a Color
//                Ball made it), then its last frame held (StoryOriginalTntPiece)
//   fired        the piece stays up while its fuse burns for 0.12s
//                (TNT3DAnimator.IgnitionDuration), then the blast and its sound
//   the clear    reaches out ring by ring: the first 0.012s after the blast, each
//                further ring 0.025s later (EffectsProfile.blastWavePerCell); a
//                gem shatters 0.018s after its ring is hit, and a TNT caught in
//                the blast lights its own fuse when its ring is hit
//   TNT + TNT    the dedicated body/roof/fuse clip plays first (1.58s) with its
//                fuse riding the socket, the board held; then the big blast,
//                without the single blasts and the explosion sound
//
// WHAT IT DOES NOT CHANGE
//
// Item still owns which cells clear, their area, scoring and the cascade. Only
// the moments are moved to the original's: the clear waits for the ignition,
// and a TNT+TNT clear waits for its anticipation. (royal-throne's blast areas
// were larger, 5x5 and 9x9; King Leo keeps its 3x3 and 5x5.)
//
// THE SWITCH
//
// -originaltnt / -nooriginaltnt, or the StoryVfx.OriginalTnt pref; on by
// default. Off, or with any imported asset missing, every call returns false
// and the board keeps RoyalTNTVFX and the kit's TNT sprite exactly as before.

using System;
using System.Collections;
using System.Collections.Generic;
using RoyalThrone.ReferenceGameplay.Preview;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JellyGarden.Scripts.Story
{
    public static class StoryOriginalTnt
    {
        // RoyalPowerupDirector.Explosion: 0.012s from the blast to the first ring, then
        // EffectsProfile.blastWavePerCell between rings; Item.JuiceDestroyCor (royalTntClear)
        // shows the gem 0.018s more before it shatters.
        public const float WaveStart = .012f, WavePerRing = .025f, ShatterAfterHit = .018f;
        // RoyalAudioDirector / ReferenceGameplayAudio gains.
        const float CreationGain = .72f, ExplodeGain = .95f, ComboGain = .45f;

        public static bool Enabled
        {
            get
            {
                if (_enabled.HasValue) return _enabled.Value;
                _enabled = StoryVfxFlags.On("-originaltnt", "StoryVfx.OriginalTnt");
                return _enabled.Value;
            }
            set { _enabled = value; }
        }
        private static bool? _enabled;

        /// <summary>Enabled, and every asset the piece and the blast need loads.</summary>
        public static bool Ready
        {
            get
            {
                if (!Enabled) return false;
                if (!_assets.HasValue)
                {
                    _assets = ReferenceTnt.Visual(ReferenceTnt.PieceKey) != null &&
                        ReferenceTnt.VfxPrefab(ReferenceTnt.ExplodeVfx) != null &&
                        ReferenceTnt.Clip(ReferenceTnt.CreationAudio) != null &&
                        ReferenceTnt.Clip(ReferenceTnt.ExplodeAudio) != null;
                    if (!_assets.Value)
                        Debug.LogWarning("[OriginalTnt] Imported TNT assets are missing; using RoyalTNTVFX.");
                }
                return _assets.Value;
            }
        }
        private static bool? _assets;

        /// <summary>
        /// Whether a TNT going off now is drawn (and heard) by the original blast:
        /// StoryBombVfx.Blow only reaches StoryRoyalVfx.Blast while it is enabled.
        /// </summary>
        public static bool OwnsBlast => Ready && StoryBombVfx.Enabled && Application.isPlaying;

        /// <summary>Blasts still playing, for tests and the validation harness.</summary>
        public static int ActiveBlasts => _host == null ? 0 : _host.Active;

        /// <summary>A TNT+TNT anticipation is playing; LevelManager holds the cascade for it.</summary>
        public static bool Anticipating => _anticipating > 0;

        /// <summary>This TNT is one of a TNT+TNT pair whose anticipation is playing.</summary>
        public static bool InCombo(Item item) => item != null && _combo.Contains(item);

        /// <summary>The TNT+TNT clear is running from the end of its anticipation.</summary>
        public static bool ResolvingCombo { get; private set; }

        // Counters for the validation harness.
        public static int Ignitions { get; private set; }
        public static int ComboAnticipations { get; private set; }
        public static float LastComboLength { get; private set; }
        /// <summary>Game time the last anticipation began and its clear ran.</summary>
        public static float LastComboStarted { get; private set; }
        public static float LastComboResolved { get; private set; }

        private static StoryOriginalTntHost _host;
        private static Square _waveOrigin;
        private static int _anticipating;
        private static readonly HashSet<Item> _combo = new HashSet<Item>();
        private static Item _pairA, _pairB;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _enabled = null;
            _assets = null;
            _host = null;
            _heldFrame = -1;
            _waveOrigin = null;
            _anticipating = 0;
            _combo.Clear();
            ResolvingCombo = false;
            Ignitions = ComboAnticipations = 0;
            LastComboLength = 0f;
        }

        /// <summary>
        /// Puts the original piece on a TNT. <paramref name="reveal"/> plays the
        /// creation clip and sound; otherwise it shows the finished piece.
        /// Returns false when the original TNT is off, so the caller keeps the kit's.
        /// </summary>
        public static bool Show(Item item, bool reveal)
        {
            if (!Application.isPlaying || item == null || !Ready) return false;
            var piece = item.GetComponent<StoryOriginalTntPiece>();
            if (piece == null) piece = item.gameObject.AddComponent<StoryOriginalTntPiece>();
            if (!piece.Show(item, reveal)) return false;
            if (reveal) Sound(ReferenceTnt.CreationAudio, CreationGain);
            return true;
        }

        /// <summary>
        /// The original single-TNT blast at <paramref name="at"/>, which is already
        /// in front of the camera (StoryRoyalVfx.EffectPosition). Returns false when
        /// it did not play, so the caller falls back to RoyalTNTVFX.
        /// </summary>
        public static bool Blast(Vector3 at, float pitch)
        {
            if (!Application.isPlaying || !Ready) return false;
            // A TNT+TNT combo drew its own big blast this frame (StoryOriginalBoosters.TntTnt).
            if (Time.frameCount == _heldFrame) return true;
            var host = Host();
            var blast = ReferenceTnt.PlayVfx(ReferenceTnt.ExplodeVfx, host.transform, at, pitch, 0, ReferenceTnt.VfxOrder);
            if (blast == null) return false;
            host.Track(blast);
            Sound(ReferenceTnt.ExplodeAudio, ExplodeGain);
            return true;
        }

        /// <summary>The single blasts of this frame belong to a combo blast already playing.</summary>
        public static void HoldBlasts() => _heldFrame = Time.frameCount;
        private static int _heldFrame = -1;

        // ── Ignition ───────────────────────────────────────────────────────

        /// <summary>
        /// RoyalPowerupDirector.Explosion's anticipation, run in the TNT's DestroyCor before
        /// DestroyPackage: the piece stays up while TntItemFuseParticles burn for 0.12s. A TNT
        /// caught in another blast waits for that blast's wave to reach its ring first.
        /// </summary>
        public static IEnumerator Ignite(Item item)
        {
            if (!OwnsBlast || item == null) yield break;
            // The two TNTs of a combo are consumed by its big blast; they light no fuse of their own.
            if (ResolvingCombo && (item == _pairA || item == _pairB)) yield break;
            float wave = WaveDelay(item);
            var piece = item.GetComponent<StoryOriginalTntPiece>();
            if (piece != null) piece.Igniting = true;
            GameObject fuse = null;
            try
            {
                if (wave > 0f) yield return new WaitForSeconds(wave);
                Ignitions++;
                float pitch = StoryBoardScale.Pitch;
                Vector3 at = item.square != null ? item.square.transform.position : item.transform.position;
                var host = Host();
                fuse = ReferenceTnt.PlayVfx(ReferenceTnt.FuseVfx, host.transform, StoryRoyalVfx.EffectPosition(at, pitch), pitch, 0, ReferenceTnt.VfxOrder);
                host.TrackFuse(fuse);
                yield return new WaitForSeconds(ReferenceTnt.IgnitionDuration);
            }
            finally
            {
                ReferenceTnt.StopEmission(fuse);
                if (piece != null) piece.Igniting = false;
            }
        }

        // ── The wave ───────────────────────────────────────────────────────

        /// <summary>DestroyPackage is about to clear around <paramref name="origin"/>.</summary>
        public static void BeginWave(Square origin) { if (OwnsBlast) _waveOrigin = origin; }

        public static void EndWave() => _waveOrigin = null;

        /// <summary>How long after the blast the wave reaches <paramref name="item"/>'s ring; 0 outside a wave.</summary>
        public static float WaveDelay(Item item)
        {
            if (_waveOrigin == null || item == null || item.square == null) return 0f;
            int ring = Mathf.Max(Mathf.Abs(item.square.col - _waveOrigin.col), Mathf.Abs(item.square.row - _waveOrigin.row));
            return WaveStart + ring * WavePerRing;
        }

        // ── TNT + TNT ──────────────────────────────────────────────────────

        /// <summary>
        /// NativeTntComboAnticipation for two swapped TNTs. Returns true when it has taken the
        /// combo: <paramref name="resolve"/> (Item.CheckPackageAndPackage again) runs the clear
        /// when the clip ends. The swap checks both orders; the second is swallowed here.
        /// </summary>
        public static bool ComboAnticipation(Item a, Item b, Vector3 at, Action resolve)
        {
            if (!OwnsBlast || ResolvingCombo || a == null || b == null) return false;
            if (_combo.Contains(a) || _combo.Contains(b)) return true;
            if (ReferenceTnt.Visual(ReferenceTnt.ComboKey) == null) return false;
            _combo.Add(a); _combo.Add(b);
            _anticipating++;
            Host().StartCoroutine(Combo(a, b, at, resolve));
            return true;
        }

        static IEnumerator Combo(Item a, Item b, Vector3 at, Action resolve)
        {
            var host = Host();
            float pitch = StoryBoardScale.Pitch;
            at = StoryRoyalVfx.EffectPosition(at, pitch);
            Conceal(a, true); Conceal(b, true);
            GameObject body = null, fuse = null;
            bool counted = true;
            try
            {
                body = host.Own(ReferenceTnt.SpawnVisual(ReferenceTnt.ComboKey, host.transform, at, pitch, 0, ReferenceTnt.ComboBodyOrder));
                var animator = body != null ? body.GetComponentInChildren<Animator>(true) : null;
                float length = ReferenceTnt.Play(animator, ReferenceTnt.ComboState);
                ComboAnticipations++;
                LastComboLength = length;
                LastComboStarted = Time.time;
                Sound(ReferenceTnt.ComboAudio, ComboGain);
                Transform socket = body != null ? ReferenceTnt.Find(body.transform, ReferenceTnt.ComboFuseSocket) : null;
                fuse = host.Own(ReferenceTnt.PlayVfx(ReferenceTnt.ComboFuseVfx, host.transform, socket != null ? socket.position : at,
                    pitch, 0, ReferenceTnt.ComboFuseOrder));
                float elapsed = 0f;
                while (elapsed < length)
                {
                    // ReferenceVfxPool.Handle.Follow: sampled after the Animator moved the socket.
                    if (fuse != null && socket != null) fuse.transform.SetPositionAndRotation(socket.position, socket.rotation);
                    yield return null;
                    elapsed += Time.deltaTime;
                }
            }
            finally
            {
                ReferenceTnt.StopEmission(fuse);
                host.Retire(fuse);
                host.Drop(body);
                _combo.Remove(a); _combo.Remove(b);
                if (counted) { _anticipating = Mathf.Max(0, _anticipating - 1); counted = false; }
            }
            LastComboResolved = Time.time;
            if (a == null || b == null || a.currentType != ItemsTypes.PACKAGE || b.currentType != ItemsTypes.PACKAGE)
                Debug.LogWarning("[OriginalTnt] TNT+TNT anticipation ended but its pair changed: " + Describe(a) + " / " + Describe(b));
            ResolvingCombo = true;
            _pairA = a; _pairB = b;
            try { resolve?.Invoke(); }
            finally { ResolvingCombo = false; _pairA = _pairB = null; }
        }

        static string Describe(Item item) => item == null ? "gone" :
            item.currentType + (item.destroying ? " destroying" : "") +
            (item.square != null ? " at " + item.square.col + "," + item.square.row : "");

        static void Conceal(Item item, bool hidden)
        {
            var piece = item != null ? item.GetComponent<StoryOriginalTntPiece>() : null;
            if (piece != null) piece.Concealed = hidden;
        }

        public static void StateChanged(GameState state)
        {
            if (state == GameState.Map || state == GameState.PrepareGame) Clear();
            if (_host != null) _host.Paused = state == GameState.Pause;
        }

        public static void Clear()
        {
            if (_host != null) _host.Clear();
            _anticipating = 0;
            _combo.Clear();
            _waveOrigin = null;
            ResolvingCombo = false;
            _pairA = _pairB = null;
        }

        // Through SoundBase, so the player's sound setting and volume apply.
        private static void Sound(string clip, float gain)
        {
            if (SoundBase.Instance != null) SoundBase.Instance.PlaySound(ReferenceTnt.Clip(clip), gain);
        }

        private static StoryOriginalTntHost Host()
        {
            if (_host != null) return _host;
            var go = new GameObject("~OriginalTnt");
            Object.DontDestroyOnLoad(go);
            _host = go.AddComponent<StoryOriginalTntHost>();
            return _host;
        }
    }

    /// <summary>
    /// Owns live blasts and fuses: pauses them with the game and retires them when done,
    /// and holds a TNT+TNT body for the length of its anticipation.
    /// </summary>
    public sealed class StoryOriginalTntHost : MonoBehaviour
    {
        private readonly List<GameObject> _blasts = new List<GameObject>();
        private readonly List<GameObject> _fuses = new List<GameObject>();
        private readonly List<GameObject> _owned = new List<GameObject>();
        private bool _paused;

        public int Active => _blasts.Count;

        public bool Paused
        {
            get { return _paused; }
            set
            {
                if (_paused == value) return;
                _paused = value;
                foreach (var blast in _blasts) ReferenceTnt.Pause(blast, value);
                foreach (var fuse in _fuses) ReferenceTnt.Pause(fuse, value);
                foreach (var go in _owned) ReferenceTnt.Pause(go, value);
            }
        }

        public void Track(GameObject blast)
        {
            if (blast == null) return;
            if (_paused) ReferenceTnt.Pause(blast, true);
            _blasts.Add(blast);
        }

        public void TrackFuse(GameObject fuse)
        {
            if (fuse == null) return;
            if (_paused) ReferenceTnt.Pause(fuse, true);
            _fuses.Add(fuse);
        }

        /// <summary>Kept until <see cref="Drop"/> or <see cref="Retire"/>, whatever its particles do.</summary>
        public GameObject Own(GameObject go)
        {
            if (go != null) _owned.Add(go);
            return go;
        }

        public void Drop(GameObject go)
        {
            if (go == null) return;
            _owned.Remove(go);
            Destroy(go);
        }

        /// <summary>Hands an owned effect to the fuse list, which removes it when its particles die.</summary>
        public void Retire(GameObject go)
        {
            if (go == null) return;
            _owned.Remove(go);
            _fuses.Add(go);
        }

        public void Clear()
        {
            StopAllCoroutines();
            foreach (var list in new[] { _blasts, _fuses, _owned })
            {
                foreach (var go in list) if (go != null) Destroy(go);
                list.Clear();
            }
        }

        private void Update()
        {
            if (_paused) return;
            Retire(_blasts);
            Retire(_fuses);
        }

        private static void Retire(List<GameObject> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (ReferenceTnt.Alive(list[i])) continue;
                if (list[i] != null) Destroy(list[i]);
                list.RemoveAt(i);
            }
        }
    }
}
