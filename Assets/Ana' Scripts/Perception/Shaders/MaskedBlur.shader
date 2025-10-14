Shader "Perception/MaskedBlur"
{
    Properties
    {
        _BlurRadius ("Blur Radius (px)", Range(0,8)) = 3
        _MaskTex    ("Mask (R=blur)", 2D) = "black" {}
        _Strength   ("Strength", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "MaskedBlur"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Fullscreen source from URP Full Screen Pass:
            TEXTURE2D_X(_BlitTexture);  SAMPLER(sampler_BlitTexture);
            TEXTURE2D_X(_MaskTex);      SAMPLER(sampler_MaskTex);

            float _BlurRadius;
            float _Strength;

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings   { float4 pos: SV_POSITION; float2 uv: TEXCOORD0; };

            Varyings Vert (Attributes a){
                Varyings o;
                float2 uv = float2((a.vertexID<<1)&2, a.vertexID&2);
                o.pos = float4(uv*2-1, 0, 1);
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y = 1-uv.y;
                #endif
                o.uv = uv;
                return o;
            }

            float3 Blur9(TEXTURE2D_X_PARAM(src, samp), float2 uv, float2 texel, float r)
            {
                float2 o = texel * r;
                float3 c = 0;
                c += SAMPLE_TEXTURE2D_X(src, samp, uv) * 0.2;
                c += SAMPLE_TEXTURE2D_X(src, samp, uv + float2( o.x, 0)) * 0.1;
                c += SAMPLE_TEXTURE2D_X(src, samp, uv + float2(-o.x, 0)) * 0.1;
                c += SAMPLE_TEXTURE2D_X(src, samp, uv + float2( 0,  o.y)) * 0.1;
                c += SAMPLE_TEXTURE2D_X(src, samp, uv + float2( 0, -o.y)) * 0.1;
                c += SAMPLE_TEXTURE2D_X(src, samp, uv + float2( o.x,  o.y)) * 0.1;
                c += SAMPLE_TEXTURE2D_X(src, samp, uv + float2(-o.x,  o.y)) * 0.1;
                c += SAMPLE_TEXTURE2D_X(src, samp, uv + float2( o.x, -o.y)) * 0.1;
                c += SAMPLE_TEXTURE2D_X(src, samp, uv + float2(-o.x, -o.y)) * 0.1;
                return c;
            }

            half4 Frag (Varyings i) : SV_Target
            {
                float2 texel = 1.0 / _ScreenParams.xy;
                float3 sharp = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, i.uv).rgb;
                float3 blur  = Blur9(_BlitTexture, sampler_BlitTexture, i.uv, texel, _BlurRadius);
                float  m     = SAMPLE_TEXTURE2D_X(_MaskTex, sampler_MaskTex, i.uv).r;
                float  k     = saturate(m * _Strength);
                return half4(lerp(sharp, blur, k), 1);
            }
            ENDHLSL
        }
    }
}
