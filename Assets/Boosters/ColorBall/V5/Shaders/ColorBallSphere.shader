Shader "RoyalThrone/ColorBallV5/Sphere"
{
 Properties { _Layer("Body / Surface",Float)=0 _Opacity("Opacity",Float)=1 _Angle("Surface longitude",Float)=0 _Exposure("Angular shutter",Float)=0 _Charge("Charge",Float)=0 _Samples("Shutter samples",Range(1,24))=6 _Widen("Horizontal widen",Float)=1 _Core("White core",Range(0,1))=0 _Energy("Colour energy",Range(0,2))=0 }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
   float _Layer,_Opacity,_Angle,_Exposure,_Charge,_Samples,_Widen,_Core,_Energy,_MotionPhase; float4x4 _RigidPose;
   v2f vert(appdata_base v){v2f o;v.vertex.xy*=1.8;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;return o;}
   float3 candy(float k){
    k=fmod(k,6);
    if(k<1)return float3(1,.045,.12); if(k<2)return float3(.01,.66,1);
    if(k<3)return float3(1,.59,.015); if(k<4)return float3(.15,.88,.035);
    if(k<5)return float3(.92,.045,.88); return float3(.02,.32,1);
   }
   float4 dots(float3 n,float angle,float lag,float opticalSample){
    // Inverse projection onto the sphere: circles foreshorten at the silhouette and wrap behind it.
    // Inverse rigid pose followed by a rigid spin. Mounted inserts never change latitude/size.
    n=mul((float3x3)_RigidPose,n);
    // Exposure integrates previous rigid orientations, not latitude/insert deformation.
    float dz=_Charge*(.18*(sin((_MotionPhase-lag)*17)-sin(_MotionPhase*17))+.055*(sin((_MotionPhase-lag)*9)-sin(_MotionPhase*9)));
    float dx=_Charge*.11*(sin((_MotionPhase-lag)*11)-sin(_MotionPhase*11));
    n.xy=float2(n.x*cos(dz)-n.y*sin(dz),n.x*sin(dz)+n.y*cos(dz));
    n.yz=float2(n.y*cos(dx)-n.z*sin(dx),n.y*sin(dx)+n.z*cos(dx));
    float cs=cos(angle),sn=sin(angle);
    float3 surface=float3(n.x*cs+n.z*sn,n.y,-n.x*sn+n.z*cs);
    float latitude=asin(clamp(surface.y,-1,1)), longitude=atan2(surface.x,surface.z);
    float row=floor((latitude+1.570796)/.60);
    float rows=5;float ly=(row+.5)*.60-1.570796;
    float columns=round(8.5*cos(ly));columns=max(3,columns);
    float u=longitude/6.283185*columns+fmod(row,2)*.5;
    float column=floor(u+.5);float2 q=float2((u-column)*6.283185/columns*cos(ly),latitude-ly)/.225;
    float r=dot(q,q),aa=max(fwidth(r),.025);float a=1-smoothstep(1-aa,1+aa,r);
    a=lerp(a,1-smoothstep(.28,1.6,r),opticalSample);
    float z=sqrt(saturate(1-r));
    float3 bump=normalize(n+float3(q.x,q.y,0)*.28+float3(0,0,z*.45));
    float light=saturate(dot(bump,normalize(float3(-.45,.65,1))));
    float3 base=candy(fmod(column+columns*10,columns)+row*2);
    float spec=pow(saturate(dot(normalize(float3(q.x,q.y,z)),normalize(float3(-.4,.5,1)))),24);
    // Brighter than V3: the charged ball has to read as lit energy, not a dark sphere with dots.
    float3 rgb=base*(.78+.55*light)+spec*.70+base*_Charge*.55;
    // Optical exposure filters fine specular detail instead of reproducing a line of
    // miniature glossy inserts outside the body.
    rgb=lerp(rgb,base*(1.10+_Charge*.3),opticalSample);
    return float4(rgb,a);
   }
   float4 frag(v2f i):SV_Target{
    float2 p=(i.uv-.5)*3.6;
    float r=dot(p,p),aa=max(fwidth(r),.004);
    float edge=1-smoothstep(1-aa,1+aa,r);
    float3 n=float3(p,sqrt(saturate(1-r)));
    if(_Layer<.5){
     clip(edge-.001);
     float light=saturate(dot(n,normalize(float3(-.45,.6,1))));
     float spec=pow(saturate(dot(n,normalize(float3(-.27,.40,1)))),34);
     float rim=pow(1-n.z,3);
     float3 rgb=lerp(float3(.012,.022,.072),float3(.038,.16,.38),light)*(.67+.33*n.z);
     rgb+=spec*float3(.36,.48,.60)+rim*float3(.025,.075,.17);
     // While charged the body has to glow, not stay a dark sphere: otherwise the gaps between the
     // colour rows read as black bands instead of the luminous energy the reference shows.
     rgb+=_Charge*.12*pow(n.z,8)*float3(.8,.88,1);
     // Bright metallic reflection remains behind the coloured exposure. Dark equatorial
     // gaps were reading as black separators between rigid, painted stripes.
     float reflection=pow(saturate(n.z),.55);
     rgb=lerp(rgb,float3(.94,.91,.78)*(.34+.66*reflection),_Charge*.94);
     rgb+=_Charge*pow(n.z,5)*.35;
     return float4(rgb,edge*_Opacity);
    }
    // Optical streaks from the rotating coloured inserts can extend beyond the metal.
    // The metal ray/silhouette and mounted detail coordinates remain strictly rigid.
    float4 optical=0;
    if(_Charge>.01){
     [unroll]for(int k=0;k<25;k++){
      float u=k/24.0,dx=(u-.5)*2.20;
      // A continuous, soft exposure footprint follows the rigid axis. Only light extends.
      float slope=.12*sin(_MotionPhase*17)+.05*sin(_MotionPhase*9);
      float2 sampleP=p-float2(dx,dx*slope);float sr=dot(sampleP,sampleP);
      if(sr<1){float3 nn=float3(sampleP,sqrt(1-sr));float4 d=dots(nn,_Angle-u*_Exposure,u*.042,1);
       float w=exp(-dx*dx*1.1)/25.0;optical+=float4(d.rgb*d.a,d.a)*w;}
     }
    }
    if(r>1){
     float a=(1-exp(-optical.a*6.0))*_Charge*(1-smoothstep(1.48,1.8,abs(p.x)));
     return float4(optical.rgb/max(.001,optical.a)*1.22,a*_Opacity);
    }
    int count=(int)clamp(_Samples,1,24);
    float inv=1.0/count;
    float4 sum=0;
    [loop]for(int s=0;s<count;s++){float u=s*inv;float4 d=dots(n,_Angle-(count==1?0:u)*_Exposure,u*.042*_Charge,0);sum+=float4(d.rgb*d.a,d.a)*inv;}
    // Integrate soft colour exposure over the shell as well as outside it: no hard
    // transition at the silhouette and no isolated dotted echoes.
    sum=lerp(sum,sum*.74+optical*.46,_Charge);
    float3 rgb=sum.rgb/max(.001,sum.a);
    // Averaging across the arc pulls the samples towards grey, because neighbouring dots differ in
    // colour. Push the chroma back out by the amount actually averaged, so a fast ball reads as
    // saturated streaks rather than a wash.
    float lum=dot(rgb,float3(.2126,.7152,.0722));
    rgb=lum+(rgb-lum)*(1+_Energy*1.35);
    rgb*=(1+_Energy*.30);
    // White-hot core, kept inside the silhouette so it never becomes a bloom halo.
    float coreR=length(float2(p.x*1.15,p.y));
    float core=pow(saturate(1-smoothstep(0,.95,coreR)),1.35)*_Core;
    rgb=lerp(rgb,float3(1,1,1),saturate(core*.35));
    rgb+=core*.45;
    float a=saturate(lerp(sum.a,1-exp(-sum.a*5.5),_Charge)+core*.45);
    // Exposure still follows V4 phase. A moving specular ridge restores depth through the streaks.
    float glint=pow(saturate(dot(n,normalize(float3(.40*sin(_MotionPhase*12),.48+.14*sin(_MotionPhase*17),1)))),30)*_Charge;
    rgb+=glint*.50;
    rgb+=float3(1,.96,.77)*pow(1-n.z,4)*_Charge*(.14+.12*sin(_MotionPhase*21+p.y*7));
    rgb+=optical.rgb*_Charge*.14;return float4(rgb,a*edge*_Opacity);
   }
   ENDCG
  }
 }
}
