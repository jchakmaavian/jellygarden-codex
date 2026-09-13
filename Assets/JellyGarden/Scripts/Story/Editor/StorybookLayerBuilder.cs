using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JellyGarden.Scripts.Story.Editor
{
    /// <summary>
    /// Packages the imagegen-cleared surfaces and original illustrations as ordinary
    /// alpha sprites. It only crops and masks; it never repaints the source artwork.
    /// The source coordinates are shared with the original HTML art board.
    /// </summary>
    public static class StorybookLayerBuilder
    {
        private const string Output = "Assets/JellyGarden/Resources/Story/TreasureHunt/UI/Layers";
        private static Texture2D _clean, _original, _controls, _panels;
        private static readonly List<string> Exported = new List<string>();

        public static void Run()
        {
            try
            {
                string root = Directory.GetCurrentDirectory();
                foreach (string arg in Environment.GetCommandLineArgs())
                    if (arg.StartsWith("-storybook-source=")) root = arg.Substring(18);
                _clean = Read(Path.Combine(root, "art-review/treasure-hunt/royal-storybook-layers/source/clean-board-v2.png"));
                _original = Read(Path.Combine(root, "art-review/treasure-hunt/ui-directions/v1/01-royal-storybook.png"));
                _panels = _clean;
                _controls = Read(Path.Combine(root,"art-review/treasure-hunt/royal-storybook-layers/source/controls-clean.png"));
                if (Mathf.Abs(_clean.width-1701)>2 || _clean.height != 924)
                    throw new Exception("Unexpected cleared board size");
                Directory.CreateDirectory(Output);
                Exported.Clear();
                StorybookArt.SourceOnly = true;
                // Complete surfaces keep the continuous paper texture, bevels and
                // folds; no nine-sliced strip is used to reconstruct these panels.
                Surface("area-sheet", 438,455,402,447, new Vector4(45,35,45,55),
                    0,60,3,36,12,19,28,12,60,5,125,0,200,0,270,3,336,8,365,14,
                    385,24,398,42,401,61,401,410,397,424,385,438,369,446,31,446,14,441,3,426,0,412);
                // Before-level includes its matching ribbon. Its close disc is a
                // separate button. The source disc is fully covered by that button.
                Surface("invitation", 876,290,374,490, Vector4.zero,
                    20,40,60,26,60,15,93,7,143,2,196,1,253,4,294,11,313,17,
                    314,29,344,34,358,38,368,48,373,62,372,88,371,447,368,467,
                    358,481,342,487,52,489,22,485,9,476,1,456,1,83,3,71,9,62,29,58);
                Surface("victory-paper",1300,423,375,387,Vector4.zero,
                    0,22,22,12,68,4,123,0,240,0,305,8,349,16,374,28,
                    374,350,371,366,361,379,344,386,23,386,9,377,0,356);
                Surface("blank-button",59,725,313,75,new Vector4(40,25,40,25),
                    42,0,269,0,287,4,302,15,310,28,312,40,307,57,295,68,
                    277,74,36,74,20,69,8,58,1,43,0,32,6,17,20,6);
                Surface("task-card",36,641,361,81,new Vector4(23,22,23,22),
                    23,0,337,0,350,4,358,14,360,26,360,56,356,70,345,78,
                    331,80,23,80,9,76,2,66,0,52,0,23,5,9);
                Surface("navigation-dock",17,803,399,100,new Vector4(42,25,42,42),
                    0,63,3,43,11,27,25,16,46,9,81,4,134,1,201,0,267,2,
                    325,6,355,12,375,22,389,37,397,56,398,75,394,85,386,93,
                    372,98,30,99,16,95,6,88,1,78);
                Surface("progress-badge",140,248,154,38,new Vector4(23,10,23,10),
                    0,0,153,0,153,18,149,29,139,36,17,36,7,33,1,23);
                Surface("wallet-coin",42,146,110,52,Vector4.zero,
                    26,0,42,6,93,6,105,12,109,26,106,40,96,46,44,46,
                    32,51,18,49,5,40,0,25,5,10,16,3);
                Surface("wallet-star",237,147,96,50,Vector4.zero,
                    20,0,25,1,35,10,75,4,89,11,95,25,91,39,81,46,
                    42,46,30,43,19,42,9,47,6,40,8,29,0,20,0,15,11,12);
                // Separate original illustrations, preserving source pixels.
                Original("victory-pip", StorybookArt.PipVictory);
                Original("victory-ribbon", StorybookArt.VictoryRibbon);
                Original("victory-star", StorybookArt.VictoryStar);
                Original("victory-button", StorybookArt.VictoryButton);
                Original("play-button", StorybookArt.PlayButton);
                Original("pip-home", StorybookArt.PipHome);
                Original("home-title", StorybookArt.HomeTitle);
                Original("coin", StorybookArt.Coin);
                Original("close", StorybookArt.CloseButton);
                Original("settings", StorybookArt.Settings);
                Original("flag", StorybookArt.Flag);
                Original("lantern", StorybookArt.Lantern);
                for(int i=0;i<3;i++)
                {
                    Original(new[]{"nav-home","nav-map","nav-collection"}[i], StorybookArt.Nav(i));
                    Original(new[]{"rocket","bomb","color-ball"}[i], StorybookArt.Booster(i));
                }
                Original("summary-check",StorybookArt.Crop("summary-check",472,569,31,30,
                    15,0,24,3,29,11,29,20,22,28,12,29,4,24,0,16,3,7));
                // Source-backed blank ribbon for shared menus (the invitation
                // already carries this ribbon in its complete panel layer).
                Surface("blank-ribbon",892,290,339,106,Vector4.zero,
                    2,39,46,27,46,15,80,6,134,1,191,0,250,4,293,14,296,26,336,38,
                    322,63,335,103,290,88,270,77,222,70,165,68,97,73,52,87,9,103,21,66);
                _clean=_controls;
                Surface("task-card",38,643,356,77,new Vector4(21,21,21,21),
                    Rounded(356,77,20));
                Surface("area-row",460,612,357,81,new Vector4(16,16,16,16),
                    Rounded(357,81,16));
                Surface("goal-well",927,419,274,75,new Vector4(22,22,22,22),
                    Rounded(274,75,22));
                Surface("booster-tile",901,549,102,106,new Vector4(25,25,25,25),
                    Rounded(102,106,25));
                Original("score-token",StorybookArt.Crop("score-token",969,431,50,50,
                    6,0,45,0,49,4,49,46,46,49,3,49,0,46,0,5));
                AssetDatabase.Refresh();
                Debug.Log("[Storybook] Packaged " + new HashSet<string>(Exported).Count + " RGBA production layers.");
            }
            catch(Exception e) { Debug.LogException(e); if(Application.isBatchMode) EditorApplication.Exit(1); }
            finally { StorybookArt.SourceOnly = false; }
        }

        private static Texture2D Read(string path)
        {
            var texture = new Texture2D(2,2,TextureFormat.RGBA32,false);
            texture.LoadImage(File.ReadAllBytes(path));
            return texture;
        }
        private static void Surface(string name,int x,int y,int w,int h,Vector4 border,params float[] outline)
        {
            var p = new Vector2[outline.Length/2];
            for(int i=0;i<p.Length;i++) p[i]=new Vector2(outline[i*2],h-outline[i*2+1]);
            Write(name,_clean,new RectInt(x,924-y-h,w,h),p,null,border);
        }
        private static void Original(string name,Sprite sprite)
        {
            Rect r=sprite.rect;
            float[] outline = StorybookArt.SourceOutlines[name];
            var vertices = new Vector2[outline.Length/2];
            for(int i=0;i<vertices.Length;i++) vertices[i]=new Vector2(outline[i*2],r.height-outline[i*2+1]);
            Write(name,_original,new RectInt((int)r.x,(int)r.y,(int)r.width,(int)r.height),vertices,null,Vector4.zero);
        }
        private static void Write(string name,Texture2D source,RectInt r,Vector2[] p,ushort[] triangles,Vector4 border)
        {
            var pixels=source.GetPixels(r.x,r.y,r.width,r.height);
            // The final edit returned 1702 columns. Normalize that one-column
            // difference to the original 1701-coordinate sheet before packaging.
            if(source.width != 1701)
                for(int y=0;y<r.height;y++) for(int x=0;x<r.width;x++)
                    pixels[y*r.width+x]=source.GetPixelBilinear((r.x+x+.5f)/1701f,(r.y+y+.5f)/924f);
            for(int y=0;y<r.height;y++) for(int x=0;x<r.width;x++)
            {
                int covered=0;
                for(int sy=0;sy<4;sy++) for(int sx=0;sx<4;sx++)
                    if(Inside(new Vector2(x+(sx+.5f)/4,y+(sy+.5f)/4),p,triangles)) covered++;
                var color=pixels[y*r.width+x]; color.a=covered/16f;
                pixels[y*r.width+x]=color;
            }
            // The cleared containers provide the actual background behind their
            // icons. Remove only matching background connected to the exterior;
            // enclosed white highlights stay part of the original illustration.
            Texture2D erased = name=="bomb" || name=="rocket" || name=="flag" || name=="lantern"
                ? _controls : name.StartsWith("nav-") || name=="summary-check" ? _panels : null;
            if(erased!=null) RemoveExterior(pixels,r,erased);
            var texture=new Texture2D(r.width,r.height,TextureFormat.RGBA32,false);
            bool transparent=false,opaque=false;
            foreach(Color c in pixels) { transparent |= c.a==0; opaque |= c.a==1; }
            if(!transparent || !opaque) throw new Exception("Missing sprite coverage: " + name);
            texture.SetPixels(pixels); texture.Apply();
            string path=Output+"/"+name+".png";
            File.WriteAllBytes(path,texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.spriteBorder=border;
            importer.spritePixelsPerUnit=100;
            importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.filterMode=FilterMode.Bilinear;
            importer.maxTextureSize=2048;
            importer.SaveAndReimport();
            Exported.Add(name);
        }
        private static bool Inside(Vector2 q,Vector2[] p,ushort[] triangles)
        {
            if(triangles!=null)
            {
                for(int i=0;i<triangles.Length;i+=3)
                {
                    Vector2 a=p[triangles[i]],b=p[triangles[i+1]],c=p[triangles[i+2]];
                    float ab=Cross(b-a,q-a),bc=Cross(c-b,q-b),ca=Cross(a-c,q-c);
                    if((ab>=0&&bc>=0&&ca>=0)||(ab<=0&&bc<=0&&ca<=0)) return true;
                }
                return false;
            }
            bool inside=false;
            for(int i=0,j=p.Length-1;i<p.Length;j=i++)
                if((p[i].y>q.y)!=(p[j].y>q.y) && q.x<(p[j].x-p[i].x)*(q.y-p[i].y)/(p[j].y-p[i].y)+p[i].x)
                    inside=!inside;
            return inside;
        }
        private static float Cross(Vector2 a,Vector2 b) => a.x*b.y-a.y*b.x;
        private static void RemoveExterior(Color[] pixels,RectInt r,Texture2D erased)
        {
            var seen=new bool[pixels.Length];
            var pending=new Queue<int>();
            for(int y=0;y<r.height;y++) for(int x=0;x<r.width;x++)
                if(x==0 || y==0 || x==r.width-1 || y==r.height-1 || pixels[y*r.width+x].a==0)
                    pending.Enqueue(y*r.width+x);
            while(pending.Count>0)
            {
                int index=pending.Dequeue();
                if(seen[index]) continue;
                seen[index]=true;
                int x=index%r.width,y=index/r.width;
                Color c=pixels[index],b=erased.GetPixelBilinear((r.x+x+.5f)/1701f,(r.y+y+.5f)/924f);
                float difference=Mathf.Max(Mathf.Abs(c.r-b.r),Mathf.Max(Mathf.Abs(c.g-b.g),Mathf.Abs(c.b-b.b)));
                if(c.a>0 && difference>.16f) continue;
                c.a=0; pixels[index]=c;
                if(x>0) pending.Enqueue(index-1);
                if(x<r.width-1) pending.Enqueue(index+1);
                if(y>0) pending.Enqueue(index-r.width);
                if(y<r.height-1) pending.Enqueue(index+r.width);
            }
        }
        private static float[] Rounded(float w,float h,float radius)
        {
            var outline=new List<float>();
            var centers=new[]{new Vector2(w-radius-1,radius+1),new Vector2(w-radius-1,h-radius-1),
                new Vector2(radius+1,h-radius-1),new Vector2(radius+1,radius+1)};
            for(int corner=0;corner<4;corner++) for(int i=0;i<=12;i++)
            {
                float a=(-90+corner*90+i*7.5f)*Mathf.Deg2Rad;
                outline.Add(centers[corner].x+radius*Mathf.Cos(a));
                outline.Add(centers[corner].y+radius*Mathf.Sin(a));
            }
            return outline.ToArray();
        }
    }
}
