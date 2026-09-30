// Compile after the existing FaceWorkboard source. Outputs evidence only.
namespace KingVanguardTechnical {
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

public static class TeethShadowPack {
    static JavaScriptSerializer Json() { return new JavaScriptSerializer { MaxJsonLength=int.MaxValue, RecursionLimit=100 }; }
    static object Call(string method,params object[] args) { return typeof(FaceWorkboard).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args); }
    static byte[] Read(string path,int w,int h) { return (byte[])Call("Read",path,w,h,false); }
    static byte[] Crop(byte[] pixels,int w,int h,FaceWorkboard.Region r) { return (byte[])Call("Crop",pixels,w,h,r); }
    static bool[] Mask(byte[] pixels,FaceWorkboard.Region r) { return (bool[])Call("LargestIsland",pixels,r.PhysicalWidth,r.PhysicalHeight); }
    static string Sha(string path) { using(var algorithm=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-","").ToLowerInvariant(); }
    static int Median(IEnumerable<int> values) { var a=values.OrderBy(v=>v).ToArray();return a[a.Length/2]; }
    struct Sample { public int b,g,r; public double luma; }
    class CoreColor { public int[] rgb; public int candidates,darkCore; public int sourceAlphaThreshold; }
    static CoreColor BrownCore(Bitmap board,FaceWorkboard.Layout layout,FaceWorkboard.Cell cell,byte[] sourceCrop,FaceWorkboard.Region target) {
        byte[] paint=(byte[])Call("PaintedCell",board,layout,cell);
        byte[] source=(byte[])Call("Upright",sourceCrop,target);
        int maxAlpha=0;for(int i=3;i<source.Length;i+=4)maxAlpha=Math.Max(maxAlpha,source[i]);
        int threshold=Math.Min(128,Math.Max(32,(int)Math.Round(maxAlpha*.65)));
        var candidates=new List<Sample>();
        for(int i=0;i<paint.Length;i+=4){
            int b=paint[i],g=paint[i+1],r=paint[i+2];
            // Select warm dark paint cores; never key/unmix green to infer alpha.
            // Source pixels contribute alpha only, never any RGB.
            if(source[i+3]<threshold||paint[i+3]<240||r-g<18||r-b<25||g-b<7||r<40||r>215||g>140)continue;
            candidates.Add(new Sample{b=b,g=g,r=r,luma=.2126*r+.7152*g+.0722*b});
        }
        if(candidates.Count<8)throw new Exception("Too few uncontaminated generated brown-core texels: "+cell.name+" / "+candidates.Count);
        var core=candidates.OrderBy(p=>p.luma).Take(Math.Max(8,candidates.Count/2)).ToArray();
        return new CoreColor{rgb=new[]{Median(core.Select(p=>p.r)),Median(core.Select(p=>p.g)),Median(core.Select(p=>p.b))},candidates=candidates.Count,darkCore=core.Length,sourceAlphaThreshold=threshold};
    }

    public static string Pack(string layoutPath,string fullLayoutPath,string boardPath,string baselinePath,string sourceMaskPath,string atlasPath,string outputPath) {
        string outFull=Path.GetFullPath(outputPath).Replace('\\','/');
        if(outFull.IndexOf("/Design/KingVanguard-InteractionFix-20260930/FaceRedesign/Packed/",StringComparison.OrdinalIgnoreCase)<0||outFull.IndexOf("/Assets/",StringComparison.OrdinalIgnoreCase)>=0)throw new Exception("Review output path only: "+outFull);
        var layout=Json().Deserialize<FaceWorkboard.Layout>(File.ReadAllText(layoutPath,Encoding.UTF8));
        if(layout.cells.Count!=7||layout.cells[0].name!="Alt Diş"||layout.cells[1].name!="Ust Diş")throw new Exception("Unexpected seven-cell layout");
        var teethLayout=Json().Deserialize<FaceWorkboard.Layout>(File.ReadAllText(layoutPath,Encoding.UTF8));
        teethLayout.cells=teethLayout.cells.Take(2).ToList();
        string narrowLayout=Path.Combine(Path.GetDirectoryName(outputPath),"TeethOnly-layout.json");
        File.WriteAllText(narrowLayout,Json().Serialize(teethLayout),new UTF8Encoding(false));
        string teethOutput=Path.Combine(Path.GetDirectoryName(outputPath),Path.GetFileNameWithoutExtension(outputPath)+"-teeth-only.png");
        FaceWorkboard.Pack(narrowLayout,boardPath,baselinePath,sourceMaskPath,atlasPath,teethOutput,false,true);
        int w,h;var regions=FaceWorkboard.ReadAtlas(atlasPath,out w,out h);
        byte[] original=Read(baselinePath,w,h),source=Read(sourceMaskPath,w,h),output=Read(teethOutput,w,h);
        var selected=new bool[w*h];var reports=new List<object>();
        using(var board=new Bitmap(boardPath))foreach(var cell in layout.cells){
            var target=regions.Single(r=>r.name==cell.name);
            if(target.width!=cell.region.width||target.height!=cell.region.height)throw new Exception("Shared region dimensions differ: "+cell.name);
            byte[] crop=Crop(source,w,h,target);bool[] mask=Mask(crop,target);
            CoreColor brown=cell.index>2?BrownCore(board,layout,cell,crop,target):null;
            int count=0,changed=0;long alphaSum=0;
            for(int p=0;p<mask.Length;p++){
                if(!mask[p])continue;
                int at=(target.y+p/target.PhysicalWidth)*w+target.x+p%target.PhysicalWidth;
                selected[at]=true;count++;alphaSum+=crop[p*4+3];int i=at*4;
                if(brown!=null){output[i]=(byte)brown.rgb[2];output[i+1]=(byte)brown.rgb[1];output[i+2]=(byte)brown.rgb[0];output[i+3]=crop[p*4+3];}
                if(output[i]!=original[i]||output[i+1]!=original[i+1]||output[i+2]!=original[i+2]||output[i+3]!=original[i+3])changed++;
            }
            reports.Add(new{name=cell.name,cell=cell.index,originalAlphaIslandPixels=count,originalAlphaSum=alphaSum,changedPixels=changed,
                rgbSource=brown==null?"generated tooth paint, nearest new-paint edge fill":"generated brown-core median only",generatedBrownCore=brown,
                originalRgbFallbackPixels=0,shadowOpacityBaked=false});
        }
        int sourceAlphaDifferences=0;
        foreach(var cell in layout.cells){var r=regions.Single(v=>v.name==cell.name);var crop=Crop(source,w,h,r);var mask=Mask(crop,r);for(int p=0;p<mask.Length;p++)if(mask[p]){int at=(r.y+p/r.PhysicalWidth)*w+r.x+p%r.PhysicalWidth;if(output[at*4+3]!=crop[p*4+3])sourceAlphaDifferences++;}}
        int changedPixels=0,outside=0;for(int p=0;p<selected.Length;p++){int i=p*4;bool diff=false;for(int c=0;c<4;c++)if(output[i+c]!=original[i+c])diff=true;if(diff){changedPixels++;if(!selected[p])outside++;}}
        var fullLayout=Json().Deserialize<FaceWorkboard.Layout>(File.ReadAllText(fullLayoutPath,Encoding.UTF8));
        var selectedNames=new HashSet<string>(layout.cells.Select(c=>c.name));var sharedChanges=new List<object>();int mouthChanged=0;
        foreach(var cell in fullLayout.cells){
            if(selectedNames.Contains(cell.name))continue;
            var r=regions.Single(v=>v.name==cell.name);var crop=Crop(source,w,h,r);var mask=Mask(crop,r);
            for(int p=0;p<mask.Length;p++)if(mask[p]){int x=r.x+p%r.PhysicalWidth,y=r.y+p/r.PhysicalWidth,at=y*w+x,i=at*4;bool diff=false;for(int c=0;c<4;c++)if(output[i+c]!=original[i+c])diff=true;if(diff){sharedChanges.Add(new{name=cell.name,x=x,y=y,sourceAlpha=crop[p*4+3],alsoSelected=selected[at]});if(cell.name=="Ağız")mouthChanged++;}}
        }
        if(sourceAlphaDifferences!=0||outside!=0||mouthChanged!=0)throw new Exception("Scope/alpha verification failed: alpha="+sourceAlphaDifferences+" outside="+outside+" mouth="+mouthChanged);
        // Atlas packing can share sub-4-alpha tails between adjacent UV regions.
        // Any nonselected opaque pixel change is forbidden.
        foreach(var overlap in sharedChanges){var t=overlap.GetType();if((byte)t.GetProperty("sourceAlpha").GetValue(overlap,null)>=4||!(bool)t.GetProperty("alsoSelected").GetValue(overlap,null))throw new Exception("Nonselected facial component changed outside shared AA tail");}
        using(var image=(Bitmap)Call("Image",output,w,h))image.Save(outputPath,ImageFormat.Png);
        byte[] roundtrip=Read(outputPath,w,h);int roundtripDifferences=0;for(int i=0;i<output.Length;i++)if(output[i]!=roundtrip[i])roundtripDifferences++;
        if(roundtripDifferences!=0)throw new Exception("PNG roundtrip mismatch");
        var report=new{output=Path.GetFullPath(outputPath),outputSha256=Sha(outputPath),baseline=baselinePath,baselineSha256=Sha(baselinePath),sourceMask=sourceMaskPath,sourceMaskSha256=Sha(sourceMaskPath),
            paintedBoard=boardPath,paintedBoardSha256=Sha(boardPath),atlas=atlasPath,width=w,height=h,changedPixels=changedPixels,outsideSelectedIslandsChanged=outside,sourceAlphaDifferences=sourceAlphaDifferences,
            unselectedMouthChanged=mouthChanged,sharedSub4AlphaTailChanges=sharedChanges,savedPngByteDifferences=roundtripDifferences,originalRgbFallbackPixels=0,regions=reports};
        string json=Json().Serialize(report);File.WriteAllText(Path.ChangeExtension(outputPath,".pack-report.json"),json,new UTF8Encoding(false));return json;
    }
}
}
