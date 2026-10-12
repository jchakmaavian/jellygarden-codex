Shader "RoyalThrone/ColorBallV3/Sphere"
{
 Properties { _Layer("Body / Surface",Float)=0 _Opacity("Opacity",Float)=1 _Angle("Surface longitude",Float)=0 _Exposure("Angular shutter",Float)=0 _Charge("Charge",Float)=0 }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
   float _Layer,_Opacity,_Angle,_Exposure,_Charge;
   v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;return o;}
   float3 candy(float k){
    k=fmod(k,6);
    if(k<1)return float3(1,.045,.12); if(k<2)return float3(.01,.66,1);
    if(k<3)return float3(1,.59,.015); if(k<4)return float3(.15,.88,.035);
    if(k<5)return float3(.92,.045,.88); return float3(.02,.32,1);
   }
   float4 dots(float3 n,float angle){
    // Inverse projection onto the sphere: circles foreshorten at the silhouette and wrap behind it.
    float3 axis=normalize(float3(.22,1,.10));
    float cs=cos(angle),sn=sin(angle);float3 surface=n*cs+cross(axis,n)*sn+axis*dot(axis,n)*(1-cs);
    float latitude=asin(surface.y), longitude=atan2(surface.x,surface.z);
    float row=floor((latitude+1.570796)/.51);
    float rows=6;float ly=(row+.5)*.51-1.570796;
    float columns=round(12*cos(ly));columns=max(3,columns);
    float u=longitude/6.283185*columns+fmod(row,2)*.5;
    float column=floor(u+.5);float2 q=float2((u-column)*6.283185/columns*cos(ly),latitude-ly)/.172;
    float r=dot(q,q),aa=max(fwidth(r),.025);float a=1-smoothstep(1-aa,1+aa,r);
    float z=sqrt(saturate(1-r));
    float3 bump=normalize(n+float3(q.x,q.y,0)*.28+float3(0,0,z*.45));
    float light=saturate(dot(bump,normalize(float3(-.45,.65,1))));
    float3 base=candy(fmod(column+columns*10,columns)+row*2);
    float spec=pow(saturate(dot(normalize(float3(q.x,q.y,z)),normalize(float3(-.4,.5,1)))),24);
    float3 rgb=base*(.45+.62*light)+spec*.58+base*_Charge*.20;
    return float4(rgb,a);
   }
   float4 frag(v2f i):SV_Target{
    float2 p=(i.uv-.5)*2;float r=dot(p,p);float aa=max(fwidth(r),.004);
    float edge=1-smoothstep(1-aa,1+aa,r);clip(edge-.001);
    float3 n=float3(p,sqrt(saturate(1-r)));
    if(_Layer<.5){
     float light=saturate(dot(n,normalize(float3(-.45,.6,1))));
     float spec=pow(saturate(dot(n,normalize(float3(-.27,.40,1)))),34);
     float rim=pow(1-n.z,3);
     float3 rgb=lerp(float3(.012,.022,.072),float3(.038,.16,.38),light)*(.67+.33*n.z);
     rgb+=spec*float3(.36,.48,.60)+rim*float3(.025,.075,.17);
     rgb+=_Charge*.12*pow(n.z,8)*float3(.8,.88,1);
     return float4(rgb,edge*_Opacity);
    }
    float4 sum=0;
    // Continuous analytical rotation with a short shutter, independent of render frame rate.
    [unroll]for(int s=0;s<6;s++){float4 d=dots(n,_Angle-(s/5.0)*_Exposure);sum+=float4(d.rgb*d.a,d.a)/6;}
    return float4(sum.rgb/max(.001,sum.a),sum.a*edge*_Opacity);
   }
   ENDCG
  }
 }
}
