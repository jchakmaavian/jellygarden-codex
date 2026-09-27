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
        // One side's band of the gold frame (the gameplay reference): `size` is its thickness across and length along the
        // side; the texture's cross-section runs from the cells outwards.
        void Edge(string name,Vector3 centre,Vector2 size)
        {
            float angle=name=="bottom"?180:name=="left"?90:name=="right"?-90:0;
            if(name=="left"||name=="right")size=new Vector2(size.y,size.x);
            Quad(TextureLayer(name,RoyalGameplayHud.FrameEdge(),2,Color.white),centre,size,new Rect(0,0,1,1),angle);EdgeCount++;
        }
        bool Occupied(int col,int row)=>occupied.Contains(new Vector2Int(col,row));
        // The frame around one cell: a band `t` wide outside each exposed side. Where the silhouette turns outwards the two
        // bands meet in a rounded corner (a quarter ring, radius t to 2t, about a point inside the cell); where it turns
        // inwards, in a mitred square. The bands stop short of both by t. Outer corners belong to the cell they wrap; inner
        // corners are drawn from the cells' top and bottom sides.
        void Frame(Vector3 p,int col,int row,float w,float h,float t)
        {
            bool up=!Occupied(col,row-1),down=!Occupied(col,row+1),left=!Occupied(col-1,row),right=!Occupied(col+1,row);
            // An end is shortened at an outer corner (the side beside it is open too) or an inner one (the cells beside and
            // beyond it are both there); otherwise the band runs straight on into the next cell's.
            float End(bool besideOpen,int col2,int row2)=>besideOpen||Occupied(col2,row2)?t:0;
            if(up){float a=End(left,col-1,row-1),b=End(right,col+1,row-1);Edge("top",p+new Vector3((a-b)*.5f,h*.5f+t*.5f),new Vector2(w-a-b,t));}
            if(down){float a=End(left,col-1,row+1),b=End(right,col+1,row+1);Edge("bottom",p+new Vector3((a-b)*.5f,-h*.5f-t*.5f),new Vector2(w-a-b,t));}
            if(left){float a=End(up,col-1,row-1),b=End(down,col-1,row+1);Edge("left",p+new Vector3(-w*.5f-t*.5f,(b-a)*.5f),new Vector2(t,h-a-b));}
            if(right){float a=End(up,col+1,row-1),b=End(down,col+1,row+1);Edge("right",p+new Vector3(w*.5f+t*.5f,(b-a)*.5f),new Vector2(t,h-a-b));}
            var outer=RoyalGameplayHud.OuterCornerUV;var inner=RoyalGameplayHud.InnerCornerUV;var size=new Vector2(2*t,2*t);var square=new Vector2(t,t);
            Layer Corners()=>TextureLayer("corners",RoyalGameplayHud.FrameCorners(),2,Color.white);
            if(up&&left)Quad(Corners(),p+new Vector3(-w*.5f,h*.5f),size,outer,0);
            if(up&&right)Quad(Corners(),p+new Vector3(w*.5f,h*.5f),size,outer,-90);
            if(down&&right)Quad(Corners(),p+new Vector3(w*.5f,-h*.5f),size,outer,180);
            if(down&&left)Quad(Corners(),p+new Vector3(-w*.5f,-h*.5f),size,outer,90);
            if(up&&!left&&Occupied(col-1,row-1))Quad(Corners(),p+new Vector3(-w*.5f+t*.5f,h*.5f+t*.5f),square,inner,-90);
            if(up&&!right&&Occupied(col+1,row-1))Quad(Corners(),p+new Vector3(w*.5f-t*.5f,h*.5f+t*.5f),square,inner,0);
            if(down&&!left&&Occupied(col-1,row+1))Quad(Corners(),p+new Vector3(-w*.5f+t*.5f,-h*.5f-t*.5f),square,inner,180);
            if(down&&!right&&Occupied(col+1,row+1))Quad(Corners(),p+new Vector3(w*.5f-t*.5f,-h*.5f-t*.5f),square,inner,90);
        }
        public void RebuildIfNeeded(LevelManager lm)
        {
            if(lm.squaresArray==null||!RoyalGameplayArt.Art("cell-light"))return;
            if(squares==lm.squaresArray&&RenderersIntact())return;
            squares=lm.squaresArray;EdgeCount=0;occupied.Clear();
            if(!white){white=new Texture2D(1,1);white.SetPixel(0,0,Color.white);white.Apply();}
            foreach(var entry in layers){var layer=entry.Value;EnsureRenderer(entry.Key,layer);layer.vertices.Clear();layer.uv.Clear();layer.triangles.Clear();layer.mesh.Clear();}
            foreach(var square in squares)if(square&&square.type!=SquareTypes.NONE)occupied.Add(new Vector2Int(square.col,square.row));
            // The frame band is a tenth of a cell, outside the cells (RoyalGameplayPresentation leaves the board room for it).
            float w=lm.squareWidth,h=lm.squareHeight,thickness=Mathf.Min(w,h)*.1f;
            // The cell art is a rounded square, so four neighbouring corners leave a small
            // concave gap. A square shadow quad showed through those gaps as dark specks over
            // the whole board. Grout fills the gaps with a board tone and the shadow now takes
            // the rounded cell silhouette, so it only reads along the board's outer edge.
            var cell=RoyalGameplayArt.Art("cell-light");
            var grout=GetLayer("grout",null,-2,new Color(.34f,.39f,.62f,1f));
            var shadow=GetLayer("shadow",cell,-1,new Color(.015f,.04f,.12f,.75f));
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
                Frame(p,col0,row0,w,h,thickness);
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
