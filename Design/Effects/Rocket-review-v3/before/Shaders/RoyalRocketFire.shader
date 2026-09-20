// Rocket-only procedural shapes. No texture atlas or vendor shader dependency.
Shader "RoyalThrone/RocketFire"
{
    Properties
    {
        _Shape ("0 trail, 1 flame, 2 star, 3 ring, 4 flash", Float) = 0
        _Boost ("Brightness", Float) = 1
        _FxTime ("Scaled animation time", Float) = 0
        _Phase ("Flame phase", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Off
        Lighting Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Shape, _Boost, _FxTime, _Phase;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 position:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            v2f vert(appdata v)
            {
                v2f o; o.position=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o;
            }
            float4 frag(v2f i):SV_Target
            {
                float2 p=i.uv*2-1;
                float alpha=0;
                float3 tint=1;
                if(_Shape<.5)
                {
                    // Soft width, continuous along the trail; the renderer controls its tail fade.
                    alpha=exp(-p.y*p.y*5.5)*smoothstep(1,.78,abs(p.y));
                }
                else if(_Shape<1.5)
                {
                    // Nozzle at u=1. Heat tongues flow toward the tapered tail at u=0.
                    float tail=1-i.uv.x;
                    float clock=_FxTime+_Phase;
                    float wave=sin(tail*19-clock*31)*.035*tail + sin(tail*37-clock*47)*.018*tail;
                    float width=(.28+.32*sin(tail*3.14159))*pow(saturate(1-tail),.72);
                    width*=.9+.10*sin(tail*24-clock*39)+.045*sin(tail*53-clock*63);
                    float side=abs(p.y-wave)/max(.008,width);
                    alpha=smoothstep(1.12,.64,side)*smoothstep(1,.88,tail);
                    float hot=smoothstep(.62,.09,side)*smoothstep(.98,.13,tail);
                    float gold=smoothstep(1.05,.40,side);
                    tint=lerp(float3(1,.19,.015),float3(1,.68,.045),gold);
                    tint=lerp(tint,float3(1,.98,.77),hot);
                }
                else if(_Shape<2.5)
                {
                    // Crisp four-point white/gold stars with a soft luminous centre.
                    float diamond=pow(abs(p.x),.55)+pow(abs(p.y),.55);
                    alpha=smoothstep(1.12,.8,diamond)+exp(-dot(p,p)*20)*.32;
                }
                else if(_Shape<3.5)
                {
                    float r=length(p);
                    alpha=exp(-pow((r-.65)*13,2))*.82;
                }
                else
                {
                    float r=length(p);
                    float rays=pow(abs(sin(atan2(p.y,p.x)*4)),14);
                    alpha=exp(-r*r*7)+rays*exp(-r*r*3)*.24;
                    alpha*=smoothstep(1,.80,r);
                }
                float3 rgb=tint*i.color.rgb*saturate(alpha)*i.color.a*_Boost;
                return float4(rgb,0);
            }
            ENDCG
        }
    }
    Fallback Off
}
