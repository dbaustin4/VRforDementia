Shader "Perception/MoodOverlay_Combined"
{
    Properties
    {
        // Tint (new naming)
        _TintColor      ("Tint Color", Color) = (1,0.95,0.9,1)
        _TintStrength   ("Tint Strength (0..1)", Range(0,1)) = 0.15

        // Legacy tint support (optional). If you set only _Tint.a, it still works.
        _Tint           ("[Legacy] Tint (RGBA, A=strength)", Color) = (1,0.95,0.9,0)

        // Breathing pulse on tint
        _PulseAmp       ("Pulse Amount (0..1)", Range(0,1)) = 0.0
        _PulseHz        ("Pulse Hz (0..3)", Range(0,3)) = 0.2

        // Perception/Simple Post bits
        _Desaturation   ("Desaturation (0..1)", Range(0,1)) = 0
        _Contrast       ("Contrast (-1..+1)", Range(-1,1)) = 0

        // Vignette (use either; combined internally)
        _VignetteStrength("Vignette Strength (0..1)", Range(0,1)) = 0
        _Vig            ("[Legacy] Vignette (0..1)", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "MoodOverlayCombined"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Blit source from URP Fullscreen Pass
            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            float4 _TintColor;
            float4 _Tint;          // legacy
            float  _TintStrength;  // 0..1
            float  _PulseAmp;      // 0..1
            float  _PulseHz;       // 0..3

            float  _Desaturation;  // 0..1
            float  _Contrast;      // -1..+1

            float  _VignetteStrength; // 0..1
            float  _Vig;              // legacy

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings   { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert (Attributes a)
            {
                Varyings o;
                float2 uv = float2((a.vertexID << 1) & 2, a.vertexID & 2);
                o.pos = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y = 1.0 - uv.y;
                #endif
                o.uv = uv;
                return o;
            }

            // Helpers
            float3 ApplyContrast(float3 c, float k)
            {
                float3 m = float3(0.5,0.5,0.5);
                return (c - m) * (k + 1.0) + m;
            }

            float3 LerpDesat(float3 c, float s)
            {
                float g = dot(c, float3(0.2126,0.7152,0.0722));
                return lerp(c, g.xxx, saturate(s));
            }

            float VignetteMask(float2 uv)
            {
                float2 d = uv*2.0 - 1.0;
                float r2 = dot(d,d);
                return saturate(1.0 - r2);
            }

            half4 Frag (Varyings i) : SV_Target
            {
                float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, i.uv).rgb;

                // Order: desaturate → contrast → tint → vignette (cheap → expensive)
                col = LerpDesat(col, _Desaturation);
                col = ApplyContrast(col, _Contrast);

                // Effective tint (supports new + legacy)
                float  tintK = max(_TintStrength, _Tint.a);
                float3 tintC = (_TintStrength > 0.0) ? _TintColor.rgb : _Tint.rgb;

                // Gentle breathing pulse on tint contribution
                float pulse = 1.0 + sin(_Time.y * 6.2831853 * _PulseHz) * _PulseAmp;

                float3 tinted = lerp(col, col * tintC * pulse, saturate(tintK));

                // Combined vignette
                float vigK = max(_VignetteStrength, _Vig);
                float vig  = pow(VignetteMask(i.uv), lerp(1.4, 4.0, saturate(vigK)));
                tinted *= lerp(1.0, vig, saturate(vigK));

                return half4(tinted, 1);
            }
            ENDHLSL
        }
    }
}
