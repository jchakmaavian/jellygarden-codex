// Installs the two ZIP packages for every gameplay entry point. They own the
// artwork and animation; Item still owns damage, scoring and board reservation.
using UnityEngine;
using RoyalRocketVFX;
using RoyalTNTVFX;

namespace JellyGarden.Scripts.Story
{
    public static class StoryRoyalVfx
    {
        const string RocketPrefab = "RoyalVfx/RocketVFX_Horizontal";
        const string TntPrefab = "RoyalVfx/TNTExplosionVFX";
        static Transform root;
        static bool rocketMissing, tntMissing;

        public static bool RocketReady => RocketVFX.Player != null;
        public static bool TntReady => TNTExplosionVFX.Player != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            root = null;
            rocketMissing = tntMissing = false;
        }

        public static void Prewarm()
        {
            if (!Application.isPlaying) return;
            Rocket();
            Tnt();
            StateChanged(LevelManager.THIS != null ? LevelManager.THIS.gameStatus : GameState.Map);
        }

        public static bool FireRocket(Vector3 at, bool horizontal, int negative, int positive, float pitch)
        {
            if (!Application.isPlaying) return false;
            // The original Royal Throne Rocket flies when it is on; see
            // StoryOriginalBoosters. RoyalRocketVFX stays the fallback.
            if (StoryOriginalBoosters.FireRocket(at, horizontal, negative, positive, pitch)) return true;
            var player = Rocket();
            if (player == null) return false;
            at = EffectPosition(at, pitch);
            // Edge launches have different distances on the two sides. The
            // package then carries each head past the screen at its own speed.
            player.Play(at, horizontal ? RocketDirection.Left : RocketDirection.Down, negative, pitch);
            player.Play(at, horizontal ? RocketDirection.Right : RocketDirection.Up, positive, pitch);
            return true;
        }

        public static float RocketReachesAt(int distance)
        {
            // Cells clear as the original halves' noses reach them.
            if (StoryOriginalBoosters.OwnsRocket) return StoryOriginalBoosters.RocketReachesAt(distance);
            var player = Application.isPlaying ? Rocket() : null;
            if (player == null) return StoryRocketFlight.ReachesAt(distance);
            var tuning = player.Tuning;
            // Same nose offset and constant velocity used by the package's
            // CellReached events. Items reserve now and hide at this contact.
            return Mathf.Max(0f, tuning.anticipation) +
                Mathf.Max(0f, Mathf.Abs(distance) - tuning.rocketScale) / Mathf.Max(1f, tuning.travelSpeed);
        }

        public static bool Blast(Vector3 at, int radius)
        {
            if (!Application.isPlaying) return false;
            float pitch = StoryBoardScale.Pitch;
            // The original Royal Throne TNT draws the blast when it is on; see
            // StoryOriginalTnt. RoyalTNTVFX stays the fallback.
            if (StoryOriginalTnt.Blast(EffectPosition(at, pitch), pitch)) return true;
            var player = Tnt();
            if (player == null) return false;
            player.Play(EffectPosition(at, pitch), radius, pitch);
            return true;
        }

        public static void StateChanged(GameState state)
        {
            // Same-scene retries and map returns also need to retire effects.
            if (state == GameState.Map || state == GameState.PrepareGame) Clear();
            bool paused = state == GameState.Pause;
            if (RocketVFX.Player != null) RocketVFX.Player.Paused = paused;
            if (TNTExplosionVFX.Player != null) TNTExplosionVFX.Player.Paused = paused;
            StoryOriginalTnt.StateChanged(state);
            StoryOriginalBoosters.StateChanged(state);
        }

        public static void Clear()
        {
            RocketVFX.Clear();
            TNTExplosionVFX.Clear();
            StoryOriginalTnt.Clear();
            StoryOriginalBoosters.Clear();
            StoryOriginalMotion.Clear();
        }

        internal static Vector3 EffectPosition(Vector3 at, float pitch)
        {
            // The board can sit on the orthographic camera's plane. Both ZIPs
            // offset layers toward -Z, and particles behind the lens disappear
            // even with a negative near clip. Keep every layer in front of it;
            // sorting orders still place the effects above the gems.
            var camera = Camera.main;
            if (camera != null && camera.orthographic)
                at.z = Mathf.Max(at.z, camera.transform.position.z +
                    Mathf.Max(0.01f, camera.nearClipPlane) + 2f * Mathf.Max(1f, pitch));
            return at;
        }

        static RocketVFXPlayer Rocket()
        {
            if (RocketVFX.Player != null) return RocketVFX.Player;
            if (rocketMissing) return null;
            rocketMissing = !Install(RocketPrefab, "~RoyalRocketVFX");
            return RocketVFX.Player;
        }

        static TNTExplosionPlayer Tnt()
        {
            if (TNTExplosionVFX.Player != null) return TNTExplosionVFX.Player;
            if (tntMissing) return null;
            tntMissing = !Install(TntPrefab, "~RoyalTNTVFX");
            return TNTExplosionVFX.Player;
        }

        static bool Install(string path, string name)
        {
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning("[RoyalVfx] Missing Resources/" + path + "; using the fallback effect.");
                return false;
            }
            if (root == null)
            {
                var host = new GameObject("~RoyalVfx");
                Object.DontDestroyOnLoad(host);
                root = host.transform;
            }
            Object.Instantiate(prefab, root).name = name;
            bool paused = LevelManager.THIS != null && LevelManager.THIS.gameStatus == GameState.Pause;
            if (RocketVFX.Player != null) RocketVFX.Player.Paused = paused;
            if (TNTExplosionVFX.Player != null) TNTExplosionVFX.Player.Paused = paused;
            return true;
        }
    }
}
