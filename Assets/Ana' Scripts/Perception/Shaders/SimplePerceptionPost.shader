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
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Full Screen Pass source:
            TEXTURE2D_X(_BlitTexture);  SAMPLER(sampler_BlitTexture);
            // (Fallback if ever needed)
            TEXTURE2D_X(_CameraColorTexture); SAMPLER(sampler_CameraColorTexture);

            float _VignetteStrength;
            float _Contrast;
            float _Desaturation;

            struct VIn  { float4 pos: POSITION; float2 uv: TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct VOut { float4 pos: SV_POSITION; float2 uv: TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };

            VOut Vert (VIn i)
            {
                VOut o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = TransformObjectToHClip(i.pos.xyz);
                o.uv  = i.uv;
                return o;
            }

            float3 ApplyContrast(float3 c, float k) { float3 m = float3(0.5,0.5,0.5); return (c - m) * (k + 1.0) + m; }
            float3 LerpDesat    (float3 c, float s) { float g = dot(c, float3(0.2126,0.7152,0.0722)); return lerp(c, g.xxx, saturate(s)); }
            float   VignetteMask(float2 uv)         { float2 d = uv*2.0 - 1.0; float r = dot(d,d); return saturate(1.0 - r); }

            half4 Frag (VOut i) : SV_Target
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
