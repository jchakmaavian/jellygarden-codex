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
   float _Layer,_Opacity,_Angle,_Exposure,_Charge,_Samples,_Widen,_Core,_Energy;
   v2f vert(appdata_base v){v2f o;float morph=smoothstep(.35,1,_Charge);v.vertex.xy*=lerp(float2(1,1),float2(1.10,.81),morph);o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;return o;}
   float3 candy(float k){
    k=fmod(k,6);
    if(k<1)return float3(1,.045,.12); if(k<2)return float3(.01,.66,1);
    if(k<3)return float3(1,.59,.015); if(k<4)return float3(.15,.88,.035);
    if(k<5)return float3(.92,.045,.88); return float3(.02,.32,1);
   }
   float4 dots(float3 n,float angle){
    // Inverse projection onto the sphere: circles foreshorten at the silhouette and wrap behind it.
    float3 axis=normalize(lerp(float3(.22,1,.10),float3(.045,1,.045),_Charge));
    float cs=cos(angle),sn=sin(angle);float3 surface=n*cs+cross(axis,n)*sn+axis*dot(axis,n)*(1-cs);
    float latitude=asin(surface.y), longitude=atan2(surface.x,surface.z);
    // Fewer, larger beads than V3. Once the surface smears, a dense grid closes into thin stripes;
    // the reference keeps distinct fat blobs with lighter gaps between them.
    float row=floor((latitude+1.570796)/.60);
    float rows=5;float ly=(row+.5)*.60-1.570796;
    float columns=round(8.5*cos(ly));columns=max(3,columns);
    float u=longitude/6.283185*columns+fmod(row,2)*.5;
    float column=floor(u+.5);float2 q=float2((u-column)*6.283185/columns*cos(ly),latitude-ly)/.225;
    float r=dot(q,q),aa=max(fwidth(r),.025);float a=1-smoothstep(1-aa,1+aa,r);
    float z=sqrt(saturate(1-r));
    float3 bump=normalize(n+float3(q.x,q.y,0)*.28+float3(0,0,z*.45));
    float light=saturate(dot(bump,normalize(float3(-.45,.65,1))));
    float3 base=candy(fmod(column+columns*10,columns)+row*2);
    float spec=pow(saturate(dot(normalize(float3(q.x,q.y,z)),normalize(float3(-.4,.5,1)))),24);
    // Brighter than V3: the charged ball has to read as lit energy, not a dark sphere with dots.
    float3 rgb=base*(.78+.55*light)+spec*.70+base*_Charge*.55;
    return float4(rgb,a);
   }
   float4 frag(v2f i):SV_Target{
    float2 p=(i.uv-.5)*2;
    // Widen is applied to the quad transform, not here: the sphere maths stays a unit sphere so the
    // streaks keep curving along their own latitude instead of being a flat horizontal stretch.
    float r=dot(p,p);float aa=max(fwidth(r),.004);
    float edge=1-smoothstep(1-max(aa,_Charge*.065),1+aa,r);clip(edge-.001);
    float3 n=normalize(float3(p.x*lerp(1,.80,_Charge),p.y*lerp(1,1.20,_Charge),sqrt(saturate(1-r))));
    if(_Layer<.5){
     float light=saturate(dot(n,normalize(float3(-.45,.6,1))));
     float spec=pow(saturate(dot(n,normalize(float3(-.27,.40,1)))),34);
     float rim=pow(1-n.z,3);
     float3 rgb=lerp(float3(.012,.022,.072),float3(.038,.16,.38),light)*(.67+.33*n.z);
     rgb+=spec*float3(.36,.48,.60)+rim*float3(.025,.075,.17);
     // While charged the body has to glow, not stay a dark sphere: otherwise the gaps between the
     // colour rows read as black bands instead of the luminous energy the reference shows.
     rgb+=_Charge*.12*pow(n.z,8)*float3(.8,.88,1);
     rgb=lerp(rgb,float3(.34,.48,.68)*(.62+.38*n.z),_Charge);
     rgb+=_Charge*pow(n.z,3)*.30;
     return float4(rgb,edge*_Opacity);
    }
    // The shutter spans the rotation of the frame being drawn and is sampled densely enough that
    // consecutive frames tile with no gap. That is what lets the ball spin fast enough to become
    // bands without strobing, at 30 or 60 FPS, since the arc comes from the real delta time.
    int count=(int)clamp(_Samples,1,24);
    float inv=1.0/count;
    float4 sum=0;
    [loop]for(int s=0;s<count;s++){float4 d=dots(n,_Angle-(count==1?0:(s*inv))*_Exposure);sum+=float4(d.rgb*d.a,d.a)*inv;}
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
    rgb=lerp(rgb,float3(1,1,1),saturate(core*.85));
    rgb+=core*.45;
    float a=saturate(sum.a*lerp(1,1.6,_Charge)+core*.85);
    // Exposure still follows V4 phase. A moving specular ridge restores depth through the streaks.
    float glint=pow(saturate(dot(n,normalize(float3(.35*sin(_Angle*.13),.55,1)))),30)*_Charge;
    rgb+=glint*.50;
    return float4(rgb,a*edge*_Opacity);
   }
   ENDCG
  }
 }
}
