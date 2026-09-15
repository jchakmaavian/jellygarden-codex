// Additive, vertex-coloured unlit shader for sparkles, rings, flashes and trails.
//
// Two consumers with different colour plumbing share it:
//   ParticleSystemRenderer / TrailRenderer - colour arrives in the vertex stream.
//   SpriteRenderer - SpriteRenderer.color arrives through _RendererColor, which Unity
//                    fills only for sprite draws. Reading it on a particle draw zeroes
//                    the colour, so the SPRITE_RENDERER_COLOR keyword selects the path.
// Works in the Built-in pipeline and in URP (rendered as SRPDefaultUnlit).
Shader "RoyalThrone/Match3Vfx/Additive"
{
    Properties
    {
        [PerRendererData] _MainTex ("Particle Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Boost ("HDR Boost", Float) = 1
        [Toggle(SPRITE_RENDERER_COLOR)] _UseRendererColor ("Use SpriteRenderer.color", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local __ SPRITE_RENDERER_COLOR
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Boost;

            #ifdef SPRITE_RENDERER_COLOR
            CBUFFER_START(UnityPerDrawSprite)
            fixed4 _RendererColor;
            CBUFFER_END
            #endif

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                fixed4 c = v.color * _Color;
                #ifdef SPRITE_RENDERER_COLOR
                c *= _RendererColor;
                #endif
                o.color = c;
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.texcoord) * i.color;
                // Premultiply so dying particles fade cleanly instead of leaving residue.
                col.rgb *= col.a * _Boost;
                return col;
            }
            ENDCG
        }
    }

    Fallback Off
}
