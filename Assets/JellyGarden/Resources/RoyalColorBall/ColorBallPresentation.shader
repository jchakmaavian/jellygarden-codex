Shader "RoyalThrone/Color Ball Presentation"
{
 Properties {
  [PerRendererData] _MainTex("Frame",2D)="white"{}
  _NextTex("Next frame",2D)="white"{}
  _FrameBlend("Frame blend",Float)=0
  _DiscWidth("Disc width",Float)=1
  _DiscHeight("Disc height",Float)=1
  _Energy("Spin energy",Float)=0
  _Envelope("Envelope",Float)=1
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False"}
  Cull Off ZWrite Off Lighting Off Blend One OneMinusSrcAlpha
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   sampler2D _MainTex,_NextTex;
   float _FrameBlend,_DiscWidth,_DiscHeight,_Energy,_Envelope;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   fixed4 frame(float2 uv){return lerp(tex2D(_MainTex,uv),tex2D(_NextTex,uv),_FrameBlend);}
   fixed4 frag(v2f i):SV_Target {
    float2 q=i.uv-.5;
    float2 uv=q/float2(max(.05,_DiscWidth),max(.05,_DiscHeight))+.5;
    fixed4 c=frame(uv);
    // Horizontal blur only: keep the original saturated bands and sphere/dots at rest.
    fixed4 blur=(frame(uv+float2(.006,0))+frame(uv-float2(.006,0)))*.5;
    c=lerp(c,blur,_Energy*.3);
    c*=step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1);
    c.rgb=lerp(c.rgb,saturate(c.rgb*1.15+.06),_Energy);
    c.a*=lerp(1,.72,_Energy);c.rgb*=c.a;
    float halo=exp(-pow(q.x/.26,2)-pow(q.y/.10,2))*_Energy*.78;
    c.rgb+=float3(1,1,.98)*halo*(1-c.a);c.a+=halo*(1-c.a);
    float core=exp(-pow(q.y/.009,2)-pow(q.x/.205,4))*_Energy;
    float star=exp(-abs(q.x)/.009-abs(q.y)/.075)*_Energy;
    float ribbons=exp(-pow((abs(q.y)-.035)/.006,2)-pow(q.x/.16,4))*_Energy*.65;
    float light=saturate(core+star+ribbons);
    c.rgb=lerp(c.rgb,float3(1,1,1),light);c.a+=light*(1-c.a);
    c*=i.color*_Envelope;
    return c;
   }
   ENDCG
  }
 }
}
