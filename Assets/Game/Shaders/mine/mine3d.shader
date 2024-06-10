Shader "Unlit/mine3d"
{
     Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DepthTex ("Depth Texture", 2D) = "white" {}
        _DepthScale ("Depth Scale", Float) = 0.05
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
            float _DepthScale;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // Get the original color
                float4 color = tex2D(_MainTex, i.texcoord);

                // Get the depth value from the red channel
                float depth = tex2D(_DepthTex, i.texcoord).r;

                // Calculate the horizontal offset based on the depth value
                float offset = depth * _DepthScale;

                // Calculate the left and right UV coordinates
                float2 uvLeft = i.texcoord - float2(offset, 0);
                float2 uvRight = i.texcoord + float2(offset, 0);

                // Combine the left and right images side by side
                float2 uv = i.texcoord;
                if (uv.x < 0.5)
                {
                    uv.x *= 2.0;
                    return tex2D(_MainTex, uvLeft);
                }
                else
                {
                    uv.x = (uv.x - 0.5) * 2.0;
                    return tex2D(_MainTex, uvRight);
                }
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}