using System.Collections.Generic;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;

namespace RoyalThrone.Presentation
{
    // One cached mesh per frame texture, rebuilt only when the board changes.
    // This decorates the occupied silhouette without changing any square or collider.
    public sealed class RoyalBoardSurface : MonoBehaviour
    {
        sealed class Layer
        {
            public Mesh mesh; public Material material;
            public MeshRenderer renderer; public int order;
            public readonly List<Vector3> vertices=new List<Vector3>();
            public readonly List<Vector2> uv=new List<Vector2>();
            public readonly List<int> triangles=new List<int>();
        }
        readonly Dictionary<string,Layer> layers=new Dictionary<string,Layer>();
        readonly HashSet<Vector2Int> occupied=new HashSet<Vector2Int>();
        Square[] squares;
        Texture2D white;
        public int EdgeCount { get; private set; }
        public int RenderLayerCount => layers.Count;

        Layer GetLayer(string key,Sprite sprite,int order,Color color)=>TextureLayer(key,sprite?sprite.texture:null,order,color);
        Layer TextureLayer(string key,Texture texture,int order,Color color)
        {
            if(!layers.TryGetValue(key,out var layer))
            {
                layer=new Layer {mesh=new Mesh {name="Board "+key},order=order};
                layer.material=new Material(Shader.Find("Sprites/Default")){name="Board "+key,mainTexture=texture?texture:white,color=color};
                layers.Add(key,layer);
            }
            EnsureRenderer(key,layer);return layer;
        }
        void EnsureRenderer(string key,Layer layer)
        {
            if(layer.renderer)return;
            // LevelManager destroys every GameField child when leaving a level.
            // This component and its owned mesh/material cache survive on GameField,
            // so recreate only the missing child renderer for the next board.
            var go=new GameObject("Board surface - "+key);go.transform.SetParent(transform,false);
            var filter=go.AddComponent<MeshFilter>();filter.sharedMesh=layer.mesh;
            layer.renderer=go.AddComponent<MeshRenderer>();layer.renderer.sortingOrder=layer.order;
            layer.renderer.sharedMaterial=layer.material;
        }
        bool RenderersIntact()
        {
            if(layers.Count==0)return false;
            foreach(var layer in layers.Values)if(!layer.renderer)return false;
            return true;
        }
        void Quad(Layer layer,Vector3 centre,Vector2 size,Sprite sprite,float angle=0)
        {
            Rect uv=new Rect(0,0,1,1);
            if(sprite){var r=sprite.textureRect;uv=new Rect(r.x/sprite.texture.width,r.y/sprite.texture.height,r.width/sprite.texture.width,r.height/sprite.texture.height);}
            Quad(layer,centre,size,uv,angle);
        }
        void Quad(Layer layer,Vector3 centre,Vector2 size,Rect uv,float angle=0)
        {
            int first=layer.vertices.Count;float x=size.x*.5f,y=size.y*.5f;
            layer.vertices.Add(centre+new Vector3(-x,-y));layer.vertices.Add(centre+new Vector3(x,-y));
            layer.vertices.Add(centre+new Vector3(x,y));layer.vertices.Add(centre+new Vector3(-x,y));
            if(angle!=0){var rotation=Quaternion.Euler(0,0,angle);for(int i=first;i<first+4;i++)layer.vertices[i]=centre+rotation*(layer.vertices[i]-centre);}
            layer.uv.Add(new Vector2(uv.xMin,uv.yMin));layer.uv.Add(new Vector2(uv.xMax,uv.yMin));
            layer.uv.Add(new Vector2(uv.xMax,uv.yMax));layer.uv.Add(new Vector2(uv.xMin,uv.yMax));
            layer.triangles.Add(first);layer.triangles.Add(first+1);layer.triangles.Add(first+2);
            layer.triangles.Add(first);layer.triangles.Add(first+2);layer.triangles.Add(first+3);
        }
        // ---- The gold frame (2026-10-04): one continuous stroke round the board's silhouette ---------------------------
        // The stroke lies outside the cells, `t` wide. Its texture is the cross-section from the cells (v 0) outwards (v 1):
        // a soft blue seam, a light gold highlight, polished gold, a deeper orange lip and a thin dark rim, each edge faded
        // over a few per cent so it reads smooth without MSAA. Corners that wrap the cells are rounded (the inner edge hugs
        // a cell corner of radius `round`, the outer edge `round + t`); corners that turn into the board are mitred, so the
        // stroke always meets the cells with no gap.
        const int StrokeTexels=96, ArcSteps=12;
        static Texture2D strokeTexture;
        static Color StrokeProfile(float v)
        {
            Color c;
            if(v<.1f)c=Color.Lerp(new Color(.17f,.29f,.62f),new Color(.98f,.86f,.45f),Mathf.SmoothStep(0,1,v/.1f));
            else if(v<.3f)c=Color.Lerp(new Color(1f,.95f,.69f),new Color(1f,.86f,.42f),(v-.1f)/.2f);
            else if(v<.66f)c=Color.Lerp(new Color(1f,.82f,.29f),new Color(.95f,.68f,.17f),(v-.3f)/.36f);
            else if(v<.84f)c=Color.Lerp(new Color(.91f,.6f,.13f),new Color(.8f,.47f,.1f),(v-.66f)/.18f);
            else c=Color.Lerp(new Color(.55f,.3f,.07f),new Color(.1f,.16f,.38f),(v-.84f)/.16f);
            c.a=Mathf.SmoothStep(0,1,Mathf.Clamp01(v/.05f))*Mathf.SmoothStep(0,1,Mathf.Clamp01((1-v)/.07f));
            return c;
        }
        public static Texture2D StrokeTexture()
        {
            if(strokeTexture)return strokeTexture;
            strokeTexture=new Texture2D(4,StrokeTexels,TextureFormat.RGBA32,false){name="Royal board frame stroke",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear,hideFlags=HideFlags.DontSave};
            var pixels=new Color[4*StrokeTexels];
            for(int y=0;y<StrokeTexels;y++){var c=StrokeProfile((y+.5f)/StrokeTexels);for(int x=0;x<4;x++)pixels[y*4+x]=c;}
            strokeTexture.SetPixels(pixels);strokeTexture.Apply(false,true);return strokeTexture;
        }
        // One loop of the silhouette (grid points, cells on the left of every edge) as a closed strip of inner/outer pairs.
        void Stroke(Layer layer,List<Vector2Int> loop,System.Func<Vector2,Vector3> local,float t,float round)
        {
            var corners=new List<Vector2Int>();
            for(int i=0;i<loop.Count;i++){
                var previous=loop[(i+loop.Count-1)%loop.Count];var here=loop[i];var next=loop[(i+1)%loop.Count];
                if(here-previous!=next-here)corners.Add(here);   // only the turns
            }
            if(corners.Count<4)return;
            var inner=new List<Vector3>();var outer=new List<Vector3>();
            for(int i=0;i<corners.Count;i++){
                var a=corners[(i+corners.Count-1)%corners.Count];var p=corners[i];var b=corners[(i+1)%corners.Count];
                Vector2 dIn=((Vector2)(p-a)).normalized,dOut=((Vector2)(b-p)).normalized;
                Vector2 nIn=new Vector2(dIn.y,-dIn.x),nOut=new Vector2(dOut.y,-dOut.x);   // outward: the right of each edge
                Vector3 P=local(p);Vector3 NIn=local(p+nIn)-P,NOut=local(p+nOut)-P;            // one cell along each normal
                NIn=NIn.normalized;NOut=NOut.normalized;
                if(dIn.x*dOut.y-dIn.y*dOut.x>0){   // a left turn wraps the cells: a rounded corner
                    var centre=P-(NIn+NOut)*round;
                    float from=Mathf.Atan2(NIn.y,NIn.x),to=Mathf.Atan2(NOut.y,NOut.x);
                    float sweep=Mathf.DeltaAngle(from*Mathf.Rad2Deg,to*Mathf.Rad2Deg)*Mathf.Deg2Rad;
                    for(int s=0;s<=ArcSteps;s++){
                        float angle=from+sweep*s/ArcSteps;var r=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle));
                        inner.Add(centre+r*round);outer.Add(centre+r*(round+t));
                    }
                }else{inner.Add(P);outer.Add(P+(NIn+NOut)*t);}   // a right turn goes into the board: mitred
            }
            int first=layer.vertices.Count,n=inner.Count;float along=0;
            for(int i=0;i<n;i++){
                if(i>0)along+=Vector3.Distance(inner[i],inner[i-1]);
                layer.vertices.Add(inner[i]);layer.vertices.Add(outer[i]);
                layer.uv.Add(new Vector2(along,0));layer.uv.Add(new Vector2(along,1));
            }
            for(int i=0;i<n;i++){
                int j=(i+1)%n;int a0=first+2*i,a1=a0+1,b0=first+2*j,b1=b0+1;
                layer.triangles.Add(a0);layer.triangles.Add(b0);layer.triangles.Add(b1);
                layer.triangles.Add(a0);layer.triangles.Add(b1);layer.triangles.Add(a1);
            }
        }
        public void RebuildIfNeeded(LevelManager lm)
        {
            if(lm.squaresArray==null||!RoyalGameplayArt.Art("cell-light"))return;
            if(squares==lm.squaresArray&&RenderersIntact())return;
            squares=lm.squaresArray;EdgeCount=0;occupied.Clear();
            if(!white){white=new Texture2D(1,1);white.SetPixel(0,0,Color.white);white.Apply();}
            foreach(var entry in layers){var layer=entry.Value;EnsureRenderer(entry.Key,layer);layer.vertices.Clear();layer.uv.Clear();layer.triangles.Clear();layer.mesh.Clear();}
            foreach(var square in squares)if(square&&square.type!=SquareTypes.NONE)occupied.Add(new Vector2Int(square.col,square.row));
            // The frame is a fine stroke, 8% of a cell, outside the cells (RoyalGameplayPresentation leaves the board room for
            // a tenth).
            float w=lm.squareWidth,h=lm.squareHeight,thickness=Mathf.Min(w,h)*.08f;
            // The cell art is a rounded square, so four neighbouring corners leave a small concave gap. The grout under the
            // cells is the cells' own lavender (between the light and dark cell), so those gaps read as one continuous
            // surface. The drop shadow lies under the grout (2026-10-04): above it, it showed through every four-cell gap and
            // along the cells' soft edges as navy dots and lines; now it only reads outside the board's silhouette.
            var cell=RoyalGameplayArt.Art("cell-light");
            var grout=GetLayer("grout",null,-3,new Color(.57f,.65f,.89f,1f));
            var shadow=GetLayer("shadow",cell,-4,new Color(.015f,.04f,.12f,.75f));
            foreach(var square in squares)
            {
                if(!square)continue;
                // Existing serialized outlines stay in place for the original board generator.
                foreach(Transform child in square.transform)if(child.name.ToLowerInvariant().Contains("outline"))
                {var old=child.GetComponent<SpriteRenderer>();if(old)old.enabled=false;}
                if(square.type==SquareTypes.NONE)continue;
                var p=transform.InverseTransformPoint(square.transform.position);p.z=-.04f;
                int col0=square.col,row0=square.row;
                Quad(grout,p+new Vector3(0,0,.12f),new Vector2(w*1.02f,h*1.02f),null);
                // Only the board's bottom-right silhouette can ever show a drop shadow. Drawing it
                // under interior cells put a dark dot in every corner gap instead.
                if(!occupied.Contains(new Vector2Int(col0,row0+1))||!occupied.Contains(new Vector2Int(col0+1,row0))||!occupied.Contains(new Vector2Int(col0+1,row0+1)))
                    Quad(shadow,p+new Vector3(.025f*w,-.065f*h,.1f),new Vector2(w*1.04f,h*1.04f),cell);
            }
            // The frame: the silhouette's loops (outline and holes) in grid points, y up, mapped through one square's place.
            Square anchor=null;foreach(var square in squares)if(square&&square.type!=SquareTypes.NONE){anchor=square;break;}
            if(anchor)
            {
                var grid=new List<Vector2Int>();foreach(var c in occupied)grid.Add(new Vector2Int(c.x,-c.y));
                var shape=JellyGarden.Scripts.Juice.RoyalHintContour.Build(grid);EdgeCount=shape.BoundaryEdges;
                var origin=transform.InverseTransformPoint(anchor.transform.position);origin.z=-.04f;
                Vector3 Local(Vector2 g)=>origin+new Vector3((g.x-anchor.col-.5f)*w,(g.y+anchor.row-.5f)*h,0);
                var frame=TextureLayer("frame",StrokeTexture(),2,Color.white);
                foreach(var loop in shape.Loops)Stroke(frame,loop,Local,thickness,Mathf.Min(w,h)*.06f);
            }
            foreach(var layer in layers.Values){layer.mesh.SetVertices(layer.vertices);layer.mesh.SetUVs(0,layer.uv);layer.mesh.SetTriangles(layer.triangles,0);layer.mesh.RecalculateBounds();}
        }
        void OnDestroy()
        {
            foreach(var layer in layers.Values){if(layer.mesh)Destroy(layer.mesh);if(layer.material)Destroy(layer.material);}
            if(white)Destroy(white);
        }
    }
}
