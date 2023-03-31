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
    float3 emissive;

    // disney

    float anisotropic;     // 0
    float sheen;           // 0
    float sheen_tint;      // 0.5
    float subsurface;      // 0
    float clearcoat;       // 0
    float clearcoat_gloss; // 0.5
};