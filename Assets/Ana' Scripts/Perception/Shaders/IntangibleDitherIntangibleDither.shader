// IntangibleDither.shader
Shader "Perception/IntangibleDither"{
SubShader{ Tags{"Queue"="Transparent" "RenderPipeline"="UniversalPipeline"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
Pass{
HLSLPROGRAM #pragma vertex Vert #pragma fragment Frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
TEXTURE2D_X(_MainTex); SAMPLER(sampler_MainTex); float4 _MainTex_ST;
float _Opacity = 1; float _JitterAmp = 0.0015; float _DitherThresh = 0.5; float _LookFade = 0.6;
float _TimeScale = 0.75;
struct V{float4 pos:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0;};
struct O{float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 wN:TEXCOORD1; float3 wV:TEXCOORD2;};
O Vert(V v){O o; o.pos=TransformObjectToHClip(v.pos); o.uv=TRANSFORM_TEX(v.uv,_MainTex);
o.wN = TransformObjectToWorldNormal(v.n); o.wV = GetWorldSpaceViewDir(TransformObjectToWorld(v.pos)); return o;}
float Bayer2x2(uint2 p){ const float m[4]={0,2,3,1}; return m[(p.x&1)+((p.y&1)<<1)]/4.0; }
float N01(float2 p){ return frac(sin(dot(p,float2(12.9898,78.233)))*43758.5453); }
float4 Frag(O i):SV_Target{
float2 uv = i.uv + (N01(i.uv*512 + _Time.y*_TimeScale).xx-0.5)*_JitterAmp;
float4 c = SAMPLE_TEXTURE2D_X(_MainTex,sampler_MainTex,uv);
float look = saturate(dot(normalize(i.wN), normalize(i.wV))); // 1 when facing
float a = c.a * _Opacity * lerp(1.0, _LookFade, look);
// ordered dither to create holes
float b = Bayer2x2(uint2(floor(i.pos.xy) ));
a = a - (b*(1-_DitherThresh)); a = saturate(a);
return float4(c.rgb, a);
}
ENDHLSL }
}}