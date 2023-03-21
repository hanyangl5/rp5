Shader "Custuom/shading"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}

    }
        SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _gdepth;
            sampler2D _GT0;
            sampler2D _GT1;
            sampler2D _GT2;
            sampler2D _GT3;
            sampler2D _GT4;
            struct BXDF {
                float NoV;
                float NoL;
                float VoL;
                float NoM;
                float VoM;
                float XoV;
                float XoL;
                float XoM;
                float YoV;
                float YoL;
                float YoM;
            };

            void InitBXDF(inout BXDF bxdf, float3 N, float3 V, float3 L) {
                bxdf.NoL = saturate(dot(N, L));
                bxdf.NoV = saturate(dot(N, V));
                bxdf.VoL = saturate(dot(V, L));
                float3 M = (V + L) / 2.0;
                bxdf.NoM = saturate(dot(N, M));
                bxdf.VoM = saturate(dot(V, M));
                // bxdf.XoV = 0.0f;
                // bxdf.XoL = 0.0f;
                // bxdf.XoM = 0.0f;
                // bxdf.YoV = 0.0f;
                // bxdf.YoL = 0.0f;
                // bxdf.YoM = 0.0f;
            }

            float3 Diffuse_Lambert(float3 albedo) { return albedo * UNITY_INV_PI; }

            float3 Fresnel_Schlick(float3 F0, float LoM) {
                return F0 + (1.0 - F0) * pow(1 - LoM, 5);
            }

            // for isotropic ndf, we provide ggx and gtr

            float NDF_GGX(float roughness2, float NoM) {
                float d = (NoM * roughness2 - NoM) * NoM + 1.0;
                return roughness2 * UNITY_INV_PI / (d * d);
            }

            // float NDF_Aniso_GGX()

            float Vis_SmithGGXCombined(float roughness2, float NoV, float NoL) {
                float Vis_SmithV = NoL * sqrt(NoV * (NoV - NoV * roughness2) + roughness2);
                float Vis_SmithL = NoV * sqrt(NoL * (NoL - NoL * roughness2) + roughness2);
                return 0.5 / (Vis_SmithV + Vis_SmithL);
            }

            float Vis_Aniso_SmithGGXCombined(float ax, float ay, float NoV, float NoL,
                                             float XoV, float XoL, float YoV, float YoL) {
                float Vis_SmithV = NoL * length(float3(ax * XoV, ay * YoV, NoV));
                float Vis_SmithL = NoV * length(float3(ax * XoL, ay * YoL, NoL));
                return 0.5 / (Vis_SmithV + Vis_SmithL);
            }


            float4 frag(v2f i) : SV_Target
            {
                float4 albedo = tex2D(_GT0, i.uv);
                float4 normal = tex2D(_GT1, i.uv);
                float2 mr = tex2D(_GT4, i.uv);
                return float4(mr, 0.0, 1.0);
                // brdf
                // float D = NDF_GGX(mat.roughness2, bxdf.NoM);
                // float G = Vis_SmithGGXCombined(mat.roughness2, bxdf.NoV, bxdf.NoL);
                // float3 F = Fresnel_Schlick(mat.f0, bxdf.NoM);
                // float3 diffuse = Diffuse_Lambert(albedo.rgb);
                // float3 specular = D * G * F;

                //return diffuse + specular;
            }
            ENDCG
        }
    }
}