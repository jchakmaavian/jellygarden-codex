Shader "RoyalThrone/PropellerV5/AirAndStars"
{
 Properties { _Color("Colour", Color)=(1,1,1,1) _Kind("0 glow, 1 star, 2 air",Float)=0 }
 SubShader { Tags {"Queue"="Transparent+20" "RenderType"="Transparent"} Pass {
 Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha One
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 fixed4 _Color;float _Kind;
 struct a {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
 struct v {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 v vert(a i){v o;o.pos=UnityObjectToClipPos(i.vertex);o.uv=i.uv;return o;}
 fixed4 frag(v i):SV_Target {
 float2 p=(i.uv-.5)*2;float r=length(p);float f;
 if(_Kind<.5)f=exp(-r*r*7)*smoothstep(1,.65,r);
 else if(_Kind<1.5){float d=pow(abs(p.x),.52)+pow(abs(p.y),.52);f=smoothstep(1.07,.91,d);}
 else if(_Kind<2.5)f=exp(-pow((r-.72)*28,2))*.7*smoothstep(1,.92,r);
 else f=pow(saturate(1-p.x*p.x),1.4)*exp(-p.y*p.y*10)*smoothstep(1,.7,abs(p.y));
 return fixed4(_Color.rgb,f*_Color.a);
 }
 ENDCG
 } }
}
