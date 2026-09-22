Shader "RoyalThrone/ColorBallV5/Light"
{
 Properties { _Color("Tint",Color)=(1,1,1,1) _Kind("Glow Star Ring Beam Orbit",Float)=0 _OrbitPhase("Orbit phase",Float)=0 _DstBlend("Destination blend",Float)=1 }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha [_DstBlend]
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;}; float4 _Color;float _Kind,_OrbitPhase,_Phase,_Pulse,_Release,_Travel,_After,_LengthCells,_Contact,_ContactAge;
   v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;return o;}
   float4 frag(v2f i):SV_Target{
    float2 p=(i.uv-.5)*2;float r=length(p),a=0;
    if(_Contact>.5){
     // Arrival gathers into a hot point, then opens into a compact warm collar.
     float opening=smoothstep(0,.060,_ContactAge),radius=lerp(.10,.63,opening);
     float hot=exp(-r*r/lerp(.020,.065,opening))*(1-smoothstep(.025,.11,_ContactAge));
     float collar=exp(-pow((r-radius)/.16,2))*.72;
     float angle=atan2(p.y,p.x);
     float petals=pow(saturate(cos(angle*4+_ContactAge*4)),16)*exp(-pow((r-radius)/.24,2))*.24;
     a=(hot*1.3+collar+petals)*saturate((1-r)*8);
     return float4(lerp(_Color.rgb,float3(1,1,.96),saturate(hot)),_Color.a*a);
    }
    if(_Kind<.5)a=lerp(exp(-r*r*6),exp(-r*r*r*r*4),_Release)*saturate((1-r)*5);
    else if(_Kind<1.5){a=pow(saturate(1-pow(abs(p.x),.55)-pow(abs(p.y),.55)),.55);a=max(a,exp(-r*r*80)*.65);}
    else if(_Kind<2.5){
     float d=abs(r-.76),angle=atan2(p.y,p.x);
     float streaks=pow(saturate(cos(angle*11+_Phase*3)),28)*exp(-pow((r-.62)/.19,2));
     a=(exp(-pow(d/.030,2))*1.4+exp(-pow(d/.12,2))*.58+streaks*.40)*saturate((1-r)*12);
     a*=.93+.07*sin(angle*16+_Phase*18);
     return float4(lerp(_Color.rgb,float3(1,1,.96),exp(-pow(d/.035,2))),_Color.a*a);
    }
    else if(_Kind<3.5||(_Kind>4.5&&_Kind<5.5)){
     float x=i.uv.x,span=max(.01,_LengthCells*_Travel),behind=(1-x)*span;
     // The leading energy rides the actual arriving front, in cell units, on every shot length.
     float front=exp(-pow((behind-.12)/.24,2))*(1-smoothstep(0,.085,_After));
     float wave=sin(x*span*9-_Phase*24),residue=smoothstep(0,.14,_After);
     float taper=lerp(.22,1,smoothstep(0,.16,x*span)*smoothstep(0,.18,behind));
     float y=(p.y-.07*wave*sin(x*3.14159))/taper;
     float ends=smoothstep(0,.012,x)*smoothstep(0,.012,1-x);
     float flow=.88+.12*sin(x*span*7-_Phase*26);
     if(_Kind<3.5){
      float shoulder=exp(-pow(abs(y)/(.56+.10*wave+.10*front),4));
      float soft=exp(-y*y*2.8)*saturate((1-abs(p.y))*4);
      a=(shoulder*.78+soft*.32)*ends*(flow+.38*front);
      return float4(lerp(_Color.rgb,float3(1,.96,.80),residue*.65),saturate(_Color.a*a));
     }
     a=(1-smoothstep(.32,.95,abs(y)))*ends*(flow+.48*front);
    }
    else if(_Kind>5.5){
     float angle=atan2(p.y*1.18,p.x);
     float shape=.040*sin(angle*3+_Phase*16)+.025*sin(angle*5-_Phase*12);
     float rr=length(p*float2(1,1.18))-shape,edge=saturate((1-r)*8);
     float broken=.68+.22*sin(angle*3-_Phase*9)+.10*cos(angle*7+_Phase*12);
     float inner=exp(-pow((rr-.62)/.052,2))*broken;
     float falloff=exp(-pow((rr-.66)/.20,2));
     float rays=pow(saturate(cos(angle*7-_Phase*4+rr*5)),22)*exp(-pow((rr-.78)/.19,2));
     if(_Kind<6.5){
      a=(inner*.85+falloff*.58+rays*.50+exp(-rr*rr*4)*.20)*edge;
      return float4(lerp(_Color.rgb,float3(1,1,.97),saturate(inner*1.4)),_Color.a*a);
     }
     // Moving upper/lower edges pass in front; the center remains transparent to the artwork.
     float frontMask=smoothstep(.12,.45,abs(p.y));
     a=(inner*.74+falloff*.15+rays*.28)*edge*frontMask;
     return float4(lerp(_Color.rgb,float3(1,1,.98),saturate(inner*1.3)),_Color.a*a);
    }
    else {
     float q=i.uv.y*2-1,theta=i.uv.x*6.283185;
     float d=frac((theta-_OrbitPhase)/6.283185);float d2=frac(d+.47);
     float comet=exp(-d*20)+.65*exp(-d2*23);
     float ribbon=(.14+comet*1.15)*exp(-q*q*9);
     float hot=exp(-q*q*100)*(.18+comet*1.3);
     return float4(lerp(_Color.rgb,float3(1,.98,.92),saturate(hot*2)),_Color.a*(ribbon+hot));
    }
    return float4(_Color.rgb,_Color.a*a);
   }
   ENDCG
  }
 }
}
