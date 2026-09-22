Shader "RoyalThrone/ColorBallV5/Light"
{
 Properties { _Color("Tint",Color)=(1,1,1,1) _Kind("Glow Star Ring Beam Orbit",Float)=0 _OrbitPhase("Orbit phase",Float)=0 }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha One
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;}; float4 _Color;float _Kind,_OrbitPhase,_Phase,_Pulse,_Release;
   v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;return o;}
   float4 frag(v2f i):SV_Target{
    float2 p=(i.uv-.5)*2;float r=length(p),a=0;
    if(_Kind<.5)a=lerp(exp(-r*r*6),exp(-r*r*r*r*4),_Release)*saturate((1-r)*5);
    else if(_Kind<1.5){a=pow(saturate(1-pow(abs(p.x),.55)-pow(abs(p.y),.55)),.55);a=max(a,exp(-r*r*80)*.65);}
    else if(_Kind<2.5){
     float d=abs(r-.76),angle=atan2(p.y,p.x);
     float streaks=pow(saturate(cos(angle*11+_Phase*3)),28)*exp(-pow((r-.62)/.19,2));
     a=(exp(-pow(d/.030,2))*1.4+exp(-pow(d/.12,2))*.58+streaks*.40)*saturate((1-r)*12);
     a*=.93+.07*sin(angle*16+_Phase*18);
     return float4(lerp(_Color.rgb,float3(1,1,.96),exp(-pow(d/.035,2))),_Color.a*a);
    }
    else if(_Kind<3.5||(_Kind>4.5&&_Kind<5.5)){
     float x=i.uv.x,tip=exp(-pow((x-.93)/.16,2)),packet=exp(-pow((x-frac(_Phase*2.4))/.13,2));
     float taper=lerp(.52,1,smoothstep(0,.06,x)*smoothstep(0,.075,1-x));
     float y=(p.y-.08*sin(x*24-_Phase*24)*sin(x*3.14159))/taper;float ends=smoothstep(0,.016,x)*smoothstep(0,.018,1-x);
     float flow=.94+.06*sin(x*17-_Phase*32);
     if(_Kind<3.5){
      a=(exp(-y*y*2.5)*.65+exp(-y*y*9)*.35)*ends*(flow+.32*tip+.15*packet);
      return float4(lerp(_Color.rgb*float3(1.75,1.55,1.08),float3(1.35,1.30,1.12),exp(-y*y*12)*.55),_Color.a*a);
     }
     a=(1-smoothstep(.40,.98,abs(y)))*ends*(flow+.40*tip+.20*packet);
    }
    else if(_Kind>5.5){
     float angle=atan2(p.y,p.x),lobes=.055*sin(angle*3+_Phase*17)+.035*sin(angle*5-_Phase*13);
     float rr=r-lobes*saturate(r*2),edge=saturate((1-r)*8);
     float upper=exp(-pow(p.x*2.2,2)-pow((abs(p.y)-.35)/.28,2));
     float rays=pow(saturate(cos(angle*7+_Phase*3)),22)*exp(-pow((rr-.65)/.24,2));
     if(_Kind<6.5){
      a=(exp(-rr*rr*2.7)*.70+upper*.70+rays*.28)*edge;
      return float4(lerp(_Color.rgb,float3(1,1,.98),saturate(upper+exp(-rr*rr*10))),_Color.a*a);
     }
     // Front light hugs the upper/lower silhouette; it never coats the rainbow center.
     float arch=exp(-pow((abs(p.y)-(.43-.12*p.x*p.x))/.16,2))*exp(-p.x*p.x*2.8);
     a=(arch*.65+rays*.32)*edge;
    }
    else {
     float q=i.uv.y*2-1,theta=i.uv.x*6.283185;
     float d=frac((theta-_OrbitPhase)/6.283185);float d2=frac(d+.47);
     float comet=exp(-d*20)+.65*exp(-d2*23);
     float ribbon=(.14+comet*1.15)*exp(-q*q*9);
     float hot=exp(-q*q*100)*(.18+comet*1.3);
     return float4(lerp(_Color.rgb,float3(1,.98,.92),saturate(hot*2)),_Color.a*(ribbon+hot));
    }
    return float4(_Color.rgb,_Color.a*a);
   }
   ENDCG
  }
 }
}
