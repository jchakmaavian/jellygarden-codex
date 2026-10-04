Shader "RoyalThrone/KingVanguardPainted" {
 Properties {
  [PerRendererData] _MainTex ("Neutral",2D)="white"{}
  _WorriedTex ("Worried",2D)="white"{} _SadTex ("Sad",2D)="white"{} _LaughTex ("Laugh",2D)="white"{}
  _HappyTex ("Happy",2D)="white"{} _ExcitedTex ("Excited",2D)="white"{}
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
   struct appdata {float4 vertex:POSITION;float4 color:COLOR;float4 uv:TEXCOORD0;float4 uv1:TEXCOORD1;float4 uv2:TEXCOORD2;};
   struct v2f {float4 vertex:SV_POSITION;fixed4 color:COLOR;float4 uv:TEXCOORD0;float4 uv1:TEXCOORD1;float4 uv2:TEXCOORD2;float4 world:TEXCOORD3;};
   sampler2D _MainTex,_WorriedTex,_SadTex,_LaughTex,_HappyTex,_ExcitedTex;float4 _Color,_ClipRect,_Expression,_Gaze,_Positive,_Lids;
   v2f vert(appdata v){v2f o;o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.uv1=v.uv1;o.uv2=v.uv2;o.color=v.color*_Color;return o;}
   float ellipse(float2 p,float2 center,float2 radius){return length((p-center)/radius);}
   float2 attended(float2 texcoord){
    float2 p=float2(texcoord.x,1-texcoord.y)*1254;
    float left=1-smoothstep(.48,1,ellipse(p,float2(516,446),float2(52,32)));
    float right=1-smoothstep(.48,1,ellipse(p,float2(690,416),float2(52,32)));
    p-=float2(_Gaze.x*8,-_Gaze.y*5)*max(left,right)*(1-_Expression.z);
    float brows=1-smoothstep(.3,1,min(ellipse(p,float2(497,349),float2(85,55)),ellipse(p,float2(698,322),float2(80,55))));
    p.y+=brows*_Lids.z;
    return float2(p.x/1254,1-p.y/1254);
   }
   fixed4 frag(v2f i):SV_Target {
    float2 p=float2(i.uv.x,1-i.uv.y)*1254;
    float w=_Expression.x,s=_Expression.y,l=_Expression.z,h=_Positive.x,e=_Positive.y;
    float n=max(0,1-w-s-l-h-e);
    fixed4 neutral=tex2D(_MainTex,attended(i.uv.xy));
    fixed3 faceColor=neutral.rgb*n;
    faceColor+=tex2D(_WorriedTex,attended(i.uv.zw)).rgb*w;
    faceColor+=tex2D(_SadTex,attended(i.uv1.xy)).rgb*s;
    fixed3 smiling=tex2D(_HappyTex,attended(i.uv1.zw)).rgb;
    fixed3 delighted=tex2D(_ExcitedTex,attended(i.uv2.xy)).rgb;
    faceColor+=smiling*h+delighted*e;
    fixed3 laughing=tex2D(_LaughTex,i.uv2.zw).rgb;
    faceColor+=laughing*l;
    // Retain opaque mouth interior while its geometry closes. Dissolving skin
    // over teeth produces a translucent double lip during strong interruptions.
    float openWeight=h+e+l;
    float mouthY=p.y+(p.x-627)*.16;
    float mouthMask=smoothstep(578,590,mouthY)*(1-smoothstep(609,625,mouthY))*(1-smoothstep(83,118,abs(p.x-627)));
    fixed3 interior=(smiling*h+delighted*e+laughing*l)/max(.0001,openWeight);
    faceColor=lerp(faceColor,interior,mouthMask*smoothstep(.015,.2,openWeight));
    float face=1-smoothstep(.84,1,ellipse(p,float2(625,590),float2(360,370)));
    fixed4 c=neutral;c.rgb=lerp(c.rgb,faceColor,face);
    float left=1-smoothstep(.65,1,ellipse(p,float2(516,443),float2(65,43)));
    float right=1-smoothstep(.65,1,ellipse(p,float2(690,415),float2(65,43)));
    c.rgb=lerp(c.rgb,laughing,max(left*_Lids.x,right*_Lids.y));
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
