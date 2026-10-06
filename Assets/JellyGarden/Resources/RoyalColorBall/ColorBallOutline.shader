Shader "RoyalThrone/Color Ball Target Outline"
{
 Properties {[PerRendererData] _MainTex("Gem",2D)="white"{}}
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"}
  Cull Off ZWrite Off Lighting Off Blend SrcAlpha One
  Pass {CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   sampler2D _MainTex;float4 _MainTex_TexelSize;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   fixed4 frag(v2f i):SV_Target {
    float2 d=_MainTex_TexelSize.xy*10;
    float a=tex2D(_MainTex,i.uv).a;
    float outer=a,inner=a;
    outer=max(outer,tex2D(_MainTex,i.uv+float2(d.x,0)).a);
    outer=max(outer,tex2D(_MainTex,i.uv-float2(d.x,0)).a);
    outer=max(outer,tex2D(_MainTex,i.uv+float2(0,d.y)).a);
    outer=max(outer,tex2D(_MainTex,i.uv-float2(0,d.y)).a);
    outer=max(outer,tex2D(_MainTex,i.uv+d).a);
    outer=max(outer,tex2D(_MainTex,i.uv-d).a);
    outer=max(outer,tex2D(_MainTex,i.uv+float2(d.x,-d.y)).a);
    outer=max(outer,tex2D(_MainTex,i.uv+float2(-d.x,d.y)).a);
    inner=min(inner,tex2D(_MainTex,i.uv+float2(d.x,0)*.6).a);
    inner=min(inner,tex2D(_MainTex,i.uv-float2(d.x,0)*.6).a);
    inner=min(inner,tex2D(_MainTex,i.uv+float2(0,d.y)*.6).a);
    inner=min(inner,tex2D(_MainTex,i.uv-float2(0,d.y)*.6).a);
    float rim=saturate(a-inner);
    float edge=saturate(outer-inner);
    return fixed4(lerp(i.color.rgb,float3(1,1,.95),rim),saturate(edge+a*.12)*i.color.a);
   }
   ENDCG}
 }
}
