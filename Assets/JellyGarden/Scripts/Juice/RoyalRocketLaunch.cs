using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    public sealed partial class RoyalPowerupDirector
    {
        // Launch hand-off for a board Rocket. The activated Item disappears in its activation frame, so a
        // pooled stand-in continues its last Idle pose through the existing anticipation window:
        // compress, stretch along the travel axis, punch, then the directional Passing sprites take over.
        // Travel, speed and FireExhaust are not involved here.
        sealed class RocketLaunchStandIn
        {
            public GameObject go; public SpriteRenderer body, charge;
            public Vector3 position, scale; public Quaternion rotation, upright;
            public float highlight; public bool used, axisX;
        }
        RocketLaunchStandIn[] rocketStandIns;
        public const float RocketLaunchCompressEnd = .40f, RocketLaunchStretchEnd = .85f;
        public int RocketLaunchStandInsActive { get { int n = 0; if (rocketStandIns != null) foreach (var s in rocketStandIns) if (s.used) n++; return n; } }

        void BuildRocketLaunchStandIns()
        {
            rocketStandIns = new RocketLaunchStandIn[6];
            for (int i = 0; i < rocketStandIns.Length; i++)
            {
                var go = new GameObject("Rocket launch stand-in " + i); go.transform.SetParent(rocketVfxRoot, false);
                var body = go.AddComponent<SpriteRenderer>(); body.sortingOrder = 36;
                var glow = new GameObject("Launch charge"); glow.transform.SetParent(go.transform, false);
                var charge = glow.AddComponent<SpriteRenderer>(); charge.sharedMaterial = RoyalRocketPieceAnimator.OverlayMaterial(); charge.sortingOrder = 37;
                go.SetActive(false);
                rocketStandIns[i] = new RocketLaunchStandIn { go = go, body = body, charge = charge };
            }
        }

        /// <summary>Quick light and spark pop under a Rocket that Match-4 has just created.</summary>
        public void RocketRevealPop(Vector3 position)
        {
            Emit(rocketGlow, position, 1, 1.05f, 0, .16f, new Color(1, .97f, .82f, .9f));
            Emit(rocketStars, position, 9, .16f, 2.2f, .30f, new Color(1, .93f, .66f, 1));
        }

        // Only the activated piece at the origin, on the matching axis, hands its pose over. Combos,
        // helicopter payloads and previews keep the flyers' own compression.
        RocketLaunchStandIn TakeRocketLaunchStandIn(Square origin, bool horizontal)
        {
            var piece = RoyalRocketPieceAnimator.At(origin);
            if (rocketStandIns == null || !piece || !piece.Destroying || piece.Horizontal != horizontal) return null;
            var source = piece.Body;
            if (!source || !source.sprite) return null;
            foreach (var s in rocketStandIns)
            {
                if (s.used) continue;
                var t = source.transform;
                s.used = true; s.axisX = piece.ArtAxisIsX;
                s.position = t.position + Vector3.back * .6f; s.rotation = t.rotation; s.scale = t.lossyScale;
                s.upright = t.rotation * Quaternion.Euler(0, 0, -piece.Turn);
                s.highlight = piece.Highlight;
                s.body.sprite = s.charge.sprite = source.sprite; s.body.color = source.color;
                s.body.sortingLayerID = s.charge.sortingLayerID = source.sortingLayerID;
                s.go.transform.SetPositionAndRotation(s.position, s.rotation); s.go.transform.localScale = s.scale;
                s.go.SetActive(true);
                return s;
            }
            return null;
        }

        void PoseRocketLaunchStandIn(RocketLaunchStandIn s, float k)
        {
            float axis, cross;
            if (k < RocketLaunchCompressEnd)
            {
                float u = k / RocketLaunchCompressEnd, e = 1 - (1 - u) * (1 - u);
                axis = Mathf.Lerp(1, .84f, e); cross = Mathf.Lerp(1, 1.10f, e);
            }
            else if (k < RocketLaunchStretchEnd)
            {
                float u = (k - RocketLaunchCompressEnd) / (RocketLaunchStretchEnd - RocketLaunchCompressEnd), e = u * u * (3 - 2 * u);
                axis = Mathf.Lerp(.84f, 1.24f, e); cross = Mathf.Lerp(1.10f, .88f, e);
            }
            else
            {
                float u = Mathf.Clamp01((k - RocketLaunchStretchEnd) / (1 - RocketLaunchStretchEnd)), e = 1 - (1 - u) * (1 - u);
                axis = Mathf.Lerp(1.24f, 1.36f, e); cross = Mathf.Lerp(.88f, .82f, e);
            }
            var scale = s.scale;
            s.go.transform.localScale = s.axisX ? new Vector3(scale.x * axis, scale.y * cross, scale.z) : new Vector3(scale.x * cross, scale.y * axis, scale.z);
            // The Idle rocking straightens during compression so the stretch points exactly along travel.
            s.go.transform.rotation = Quaternion.Slerp(s.rotation, s.upright, Mathf.Clamp01(k / RocketLaunchCompressEnd));
            float glow = Mathf.Lerp(s.highlight, .62f, k * k);
            s.charge.color = new Color(1, .97f, .86f, glow);
            s.charge.enabled = glow > .001f;
        }

        void ReleaseRocketLaunchStandIn(RocketLaunchStandIn s)
        {
            if (s == null) return;
            s.used = false; s.charge.enabled = false; s.body.sprite = s.charge.sprite = null;
            s.go.SetActive(false);
        }

        void ClearRocketLaunchStandIns()
        {
            if (rocketStandIns != null) foreach (var s in rocketStandIns) if (s.used) ReleaseRocketLaunchStandIn(s);
        }
    }
}
