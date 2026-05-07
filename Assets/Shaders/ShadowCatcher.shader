// Invisible plane that catches shadows. Renders only the shadow as a
// darkening overlay on whatever's behind it (the pre-rendered background).
// Apply to a flat plane positioned on the painted floor.

Shader "Custom/ShadowCatcher"
{
    Properties
    {
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "AlphaTest+50" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"

            float _ShadowStrength;

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                LIGHTING_COORDS(0, 1)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float atten = LIGHT_ATTENUATION(i);
                float shadow = (1.0 - atten) * _ShadowStrength;
                return fixed4(0, 0, 0, shadow);
            }
            ENDCG
        }
    }
    Fallback "VertexLit"
}
