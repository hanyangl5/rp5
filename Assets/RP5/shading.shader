Shader "Custuom/shading"
{

    // CustomEditor = "ExampleCustomEditor"
    Properties
    {
    // Material Properties
    }
    
    SubShader
    {
    Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM

            #include "UnityCG.cginc"
            #include "HLSLSupport.cginc"
            #include "brdf.hlsl"

            #pragma vertex VS_MAIN
            #pragma fragment PS_MAIN

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

            sampler2D gdepth;
            sampler2D gbuffer_0;
            sampler2D gbuffer_1;
            sampler2D gbuffer_2;
            sampler2D gbuffer_3;
            sampler2D gbuffer_4;
            
            SamplerState sampler1; // a bilinear sampler to fetch gbuffer

            float4 PS_MAIN(PsInput i) : SV_Target
            {
                //float3 albedo = gbuffer_0.Sample(sampler1, i.uv).rgb;
                //float3 normal = gbuffer_1.Sample(sampler1, i.uv).rgb;
                //float2 mr = gbuffer_4.Sample(sampler1, i.uv);

                float3 albedo = tex2D(gbuffer_0, i.uv).rgb;
                float3 normal = tex2D(gbuffer_1, i.uv).rgb;
                float2 mr = tex2D(gbuffer_4, i.uv);
                return float4(normal, 1.0);
                // brdf
                // float D = NDF_GGX(mat.roughness2, bxdf.NoM);
                // float G = Vis_SmithGGXCombined(mat.roughness2, bxdf.NoV, bxdf.NoL);
                // float3 F = Fresnel_Schlick(mat.f0, bxdf.NoM);
                // float3 diffuse = Diffuse_Lambert(albedo.rgb);
                // float3 specular = D * G * F;

                //return albedo;
            }
            ENDHLSL
        }
    }

    // FallBack
}