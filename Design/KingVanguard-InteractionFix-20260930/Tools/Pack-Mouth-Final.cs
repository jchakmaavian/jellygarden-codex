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

public static class NaturalMouthPacker {
    static object Call(string n,params object[] a){return typeof(FaceWorkboard).GetMethod(n,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,a);}
    static JavaScriptSerializer Json(){return new JavaScriptSerializer{MaxJsonLength=int.MaxValue};}
    static byte[] Read(string p,int w,int h){return (byte[])Call("Read",p,w,h,false);}
    static string Sha(string p){using(var h=SHA256.Create())using(var f=File.OpenRead(p))return BitConverter.ToString(h.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
    static IEnumerable<int> Neighbors(int p,int w,int h){int x=p%w,y=p/w;if(x>0)yield return p-1;if(x<w-1)yield return p+1;if(y>0)yield return p-w;if(y<h-1)yield return p+w;}
    static bool Core(byte[] p,int i){int b=p[i],g=p[i+1],r=p[i+2];return p[i+3]>=250&&g>=70&&b>=40&&r-g>=5&&r-g<=100&&g-b>=0&&g-b<=90;}
    public static string Run(string task,string reference,string rig){
        string dir=Path.Combine(task,"FaceRedesign/PackedFinal");Directory.CreateDirectory(dir);
        string baseline=Path.Combine(task,"FaceRedesign/PackedClean/"+rig+"_Vanguard.png"),sourcePath=Path.Combine(reference,"Original-rfv387/"+rig+".png"),atlasPath=Path.Combine(reference,"Original-rfv387/"+rig+".atlas"),paintPath=Path.Combine(task,"FaceRedesign/Mouth-natural-peach.png");
        string expected=rig=="King"?"ad304a503c4558e395cc890af073fae41b1ca9cd4cb73f4c25b2933c51a3c675":"01698a75def1e6272470ca1c348e2763e9a8cf9e60f394313a68053229178318";
        if(Sha(baseline)!=expected)throw new Exception("PackedClean baseline changed: "+rig);
        int w,h;var regions=FaceWorkboard.ReadAtlas(atlasPath,out w,out h);byte[] original=Read(baseline,w,h),source=Read(sourcePath,w,h),output=(byte[])original.Clone();
        var layout=Json().Deserialize<FaceWorkboard.Layout>(File.ReadAllText(Path.Combine(reference,"FaceRedesign/Portrait/Portrait-layout.json"),Encoding.UTF8));
        var cell=layout.cells.Single(v=>v.index==20);var r=regions.Single(v=>v.name==cell.name);
        byte[] crop=(byte[])Call("Crop",source,w,h,r);bool[] mask=(bool[])Call("LargestIsland",crop,r.PhysicalWidth,r.PhysicalHeight);
        byte[] paint=Read(paintPath,r.width,r.height);int count=r.width*r.height;var core=new bool[count];
        for(int p=0;p<count;p++)core[p]=Core(paint,p*4);
        var nearest=Enumerable.Repeat(-1,count).ToArray();var queue=new Queue<int>();int seeds=0;
        for(int p=0;p<count;p++)if(core[p]&&Neighbors(p,r.width,r.height).All(n=>core[n])){nearest[p]=p;queue.Enqueue(p);seeds++;}
        if(seeds<200)throw new Exception("Insufficient new warm opaque eroded paint: "+seeds);
        while(queue.Count>0){int p=queue.Dequeue();foreach(int n in Neighbors(p,r.width,r.height))if(nearest[n]<0){nearest[n]=nearest[p];queue.Enqueue(n);}}
        byte[] logical=new byte[paint.Length];for(int p=0;p<count;p++){int from=nearest[p]*4;for(int c=0;c<3;c++)logical[p*4+c]=paint[from+c];logical[p*4+3]=255;}
        byte[] packed=(byte[])Call("Packed",logical,r);var allowed=new bool[w*h];int maskCount=0;
        for(int p=0;p<mask.Length;p++)if(mask[p]){int at=(r.y+p/r.PhysicalWidth)*w+r.x+p%r.PhysicalWidth;allowed[at]=true;for(int c=0;c<3;c++)output[at*4+c]=packed[p*4+c];output[at*4+3]=crop[p*4+3];maskCount++;}
        int outside=0,alpha=0,total=0,sourceAlpha=0;for(int p=0;p<allowed.Length;p++){int i=p*4;bool diff=false;for(int c=0;c<4;c++)if(output[i+c]!=original[i+c])diff=true;if(diff){total++;if(!allowed[p])outside++;}if(output[i+3]!=original[i+3])alpha++;if(allowed[p]&&output[i+3]!=source[i+3])sourceAlpha++;}
        if(outside!=0||alpha!=0||sourceAlpha!=0)throw new Exception("Mouth preservation failed: outside="+outside+" baseline alpha="+alpha+" source alpha="+sourceAlpha);
        string path=Path.Combine(dir,rig+"_Vanguard.png");using(var bmp=(Bitmap)Call("Image",output,w,h))bmp.Save(path,ImageFormat.Png);
        byte[] saved=Read(path,w,h);int roundtrip=0;for(int i=0;i<saved.Length;i++)if(saved[i]!=output[i])roundtrip++;if(roundtrip!=0)throw new Exception("PNG roundtrip mismatch");
        byte[] finalCrop=(byte[])Call("Crop",output,w,h,r);for(int p=0;p<mask.Length;p++)if(!mask[p])finalCrop[p*4+3]=0;byte[] preview=(byte[])Call("Upright",finalCrop,r);using(var bmp=(Bitmap)Call("Image",preview,r.width,r.height))bmp.Save(Path.Combine(dir,rig+"-mouth-preview.png"),ImageFormat.Png);
        var report=new{output=path,outputSha256=Sha(path),baseline=baseline,baselineSha256=Sha(baseline),newPaint=paintPath,newPaintSha256=Sha(paintPath),region=cell.name,method="Full generated canvas resized to native logical region. RGB sampled only from opaque one-pixel-eroded warm peach paint, nearest filled through source UV island. Exact original source alpha retained; no original facial RGB fallback.",width=w,height=h,logicalWidth=r.width,logicalHeight=r.height,cleanNewPaintSeeds=seeds,sourceIslandPixels=maskCount,changedRgbPixels=total,outsideMouthIslandChanged=outside,baselineAlphaByteDifferences=alpha,originalSourceAlphaDifferences=sourceAlpha,sourceRgbFallbackPixels=0,savedPngByteDifferences=roundtrip};
        string json=Json().Serialize(report);File.WriteAllText(Path.ChangeExtension(path,".mouth-report.json"),json,new UTF8Encoding(false));return json;
    }
}
}
