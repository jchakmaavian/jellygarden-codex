Shader "RoyalThrone/ColorBallV3/Light"
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
   struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;}; float4 _Color;float _Kind,_OrbitPhase;
   v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;return o;}
   float4 frag(v2f i):SV_Target{
    float2 p=(i.uv-.5)*2;float r=length(p),a=0;
    if(_Kind<.5)a=exp(-r*r*6)*saturate((1-r)*5);
    else if(_Kind<1.5){a=pow(saturate(1-pow(abs(p.x),.55)-pow(abs(p.y),.55)),.55);a=max(a,exp(-r*r*80)*.65);}
    else if(_Kind<2.5)a=exp(-pow((r-.76)/.027,2));
    else if(_Kind<3.5)a=exp(-p.y*p.y*9)*smoothstep(0,.7,i.uv.x)*saturate((1-i.uv.x)*12);
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
