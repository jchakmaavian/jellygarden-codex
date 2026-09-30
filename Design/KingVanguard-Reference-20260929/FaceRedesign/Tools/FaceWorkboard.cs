using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

// Technical extraction/registration only. Does not generate facial artwork.
public static class FaceWorkboard {
    public class Region {
        public string name;
        public int x,y,width,height,rotation;
        public int[] original,offset;
        public int PhysicalWidth { get { return rotation==90||rotation==270?height:width; } }
        public int PhysicalHeight { get { return rotation==90||rotation==270?width:height; } }
    }
    public class Cell {
        public int index;
        public string name,label,hint,maskPath;
        public bool technicalShadow;
        public Region region;
        public int[] cellRect,paintRect;
        public int sourceMaskPixels;
    }
    public class Layout {
        public int schemaVersion=1;
        public string name,sourcePath,atlasPath,sourceAlpha,boardPath;
        public int width,height,columns,rows,cellSize,atlasWidth,atlasHeight;
        public string matte="00ff00",rotationConvention="Spine90 packed counterclockwise; workboard crop rotates clockwise to upright";
        public string maskPolicy="Largest connected alpha core at alpha>=16; other components excluded; nearest core labels own alpha fringes within 3 pixels.";
        public bool noOriginalFacialRgbFallback=true;
        public List<Cell> cells=new List<Cell>();
    }
    static JavaScriptSerializer Json() { return new JavaScriptSerializer { MaxJsonLength=int.MaxValue, RecursionLimit=100 }; }
    public static string Serialize(object value) { return Json().Serialize(value); }
    public static List<Region> ReadAtlas(string path,out int width,out int height) {
        width=height=0; Region current=null; var result=new List<Region>();
        foreach(string raw in File.ReadAllLines(path,Encoding.UTF8)) {
            string t=raw.Trim(); if(t.Length==0)continue;
            if(raw.StartsWith("size:")){int[] p=Pair(t);width=p[0];height=p[1];continue;}
            if(!char.IsWhiteSpace(raw[0])&&!t.Contains(":")&&!t.EndsWith(".png")){current=new Region{name=t};result.Add(current);continue;}
            if(current==null)continue;
            if(t.StartsWith("xy:")){int[] p=Pair(t);current.x=p[0];current.y=p[1];}
            else if(t.StartsWith("size:")){int[] p=Pair(t);current.width=p[0];current.height=p[1];}
            else if(t.StartsWith("orig:"))current.original=Pair(t);
            else if(t.StartsWith("offset:"))current.offset=Pair(t);
            else if(t.StartsWith("rotate:")){string v=t.Substring(7).Trim();current.rotation=v=="true"?90:v=="false"?0:int.Parse(v);}
        }
        if(width<=0||height<=0)throw new Exception("Atlas page dimensions missing: "+path);
        return result;
    }
    static int[] Pair(string s) { var m=Regex.Match(s,@"(-?\d+),\s*(-?\d+)"); if(!m.Success)throw new Exception(s);return new[]{int.Parse(m.Groups[1].Value),int.Parse(m.Groups[2].Value)}; }
    static Bitmap Image(byte[] pixels,int width,int height) {
        var image=new Bitmap(width,height,PixelFormat.Format32bppArgb);
        var data=image.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
        Marshal.Copy(pixels,0,data.Scan0,pixels.Length);image.UnlockBits(data);return image;
    }
    static byte[] Pixels(Bitmap image) {
        var data=image.LockBits(new Rectangle(0,0,image.Width,image.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        var pixels=new byte[image.Width*image.Height*4];Marshal.Copy(data.Scan0,pixels,0,pixels.Length);image.UnlockBits(data);return pixels;
    }
    static byte[] Read(string path,int width,int height,bool pma) {
        using(var original=new Bitmap(path))using(var image=new Bitmap(width,height,PixelFormat.Format32bppArgb)) {
            byte[] result;
            if(original.Width==width&&original.Height==height)result=Pixels(original);
            else {using(var g=Graphics.FromImage(image)) {g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.DrawImage(original,new Rectangle(0,0,width,height),0,0,original.Width,original.Height,GraphicsUnit.Pixel);}result=Pixels(image);}
            if(pma)for(int i=0;i<result.Length;i+=4) {int a=result[i+3];if(a>0&&a<255)for(int c=0;c<3;c++)result[i+c]=(byte)Math.Min(255,(result[i+c]*255+a/2)/a);}
            return result;
        }
    }
    static byte[] Crop(byte[] atlas,int aw,int ah,Region r) {
        int w=r.PhysicalWidth,h=r.PhysicalHeight;var result=new byte[w*h*4];
        if(r.x<0||r.y<0||r.x+w>aw||r.y+h>ah)throw new Exception("Region outside atlas: "+r.name);
        for(int y=0;y<h;y++)Buffer.BlockCopy(atlas,((r.y+y)*aw+r.x)*4,result,y*w*4,w*4);
        return result;
    }
    // A rectangle may enclose smaller, tightly packed neighboring regions in transparent corners.
    static bool[] LargestIsland(byte[] source,int w,int h) {
        int n=w*h;var labels=new int[n];int best=0,bestCount=0,label=0;var q=new Queue<int>();
        for(int i=0;i<n;i++) {
            if(labels[i]!=0||source[i*4+3]<16)continue;
            label++;int count=0;labels[i]=label;q.Enqueue(i);
            while(q.Count>0){int p=q.Dequeue();count++;foreach(int v in Neighbors(p,w,h))if(labels[v]==0&&source[v*4+3]>=16){labels[v]=label;q.Enqueue(v);}}
            if(count>bestCount){bestCount=count;best=label;}
        }
        if(best==0)return new bool[n];
        var distance=new int[n];for(int i=0;i<n;i++){distance[i]=labels[i]>0?0:int.MaxValue;if(labels[i]>0)q.Enqueue(i);}
        while(q.Count>0){int p=q.Dequeue();if(distance[p]>=3)continue;foreach(int v in Neighbors(p,w,h))if(distance[v]>distance[p]+1){labels[v]=labels[p];distance[v]=distance[p]+1;q.Enqueue(v);}}
        var mask=new bool[n];for(int i=0;i<n;i++)mask[i]=labels[i]==best&&source[i*4+3]>0;return mask;
    }
    static IEnumerable<int> Neighbors(int p,int w,int h) {int x=p%w,y=p/w;if(x>0)yield return p-1;if(x<w-1)yield return p+1;if(y>0)yield return p-w;if(y<h-1)yield return p+w;}
    static byte[] Upright(byte[] packed,Region r) {using(var image=Image(packed,r.PhysicalWidth,r.PhysicalHeight)){if(r.rotation==90)image.RotateFlip(RotateFlipType.Rotate90FlipNone);else if(r.rotation==270)image.RotateFlip(RotateFlipType.Rotate270FlipNone);else if(r.rotation==180)image.RotateFlip(RotateFlipType.Rotate180FlipNone);return Pixels(image);}}
    static byte[] Packed(byte[] upright,Region r) {using(var image=Image(upright,r.width,r.height)){if(r.rotation==90)image.RotateFlip(RotateFlipType.Rotate270FlipNone);else if(r.rotation==270)image.RotateFlip(RotateFlipType.Rotate90FlipNone);else if(r.rotation==180)image.RotateFlip(RotateFlipType.Rotate180FlipNone);return Pixels(image);}}
    static string Label(string n) {
        switch(n){
            case "Sakal Surat":return "FACE + BEARD UNDERLAY";case "Burun":return "BUTTON NOSE";case "Bıyık":return "WHITE MOUSTACHE";case "Taç":return "BLUE VELVET GOLD CROWN";
            case "Sol Yanak":return "LEFT ROSY CHEEK";case "Sağ Yanak":return "RIGHT ROSY CHEEK";case "Sol Kaş":return "LEFT DARK EYEBROW";case "Sağ Kaş":return "RIGHT DARK EYEBROW";
            case "Sol Göz":return "LEFT EYE WHITE";case "Sağ Göz":return "RIGHT EYE WHITE";case "Sol  Göz Bebeği":return "LEFT BROWN IRIS + PUPIL";case "Sağ Göz Bebeği":return "RIGHT BROWN IRIS + PUPIL";
            case "Sol Göz kapağı":return "LEFT SKIN EYELID";case "Sağ Göz Kapağı":return "RIGHT SKIN EYELID";case "Sol Kirpik":return "LEFT LASH / LID RIM";case "Sağ Kirpik":return "RIGHT LASH / LID RIM";
            case "Sol Kulak":return "LEFT EAR";case "Sağ Kulak":return "RIGHT EAR";case "Ağız":return "OPEN MOUTH INTERIOR";case "Ust Diş":return "UPPER TEETH";case "Alt Diş":return "LOWER TEETH";case "Dil":return "TONGUE";
            case "Sakal Gölge":return "BEARD SHADOW MASK";case "Bıyık Gölge":return "MOUSTACHE SHADOW MASK";case "Taç Alt Gölge":return "CROWN UNDERSIDE SHADOW";case "Sol Kaş Gölge":return "LEFT BROW SHADOW";case "Sağ Kaş Gölge":return "RIGHT BROW SHADOW";
            case "01900_kafa":return "HEAD + BEARD + CROWN";case "02500_biyik":return "MOUSTACHE + BUTTON NOSE";
            case "01910_sag_goz":return "RIGHT EYE WHITE";case "01920_sag_goz_bebek":return "RIGHT BROWN IRIS + PUPIL";case "01930_sol_goz":return "LEFT EYE WHITE";case "01940_sol_goz_bebek":return "LEFT BROWN IRIS + PUPIL";
            case "01950_sol_goz_kapak":return "LEFT SKIN EYELID";case "01960_sag_goz_kapak":return "RIGHT SKIN EYELID";case "01970_agiz":return "MOUTH INTERIOR";
            case "02600_kas_sag_G":return "RIGHT BROW SHADOW";case "02700_kas_sag":return "RIGHT DARK EYEBROW";case "02800_kas_sol_G":return "LEFT BROW SHADOW";case "02900_kas_sol":return "LEFT DARK EYEBROW";
            case "00900_A_fqace_G":return "FACE SHADOW MASK";case "01000_A_mouth":return "BEDTIME MOUTH";case "01100_A_teeth":return "BEDTIME TEETH";case "01200_A_Face copy 2":return "BEDTIME FACE + BEARD";case "01300_A_nose":return "BUTTON NOSE";case "01400_A_moustache":return "WHITE MOUSTACHE";case "01700_A_right_eyebrow":return "RIGHT DARK EYEBROW";case "01800_A_left_eyebrow":return "LEFT DARK EYEBROW";
            default:return n;
        }
    }
    static string Hint(string n) {
        if(n=="Sakal Surat")return "Paint only the round-cheek face/beard underlay. Do not add eyes, pupils, eyebrows, nose, moustache, crown or mouth; those animate as separate cells.";
        if(n=="01900_kafa")return "One head underlay with full white beard and new blue/gold crown. Do not paint eyes, eyebrows, nose or moustache; those animate as separate cells.";
        if(n=="02500_biyik")return "This native attachment combines the nose and moustache. Paint both together as one registered component.";
        if(n=="01200_A_Face copy 2")return "Bedtime face and beard with eyes/eyelids baked in; preserve source resting/expression pose. Nose, moustache and eyebrows remain separate components.";
        if(n=="Taç")return "New distinct five-point gold crown, deep blue velvet, blue central jewel; separate attachment.";
        return "One isolated registered component only. Keep its placement, dimensions, silhouette and orientation; do not add another face or neighboring part.";
    }
    public static string Build(string sourcePath,string atlasPath,string[] names,string outputDirectory,string boardName,bool sourcePma,int columns,int cellSize) {
        int aw,ah;var all=ReadAtlas(atlasPath,out aw,out ah);var regions=names.Select(n=>all.Single(r=>r.name==n)).ToList();
        return BuildRegions(sourcePath,atlasPath,regions,aw,ah,outputDirectory,boardName,sourcePma,columns,cellSize);
    }
    public static string BuildRegions(string sourcePath,string atlasPath,List<Region> regions,int aw,int ah,string outputDirectory,string boardName,bool sourcePma,int columns,int cellSize) {
        CheckOutput(outputDirectory);
        Directory.CreateDirectory(outputDirectory);Directory.CreateDirectory(Path.Combine(outputDirectory,"Parts"));
        var layout=new Layout{name=boardName,sourcePath=Path.GetFullPath(sourcePath),atlasPath=Path.GetFullPath(atlasPath),sourceAlpha=sourcePma?"premultiplied decoded to straight":"straight",columns=columns,rows=(regions.Count+columns-1)/columns,cellSize=cellSize,atlasWidth=aw,atlasHeight=ah};
        layout.width=columns*cellSize;layout.height=layout.rows*cellSize;layout.boardPath=Path.GetFullPath(Path.Combine(outputDirectory,boardName+"-source-workboard.png"));
        byte[] source=Read(sourcePath,aw,ah,sourcePma);
        using(var board=new Bitmap(layout.width,layout.height,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(board))using(var font=new Font("Arial",10,FontStyle.Bold))using(var small=new Font("Arial",8)) {
            g.Clear(Color.Lime);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
            for(int index=0;index<regions.Count;index++) {
                var r=regions[index];byte[] packed=Crop(source,aw,ah,r);bool[] mask=LargestIsland(packed,r.PhysicalWidth,r.PhysicalHeight);int count=0;
                for(int p=0;p<mask.Length;p++)if(!mask[p]){packed[p*4]=packed[p*4+1]=packed[p*4+2]=packed[p*4+3]=0;}else count++;
                byte[] upright=Upright(packed,r);int cx=index%columns*cellSize,cy=index/columns*cellSize;
                float scale=Math.Min((cellSize-32f)/r.width,(cellSize-58f)/r.height);int pw=Math.Max(1,(int)Math.Round(r.width*scale)),ph=Math.Max(1,(int)Math.Round(r.height*scale));
                var paint=new Rectangle(cx+(cellSize-pw)/2,cy+42+(cellSize-48-ph)/2,pw,ph);
                string part=Path.Combine("Parts",(index+1).ToString("00")+".png"),maskPath=Path.Combine("Parts",(index+1).ToString("00")+"-mask.png");
                using(var image=Image(upright,r.width,r.height)){image.Save(Path.Combine(outputDirectory,part),ImageFormat.Png);g.DrawImage(image,paint);}
                var alpha=(byte[])upright.Clone();for(int i=0;i<alpha.Length;i+=4)alpha[i]=alpha[i+1]=alpha[i+2]=255;
                using(var image=Image(alpha,r.width,r.height))image.Save(Path.Combine(outputDirectory,maskPath),ImageFormat.Png);
                g.FillRectangle(Brushes.MidnightBlue,cx,cy,cellSize,36);g.DrawRectangle(Pens.DarkSlateGray,cx,cy,cellSize-1,cellSize-1);
                g.DrawString((index+1).ToString("00")+" "+Label(r.name),font,Brushes.White,new RectangleF(cx+5,cy+3,cellSize-10,16));
                g.DrawString(r.name+" | "+r.width+" x "+r.height,small,Brushes.LightSteelBlue,cx+5,cy+19);
                layout.cells.Add(new Cell{index=index+1,name=r.name,label=Label(r.name),hint=Hint(r.name),technicalShadow=r.name.Contains("Gölge")||r.name.EndsWith("_G"),maskPath=maskPath.Replace('\\','/'),region=r,cellRect=new[]{cx,cy,cellSize,cellSize},paintRect=new[]{paint.X,paint.Y,paint.Width,paint.Height},sourceMaskPixels=count});
            }
            board.Save(layout.boardPath,ImageFormat.Png);
        }
        string metadata=Path.Combine(outputDirectory,boardName+"-layout.json");File.WriteAllText(metadata,Serialize(layout),new UTF8Encoding(false));return metadata;
    }

    static void CheckOutput(string path) {
        string full=Path.GetFullPath(path).Replace('\\','/');
        if(full.IndexOf("/FaceRedesign",StringComparison.OrdinalIgnoreCase)<0||full.IndexOf("/Assets/",StringComparison.OrdinalIgnoreCase)>=0)throw new Exception("Output must stay under the technical FaceRedesign evidence directory, never Assets: "+path);
    }
    static string Sha256(string path) {using(var sha=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
    static byte[] PaintedCell(Bitmap board,Layout layout,Cell cell) {
        float sx=(float)board.Width/layout.width,sy=(float)board.Height/layout.height;int[] p=cell.paintRect;
        using(var image=new Bitmap(cell.region.width,cell.region.height,PixelFormat.Format32bppArgb)) {
            using(var g=Graphics.FromImage(image)) {
                g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                g.DrawImage(board,new Rectangle(0,0,image.Width,image.Height),p[0]*sx,p[1]*sy,p[2]*sx,p[3]*sy,GraphicsUnit.Pixel);
            }
            return Pixels(image);
        }
    }
    // RGB propagation uses the NEW paint only. Source facial RGB is never a candidate.
    static int[] NearestNewPaint(byte[] pixels,int w,int h) {
        var nearest=new int[w*h];var queue=new Queue<int>();
        for(int p=0;p<nearest.Length;p++) {
            int i=p*4,green=pixels[i+1]-Math.Max(pixels[i],pixels[i+2]);
            nearest[p]=-1;
            if(pixels[i+3]>=32&&green<=12){nearest[p]=p;queue.Enqueue(p);}
        }
        if(queue.Count==0)throw new Exception("Cell has no usable new artwork after green keying; source fallback is prohibited.");
        while(queue.Count>0){int p=queue.Dequeue();foreach(int n in Neighbors(p,w,h))if(nearest[n]<0){nearest[n]=nearest[p];queue.Enqueue(n);}}
        return nearest;
    }
    static int KeyAlpha(byte[] pixels,int i) {
        int dominance=pixels[i+1]-Math.Max(pixels[i],pixels[i+2]);
        if(dominance<=12)return pixels[i+3];
        if(dominance>=160)return 0;
        return (pixels[i+3]*(160-dominance)+74)/148;
    }
    public static string ValidateRegistration(string layoutPath,string sourceMaskPath,string targetAtlasPath) {
        var layout=Json().Deserialize<Layout>(File.ReadAllText(layoutPath,Encoding.UTF8));int aw=layout.atlasWidth,ah=layout.atlasHeight;
        var regions=string.IsNullOrEmpty(targetAtlasPath)?layout.cells.Select(c=>c.region).ToList():ReadAtlas(targetAtlasPath,out aw,out ah);
        byte[] source=Read(sourceMaskPath,aw,ah,false);var owners=new int[aw*ah];int overlaps=0,maskPixels=0,rotationDifferences=0;var details=new List<object>();var collisions=new List<object>();
        foreach(var cell in layout.cells){
            var r=regions.Single(v=>v.name==cell.name);if(r.width!=cell.region.width||r.height!=cell.region.height)throw new Exception("Logical dimensions differ: "+cell.name);
            byte[] crop=Crop(source,aw,ah,r),roundTrip=Packed(Upright(crop,r),r);for(int i=0;i<crop.Length;i++)if(crop[i]!=roundTrip[i])rotationDifferences++;
            bool[] mask=LargestIsland(crop,r.PhysicalWidth,r.PhysicalHeight);int count=0;
            for(int p=0;p<mask.Length;p++)if(mask[p]){count++;int at=(r.y+p/r.PhysicalWidth)*aw+r.x+p%r.PhysicalWidth;if(owners[at]!=0&&owners[at]!=cell.index){overlaps++;collisions.Add(new{first=owners[at],second=cell.index,x=at%aw,y=at/aw,alpha=source[at*4+3]});}owners[at]=cell.index;}
            maskPixels+=count;details.Add(new{name=cell.name,pixels=count,rotation=r.rotation});
        }
        return Serialize(new{layout=layout.name,atlasWidth=aw,atlasHeight=ah,regions=layout.cells.Count,maskPixels=maskPixels,overlappingOwnershipPixels=overlaps,overlaps=collisions,rotationRoundTripByteDifferences=rotationDifferences,details=details});
    }
    public static string Pack(string layoutPath,string paintedBoardPath,string baseAtlasPath,string sourceMaskPath,string targetAtlasPath,string outputPath,bool preserveTechnicalShadows,bool preserveSourceCoverage) {
        CheckOutput(outputPath);
        var layout=Json().Deserialize<Layout>(File.ReadAllText(layoutPath,Encoding.UTF8));
        int aw=layout.atlasWidth,ah=layout.atlasHeight;
        List<Region> targetRegions;
        if(string.IsNullOrEmpty(targetAtlasPath))targetRegions=layout.cells.Select(c=>c.region).ToList();
        else targetRegions=ReadAtlas(targetAtlasPath,out aw,out ah);
        using(var baseImage=new Bitmap(baseAtlasPath))if(baseImage.Width!=aw||baseImage.Height!=ah)throw new Exception("Base atlas must already have exact atlas dimensions; outfit resampling is prohibited.");
        byte[] original=Read(baseAtlasPath,aw,ah,false),output=(byte[])original.Clone(),source=Read(sourceMaskPath,aw,ah,false);
        var ownership=new int[aw*ah];var reports=new List<object>();int totalPainted=0,totalOverlap=0,totalCleared=0;
        using(var board=new Bitmap(paintedBoardPath)) {
            foreach(var cell in layout.cells) {
                var target=targetRegions.Single(r=>r.name==cell.name);
                if(target.width!=cell.region.width||target.height!=cell.region.height)throw new Exception("Shared logical dimensions differ: "+cell.name);
                if(preserveTechnicalShadows&&cell.technicalShadow){reports.Add(new {name=cell.name,preservedTechnicalShadow=true});continue;}
                byte[] crop=Crop(source,aw,ah,target);bool[] mask=LargestIsland(crop,target.PhysicalWidth,target.PhysicalHeight);
                byte[] painted=PaintedCell(board,layout,cell);int[] nearest=null;
                if(!cell.technicalShadow){try{nearest=NearestNewPaint(painted,cell.region.width,cell.region.height);}catch(Exception e){throw new Exception(cell.name+": "+e.Message);}}
                byte[] clean=new byte[painted.Length];int keyed=0,edgeRgbFilled=0;
                for(int p=0;p<painted.Length/4;p++) {
                    int i=p*4,alpha=KeyAlpha(painted,i),dominance=painted[i+1]-Math.Max(painted[i],painted[i+2]);
                    if(cell.technicalShadow){
                        // A black/brown translucent shadow composited onto known pure green has no opaque core.
                        // Solve its matte coverage from NEW RGB, then remove the known green contribution.
                        int a=Math.Max(0,Math.Min(255,255-painted[i+1]+Math.Max(painted[i],painted[i+2])));
                        clean[i+3]=(byte)(a*painted[i+3]/255);
                        if(a>0){clean[i]=(byte)Math.Min(255,painted[i]*255/a);clean[i+2]=(byte)Math.Min(255,painted[i+2]*255/a);clean[i+1]=(byte)Math.Max(0,Math.Min(255,(painted[i+1]-(255-a))*255/a));}
                        else keyed++;
                        continue;
                    }
                    int rgb=(dominance>12||painted[i+3]<32)?nearest[p]*4:i;
                    if(rgb!=i)edgeRgbFilled++;
                    if(alpha==0)keyed++;
                    clean[i]=painted[rgb];clean[i+1]=painted[rgb+1];clean[i+2]=painted[rgb+2];clean[i+3]=(byte)alpha;
                }
                byte[] packed=Packed(clean,target);int paintedPixels=0,cleared=0,changedRgb=0,sourceAlphaSum=0,outputAlphaSum=0,overlaps=0;
                for(int p=0;p<mask.Length;p++) {
                    if(!mask[p])continue;
                    int x=target.x+p%target.PhysicalWidth,y=target.y+p/target.PhysicalWidth,i=(y*aw+x)*4,j=p*4;
                    int owner=y*aw+x;
                    if(ownership[owner]!=0&&ownership[owner]!=cell.index){
                        // One source portrait texel is shared by the brow/shadow AA tails (alpha=2).
                        // Resolve only sub-4-alpha ambiguity to the smaller UV rectangle; never merge opaque parts.
                        if(crop[j+3]>=4)throw new Exception("Nontrivial source face-island ownership overlap: "+cell.name);
                        overlaps++;var previous=layout.cells.Single(c=>c.index==ownership[owner]);
                        if(previous.region.width*previous.region.height<=cell.region.width*cell.region.height)continue;
                    }
                    ownership[owner]=cell.index;
                    int a=preserveSourceCoverage?crop[j+3]:Math.Min(crop[j+3],packed[j+3]);
                    // New paint supplies even transparent-edge RGB. Only source alpha/UV ownership is consulted.
                    output[i]=packed[j];output[i+1]=packed[j+1];output[i+2]=packed[j+2];output[i+3]=(byte)a;
                    if(a==0)cleared++;else paintedPixels++;
                    if(original[i]!=output[i]||original[i+1]!=output[i+1]||original[i+2]!=output[i+2])changedRgb++;
                    sourceAlphaSum+=crop[j+3];outputAlphaSum+=a;
                }
                totalPainted+=paintedPixels;totalCleared+=cleared;totalOverlap+=overlaps;
                reports.Add(new {name=cell.name,newVisiblePixels=paintedPixels,clearedSourcePixels=cleared,changedRgbPixels=changedRgb,greenKeyedCellPixels=keyed,newPaintEdgeRgbFilled=edgeRgbFilled,sourceAlphaSum=sourceAlphaSum,outputAlphaSum=outputAlphaSum,alphaCoverageRatio=sourceAlphaSum==0?0:(double)outputAlphaSum/sourceAlphaSum,overlappingOwnershipPixels=overlaps,sourceRgbFallbackPixels=0});
            }
        }
        int unrelatedChanges=0;for(int p=0;p<ownership.Length;p++)if(ownership[p]==0)for(int c=0;c<4;c++)if(output[p*4+c]!=original[p*4+c]){unrelatedChanges++;break;}
        if(unrelatedChanges!=0)throw new Exception("Unrelated atlas pixels changed.");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)));
        using(var image=Image(output,aw,ah))image.Save(outputPath,ImageFormat.Png);
        byte[] persisted=Read(outputPath,aw,ah,false);int persistedDifferences=0;for(int i=0;i<output.Length;i++)if(output[i]!=persisted[i])persistedDifferences++;
        if(persistedDifferences>0)throw new Exception("Saved PNG did not preserve packed RGBA bytes: "+persistedDifferences);
        string report=Serialize(new {layout=Path.GetFullPath(layoutPath),paintedBoard=Path.GetFullPath(paintedBoardPath),paintedBoardSha256=Sha256(paintedBoardPath),baseAtlas=Path.GetFullPath(baseAtlasPath),baseAtlasSha256=Sha256(baseAtlasPath),sourceAlphaMask=Path.GetFullPath(sourceMaskPath),sourceAlphaMaskSha256=Sha256(sourceMaskPath),targetAtlas=targetAtlasPath,output=Path.GetFullPath(outputPath),outputSha256=Sha256(outputPath),width=aw,height=ah,sourceRgbFallbackPixels=0,unrelatedAtlasPixelsChanged=unrelatedChanges,savedPngRgbaByteDifferences=persistedDifferences,sub4AlphaOwnershipAmbiguitiesResolved=totalOverlap,totalNewVisiblePixels=totalPainted,totalClearedSourcePixels=totalCleared,preserveTechnicalShadows=preserveTechnicalShadows,alphaPolicy=preserveSourceCoverage?"source alpha coverage, all RGB from new paint via nearest-edge fill":"new keyed paint alpha clipped to source UV island alpha",regions=reports});
        File.WriteAllText(Path.ChangeExtension(outputPath,".pack-report.json"),report,new UTF8Encoding(false));return report;
    }
}
