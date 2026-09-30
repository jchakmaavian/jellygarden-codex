using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// Technical texture integration: generated surface painting is registered to the
// APK atlas. Original UV rectangles and alpha silhouettes are retained, so no
// mesh weights, clipping geometry, attachment pivots or animation keys change.
public static class PackPaintedAtlas {
    class Region { public string Name; public int X,Y,W,H; public bool Rotate; }
    static byte[] Read(string path,int w,int h) {
        using(var source=new Bitmap(path)) using(var image=new Bitmap(w,h,PixelFormat.Format32bppArgb)) {
            using(var g=Graphics.FromImage(image)) { g.CompositingMode=CompositingMode.SourceCopy; g.InterpolationMode=InterpolationMode.HighQualityBicubic; g.PixelOffsetMode=PixelOffsetMode.HighQuality; g.DrawImage(source,new Rectangle(0,0,w,h),0,0,source.Width,source.Height,GraphicsUnit.Pixel); }
            var data=image.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            var bytes=new byte[w*h*4]; Marshal.Copy(data.Scan0,bytes,0,bytes.Length); image.UnlockBits(data);return bytes;
        }
    }
    static bool Matte(byte[] pixels,int i) { int b=pixels[i],g=pixels[i+1],r=pixels[i+2];return g-Math.Max(r,b)>24 && g>75; }
    static void Straight(byte[] pixels,int i) { int a=pixels[i+3];if(a>0 && a<255) for(int c=0;c<3;c++) pixels[i+c]=(byte)Math.Min(255,(pixels[i+c]*255+a/2)/a); }
    static bool[] Island(byte[] source,int width,int height,int x0,int y0,int rw,int rh) {
        var visited=new bool[rw*rh];var best=new List<int>();
        for(int local=0;local<visited.Length;local++) {
            int px=x0+local%rw,py=y0+local/rw;
            if(visited[local]||px>=width||py>=height||source[(py*width+px)*4+3]<32)continue;
            var island=new List<int>();var queue=new Queue<int>();visited[local]=true;queue.Enqueue(local);
            while(queue.Count>0) {
                int c=queue.Dequeue();island.Add(c);int x=c%rw,y=c/rw;
                foreach(int d in new[]{-1,1,-rw,rw}) {
                    int n=c+d;if(n<0||n>=visited.Length||visited[n])continue;
                    int nx=n%rw,ny=n/rw;if(Math.Abs(nx-x)+Math.Abs(ny-y)!=1||x0+nx>=width||y0+ny>=height)continue;
                    if(source[((y0+ny)*width+x0+nx)*4+3]<32)continue;visited[n]=true;queue.Enqueue(n);
                }
            }
            if(island.Count>best.Count)best=island;
        }
        var result=new bool[rw*rh];foreach(int c in best) {
            int x=c%rw,y=c/rw;
            for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++){int nx=x+dx,ny=y+dy;if(nx>=0&&ny>=0&&nx<rw&&ny<rh)result[ny*rw+nx]=true;}
        }
        return result;
    }
    public static string Pack(string sourcePath,string paintPath,string atlasPath,string outputPath) {
        var lines=File.ReadAllLines(atlasPath);int width=0,height=0;var regions=new List<Region>();Region region=null;
        foreach(var line in lines) {
            if(line.StartsWith("size:")){var m=Regex.Match(line,@"(\d+),\s*(\d+)");width=int.Parse(m.Groups[1].Value);height=int.Parse(m.Groups[2].Value);}
            if(line.Length>0 && !char.IsWhiteSpace(line[0]) && !line.Contains(":") && !line.EndsWith(".png")){region=new Region{Name=line};regions.Add(region);}
            if(region==null)continue;
            var values=Regex.Match(line,@"(\d+),\s*(\d+)");
            if(line.TrimStart().StartsWith("xy:")&&values.Success){region.X=int.Parse(values.Groups[1].Value);region.Y=int.Parse(values.Groups[2].Value);}
            if(line.StartsWith("  size:")&&values.Success){region.W=int.Parse(values.Groups[1].Value);region.H=int.Parse(values.Groups[2].Value);}
            if(line.TrimStart().StartsWith("rotate:"))region.Rotate=line.Contains("true")||line.Contains("90")||line.Contains("270");
        }
        var source=Read(sourcePath,width,height);var paint=Read(paintPath,width,height);var output=(byte[])source.Clone();
        for(int i=0;i<output.Length;i+=4)Straight(output,i); // packaged reference textures use PMA
        int changed=0,repaired=0,fallback=0;var report=new List<string>();
        foreach(var r in regions) {
            // Only crown and costume surface islands are replaced. The face,
            // Sakal Surat (beard/face), Bıyık (moustache), eye/cheek/lash/shadow
            // islands retain source registration for the original deform timelines.
            bool costume=r.Name=="Pelerin" || r.Name=="Taç" || r.Name=="el_OK" || r.Name=="govde" || r.Name.StartsWith("buyun_") || r.Name.Contains("arms_") || r.Name.Contains("_omuz") || r.Name.StartsWith("sag_kol_") || r.Name.StartsWith("sol_kol_") || r.Name=="00300_A_left_arm" || r.Name=="00400_A_body" || r.Name=="02600_A_right_arm_bottom" || r.Name=="03300_A_right_arm" || r.Name=="03500_A_hat" || r.Name.StartsWith("drill_costume_");
            if(!costume)continue;
            int rw=r.Rotate?r.H:r.W,rh=r.Rotate?r.W:r.H,count=0;
            // Tight atlas packing allows other facial islands inside the bounding
            // rectangle's transparent corners. Never overwrite those neighboring parts.
            var islandMask=Island(source,width,height,r.X,r.Y,rw,rh);
            for(int y=r.Y;y<Math.Min(height,r.Y+rh);y++)for(int x=r.X;x<Math.Min(width,r.X+rw);x++) {
                if(!islandMask[(y-r.Y)*rw+x-r.X])continue;
                int i=(y*width+x)*4;if(source[i+3]<4){output[i+3]=0;continue;}int p=i;
                // A generated matte gap is missing paint, not missing source geometry.
                // Restore the source pixel in straight alpha so facial/costume meshes
                // keep their original coverage instead of acquiring transparent holes.
                if(Matte(paint,p)) {
                    output[i]=source[i];output[i+1]=source[i+1];output[i+2]=source[i+2];output[i+3]=source[i+3];
                    Straight(output,i);fallback++;continue;
                }
                output[i]=paint[p];output[i+1]=paint[p+1];output[i+2]=paint[p+2];count++;changed++;
            }
            report.Add(r.Name+": "+count);
        }
        // Transparent texels must not carry a matte or publisher-colored fringe.
        for(int i=0;i<output.Length;i+=4)if(output[i+3]==0)output[i]=output[i+1]=output[i+2]=0;
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        using(var image=new Bitmap(width,height,PixelFormat.Format32bppArgb)) {
            var data=image.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);Marshal.Copy(output,0,data.Scan0,output.Length);image.UnlockBits(data);image.Save(outputPath,ImageFormat.Png);
        }
        return width+"x"+height+"; painted texels="+changed+"; edge matte repairs="+repaired+"; source edge fallback="+fallback+
            "\nFacial policy: source face, Sakal Surat and Bıyık preserved; crown and costume painted.\n"+string.Join("\n",report.ToArray());
    }

    // Compare against the same source-to-atlas registration used by Pack. The
    // existing sub-4-alpha technical edge cleanup is the only allowed difference.
    public static string VerifyAlpha(string sourcePath,string outputPath) {
        int width,height;byte[] output;
        using(var image=new Bitmap(outputPath)) {
            width=image.Width;height=image.Height;
            var data=image.LockBits(new Rectangle(0,0,width,height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            output=new byte[width*height*4];Marshal.Copy(data.Scan0,output,0,output.Length);image.UnlockBits(data);
        }
        var source=Read(sourcePath,width,height);
        int equal=0,different=0,technical=0,unexpected=0,addedCoverage=0,maxDifference=0;
        for(int i=0;i<source.Length;i+=4) {
            int a=source[i+3],b=output[i+3];
            if(a==b){equal++;continue;}
            different++;maxDifference=Math.Max(maxDifference,Math.Abs(a-b));
            if(a<4 && b==0)technical++;else unexpected++;
            if(b>a)addedCoverage++;
        }
        return "{\"width\":"+width+",\"height\":"+height+",\"pixels\":"+(width*height)+
            ",\"equal_alpha_pixels\":"+equal+",\"different_alpha_pixels\":"+different+
            ",\"original_alpha_below_4_cleaned_to_zero\":"+technical+
            ",\"unexpected_alpha_differences\":"+unexpected+",\"added_alpha_coverage_pixels\":"+addedCoverage+
            ",\"max_alpha_difference\":"+maxDifference+"}";
    }
}
