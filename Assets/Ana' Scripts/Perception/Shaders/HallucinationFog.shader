// HallucinationFog.shader (screen‑door translucent, scrolling noise)
Shader "Perception/HallucinationFog"{
SubShader{ Tags{"Queue"="Transparent" "RenderPipeline"="UniversalPipeline"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
Pass{ HLSLPROGRAM #pragma vertex Vert #pragma fragment Frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
float4 _Tint = float4(0.9,0.9,1.0,0.25); float2 _Scroll1=float2(0.02,0.01), _Scroll2=float2(-0.015,0.017);
TEXTURE2D_X(_NoiseA); SAMPLER(sampler_NoiseA); TEXTURE2D_X(_NoiseB); SAMPLER(sampler_NoiseB);
struct V{float4 pos:POSITION; float2 uv:TEXCOORD0;}; struct O{float4 pos:SV_POSITION; float2 uv:TEXCOORD0;};
O Vert(V v){O o; o.pos=TransformObjectToHClip(v.pos); o.uv=v.uv; return o;}
float4 Frag(O i):SV_Target{
float a = SAMPLE_TEXTURE2D_X(_NoiseA,sampler_NoiseA,i.uv + _Scroll1*_Time.y).r;
float b = SAMPLE_TEXTURE2D_X(_NoiseB,sampler_NoiseB,i.uv + _Scroll2*_Time.y).r;
float fog = smoothstep(0.4, 0.7, (a+b)*0.5);
return float4(_Tint.rgb, _Tint.a*fog);
} ENDHLSL }
}}