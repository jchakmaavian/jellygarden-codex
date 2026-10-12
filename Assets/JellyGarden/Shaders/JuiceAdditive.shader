// Additive, vertex-coloured unlit shader used only by the match-juice VFX layers.
// Kept deliberately minimal: no lighting, no fog, no depth write - these are short
// lived overlay particles drawn above the board sprites.
//
// Two consumers with DIFFERENT colour plumbing share this shader:
//
//   ParticleSystemRenderer - supplies colour per particle in the vertex stream.
//   SpriteRenderer         - supplies SpriteRenderer.color via the _RendererColor
//                            uniform, which lives in the UnityPerDrawSprite constant
//                            buffer. Unity only populates that buffer for sprite
//                            draws; for a particle draw it is unset, so reading it
//                            there multiplies the colour by garbage/zero and the
//                            particles vanish.
//
// The SPRITE_RENDERER_COLOR keyword selects between them. MatchVFXService enables it
// on the overlay material only.
Shader "JellyGarden/JuiceAdditive"
{
    Properties
    {
        [PerRendererData] _MainTex ("Particle Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Tint ("Tint", Color) = (1,1,1,1)
        // Pushes the additive output above 1.0 so HDR bloom picks up the sparkles
        // while the board art, which sits below 1.0, stays untouched.
        _Boost ("HDR Boost", Float) = 1
        [Toggle(SPRITE_RENDERER_COLOR)] _UseRendererColor ("Use SpriteRenderer.color", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Both variants are compiled unconditionally so neither can be stripped.
            #pragma multi_compile_local __ SPRITE_RENDERER_COLOR
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Tint;
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

                fixed4 c = v.color * _Color * _Tint;
                #ifdef SPRITE_RENDERER_COLOR
                c *= _RendererColor;
                #endif
                o.color = c;

                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 tex = tex2D(_MainTex, i.texcoord);
                float4 col = tex * i.color;
                // Premultiply by alpha so the additive blend fades out cleanly
                // instead of leaving a bright residue as particles die.
                col.rgb *= col.a * _Boost;
                return col;
            }
            ENDCG
        }
    }

    Fallback Off
}
