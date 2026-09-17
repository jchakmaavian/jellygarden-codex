Shader "RoyalThrone/TNTGlow" {
 Properties { _Color("Color",Color)=(1,1,1,1) _Mode("Shape",Float)=0 _FxTime("Time",Float)=0 _Seed("Variation",Float)=0 }
 SubShader { Tags {"Queue"="Transparent+10" "RenderType"="Transparent"} Pass {
 Blend One OneMinusSrcAlpha ZWrite Off Cull Off
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _Color; float _Mode,_FxTime,_Seed;
 struct v2f { float4 pos:SV_POSITION;float2 uv:TEXCOORD0; };
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;return o;}
 float hash21(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 q=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash21(q),hash21(q+float2(1,0)),f.x),lerp(hash21(q+float2(0,1)),hash21(q+1),f.x),f.y);}
 float star(float2 p){float r=length(p);return pow(saturate(1-abs(p.x)*abs(p.y)*48-r*.8),4)*saturate(1-r);}
 fixed4 frag(v2f i):SV_Target {float2 p=(i.uv-.5)*2;float r=length(p);float a=exp(-r*r*5)*smoothstep(1,.7,r);
 float grain=noise(p*9+float2(_FxTime*3,-_FxTime*5));
 // Disconnected luminous patches travel with the wave. There is no solid ring.
 if(_Mode>0.5&&_Mode<1.5){float rim=.70+.09*(noise(p*4)-.5);float patch=smoothstep(.38,.70,noise(p*13-float2(_FxTime*2,0)));a=exp(-pow((r-rim)/.17,2))*patch*1.5*smoothstep(1,.84,r);}
 if(_Mode>1.5&&_Mode<2.5)a=pow(saturate(1-abs(p.x)*abs(p.y)*35-r*.65),4)*saturate(1-r);
 if(_Mode>2.5&&_Mode<3.5){float y=i.uv.y;float x=p.x+.12*sin(y*15+_FxTime*40);a=pow(saturate(1-abs(x)/(max(.03,(1-y)*.8))),1.5)*smoothstep(0,.12,y)*pow(1-y,.5);}
 // Layer broad streaks over an illuminated interior; suppress the pinwheel hub.
 if(_Mode>3.5&&_Mode<4.5){float angle=atan2(p.y,p.x);float warp=angle+.17*sin(angle*5)+.10*noise(p*3);float rays=pow(saturate(sin(warp*11+.7)),1.6)+.3*pow(saturate(sin(warp*19-1.8)),3);float reach=.74+.14*sin(angle*9+1.7);a=(.13+rays*.56)*smoothstep(.08,.30,r)*(1-smoothstep(reach-.32,reach+.13,r))*(.30+.70*grain);}
 if(_Mode>4.5&&_Mode<5.5){float rim=.76+(grain-.5)*.13;float open=smoothstep(.045,.14,_FxTime);float holes=smoothstep(.36,.75,noise(p*7+float2(_FxTime*3,0)));a=(1-smoothstep(rim-.22,rim+.035,r))*lerp(1,holes*.6,open)*(1-open*.92*(1-smoothstep(.10,.62,r)));}
 if(_Mode>5.5&&_Mode<6.5){float angle=atan2(p.y,p.x);float rim=.68+.06*sin(angle*11+_FxTime*48)+.025*sin(angle*23-_FxTime*60);a=exp(-pow((r-rim)*13,2))*(.85+.15*sin(angle*19+_FxTime*70))*smoothstep(1,.86,r);}
 // Cell-local sparks vary between cells. The break arc lasts only the impact beat.
 if(_Mode>6.5&&_Mode<7.5){float t=saturate(_FxTime/.60);a=exp(-r*r*12)*(1-smoothstep(0,.12,_FxTime));a+=exp(-pow((r-(.23+t*.6))*16,2))*.22*(1-smoothstep(.025,.085,_FxTime))*smoothstep(.25,.65,grain);[unroll]for(int n=0;n<6;n++){float h=hash21(float2(n+_Seed,7));float angle=n*2.39996+_Seed;float reach=.1+h*.42+t*.23;float2 q=p-float2(cos(angle),sin(angle))*reach-float2(0,-t*t*.14);float flash=pow(.5+.5*sin(_FxTime*(18+h*17)+h*30),2);a+=star(q/(.10+h*.14))*(.35+1.1*flash);}a*=1-smoothstep(.35,1,t);}
 // An uneven soft light field fills the blast interior and persists into the fall.
 if(_Mode>7.5){float cloud=noise(p*5/(1+_FxTime)+float2(_FxTime*.6,-_FxTime));float fine=noise(p*16+_FxTime);a=exp(-r*r*2.3)*(.14+.70*smoothstep(.22,.75,cloud)+.20*smoothstep(.65,.85,fine))*smoothstep(1,.78,r);}
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
