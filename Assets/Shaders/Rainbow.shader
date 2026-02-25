Shader "Custom/Rainbow"
{
    Properties
    {
        _Hue ("Hue", Range(0, 1)) = 0
        _Saturation ("Saturation", Range(0, 1)) = 1
        _Value ("Value", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf UnlitShadows fullforwardshadows addshadow

        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
        };

        float _Hue;
        float _Saturation;
        float _Value;

        inline fixed4 LightingUnlitShadows(SurfaceOutput s, fixed3 lightDir, fixed atten)
        {
            fixed4 c;
            c.rgb = s.Albedo * atten;
            c.a = s.Alpha;
            return c;
        }

        // HSV to RGB conversion helper
        fixed3 hsv2rgb(float3 c)
        {
            fixed4 K = fixed4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            fixed3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
            return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed3 rgb = hsv2rgb(float3(_Hue, _Saturation, _Value));
            o.Albedo = rgb;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
