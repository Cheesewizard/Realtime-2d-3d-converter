Shader "Unlit/SideBySide3D"
{
Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _LeftDepthTex ("Left Depth Texture", 2D) = "white" {}
        _RightDepthTex ("Right Depth Texture", 2D) = "white" {}
        _LeftShift ("Left Eye Shift", Float) = 0.02
        _RightShift ("Right Eye Shift", Float) = -0.02
        _DepthFactor ("Depth Factor", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 texcoord : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _LeftDepthTex;
            sampler2D _RightDepthTex;
            float _LeftShift;
            float _RightShift;
            float _DepthFactor;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                float4 color;
                float depth;

                if (uv.x < 0.5)
                {
                    // Left eye
                    uv.x *= 2.0;
                    depth = tex2D(_LeftDepthTex, uv).r;
                    uv.x += _LeftShift + depth * _DepthFactor;
                    color = tex2D(_MainTex, uv * 0.5);
                }
                else
                {
                    // Right eye
                    uv.x = (uv.x - 0.5) * 2.0;
                    depth = tex2D(_RightDepthTex, uv).r;
                    uv.x += _RightShift - depth * _DepthFactor;
                    color = tex2D(_MainTex, uv * 0.5 + 0.5);
                }

                return color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}