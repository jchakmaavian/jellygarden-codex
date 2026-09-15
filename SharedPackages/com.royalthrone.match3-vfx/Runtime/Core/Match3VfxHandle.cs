using UnityEngine;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>
    /// Tracks one power-up sequence including the delayed cell hits it starts.
    /// <c>yield return handle;</c> waits until every hit of that sequence has been delivered.
    /// </summary>
    public sealed class Match3VfxHandle : CustomYieldInstruction
    {
        int open;

        /// <summary>A handle that is already complete (returned when nothing could play).</summary>
        public static Match3VfxHandle Completed => new Match3VfxHandle();

        public bool IsDone => open <= 0;
        /// <summary>True when <see cref="Match3Vfx.Clear"/> stopped the sequence early.</summary>
        public bool Cancelled { get; private set; }
        public override bool keepWaiting => !IsDone;

        internal void Open() => open++;
        internal void Close() { if (open > 0) open--; }
        internal void Cancel() { open = 0; Cancelled = true; }
    }
}
