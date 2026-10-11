using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RoyalThrone.ReferenceGameplay.Preview.ReferenceTnt;
using static RoyalThrone.ReferenceGameplay.Preview.ReferenceBoosters;

namespace RoyalThrone.ReferenceGameplay.Preview
{
    // Standalone player for every booster, combo and finish effect imported from royal-throne.
    // It rebuilds the game's presentation (ReferencePieceVisual, PropellerVisualV5, ColorBallVisualV4,
    // ReferenceDualColorBallVisual, RoyalPowerupDirector, ReferenceWinFinish) on an empty board:
    // same prefabs, clips, sockets, sorting orders, sounds and timings, with no LevelManager.
    // The game drew a flying Rocket with its own art; here the APK's half-rocket part sprites fly.
    public sealed class ReferenceBoosterPreview : MonoBehaviour
    {
        public enum Sequence
        {
            RocketHorizontal, RocketVertical, Propeller, ColorBall, Tnt,
            RocketRocket, TntRocket, PropellerTnt, PropellerRocket, PropellerPropeller,
            ColorBallColorBall, TntTnt, Match, MovesToRockets, WinStars
        }

        static readonly string[] Labels =
        {
            "Rocket (horizontal)", "Rocket (vertical)", "Propeller", "Color Ball", "TNT",
            "Rocket + Rocket", "TNT + Rocket", "Propeller + TNT", "Propeller + Rocket", "Propeller + Propeller",
            "Color Ball + Color Ball", "TNT + TNT", "Match 3", "Moves left → Rockets", "Win stars"
        };

        public static int SequenceCount => Labels.Length;
        public static string Label(Sequence s) => Labels[(int)s];

        enum Payload { None, Tnt, Rocket }

        // The piece sits under the VFX (35).
        const int PieceOrder = 30;
        // The win stars were drawn by a size-4 orthographic camera; this board's camera is size 8.
        const float WinStarScale = 2;
        // Board colour of the Color Ball's targets and the match (blue).
        const int TargetColor = 1;

        public Sequence sequence = Sequence.RocketHorizontal;
        public bool loop = true;
        [Tooltip("Step to the next sequence after each one.")]
        public bool playAll;
        [Tooltip("World size of one board cell; the game scales every imported asset by this.")]
        public float cell = 1.2f;
        [Tooltip("Seconds a finished piece sits idle before it fires.")]
        public float idleHold = .6f;
        [Tooltip("Pause between plays.")]
        public float gap = .7f;
        [Range(.1f, 2)] public float speed = 1;
        public bool sound = true;

        public string Phase { get; private set; } = "Idle";
        public int Plays { get; private set; }

        readonly List<GameObject> spawned = new List<GameObject>();
        AudioSource voice;
        int running;
        Vector2 scroll;

        void OnEnable()
        {
            if (!voice) { voice = gameObject.AddComponent<AudioSource>(); voice.playOnAwake = false; voice.spatialBlend = 0; }
            Restart();
        }

        void OnDisable() { StopAllCoroutines(); Cleanup(); Time.timeScale = 1; }

        void Update() => Time.timeScale = speed;

        public void Restart()
        {
            StopAllCoroutines();
            Cleanup();
            running = 0;
            StartCoroutine(Run());
        }

        public void Select(Sequence s) { sequence = s; Restart(); }

        IEnumerator Run()
        {
            do
            {
                yield return Play(sequence);
                Plays++;
                Phase = "Idle";
                yield return new WaitForSeconds(gap);
                Cleanup();
                if (playAll) sequence = (Sequence)(((int)sequence + 1) % SequenceCount);
            } while (loop);
        }

        IEnumerator Play(Sequence s)
        {
            Vector3 p = transform.position;
            switch (s)
            {
                case Sequence.RocketHorizontal: return RocketPiece(p, true);
                case Sequence.RocketVertical: return RocketPiece(p, false);
                case Sequence.Propeller: return PropellerPiece(p);
                case Sequence.ColorBall: return ColorBallPiece(p);
                case Sequence.Tnt: return SingleTnt(p);
                case Sequence.RocketRocket: return RocketRocket(p);
                case Sequence.TntRocket: return TntRocket(p);
                case Sequence.PropellerTnt: return PropellerCombo(p, Payload.Tnt);
                case Sequence.PropellerRocket: return PropellerCombo(p, Payload.Rocket);
                case Sequence.PropellerPropeller: return PropellerPropeller(p);
                case Sequence.ColorBallColorBall: return ColorBallColorBall(p);
                case Sequence.TntTnt: return TntTnt(p);
                case Sequence.Match: return Match(p);
                case Sequence.MovesToRockets: return MovesToRockets(p);
                default: return WinStars(p);
            }
        }

        Vector3 Cell(int col, int row) => transform.position + new Vector3(col, row) * cell;

        // ── Rocket ──────────────────────────────────────────────────────────────

        // ReferencePieceVisual reveal → idle, then both halves leave along the line.
        IEnumerator RocketPiece(Vector3 p, bool horizontal)
        {
            Phase = "Creation";
            var piece = RevealRocket(p, horizontal);
            yield return new WaitForSeconds(StateLength(piece, horizontal ? RocketHorizontalState : RocketVerticalState));
            Freeze(piece, horizontal ? RocketHorizontalState : RocketVerticalState);
            Phase = "Idle piece";
            yield return new WaitForSeconds(idleHold);
            if (piece) piece.SetActive(false);
            yield return Rockets(p, horizontal, 1);
        }

        GameObject RevealRocket(Vector3 p, bool horizontal)
        {
            var piece = Track(SpawnVisual(RocketKey, transform, p, cell, 0, PieceOrder));
            ReferenceTnt.Play(AnimatorOf(piece), horizontal ? RocketHorizontalState : RocketVerticalState);
            Branch(piece, horizontal);
            Sound(RocketCreationAudio, .72f);
            return piece;
        }

        // Pair: one Rocket each way through the origin.
        IEnumerator RocketRocket(Vector3 p)
        {
            Phase = "Creation";
            var piece = RevealRocket(p, true);
            yield return new WaitForSeconds(StateLength(piece, RocketHorizontalState));
            Freeze(piece, RocketHorizontalState);
            yield return new WaitForSeconds(idleHold);
            if (piece) piece.SetActive(false);
            yield return Together(Rockets(p, true, 1), Rockets(p, false, 1));
        }

        // RoyalRocketTntCombo: the charge holds until its first particles show, then a burst and three lanes each way.
        IEnumerator TntRocket(Vector3 p)
        {
            Phase = "Combo charge";
            var charge = Track(PlayVfx(TntRocketChargeVfx, transform, p, cell, 0, VfxOrder));
            Sound(RocketTntAudio, .45f);
            float age = 0;
            while (charge && age < .5f)
            {
                yield return null;
                age += Time.deltaTime;
                if (ParticleCount(charge) > 0) { yield return null; break; }
            }
            StopEmission(charge);
            Track(PlayVfx(TntRocketVfx, transform, p, cell, 0, VfxOrder));
            yield return Together(Rockets(p, true, 3), Rockets(p, false, 3));
        }

        // RoyalRocketSequence flight: constant speed to the screen edge, the imported trail at each half's fins.
        IEnumerator Rockets(Vector3 p, bool horizontal, int lanes)
        {
            Phase = lanes > 1 ? "Rocket lanes" : "Rocket flight";
            Sound(RocketAudio, .72f);
            Vector3 axis = horizontal ? Vector3.right : Vector3.up, across = horizontal ? Vector3.up : Vector3.right;
            var halves = new List<Transform>();
            var trails = new List<GameObject>();
            var starts = new List<Vector3>();
            var directions = new List<Vector3>();
            var nozzles = new List<float>();
            for (int lane = -lanes / 2; lane <= lanes / 2; lane++)
                for (int d = -1; d <= 1; d += 2)
                {
                    Vector3 start = p + across * lane * cell;
                    var half = RocketHalf(start, horizontal, d);
                    halves.Add(half ? half.transform : null);
                    nozzles.Add(half ? (horizontal ? half.bounds.extents.x : half.bounds.extents.y) : 0);
                    var trail = Track(PlayVfx(RocketTrailVfx, transform, start, cell, 0, VfxOrder));
                    if (trail)
                    {
                        var tail = -axis * d;
                        trail.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(tail.y, tail.x) * Mathf.Rad2Deg - 90);
                        // The imported trail keeps pace with the Rocket's 0.95x animation rate.
                        foreach (var ps in trail.GetComponentsInChildren<ParticleSystem>(true)) { var main = ps.main; main.simulationSpeed = .95f; }
                    }
                    trails.Add(trail);
                    starts.Add(start);
                    directions.Add(axis * d);
                }
            float exit = ExitCells(horizontal), distance = 0;
            while (distance < exit)
            {
                distance = Mathf.Min(exit, distance + RocketCellsPerSecond * Time.deltaTime);
                for (int i = 0; i < halves.Count; i++)
                {
                    Vector3 at = starts[i] + directions[i] * distance * cell;
                    if (halves[i]) halves[i].position = at;
                    if (trails[i]) trails[i].transform.position = at - directions[i] * nozzles[i];
                }
                yield return null;
            }
            foreach (var trail in trails) StopEmission(trail);
            foreach (var half in halves) if (half) half.gameObject.SetActive(false);
            foreach (var trail in trails) yield return WaitFor(trail);
        }

        // One flying half, drawn with the APK's directional part art at the piece's scale.
        SpriteRenderer RocketHalf(Vector3 at, bool horizontal, int direction)
        {
            string name = horizontal ? (direction < 0 ? RocketLeftSprite : RocketRightSprite) : (direction < 0 ? RocketBottomSprite : RocketTopSprite);
            var sprite = ReferenceBoosters.Sprite(name);
            if (!sprite) { Debug.LogError("[Reference Boosters] Missing rocket half " + name); return null; }
            var go = Track(new GameObject("Rocket half " + name));
            go.transform.SetParent(transform, false);
            go.transform.position = at;
            go.transform.localScale = Vector3.one * cell;
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite; r.sortingOrder = FlyerOrder;
            return r;
        }

        // ReferencePieceVisual.RocketBranches: one Rocket prefab, the orientation picks its branch.
        static void Branch(GameObject piece, bool horizontal)
        {
            if (!piece) return;
            var h = Find(piece.transform, RocketHorizontalBranch);
            var v = Find(piece.transform, RocketVerticalBranch);
            if (h) h.gameObject.SetActive(horizontal);
            if (v) v.gameObject.SetActive(!horizontal);
        }

        float ExitCells(bool horizontal)
        {
            var camera = Camera.main;
            float extent = camera ? (horizontal ? camera.orthographicSize * camera.aspect : camera.orthographicSize) : 8;
            return extent / cell + 1.5f;
        }

        // ── Propeller ───────────────────────────────────────────────────────────

        IEnumerator PropellerPiece(Vector3 p)
        {
            Phase = "Creation";
            var view = Track(SpawnVisual(PropellerKey, transform, p, cell, 0, BoosterOrder));
            float length = ReferenceTnt.Play(AnimatorOf(view), PropellerCreationState);
            Sound(PropellerCreationAudio, .72f);
            yield return new WaitForSeconds(length);
            Freeze(view, PropellerCreationState);
            Phase = "Idle piece";
            yield return new WaitForSeconds(idleHold);
            yield return PropellerFlight(view, p, Cell(-2, 4), Payload.None);
        }

        // A swapped pair: the Propeller takes off carrying the other booster.
        IEnumerator PropellerCombo(Vector3 p, Payload payload)
        {
            Phase = "Idle piece";
            var view = IdlePropeller(p);
            yield return new WaitForSeconds(idleHold * .5f);
            yield return PropellerFlight(view, p, Cell(2, 3), payload);
        }

        // Three Propellers leave 0.07s apart for three targets.
        IEnumerator PropellerPropeller(Vector3 p)
        {
            Phase = "Idle piece";
            var first = IdlePropeller(p);
            yield return new WaitForSeconds(idleHold * .5f);
            var targets = new[] { Cell(-2, 4), Cell(2, 3), Cell(1, -4) };
            for (int i = 0; i < targets.Length; i++)
            {
                Spawn(PropellerFlight(i == 0 ? first : IdlePropeller(p), p, targets[i], Payload.None));
                yield return new WaitForSeconds(.07f);
            }
            while (running > 0) yield return null;
        }

        // PropellerVisualV5.Take without a reveal: the creation clip's last frame, held.
        GameObject IdlePropeller(Vector3 p)
        {
            var view = Track(SpawnVisual(PropellerKey, transform, p, cell, 0, BoosterOrder));
            Freeze(view, PropellerCreationState);
            return view;
        }

        // PropellerVisualV5 Activation → Flight → Impact, on PropellerGameplayV5's cubic path.
        IEnumerator PropellerFlight(GameObject view, Vector3 from, Vector3 to, Payload payload)
        {
            if (!view) yield break;
            var animator = AnimatorOf(view);
            if (animator) animator.speed = 1;
            float takeoffLength = ReferenceTnt.Play(animator, PropellerTakeoffState), clock = 0;
            bool flying = false;
            var carried = Carried(payload, from);
            GameObject fuse = null;
            if (payload == Payload.Tnt)
            {
                fuse = Track(PlayVfx(PropellerTntFuseVfx, transform, from, cell, 0, VfxOrder));
                Sound(PropellerTntAudio, .72f);
            }

            Phase = "Propeller takeoff";
            bool lifted = false;
            float pairedAge = 0;
            while (clock < PropellerActivationSeconds)
            {
                clock += Time.deltaTime; pairedAge += Time.deltaTime;
                // The takeoff clip hands over to the looping fly clip when it ends.
                if (!flying && clock >= takeoffLength) { ReferenceTnt.Play(animator, PropellerFlyState); flying = true; }
                if (!lifted && clock >= PropellerTakeoffAt)
                {
                    lifted = true;
                    Track(PlayVfx(PropellerTakeoffVfx, transform, from, cell, 0, VfxOrder));
                    Sound(PropellerTakeoffAudio, .72f);
                }
                Carry(view, carried, fuse, pairedAge, 0);
                yield return null;
            }

            Phase = "Propeller flight";
            var flyVoice = Owned(view, PropellerFlyAudio, .45f);
            float cells = Vector2.Distance(from, to) / cell, duration = PropellerFlightDuration(cells), t = 0, lean = 0;
            Vector3 lift = from + Vector3.up * cell * .30f;
            Vector3 approach = Vector3.Lerp(from, to, .68f) + Vector3.up * cell * PropellerArc(cells) * .65f;
            while (t < duration)
            {
                t += Time.deltaTime; clock += Time.deltaTime; pairedAge += Time.deltaTime;
                if (!flying && clock >= takeoffLength) { ReferenceTnt.Play(animator, PropellerFlyState); flying = true; }
                float progress = Mathf.Clamp01(t / duration), k = PropellerEase(progress, duration), a = 1 - k;
                Vector3 at = a * a * a * from + 3 * a * a * k * lift + 3 * a * k * k * approach + k * k * k * to;
                Vector3 tangent = 3 * a * a * (lift - from) + 6 * a * k * (approach - lift) + 3 * k * k * (to - approach);
                lean = Mathf.Lerp(lean, -((Vector2)tangent).normalized.x * 10, 1 - Mathf.Exp(-Time.deltaTime * 17));
                view.transform.SetPositionAndRotation(at, Quaternion.Euler(0, 0, lean));
                Carry(view, carried, fuse, pairedAge, progress);
                yield return null;
            }

            Phase = "Propeller impact";
            if (flyVoice) flyVoice.Stop();
            view.transform.SetPositionAndRotation(to, Quaternion.identity);
            yield return new WaitForSeconds(.022f);
            view.SetActive(false);
            if (carried) carried.gameObject.SetActive(false);
            StopEmission(fuse);
            if (payload == Payload.Rocket) Track(PlayVfx(PropellerRocketVfx, transform, to, cell, 0, VfxOrder));
            else if (payload == Payload.None) Track(PlayVfx(PropellerImpactVfx, transform, to, cell, 0, VfxOrder));
            Sound(PropellerHitAudio, .72f);
            if (payload == Payload.Tnt) yield return TntBlast(to);
            else if (payload == Payload.Rocket) { yield return new WaitForSeconds(.07f); yield return Rockets(to, true, 1); }
            else yield return new WaitForSeconds(PropellerImpactSeconds + .4f);
        }

        SpriteRenderer Carried(Payload payload, Vector3 at)
        {
            if (payload == Payload.None) return null;
            var sprite = ReferenceBoosters.Sprite(payload == Payload.Tnt ? TntSprite : RocketSprite);
            if (!sprite) { Debug.LogError("[Reference Boosters] Missing carried sprite for " + payload); return null; }
            var go = Track(new GameObject("Carried " + payload));
            go.transform.SetParent(transform, false);
            go.transform.position = at;
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite; r.sortingOrder = BoosterOrder + 1;
            return r;
        }

        // PropellerVisualV5.UpdateCarried: the pair circles the flight anchor, swapping near and far.
        void Carry(GameObject view, SpriteRenderer carried, GameObject fuse, float pairedAge, float progress)
        {
            if (!carried) return;
            float spread = Mathf.SmoothStep(0, 1, pairedAge / .18f) * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.80f, 1, progress)));
            float angle = pairedAge * Mathf.PI * 2 / .72f, depth = Mathf.Sin(angle);
            carried.transform.position = view.transform.position + new Vector3(Mathf.Cos(angle) * .65f, depth * .16f, 0) * cell * spread;
            carried.transform.localScale = Vector3.one * cell * (1 + .07f * depth);
            carried.sortingOrder = depth >= 0 ? BoosterOrder + 1 : BoosterOrder - 1;
            view.transform.localScale = Vector3.one * cell * (1 - .07f * depth);
            if (!fuse) return;
            // The TNT artwork's rope tip: (176,166) in its 536x508 source, top-origin.
            var bounds = carried.sprite.bounds;
            var tip = new Vector3(bounds.min.x + bounds.size.x * (176f / 536f), bounds.min.y + bounds.size.y * (1 - 166f / 508f), 0);
            fuse.transform.position = carried.transform.TransformPoint(tip);
            var group = fuse.GetComponent<UnityEngine.Rendering.SortingGroup>();
            if (group) group.sortingOrder = carried.sortingOrder + 1;
        }

        // ── Color Ball ──────────────────────────────────────────────────────────

        // ColorBallVisualV4: reveal → idle → charge → a beam to each target → final pulse.
        IEnumerator ColorBallPiece(Vector3 p)
        {
            Phase = "Creation";
            var ball = Track(SpawnVisual(ColorBallKey, transform, p, cell, 0, BoosterOrder));
            var animator = AnimatorOf(ball);
            ReferenceTnt.Play(animator, ColorBallCreationState);
            Sound(ColorBallCreationAudio, .72f);
            yield return new WaitForSeconds(ColorBallRevealSeconds);
            ReferenceTnt.Play(animator, ColorBallDefaultState);
            Phase = "Idle piece";
            yield return new WaitForSeconds(idleHold);

            Phase = "Charge";
            ReferenceTnt.Play(animator, ColorBallUseState);
            // The beams and the use particles start from the animated ball (Animator/Main).
            Transform source = ball ? Find(ball.transform, "Main") : null;
            Vector3 origin = source ? source.position : p;
            var use = Track(PlayVfx(ColorBallUseVfx, transform, origin, cell, 0, VfxOrder));
            var start = Owned(ball, ColorBallStartAudio, .45f);
            yield return new WaitForSeconds(ColorBallChargeSeconds);

            Phase = "Beams";
            var targets = new[] { Cell(-3, 4), Cell(1, 4), Cell(3, 2), Cell(-2, 1), Cell(2, -1), Cell(-3, -2), Cell(0, -3), Cell(3, -4) };
            for (int i = 0; i < targets.Length; i++)
            {
                Spawn(Ray(source ? source.position : origin, targets[i], TargetColor));
                Sound(ColorBallRayAudio[i % ColorBallRayAudio.Length], .72f);
                yield return new WaitForSeconds(ColorBallBeamGap);
            }
            while (running > 0) yield return null;

            Phase = "Final pulse";
            yield return new WaitForSeconds(ColorBallFinalPulseAt);
            if (start) start.Stop();
            StopEmission(use);
            if (ball) ball.SetActive(false);
            Sound(ColorBallExplodeAudio, .72f);
            yield return WaitFor(Track(PlayVfx(ColorBallExplodeVfx, transform, origin, cell, 0, VfxOrder)));
        }

        // ColorBallVfxV4 native beam: the line's head travels to the target, then the whole line fades.
        IEnumerator Ray(Vector3 from, Vector3 to, int color)
        {
            var ray = Track(PlayVfx(ColorBallRayVfx, transform, from, cell, 0, RayOrder));
            if (!ray) yield break;
            var lines = ray.GetComponentsInChildren<LineRenderer>(true);
            var alphas = new GradientAlphaKey[lines.Length][];
            for (int i = 0; i < lines.Length; i++)
            {
                alphas[i] = lines[i].colorGradient.alphaKeys;
                lines[i].widthMultiplier *= cell;
                lines[i].positionCount = 2;
            }
            float duration = RayDuration(Vector3.Distance(from, to) / cell), age = 0;
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
                if (!hit && age >= duration) { hit = true; Track(PlayVfx(ColorBallContactVfx, transform, to, cell, 0, VfxOrder, color)); }
                yield return null;
            }
            ray.SetActive(false);
        }

        // ReferenceDualColorBallVisual: the two-ball clip with its charge, then the board-wide blast.
        IEnumerator ColorBallColorBall(Vector3 p)
        {
            Phase = "Combo anticipation";
            var body = Track(SpawnVisual(ColorBallComboKey, transform, p, cell, 0, ComboBodyOrder));
            float length = ReferenceTnt.Play(AnimatorOf(body), ColorBallComboState);
            Sound(ColorBallComboAudio, .45f);
            var charge = Track(PlayVfx(ColorBallComboChargeVfx, transform, p, cell, 0, VfxOrder));
            yield return new WaitForSeconds(length);
            StopEmission(charge);
            if (body) body.SetActive(false);
            Phase = "Combo explosion";
            yield return WaitFor(Track(PlayVfx(ColorBallComboExplodeVfx, transform, p, cell, 0, VfxOrder)));
        }

        // ── TNT (same as ReferenceTntPreview) ──────────────────────────────────

        IEnumerator SingleTnt(Vector3 p)
        {
            Phase = "Creation";
            var piece = Track(SpawnVisual(PieceKey, transform, p, cell, 0, PieceOrder));
            var animator = AnimatorOf(piece);
            float length = ReferenceTnt.Play(animator, CreationState);
            Sound(CreationAudio, .72f);
            yield return new WaitForSeconds(length);
            Idle(animator);
            Phase = "Idle piece";
            yield return new WaitForSeconds(idleHold);
            if (piece) piece.SetActive(false);
            yield return TntBlast(p);
        }

        // RoyalPowerupDirector.Explosion: the 0.12s fuse, then the blast.
        IEnumerator TntBlast(Vector3 p)
        {
            Phase = "Ignition";
            var fuse = Track(PlayVfx(FuseVfx, transform, p, cell, 0, VfxOrder));
            yield return new WaitForSeconds(IgnitionDuration);
            StopEmission(fuse);
            Phase = "Explosion";
            Sound(ExplodeAudio, .72f);
            yield return WaitFor(Track(PlayVfx(ExplodeVfx, transform, p, cell, 0, VfxOrder)));
        }

        IEnumerator TntTnt(Vector3 p)
        {
            Phase = "Combo anticipation";
            var body = Track(SpawnVisual(ComboKey, transform, p, cell, 0, ComboBodyOrder));
            float length = ReferenceTnt.Play(AnimatorOf(body), ComboState);
            Sound(ComboAudio, .45f);
            Transform socket = body ? Find(body.transform, ComboFuseSocket) : null;
            var fuse = Track(PlayVfx(ComboFuseVfx, transform, socket ? socket.position : p, cell, 0, ComboFuseOrder));
            float t = 0;
            while (t < length)
            {
                if (fuse && socket) fuse.transform.SetPositionAndRotation(socket.position, socket.rotation);
                yield return null;
                t += Time.deltaTime;
            }
            StopEmission(fuse);
            if (body) body.SetActive(false);
            Phase = "Combo explosion";
            yield return WaitFor(Track(PlayVfx(ComboExplodeVfx, transform, p, cell, 0, VfxOrder)));
        }

        // ── Match and finish ────────────────────────────────────────────────────

        // A swap accent, then CandyShatterService's per-gem explosion in the gems' colour.
        IEnumerator Match(Vector3 p)
        {
            Phase = "Swap";
            Track(PlayVfx(SwapVfx, transform, p + Vector3.right * cell * .5f, cell, 0, VfxOrder));
            yield return new WaitForSeconds(.15f);
            Phase = "Match 3";
            int color = Plays % 5;
            GameObject last = null;
            for (int i = -1; i <= 1; i++) last = Track(PlayVfx(MatchVfx, transform, p + Vector3.right * i * cell, cell, 0, VfxOrder, color));
            Sound(MatchAudio[Plays % MatchAudio.Length], .72f);
            yield return WaitFor(last);
        }

        // ReferenceWinFinish: each move left flies from the counter, lands, becomes a Rocket; then they fire.
        IEnumerator MovesToRockets(Vector3 p)
        {
            Phase = "Moves fly in";
            var camera = Camera.main;
            Vector3 counter = p + Vector3.up * ((camera ? camera.orthographicSize : 8) - 1.2f);
            var cells = new[] { Cell(-2, 2), Cell(1, -1), Cell(2, 3) };
            var pieces = new GameObject[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                int index = i;
                Spawn(MoveFlight(counter, cells[i], index % 2 == 0, piece => pieces[index] = piece));
                yield return new WaitForSeconds(.12f);
            }
            while (running > 0) yield return null;
            Phase = "Rockets wait";
            yield return new WaitForSeconds(.75f);
            Phase = "Rockets fire";
            for (int i = 0; i < cells.Length; i++)
            {
                if (pieces[i]) pieces[i].SetActive(false);
                Spawn(Rockets(cells[i], i % 2 == 0, 1));
                yield return new WaitForSeconds(.18f);
            }
            while (running > 0) yield return null;
        }

        IEnumerator MoveFlight(Vector3 from, Vector3 to, bool horizontal, System.Action<GameObject> landed)
        {
            var flight = Track(PlayVfx(MovesFlightVfx, transform, from, cell, 0, VfxOrder));
            float progress = 0;
            while (progress < 1)
            {
                progress = Mathf.Min(1, progress + Time.deltaTime / .45f);
                if (flight) flight.transform.position = Vector3.Lerp(from, to, progress) + Vector3.right * Mathf.Sin(progress * Mathf.PI) * .5f;
                yield return null;
            }
            StopEmission(flight);
            Track(PlayVfx(MovesHitVfx, transform, to, cell, 0, VfxOrder));
            var piece = RevealRocket(to, horizontal);
            landed(piece);
            yield return new WaitForSeconds(StateLength(piece, horizontal ? RocketHorizontalState : RocketVerticalState));
            Freeze(piece, horizontal ? RocketHorizontalState : RocketVerticalState);
        }

        // RoyalWinConfetti: the star hit burst over the looping idle sparkle.
        IEnumerator WinStars(Vector3 p)
        {
            Phase = "Win stars";
            var hit = Track(PlayVfx(StarHitVfx, transform, p, WinStarScale, 0, VfxOrder));
            var idle = Track(PlayVfx(StarIdleVfx, transform, p, WinStarScale, 0, VfxOrder));
            yield return new WaitForSeconds(3);
            StopEmission(idle);
            yield return WaitFor(hit);
            yield return WaitFor(idle);
        }

        // ── Plumbing ───────────────────────────────────────────────────────────

        static Animator AnimatorOf(GameObject go) => go ? go.GetComponentInChildren<Animator>(true) : null;

        static float StateLength(GameObject piece, string state)
        {
            var animator = AnimatorOf(piece);
            if (animator && animator.runtimeAnimatorController)
                foreach (var clip in animator.runtimeAnimatorController.animationClips) if (clip.name == state) return clip.length;
            return 0;
        }

        // ReferencePieceVisual.SetIdle: a creation clip's last frame, frozen.
        static void Freeze(GameObject piece, string state)
        {
            var animator = AnimatorOf(piece);
            if (!animator) return;
            ReferenceTnt.Play(animator, state, 1);
            animator.speed = 0;
        }

        static int ParticleCount(GameObject vfx)
        {
            int n = 0;
            if (vfx) foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>()) n += ps.particleCount;
            return n;
        }

        GameObject Track(GameObject go)
        {
            if (go) spawned.Add(go);
            return go;
        }

        // Runs a branch alongside the caller; callers wait for running to reach zero.
        void Spawn(IEnumerator routine) => StartCoroutine(Counted(routine));

        IEnumerator Counted(IEnumerator routine)
        {
            running++;
            try { yield return routine; }
            finally { running = Mathf.Max(0, running - 1); }
        }

        IEnumerator Together(IEnumerator a, IEnumerator b)
        {
            Spawn(a);
            Spawn(b);
            yield return null;
            while (running > 0) yield return null;
        }

        static IEnumerator WaitFor(GameObject vfx)
        {
            yield return null;
            while (Alive(vfx)) yield return null;
        }

        void Sound(string clipName, float gain)
        {
            var clip = sound ? Clip(clipName) : null;
            if (clip) voice.PlayOneShot(clip, gain);
        }

        // A sustained cue that belongs to one piece and stops with it (ReferenceActivationVisual.PlayOwned).
        AudioSource Owned(GameObject owner, string clipName, float gain)
        {
            var clip = sound ? Clip(clipName) : null;
            if (!clip || !owner) return null;
            var source = owner.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0; source.clip = clip; source.volume = gain;
            source.Play();
            return source;
        }

        void Cleanup()
        {
            foreach (var go in spawned) if (go) Destroy(go);
            spawned.Clear();
        }

        void OnGUI()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Space) Restart();
                else if (e.keyCode == KeyCode.RightArrow) Select((Sequence)(((int)sequence + 1) % SequenceCount));
                else if (e.keyCode == KeyCode.LeftArrow) Select((Sequence)(((int)sequence + SequenceCount - 1) % SequenceCount));
            }
            GUILayout.BeginArea(new Rect(12, 12, 280, Mathf.Min(Screen.height - 24, 640)), GUI.skin.box);
            GUILayout.Label("Royal Throne (original) — " + Label(sequence));
            GUILayout.Label("Phase: " + Phase);
            scroll = GUILayout.BeginScrollView(scroll);
            for (int i = 0; i < SequenceCount; i++)
            {
                var s = (Sequence)i;
                if (GUILayout.Toggle(sequence == s, Labels[i], GUI.skin.button) && sequence != s) Select(s);
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Space  Replay    ←/→  Previous/next")) Restart();
            loop = GUILayout.Toggle(loop, "Loop");
            playAll = GUILayout.Toggle(playAll, "Play all in turn");
            sound = GUILayout.Toggle(sound, "Sound");
            GUILayout.Label("Speed " + speed.ToString("0.00") + "x");
            speed = GUILayout.HorizontalSlider(speed, .1f, 2);
            GUILayout.EndArea();
        }
    }
}
