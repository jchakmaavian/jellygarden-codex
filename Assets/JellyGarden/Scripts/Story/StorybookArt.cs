using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    /// <summary>
    /// UV cutouts of the approved HTML artwork. The source PNG is copied verbatim;
    /// native sprite meshes exclude the concept's backdrop. No screenshot of a
    /// whole screen is used as a control, and all variable text stays live.
    /// Coordinates below are top-left pixels in the 1701 x 924 review board.
    /// </summary>
    public static class StorybookArt
    {
        public static bool SourceOnly;
        public static readonly Dictionary<string, float[]> SourceOutlines = new Dictionary<string, float[]>();
        private static Texture2D _atlas;
        private static Font _font;
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        public static Font Font => _font != null ? _font :
            (_font = Resources.Load<Font>("Story/Fonts/storybook_title"));
        public static Texture2D Atlas => _atlas != null ? _atlas :
            (_atlas = Resources.Load<Texture2D>("Story/TreasureHunt/UI/storybook_reference"));

        public static Sprite Crop(string key, float x, float y, float w, float h, params float[] outline)
        {
            if (SourceOnly) SourceOutlines[key] = outline;
            if (!SourceOnly)
            {
                Sprite layer = StorybookLayers.Get(key);
                if (layer != null) return layer;
            }
            if (Sprites.TryGetValue(key, out Sprite cached)) return cached;
            var sprite = Sprite.Create(Atlas, new Rect(x, 924 - y - h, w, h),
                new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
            sprite.name = "Storybook_" + key;
            if (outline.Length >= 6)
            {
                var vertices = new Vector2[outline.Length / 2];
                for (int i = 0; i < vertices.Length; i++)
                    // OverrideGeometry takes unscaled pixels in Sprite Rect
                    // space; Unity applies the pivot and PPU afterwards.
                    vertices[i] = new Vector2(outline[i * 2], h - outline[i * 2 + 1]);
                sprite.OverrideGeometry(vertices, Triangulate(vertices));
            }
            Sprites.Add(key, sprite);
            return sprite;
        }

        public static Image Image(string name, Transform parent, Sprite sprite)
        {
            Image image = StoryUi.CreateImage(name, parent, sprite, Color.white);
            image.useSpriteMesh = true;
            image.raycastTarget = false;
            return image;
        }

        public static Sprite VictoryRibbon => Crop("victory-ribbon",1294,371,386,114,
            4,54, 37,39, 34,26, 39,19, 73,10, 132,3, 191,0, 253,4, 317,15, 346,27,
            348,39, 382,55, 361,74, 372,112, 328,96, 295,85, 230,77, 173,76, 100,82,
            62,92, 17,112, 24,75);
        public static Sprite PipVictory => Crop("victory-pip",1310,191,327,192,
            4,111,25,98,55,86,85,75,112,65,116,67,124,77,130,99,145,130,158,139,
            165,145,166,137,159,134,161,124,157,119,146,123,145,113,142,105,
            132,99,140,88,145,78,134,77,136,74,145,70,142,66,140,40,142,9,
            150,16,165,24,181,30,192,39,180,0,212,15,219,29,227,15,234,30,
            240,23,257,34,256,48,264,57,261,67,273,70,287,66,282,76,280,83,
            273,96,261,103,258,121,249,129,235,136,220,139,211,147,211,156,
            220,155,224,146,233,142,241,145,238,154,260,134,282,118,291,119,
            304,137,316,155,323,173,301,162,293,160,291,179,279,176,268,177,
            255,185,243,192,139,192,139,182,130,181,125,184,119,183,117,172,
            101,166,87,169,74,174,62,182,58,160,51,139,42,124,27,116);
        public static Sprite VictoryStar => Crop("victory-star",1404,493,163,155,
            81,1, 89,6, 110,45, 154,51, 162,59, 159,68, 126,98, 134,143,
            131,152, 122,154, 81,133, 40,154, 31,152, 29,143, 37,97,
            3,66, 1,58, 8,51, 53,45, 73,7);
        public static Sprite VictoryButton => Crop("victory-button",1332,703,310,82,
            40,1,270,1,290,7,303,21,309,38,307,56,295,72,276,80,36,80,
            16,73,4,58,0,40,4,23,16,8);
        public static Sprite PlayButton => Crop("play-button",914,668,300,81,
            42,1,258,1,278,7,292,20,299,38,296,58,283,72,265,79,36,79,
            17,72,5,58,0,40,4,23,18,8);
        public static Sprite PipHome => Crop("pip-home",296,328,106,134,
            4,42,9,28,19,19,32,13,33,9,48,10,51,8,59,12,69,10,74,16,82,12,
            85,22,91,20,92,31,99,40,104,40,100,48,105,62,104,75,98,88,87,103,
            91,112,90,122,84,128,24,130,17,126,15,116,17,108,13,106,4,91,1,74,0,59,3,47);
        public static Sprite HomeTitle => Crop("home-title",104,196,229,77,
            1,20,30,10,82,2,132,0,184,6,228,21,216,42,221,64,194,73,165,65,
            103,61,45,64,12,76,6,49);
        public static Sprite Coin => Crop("coin",43,147,50,50,
            24,0,37,3,46,13,49,25,45,39,35,47,22,49,9,44,1,32,0,20,6,8);
        public static Sprite CloseButton => Circle("close",1192,318,59,61);
        public static Sprite SmallStar => VictoryStar;
        public static Sprite Settings => Crop("settings",353,149,45,47,
            11,1,34,1,43,9,44,35,37,44,10,46,1,38,0,12);
        public static Sprite Flag => Crop("flag",479,623,62,62,
            1,1,10,1,13,5,38,4,54,0,55,30,47,45,31,42,13,39,13,60,7,62,5,11,0,7);
        public static Sprite Lantern => Crop("lantern",480,700,48,68,
            21,0,29,0,30,10,39,17,47,25,42,32,40,59,33,64,31,67,18,67,16,64,9,60,6,33,0,26,10,17,17,10);

        public static Sprite Nav(int i)
        {
            if (i == 0) return Crop("nav-home",73,811,49,46,
                0,25,20,4,24,1,29,6,39,12,39,5,45,5,45,18,48,24,47,28,
                44,27,44,45,29,45,29,31,24,30,20,32,20,45,5,45,5,27,2,28);
            if (i == 1) return Crop("nav-map",187,816,51,45,1,6,14,0,29,5,45,1,49,7,49,44,32,40,16,45,1,40);
            return Crop("nav-collection",314,816,40,46,4,0,36,3,39,9,36,42,3,45,0,39);
        }

        private static Sprite Circle(string key,float x,float y,float w,float h)
        {
            var outline = new float[96];
            for(int i=0;i<48;i++)
            {
                float a=i*Mathf.PI/24;
                outline[i*2]=w*.5f+(w*.5f-1)*Mathf.Cos(a);
                outline[i*2+1]=h*.5f+(h*.5f-1)*Mathf.Sin(a);
            }
            return Crop(key,x,y,w,h,outline);
        }

        public static Sprite Booster(int i)
        {
            if (i == 0) return Crop("rocket",915,557,74,89,
                42,1,60,1,69,12,73,34,58,46,56,69,39,88,35,66,25,76,15,71,0,80,7,59,1,55,8,39,24,27);
            if (i == 1) return Crop("bomb",1023,553,86,93,
                47,2,57,5,63,0,71,4,75,12,68,17,67,27,80,45,85,61,80,78,
                67,88,48,92,24,87,8,73,0,54,3,36,14,23,35,17,44,21,48,13,43,9);
            return Crop("color-ball",1136,563,79,79,
                39,0,59,5,73,19,78,39,74,59,59,73,39,78,20,74,5,59,0,39,5,19,19,5);
        }

        // Separate edge strips make the card resize without scaling the folds,
        // and leave the centre free of the concept's sample text and rewards.
        public static void PaperEdges(RectTransform parent, float unit, bool folds)
        {
            Edge("PaperLeft", parent, Crop("paper-left",879,407,18,332), 0,0,0,1, 0,20,16*unit,-20);
            Edge("PaperRight", parent, Crop("paper-right",1229,407,19,332), 1,0,1,1, -17*unit,20,0,-20);
            Edge("PaperTop", parent, Crop("paper-top",492,455,287,21), 0,1,1,1, 18*unit,-18*unit,-18*unit,0);
            Edge("PaperBottom", parent, Crop("paper-bottom",925,758,273,20), 0,0,1,0, 22*unit,0,-22*unit,18*unit);
            if (folds)
            {
                Edge("FoldLeft",parent,Crop("fold-left",877,744,49,36,
                    0,0,24,5,36,7,49,26,48,35,20,31,7,23),0,0,0,0,0,0,44*unit,32*unit);
                Edge("FoldRight",parent,Crop("fold-right",1198,739,51,41,
                    0,38,20,25,25,13,51,0,50,22,43,33,31,39),1,0,1,0,-46*unit,0,0,37*unit);
                Edge("FoldTop",parent,Crop("fold-top",445,458,48,42,
                    0,21,15,11,47,0,35,13,23,41,13,25),0,1,0,1,0,-38*unit,44*unit,0);
            }
        }

        public static void Edge(string name, RectTransform parent, Sprite sprite,
            float ax, float ay, float bx, float by, float left, float bottom, float right, float top)
        {
            var r = Image(name,parent,sprite).rectTransform;
            r.anchorMin = new Vector2(ax,ay); r.anchorMax = new Vector2(bx,by);
            r.offsetMin = new Vector2(left,bottom); r.offsetMax = new Vector2(right,top);
        }

        public static Image BlankButton(string name, Transform parent, float unit = 1)
        {
            if (StorybookLayers.Get("blank-button") != null)
                return StorybookLayers.Sliced(name, parent, "blank-button");
            Image root = StoryUi.CreateImage(name,parent,StoryUi.RoundedSprite(Mathf.RoundToInt(38*unit)),Color.white);
            root.type = UnityEngine.UI.Image.Type.Sliced;
            StoryCastleUi.Gradient(root,new Color32(0,113,238,255),new Color32(0,57,145,255));
            var rim = Crop("button-rim",1332,703,310,82);
            // The centre is omitted: the illustrated rim has no static label.
            var border = Sprite.Create(Atlas,rim.rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(44,24,44,21));
            var edge = StoryUi.CreateImage("IllustratedRim",root.transform,border,Color.white);
            edge.type = UnityEngine.UI.Image.Type.Sliced; edge.fillCenter = false;
            edge.pixelsPerUnitMultiplier = 1 / unit;
            StoryUi.Stretch(edge.rectTransform);
            root.gameObject.AddComponent<StorybookButtonRim>().Rim = edge;
            var mask = root.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = true;
            return root;
        }

        public static Image BlankRibbon(string name, Transform parent)
        {
            if (StorybookLayers.Get("blank-ribbon") != null)
                return StorybookLayers.Image(name, parent, "blank-ribbon");
            Image root = Image(name,parent,Crop("level-ribbon",892,290,339,106,
                2,39,46,27,46,15,80,6,134,1,191,0,250,4,293,14,296,26,336,38,
                322,63,335,103,290,88,270,77,222,70,165,68,97,73,52,87,9,103,21,66));
            // Replace only the sample level lettering with uninterrupted blue
            // sampled from the same ribbon at the same height.
            var patch = Image("LiveTitleGround",root.transform,Crop("ribbon-blue",976,309,12,42));
            patch.rectTransform.anchorMin = new Vector2(98f/339,45f/106);
            patch.rectTransform.anchorMax = new Vector2(252f/339,87f/106);
            patch.rectTransform.offsetMin = patch.rectTransform.offsetMax = Vector2.zero;
            return root;
        }

        private static ushort[] Triangulate(Vector2[] p)
        {
            var ids = new List<int>(); float area = 0;
            for (int i=0;i<p.Length;i++) area += Cross(p[i],p[(i+1)%p.Length]);
            for (int i=0;i<p.Length;i++) ids.Add(area > 0 ? i : p.Length-1-i);
            var triangles = new List<ushort>(); int guard = p.Length*p.Length;
            while (ids.Count>2 && guard-- > 0)
            {
                bool clipped = false;
                for (int k=0;k<ids.Count;k++)
                {
                    int a=ids[(k+ids.Count-1)%ids.Count],b=ids[k],c=ids[(k+1)%ids.Count];
                    if (Cross(p[b]-p[a],p[c]-p[b]) <= .000001f) continue;
                    bool occupied=false;
                    foreach(int v in ids)
                        if(v!=a && v!=b && v!=c && Inside(p[v],p[a],p[b],p[c])) { occupied=true; break; }
                    if(occupied) continue;
                    triangles.Add((ushort)a);triangles.Add((ushort)b);triangles.Add((ushort)c);
                    ids.RemoveAt(k);clipped=true;break;
                }
                if(!clipped) break;
            }
            if (triangles.Count != (p.Length-2)*3) Debug.LogError("[Storybook] Invalid artwork outline");
            return triangles.ToArray();
        }
        private static float Cross(Vector2 a,Vector2 b) => a.x*b.y-a.y*b.x;
        private static bool Inside(Vector2 p,Vector2 a,Vector2 b,Vector2 c) =>
            Cross(b-a,p-a)>=0 && Cross(c-b,p-b)>=0 && Cross(a-c,p-c)>=0;
    }

    // Keep the illustrated capsule ends proportional to the actual button
    // height, including smaller controls such as Settings' Reset Progress.
    public sealed class StorybookButtonRim : MonoBehaviour
    {
        public Image Rim;
        private float _height;
        private void LateUpdate()
        {
            float height = ((RectTransform)transform).rect.height;
            if (Rim == null || height <= 0 || Mathf.Abs(height-_height)<.1f) return;
            _height = height;
            Rim.pixelsPerUnitMultiplier = 82f/height;
        }
    }
}
