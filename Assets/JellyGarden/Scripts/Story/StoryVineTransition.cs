// Native version of the approved Area 1 preview. Full paintings remain
// non-readable; leaf positions are measured offline from their pixel diff.
using System;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryVineTransition : MonoBehaviour
    {
        // Preview time starts at .62: the click has already happened in the game.
        public const float Duration = 4.55f;
        public const float RevealAt = 3.35f;
        private Image _before;
        private Material _material;
        private StoryVineParticles _particles;

        public static string DataPath(int areaId, int taskIndex)
        {
            return StoryCampaign.ResourceRoot + string.Format("/Transitions/area_{0:000}_task{1}_vines", areaId, taskIndex);
        }

        public static StoryVineTransition Attach(RectTransform parent, Sprite before, Sprite after,
            int areaId, int taskIndex)
        {
            var shader = Resources.Load<Shader>("Story/UI/VineDissolve");
            var data = Resources.Load<TextAsset>(DataPath(areaId, taskIndex));
            if (parent == null || before == null || after == null || shader == null || !shader.isSupported || data == null)
                return null;
            var anchors = JsonUtility.FromJson<VineParticleData>(data.text);
            if (anchors == null || anchors.particles == null || anchors.particles.Length == 0) return null;

            RectTransform root = StoryUi.Stretch(StoryUi.CreateRect("VineTransition", parent));
            // The live room already covers. On a standalone stage, fit the exact
            // same painting; particles and mask then share its coordinate system.
            var fitter = root.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectRatio = before.rect.width / before.rect.height;
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            var transition = root.gameObject.AddComponent<StoryVineTransition>();
            var clean = StoryUi.CreateImage("CleanStone", root, after, Color.white);
            StoryUi.Stretch(clean.rectTransform); clean.raycastTarget = false;
            transition._before = StoryUi.CreateImage("DissolvingVines", root, before, Color.white);
            StoryUi.Stretch(transition._before.rectTransform); transition._before.raycastTarget = false;
            transition._material = new Material(shader) { name = "Vine clearing (instance)" };
            transition._material.SetVector("_SpriteUV", DataUtility.GetOuterUV(before));
            transition._before.material = transition._material;
            RectTransform particles = StoryUi.Stretch(StoryUi.CreateRect("FallingLeaves", root));
            transition._particles = particles.gameObject.AddComponent<StoryVineParticles>();
            transition._particles.raycastTarget = false;
            transition._particles.Particles = anchors.particles;
            transition.SetTime(0f);
            return transition;
        }

        public void SetTime(float elapsed)
        {
            float previewTime = elapsed + .62f;
            float progress = (previewTime - 1.02f) / 2.55f;
            _material.SetFloat("_Progress", progress);
            // A stencil mask can give the UI a derived material instance.
            _before.materialForRendering.SetFloat("_Progress", progress);
            _particles.Seconds = previewTime;
            _particles.SetVerticesDirty();
        }

        private void OnDestroy()
        {
            if (_material == null) return;
            if (_before != null) _before.material = null;
            if (Application.isPlaying) Destroy(_material); else DestroyImmediate(_material);
        }
    }

    [Serializable] internal sealed class VineParticleData { public VineParticle[] particles; }
    [Serializable] internal sealed class VineParticle
    {
        public float x, y, start, life, size, rotation, spin;
        public int seed, side;
        public bool bright, sparkle;
    }

    // One UI mesh for all leaves and glints. No per-leaf GameObjects, texture
    // readbacks, or allocations on the animation's frame loop.
    internal sealed class StoryVineParticles : MaskableGraphic
    {
        internal VineParticle[] Particles;
        internal float Seconds;
        private static readonly Vector2[] Glints = {
            new Vector2(.50f,.289f), new Vector2(.283f,.371f), new Vector2(.71f,.398f),
            new Vector2(.226f,.535f), new Vector2(.743f,.56f)
        };
        private static float Smooth(float p) { return Mathf.SmoothStep(0f, 1f, p); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Particles == null) return;
            foreach (VineParticle p in Particles)
            {
                float age = Seconds - p.start;
                if (p.sparkle)
                {
                    if (age > 0f && age < .85f)
                        Glint(vh, new Vector2(p.x + Mathf.Sin(age * 3f + p.seed) * 6f, p.y - age * 21f),
                            p.size, Mathf.Sin(age / .85f * Mathf.PI) * .75f);
                    continue;
                }
                if (age <= 0f || age >= p.life) continue;
                float a = age / p.life;
                float alpha = Smooth(age / .1f) * (1f - Smooth((a - .56f) / .44f));
                float drift = p.side * (35f * age + 22f * age * age) + Mathf.Sin(age * 5f + p.seed) * 10f * age;
                Vector2 center = new Vector2(p.x + drift, p.y - 28f * age + 70f * age * age);
                float scale = (.75f + .25f * Mathf.Sin(Mathf.Min(age / .16f, 1f) * Mathf.PI / 2f)) * (1f - .45f * Smooth(a));
                float flip = .65f + .35f * Mathf.Cos(p.seed + age * 7f);
                float angle = p.rotation + p.spin * age;
                Color light = p.bright ? new Color(.78f,.84f,.39f,alpha) : new Color(.61f,.67f,.24f,alpha);
                Color middle = p.bright ? new Color(.51f,.64f,.25f,alpha) : new Color(.33f,.53f,.20f,alpha);
                Color dark = new Color(.20f,.36f,.20f,alpha);
                int first = vh.currentVertCount;
                Vertex(vh, center, middle);
                const int segments = 16;
                for (int k = 0; k <= segments; k++)
                {
                    float t = (k % segments) / (float)segments * 2f;
                    Vector2 local = t < 1f
                        ? Bezier(new Vector2(-1,0),new Vector2(-.38f,-.9f),new Vector2(.63f,-.6f),new Vector2(1,0),t)
                        : Bezier(new Vector2(1,0),new Vector2(.25f,.9f),new Vector2(-.7f,.55f),new Vector2(-1,0),t-1f);
                    Color tint = local.y < 0 ? Color.Lerp(middle,light,-local.y) : Color.Lerp(middle,dark,local.y);
                    Vertex(vh, center + Rotate(new Vector2(local.x * flip,local.y) * (p.size * scale), angle), tint);
                    if (k > 0) vh.AddTriangle(first, first+k, first+k+1);
                }
                // A fine golden vein gives a leaf shape even as it turns edge-on.
                int vein = vh.currentVertCount;
                Color veinColor = new Color(.82f,.81f,.47f,alpha*.35f);
                Vertex(vh, center + Rotate(new Vector2(-.7f*p.size*flip,-.45f)*scale,angle),veinColor);
                Vertex(vh, center + Rotate(new Vector2(.8f*p.size*flip,0)*scale,angle),veinColor);
                Vertex(vh, center + Rotate(new Vector2(-.7f*p.size*flip,.45f)*scale,angle),veinColor);
                vh.AddTriangle(vein,vein+1,vein+2);
            }
            for (int i=0; i<Glints.Length; i++)
            {
                float a=(Seconds-3.68f-i*.1f)/.8f;
                if (a>0f && a<1f) Glint(vh,Vector2.Scale(Glints[i],new Vector2(720,1280)),6f,Mathf.Sin(a*Mathf.PI)*.85f);
            }
        }

        private void Glint(VertexHelper vh, Vector2 point, float radius, float alpha)
        {
            int first = vh.currentVertCount;
            Vertex(vh,point,new Color(1f,.94f,.73f,alpha*.45f));
            for(int i=0;i<=12;i++)
            {
                float a=i*Mathf.PI/6f;
                Vertex(vh,point+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*3f,new Color(1f,.94f,.73f,0f));
                if(i>0)vh.AddTriangle(first,first+i,first+i+1);
            }
            first=vh.currentVertCount;
            Color color=new Color(1f,.96f,.79f,alpha);
            Vertex(vh,point,color);
            for(int i=0;i<=8;i++)
            {
                float a=i*Mathf.PI/4f;
                float r=(i%2==0?1f:.16f)*radius;
                Vertex(vh,point+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,color);
                if(i>0)vh.AddTriangle(first,first+i,first+i+1);
            }
        }

        private void Vertex(VertexHelper vh, Vector2 point, Color color)
        {
            Rect r=rectTransform.rect;
            vh.AddVert(new Vector3(r.xMin+point.x/720f*r.width,r.yMax-point.y/1280f*r.height,0),color,Vector2.zero);
        }
        private static Vector2 Rotate(Vector2 p,float a)
        {
            float c=Mathf.Cos(a),s=Mathf.Sin(a);return new Vector2(p.x*c-p.y*s,p.x*s+p.y*c);
        }
        private static Vector2 Bezier(Vector2 a,Vector2 b,Vector2 c,Vector2 d,float t)
        {
            float u=1f-t;return a*(u*u*u)+b*(3f*u*u*t)+c*(3f*u*t*t)+d*(t*t*t);
        }
    }
}
