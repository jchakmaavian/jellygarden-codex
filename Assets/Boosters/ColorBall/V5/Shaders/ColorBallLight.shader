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
   float4 _Color;float _Kind,_OrbitPhase,_Phase,_Pulse,_Release,_Travel,_After,_LengthCells,_Contact,_ContactAge,_Detail,_Leading,_Aim,_SourceFlare;
   v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;return o;}
   float bell(float x,float w){return exp(-x*x/(w*w));}
   // Smooth, spatially coherent modulation. It changes energy, never the metal geometry.
   float flow(float2 p){return .5+.25*sin(p.x*7+p.y*3)+.16*sin(p.x*13-p.y*9)+.09*cos(p.x*23+p.y*5);}
   float4 frag(v2f i):SV_Target{
    float2 p=(i.uv-.5)*2;float r=length(p),a=0;
    if(_SourceFlare>.5){
     float hot=bell(p.y,.24)*exp(-pow((p.x+.18)*2.1,2));
     float warm=bell(p.y,.48)*exp(-pow((p.x+.10)*1.65,2));
     float curl=bell(p.y-.14*sin(p.x*3),.17)*exp(-p.x*p.x*4);
     return float4(lerp(float3(1,.84,.40),float3(1,1,.95),hot),_Color.a*(hot*.68+warm*.38+curl*.16)*saturate((1-r)*5));
    }
    if(_Leading>.5){
     float core=exp(-dot(p*float2(2.7,2.0),p*float2(2.7,2.0)));
     float wake=bell(p.y,.38)*exp(-pow((p.x+.20)*1.6,2));
     return float4(lerp(float3(1,.87,.43),float3(1,1,.98),core),_Color.a*(core+wake*.40)*saturate((1-r)*5));
    }
    if(_Contact>.5){
     float t=_ContactAge,opening=smoothstep(0,.065,t),fade=1-smoothstep(.045,.22,t);
     float2 q=p/lerp(.32,.90,opening);
     float center=exp(-dot(q,q)*4.3)*(1-smoothstep(.055,.14,t));
     // Incoming axial energy opens sideways into a soft petal-shaped contact, not a ring/bubble.
     float cloud=exp(-dot(q*float2(1.25,.90),q*float2(1.25,.90))*3)*fade;
     float channel=bell(q.y,.24)*exp(-abs(q.x+.12)*2.2)*fade;
     float shoulder=bell(q.x+.30*q.y*q.y-.06,.13)*exp(-abs(q.y)*2.5);
     shoulder+=bell(q.x-.24*q.y*q.y+.08,.10)*exp(-abs(q.y)*3)*.55;
     float residue=shoulder*smoothstep(0,.025,t)*(1-smoothstep(.08,.22,t));
     a=center*1.28+cloud*(.86+_Detail*.16)+channel*.64+residue*(.6+_Detail*.35);
     return float4(lerp(float3(1,.84,.38),float3(1,1,.98),saturate(center*1.5+channel*.65)),saturate(_Color.a*a)*saturate((1-r)*7));
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
     float anchor=sin(x*3.141593),path=anchor*(.21*sin(x*span*2.0-_Phase*14)+.065*sin(x*span*4+_Phase*9));
     float y=p.y-path;
     float front=bell(behind-.08,.23)*(1-smoothstep(0,.075,_After));
     float packet=bell(frac(x*span*.48-_Phase*4.5)-.5,.23);
     float taper=lerp(.38,1,smoothstep(0,.25,x*span)*smoothstep(0,.18,behind));
     float width=(.44+.045*sin(x*span*4-_Phase*18)+.09*packet+.12*front)*taper;
     float ends=smoothstep(0,.009,x)*smoothstep(0,.01,1-x);
     float pulse=.88+.12*sin(x*span*5-_Phase*19);
     if(_Kind<3.5){
      float body=bell(y,width),warm=bell(y,width*1.70);
      float channel=bell(y-width*.67,width*.24)+bell(y+width*.55,width*.32)*.65;
      float gathered=(front*.62+packet*.27)*body+channel*(.15+.20*packet);
      a=((body*.88+warm*.38)*(pulse+front*.25)+_Detail*gathered*.5)*ends*(1-smoothstep(.80,1,abs(p.y)));
      return float4(lerp(float3(1,.79,.30),float3(1,.96,.73),saturate(body*.65+front*.45+packet*.2)),saturate(_Color.a*a));
     }
     a=bell(y,width*.30)*ends*(pulse+front*.38+packet*.12+_Detail*(front*.22+packet*.15));
     return float4(float3(1,1,.97),_Color.a*a);
    }
    else if(_Kind>5.5){
     bool front=_Kind>6.5;float ribbons=0,hot=0;
     // Tangential streams unfurl from the source. Each bends and tapers outwards;
     // none is a polar contour, closed hoop, or a repeated straight radial spoke.
     [unroll]for(int k=0;k<5;k++){
      float seed=k*2.39996,theta=seed+_Phase*(.75+.12*sin(seed))+.22*sin(_Phase*5+seed);
      float cs=cos(theta),sn=sin(theta);float2 q=float2(p.x*cs+p.y*sn,-p.x*sn+p.y*cs);
      float reach=.62+.11*sin(_Phase*7+seed),u=(q.x-.16)/reach;
      float envelope=smoothstep(0,.16,u)*(1-smoothstep(.34,1,u));
      float curve=.16+.21*u*u+.065*sin(u*5-_Phase*8+seed);
      float width=lerp(.065,.012,saturate(u))*(1+.25*sin(_Phase*6+seed));
      float depth=front?(k%2==0?.28:.62):1;
      float d=q.y-curve;
      ribbons+=bell(d,width*2.8)*envelope*depth*(.26+_Detail*.08);
      hot+=bell(d,width)*envelope*depth*(.7+_Detail*.15);
     }
     float drift=.07*sin(_Phase*8);
     float2 up=(p-float2(drift,.30))*float2(3.2,3.2),down=(p-float2(-drift,-.29))*float2(3.6,3.5);
     float white=(exp(-dot(up,up))+exp(-dot(down,down)))*(front?.19:.95);
     float warm=exp(-dot(p*float2(2.5,2),p*float2(2.5,2)))*(front?.025:.42);
     float2 shot=float2(p.x*cos(_Aim)+p.y*sin(_Aim),-p.x*sin(_Aim)+p.y*cos(_Aim));
     float gathered=bell(shot.y,.095)*exp(-pow((shot.x-.29)*3.6,2))*_Pulse;
     float edge=1-smoothstep(.76,1,r);
     a=(ribbons+hot*.62+white*(.86+.14*sin(_Phase*11))+warm+gathered*.6)*edge*(1+_Pulse*(.22+_Detail*.10));
     return float4(lerp(_Color.rgb,float3(1,1,.97),saturate(hot+white)),saturate(_Color.a*a));
    }
    else {float q=i.uv.y*2-1,theta=i.uv.x*6.283185,d=frac((theta-_OrbitPhase)/6.283185);a=exp(-d*20)*exp(-q*q*9);}
    return float4(_Color.rgb,_Color.a*a);
   }
   ENDCG
  }
 }
}
