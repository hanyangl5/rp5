Shader "Custuom/OpaqueFromVector"
{
    Properties
    {
        // [optional: attribute] name("display text in Inspector", type name) = default value
        _metallic ("metallic", Range (0.0, 1.0)) = 0.5
        _roughness ("rougness", Range (0.0, 1.0)) = 0.5
        _albedo ("albedo", Color) = (1.0, 1.0, 1.0)
        _emissive ("emissive", Color) = (0.0, 0.0, 0.0)
        _emissive_intensity ("emissive_intensity", float) = 1.0
        _anisotropy ("anisotropy", Range (-1.0, 1.0)) = 0.5
    }
    SubShader
    {
        Tags { "LightMode"="OpaqueGeometry" }

        Pass
        {
            CGPROGRAM
            #pragma vertex VSMain
            #pragma fragment PSMain
            #pragma require WaveBasic
            #pragma enable_d3d11_debug_symbols
            #include "../Shaders/include/rp5.h"

            DECLARE_SCENE_CONSTANTS
            float4 _albedo;
            float4 _emissive;
            float _emissive_intensity;
            float _metallic;
            float _roughness;
            float _anisotropy;
            
            VsOutput VSMain(VsInput v) {
                VsOutput o;
                INIT_VS_OUT
                return o;
            }

            void PSMain(
                PsInput i,
                out float4 gbuffer0 : SV_Target0,
                out float4 gbuffer1 : SV_Target1,
                out float2 gbuffer2 : SV_Target2,
                out float4 gbuffer3 : SV_Target3,
                out float2 gbuffer4 : SV_Target4,
                out float4 gbuffer5 : SV_Target5) {
                float3 normal_ws = normalize(float3(i.t2w0.z, i.t2w1.z, i.t2w2.z));
                gbuffer0 = float4(_albedo.rgb, 0);
                gbuffer1 = float4(normal_ws, 0);
                gbuffer2 = ComputeMotionVector(float4(i.t2w0.w, i.t2w1.w, i.t2w2.w, 1.0), view_projection_non_jittered, view_projection_prev_non_jittered);
                gbuffer3 = float4(_emissive * _emissive_intensity);
                gbuffer4 = float2(_metallic, _roughness); // metalic roughness
                gbuffer5 = float4(i.t2w0.x, i.t2w1.x, i.t2w2.x, _anisotropy);
            }
            ENDCG
        }
    }
}