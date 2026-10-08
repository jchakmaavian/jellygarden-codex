using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalThrone.ReferenceGameplay.Preview
{
    // Standalone player for the TNT imported from royal-throne. It reproduces the game's
    // presentation (RoyalPowerupDirector.Explosion / NativeTntComboAnticipation,
    // ReferencePieceVisual, ReferenceVfxPool) without LevelManager or the board:
    // same prefabs, clips, sockets, sorting orders, gains and the 0.12s ignition.
    public sealed class ReferenceTntPreview : MonoBehaviour
    {
        public enum Sequence { SingleTnt, TntTntCombo }

        public const string PieceKey = "TNT", ComboKey = "TNTTNTComboRfv387";
        public const string CreationState = "TntCreationAnimation", ComboState = "TntTntComboExplodeAnimation";
        public const string FuseVfx = "TntItemFuseParticles", ExplodeVfx = "TntItemExplodeParticles";
        public const string ComboFuseVfx = "TntTntComboFuseParticles", ComboExplodeVfx = "TntTntComboExplodeParticles";
        public const string CreationAudio = "tnt_creation__15882", ExplodeAudio = "tnt_explode__15838", ComboAudio = "tnt_tnt_combo__rfv387_17741";
        public const string ComboFuseSocket = "FitilPartikul";
        // TNT3DAnimator.IgnitionDuration on royal-throne.
        public const float IgnitionDuration = .12f;
        // Sorting orders the game uses: piece under VFX (35), combo body 36, its fuse 37.
        const int PieceOrder = 30, VfxOrder = 35, ComboBodyOrder = 36, ComboFuseOrder = 37;

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
            var piece = Spawn("ReferenceGameplay/Visuals/" + PieceKey, p, Vector3.one * cell, PieceOrder);
            var animator = piece ? piece.GetComponentInChildren<Animator>(true) : null;
            float length = Play(animator, CreationState);
            Sound(CreationAudio, .72f);
            yield return new WaitForSeconds(length);
            // ReferencePieceVisual.SetIdle: the creation clip's last frame, frozen.
            if (animator) { Play(animator, CreationState, 1); animator.speed = 0; }
            Phase = "Idle piece";
            yield return new WaitForSeconds(idleHold);

            Phase = "Ignition";
            var fuse = Vfx(FuseVfx, p, VfxOrder);
            yield return new WaitForSeconds(IgnitionDuration);
            StopEmission(fuse);
            if (piece) piece.SetActive(false);

            Phase = "Explosion";
            var blast = Vfx(ExplodeVfx, p, VfxOrder);
            Sound(ExplodeAudio, .72f);
            yield return WaitFor(blast);
        }

        // The dedicated TNT+TNT body/roof/fuse clip, fuse riding its socket, then the big blast.
        IEnumerator TntTntCombo()
        {
            Vector3 p = transform.position;
            Phase = "Combo anticipation";
            var body = Spawn("ReferenceGameplay/Visuals/" + ComboKey, p, Vector3.one * cell, ComboBodyOrder);
            var animator = body ? body.GetComponentInChildren<Animator>(true) : null;
            float length = Play(animator, ComboState);
            Sound(ComboAudio, .45f);
            Transform socket = body ? Find(body.transform, ComboFuseSocket) : null;
            var fuse = Vfx(ComboFuseVfx, socket ? socket.position : p, ComboFuseOrder);
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
            yield return WaitFor(Vfx(ComboExplodeVfx, p, VfxOrder));
        }

        GameObject Spawn(string resource, Vector3 position, Vector3 scale, int order)
        {
            var prefab = Resources.Load<GameObject>(resource);
            if (!prefab) { Debug.LogError("[TNT preview] Missing resource " + resource); return null; }
            var go = Instantiate(prefab, position, Quaternion.identity, transform);
            go.transform.localScale = scale;
            var sorting = go.GetComponent<SortingGroup>();
            if (!sorting) sorting = go.AddComponent<SortingGroup>();
            sorting.sortingLayerID = 0; sorting.sortingOrder = order;
            spawned.Add(go);
            return go;
        }

        // ReferenceVfxPool.Play with no colour: prefab scale × cell, sorting group, blast_part_* off.
        GameObject Vfx(string key, Vector3 position, int order)
        {
            var prefab = Resources.Load<GameObject>("ReferenceGameplay/VFX/" + key);
            var go = Spawn("ReferenceGameplay/VFX/" + key, position, (prefab ? prefab.transform.localScale : Vector3.one) * cell, order);
            if (!go) return null;
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (ps.name.StartsWith("blast_part_")) ps.gameObject.SetActive(false);
                if (ps.gameObject.activeInHierarchy) ps.Play(false);
            }
            return go;
        }

        static void StopEmission(GameObject vfx)
        {
            if (vfx) foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>()) ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        static IEnumerator WaitFor(GameObject vfx)
        {
            yield return null;
            while (vfx)
            {
                bool alive = false;
                foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>()) if (ps.IsAlive(false)) { alive = true; break; }
                if (!alive) yield break;
                yield return null;
            }
        }

        static float Play(Animator animator, string state, float normalizedTime = 0)
        {
            if (!animator || !animator.runtimeAnimatorController) return 0;
            animator.enabled = true; animator.speed = 1;
            animator.Play(state, 0, normalizedTime); animator.Update(0);
            foreach (var clip in animator.runtimeAnimatorController.animationClips) if (clip.name == state) return clip.length;
            Debug.LogWarning("[TNT preview] Clip unavailable: " + state);
            return 0;
        }

        static Transform Find(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
            return null;
        }

        void Sound(string clipName, float gain)
        {
            if (!sound) return;
            var clip = Resources.Load<AudioClip>("ReferenceGameplay/Audio/" + clipName);
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
