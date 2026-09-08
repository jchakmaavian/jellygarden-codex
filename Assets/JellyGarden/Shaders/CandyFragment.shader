// Unlit, alpha-blended shader for candy shatter fragments.
//
// Fragments are meshes (not sprites), so they cannot use Sprites/Default: that shader
// multiplies by _RendererColor, which Unity only populates for SpriteRenderer draws.
// On a MeshRenderer that uniform is unset, which would zero the colour out.
//
// This shader takes its tint from _Color instead, which a MaterialPropertyBlock can set
// per fragment - so every fragment fades independently with no extra material instances.
Shader "JellyGarden/CandyFragment"
{
    Properties
    {
        _MainTex ("Candy Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
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

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
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
                // _MainTex_ST is driven per-fragment so the mesh's 0-1 UVs land on the
                // correct region of the texture, which keeps this atlas-safe.
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.texcoord) * _Color;
            }
            ENDCG
        }
    }

    Fallback Off
}
