Shader "Custom/Painting"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _InactiveColour ("Inactive Colour", Color) = (1, 1, 1, 1)
        [HideInInspector] _displayMask ("Display Mask", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _InactiveColour;
            float _displayMask;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // No flipping; pass UVs directly
                o.uv = v.uv;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texCol = tex2D(_MainTex, i.uv);
                return texCol * _displayMask + _InactiveColour * (1 - _displayMask);
            }
            ENDCG
        }
    }
    Fallback "Standard"
}