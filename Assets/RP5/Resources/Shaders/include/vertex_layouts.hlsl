#ifndef _VERTEX_LAYOUTS_
#define _VERTEX_LAYOUTS_

struct VsInputDefault {
    float4 pos : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
};
struct PsInputDefault {
    float2 uv : TEXCOORD0;
    float4 position : SV_POSITION;
    float3 normal : NORMAL;
    float4 position_ws : TEXCOORD02;
};

typedef VsInputDefault VsInput;
typedef PsInputDefault PsInput;
typedef PsInput VsOutput;

#endif //VERTEX_LAYOUTS_HLSL
