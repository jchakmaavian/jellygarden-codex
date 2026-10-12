Shader "RoyalThrone/TNTBody" {
 Properties { _Color("Color",Color)=(1,0.05,0.02,1) }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Pass {
 ZWrite On Cull Back
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _Color;
 struct v2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; };
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);return o;}
 fixed4 frag(v2f i):SV_Target {float3 n=normalize(i.normal);float lit=.48+.52*saturate(dot(n,normalize(float3(-.5,.7,-1))));float shine=pow(saturate(dot(n,normalize(float3(-.35,.6,-1)))),24);return float4(_Color.rgb*lit+shine*.52,1);}
 ENDCG
 } }
}
