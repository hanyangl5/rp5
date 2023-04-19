Shader "Custuom/Opaque"
{
    Properties
    {
        // [optional: attribute] name("display text in Inspector", type name) = default value
        [MainColor, Gamma]_albedo_tex("albedo tex", 2D) = "dark" {}
        [Emissive, Gamma]_emissive_tex("emissive tex", 2D) = "dark" {}
        _metallic_tex("metallic tex", 2D) = "dark" {}
        _roughness_tex("roughness tex", 2D) = "dark" {}
        [Normal]_normal_map("normal map", 2D) = "dark" {}
        _ao_map_tex("ao map", 2D) = "dark" {}
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
            
            #include "UnityCG.cginc"

            struct VsInput {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            struct PsInput {
                float2 uv : TEXCOORD0;
                float4 position_clip : SV_POSITION;
                float4 position_clip_prev : TEXCOORD01;
                float3 normal : NORMAL;
                float4 position_ws : TEXCOORD02;
            };
            typedef PsInput VsOutput; 

            sampler2D _albedo_tex;
            float4 _albedo_tex_ST;
            sampler2D _metallic_tex;
            sampler2D _roughness_tex;
            sampler2D _normal_map;
            sampler2D _emissive_tex;
            float4x4 view_projection_prev;
            float4x4 view_projection;
            float2 jitter_offset_prev;
            float2 jitter_offset;
            float _emissive_intensity;

            VsOutput VSMain(VsInput v) {
                VsOutput o;
                float4 postion_ws = mul(unity_ObjectToWorld, v.pos);
                o.position_clip = mul(view_projection, postion_ws);
                o.position_clip_prev = mul(view_projection_prev, postion_ws);
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
                float4 albedo = tex2D(_albedo_tex, i.uv);
                float m = tex2D(_metallic_tex, i.uv).r;
                float r = tex2D(_roughness_tex, i.uv).r;
                float3 emissive = tex2D(_emissive_tex, i.uv).rgb;
                //float4 normal = tex2D(_normal_map, i.uv);
                float3 normal = normalize(i.normal);
                albedo.rgb = pow(albedo.rgb,float3(2.2, 2.2, 2.2));
                gbuffer0 = float4(albedo.rgb, 0.0);
                gbuffer1 = float4(normal, 0.0);
                // mv should be in [-1, 1]
                gbuffer2 = ((i.position_clip / i.position_clip.w - jitter_offset)  - (i.position_clip_prev / i.position_clip_prev.w - jitter_offset_prev)) * 0.5;

                gbuffer3 = float4(emissive * _emissive_intensity, 1.0);
                gbuffer4 = float2(m, r); // metalic roughness
            }
            ENDCG
        }
    }
}