#ifndef _VERTEX_LAYOUTS_
#define _VERTEX_LAYOUTS_

struct VsInputDefault {
    float4 pos : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
    float3 tangent : TANGENT;
};
struct PsInputDefault {
    float2 uv : TEXCOORD0;
    float4 position : SV_POSITION;
    float3 normal : NORMAL;
    float4 position_ws : TEXCOORD02;
    float3 tangent_ws : TANGENT;
};

typedef VsInputDefault VsInput;
typedef PsInputDefault PsInput;
typedef PsInput VsOutput;

#endif //VERTEX_LAYOUTS_HLSL
