struct SceneLightSetting{
    // 1 bit for shadow
    // 1 bit for affect scene
    // 1 bit for using temperature to derive color
};


struct DirectionalLight {
    float3 color;
    float intensity;
    float temperature;
    float3 direction;
};

struct SpotLight {
    float3 color;
    float intensity;
    float temperature;  
};

struct PointLight {

};

// --- area lights --- 

// DECIMA ENGINE: ADVANCES IN LIGHTING AND AA
struct CapsuleLight {

};

// Disk Light,  Moving the Frostbite engine to Physically Based Rendering
struct DiskLight {

};

// Eric Heiz, LTC Polygon Light

struct RectLight {

};
