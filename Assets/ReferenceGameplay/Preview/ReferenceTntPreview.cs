using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RoyalThrone.ReferenceGameplay.Preview.ReferenceTnt;

namespace RoyalThrone.ReferenceGameplay.Preview
{
    // Standalone player for the TNT imported from royal-throne. It reproduces the game's
    // presentation (RoyalPowerupDirector.Explosion / NativeTntComboAnticipation,
    // ReferencePieceVisual, ReferenceVfxPool) without LevelManager or the board:
    // same prefabs, clips, sockets, sorting orders, gains and the 0.12s ignition.
    public sealed class ReferenceTntPreview : MonoBehaviour
    {
        public enum Sequence { SingleTnt, TntTntCombo }

        // The piece sits under the VFX (35).
        const int PieceOrder = 30;

        public Sequence sequence = Sequence.SingleTnt;
        public bool loop = true;
        [Tooltip("World size of one board cell; the game scales every TNT asset by this.")]
        public float cell = 1.4f;
        [Tooltip("Seconds the finished TNT piece sits idle before it ignites.")]
        public float idleHold = .8f;
        [Tooltip("Pause between loops.")]
        public float gap = .7f;
        [Range(.1f, 2)] public float speed = 1;
        public bool sound = true;

        public string Phase { get; private set; } = "Idle";
        public int Plays { get; private set; }

        readonly List<GameObject> spawned = new List<GameObject>();
        AudioSource voice;
        Coroutine running;

        void OnEnable()
        {
            if (!voice) { voice = gameObject.AddComponent<AudioSource>(); voice.playOnAwake = false; voice.spatialBlend = 0; }
            Restart();
        }

        void OnDisable() { Cleanup(); running = null; Time.timeScale = 1; }

        void Update() => Time.timeScale = speed;

        public void Restart()
        {
            if (running != null) StopCoroutine(running);
            Cleanup();
            running = StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            do
            {
                yield return sequence == Sequence.SingleTnt ? SingleTnt() : TntTntCombo();
                Plays++;
                Phase = "Idle";
                yield return new WaitForSeconds(gap);
                Cleanup();
            } while (loop);
            running = null;
        }

        // Piece reveal → idle → 0.12s fuse → blast, as for an ordinary TNT on the board.
        IEnumerator SingleTnt()
        {
            Vector3 p = transform.position;
            Phase = "Creation";
            var piece = Track(SpawnVisual(PieceKey, transform, p, cell, 0, PieceOrder));
            var animator = piece ? piece.GetComponentInChildren<Animator>(true) : null;
            float length = Play(animator, CreationState);
            Sound(CreationAudio, .72f);
            yield return new WaitForSeconds(length);
            Idle(animator);
            Phase = "Idle piece";
            yield return new WaitForSeconds(idleHold);

            Phase = "Ignition";
            var fuse = Track(PlayVfx(FuseVfx, transform, p, cell, 0, VfxOrder));
            yield return new WaitForSeconds(IgnitionDuration);
            StopEmission(fuse);
            if (piece) piece.SetActive(false);

            Phase = "Explosion";
            var blast = Track(PlayVfx(ExplodeVfx, transform, p, cell, 0, VfxOrder));
            Sound(ExplodeAudio, .72f);
            yield return WaitFor(blast);
        }

        // The dedicated TNT+TNT body/roof/fuse clip, fuse riding its socket, then the big blast.
        IEnumerator TntTntCombo()
        {
            Vector3 p = transform.position;
            Phase = "Combo anticipation";
            var body = Track(SpawnVisual(ComboKey, transform, p, cell, 0, ComboBodyOrder));
            var animator = body ? body.GetComponentInChildren<Animator>(true) : null;
            float length = Play(animator, ComboState);
            Sound(ComboAudio, .45f);
            Transform socket = body ? Find(body.transform, ComboFuseSocket) : null;
            var fuse = Track(PlayVfx(ComboFuseVfx, transform, socket ? socket.position : p, cell, 0, ComboFuseOrder));
            float t = 0;
            while (t < length)
            {
                // ReferenceVfxPool.Handle.Follow: sampled after the Animator moved the socket.
                if (fuse && socket) fuse.transform.SetPositionAndRotation(socket.position, socket.rotation);
                yield return null;
                t += Time.deltaTime;
            }
            StopEmission(fuse);
            if (body) body.SetActive(false);

            Phase = "Combo explosion";
            yield return WaitFor(Track(PlayVfx(ComboExplodeVfx, transform, p, cell, 0, VfxOrder)));
        }

        GameObject Track(GameObject go)
        {
            if (go) spawned.Add(go);
            return go;
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
                if (e.keyCode == KeyCode.Alpha1) { sequence = Sequence.SingleTnt; Restart(); }
                else if (e.keyCode == KeyCode.Alpha2) { sequence = Sequence.TntTntCombo; Restart(); }
                else if (e.keyCode == KeyCode.Space) Restart();
            }
            GUILayout.BeginArea(new Rect(12, 12, 260, 220), GUI.skin.box);
            GUILayout.Label("Royal Throne TNT (original) — " + Phase);
            if (GUILayout.Button("1  Single TNT")) { sequence = Sequence.SingleTnt; Restart(); }
            if (GUILayout.Button("2  TNT + TNT combo")) { sequence = Sequence.TntTntCombo; Restart(); }
            if (GUILayout.Button("Space  Replay")) Restart();
            loop = GUILayout.Toggle(loop, "Loop");
            sound = GUILayout.Toggle(sound, "Sound");
            GUILayout.Label("Speed " + speed.ToString("0.00") + "x");
            speed = GUILayout.HorizontalSlider(speed, .1f, 2);
            GUILayout.EndArea();
        }
    }
}
