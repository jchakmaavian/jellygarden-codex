Shader "RoyalThrone/TNTGlow" {
 Properties { _Color("Color",Color)=(1,1,1,1) _Mode("Shape",Float)=0 _FxTime("Time",Float)=0 }
 SubShader { Tags {"Queue"="Transparent+10" "RenderType"="Transparent"} Pass {
 Blend One One ZWrite Off Cull Off
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _Color; float _Mode,_FxTime;
 struct v2f { float4 pos:SV_POSITION;float2 uv:TEXCOORD0; };
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;return o;}
 fixed4 frag(v2f i):SV_Target {float2 p=(i.uv-.5)*2;float r=length(p);float a=exp(-r*r*5)*smoothstep(1,.7,r);
 if(_Mode>0.5&&_Mode<1.5)a=exp(-pow((r-.75)*18,2))*saturate(1-r)*3;
 if(_Mode>1.5&&_Mode<2.5)a=pow(saturate(1-abs(p.x)*abs(p.y)*35-r*.65),4)*saturate(1-r);
 if(_Mode>2.5&&_Mode<3.5){float y=i.uv.y;float x=p.x+.12*sin(y*15+_FxTime*40);a=pow(saturate(1-abs(x)/(max(.03,(1-y)*.8))),1.5)*smoothstep(0,.12,y)*pow(1-y,.5);}
 if(_Mode>3.5){float angle=atan2(p.y,p.x);a=pow(saturate(sin(angle*13+r*5)),10)*pow(saturate(1-r),1.5)*smoothstep(.15,.35,r);}
 return float4(_Color.rgb*_Color.a*a,0); }
 ENDCG
 } }
}
