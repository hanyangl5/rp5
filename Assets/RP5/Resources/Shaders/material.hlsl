#define HAS_BASE_COLOR 0x01
#define HAS_NORMAL 0x10
#define HAS_METALLIC_ROUGHNESS 0x100
#define HAS_EMISSIVE 0x1000
#define HAS_ALPHA 0x10000

struct MaterialProperties {
    float3 albedo;
    float3 normal;
    float3 f0;
    float metallic;  // 0
    float roughness; // 0.5
    float roughness2;
    float a2;
    float3 emissive;
};

MaterialProperties InitMaterial(float3 albedo, float3 normal, float metallic, float roughness, float3 emissive) {
    MaterialProperties mat;
    mat.albedo = albedo;
    mat.roughness = roughness;
    mat.roughness2 = roughness * roughness;
    mat.a2 = mat.roughness2 * mat.roughness2;
    mat.metallic = metallic;
    mat.emissive = emissive;
    mat.f0 = lerp(albedo, float3(0.04, 0.04, 0.04), metallic);
    return mat;
}