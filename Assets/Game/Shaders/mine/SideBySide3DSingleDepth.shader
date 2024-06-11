Shader "Custom/SideBySide3DSingleDepth"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _DepthTex ("Depth Texture", 2D) = "white" {}
        _LeftShift ("Left Eye Shift", Range(-0.1, 0.1)) = 0.02
        _RightShift ("Right Eye Shift", Range(-0.1, 0.1)) = -0.02
        _DepthFactor ("Depth Factor", Range(0, 1)) = 0.02
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
            sampler2D _DepthTex;
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
                // Sample normalized depth from red channel
                float depth = tex2D(_DepthTex, uv).r;

                // Adjust UVs for left and right eye views
                if (uv.x < 0.5)
                {
                    uv.x = clamp(uv.x * 2.0 + (_LeftShift * depth * _DepthFactor), 0.0, 1.0);
                }
                else
                {
                    uv.x = clamp((uv.x - 0.5) * 2.0 + (_RightShift * depth * _DepthFactor), 0.0, 1.0);
                }

                // Clamp UV coordinates to avoid sampling out of bounds
                uv = clamp(uv, 0.0, 1.0);

                return tex2D(_MainTex, uv);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}