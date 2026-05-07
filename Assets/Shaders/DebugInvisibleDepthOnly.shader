// Editor-only visualization shader for the invisible depth-occluder mesh.
// Use this in a separate "DebugInvisibleMat" material so you can swap to it
// when positioning objects, then swap back to "InvisibleMat" (which uses
// InvisibleDepthOnly.shader) when you want the mesh invisible again.
//
// Renders the geometry as a flat shaded color so you can see its shape.
// Same render queue and depth behavior as InvisibleDepthOnly so swapping
// preserves identical 3D behavior — only the visibility differs.

Shader "Custom/DebugInvisibleDepthOnly"
{
    Properties
    {
        _DebugColor ("Debug Color", Color) = (0.4, 0.6, 0.9, 1.0)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry-1" }

        Pass
        {
            ZWrite On
            ZTest LEqual
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _DebugColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Cheap fake lighting so geometry shape is readable.
                float3 lightDir = normalize(float3(0.5, 1.0, 0.3));
                float ndl = saturate(dot(normalize(i.worldNormal), lightDir));
                float shaded = 0.35 + 0.65 * ndl;
                return fixed4(_DebugColor.rgb * shaded, 1.0);
            }
            ENDCG
        }
    }
}
