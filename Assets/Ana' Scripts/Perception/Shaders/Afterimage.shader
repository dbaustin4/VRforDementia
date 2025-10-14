Shader "Perception/Afterimage"
{
    Properties
    {
        _Echo     ("Echo Strength", Range(0,1)) = 0.35
        _GhostOff ("Ghost Offset (px)", Range(0,5)) = 1.2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_BlitTexture);  SAMPLER(sampler_BlitTexture);
            float _Echo;
            float _GhostOff;

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

            half4 Frag (Varyings i) : SV_Target
            {
                float2 texel = 1.0 / _ScreenParams.xy;

                // current frame
                float3 cur = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, i.uv).rgb;

                // small offset sample -> "ghost"
                float2 off = float2(_GhostOff, 0) * texel;
                float3 ghost = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, i.uv + off).rgb;

                float3 outc = lerp(cur, ghost, saturate(_Echo));
                return half4(outc, 1.0);
            }
            ENDHLSL
        }
    }
}
