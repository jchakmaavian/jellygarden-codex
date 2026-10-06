Shader "RoyalThrone/PropellerV2/PolishedSurface"
{
 Properties { _Color("Colour", Color)=(1,1,1,1) _DepthWrite("Depth write",Float)=1 }
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" } Pass {
 Cull Back ZWrite [_DepthWrite] Blend SrcAlpha OneMinusSrcAlpha
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 fixed4 _Color;
 struct a {float4 vertex:POSITION;float3 normal:NORMAL;};
 struct v {float4 pos:SV_POSITION;float3 n:TEXCOORD0;};
 v vert(a i){v o;o.pos=UnityObjectToClipPos(i.vertex);o.n=UnityObjectToWorldNormal(i.normal);return o;}
 fixed4 frag(v i):SV_Target {
 float3 n=normalize(i.n),light=normalize(float3(-.45,.7,-1));
 float diffuse=saturate(dot(n,light));
 float spec=pow(saturate(dot(n,normalize(light+float3(0,0,-1)))),28);
 float rim=pow(1-abs(n.z),3)*.14;
 return fixed4(_Color.rgb*(.52+.63*diffuse)+spec*.92+rim*_Color.rgb,_Color.a);
 }
 ENDCG
 } }
}
