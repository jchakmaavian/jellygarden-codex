Shader "RoyalThrone/PropellerV3/LocalChips"
{
 Properties { _MainTex("Painted chips",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) }
 SubShader { Tags {"Queue"="Transparent+20" "RenderType"="Transparent"} Pass {
 Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;fixed4 _Color;
 struct a {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
 struct v {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 v vert(a i){v o;o.pos=UnityObjectToClipPos(i.vertex);o.uv=i.uv;return o;}
 fixed4 frag(v i):SV_Target{return tex2D(_MainTex,i.uv)*_Color;}
 ENDCG
 } }
}
