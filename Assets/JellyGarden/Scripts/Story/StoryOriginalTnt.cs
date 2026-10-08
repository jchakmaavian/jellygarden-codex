// The original Royal Throne TNT on the King Leo board, in place of RoyalTNTVFX.
//
// The art, clips, particles and sounds are the ones imported from royal-throne
// under Assets/ReferenceGameplay; ReferenceTnt draws them by the rules the game
// used there. This layer only decides WHEN: a TNT piece plays the creation clip
// and then holds its last frame (StoryOriginalTntPiece), and a detonation plays
// the original blast and its sound.
//
// WHAT IT DOES NOT CHANGE
//
// Item still owns the clear, its 3x3 area, scoring and timing. Royal Throne held
// the damage for a 0.12s fuse before its blast; King Leo clears on the tap, so
// the blast starts on the tap too and there is no separate ignition beat.
//
// THE SWITCH
//
// -originaltnt / -nooriginaltnt, or the StoryVfx.OriginalTnt pref; on by
// default. Off, or with any imported asset missing, every call returns false
// and the board keeps RoyalTNTVFX and the kit's TNT sprite exactly as before.

using System.Collections.Generic;
using RoyalThrone.ReferenceGameplay.Preview;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public static class StoryOriginalTnt
    {
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

        private static StoryOriginalTntHost _host;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _enabled = null;
            _assets = null;
            _host = null;
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
            if (reveal) Sound(ReferenceTnt.CreationAudio);
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
            var host = Host();
            var blast = ReferenceTnt.PlayVfx(ReferenceTnt.ExplodeVfx, host.transform, at, pitch, 0, ReferenceTnt.VfxOrder);
            if (blast == null) return false;
            host.Track(blast);
            Sound(ReferenceTnt.ExplodeAudio);
            return true;
        }

        public static void StateChanged(GameState state)
        {
            if (state == GameState.Map || state == GameState.PrepareGame) Clear();
            if (_host != null) _host.Paused = state == GameState.Pause;
        }

        public static void Clear()
        {
            if (_host != null) _host.Clear();
        }

        // Through SoundBase, so the player's sound setting and volume apply.
        private static void Sound(string clip)
        {
            if (SoundBase.Instance != null) SoundBase.Instance.PlaySound(ReferenceTnt.Clip(clip));
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

    /// <summary>Owns live blasts: pauses them with the game and retires them when done.</summary>
    public sealed class StoryOriginalTntHost : MonoBehaviour
    {
        private readonly List<GameObject> _blasts = new List<GameObject>();
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
            }
        }

        public void Track(GameObject blast)
        {
            if (_paused) ReferenceTnt.Pause(blast, true);
            _blasts.Add(blast);
        }

        public void Clear()
        {
            foreach (var blast in _blasts) if (blast != null) Destroy(blast);
            _blasts.Clear();
        }

        private void Update()
        {
            if (_paused) return;
            for (int i = _blasts.Count - 1; i >= 0; i--)
            {
                if (ReferenceTnt.Alive(_blasts[i])) continue;
                if (_blasts[i] != null) Destroy(_blasts[i]);
                _blasts.RemoveAt(i);
            }
        }
    }
}
