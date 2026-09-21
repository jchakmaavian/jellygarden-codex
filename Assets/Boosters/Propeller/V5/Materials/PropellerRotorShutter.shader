Shader "RoyalThrone/PropellerV5/RotorShutter"
{
 Properties { _Color("Tint",Color)=(1,1,1,1) _Phase("Phase",Float)=0 _Sweep("Shutter arc",Float)=1.6 _Opacity("Opacity",Float)=1 }
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" } Pass {
 Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float _Phase,_Sweep,_Opacity;
 struct a {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
 struct v {float4 pos:SV_POSITION;float2 p:TEXCOORD0;};
 v vert(a i){v o;o.pos=UnityObjectToClipPos(i.vertex);o.p=(i.uv-.5)*.94;return o;}
 fixed4 frag(v i):SV_Target
 {
   float r=length(i.p),theta=atan2(i.p.y,i.p.x)-_Phase-.523599;
   float sum=0,light=0;
   // Integrate shaped paddles over a shutter arc in the real tilted plane.
   [unroll] for(int k=0;k<16;k++){
     float age=(k+.5)/16.;
     float angle=theta+age*_Sweep;
     angle=(frac(angle/2.094395+.5)-.5)*2.094395;
     float x=r*cos(angle),y=r*sin(angle),u=saturate((x-.058)/.39);
     float width=.029+.142*sin(pow(u,.70)*2.85885);
     width*=sqrt(saturate((1-u)/.08));
     float coverage=(1-smoothstep(width-.009,width+.009,abs(y)))*smoothstep(.054,.072,x)*(1-smoothstep(.43,.455,x));
     float weight=1-age*.82;
     sum+=coverage*weight;light+=coverage*weight*(.72+.28*saturate(1-abs(y)/max(.01,width)));
   }
   float alpha=saturate(sum*.17)*_Opacity;
   float3 color=lerp(float3(1,.76,.045),float3(1,.045,.105),smoothstep(.343,.365,r));
   color*=light/max(.001,sum);color+=pow(saturate(1-abs(r-.30)*14),4)*.12;
   return fixed4(color,alpha);
 }
 ENDCG
 } }
}
