Shader "Perception/Simple Post"
{
    Properties
    {
        _VignetteStrength("Vignette Strength", Range(0,1)) = 0
        _Contrast("Contrast (-1..1)", Range(-1,1)) = 0
        _Desaturation("Desaturation", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "Perception"
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Source from Full Screen Pass:
            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            float _VignetteStrength;   // 0..1
            float _Contrast;           // -1..+1
            float _Desaturation;       // 0..1

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            // Fullscreen triangle without Fullscreen.hlsl
            Varyings Vert (Attributes a)
            {
                Varyings o;
                float2 uv = float2((a.vertexID << 1) & 2, a.vertexID & 2);   // (0,0), (2,0), (0,2)
                o.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);             // NDC
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y = 1.0 - uv.y;
                #endif
                o.uv = uv;
                return o;
            }

            float3 ApplyContrast(float3 c, float k) { float3 m = float3(0.5,0.5,0.5); return (c - m) * (k + 1.0) + m; }
            float3 LerpDesat    (float3 c, float s) { float g = dot(c, float3(0.2126,0.7152,0.0722)); return lerp(c, g.xxx, saturate(s)); }
            float   VignetteMask(float2 uv)         { float2 d = uv*2.0 - 1.0; float r = dot(d,d); return saturate(1.0 - r); }

            half4 Frag (Varyings i) : SV_Target
            {
                float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, i.uv).rgb;
                col = LerpDesat(col, _Desaturation);
                col = ApplyContrast(col, _Contrast);
                float vig = pow(VignetteMask(i.uv), lerp(1.4, 4.0, _VignetteStrength));
                col *= lerp(1.0, vig, _VignetteStrength);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
