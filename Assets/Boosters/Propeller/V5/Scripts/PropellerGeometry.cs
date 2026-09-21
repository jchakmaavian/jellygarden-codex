using System.Collections.Generic;
using UnityEngine;

namespace RoyalThrone.Boosters.Propeller.V5
{
    // Small native meshes derived from the supplied blue/gold/red propeller silhouette.
    // No dependence on old propeller layers, animations, shared explosion materials or textures.
    public static class PropellerGeometry
    {
        public static Mesh Sphere(int rings=14,int sides=28)
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int y=0;y<=rings;y++)for(int x=0;x<=sides;x++){
                float p=Mathf.PI*y/rings,a=Mathf.PI*2*x/sides;
                v.Add(new Vector3(Mathf.Sin(p)*Mathf.Cos(a),Mathf.Cos(p),Mathf.Sin(p)*Mathf.Sin(a)));
            }
            Grid(t,rings,sides);return Finish("Propeller sphere",v,t);
        }
        public static Mesh Band()
        {
            var v=new List<Vector3>();var t=new List<int>();const int sides=40,rings=8;
            for(int y=0;y<=rings;y++)for(int x=0;x<=sides;x++){
                float b=y*Mathf.PI*2/rings,a=x*Mathf.PI*2/sides;
                float r=.279f+.024f*Mathf.Cos(b);
                v.Add(new Vector3(r*Mathf.Cos(a),.041f*Mathf.Sin(b),r*Mathf.Sin(a)));
            }
            Grid(t,rings,sides);t.Reverse();return Finish("Gold belt",v,t);
        }
        public static Mesh Blade(float begin,float end)
        {
            var v=new List<Vector3>();var t=new List<int>();const int steps=20,across=10;
            // Domed top and rolled edges catch a moving highlight; neither surface is a flat card.
            for(int z=0;z<2;z++)for(int i=0;i<=steps;i++)for(int j=0;j<=across;j++){
                float u=Mathf.Lerp(begin,end,(float)i/steps),x=.058f+u*.39f;
                float w=.029f+.142f*Mathf.Sin(Mathf.Pow(u,.70f)*Mathf.PI*.91f);
                if(u>.92f)w*=Mathf.Sqrt(Mathf.Max(.008f,(1-u)/.08f));
                float a=(float)j/across*2-1,arch=Mathf.Sqrt(Mathf.Max(0,1-a*a));
                float depth=(z==0?-1:1)*(.010f+.035f*arch)*(1-.30f*u);
                v.Add(new Vector3(x,w*a,depth));
            }
            int offset=(steps+1)*(across+1);
            for(int i=0;i<steps;i++)for(int j=0;j<across;j++){
                int a=i*(across+1)+j,b=a+across+1;
                Tri(t,a,a+1,b);Tri(t,a+1,b+1,b);
                Tri(t,a+offset,b+offset,a+1+offset);Tri(t,a+1+offset,b+offset,b+1+offset);
            }
            for(int i=0;i<steps;i++)foreach(int j in new[]{0,across}){
                int a=i*(across+1)+j,b=a+across+1;
                if(j==0){Tri(t,a,b,a+offset);Tri(t,b,b+offset,a+offset);}
                else {Tri(t,a,a+offset,b);Tri(t,b,a+offset,b+offset);}
            }
            for(int j=0;j<across;j++)foreach(int i in new[]{0,steps}){
                int a=i*(across+1)+j;
                if(i==0){Tri(t,a,a+offset,a+1);Tri(t,a+1,a+offset,a+1+offset);}
                else {Tri(t,a,a+1,a+offset);Tri(t,a+1,a+1+offset,a+offset);}
            }
            return Finish("Rounded paddle",v,t);
        }
        public static Mesh Quad()
        {
            var m=new Mesh{name="Air star quad"};m.vertices=new[]{new Vector3(-.5f,-.5f),new Vector3(-.5f,.5f),new Vector3(.5f,.5f),new Vector3(.5f,-.5f)};
            m.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};m.triangles=new[]{0,1,2,0,2,3};m.RecalculateNormals();return m;
        }
        static void Grid(List<int> t,int rows,int cols){for(int r=0;r<rows;r++)for(int c=0;c<cols;c++){int a=r*(cols+1)+c,b=a+cols+1;Tri(t,a,a+1,b);Tri(t,a+1,b+1,b);}}
        static void Tri(List<int> t,int a,int b,int c){t.Add(a);t.Add(b);t.Add(c);}
        static Mesh Finish(string name,List<Vector3> v,List<int> t){var m=new Mesh{name=name};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
    }
}
