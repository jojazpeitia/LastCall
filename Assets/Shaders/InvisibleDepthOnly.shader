// Two-pass invisible mesh:
//   Pass 1: depth-only occluder
//   Pass 2: ForwardBase  - directional light shadow only (not unlit areas)
//   Pass 3: ForwardAdd   - point/spot light shadow only
//
// Key fix: separates SHADOW_ATTENUATION (real shadow blocking) from
// LIGHT_ATTENUATION (which combines shadow + range falloff). We only
// darken pixels where shadows are actually cast, not just dim areas.
//
// Apply to FBX scene geometry. Mesh Renderer settings:
//   Cast Shadows: Off
//   Receive Shadows: On

Shader "Custom/InvisibleDepthOnly"
{
    Properties
    {
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry-1" }

        // ---- Pass 1: depth-only occluder ----
        Pass
        {
            Tags { "LightMode" = "Always" }
            ColorMask 0
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; };
            struct v2f    { float4 pos : SV_POSITION; };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); return o; }
            fixed4 frag(v2f i) : SV_Target { return 0; }
            ENDCG
        }

        // ---- Pass 2: ForwardBase (directional light shadow) ----
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

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
                SHADOW_COORDS(0)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                TRANSFER_SHADOW(o)
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // SHADOW_ATTENUATION returns 1 in lit areas, < 1 only where
                // shadow casters block the directional light. This does NOT
                // include range falloff, so unlit areas stay unaffected.
                fixed shadowAtten = SHADOW_ATTENUATION(i);
                float shadow = (1.0 - shadowAtten) * _ShadowStrength;
                return fixed4(0, 0, 0, shadow);
            }
            ENDCG
        }

        // ---- Pass 3: ForwardAdd (point/spot light shadows) ----
        Pass
        {
            Tags { "LightMode" = "ForwardAdd" }
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"

            float _ShadowStrength;

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_LIGHTING_COORDS(0, 1)
                float3 worldPos : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_LIGHTING(o, float2(0,0))
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Combined attenuation (shadow * range falloff).
                fixed combinedAtten = UNITY_SHADOW_ATTENUATION(i, i.worldPos);

                // Compute pure range-only attenuation by sampling without shadows.
                // We approximate "is this pixel within useful light range" by
                // checking distance vs light range.
                float3 toLight = _WorldSpaceLightPos0.xyz - i.worldPos;
                float dist2 = dot(toLight, toLight);
                // If completely out of range, attenuation is essentially 0.
                // Use a soft mask: only show shadow when light is actually reaching.
                float rangeMask = 1.0 / (1.0 + 25.0 * dist2 * unity_LightAtten[3].z);
                rangeMask = saturate(rangeMask * 4.0); // soften edge

                // The "shadow part" of attenuation is roughly combinedAtten / rangeAtten,
                // but we approximate by only drawing shadow where rangeMask > threshold
                // AND combinedAtten is meaningfully less than rangeMask.
                float shadowAmount = saturate(rangeMask - combinedAtten);
                float shadow = shadowAmount * _ShadowStrength;
                return fixed4(0, 0, 0, shadow);
            }
            ENDCG
        }
    }

    Fallback "VertexLit"
}
