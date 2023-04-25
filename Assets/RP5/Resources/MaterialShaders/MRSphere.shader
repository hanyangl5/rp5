Shader "Custuom/Sphere"
{
    Properties
    {
        // [optional: attribute] name("display text in Inspector", type name) = default value
        [MainColor, Gamma]_albedo_tex("albedo tex", 2D) = "black" {}
        [Emissive, Gamma]_emissive_tex("emissive tex", 2D) = "black" {}
        _metallic_roughness_tex("metallic roughness tex", 2D) = "black" {}
        [Normal]_normal_map("normal map", 2D) = "black" {}
        
        _emissive_intensity ("emissive_intensity", float) = 1.0
    
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
            #include "UnityCG.cginc"
            #include "../Shaders/include/vertex_layouts.hlsl"
            #include "../Shaders/include/material.hlsl"

            sampler2D _albedo_tex;
            float4 _albedo_tex_ST;
            sampler2D _metallic_roughness_tex;
            sampler2D _normal_map;
            sampler2D _emissive_tex;
            float _emissive_intensity;
            float4x4 view_projection; //current jittered view projection matrix
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
                float4 albedo = tex2D(_albedo_tex, i.uv);
                float2 mr = tex2D(_metallic_roughness_tex, i.uv).bg;
                float3 emissive = tex2D(_emissive_tex, i.uv).rgb;
                float3 normal = normalize(float3(i.t2w0.z, i.t2w1.z, i.t2w2.z));
 
                gbuffer0 = float4(albedo.rgb, 0.0);
                gbuffer1 = float4(normal, asfloat(MATERIAL_ID_OPAQUE));
                gbuffer3 = float4(emissive * _emissive_intensity, 1.0);
                gbuffer4 = mr; // metalic roughness
                gbuffer5 = float4(0.0, 0.0, 0.0, 0.0);
            }
            ENDCG
        }
    }
}