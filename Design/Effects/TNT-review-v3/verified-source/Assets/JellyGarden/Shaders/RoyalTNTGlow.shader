Shader "RoyalThrone/TNTGlow" {
 Properties { _Color("Color",Color)=(1,1,1,1) _Mode("Shape",Float)=0 _FxTime("Time",Float)=0 }
 SubShader { Tags {"Queue"="Transparent+10" "RenderType"="Transparent"} Pass {
 Blend One OneMinusSrcAlpha ZWrite Off Cull Off
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _Color; float _Mode,_FxTime;
 struct v2f { float4 pos:SV_POSITION;float2 uv:TEXCOORD0; };
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;return o;}
 fixed4 frag(v2f i):SV_Target {float2 p=(i.uv-.5)*2;float r=length(p);float a=exp(-r*r*5)*smoothstep(1,.7,r);
 if(_Mode>0.5&&_Mode<1.5){float angle=atan2(p.y,p.x);float rim=.75+.022*sin(angle*17+_FxTime*12)+.018*sin(angle*29-_FxTime*17);a=exp(-pow((r-rim)*16,2))*saturate(1-r)*3;}
 if(_Mode>1.5&&_Mode<2.5)a=pow(saturate(1-abs(p.x)*abs(p.y)*35-r*.65),4)*saturate(1-r);
 if(_Mode>2.5&&_Mode<3.5){float y=i.uv.y;float x=p.x+.12*sin(y*15+_FxTime*40);a=pow(saturate(1-abs(x)/(max(.03,(1-y)*.8))),1.5)*smoothstep(0,.12,y)*pow(1-y,.5);}
 if(_Mode>3.5&&_Mode<4.5){float angle=atan2(p.y,p.x);float rays=pow(saturate(sin(angle*19+.6*sin(angle*7))),10);a=rays*smoothstep(.06,.24,r)*pow(saturate(1-r),.65)*(.82+.18*sin(angle*13))+.13*exp(-r*r*4);}
 if(_Mode>4.5&&_Mode<5.5){float angle=atan2(p.y,p.x);float rim=.76+.02*sin(angle*17+_FxTime*20)+.018*sin(angle*29);a=1-smoothstep(rim-.15,rim+.03,r);}
 if(_Mode>5.5){float angle=atan2(p.y,p.x);float rim=.68+.06*sin(angle*11+_FxTime*48)+.025*sin(angle*23-_FxTime*60);a=exp(-pow((r-rim)*13,2))*(.85+.15*sin(angle*19+_FxTime*70))*smoothstep(1,.86,r);}
 // Gold rays/rings need slight coverage against the pale board; purely additive
 // yellow over lavender clips to white before the gold edge can be read.
 float3 tint=_Color.rgb;
 if(_Mode>3.5&&_Mode<4.5)tint=lerp(float3(1,.97,.78),tint,smoothstep(.18,.72,r));
 float cover=(_Mode>.5&&_Mode<1.5||_Mode>3.5&&_Mode<4.5)?saturate(a*_Color.a*.72):0;
 return float4(tint*_Color.a*a,cover); }
 ENDCG
 } }
}
