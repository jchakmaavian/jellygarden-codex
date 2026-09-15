// Rocket-only procedural shapes. No texture atlas or vendor shader dependency.
Shader "RoyalThrone/RocketFire"
{
    Properties
    {
        _Shape ("0 trail, 1 flame, 2 star, 3 ring, 4 flash, 5 flame ribbon", Float) = 0
        _Boost ("Brightness", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination blend", Float) = 1
        _FxTime ("Scaled animation time", Float) = 0
        _Phase ("Flame phase", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One [_DstBlend]
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
                    alpha=exp(-p.y*p.y*3.2)*smoothstep(1,.78,abs(p.y));
                }
                else if(_Shape<1.5)
                {
                    // Nozzle at u=1. Heat tongues flow toward the tapered tail at u=0.
                    float tail=1-i.uv.x;
                    float clock=_FxTime+_Phase;
                    float wave=sin(tail*19-clock*31)*.035*tail + sin(tail*37-clock*47)*.018*tail;
                    float width=(.48+.28*sin(tail*3.14159))*pow(saturate(1-tail),.72);
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
                else if(_Shape<4.5)
                {
                    float r=length(p);
                    float rays=pow(abs(sin(atan2(p.y,p.x)*4)),14);
                    alpha=exp(-r*r*7)+rays*exp(-r*r*3)*.24;
                    alpha*=smoothstep(1,.80,r);
                }
                if(_Shape>4.5)
                {
                    // A continuous turbulent flame field, not isolated puffs or a thin beam.
                    float along=i.uv.x;
                    float clock=_FxTime+_Phase;
                    float edge=.78+.085*sin(along*16-clock*22)+.055*sin(along*31+clock*29);
                    float side=abs(p.y+.022*sin(along*23-clock*34));
                    alpha=smoothstep(edge+.16,edge-.15,side);
                    float tongues=.045*sin(along*24-clock*35)+.025*sin(along*47+clock*41);
                    float gold=smoothstep(edge,.35,side+tongues);
                    float hot=smoothstep(.34,.08,side+tongues);
                    tint=lerp(float3(1,.22,.015),float3(1,.79,.08),gold);
                    tint=lerp(tint,float3(1,.98,.79),hot);
                }
                float3 rgb=tint*i.color.rgb*saturate(alpha)*i.color.a*_Boost;
                return float4(rgb,saturate(alpha)*i.color.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
