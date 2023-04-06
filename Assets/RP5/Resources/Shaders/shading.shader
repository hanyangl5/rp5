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
            #include "common.hlsl"
            #include "debug.hlsl"
            #include "common_math.hlsl"
            #pragma vertex VS_MAIN
            #pragma fragment PS_MAIN

CBUFFER_START(SceneConstant)
	uint directional_light_count;
	uint point_light_count;
	uint spot_light_count;
    uint pad0;
CBUFFER_END

            StructuredBuffer<DirectionalLight> directional_lights;
            StructuredBuffer<PointLight> point_lights;
            StructuredBuffer<SpotLight> spot_lights;

            sampler2D gdepth;
            sampler2D gbuffer_0;
            sampler2D gbuffer_1;
            sampler2D gbuffer_2;
            sampler2D gbuffer_3;
            sampler2D gbuffer_4;
            //Texture2D<float4> gbuffera;
            //SamplerState sampler1; // a bilinear sampler to fetch gbuffer

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


            float4 PS_MAIN(PsInput ps_in) : SV_Target
            {

/*



                float d = UNITY_SAMPLE_DEPTH(tex2D(_gdepth, uv));
                float d_lin = Linear01Depth(d);
                depthOut = d;

                // 反投影重建世界坐标
                float4 ndcPos = float4(uv*2-1, d, 1);
                float4 worldPos = mul(_vpMatrixInv, ndcPos);
                worldPos /= worldPos.w;

*/

                float4 world_pos = tex2D(gbuffer_3, ps_in.uv);

                // invalid gbuffer pixel
                if(Equal(world_pos, float3(0.0, 0.0, 0.0)) == 0.0) {
                    return float4(0.0,0.0,0.0,1.0);
                }
                float3 albedo = tex2D(gbuffer_0, ps_in.uv).rgb;
                float3 normal = tex2D(gbuffer_1, ps_in.uv).rgb; // to [-1, 1]
                float2 mr = tex2D(gbuffer_4, ps_in.uv);

                float roughness = mr.y;
                float roughness2 = roughness * roughness;
                float a2 = roughness2 * roughness2;
                float metallic = mr.x;
                float3 f0 = lerp(float3(0.04, 0.04, 0.04), albedo, metallic);
                float3 v = normalize(_WorldSpaceCameraPos.xyz - world_pos);

                float3 radiance = float3(0.0, 0.0, 0.0);


                // direct lighting
                {
                    for (uint i = 0; i < directional_light_count; i++) {
                        
                        float3 light_color = directional_lights[i].color;
                        float light_intensity = directional_lights[i].intensity;
                        float3 light_dir = normalize(-directional_lights[i].direction);

                        if (dot(normal, light_dir) < 0.0) {
                            continue;
                        }
                        BXDF bxdf = InitBXDF(normal, v, light_dir);
                        float D = NDF_GGX(a2, bxdf.NoM);
                        float G = Vis_SmithGGXCombined(a2, bxdf.NoV, bxdf.NoL);
                        float3 F = Fresnel_Schlick(f0, bxdf.VoM);

                        float3 diffuse = Diffuse_Lambert(albedo); 

                        float3 specular = D * G * F;

                        radiance += (diffuse + specular) * bxdf.NoL * light_color * light_intensity; // attenuation;
                    }
                }

                {
                    for (uint i = 0; i < point_light_count; i++) {
                        float3 light_color = point_lights[i].color;
                        float light_intensity = point_lights[i].intensity;
                        
                        float3 light_dir = point_lights[i].position - world_pos;

                        if (dot(normal, light_dir) < 0.0) {
                            continue;
                        }

                        float distance = length(light_dir);
                        light_dir = normalize(light_dir);

                        BXDF bxdf = InitBXDF(normal, v, light_dir);
                        float D = NDF_GGX(a2, bxdf.NoM);
                        float G = Vis_SmithGGXCombined(a2, bxdf.NoV, bxdf.NoL);
                        float3 F = Fresnel_Schlick(f0, bxdf.NoM);

                        float3 diffuse = Diffuse_Lambert(albedo);

                        float3 specular = D * G * F;
                        float distance_attenuation = DistanceFalloff(distance, point_lights[i].falloff);
                        radiance += (diffuse + specular) * bxdf.NoL * light_color * light_intensity * distance_attenuation;
                    }

                }

                {
                    for (uint i = 0; i < spot_light_count; i++) {
                        float3 light_color = spot_lights[i].color;
                        float light_intensity = spot_lights[i].intensity;
                        
                        float3 light_dir = -spot_lights[i].direction;
                        if (dot(normal, light_dir) < 0.0) {
                            continue;
                        }
                        float distance = length(spot_lights[i].position - world_pos);
                        light_dir = normalize(light_dir);

                        BXDF bxdf = InitBXDF(normal, v, light_dir);
                        float D = NDF_GGX(a2, bxdf.NoM);
                        float G = Vis_SmithGGXCombined(a2, bxdf.NoV, bxdf.NoL);
                        float3 F = Fresnel_Schlick(f0, bxdf.NoM);

                        float3 diffuse = Diffuse_Lambert(albedo);

                        float3 specular = D * G * F;
                        float distance_attenuation = DistanceFalloff(distance, spot_lights[i].falloff);
                        float angle_attenuation = AngleFalloff(spot_lights[i].inner_cone, spot_lights[i].outer_cone, spot_lights[i].direction, light_dir);

                        radiance += (diffuse + specular) * bxdf.NoL * light_color * light_intensity * distance_attenuation * angle_attenuation;
                    }

                }
                return float4(radiance, 1.0);
            }
            ENDHLSL
        }
    }

    // FallBack
}