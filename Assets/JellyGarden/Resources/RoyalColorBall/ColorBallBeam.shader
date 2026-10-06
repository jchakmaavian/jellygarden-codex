Shader "RoyalThrone/Color Ball Beam"
{
 Properties { _Sharpness("Cross section",Float)=5 _Age("Age",Float)=0 _LengthCells("Length",Float)=1 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
  Cull Off ZWrite Off Lighting Off Blend SrcAlpha One
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   float _Sharpness,_Age,_LengthCells;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   fixed4 frag(v2f i):SV_Target {
    float cross=i.uv.y*2-1;
    float edge=exp(-cross*cross*_Sharpness)*smoothstep(1,.72,abs(cross));
    float pulse=exp(-pow((i.uv.x-frac(_Age*5.5))/.10,2));
    float flow=.92+.08*sin(i.uv.x*max(1,_LengthCells)*18-_Age*65);
    return fixed4(i.color.rgb*(1+pulse*.3),i.color.a*edge*flow);
   }
   ENDCG
  }
 }
}
