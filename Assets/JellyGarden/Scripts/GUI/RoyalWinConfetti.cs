using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // Renders the APK's native ParticleSystems into the UI; no reconstructed particle motion.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalWinConfetti : MaskableGraphic
    {
        public Vector2 origin;
        public int VisibleSparkles { get; private set; }
        GameObject stage;
        Camera effectCamera;
        RenderTexture target;
        Material composite;
        ParticleSystem[] systems;
        float previous = -1;
        bool previousCalm;
        public override Texture mainTexture => target ? target : Texture2D.blackTexture;

        bool Build()
        {
            if (stage) return true;
            var hit = Resources.Load<GameObject>("ReferenceGameplay/VFX/StarHitParticles");
            var idle = Resources.Load<GameObject>("ReferenceGameplay/VFX/StarIdleParticles");
            var shader = Resources.Load<Shader>("RoyalGameplay/UIAdditive");
            if (!hit || !idle || !shader) return false;
            stage = new GameObject("Native win VFX render stage") { hideFlags = HideFlags.HideAndDontSave };
            stage.transform.position = new Vector3(20000, 20000, 0);
            foreach (var prefab in new[] { hit, idle })
            {
                var instance = Instantiate(prefab, stage.transform, false);
                instance.transform.localPosition = Vector3.zero;
                foreach (var tr in instance.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = 31;
            }
            systems = stage.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                // Presentation tuning on instances only: retain the recovered source prefabs.
                var main = ps.main;
                if (ps.name == "star_softlight")
                    main.startSizeMultiplier *= 1.5f;
                else if (ps.name == "star_softglow")
                    main.startSizeMultiplier *= 1.35f;
                else if (ps.name == "light_beam (1)")
                    main.startSizeMultiplier *= 1.12f;
                else if (ps.name == "_stars (1)" || ps.name == "spark(add_spark_hard)")
                {
                    main.startSizeMultiplier *= 1.12f;
                    var emission = ps.emission;
                    emission.rateOverTimeMultiplier *= 1.2f;
                }
                ps.useAutoRandomSeed = false;
                ps.randomSeed = (uint)(17 + System.Array.IndexOf(systems, ps));
            }
            var cameraObject = new GameObject("Native win VFX camera");
            cameraObject.transform.SetParent(stage.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 0, -10);
            effectCamera = cameraObject.AddComponent<Camera>();
            effectCamera.enabled = false;
            effectCamera.orthographic = true;
            effectCamera.orthographicSize = 4;
            effectCamera.nearClipPlane = .1f;
            effectCamera.farClipPlane = 20;
            effectCamera.cullingMask = 1 << 31;
            effectCamera.clearFlags = CameraClearFlags.SolidColor;
            effectCamera.backgroundColor = Color.black;
            effectCamera.allowHDR = false;
            effectCamera.allowMSAA = false;
            target = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32) { name = "Native win particles" };
            target.Create();
            effectCamera.targetTexture = target;
            composite = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            material = composite;
            raycastTarget = false;
            SetAllDirty();
            return true;
        }

        public void Pose(float seconds, bool reduced)
        {
            if (!Build()) return;
            var card = GetComponentInParent<RoyalWinCard>();
            if (card)
            {
                // The native glow and sparkles replace the earlier procedural layers.
                if (card.sparkles) card.sparkles.gameObject.SetActive(false);
                if (card.haloSparkles) card.haloSparkles.gameObject.SetActive(false);
                if (card.rays) card.rays.gameObject.SetActive(false);
                if (card.glow) card.glow.enabled = false;
                if (card.flash) card.flash.enabled = false;
                if (card.pillowGlow) card.pillowGlow.enabled = false;
                if (card.star && transform.GetSiblingIndex() > card.star.GetSiblingIndex())
                    transform.SetSiblingIndex(card.star.GetSiblingIndex());
            }
            float time = reduced ? .8f : Mathf.Max(0, seconds - .2f);
            bool restart = previous < 0 || time < previous || previousCalm != reduced;
            if (!restart && Mathf.Approximately(previous, time)) return;
            VisibleSparkles = 0;
            foreach (var ps in systems)
            {
                if (!ps.gameObject.activeInHierarchy) continue;
                ps.Simulate(restart ? time : time - previous, false, restart, true);
                ps.Pause(false);
                VisibleSparkles += ps.particleCount;
            }
            previous = time;
            previousCalm = reduced;
            effectCamera.Render();
            color = new Color(1, 1, 1, Mathf.Clamp01(seconds / .2f));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            const float half = 275;
            vh.AddVert(origin + new Vector2(-half, -half), color, new Vector2(0, 0));
            vh.AddVert(origin + new Vector2(-half, half), color, new Vector2(0, 1));
            vh.AddVert(origin + new Vector2(half, half), color, new Vector2(1, 1));
            vh.AddVert(origin + new Vector2(half, -half), color, new Vector2(1, 0));
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(0, 2, 3);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            previous = -1;
            if (stage) stage.SetActive(false);
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            if (stage) stage.SetActive(true);
            previous = -1;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (stage) Destroy(stage);
            if (target) { target.Release(); Destroy(target); }
            if (composite) Destroy(composite);
        }
    }
}
