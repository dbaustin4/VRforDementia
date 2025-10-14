Shader "Perception/HallucinationFog"
{
    Properties
    {
        _Tint("Tint (RGBA)", Color) = (0.9,0.9,1.0,0.25)
        _Scroll1("Scroll 1 (XY)", Vector) = (0.02, 0.01, 0, 0)
        _Scroll2("Scroll 2 (XY)", Vector) = (-0.015, 0.017, 0, 0)
        _NoiseScale("Noise Tiling", Float) = 1.2
        _Softness("Softness", Range(0,1)) = 0.6
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        ZWrite Off Blend SrcAlpha OneMinusSrcAlpha Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _Tint;
            float2 _Scroll1, _Scroll2;
            float  _NoiseScale, _Softness;

            struct VIn { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct VOut{ float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            VOut Vert(VIn v){ VOut o; o.pos = TransformObjectToHClip(v.pos.xyz); o.uv = v.uv * _NoiseScale; return o; }

            // lightweight procedural value-noise
            float2 hash(float2 p){ p = float2(dot(p,float2(127.1,311.7)), dot(p,float2(269.5,183.3))); return frac(sin(p)*43758.5453); }
            float noise2d(float2 p){
                float2 i = floor(p), f = frac(p);
                float2 u = f*f*(3.0-2.0*f);
                float a = dot(hash(i+float2(0,0)), f-float2(0,0));
                float b = dot(hash(i+float2(1,0)), f-float2(1,0));
                float c = dot(hash(i+float2(0,1)), f-float2(0,1));
                float d = dot(hash(i+float2(1,1)), f-float2(1,1));
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y)*0.5+0.5;
            }

            half4 Frag(VOut i):SV_Target
            {
                float t = _Time.y;
                float n1 = noise2d(i.uv + _Scroll1 * t * 2.0);
                float n2 = noise2d(i.uv + _Scroll2 * t * 2.0 + 10.0);
                float n  = (n1 + n2) * 0.5;
                float fog = smoothstep(1.0 - _Softness, 1.0, n);
                return half4(_Tint.rgb, _Tint.a * fog);
            }
            ENDHLSL
        }
    }
}
