// 512px / 60fps authored Color Ball clips. Original pixels supply every color band.
// No invented back-face dots or recolored decals are used by this renderer.
using System;
using System.IO;
using System.Collections.Generic;

public static class ColorBallMotion
{
    public const int RevealFrames=60, IdleFrames=120, BoostFrames=108;
    public const int LoopStart=18, LoopLength=60, FinishStart=78;
    public const double Cell=112;
    static double sourceWidth;
    static readonly Dictionary<int,Img> spinCache=new Dictionary<int,Img>();
    static double Ease(double a,double b,double t){t=Math.Max(0,Math.Min(1,(t-a)/(b-a)));return t*t*(3-2*t);}
    static double Mix(double a,double b,double t){return a+(b-a)*t;}
    static double Wrap(double x){return x-Math.Floor(x);}

    static void Prepare()
    {
        if(sourceWidth>0)return;
        int lo=512,hi=0;
        for(int y=0;y<512;y++)for(int x=0;x<512;x++)if(BallRig.Src.A[y*512+x]>.5f){lo=Math.Min(lo,x);hi=Math.Max(hi,x);}
        sourceWidth=hi-lo+1;
        CB.Kc=Cell/sourceWidth;
    }

    // Latitude-preserving horizontal smear of the ORIGINAL dot cutout. Each row is
    // wrapped once and sampled across the shutter; navy illumination stays fixed.
    static Img Spin(double phase)
    {
        int key=(int)Math.Round(Wrap(phase)*10000)%10000;
        Img cached;if(spinCache.TryGetValue(key,out cached))return cached;
        var dst=BallRig.Body.Clone();var dots=BallRig.StudLayer;
        for(int y=0;y<512;y++)
        {
            int l=512,r=0;
            for(int x=0;x<512;x++)if(BallRig.Src.A[y*512+x]>.15f){l=Math.Min(l,x);r=Math.Max(r,x);}
            double width=r-l+1;if(width<2)continue;
            for(int x=l;x<=r;x++)
            {
                int i=y*512+x;float ba=dst.A[i];if(ba<.005f)continue;
                float rr=0,gg=0,bb=0,aa=0;
                for(int k=0;k<32;k++)
                {
                    double u=Wrap((x-l+.5)/width+phase+(k-15.5)/32*.42);
                    float sr,sg,sb,sa;dots.Sample(l+u*width,y+.5,out sr,out sg,out sb,out sa);
                    rr+=sr;gg+=sg;bb+=sb;aa+=sa;
                }
                if(aa<.001f)continue;
                float coverage=Math.Min(1,aa/32*2.7f)*ba;
                // Preserve hue, bring the original specular colors up slightly.
                dst.Over(i,Math.Min(1,rr/aa*1.10f)*coverage,Math.Min(1,gg/aa*1.10f)*coverage,Math.Min(1,bb/aa*1.10f)*coverage,coverage);
            }
        }
        if(spinCache.Count<180)spinCache[key]=dst;
        return dst;
    }

    static void Ball(Img dst,double scale,double flat,double phase,double smear,double dy)
    {
        Img tex=smear<=0?BallRig.Src:smear>=1?Spin(phase):RevealSeq.LerpMax(BallRig.Src,Spin(phase),(float)smear);
        double footprint=Mix(1,sourceWidth/(2*BallRig.R),smear);
        Fx.DrawLayer(dst,tex,CB.BallPose(scale*footprint,1,flat,0,0,dy),1,2);
    }
    static void Halo(Img dst,double size,double flat,double power,double dy)
    {
        CB.WhiteGlow(dst,256,256+dy,Cell*size*.65,power,Math.Max(.56,flat));
        CB.TightGlow(dst,256,256+dy,Cell*size*.44,Cell*size*.70,power*1.45,Math.Max(.63,flat));
    }
    static void Stars(Img dst,double phase,double size,double strength,double dy)
    {
        for(int k=0;k<9;k++)
        {
            double u=Wrap(phase+k*.137),v=Math.Pow(Math.Sin(u*Math.PI),3)*strength;
            double angle=k*2.39996,rad=Cell*size*(.47+.08*Math.Sin(k*4.8));
            CB.FatStar(dst,256+Math.Cos(angle)*rad,256+dy+Math.Sin(angle)*rad*.58,Cell*(.035+.028*(k%3))*v,k*27+u*24,.95,k%3==0?.48:.95);
        }
    }
    static Img Frame(string sequence,int f)
    {
        var dst=new Img(512,512);
        if(sequence=="Idle") { Ball(dst,1,1,0,0,0);return dst; }
        if(sequence=="Reveal")
        {
            double appear=Ease(2,12,f),settle=Ease(19,53,f);
            double size=Mix(.18,3,appear)*(1-settle)+settle;
            double flat=Mix(.30,1,Ease(29,53,f)),dy=-38*(1-Ease(12,49,f));
            double smear=1-Ease(33,53,f),light=Ease(0,7,f)*(1-Ease(35,53,f));
            double remain=Math.Max(0,53-f),phase=f<33?(.13*(f-33)-.8666667):(-.8666667*Math.Pow(remain/20,3));
            Halo(dst,size,flat,light*.38,dy);
            if(f>=2)Ball(dst,size,flat,phase,smear,dy);
            Stars(dst,f/60.0,size,light,dy);
            if(f<8)CB.FatStar(dst,256,226,11*Math.Sin(Math.PI*f/8),10,1);
            return dst;
        }
        if(f<FinishStart)
        {
            double charge=Ease(0,18,f),size=Mix(1,3,charge),flat=Mix(1,.42,charge);
            double phase=f<18?.1*f*f/36:.9+.1*(f-18);
            double cycle=2*Math.PI*(f-18)/60;
            double glow=charge*(.40+.025*Math.Sin(cycle));
            Halo(dst,size,flat,glow,0);
            Ball(dst,size,flat,phase,Ease(0,13,f),0);
            // Compact white-hot center; never wash out the surrounding navy/bands.
            CB.WhiteGlow(dst,256,256,Cell*.21,charge*.92,.57);
            double flash=charge*(.50+.14*Math.Sin(cycle*2));
            CB.Spike(dst,256,256,0,Cell*1.03,1.6,flash);
            CB.Spike(dst,256,256,90,Cell*1.36,1.4,flash*.82);
            Stars(dst,(f-18)/60.0,size,charge,0);
            return dst;
        }
        int g=f-FinishStart;
        double fade=1-Ease(0,12,g),ring=Ease(0,5,g)*(1-Ease(8,29,g));
        if(g==0)return Frame("Boosting",LoopStart); // finish starts at the loop boundary
        Halo(dst,3,.42,.4*fade,0);
        if(fade>0){var ball=new Img(512,512);Ball(ball,3,.42,.9+.1*g,1,0);Fx.DrawLayer(dst,ball,Affine.Identity,(float)fade,1);}
        CB.WhiteGlow(dst,256,256,Cell*(.23+.10*Ease(0,6,g)),fade*.9,.65);
        CB.ColorRing(dst,256,256,Cell*Mix(.5,1.9,Ease(0,29,g)),3.0,ring*.65,new[]{1f,.97f,.82f},.76,0);
        Stars(dst,g/60.0,3,fade,0);
        return dst;
    }
    public static void Render(string sequence,string dir,int from,int to)
    {
        Prepare();Directory.CreateDirectory(dir);
        for(int f=from;f<=to;f++)CB.Save(Frame(sequence,f),dir,"ColorBall_"+sequence,f);
    }
}
