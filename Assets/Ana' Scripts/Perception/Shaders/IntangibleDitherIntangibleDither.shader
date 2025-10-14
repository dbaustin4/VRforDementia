Shader "Perception/IntangibleDither"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Opacity("Opacity", Range(0,1)) = 1
        _JitterAmp("UV Jitter", Range(0,0.01)) = 0.0015
        _Dither("Dither Strength", Range(0,1)) = 0.5
        _LookFade("Fade When Looked-At", Range(0,1)) = 0.6
        _TimeScale("Noise Speed", Range(0,2)) = 0.75
    }
    SubShader
    {
        Tags{ "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float _Opacity, _JitterAmp, _Dither, _LookFade, _TimeScale;

            struct VIn { float4 pos:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0; };
            struct VOut{ float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 wN:TEXCOORD1; float3 wV:TEXCOORD2; };

            VOut Vert(VIn v){
                VOut o; o.pos = TransformObjectToHClip(v.pos.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.wN = TransformObjectToWorldNormal(v.n);
                o.wV = GetWorldSpaceViewDir(TransformObjectToWorld(v.pos));
                return o;
            }

            float N01(float2 p){ return frac(sin(dot(p,float2(12.9898,78.233))+_Time.y*_TimeScale)*43758.5453); }
            float Bayer2x2(uint2 p){ const float m[4]={0,2,3,1}; return m[(p.x&1)+((p.y&1)<<1)]/4.0; }

            half4 Frag(VOut i):SV_Target
            {
                float2 uv = i.uv + (N01(i.uv*512)*2-1)*_JitterAmp;
                half4 c = SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, uv);
                float look = saturate(dot(normalize(i.wN), normalize(i.wV))); // 1 when facing
                float a = c.a * _Opacity * lerp(1.0, _LookFade, look);
                // ordered dither holes
                float b = Bayer2x2(uint2(floor(i.pos.xy)));
                a = saturate(a - b * _Dither);
                return half4(c.rgb, a);
            }
            ENDHLSL
        }
    }
}
