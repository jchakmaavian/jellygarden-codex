Shader "RoyalThrone/KingVanguardPainted" {
 Properties {
  [PerRendererData] _MainTex ("Neutral",2D)="white"{}
  _WorriedTex ("Worried",2D)="white"{} _SadTex ("Sad",2D)="white"{} _LaughTex ("Laugh",2D)="white"{}
  _Color ("Tint",Color)=(1,1,1,1)
  _StencilComp ("Stencil Comparison",Float)=8 _Stencil ("Stencil ID",Float)=0
  _StencilOp ("Stencil Operation",Float)=0 _StencilWriteMask ("Stencil Write Mask",Float)=255 _StencilReadMask ("Stencil Read Mask",Float)=255
  _ColorMask ("Color Mask",Float)=15
  [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip",Float)=0
 }
 SubShader {
  Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True"}
  Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct v2f {float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;float4 world:TEXCOORD1;};
   sampler2D _MainTex,_WorriedTex,_SadTex,_LaughTex;float4 _Color,_ClipRect,_Expression,_Gaze;
   v2f vert(appdata v){v2f o;o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
   float ellipse(float2 p,float2 center,float2 radius){return length((p-center)/radius);}
   float2 eyeUV(float2 p,float2 center){
    // The aperture boundary stays fixed; only the iris and surrounding sclera flow.
    float influence=1-smoothstep(.48,1,ellipse(p,center,float2(52,32)));
    return p-float2(_Gaze.x*8,-_Gaze.y*5)*influence*(1-_Expression.z);
   }
   float2 uv(float2 pixel){return float2(pixel.x/1254,1-pixel.y/1254);}
   fixed4 frag(v2f i):SV_Target {
    float2 p=float2(i.uv.x,1-i.uv.y)*1254;
    float2 eyes=eyeUV(eyeUV(p,float2(516,446)),float2(690,416));
    fixed4 neutral=tex2D(_MainTex,uv(eyes));
    float face=1-smoothstep(.84,1,ellipse(p,float2(625,590),float2(360,370)));
    fixed4 c=neutral;
    c.rgb=lerp(c.rgb,tex2D(_WorriedTex,uv(eyes)).rgb,_Expression.x*face);
    c.rgb=lerp(c.rgb,tex2D(_SadTex,uv(p)).rgb,_Expression.y*face);
    c.rgb=lerp(c.rgb,tex2D(_LaughTex,i.uv).rgb,_Expression.z*face);
    float left=1-smoothstep(.65,1,ellipse(p,float2(516,443),float2(65,43)));
    float right=1-smoothstep(.65,1,ellipse(p,float2(690,415),float2(65,43)));
    c.rgb=lerp(c.rgb,tex2D(_LaughTex,uv(p+float2(-16,-12))).rgb,left*_Expression.w);
    c.rgb=lerp(c.rgb,tex2D(_LaughTex,uv(p+float2(14,-6))).rgb,right*_Expression.w);
    c*=i.color;
    #ifdef UNITY_UI_CLIP_RECT
    c.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
    #endif
    #ifdef UNITY_UI_ALPHACLIP
    clip(c.a-.001);
    #endif
    return c;
   }
   ENDCG
  }
 }
}
