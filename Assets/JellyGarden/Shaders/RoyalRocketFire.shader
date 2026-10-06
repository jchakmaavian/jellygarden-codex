// Rocket-only procedural shapes. No texture atlas or vendor shader dependency.
Shader "RoyalThrone/RocketFire"
{
    Properties
    {
        _Shape ("0 trail, 1 flame, 2 star, 3 ring, 4 flash, 5 flame ribbon, 6 white smoke", Float) = 0
        _Boost ("Brightness", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination blend", Float) = 1
        _FxTime ("Scaled animation time", Float) = 0
        _TravelFraction ("Visible fraction of the seven-cell plume", Float) = 1
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
            float _Shape, _Boost, _FxTime, _Phase, _TravelFraction;
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
                    // Compact where it leaves the nozzle, widest a third of the way back.
                    float width=(.30+.34*sin(tail*3.14159))*pow(saturate(1-tail),.72);
                    width*=.9+.10*sin(tail*24-clock*39)+.045*sin(tail*53-clock*63);
                    float side=abs(p.y-wave)/max(.008,width);
                    alpha=smoothstep(1.12,.64,side)*smoothstep(1,.88,tail);
                    float hot=smoothstep(.62,.09,side)*smoothstep(.98,.13,tail);
                    float gold=smoothstep(1.05,.40,side);
                    tint=lerp(float3(1,.48,.05),float3(1,.86,.26),gold);
                    tint=lerp(tint,float3(1,1,.90),hot);
                }
                else if(_Shape<2.5)
                {
                    // Crisp four-point white/gold stars with a soft luminous centre.
                    float diamond=pow(abs(p.x),.45)+pow(abs(p.y),.45);
                    alpha=smoothstep(1.15,.72,diamond)+exp(-dot(p,p)*26)*.42;
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
                if(_Shape>4.5 && _Shape<5.5)
                {
                    // UV distance is measured behind the nozzle, not elapsed flight length.
                    // For a mature plume age*7 is roughly the cell count behind the nozzle.
                    float age=(1-i.uv.x)*_TravelFraction;
                    float clock=_FxTime+_Phase;
                    // Compact directly at the nozzle, swelling through the bright body, then
                    // narrowing again so the smoke particles carry the silhouette outward.
                    float rise=smoothstep(0,.13,age);
                    float fall=smoothstep(.64,1,age);
                    float taper=lerp(.22,.72,rise);
                    taper=lerp(taper,.25,fall);
                    float waves=.055*sin(age*61-clock*23)+.035*sin(age*113+clock*31);
                    float edge=taper+waves*(.35+.65*rise);
                    float side=abs(p.y+.030*sin(age*79-clock*34)*rise);
                    // Long, gentle dissolve that still reaches zero exactly at the ribbon end,
                    // so the trail never shows a hard cutoff.
                    float tailFade=pow(1-smoothstep(.70,1,age),.78);
                    alpha=smoothstep(edge+.17,edge-.12,side)*tailFade;
                    // Even a short plume that exits early needs a soft cap at its birth point.
                    alpha*=smoothstep(0,.08,i.uv.x*_TravelFraction);
                    float tongues=.040*sin(age*91-clock*35)+.022*sin(age*173+clock*41);
                    float gold=smoothstep(edge,.30*edge,side+tongues);
                    float hot=smoothstep(lerp(.20,.34,rise)*edge,.045*edge,side+tongues);
                    float3 fire=lerp(float3(1,.52,.06),float3(1,.82,.22),gold);
                    fire=lerp(fire,float3(1,1,.92),hot);
                    // Gold survives well past the first cell, cooling through the star-lit
                    // body into the soft white tail rather than turning white immediately.
                    tint=lerp(fire,float3(1,1,.98),smoothstep(.60,.86,age));
                }
                else if(_Shape>5.5)
                {
                    // Textureless soft white smoke/light, never dark smoke or a square quad.
                    float r=length(p);
                    alpha=exp(-r*r*3.4)*smoothstep(1,.45,r);
                }
                float3 rgb=tint*i.color.rgb*saturate(alpha)*i.color.a*_Boost;
                return float4(rgb,saturate(alpha)*i.color.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
