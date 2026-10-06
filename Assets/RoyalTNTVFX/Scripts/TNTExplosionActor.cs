using UnityEngine;

namespace RoyalTNTVFX
{
    /// <summary>
    /// One pooled TNT explosion. A fixed set of quads (five blast layers, 28 star sparkles and
    /// 25 cell-impact sparks) animated through material property blocks on the
    /// RoyalTNTVFX/TNTGlow shader. It uses no ParticleSystems, so there is nothing to leak:
    /// when the explosion ends every renderer is disabled and the object deactivates.
    ///
    /// You normally never touch this directly - <see cref="TNTExplosionPlayer"/> owns the pool.
    /// </summary>
    [AddComponentMenu("Royal TNT VFX/TNT Explosion Actor")]
    [DisallowMultipleComponent]
    public sealed class TNTExplosionActor : MonoBehaviour
    {
        public enum VisualState { Idle, Activated, Ignition, Explosion, Debris, Complete }

        /// <summary>Squash-and-glow time before the blast, in seconds.</summary>
        public const float IgnitionDuration = .12f;
        /// <summary>Blast length from Explode() to cleanup, in seconds.</summary>
        public const float ExplosionDuration = .86f;

        [Tooltip("WhiteHotCore, GoldBlastGlow, RadialRays, Shockwave, SmokeGlow - in that order.")]
        public Renderer[] blast;
        [Tooltip("Four-point star sparkles thrown out by the blast.")]
        public Renderer[] stars;
        [Tooltip("Per-cell spark flashes, recycled in order when a blast covers more cells.")]
        public Renderer[] impacts;

        public VisualState State { get; private set; }
        public float VisualAge => age;
        public float Strength => strength;

        internal TNTExplosionPlayer Owner { get; set; }

        MaterialPropertyBlock props;
        float[] impactBorn;
        int impactCursor;
        float age, strength = 1;

        static readonly Color Gold = new Color(1, .68f, .075f);
        static readonly Color Warm = new Color(1, .97f, .78f);

        /// <summary>Hide everything and return to a clean, reusable state.</summary>
        public void ResetVisualState()
        {
            props = props ?? new MaterialPropertyBlock();
            strength = 1;
            age = 0;
            transform.rotation = Quaternion.identity;
            for (int i = 0; i < blast.Length; i++)
            {
                var r = blast[i];
                r.enabled = false;
                r.transform.localPosition = Vector3.back * i * .01f;
                r.transform.localRotation = Quaternion.identity;
            }
            foreach (var r in stars) r.enabled = false;
            State = VisualState.Idle;
            impactCursor = 0;
            if (impacts != null) foreach (var r in impacts) r.enabled = false;
        }

        /// <summary>Start the ignition glow. Call Explode() once the anticipation is over.</summary>
        public void Activate()
        {
            ResetVisualState();
            State = VisualState.Activated;
        }

        /// <summary>Open the blast. 1 is a standard TNT; the game uses 1.45 for large blasts.</summary>
        public void Explode(float intensity = 1)
        {
            age = 0;
            strength = intensity;
            State = VisualState.Explosion;
        }

        /// <summary>Flash a local spark burst on one cell of the blast area.</summary>
        public void CellImpact(Vector3 position)
        {
            if (impacts == null || impacts.Length == 0) return;
            if (impactBorn == null || impactBorn.Length != impacts.Length) impactBorn = new float[impacts.Length];
            int i = impactCursor++ % impacts.Length;
            impacts[i].transform.position = position + Vector3.back * 1.3f;
            impactBorn[i] = age;
            impacts[i].enabled = true;
        }

        /// <summary>Immediately hide and deactivate, returning this actor to the pool.</summary>
        public void Cleanup()
        {
            ResetVisualState();
            State = VisualState.Complete;
            gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (Owner != null && Owner.Paused) return;
            age += Time.deltaTime;

            if (impacts != null && impactBorn != null)
                for (int i = 0; i < impacts.Length; i++)
                    if (impacts[i].enabled)
                    {
                        float t = age - impactBorn[i];
                        Layer(impacts[i], 1.22f + t * .65f, Warm, t < .60f ? 1.45f : 0, 7, t, i * .731f);
                    }

            if (State == VisualState.Activated || State == VisualState.Ignition)
            {
                float k = Mathf.Clamp01(age / IgnitionDuration);
                Layer(blast[0], .18f + .40f * k, Warm, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.65f, 1, k)) * 1.5f);
                if (age >= .08f) State = VisualState.Ignition;
            }
            else if (State == VisualState.Explosion || State == VisualState.Debris)
            {
                float t = age;
                if (t > .25f) State = VisualState.Debris;
                // The compact flash opens into textured rays while the broken outer wave is
                // still expanding. Cell sparkles overlap the tail of the blast.
                float expand = Mathf.SmoothStep(0, 1, t / .15f);
                float ballFade = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.075f, .19f, t));
                float rayFade = Mathf.SmoothStep(0, 1, t / .09f) * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.20f, .40f, t)));
                float waveFade = Mathf.SmoothStep(0, 1, t / .10f) * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.19f, .36f, t)));
                Layer(blast[0], Mathf.Lerp(.30f, 4.55f, expand) * strength, new Color(1, .98f, .86f), ballFade * 1.45f, 5);
                Layer(blast[1], Mathf.Lerp(.55f, 5.9f, expand) * strength, new Color(1, .76f, .025f), rayFade * .62f, 8);
                Layer(blast[2], Mathf.Lerp(1.1f, 6.6f, Mathf.Clamp01(t / .24f)) * strength, new Color(1, .82f, .25f), rayFade * 1.3f, 4);
                Layer(blast[3], Mathf.Lerp(.45f, 6.3f, Mathf.Clamp01(t / .23f)) * strength, new Color(1, .90f, .58f), waveFade * 1.1f, 1);
                Layer(blast[4], Mathf.Lerp(2.3f, 5.7f, Mathf.Clamp01(t / .35f)) * strength, Warm, Mathf.Sin(Mathf.Clamp01(t / .66f) * Mathf.PI) * .68f, 8);
                Sparkles(t, .72f, 3.0f * strength);
                if (t >= ExplosionDuration)
                {
                    Cleanup();
                    if (Owner != null) Owner.NotifyActorFinished(this);
                }
            }
        }

        void Layer(Renderer r, float size, Color color, float opacity, float mode = -1, float localTime = -1, float seed = 0)
        {
            r.enabled = opacity > .001f;
            if (!r.enabled) return;
            r.transform.localScale = Vector3.one * size;
            props.Clear();
            color.a = opacity;
            props.SetColor("_Color", color);
            props.SetFloat("_FxTime", localTime >= 0 ? localTime : age);
            props.SetFloat("_Seed", seed);
            if (mode >= 0) props.SetFloat("_Mode", mode);
            r.SetPropertyBlock(props);
        }

        void Sparkles(float t, float life, float radius)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                float delay = (i % 5) * .028f;
                float u = (t - delay) / life;
                var r = stars[i];
                r.enabled = u > 0 && u < 1;
                if (!r.enabled) continue;
                float angle = i * 2.39996f;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                r.transform.localPosition = dir * radius * (.04f + .11f * (i % 5) + u * .32f) + Vector3.up * -u * u * .8f + Vector3.back * .7f;
                r.transform.localRotation = Quaternion.Euler(0, 0, i * 47 + t * (i % 2 == 0 ? 70 : -90));
                float size = (i % 6 == 0 ? .25f : .09f + (i % 3) * .035f) * (1 - u * .6f) * 1.55f;
                Layer(r, size, i % 3 == 0 ? Gold : Warm, Mathf.Sin(u * Mathf.PI) * (1.1f + .6f * Mathf.Sin(t * 37 + i * 2)));
            }
        }
    }
}
