Shader "Project2048/Effects/ClawSlash2D"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.78, 0.98, 1, 1)
        _Intensity ("Intensity", Float) = 1
        _EdgeSoftness ("Edge Softness", Range(0.02, 0.6)) = 0.42
        _TipSoftness ("Tip Softness", Range(0.02, 0.35)) = 0.16
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _BaseColor;
            float _Intensity;
            float _EdgeSoftness;
            float _TipSoftness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float across = abs(input.uv.y * 2.0 - 1.0);
                float edge = 1.0 - smoothstep(1.0 - _EdgeSoftness, 1.0, across);
                float head = smoothstep(0.0, _TipSoftness, input.uv.x);
                float tail = 1.0 - smoothstep(1.0 - _TipSoftness, 1.0, input.uv.x);
                float alpha = _BaseColor.a * input.color.a * edge * head * tail;
                fixed3 color = _BaseColor.rgb * _Intensity;
                return fixed4(color, alpha);
            }
            ENDCG
        }
    }
}
