// MaskedKawaseBlur.shader
Shader "Hidden/Perception/MaskedKawase"{
SubShader{ Tags{"RenderPipeline"="UniversalPipeline"}
Pass{ Name "Down" ZTest Always ZWrite Off Cull Off
HLSLPROGRAM #pragma vertex Vert #pragma fragment Frag
#pragma multi_compile _ UNITY_SINGLE_PASS_STEREO
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
TEXTURE2D_X(_CameraColorTexture); SAMPLER(sampler_CameraColorTexture);
struct V{float4 pos:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID};
struct O{float4 pos:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
O Vert(V i){O o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.pos=TransformVertex(i.pos); o.uv=i.uv; return o;}
float4 Frag(O i):SV_Target{ return SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, i.uv);} ENDHLSL }
Pass{ Name "KawaseA" ZTest Always ZWrite Off Cull Off
HLSLPROGRAM #pragma vertex Vert #pragma fragment Frag #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
TEXTURE2D_X(_A); SAMPLER(sampler_A);
float2 _TexelSize; struct V{float4 pos:POSITION; float2 uv:TEXCOORD0;}; struct O{float4 pos:SV_POSITION; float2 uv:TEXCOORD0;};
O Vert(V v){O o; o.pos=TransformVertex(v.pos); o.uv=v.uv; return o;}
float4 Frag(O i):SV_Target{
float2 o = _TexelSize * 1.5;
float4 c = SAMPLE_TEXTURE2D_X(_A,sampler_A,i.uv);
c += SAMPLE_TEXTURE2D_X(_A,sampler_A,i.uv+float2( o.x, 0));
c += SAMPLE_TEXTURE2D_X(_A,sampler_A,i.uv+float2(-o.x, 0));
c += SAMPLE_TEXTURE2D_X(_A,sampler_A,i.uv+float2(0, o.y));
c += SAMPLE_TEXTURE2D_X(_A,sampler_A,i.uv+float2(0, -o.y));
return c/5.0; }
ENDHLSL }
Pass{ Name "KawaseB" ZTest Always ZWrite Off Cull Off
HLSLPROGRAM #pragma vertex Vert #pragma fragment Frag #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
TEXTURE2D_X(_B); SAMPLER(sampler_B);
float2 _TexelSize; struct V{float4 pos:POSITION; float2 uv:TEXCOORD0;}; struct O{float4 pos:SV_POSITION; float2 uv:TEXCOORD0;};
O Vert(V v){O o; o.pos=TransformVertex(v.pos); o.uv=v.uv; return o;}
float4 Frag(O i):SV_Target{
float2 o = _TexelSize * 2.5;
float4 c = SAMPLE_TEXTURE2D_X(_B,sampler_B,i.uv);
c += SAMPLE_TEXTURE2D_X(_B,sampler_B,i.uv+float2( o.x, o.y));
c += SAMPLE_TEXTURE2D_X(_B,sampler_B,i.uv+float2(-o.x, o.y));
c += SAMPLE_TEXTURE2D_X(_B,sampler_B,i.uv+float2( o.x, -o.y));
c += SAMPLE_TEXTURE2D_X(_B,sampler_B,i.uv+float2(-o.x, -o.y));
return c/5.0; }
ENDHLSL }
Pass{ Name "Composite" ZTest Always ZWrite Off Cull Off
HLSLPROGRAM #pragma vertex Vert #pragma fragment Frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
TEXTURE2D_X(_CameraColorTexture); SAMPLER(sampler_CameraColorTexture);
TEXTURE2D_X(_ImportantMask); SAMPLER(sampler_ImportantMask);
float _MaskedBlurStrength; struct V{float4 pos:POSITION; float2 uv:TEXCOORD0;}; struct O{float4 pos:SV_POSITION; float2 uv:TEXCOORD0;};
O Vert(V v){O o; o.pos=TransformVertex(v.pos); o.uv=v.uv; return o;}
float4 Frag(O i):SV_Target{
float3 sharp = SAMPLE_TEXTURE2D_X(_CameraColorTexture,sampler_CameraColorTexture,i.uv).rgb;
float m = SAMPLE_TEXTURE2D_X(_ImportantMask,sampler_ImportantMask,i.uv).r;
// Invert logic: blur where mask is 1 (important things become unreadable)
float3 blur = sharp; // this is the blurred RT bound as source for this pass
// NOTE: In the feature, we blit from the blurred RT here
float3 outc = lerp(sharp, blur, saturate(m * _MaskedBlurStrength));
return float4(outc,1);
} ENDHLSL }
}}