using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>
    /// Entry point of the kit. Every call is presentation only: it never changes matches,
    /// scores, targets or board ownership. Power-up sequences report cell contacts through
    /// <see cref="IMatch3Board.HitCell"/> so the game applies its own rules.
    ///
    /// The pooled rig is created on first use (or by <see cref="Prewarm"/>) and survives scene
    /// loads. Call <see cref="Clear"/> when leaving a level so no effect outlives its board.
    /// </summary>
    public static class Match3Vfx
    {
        static Match3VfxSettings settings;
        static IMatch3VfxHost host;
        static Match3VfxRig rig;
        static bool quitting;

        /// <summary>Raised with a 0-1.5 strength for character reactions (for example a king's expression).</summary>
        public static event Action<float> Reaction;

        /// <summary>Raised with a duration in seconds when a loud effect wants the music lowered.</summary>
        public static event Action<float> MusicDuckRequested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            settings = null;
            host = null;
            rig = null;
            quitting = false;
            Reaction = null;
            MusicDuckRequested = null;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        static void OnQuitting() => quitting = true;

        // ------------------------------------------------------------------ configuration

        /// <summary>
        /// Active settings. Defaults to Resources/Match3VfxSettings, then to built-in defaults.
        /// Assigning new settings rebuilds the pools on next use.
        /// </summary>
        public static Match3VfxSettings Settings
        {
            get
            {
                if (!settings)
                {
                    settings = Resources.Load<Match3VfxSettings>(Match3VfxSettings.ResourcesPath);
                    if (!settings) settings = Match3VfxSettings.CreateDefaults();
                }
                return settings;
            }
            set
            {
                if (settings == value) return;
                settings = value;
                if (rig) Shutdown();
            }
        }

        /// <summary>Game state the kit reads (cell size, pause, reduced motion...).</summary>
        public static IMatch3VfxHost Host
        {
            get => host ?? (host = new Match3VfxHost());
            set => host = value;
        }

        /// <summary>Grid used by power-up sequences. Required for PlayRocket/PlayExplosion/PlayHelicopter/PlayColorOrb.</summary>
        public static IMatch3Board Board { get; set; }

        public static void Configure(Match3VfxSettings newSettings, IMatch3VfxHost newHost, IMatch3Board board = null)
        {
            if (newSettings) Settings = newSettings;
            if (newHost != null) Host = newHost;
            Board = board;
        }

        /// <summary>Builds pools and textures now so the first match never hitches.</summary>
        public static void Prewarm() => _ = Rig;

        /// <summary>True while a power-up sequence still has cell hits to deliver. Wait for false before refilling.</summary>
        public static bool Busy => rig && rig.Sequences.Busy;

        public static bool IsReady => rig;

        /// <summary>Stops every effect and sequence immediately. Call when a level ends or the board is replaced.</summary>
        public static void Clear()
        {
            if (rig) rig.ClearAll();
        }

        /// <summary>Destroys the rig. The next call builds a fresh one with the current settings.</summary>
        public static void Shutdown()
        {
            if (!rig) { rig = null; return; }
            rig.ClearAll();
            if (Application.isPlaying) Object.Destroy(rig.gameObject);
            else Object.DestroyImmediate(rig.gameObject);
            rig = null;
        }

        internal static Match3VfxRig Rig
        {
            get
            {
                if (quitting || !Application.isPlaying) return null;
                if (!rig)
                {
                    var go = new GameObject("~Match3Vfx (pooled effects)");
                    Object.DontDestroyOnLoad(go);
                    rig = go.AddComponent<Match3VfxRig>();
                    rig.Build(Settings);
                }
                return rig;
            }
        }

        internal static Match3VfxRig ExistingRig => rig;
        internal static void RaiseReaction(float strength) => Reaction?.Invoke(strength);
        internal static void RaiseMusicDuck(float seconds) => MusicDuckRequested?.Invoke(seconds);

        // ------------------------------------------------------------------ matches

        /// <summary>Emphasis for a group: bigger matches and deeper cascades read stronger, inside a ceiling.</summary>
        public static float ComputeIntensity(int matchSize, int cascadeStep)
        {
            var s = Settings;
            float intensity = 1f;
            if (matchSize >= 5) intensity += s.fiveMatchBonus;
            else if (matchSize == 4) intensity += s.fourMatchBonus;
            if (cascadeStep > 1) intensity += s.cascadeBonusPerStep * (cascadeStep - 1);
            return Mathf.Clamp(intensity, 0.5f, s.intensityMax);
        }

        /// <summary>Stagger for the piece at <paramref name="index"/> inside one group.</summary>
        public static float StaggerFor(int index) => Mathf.Max(0, index) * Settings.matchStagger;

        /// <summary>
        /// Group-level feedback for one confirmed match: shared centre accent, match sound,
        /// optional hit-stop and a reaction event. Call once per group, then pop each piece with
        /// <see cref="Match3PieceMotion.Pop"/>. Returns the intensity to pass to the pops.
        /// </summary>
        public static float PlayMatchGroup(IReadOnlyList<Vector3> positions, Color color, int matchSize, int cascadeStep)
        {
            float intensity = ComputeIntensity(matchSize, cascadeStep);
            var r = Rig;
            var s = Settings;
            if (!r || !s.enableEffects || positions == null || positions.Count == 0) return intensity;

            Vector3 center = Vector3.zero;
            for (int i = 0; i < positions.Count; i++) center += positions[i];
            center /= positions.Count;

            RaiseReaction(Mathf.Min(0.45f, intensity * 0.22f));
            r.Sfx.Play(Match3SfxCue.Match);
            r.Burst.PlayCenter(center, color, intensity, s.groupAccentDelay);
            if (s.enableHitStop)
                r.HitStop.Request(s.groupAccentDelay, s.hitStopDuration + s.hitStopIntensityBonus * (intensity - 1f), s.hitStopScale);
            return intensity;
        }

        /// <summary>Flash, stars, micro sparkles and tinted fragments for one piece.</summary>
        public static void PlayMatchBurst(Vector3 position, Color color, float intensity = 1f, float delay = 0f)
        {
            var r = Rig;
            if (r && Settings.enableEffects) r.Burst.PlayBurst(position, color, intensity, delay);
        }

        /// <summary>Shared ring/star accent at a group centre (or the optional match impact prefab).</summary>
        public static void PlayMatchCenter(Vector3 position, Color color, float intensity = 1f, float delay = 0f)
        {
            var r = Rig;
            if (r && Settings.enableEffects) r.Burst.PlayCenter(position, color, intensity, delay);
        }

        /// <summary>Short additive redraw of a sprite, just before it pops.</summary>
        public static void PlayShine(SpriteRenderer source, float delay = 0f)
        {
            var r = Rig;
            var s = Settings;
            if (r && s.enableEffects) r.Burst.PlayShine(source, s.shineDuration, s.shineStrength, delay);
        }

        /// <summary>Breaks the sprite currently shown by <paramref name="source"/> into pooled debris. Hide the source yourself.</summary>
        public static void Shatter(SpriteRenderer source, float intensity = 1f, Match3ShatterStyle style = Match3ShatterStyle.Match, int colorIndex = -1)
        {
            var r = Rig;
            if (r && Settings.enableEffects && Settings.enableShatter) r.Shatter.Shatter(source, intensity, style, colorIndex);
        }

        public static void RequestHitStop(float delay, float duration, float scale)
        {
            var r = Rig;
            if (r && Settings.enableEffects) r.HitStop.Request(delay, duration, scale);
        }

        // ------------------------------------------------------------------ power-ups

        /// <summary>Activates one power-up. Colour orbs need <paramref name="orbTargets"/> chosen by the game.</summary>
        public static Match3VfxHandle PlayPowerup(Match3Power power, Vector2Int origin, IReadOnlyList<Vector2Int> orbTargets = null)
        {
            var r = ReadyForSequence();
            return r ? r.Sequences.Single(power, origin, orbTargets) : Match3VfxHandle.Completed;
        }

        /// <summary>
        /// Two power-ups swapped together. Orb pairs use <paramref name="orbTargets"/>;
        /// <paramref name="convert"/> is called for each orb target before it is hit so the game can
        /// turn it into the other power-up.
        /// </summary>
        public static Match3VfxHandle PlayCombo(Match3Power a, Match3Power b, Vector2Int origin,
            IReadOnlyList<Vector2Int> orbTargets = null, Action<Vector2Int, Match3Power> convert = null)
        {
            var r = ReadyForSequence();
            return r ? r.Sequences.Pair(a, b, origin, orbTargets, convert) : Match3VfxHandle.Completed;
        }

        public static Match3VfxHandle PlayRocket(Vector2Int origin, bool horizontal, int lanes = 1)
        {
            var r = ReadyForSequence();
            return r ? r.Sequences.StartRocket(origin, horizontal, lanes) : Match3VfxHandle.Completed;
        }

        public static Match3VfxHandle PlayExplosion(Vector2Int origin, int radius = 2)
        {
            var r = ReadyForSequence();
            return r ? r.Sequences.StartExplosion(origin, radius) : Match3VfxHandle.Completed;
        }

        public static Match3VfxHandle PlayHelicopter(Vector2Int origin, Match3Power payload = Match3Power.None, int count = 1)
        {
            var r = ReadyForSequence();
            return r ? r.Sequences.StartHelicopter(origin, payload, count) : Match3VfxHandle.Completed;
        }

        public static Match3VfxHandle PlayColorOrb(Vector2Int origin, IReadOnlyList<Vector2Int> targets,
            Match3Power payload = Match3Power.None, Action<Vector2Int, Match3Power> convert = null)
        {
            var r = ReadyForSequence();
            return r ? r.Sequences.StartOrb(origin, targets, payload, convert, false) : Match3VfxHandle.Completed;
        }

        static Match3VfxRig ReadyForSequence()
        {
            if (Board == null)
            {
                Debug.LogWarning("[Match3Vfx] Assign Match3Vfx.Board before playing power-up sequences.");
                return null;
            }
            return Rig;
        }

        /// <summary>TNT-style blast at a world position (visual only, no cell hits).</summary>
        public static void PlayBlast(Vector3 position, float strength = 1f)
        {
            var r = Rig;
            if (r && Settings.enableEffects) r.Powerups.Blast(position, strength);
        }

        /// <summary>Power-up formed: small ring, sparkles, create sound.</summary>
        public static void PlayCreation(Vector3 position)
        {
            var r = Rig;
            if (r && Settings.enableEffects) r.Powerups.Creation(position);
        }

        /// <summary>A blocker layer chipped: dust and embers.</summary>
        public static void PlayChip(Vector3 position)
        {
            var r = Rig;
            if (r && Settings.enableEffects) r.Powerups.Chip(position);
        }

        public static void PlayFirework(Vector3 position, float intensity = 1f)
        {
            var r = Rig;
            if (r && Settings.enableEffects) r.Powerups.Firework(position, intensity);
        }

        /// <summary>Plays an effect at a world position without a board, for editor previews and menus.</summary>
        public static void Preview(Match3PreviewEffect effect, Vector3 position)
        {
            var r = Rig;
            if (r) r.Preview(effect, position);
        }

        // ------------------------------------------------------------------ feedback

        /// <summary>Adds camera shake energy (0-1.5). Ignored with reduced motion.</summary>
        public static void Shake(float energy)
        {
            var r = Rig;
            if (r && Settings.enableEffects) r.Shake.Add(energy);
        }

        public static void PlaySfx(Match3SfxCue cue, float strength = 1f)
        {
            var r = Rig;
            if (r) r.Sfx.Play(cue, strength);
        }

        /// <summary>Plays an arbitrary clip through the capped voice pool.</summary>
        public static void PlayClip(AudioClip clip, float gain = 1f, int priority = 150, float pitch = 1f)
        {
            var r = Rig;
            if (r) r.Sfx.PlayClip(clip, gain, priority, pitch);
        }

        /// <summary>Shows GREAT/BRAVO/... for a cascade count. Non-blocking; respects a cooldown. Returns true when shown.</summary>
        public static bool ShowComboBanner(int cascade)
        {
            var r = Rig;
            return r && Settings.enableEffects && Settings.enableComboBanner && r.Banner.Show(cascade);
        }
    }
}
