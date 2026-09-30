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

public static class MatteEdgeCleanup {
    static object Call(string n,params object[] a){return typeof(FaceWorkboard).GetMethod(n,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,a);}
    static JavaScriptSerializer Json(){return new JavaScriptSerializer{MaxJsonLength=int.MaxValue};}
    static byte[] Read(string p,int w,int h){return (byte[])Call("Read",p,w,h,false);}
    static string Sha(string p){using(var h=SHA256.Create())using(var f=File.OpenRead(p))return BitConverter.ToString(h.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
    static bool Spill(byte[] p,int i){int b=p[i],g=p[i+1],r=p[i+2];return g-b>=18&&(g>=r-12||2*g-r-b>24);}
    static bool CleanCore(byte[] p,int i){int b=p[i],g=p[i+1],r=p[i+2];return p[i+3]>=250&&r>=g&&r>=b&&2*g-r-b<=12&&!Spill(p,i);}
    static IEnumerable<int> Neighbors(int p,int w,int h){int x=p%w,y=p/w;if(x>0)yield return p-1;if(x<w-1)yield return p+1;if(y>0)yield return p-w;if(y<h-1)yield return p+w;}
    static int[] Nearest(byte[] paint,byte[] source,int w,int h,out int seedCount){
        var clean=new bool[w*h];for(int p=0;p<clean.Length;p++)clean[p]=source[p*4+3]>=128&&CleanCore(paint,p*4);
        var nearest=Enumerable.Repeat(-1,w*h).ToArray();var q=new Queue<int>();seedCount=0;
        for(int p=0;p<clean.Length;p++)if(clean[p]&&Neighbors(p,w,h).All(n=>clean[n])){nearest[p]=p;q.Enqueue(p);seedCount++;}
        if(seedCount<5)throw new Exception("Too few uncontaminated eroded new-paint seeds: "+seedCount);
        while(q.Count>0){int p=q.Dequeue();foreach(int n in Neighbors(p,w,h))if(nearest[n]<0){nearest[n]=nearest[p];q.Enqueue(n);}}
        return nearest;
    }
    public static string Run(string task,string reference,string rig){
        string dir=Path.Combine(task,"FaceRedesign/PackedClean");Directory.CreateDirectory(dir);
        string baseline=Path.Combine(task,"FaceRedesign/Packed/"+rig+"_Vanguard.png"),sourcePath=Path.Combine(reference,"Original-rfv387/"+rig+".png"),atlasPath=Path.Combine(reference,"Original-rfv387/"+rig+".atlas");
        int w,h;var regions=FaceWorkboard.ReadAtlas(atlasPath,out w,out h);byte[] original=Read(baseline,w,h),source=Read(sourcePath,w,h),output=(byte[])original.Clone();
        var face=Json().Deserialize<FaceWorkboard.Layout>(File.ReadAllText(Path.Combine(reference,"FaceRedesign/Portrait/Portrait-layout.json"),Encoding.UTF8));
        var teeth=Json().Deserialize<FaceWorkboard.Layout>(File.ReadAllText(Path.Combine(task,"Inspection/FaceRedesign/Source/TeethShadow-layout.json"),Encoding.UTF8));
        int[] faceIds={20,8,12,3,16,17};var reports=new List<object>();var allowed=new bool[w*h];
        using(var teethBoard=new Bitmap(Path.Combine(task,"FaceRedesign/TeethShadow-painted-workboard.png")))
        using(var faceBoard=new Bitmap(Path.Combine(reference,"FaceRedesign/Portrait/Portrait-painted-workboard.png")))
        using(var mouthBoard=new Bitmap(Path.GetFullPath(Path.Combine(task,"../KingVanguard-FaceRigFix-20260930/FaceRedesign/Portrait-lip-cover-workboard.png")))){
            var jobs=new List<Tuple<FaceWorkboard.Layout,FaceWorkboard.Cell,Bitmap>>();
            foreach(var c in teeth.cells.Take(2))jobs.Add(Tuple.Create(teeth,c,teethBoard));
            foreach(int id in faceIds){var c=face.cells.Single(v=>v.index==id);jobs.Add(Tuple.Create(face,c,id==20?mouthBoard:faceBoard));}
            foreach(var job in jobs){var layout=job.Item1;var cell=job.Item2;var board=job.Item3;var r=regions.Single(v=>v.name==cell.name);
                byte[] crop=(byte[])Call("Crop",source,w,h,r);bool[] mask=(bool[])Call("LargestIsland",crop,r.PhysicalWidth,r.PhysicalHeight);
                for(int p=0;p<mask.Length;p++)if(!mask[p])crop[p*4+3]=0;
                byte[] logicalSource=(byte[])Call("Upright",crop,r),paint=(byte[])Call("PaintedCell",board,layout,cell);
                int seeds;int[] nearest=Nearest(paint,logicalSource,r.width,r.height,out seeds);
                var clean=(byte[])paint.Clone();for(int p=0;p<nearest.Length;p++){int from=nearest[p]*4;for(int c=0;c<3;c++)clean[p*4+c]=paint[from+c];}
                byte[] packed=(byte[])Call("Packed",clean,r);int changed=0,visible=0;
                for(int p=0;p<mask.Length;p++)if(mask[p]){int at=(r.y+p/r.PhysicalWidth)*w+r.x+p%r.PhysicalWidth,i=at*4;allowed[at]=true;if(!Spill(output,i))continue;for(int c=0;c<3;c++)output[i+c]=packed[p*4+c];changed++;if(output[i+3]>=16)visible++;}
                reports.Add(new{name=cell.name,changedRgbPixels=changed,changedVisiblePixels=visible,uncontaminatedNewPaintSeeds=seeds,alphaChanged=0,originalRgbFallback=0});
            }
        }
        int outside=0,alphas=0,total=0,remaining=0;
        for(int p=0;p<allowed.Length;p++){int i=p*4;if(output[i+3]!=original[i+3])alphas++;bool changed=false;for(int c=0;c<4;c++)if(output[i+c]!=original[i+c])changed=true;if(changed){total++;if(!allowed[p])outside++;}if(allowed[p]&&output[i+3]>=16&&Spill(output,i))remaining++;}
        if(outside!=0||alphas!=0||remaining!=0)throw new Exception("Cleanup verification failed: outside="+outside+" alpha="+alphas+" remaining="+remaining);
        string path=Path.Combine(dir,rig+"_Vanguard.png");using(var bitmap=(Bitmap)Call("Image",output,w,h))bitmap.Save(path,ImageFormat.Png);
        byte[] saved=Read(path,w,h);int roundtrip=0;for(int i=0;i<saved.Length;i++)if(saved[i]!=output[i])roundtrip++;if(roundtrip!=0)throw new Exception("PNG roundtrip mismatch");
        var report=new{output=path,outputSha256=Sha(path),baseline=baseline,baselineSha256=Sha(baseline),method="Replace only G-B>=18 and (G>=R-12 or 2G-R-B>24) matte-contaminated RGB inside eight source UV alpha islands. Replacement is nearest eroded NEW-paint core with R>=G, R>=B, and 2G-R-B<=12. Installed alpha and all other colors are unchanged.",changedRgbPixels=total,outsideSelectedIslandsChanged=outside,alphaByteDifferences=alphas,remainingVisibleSpillPixels=remaining,savedPngByteDifferences=roundtrip,regions=reports};
        string json=Json().Serialize(report);File.WriteAllText(Path.ChangeExtension(path,".clean-report.json"),json,new UTF8Encoding(false));return json;
    }
}
}
