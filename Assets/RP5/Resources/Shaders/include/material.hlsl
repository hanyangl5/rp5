#ifndef MATERIAL_HLSL
#define MATERIAL_HLSL

#define MATERIAL_ID_OPAQUE 0
#define MATERIAL_ID_MASKED 1
#define MATERIAL_ID_SKIN 2
#define MATERIAL_ID_EYE 3
#define MATERIAL_ID_FUR 4
#define MATERIAL_ID_TRANSPARENT 5
#define MATERIAL_ID_CLOTH 6

struct MaterialProperties {
    float3 albedo;
    float3 normal;
    float3 f0;
    float metallic;  // 0
    float roughness; // 0.5
    float roughness2;
    float a2;
    float3 emissive;
    float anisotropy;
    uint id;
};

MaterialProperties InitMaterial(float3 albedo, float3 normal, float metallic, float roughness, float3 emissive, float anisotropy = 0.0) {
    MaterialProperties mat;
    mat.albedo = albedo;
    mat.roughness = roughness;
    mat.roughness2 = roughness * roughness;
    mat.a2 = mat.roughness2 * mat.roughness2;
    mat.metallic = metallic;
    mat.emissive = emissive;
    mat.f0 = lerp(albedo, float3(0.04, 0.04, 0.04), metallic);
    mat.anisotropy = anisotropy;
    return mat;
}

#endif // MATERIAL_HLSL
