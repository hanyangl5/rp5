#ifndef _BRDF_H
#define _BRDF_H

#include "common.hlsl"
#include "common_math.hlsl"

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

BXDF InitBXDF(float3 N, float3 V, float3 L, float3 X = 0, float3 Y = 0) {
    BXDF bxdf;
    bxdf.NoL = dot(N, L);
    bxdf.NoV = dot(N, V);
    bxdf.VoL = dot(V, L);
    float3 M = (V + L) / 2.0;
    bxdf.NoM = saturate(dot(N, M));
    bxdf.VoM = saturate(dot(V, M));
    if (all(X == float3(0.0, 0.0, 0.0)) && all(Y == float3(0.0, 0.0, 0.0))) {
        return bxdf;
    }
	bxdf.XoV = dot(X, V);
	bxdf.XoL = dot(X, L);
	bxdf.XoM = saturate(dot(M, X));
	bxdf.YoV = dot(Y, V);
	bxdf.YoL = dot(Y, L);
	bxdf.YoM = saturate(dot(Y, M));
    return bxdf;
}

// BXDF InitBXDF(float3 N, float3 V, float3 L) {
//     BXDF bxdf;
//     bxdf.NoL = dot(N, L);
//     bxdf.NoV = dot(N, V);
//     bxdf.VoL = dot(V, L);
//     float3 M = (V + L) / 2.0;
//     bxdf.NoM = saturate(dot(N, M));
//     bxdf.VoM = saturate(dot(V, M));
//     bxdf.XoV = 0.0f;
//     bxdf.XoL = 0.0f;
//     bxdf.XoM = 0.0f;
//     bxdf.YoV = 0.0f;
//     bxdf.YoL = 0.0f;
//     bxdf.YoM = 0.0f;
//     return bxdf;
// }

float3 Diffuse_Lambert(float3 albedo) { return albedo * _1DIVPI; }

float3 Fresnel_Schlick(float3 F0, float LoM) {
    return F0 + (1.0 - F0) * pow(1 - LoM, 5);
}

// for isotropic ndf, we provide ggx and gtr

float NDF_GGX(float a2, float NoM) {
    float d = (NoM * a2 - NoM) * NoM + 1.0;
    return a2 * _1DIVPI / (d * d);
}

// float NDF_Aniso_GGX()

float NDF_Aniso_GGX( float ax, float ay, float NoH, float XoM, float YoM )
{
	float a2 = ax * ay;
	float3 V = float3(ay * XoM, ax * YoM, a2 * NoH);
	float S = dot(V, V);

	return (1.0f * _1DIVPI) * a2 * Pow2(a2 / S);
}

float Vis_SmithGGXCombined(float a2, float NoV, float NoL) {
    float Vis_SmithV = NoL * sqrt(NoV * (NoV - NoV * a2) + a2);
    float Vis_SmithL = NoV * sqrt(NoL * (NoL - NoL * a2) + a2);
    return 0.5 / (Vis_SmithV + Vis_SmithL);
}

float Vis_Aniso_SmithGGXCombined(float ax, float ay, float NoV, float NoL,
                                    float XoV, float XoL, float YoV, float YoL) {
    float Vis_SmithV = NoL * length(float3(ax * XoV, ay * YoV, NoV));
    float Vis_SmithL = NoV * length(float3(ax * XoL, ay * YoL, NoL));
    return 0.5 / (Vis_SmithV + Vis_SmithL);
}

// Convert a roughness and an anisotropy factor into GGX alpha values respectively for the major and minor axis of the tangent frame
void GetAnisotropicRoughness(float Alpha, float Anisotropy, out float ax, out float ay)
{
#if 1
	// Anisotropic parameters: ax and ay are the roughness along the tangent and bitangent	
	// Kulla 2017, "Revisiting Physically Based Shading at Imageworks"
	ax = max(Alpha * (1.0 + Anisotropy), 0.001f);
	ay = max(Alpha * (1.0 - Anisotropy), 0.001f);
#else
	float K = sqrt(1.0f - 0.95f * Anisotropy);
	ax = max(Alpha / K, 0.001f);
	ay = max(Alpha * K, 0.001f);
#endif
}


#endif