Shader "RoyalThrone/ColorBallV5/Light" {
 Properties { _Color("Tint",Color)=(1,1,1,1) _Kind("Glow Star Ring Beam Orbit",Float)=0 _OrbitPhase("Orbit phase",Float)=0 _DstBlend("Destination blend",Float)=1 }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha [_DstBlend]
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
   float4 _Color;float _Kind,_OrbitPhase,_Phase,_Pulse,_Release,_Travel,_After,_LengthCells,_Contact,_ContactAge,_Detail;
   v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;return o;}
   float bell(float x,float w){return exp(-x*x/(w*w));}
   // Smooth, spatially coherent modulation. It changes energy, never the metal geometry.
   float flow(float2 p){return .5+.25*sin(p.x*7+p.y*3)+.16*sin(p.x*13-p.y*9)+.09*cos(p.x*23+p.y*5);}
   float4 frag(v2f i):SV_Target{
    float2 p=(i.uv-.5)*2;float r=length(p),a=0;
    if(_Contact>.5){
     float t=_ContactAge,opening=smoothstep(0,.07,t),fade=1-smoothstep(.07,.22,t);
     float2 q=p/lerp(.20,.72,opening);
     float center=exp(-dot(q,q)*7)*(1-smoothstep(.025,.12,t));
     float cloud=exp(-dot(q*float2(.85,1.3),q*float2(.85,1.3))*3)*fade;
     float flare=bell(q.y+.055*sin(q.x*9-t*32),.065)*exp(-abs(q.x)*2.2)*fade;
     float wing=bell(q.x+.12*q.y,.10)*exp(-abs(q.y)*4)*fade*.40;
     // The arriving channel opens into two unequal, curved shoulders, then a short residue.
     float shoulder=bell(q.y-(.12+.16*q.x*q.x),.09)*exp(-abs(q.x)*2.4);
     shoulder+=bell(q.y+(.10+.10*q.x*q.x),.07)*exp(-abs(q.x+.13)*3.3)*.65;
     float residue=shoulder*smoothstep(.012,.055,t)*(1-smoothstep(.085,.22,t));
     a=center*1.5+cloud*.70+flare*.8+wing+_Detail*(center*.32+residue*.60+flare*.22);
     return float4(lerp(_Color.rgb,float3(1,1,.97),saturate(center+flare*.8+_Detail*residue*.15)),_Color.a*a*saturate((1-r)*7));
    }
    if(_Kind<.5)a=lerp(exp(-r*r*6),exp(-r*r*r*r*4),_Release)*saturate((1-r)*5);
    else if(_Kind<1.5){a=pow(saturate(1-pow(abs(p.x),.55)-pow(abs(p.y),.55)),.55);a=max(a,exp(-r*r*80)*.65);}
    else if(_Kind<2.5){
     float angle=atan2(p.y,p.x),d=r-(.70+.018*sin(angle*3+_Phase*11));
     float rim=bell(d,.038),soft=bell(d,.11)*.55;
     float etched=bell(d+.045,.022)*(.5+.5*sin(angle*7+_Phase*9));
     float broken=pow(saturate(sin(angle*3-_Phase*8)),5)*bell(d-.055,.065);
     a=(rim+soft+_Detail*(etched*.32+broken*.38))*(.75+.25*sin(angle*5+_Phase*14))*saturate((1-r)*10);
     return float4(lerp(_Color.rgb,float3(1,1,.97),rim),_Color.a*a);
    }
    else if(_Kind<3.5||(_Kind>4.5&&_Kind<5.5)){
     float x=i.uv.x,span=max(.01,_LengthCells*_Travel),behind=(1-x)*span;
     // Both beam layers share this support width and centerline. Endpoints are exactly pinned.
     float anchor=sin(x*3.141593),path=anchor*(.16*sin(x*span*2.6-_Phase*20)+.075*sin(x*span*5+_Phase*13));
     float y=p.y-path;
     float front=bell(behind-.08,.23)*(1-smoothstep(0,.075,_After));
     float packet=bell(frac(x*span*.75-_Phase*7)-.5,.15);
     float taper=lerp(.38,1,smoothstep(0,.25,x*span)*smoothstep(0,.18,behind));
     float width=(.38+.09*sin(x*span*5-_Phase*24)+.08*packet+.08*front)*taper;
     float ends=smoothstep(0,.009,x)*smoothstep(0,.01,1-x);
     float pulse=.72+.28*sin(x*span*7-_Phase*29);
     if(_Kind<3.5){
      float body=bell(y,width),warm=bell(y,width*1.95);
      float channel=bell(y-width*.67,width*.24)+bell(y+width*.55,width*.32)*.65;
      float gathered=(front*.62+packet*.27)*body+channel*(.15+.20*packet);
      a=((body*.84+warm*.20)*(pulse+front*.25)+_Detail*gathered)*ends*(1-smoothstep(.72,1,abs(p.y)));
      return float4(lerp(_Color.rgb,float3(1,.96,.78),saturate(front*(.6+_Detail*.18)+packet*.2)),saturate(_Color.a*a));
     }
     a=bell(y,width*.40)*ends*(pulse+front*.48+packet*.12+_Detail*(front*.40+packet*.22));
     return float4(float3(1,1,.97),_Color.a*a);
    }
    else if(_Kind>5.5){
     bool front=_Kind>6.5;
     float2 q=p*float2(1,1.13);float angle=atan2(q.y,q.x),rr=length(q);
     float ribbons=0,hot=0,shoulders=0;
     // Unequal finite wisps, with tapered heads/tails, stream around the sphere. No complete hoop.
     [unroll]for(int k=0;k<3;k++){
      float seed=k*2.39996,head=_Phase*(3.7+k*.41)+seed;
      float u=frac((angle-head)/6.283185),lengthArc=.13+.031*sin(seed);
      float envelope=smoothstep(0,.018,u)*(1-smoothstep(lengthArc*.42,lengthArc,u));
      float radius=.43+k*.051+.065*sin(angle*2+seed+_Phase*5)+.022*sin(angle*5-seed-_Phase*3);
      float width=(.038+.020*flow(float2(angle+seed,_Phase*2)))*(.4+.6*envelope);
      float trail=bell(rr-radius,width)*envelope;
      float depth=front?smoothstep(.24,.68,abs(sin(angle))):1;
      float variation=.74+.26*flow(float2(angle*2+_Phase*4,rr*10-seed));
      ribbons+=trail*variation*depth*(.48+.10*k);
      hot+=bell(rr-radius,width*.40)*envelope*depth*.55;
      // Richer energy cross-section follows the same finite wisp, never a new static orbit.
      shoulders+=bell(rr-radius-width*.65,width*1.7)*envelope*depth*variation*.38;
      hot+=_Detail*bell(rr-radius+width*.18,width*.23)*envelope*depth*(.30+.20*variation);
     }
     float drift=.10*sin(_Phase*8),upper=exp(-dot((p-float2(drift,.38))*float2(2.5,4.5),(p-float2(drift,.38))*float2(2.5,4.5)));
     float lower=exp(-dot((p-float2(-drift,-.37))*float2(3.2,5.5),(p-float2(-drift,-.37))*float2(3.2,5.5)));
     float soft=(upper+lower)*(1.12+.18*sin(_Phase*13))*(front?.27:1.05);
     float side=exp(-dot(p*float2(1.9,2.8),p*float2(1.9,2.8)))*(front?0:.18);
     float edge=saturate((1-r)*8);
     a=(ribbons*.65+soft+side+_Detail*(shoulders+ribbons*.22+hot*.18))*edge*(1+_Pulse*(.22+_Detail*.13));
     return float4(lerp(_Color.rgb,float3(1,1,.98),saturate(hot+soft*.78)),_Color.a*a);
    }
    else {float q=i.uv.y*2-1,theta=i.uv.x*6.283185,d=frac((theta-_OrbitPhase)/6.283185);a=exp(-d*20)*exp(-q*q*9);}
    return float4(_Color.rgb,_Color.a*a);
   }
   ENDCG
  }
 }
}
