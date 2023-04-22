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
    }
    SubShader
    {
        Tags { "LightMode"="geometry" }

        Pass
        {
            CGPROGRAM
            #pragma vertex VSMain
            #pragma fragment PSMain
            #pragma require WaveBasic
            #include "UnityCG.cginc"
            #include "../Shaders/include/vertex_layouts.hlsl"
            
            float4 _albedo;
            float4 _emissive;
            float _emissive_intensity;
            float _metallic;
            float _roughness;
            float4x4 view_projection; //current jittered view projection matrix
            
            VsOutput VSMain(VsInput v) {
                VsOutput o;
                float4 postion_ws = mul(unity_ObjectToWorld, v.pos);
                o.position = mul(view_projection, postion_ws);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.position_ws = postion_ws;
                return o;
            }

            void PSMain(
                PsInput i,
                out float4 gbuffer0 : SV_Target0,
                out float4 gbuffer1 : SV_Target1,
                out float2 gbuffer2 : SV_Target2,
                out float4 gbuffer3 : SV_Target3,
                out float2 gbuffer4 : SV_Target4) {
                //float4 normal = tex2D(_normal_map, i.uv);
                float3 normal = normalize(i.normal); // to [0, 1]

                gbuffer0 = float4(_albedo.rgb, 0);
                gbuffer1 = float4(normal, 0);
                // mv should be in [-1, 1]
                gbuffer3 = float4(_emissive * _emissive_intensity);
                gbuffer4 = float2(_metallic, _roughness); // metalic roughness
            }
            ENDCG
        }
    }
}