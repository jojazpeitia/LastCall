// Renders an unlit texture in the Background queue with no depth test/write.
// Supports a _UVOffset parameter so the image can be "panned" within the quad
// for a subtle Ken-Burns-style illusion without moving the actual 3D camera.
//
// Use on the pre-rendered backdrop quad. A separate script (BackgroundPanner)
// drives _UVOffset based on player screen position.

Shader "Custom/UnlitBackground"
{
    Properties
    {
        _MainTex   ("Texture", 2D) = "white" {}
        _UVOffset  ("UV Offset (x,y)", Vector) = (0, 0, 0, 0)
        _UVZoom    ("UV Zoom (1 = no zoom)", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Background" "Queue" = "Background" }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            Lighting Off
            Fog { Mode Off }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float2    _UVOffset;
            float     _UVZoom;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // Apply standard tiling/offset, then the runtime pan offset
                // and zoom (zoom around the texture center, 0.5,0.5).
                float2 baseUV = TRANSFORM_TEX(v.uv, _MainTex);
                float2 centered = baseUV - 0.5;
                centered /= max(_UVZoom, 0.0001);
                o.uv = centered + 0.5 + _UVOffset;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}