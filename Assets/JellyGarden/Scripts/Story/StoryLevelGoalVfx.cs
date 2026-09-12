using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    /// <summary>A soft halo and two glints, with unscaled idle motion for score stars.</summary>
    public sealed class StoryLevelGoalVfx : MaskableGraphic
    {
        private RectTransform _star;
        private Vector2 _rest;
        private float _began, _elapsed;

        public void SetStar(RectTransform star)
        {
            ResetStar();
            _star=star;
            if(star!=null) _rest=star.anchoredPosition;
            _began=StoryClock.Now;
            _elapsed=0;
            SetVerticesDirty();
        }
        private void Update()
        {
            if(_star==null) return;
            _elapsed=StoryClock.Now-_began;
            float wave=Mathf.Sin(_elapsed*Mathf.PI*2f/3.6f);
            _star.anchoredPosition=_rest+new Vector2(0,wave*2f);
            _star.localRotation=Quaternion.Euler(0,0,wave*4f);
            _star.localScale=Vector3.one*(1f+wave*.025f);
            SetVerticesDirty();
        }
        private void ResetStar()
        {
            if(_star==null) return;
            _star.anchoredPosition=_rest;
            _star.localRotation=Quaternion.identity;
            _star.localScale=Vector3.one;
        }
        protected override void OnDisable() { ResetStar();base.OnDisable(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if(_star==null) return;
            Rect rect=rectTransform.rect;
            Vector2 center=rect.center;
            int start=mesh.currentVertCount;
            Add(mesh,center,new Color(1f,.82f,.35f,.22f));
            const int segments=48;
            for(int i=0;i<=segments;i++)
            {
                float a=i*Mathf.PI*2/segments;
                Add(mesh,center+new Vector2(Mathf.Cos(a)*rect.width*.5f,Mathf.Sin(a)*rect.height*.48f),
                    new Color(1f,.85f,.45f,0));
                if(i>0) mesh.AddTriangle(start,start+i,start+i+1);
            }
            Glint(mesh,center+new Vector2(rect.width*.35f,rect.height*.28f),5.5f,0);
            Glint(mesh,center+new Vector2(-rect.width*.35f,-rect.height*.28f),3.6f,1.7f);
        }
        private void Glint(VertexHelper mesh,Vector2 center,float size,float phase)
        {
            float light=.7f+Mathf.Sin(_elapsed*1.75f+phase)*.25f;
            Color tint=new Color(.84f,.65f,.27f,light);
            int start=mesh.currentVertCount;
            Add(mesh,center,tint);
            for(int i=0;i<=8;i++)
            {
                float a=i*Mathf.PI/4;
                float radius=size*(i%2==0?1f:.22f)*(.85f+light*.15f);
                Add(mesh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,tint);
                if(i>0) mesh.AddTriangle(start,start+i,start+i+1);
            }
        }
        private void Add(VertexHelper mesh,Vector2 p,Color tint) => mesh.AddVert(p,tint*color,Vector2.zero);
    }
}
