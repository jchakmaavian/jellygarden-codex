using System.Collections.Generic;
using UnityEngine;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>Power-up families understood by the sequences.</summary>
    public enum Match3Power
    {
        None,
        RocketHorizontal,
        RocketVertical,
        Tnt,
        Helicopter,
        ColorOrb
    }

    /// <summary>What cleared a cell, so the game can pick its own rules or flags.</summary>
    public enum Match3HitSource
    {
        Rocket,
        Blast,
        Helicopter,
        ColorOrb
    }

    /// <summary>How a piece leaves the board.</summary>
    public enum Match3PopKind
    {
        /// <summary>Confirmed 3+ match: soft pop, sparkle burst, chips that fade.</summary>
        Match,
        /// <summary>Cleared by a blast: snappy pop, reduced burst, solid chips that fall off the board.</summary>
        PowerClear,
        /// <summary>Cleared by a rocket head that already played its contact flash: immediate chips.</summary>
        Rocket
    }

    /// <summary>Debris behaviour for <see cref="Match3Vfx.Shatter"/>.</summary>
    public enum Match3ShatterStyle
    {
        Match,
        PowerClear,
        Rocket
    }

    /// <summary>Sound cues. Assign clips in <see cref="Match3VfxSettings"/>; missing clips are silent.</summary>
    public enum Match3SfxCue
    {
        Match,
        Land,
        Rocket,
        Tnt,
        Helicopter,
        Create,
        Click,
        Firework,
        Celebrate
    }

    /// <summary>Effects that can be previewed without a board.</summary>
    public enum Match3PreviewEffect
    {
        Match3,
        Match4,
        Match5,
        Rocket,
        Tnt,
        Helicopter,
        Creation,
        Firework,
        ComboBanner
    }

    /// <summary>
    /// What the kit needs to know about the running game. Implement it once per project
    /// (see the JellyGarden adapter sample) or use <see cref="Match3VfxHost"/> and set its fields.
    /// </summary>
    public interface IMatch3VfxHost
    {
        /// <summary>World units per board cell. Effect sizes are multiplied by this.</summary>
        float CellSize { get; }

        /// <summary>While true, particles, projectiles and sequences hold their state.</summary>
        bool IsPaused { get; }

        /// <summary>Accessibility preference: no flash, no shake, fewer particles.</summary>
        bool ReducedMotion { get; }

        bool SoundEnabled { get; }

        /// <summary>World Y of the lowest board cell centre, or NegativeInfinity if unknown. Debris recycles below it.</summary>
        float BoardBottomY { get; }

        /// <summary>Camera used for impact shake, or null for no shake.</summary>
        Camera ShakeCamera { get; }
    }

    /// <summary>
    /// Grid access for power-up sequences. The kit decides WHEN a cell is reached and what it
    /// looks like; the game decides WHAT happens to the cell in <see cref="HitCell"/>.
    /// Cell coordinates are (column, row). Row direction is up to the game.
    /// </summary>
    public interface IMatch3Board
    {
        int Columns { get; }
        int Rows { get; }

        /// <summary>True when the coordinate is a playable cell (inside the grid and not a hole).</summary>
        bool IsCell(Vector2Int cell);

        /// <summary>
        /// Centre of a cell in world space. Must also answer for coordinates one step outside the
        /// grid, which rockets use to fly past the edge.
        /// </summary>
        Vector3 CellToWorld(Vector2Int cell);

        /// <summary>Apply the game's destruction rules to one cell. Called at the visual contact moment.</summary>
        void HitCell(Vector2Int cell, Match3HitSource source);

        /// <summary>
        /// Choose where a helicopter flies. Skip cells in <paramref name="reserved"/> so several
        /// helicopters spread out. Return false when nothing is worth targeting.
        /// </summary>
        bool TryPickHelicopterTarget(Vector2Int origin, ICollection<Vector2Int> reserved, out Vector2Int target);
    }

    /// <summary>Plain host with settable values. Good for prototypes, tests and simple games.</summary>
    public sealed class Match3VfxHost : IMatch3VfxHost
    {
        public float cellSize = 1f;
        public bool isPaused;
        public bool reducedMotion;
        public bool soundEnabled = true;
        public float boardBottomY = float.NegativeInfinity;
        /// <summary>Null falls back to Camera.main.</summary>
        public Camera shakeCamera;
        public bool useMainCameraForShake = true;

        public float CellSize => Mathf.Max(0.0001f, cellSize);
        public bool IsPaused => isPaused;
        public bool ReducedMotion => reducedMotion;
        public bool SoundEnabled => soundEnabled;
        public float BoardBottomY => boardBottomY;
        public Camera ShakeCamera => shakeCamera ? shakeCamera : (useMainCameraForShake ? Camera.main : null);
    }
}
