Shader "Royal Throne/Area Layer"
{
    Properties
    {
        _MainTex ("Artwork",2D)="white" {}
        _Color ("Tint",Color)=(1,1,1,1)
        _NoiseTex ("Flow noise",2D)="gray" {}
        _ReflectionTex ("Scene reflection",2D)="black" {}
        _Water ("Water",Float)=0
        _FlowKind ("Pool / river / distant",Float)=0
        _WaterSize ("Water sprite size in scene units",Vector)=(1,1,0,0)
        _Clock ("Animation clock",Float)=0
        _WaterLine ("Reflection surface",Float)=.5
        _MaskMode ("Inside / outside mask",Float)=0
        _MaskRect ("Mask rectangle",Vector)=(0,0,1,1)
        _DstBlend ("Destination blend",Float)=10
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
        Cull Off Lighting Off ZWrite Off ZTest Always
        Blend SrcAlpha [_DstBlend], One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex,_NoiseTex,_ReflectionTex;
            float4 _MainTex_TexelSize,_ReflectionTex_TexelSize,_Color,_MaskRect,_WaterSize;
            float4x4 _WorldToMask;
            float _Water,_Clock,_WaterLine,_FlowKind,_MaskMode;
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 screen:TEXCOORD1;float2 mask:TEXCOORD2;fixed4 color:COLOR;};
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.screen=ComputeScreenPos(o.vertex);o.mask=mul(_WorldToMask,mul(unity_ObjectToWorld,v.vertex)).xy;o.color=v.color*_Color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                if(_MaskMode>.5){float inside=step(_MaskRect.x,i.mask.x)*step(_MaskRect.y,i.mask.y)*step(i.mask.x,_MaskRect.z)*step(i.mask.y,_MaskRect.w);clip((_MaskMode<1.5?inside:1-inside)-.5);}
                fixed4 original=tex2D(_MainTex,i.uv);clip(original.a-.002);
                fixed3 rgb=original.rgb;
                if(_Water>.5){
                    // Every pattern is measured in the sprite's own scene units (_WaterSize), so it keeps one size whatever the
                    // sprite's shape: on the long, flat sea the UV-based ripple repeated about every screen pixel and aliased into a
                    // vibration. Now: a slow swell of two noise layers, a long ripple (about 0.4 units) and soft highlights about
                    // 0.3 units apart; the water shifts by at most about 0.04 units (a few pixels), half that on distant water.
                    float2 size=max(_WaterSize.xy,float2(.01,.01)),p=i.uv*size;
                    float river=step(.5,_FlowKind),farWater=step(1.5,_FlowKind);
                    float2 flow=lerp(float2(.05,.03),float2(.16,-.04),river);
                    flow=lerp(flow,float2(.05,.008),farWater);
                    float2 n1=tex2D(_NoiseTex,frac(p*float2(.55,1.3)+_Clock*flow)).rg-.5;
                    float2 n2=tex2D(_NoiseTex,frac(p*float2(1.1,.8)-_Clock*flow*.63+float2(.37,.12))).rg-.5;
                    float2 ripple=float2(sin(p.y*15.7-_Clock*1.6),cos(p.x*12.6+_Clock*1.1))*.004;
                    float2 drift=(n1*.05+n2*.025+ripple)*(1-farWater*.5)/size;
                    fixed3 a=tex2D(_MainTex,saturate(i.uv+drift)).rgb;
                    float streak=pow(saturate(.5+.5*sin(p.y*22-p.x*4.5-_Clock*(1.1+river*1.4)+n1.r*4)),16);
                    float sparkle=pow(saturate(.5+.5*sin((p.x+p.y)*18+_Clock*.9+n2.g*4)),14);
                    a+=fixed3(.09,.30,.35)*(streak*.14+sparkle*.10);
                    float2 screen=i.screen.xy/i.screen.w;
                    float2 mirror=float2(screen.x+drift.x,_WaterLine*2-screen.y+drift.y*.7);
                    #if UNITY_UV_STARTS_AT_TOP
                    if(_ReflectionTex_TexelSize.y<0)mirror.y=1-mirror.y;
                    #endif
                    fixed3 reflection=tex2D(_ReflectionTex,saturate(mirror)).rgb;
                    float rim=smoothstep(.02,.18,i.uv.y)*smoothstep(1,.72,i.uv.y);
                    rgb=lerp(a,reflection*fixed3(.69,.88,1),rim*(.14-farWater*.06));
                }
                return fixed4(rgb*(_Water>.5?1:i.color.rgb),original.a*i.color.a);
            }
            ENDCG
        }
    }
}
