
// 1 bit for shadow
// 1 bit for affect scene
// 1 bit for using temperature to derive color
// uint packed_setting;

#define DIRECTIONAL_LIGHT 0
#define POINT_LIGHT 1
#define SPOT_LIGHT 2
#define CAPSULE_LIGHT 3
#define DISK_LIGHT 4
#define RECT_LIGHT 5

struct DirectionalLight {
    float3 color;
    float intensity;
    float temperature;
    uint packed_scene_setting;
    float3 direction;
};

struct SpotLight {
    float3 color;
    float intensity;
    float temperature;
    uint packed_scene_setting;
    float3 direction;
    float falloff;
    float3 position; 
    float inner_cone;   
    float outer_cone;
};

struct PointLight {
    float3 color;
    float intensity;
    float temperature;  
    uint packed_scene_setting;
    float falloff;
    float3 position;
};

// --- area lights --- 

// DECIMA ENGINE: ADVANCES IN LIGHTING AND AA
struct CapsuleLight {
    float3 color;
    float intensity;
    float temperature;
    uint packed_scene_setting;
    float falloff;
    float3 position;
    float radius;
    float length;
    float3 tangent;
};

// Disk Light,  Moving the Frostbite engine to Physically Based Rendering
// struct DiskLight { 
// };

// Eric Heiz, LTC Polygon Light

// struct RectLight {
// };


// functions

// Moving the Frostbite engine to Physically Based Rendering

float DistanceFalloff(float dist, float r) {
    // Brian Karis, 2013. Real Shading in Unreal Engine 4.
    float d2 = dist * dist;
    float r2 = r * r;
    float a = saturate(1.0f - (d2 * d2) / (r2 * r2));
    return a * a / max(d2, 1e-4);
}

float AngleFalloff(float inner_cone, float outer_cone, float3 direction, float3 light_dir) {
    float cosOuter = cos(outer_cone);
    float spotScale = 1.0 / max(cos(inner_cone) - cosOuter, 1e-4);
    float spotOffset = -cosOuter * spotScale;

    float cd = dot(normalize(-direction), light_dir);
    float attenuation = clamp(cd * spotScale + spotOffset, 0.0, 1.0);
    return attenuation * attenuation;
}