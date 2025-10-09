// MaskWrite.shader
Shader "Hidden/Perception/MaskWrite"{ SubShader{ Tags{"RenderPipeline"="UniversalPipeline"} Pass{ ZWrite Off ZTest LEqual
HLSLPROGRAM #pragma vertex Vert #pragma fragment Frag #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct V{float4 pos:POSITION;}; struct O{float4 pos:SV_POSITION;}; O Vert(V v){O o; o.pos=TransformObjectToHClip(v.pos); return o;}
float4 Frag():SV_Target{ return float4(1,1,1,1);} ENDHLSL }} }