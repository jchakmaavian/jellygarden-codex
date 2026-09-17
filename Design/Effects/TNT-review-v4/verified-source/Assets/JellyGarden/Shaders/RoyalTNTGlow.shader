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
 float hash21(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 q=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash21(q),hash21(q+float2(1,0)),f.x),lerp(hash21(q+float2(0,1)),hash21(q+1),f.x),f.y);}
 float star(float2 p){float r=length(p);return pow(saturate(1-abs(p.x)*abs(p.y)*48-r*.8),4)*saturate(1-r);}
 fixed4 frag(v2f i):SV_Target {float2 p=(i.uv-.5)*2;float r=length(p);float a=exp(-r*r*5)*smoothstep(1,.7,r);
 float grain=noise(p*9+float2(_FxTime*3,-_FxTime*5));
 if(_Mode>0.5&&_Mode<1.5){float angle=atan2(p.y,p.x);float rim=.74+.035*sin(angle*7+_FxTime*7)+.025*sin(angle*17-_FxTime*11);float width=lerp(.075,.15,noise(p*5));a=exp(-pow((r-rim)/width,2))*(.32+1.2*smoothstep(.22,.7,grain))*smoothstep(1,.87,r);}
 if(_Mode>1.5&&_Mode<2.5)a=pow(saturate(1-abs(p.x)*abs(p.y)*35-r*.65),4)*saturate(1-r);
 if(_Mode>2.5&&_Mode<3.5){float y=i.uv.y;float x=p.x+.12*sin(y*15+_FxTime*40);a=pow(saturate(1-abs(x)/(max(.03,(1-y)*.8))),1.5)*smoothstep(0,.12,y)*pow(1-y,.5);}
 if(_Mode>3.5&&_Mode<4.5){float angle=atan2(p.y,p.x);float warp=angle+.12*sin(angle*5)+.04*sin(angle*11);float rays=pow(saturate(sin(warp*13+.7)),2.5)+.4*pow(saturate(sin(warp*23-1.8)),5);float reach=.66+.22*sin(angle*9+1.7);a=rays*smoothstep(.025,.16,r)*(1-smoothstep(reach-.28,reach+.12,r))*(.45+.55*grain);}
 if(_Mode>4.5&&_Mode<5.5){float rim=.76+(grain-.5)*.13;float open=smoothstep(.07,.21,_FxTime);float holes=smoothstep(.28,.70,noise(p*6+float2(_FxTime*7,0)));a=(1-smoothstep(rim-.18,rim+.035,r))*lerp(1,holes,open)*(1-open*.85*(1-smoothstep(.15,.60,r)));}
 if(_Mode>5.5&&_Mode<6.5){float angle=atan2(p.y,p.x);float rim=.68+.06*sin(angle*11+_FxTime*48)+.025*sin(angle*23-_FxTime*60);a=exp(-pow((r-rim)*13,2))*(.85+.15*sin(angle*19+_FxTime*70))*smoothstep(1,.86,r);}
 // One pooled quad per cell carries the little break ring and four distinct stars.
 if(_Mode>6.5&&_Mode<7.5){float t=saturate(_FxTime/.55);a=exp(-r*r*14)*(1-smoothstep(0,.22,t));a+=exp(-pow((r-(.25+t*.36))*15,2))*.32*(1-t);[unroll]for(int n=0;n<4;n++){float angle=n*1.5708+.4;float2 q=p-float2(cos(angle),sin(angle))*(.28+t*.35);float flash=.5+.5*sin(_FxTime*32+n*2);a+=star(q/(.18+.07*flash))*(.65+.55*flash);}a*=1-smoothstep(.45,1,t);}
 if(_Mode>7.5){a=exp(-r*r*3.8)*(.25+.75*noise(p*5+float2(_FxTime,-_FxTime*2)))*smoothstep(1,.7,r);}
 // Gold rays/rings need slight coverage against the pale board; purely additive
 // yellow over lavender clips to white before the gold edge can be read.
 float3 tint=_Color.rgb;
 if(_Mode>3.5&&_Mode<4.5)tint=lerp(float3(1,.98,.83),tint,smoothstep(.10,.60,r));
 if(_Mode>.5&&_Mode<1.5)tint=lerp(tint,float3(1,.99,.86),smoothstep(.65,1,grain));
 float cover=(_Mode>.5&&_Mode<1.5||_Mode>3.5&&_Mode<4.5)?saturate(a*_Color.a*.72):0;
 return float4(tint*_Color.a*a,cover); }
 ENDCG
 } }
}
