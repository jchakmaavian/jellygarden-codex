using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>
    /// Timing logic for power-ups. Decides when each cell is reached and plays the visuals; the
    /// game's <see cref="IMatch3Board.HitCell"/> applies destruction at that moment. Every started
    /// hit is owned by the sequence's <see cref="Match3VfxHandle"/>, so refill can wait for it.
    /// </summary>
    internal sealed class PowerupSequencer
    {
        static readonly Vector2Int[] Neighbours = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        readonly Match3VfxRig rig;
        readonly HashSet<Vector2Int> reserved = new HashSet<Vector2Int>();
        readonly List<Match3VfxHandle> live = new List<Match3VfxHandle>(8);
        int pending;

        public bool Busy => pending > 0;
        public int PendingOperations => pending;
        public int RocketLaunches { get; private set; }
        public int RocketCellContacts { get; private set; }
        public float LastRocketSweep { get; private set; }
        public int Explosions { get; private set; }
        public int HelicopterFlights { get; private set; }
        public int OrbActivations { get; private set; }

        IMatch3Board Board => Match3Vfx.Board;
        Match3VfxSettings S => rig.Settings;

        public PowerupSequencer(Match3VfxRig rig) { this.rig = rig; }

        // ------------------------------------------------------------------ ownership

        void Run(IEnumerator sequence, Match3VfxHandle handle)
        {
            if (handle.IsDone && !live.Contains(handle)) live.Add(handle);
            pending++;
            handle.Open();
            rig.StartCoroutine(Own(sequence, handle));
        }

        IEnumerator Own(IEnumerator sequence, Match3VfxHandle handle)
        {
            try { yield return sequence; }
            finally
            {
                // StopAllCoroutines skips finally blocks; Cancel() resets the counters for that path.
                if (!handle.Cancelled)
                {
                    pending = Mathf.Max(0, pending - 1);
                    handle.Close();
                    if (handle.IsDone) live.Remove(handle);
                }
            }
        }

        public void Cancel()
        {
            pending = 0;
            reserved.Clear();
            foreach (var handle in live) handle.Cancel();
            live.Clear();
        }

        bool Paused => rig.Paused;

        // ------------------------------------------------------------------ public starts

        public Match3VfxHandle Single(Match3Power power, Vector2Int origin, IReadOnlyList<Vector2Int> orbTargets)
        {
            var handle = new Match3VfxHandle();
            switch (power)
            {
                case Match3Power.RocketHorizontal: Run(Rocket(origin, true, 1, handle), handle); break;
                case Match3Power.RocketVertical: Run(Rocket(origin, false, 1, handle), handle); break;
                case Match3Power.Tnt: Run(Explosion(origin, 2, handle), handle); break;
                case Match3Power.Helicopter: Run(Helicopter(origin, Match3Power.None, 1, handle), handle); break;
                case Match3Power.ColorOrb: Run(Orb(origin, orbTargets, Match3Power.None, null, false, handle), handle); break;
            }
            return handle;
        }

        public Match3VfxHandle Pair(Match3Power a, Match3Power b, Vector2Int origin, IReadOnlyList<Vector2Int> orbTargets, Action<Vector2Int, Match3Power> convert)
        {
            if (a == Match3Power.None) return Single(b, origin, orbTargets);
            if (b == Match3Power.None) return Single(a, origin, orbTargets);

            var handle = new Match3VfxHandle();
            if (a == Match3Power.ColorOrb || b == Match3Power.ColorOrb)
            {
                bool both = a == b;
                Run(Orb(origin, orbTargets, both ? Match3Power.None : (a == Match3Power.ColorOrb ? b : a), convert, both, handle), handle);
            }
            else if (a == Match3Power.Helicopter || b == Match3Power.Helicopter)
            {
                Run(Helicopter(origin, a == Match3Power.Helicopter ? b : a, a == b ? 3 : 1, handle), handle);
            }
            else if (a == Match3Power.Tnt && b == Match3Power.Tnt)
            {
                Run(Explosion(origin, 4, handle), handle);
            }
            else
            {
                // Rocket + rocket = one cross; rocket + TNT = a three-lane cross.
                int lanes = a == Match3Power.Tnt || b == Match3Power.Tnt ? 3 : 1;
                Run(Rocket(origin, true, lanes, handle), handle);
                Run(Rocket(origin, false, lanes, handle), handle);
            }
            return handle;
        }

        public Match3VfxHandle StartRocket(Vector2Int origin, bool horizontal, int lanes)
        {
            var handle = new Match3VfxHandle();
            Run(Rocket(origin, horizontal, Mathf.Max(1, lanes), handle), handle);
            return handle;
        }

        public Match3VfxHandle StartExplosion(Vector2Int origin, int radius)
        {
            var handle = new Match3VfxHandle();
            Run(Explosion(origin, Mathf.Max(0, radius), handle), handle);
            return handle;
        }

        public Match3VfxHandle StartHelicopter(Vector2Int origin, Match3Power payload, int count)
        {
            var handle = new Match3VfxHandle();
            Run(Helicopter(origin, payload, Mathf.Max(1, count), handle), handle);
            return handle;
        }

        public Match3VfxHandle StartOrb(Vector2Int origin, IReadOnlyList<Vector2Int> targets, Match3Power payload, Action<Vector2Int, Match3Power> convert, bool all)
        {
            var handle = new Match3VfxHandle();
            Run(Orb(origin, targets, payload, convert, all, handle), handle);
            return handle;
        }

        // ------------------------------------------------------------------ rocket

        IEnumerator RocketContact(Vector2Int cell, Match3VfxHandle handle)
        {
            var board = Board;
            if (board == null) yield break;
            RocketCellContacts++;
            rig.Powerups.RocketImpact(board.CellToWorld(cell), false);
            // The head reaches the cell first; the intact piece stays visible for the contact flash.
            yield return rig.Wait(S.rocketContactHold);
            if (board == Board && board.IsCell(cell)) board.HitCell(cell, Match3HitSource.Rocket);
        }

        IEnumerator Rocket(Vector2Int origin, bool horizontal, int lanes, Match3VfxHandle handle)
        {
            var board = Board;
            if (board == null) yield break;
            var s = S;

            Vector2Int axis = horizontal ? Vector2Int.right : Vector2Int.up;
            Vector2Int across = horizontal ? Vector2Int.up : Vector2Int.right;
            Vector3 p = board.CellToWorld(origin);
            Vector3 stepPositive = board.CellToWorld(origin + axis) - p;
            Vector3 stepNegative = board.CellToWorld(origin - axis) - p;
            Vector3 acrossStep = board.CellToWorld(origin + across) - p;
            float pitch = Mathf.Max(0.0001f, stepPositive.magnitude);

            int index = horizontal ? origin.x : origin.y;
            int count = horizontal ? board.Columns : board.Rows;
            int negative = Mathf.Max(0, index);
            int positive = Mathf.Max(0, count - index - 1);
            float reach = Mathf.Max(negative, positive) + 0.8f;
            float duration = Mathf.Clamp(reach / Mathf.Max(1f, s.rocketCellsPerSecond), s.rocketMinSweep, Mathf.Max(s.rocketMinSweep, s.rocketMaxSweep));
            LastRocketSweep = duration;
            RocketLaunches++;

            int half = lanes / 2;
            int laneCount = half * 2 + 1;
            var heads = new Flyer[laneCount * 2];
            int k = 0;
            for (int lane = -half; lane <= half; lane++)
            {
                for (int d = -1; d <= 1; d += 2)
                {
                    var f = rig.Powerups.TakeFlyer(horizontal ? Match3Power.RocketHorizontal : Match3Power.RocketVertical, p + acrossStep * lane, 0.72f);
                    if (f != null)
                    {
                        f.ConfigureRocketTrail(pitch);
                        Vector3 dir = d > 0 ? stepPositive : stepNegative;
                        f.Root.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                    }
                    heads[k++] = f;
                }
            }

            float anticipation = 0f;
            while (anticipation < s.rocketAnticipation)
            {
                yield return null;
                if (!Paused) anticipation += Time.deltaTime;
                float compression = 1f - 0.07f * Mathf.Sin(Mathf.Clamp01(anticipation / s.rocketAnticipation) * Mathf.PI);
                foreach (var f in heads) if (f != null) f.SetPunch(compression);
            }

            rig.Powerups.RocketImpact(p, true);
            rig.Sfx.Play(Match3SfxCue.Rocket);
            foreach (var f in heads) if (f != null) f.StartTrails();

            var hit = new HashSet<Vector2Int>();
            float t = 0f, nextSpark = 0f;
            int stepDone = -1;
            while (t < duration)
            {
                yield return null;
                if (Paused) continue;
                // Never jump over a cell during a hitch: the head must visibly lead the destruction.
                t = Mathf.Min(duration, t + Mathf.Min(Time.deltaTime, duration / reach * 0.4f));
                float distance = reach * t / duration;
                k = 0;
                for (int lane = -half; lane <= half; lane++)
                {
                    for (int d = -1; d <= 1; d += 2)
                    {
                        int edge = d < 0 ? negative : positive;
                        var f = heads[k++];
                        Vector3 dir = d > 0 ? stepPositive : stepNegative;
                        Vector3 head = p + acrossStep * lane + dir * Mathf.Min(distance, edge + 0.8f);
                        if (f != null) f.Root.position = head + Vector3.back * 0.6f;
                        if (distance <= edge + 0.8f && t >= nextSpark) rig.Powerups.TrailSpark(head - dir * 0.25f);
                        if (distance >= edge + 0.8f && f != null)
                        {
                            rig.Powerups.RocketImpact(head, true);
                            rig.Powerups.Retire(f);
                            heads[k - 1] = null;
                        }
                        int last = Mathf.Min(edge, Mathf.FloorToInt(distance));
                        for (int step = stepDone + 1; step <= last; step++)
                        {
                            var cell = origin + axis * (step * d) + across * lane;
                            if (board.IsCell(cell) && hit.Add(cell)) Run(RocketContact(cell, handle), handle);
                        }
                    }
                }
                stepDone = Mathf.FloorToInt(distance);
                if (t >= nextSpark) nextSpark = t + 0.02f;
            }

            foreach (var f in heads)
            {
                if (f == null) continue;
                rig.Powerups.RocketImpact(f.Root.position, true);
                rig.Powerups.Retire(f);
            }
            Match3Vfx.RaiseReaction(0.8f);
        }

        // ------------------------------------------------------------------ TNT

        IEnumerator Explosion(Vector2Int origin, int radius, Match3VfxHandle handle)
        {
            var board = Board;
            if (board == null) yield break;
            var s = S;
            Vector3 p = board.CellToWorld(origin);
            var fuse = rig.Powerups.TakeFlyer(Match3Power.Tnt, p, 0.9f);
            float elapsed = 0f;
            while (elapsed < s.tntAnticipation)
            {
                yield return null;
                if (!Paused) elapsed += Time.deltaTime;
                if (fuse != null) fuse.SetPunch(1f + (rig.ReducedMotion ? 0f : 0.2f) * Mathf.Sin(Mathf.Clamp01(elapsed / s.tntAnticipation) * Mathf.PI));
            }
            if (fuse != null) rig.Powerups.Retire(fuse);

            Explosions++;
            rig.Powerups.Blast(p, radius > 2 ? 1.45f : 1f);
            rig.Sfx.Play(Match3SfxCue.Tnt);
            for (int distance = 0; distance <= radius; distance++)
            {
                for (int y = -distance; y <= distance; y++)
                {
                    for (int x = -distance; x <= distance; x++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != distance) continue;
                        var cell = new Vector2Int(origin.x + x, origin.y + y);
                        if (board.IsCell(cell)) board.HitCell(cell, Match3HitSource.Blast);
                    }
                }
                yield return rig.Wait(s.blastWavePerCell);
            }
        }

        // ------------------------------------------------------------------ helicopter

        IEnumerator Helicopter(Vector2Int origin, Match3Power payload, int count, Match3VfxHandle handle)
        {
            var board = Board;
            if (board == null) yield break;
            foreach (var n in Neighbours)
            {
                var cell = origin + n;
                if (board.IsCell(cell)) board.HitCell(cell, Match3HitSource.Helicopter);
            }
            for (int i = 0; i < count; i++)
            {
                if (board.TryPickHelicopterTarget(origin, reserved, out var target))
                {
                    reserved.Add(target);
                    Run(Flight(origin, target, payload, handle), handle);
                }
                yield return rig.Wait(0.08f);
            }
        }

        IEnumerator Flight(Vector2Int origin, Vector2Int target, Match3Power payload, Match3VfxHandle handle)
        {
            var board = Board;
            if (board == null) yield break;
            var s = S;
            float cell = rig.Cell;
            Vector3 start = board.CellToWorld(origin) + Vector3.back * 0.8f;
            Vector3 end = board.CellToWorld(target) + Vector3.back * 0.8f;
            var f = rig.Powerups.TakeFlyer(Match3Power.Helicopter, start, 0.85f);
            rig.Sfx.Play(Match3SfxCue.Helicopter);
            HelicopterFlights++;

            float t = 0f;
            while (t < s.helicopterLift)
            {
                yield return null;
                if (!Paused) t += Time.deltaTime;
                if (f != null)
                {
                    f.Root.position = start + Vector3.up * cell * 0.5f * Mathf.Clamp01(t / s.helicopterLift);
                    f.Root.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 80f) * 12f);
                }
            }

            Vector3 lifted = start + Vector3.up * cell * 0.5f;
            Vector3 control = (lifted + end) * 0.5f + Vector3.up * cell * 2f;
            t = 0f;
            while (t < s.helicopterFlight)
            {
                yield return null;
                if (!Paused) t += Time.deltaTime;
                float q = Mathf.SmoothStep(0f, 1f, t / s.helicopterFlight);
                if (f != null)
                {
                    f.Root.position = (1f - q) * (1f - q) * lifted + 2f * (1f - q) * q * control + q * q * end;
                    f.Root.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(q * Mathf.PI) * -25f);
                }
            }

            if (f != null) rig.Powerups.Retire(f);
            reserved.Remove(target);
            rig.Powerups.Blast(end, 0.48f);

            if (payload == Match3Power.Tnt) yield return Explosion(target, 2, handle);
            else if (payload == Match3Power.RocketHorizontal || payload == Match3Power.RocketVertical)
                yield return Rocket(target, payload == Match3Power.RocketHorizontal, 1, handle);
            else if (board.IsCell(target)) board.HitCell(target, Match3HitSource.Helicopter);
        }

        // ------------------------------------------------------------------ colour orb

        IEnumerator Orb(Vector2Int origin, IReadOnlyList<Vector2Int> targets, Match3Power payload, Action<Vector2Int, Match3Power> convert, bool all, Match3VfxHandle handle)
        {
            var board = Board;
            if (board == null) yield break;
            var s = S;
            // Copy: the game may rebuild its list while the sequence is running.
            var cells = new List<Vector2Int>(targets ?? Array.Empty<Vector2Int>());
            OrbActivations++;
            rig.Powerups.Creation(board.CellToWorld(origin));
            yield return rig.Wait(s.orbCharge);

            bool converts = convert != null && payload != Match3Power.None && payload != Match3Power.ColorOrb;
            foreach (var cell in cells)
            {
                rig.Powerups.OrbSpark(board.CellToWorld(cell));
                if (converts) convert(cell, payload);
            }
            yield return rig.Wait(s.orbConvertHold);

            foreach (var cell in cells)
            {
                if (board.IsCell(cell)) board.HitCell(cell, Match3HitSource.ColorOrb);
                yield return rig.Wait(all ? 0.005f : 0.014f);
            }
            Match3Vfx.RaiseReaction(1.4f);
        }
    }
}
