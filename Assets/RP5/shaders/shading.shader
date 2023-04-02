Shader "Custuom/shading"
{

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM

            #include "UnityCG.cginc"
            #include "HLSLSupport.cginc"
            #include "brdf.hlsl"
            #include "light.hlsl"
            #include "UnityLightingCommon.cginc"

            #pragma vertex VS_MAIN
            #pragma fragment PS_MAIN

            StructuredBuffer<DirectionalLight> directional_lights;
            StructuredBuffer<PointLight> point_lights;
            StructuredBuffer<SpotLight> spot_lights;


            sampler2D gdepth;
            sampler2D gbuffer_0;
            sampler2D gbuffer_1;
            sampler2D gbuffer_2;
            sampler2D gbuffer_3;
            sampler2D gbuffer_4;
            SamplerState sampler1; // a bilinear sampler to fetch gbuffer

            struct VsInput {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VsOutput {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            typedef VsOutput PsInput;

            VsOutput VS_MAIN(VsInput vs_in) {
                VsOutput o;
                o.pos = UnityObjectToClipPos(vs_in.pos);
                o.uv = vs_in.uv;
                return o;
            }


            float4 PS_MAIN(PsInput i) : SV_Target
            {
                //float3 albedo = gbuffer_0.Sample(sampler1, i.uv).rgb;
                //float3 normal = gbuffer_1.Sample(sampler1, i.uv).rgb;
                //float2 mr = gbuffer_4.Sample(sampler1, i.uv);

/*

                float2 uv = i.uv;
                float4 GT2 = tex2D(_GT2, uv);
                float4 GT3 = tex2D(_GT3, uv);

                // 从 Gbuffer 解码数据
                float3 albedo = tex2D(_GT0, uv).rgb;
                float3 normal = tex2D(_GT1, uv).rgb * 2 - 1;
                float2 motionVec = GT2.rg;
                float roughness = GT2.b;
                float metallic = GT2.a;
                float3 emission = GT3.rgb;
                float occlusion = GT3.a;

                float d = UNITY_SAMPLE_DEPTH(tex2D(_gdepth, uv));
                float d_lin = Linear01Depth(d);
                depthOut = d;

                // 反投影重建世界坐标
                float4 ndcPos = float4(uv*2-1, d, 1);
                float4 worldPos = mul(_vpMatrixInv, ndcPos);
                worldPos /= worldPos.w;

*/
                float3 light_color = float3(1.0, 1.0, 1.0);
                float light_intensity = 10.0f;
                float3 light_dir = float3(1.0, 0.0, 0.0);
                float4 world_pos = tex2D(gbuffer_3, i.uv);
                float3 albedo = tex2D(gbuffer_0, i.uv).rgb;
                float3 normal = normalize(tex2D(gbuffer_1, i.uv).rgb); // SIGNED OR UNSIGNED?
                float2 mr = tex2D(gbuffer_4, i.uv);

                float roughness = mr.y;
                float roughness2 = roughness * roughness;
                float metallic = mr.x;

                float3 f0 = lerp(float3(0.04, 0.04, 0.04), albedo, metallic);
                //float3 light_dir = normalize(_WorldSpaceLightPos0.xyz - world_pos);
                //light_dir = light_direction;
                float3 v = normalize(_WorldSpaceCameraPos.xyz - world_pos);
                BXDF bxdf = InitBXDF(normal, v, light_dir);
                float D = NDF_GGX(roughness2, bxdf.NoM);
                float G = Vis_SmithGGXCombined(roughness2, bxdf.NoV, bxdf.NoL);
                float3 F = Fresnel_Schlick(f0, bxdf.NoM);

                float3 diffuse = Diffuse_Lambert(albedo);

                float3 specular = D * G * F;

                float3 radiance = (diffuse + specular) * bxdf.NoL * light_color * light_intensity; // attenuation;
                return float4(radiance, 1.0);
            }
            ENDHLSL
        }
    }

    // FallBack
}