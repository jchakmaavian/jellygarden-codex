using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    /// <summary>The cut-corner enamel frame from Soft Royal concept 02, drawn as UI geometry.</summary>
    public sealed class StorySapphireFrame : MaskableGraphic
    {
        private static readonly Color32 Edge = new Color32(119, 81, 37, 255);
        private static readonly Color32 Navy = new Color32(36, 78, 103, 255);
        private static readonly Color32 Light = new Color32(255, 240, 176, 255);
        private static readonly float[] Stops = { 0f, .12f, .3f, .49f, .67f, .85f, 1f };
        private static readonly Color32[] Gold = {
            new Color32(161,107,41,255), new Color32(255,236,171,255),
            new Color32(211,164,81,255), new Color32(245,213,139,255),
            new Color32(181,130,54,255), new Color32(235,198,117,255), new Color32(152,97,39,255)
        };

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Vector2[] rim = CutRect(8, 8, 316, 528, 27);
            Stroke(mesh, rim, 24, Edge);
            Stroke(mesh, rim, 22, null);
            Stroke(mesh, rim, 17, Navy);
            Stroke(mesh, rim, 1, new Color32(75,119,144,255));
            Stroke(mesh, CutRect(21,21,303,515,17), 1.2f, Light);
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2[] clasp = Mirror(new[] { V(3,37), V(37,3), V(45,10), V(10,45) }, corner);
                Fill(mesh, clasp, null);
                Stroke(mesh, clasp, 1, Edge);
                Line(mesh, Mirror(V(10,36),corner), Mirror(V(36,10),corner), 1.3f, Light);
                Vector2 center = Mirror(V(22,22),corner);
                Fill(mesh, Diamond(center,6,6), null);
                Stroke(mesh, Diamond(center,6,6), .6f, Edge);
                Fill(mesh, Diamond(center,4,5), new Color32(91,145,164,255));
                Stroke(mesh, Diamond(center,4,5), 1, Light);
            }
            Line(mesh,V(136,528),V(188,528),1.5f,Light);
            Fill(mesh,Diamond(V(162,528),5,5),null);
            Stroke(mesh,Diamond(V(162,528),5,5),.6f,Edge);
        }

        private static Vector2 V(float x,float y) => new Vector2(x,y);
        private static Vector2[] CutRect(float l,float t,float r,float b,float cut) => new[] {
            V(l+cut,t),V(r-cut,t),V(r,t+cut),V(r,b-cut),V(r-cut,b),V(l+cut,b),V(l,b-cut),V(l,t+cut)
        };
        private static Vector2[] Diamond(Vector2 p,float x,float y) => new[] {
            p+V(0,-y),p+V(x,0),p+V(0,y),p+V(-x,0)
        };
        private static Vector2 Mirror(Vector2 p,int corner) =>
            V((corner&1)==0?p.x:324-p.x,(corner&2)==0?p.y:536-p.y);
        private static Vector2[] Mirror(Vector2[] points,int corner)
        {
            for(int i=0;i<points.Length;i++) points[i]=Mirror(points[i],corner);
            return points;
        }
        private static Color Metal(Vector2 p)
        {
            float t=Mathf.Clamp01((p.x/324f*.8f+p.y/536f)/1.64f);
            for(int i=1;i<Stops.Length;i++)
                if(t<=Stops[i]) return Color.Lerp(Gold[i-1],Gold[i],Mathf.InverseLerp(Stops[i-1],Stops[i],t));
            return Gold[Gold.Length-1];
        }
        private void Vertex(VertexHelper mesh,Vector2 p,Color? tint)
        {
            Rect r=rectTransform.rect;
            mesh.AddVert(new Vector3(r.xMin+p.x/324f*r.width,r.yMax-p.y/536f*r.height),
                (tint??Metal(p))*color,Vector2.zero);
        }
        private void Fill(VertexHelper mesh,Vector2[] points,Color? tint)
        {
            Vector2 center=Vector2.zero;
            foreach(Vector2 p in points) center+=p;
            center/=points.Length;
            int start=mesh.currentVertCount;
            Vertex(mesh,center,tint);
            foreach(Vector2 p in points) Vertex(mesh,p,tint);
            for(int i=0;i<points.Length;i++) mesh.AddTriangle(start,start+i+1,start+(i+1)%points.Length+1);
        }
        private void Stroke(VertexHelper mesh,Vector2[] points,float width,Color? tint)
        {
            int start=mesh.currentVertCount;
            for(int i=0;i<points.Length;i++)
            {
                Vector2 before=(points[i]-points[(i+points.Length-1)%points.Length]).normalized;
                Vector2 after=(points[(i+1)%points.Length]-points[i]).normalized;
                Vector2 n1=V(-before.y,before.x),n2=V(-after.y,after.x);
                Vector2 join=(n1+n2).normalized;
                Vector2 offset=join*(width*.5f/Mathf.Max(.1f,Vector2.Dot(join,n1)));
                Vertex(mesh,points[i]+offset,tint);
                Vertex(mesh,points[i]-offset,tint);
            }
            for(int i=0;i<points.Length;i++)
            {
                int a=start+i*2,b=start+(i+1)%points.Length*2;
                mesh.AddTriangle(a,b,b+1);mesh.AddTriangle(a,b+1,a+1);
            }
        }
        private void Line(VertexHelper mesh,Vector2 a,Vector2 b,float width,Color tint)
        {
            Vector2 d=(b-a).normalized,n=V(-d.y,d.x)*width*.5f;
            Fill(mesh,new[]{a-n,b-n,b+n,a+n},tint);
        }
    }
}
