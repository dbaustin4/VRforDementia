Shader "Perception/ScreenTint"
{
    Properties
    {
        _Tint    ("Tint (RGBA)", Color) = (1,0.95,0.9, 0.15)
        _PulseAmp("Pulse Amount", Range(0,1)) = 0.0
        _PulseHz ("Pulse Hz", Range(0,3)) = 0.2
        _Vig     ("Vignette", Range(0,1)) = 0.0
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

            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            float4 _Tint;
            float  _PulseAmp;
            float  _PulseHz;
            float  _Vig;

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

            float VMask(float2 uv)
            {
                float2 d = uv * 2.0 - 1.0;
                float r2 = dot(d, d);
                return saturate(1.0 - r2);
            }

            half4 Frag (Varyings i) : SV_Target
            {
                float3 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, i.uv).rgb;

                // gentle breathing pulse
                float pulse = 1.0 + sin(_Time.y * 6.2831853 * _PulseHz) * _PulseAmp;

                // vignette mask (raised to sharpen)
                float vig = lerp(1.0, pow(VMask(i.uv), 2.5), _Vig);

                // tint blend
                float3 tinted = lerp(src, src * _Tint.rgb * pulse, _Tint.a);
                tinted *= vig;

                return half4(tinted, 1.0);
            }
            ENDHLSL
        }
    }
}
