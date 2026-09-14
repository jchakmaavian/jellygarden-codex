// Why a cleared gem shows no effect.
//
// Three candidates came out of reading the kit, and reading cannot separate
// them — each would look exactly like "the cell just becomes empty":
//
//   POOL      LevelManager.GetExplFromPool() is a linear scan over 20 slots
//             that returns null when every slot reads active, and Item.cs
//             no-ops on null. If HideDelayed never recycles a slot, the first
//             20 clears show an effect and every clear after shows nothing.
//
//   SORTING   ItemExpl is authored at sortingOrder 0, the same order as the
//             cell it lands on. The gem is at 2. A tie at 0 is broken by
//             distance, so the effect can end up under the board's own tile.
//
//   SCALE     Item.cs hardcodes localScale 0.5 while the story plays at
//             Pitch/KitPitch = 1.427. The effect draws at about 55% of the
//             gem beside it, which is small enough to miss.
//
// This watches the pool from outside and prints what actually happened. It
// touches nothing: no kit script is edited, no prefab is changed, and the
// probe only reads. Delete the file to remove it.
//
//     tools/run_player.sh -storyexplosionprobe
//
// One line per lease, plus a summary every time the pool runs dry.

using System.Text;
using UnityEngine;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryExplosionProbe : MonoBehaviour
    {
        private const string Flag = "-storyexplosionprobe";
        private const string LiftFlag = "-storyliftorder";

        private static bool _installed;

        /// <summary>
        /// Whether to raise a leased effect's sorting order above the gems.
        ///
        /// Opt-in, and deliberately not the default: it is a diagnostic for one
        /// question — is the blast being drawn under the board's own cell tile?
        /// ItemExpl and Square are BOTH authored at sortingOrder 0 on the
        /// Default layer, and on a Storybook board the cell tile is painted
        /// onto that square's own renderer, so the two tie and the winner is
        /// decided by distance rather than by intent.
        /// </summary>
        public static bool LiftOrder
        {
            get
            {
                foreach (string arg in global::System.Environment.GetCommandLineArgs())
                    if (arg == LiftFlag) return true;
                return false;
            }
        }

        /// <summary>Whether this launch asked for the probe.</summary>
        public static bool Requested
        {
            get
            {
                foreach (string arg in global::System.Environment.GetCommandLineArgs())
                    if (arg == Flag) return true;
                return false;
            }
        }

        public static void Install()
        {
            if (_installed || !Requested) return;
            _installed = true;
            var go = new GameObject("~StoryExplosionProbe");
            DontDestroyOnLoad(go);
            go.AddComponent<StoryExplosionProbe>();
            Debug.Log("[Probe] explosion probe armed");
        }

        // Two bursts of consecutive frames. Enough to catch a 267ms flipbook
        // and a 420ms spray whatever frame they actually start drawing on,
        // without filling the disk.
        private const int BurstFrames = 6;
        private const int MaxShots = 12;

        private LevelManager _manager;
        private bool[] _wasActive;
        private int _leases;
        private int _dryReports;
        private float _nextSweep;
        private int _shots;
        private bool _shooting;

        /// <summary>Where the blast shots land, beside the playtest's own.</summary>
        private static string ShotDir
        {
            get
            {
                return global::System.IO.Path.Combine(Application.persistentDataPath, "blastshots");
            }
        }

        /// <summary>
        /// One screenshot taken while a blast is actually on screen.
        ///
        /// It has to wait for the end of the frame: StoryWindow.Capture reads
        /// the back buffer, and reading it mid-frame gets the previous frame's
        /// contents. One extra frame of delay also lands the shot on the
        /// effect's second animation frame, which is its peak.
        /// </summary>
        private global::System.Collections.IEnumerator Shoot()
        {
            if (_shooting) yield break;
            _shooting = true;

            // A BURST, not one shot. The single shot this replaced fired one
            // frame after the lease was noticed, and that is very likely before
            // anything had rendered: the flipbook's Animator writes its sprite
            // on ITS next update, and a ParticleSystem given Play() does not
            // put a particle on screen until the frame after emission. Six
            // consecutive frames cannot all miss a 267ms flipbook or a 420ms
            // shard spray, so if every frame of a burst is empty the effect is
            // genuinely not drawing rather than mistimed.
            for (int frame = 0; frame < BurstFrames && _shots < MaxShots; frame++)
            {
                yield return new WaitForEndOfFrame();
                try
                {
                    if (!global::System.IO.Directory.Exists(ShotDir))
                        global::System.IO.Directory.CreateDirectory(ShotDir);
                    string path = global::System.IO.Path.Combine(
                        ShotDir, string.Format("blast_{0:0000}_f{1}.png", _shots + 1, frame));
                    StoryWindow.Capture(path);
                    _shots++;
                    Debug.Log("[Probe] shot f" + frame + " -> " + path);
                }
                catch (global::System.Exception e)
                {
                    Debug.Log("[Probe] shot failed: " + e.Message);
                }
            }

            yield return new WaitForSeconds(0.5f);
            _shooting = false;
        }

        private void Update()
        {
            if (_manager == null)
            {
                _manager = LevelManager.THIS;
                if (_manager == null) return;
                _wasActive = null;
                Debug.Log("[Probe] LevelManager found; pool length " +
                          (_manager.itemExplPool == null ? -1 : _manager.itemExplPool.Length));
            }

            var pool = _manager.itemExplPool;
            if (pool == null) return;

            if (_wasActive == null || _wasActive.Length != pool.Length)
                _wasActive = new bool[pool.Length];

            int active = 0;
            int nulls = 0;

            for (int i = 0; i < pool.Length; i++)
            {
                ItemAnimEvents slot = pool[i];
                if (slot == null) { nulls++; continue; }

                bool now = slot.IsActive();
                if (now) active++;

                // A slot that just went from idle to active is a lease: this is
                // the moment Item.cs positioned and scaled it, so it is the
                // only moment worth measuring.
                if (now && !_wasActive[i])
                {
                    _leases++;

                    // THE TEST: the effect is authored at sortingOrder 0 — the
                    // same order as the cell tile it lands on, while the gem it
                    // replaces is at 2. The earlier "over-cell: none" check was
                    // worthless: Square.prefab carries no Collider2D, so
                    // Physics2D.OverlapPointAll could never see the tile.
                    //
                    // Lifting the order above the gems is a one-line, runtime
                    // only change to a pooled object. If the blast appears, the
                    // fault is sorting; if it does not, it is something else
                    // and this must be reverted rather than left in.
                    if (LiftOrder)
                    {
                        var lift = slot.GetComponent<SpriteRenderer>();
                        if (lift != null) lift.sortingOrder = 6;
                    }

                    Report(i, slot);
                }
                _wasActive[i] = now;
            }

            // The pool being full is the failure the kit hides: GetExplFromPool
            // returns null and the caller silently skips the effect.
            if (active == pool.Length && nulls == 0 && Time.time >= _nextSweep)
            {
                _nextSweep = Time.time + 1f;
                _dryReports++;
                Debug.Log("[Probe] POOL DRY: all " + pool.Length +
                          " slots read active. Every clear from here shows nothing. " +
                          "(dry report #" + _dryReports + ", leases so far " + _leases + ")");
            }

            if (nulls > 0 && Time.time >= _nextSweep)
            {
                _nextSweep = Time.time + 1f;
                Debug.Log("[Probe] " + nulls + " of " + pool.Length + " pool slots are NULL");
            }
        }

        private void Report(int index, ItemAnimEvents slot)
        {
            var sb = new StringBuilder();
            sb.Append("[Probe] lease #").Append(_leases)
              .Append(" slot ").Append(index);

            Transform t = slot.transform;
            sb.Append(" pos ").Append(t.position.ToString("F2"));
            sb.Append(" scale ").Append(t.localScale.x.ToString("F3"));

            var sr = slot.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sb.Append(" order ").Append(sr.sortingOrder);
                sb.Append(" layer ").Append(sr.sortingLayerName);
                sb.Append(" sprite ").Append(sr.sprite == null ? "NULL" : sr.sprite.name);
                sb.Append(" enabled ").Append(sr.enabled);
                sb.Append(" alpha ").Append(sr.color.a.ToString("F2"));
            }
            else sb.Append(" NO SpriteRenderer");

            var an = slot.GetComponent<Animator>();
            if (an != null)
            {
                sb.Append(" animator ").Append(an.enabled ? "on" : "OFF");
                sb.Append(" color=").Append(an.GetInteger("color"));
                sb.Append(" controller ")
                  .Append(an.runtimeAnimatorController == null
                      ? "NULL" : an.runtimeAnimatorController.name);
            }
            else sb.Append(" NO Animator");

            // What the effect has to beat to be seen. The gem it replaces sits
            // at order 2, the cell under it at 0.
            sb.Append(" | gemScale ").Append(StoryBoardScale.ItemScale.ToString("F3"));
            sb.Append(" factor ").Append(StoryBoardScale.Factor.ToString("F3"));

            // An orthographic camera with a NEGATIVE near clip renders a slab
            // that extends behind its own transform, so WorldToViewportPoint
            // returning a negative z does NOT mean the object is unseen — the
            // gems sit at the same z and are plainly visible. The honest test
            // is the clip range, not the sign.
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 v = cam.WorldToViewportPoint(t.position);
                bool framed = v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f;
                bool inSlab = v.z >= cam.nearClipPlane && v.z <= cam.farClipPlane;
                sb.Append(" viewport ").Append(v.ToString("F2"))
                  .Append(" clip[").Append(cam.nearClipPlane.ToString("F1"))
                  .Append(",").Append(cam.farClipPlane.ToString("F0")).Append("]")
                  .Append(framed ? " FRAMED" : " OUTSIDE-FRAME")
                  .Append(inSlab ? " IN-SLAB" : " CLIPPED");
            }

            // What is drawn over this exact cell, and in what order. This is
            // the remaining candidate: the effect is authored at order 0, the
            // same order as the cell tile it lands on, while the gem it
            // replaces is at 2. A tie is broken arbitrarily.
            Collider2D[] hits = Physics2D.OverlapPointAll(t.position);
            var covering = new StringBuilder();
            foreach (Collider2D hit in hits)
            {
                if (hit == null) continue;
                var other = hit.GetComponent<SpriteRenderer>();
                if (other == null || other == sr) continue;
                covering.Append(' ').Append(hit.name).Append('@').Append(other.sortingOrder);
            }
            sb.Append(" | over-cell:").Append(covering.Length == 0 ? " none" : covering.ToString());

            Debug.Log(sb.ToString());

            // A log line is not a picture. Every wrong conclusion about this
            // effect so far survived the numbers and died the moment someone
            // looked, so the probe takes its own shot while a blast is on
            // screen — the one frame no start/end capture can catch.
            if (_shots < MaxShots) StartCoroutine(Shoot());
        }
    }
}
