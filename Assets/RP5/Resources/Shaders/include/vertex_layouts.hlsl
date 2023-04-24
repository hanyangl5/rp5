#ifndef _VERTEX_LAYOUTS_
#define _VERTEX_LAYOUTS_

struct VsInputDefault {
    float4 pos : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
};
struct PsInputDefault {
    float2 uv : TEXCOORD0;
    float4 position : SV_POSITION;
    //float3 normal : NORMAL;
    //float4 position_ws : TEXCOORD02;
    //float4 tangent_ws : TANGENT;
    float4 t2w0 : TEXCOORD1;
    float4 t2w1 : TEXCOORD2;
    float4 t2w2 : TEXCOORD3;//xyz 存储着 从切线空间到世界空间的矩阵，w存储着世界坐标
};

typedef VsInputDefault VsInput;
typedef PsInputDefault PsInput;
typedef PsInput VsOutput;

#define INIT_VS_OUT \
float4 postion_ws = mul(unity_ObjectToWorld, v.pos); \
o.position = mul(view_projection, postion_ws); \
o.uv = v.uv; \
float3 normal_ws = UnityObjectToWorldNormal(v.normal); \
half3 tangent_ws = UnityObjectToWorldDir(v.tangent); \
half3 bitangent_ws = cross(normal_ws, tangent_ws) * v.tangent.w; \
o.t2w0 = float4(tangent_ws.x, bitangent_ws.x, normal_ws.x, postion_ws.x); \
o.t2w1 = float4(tangent_ws.y, bitangent_ws.y, normal_ws.y, postion_ws.y); \
o.t2w2 = float4(tangent_ws.z, bitangent_ws.z, normal_ws.z, postion_ws.z);


#endif //VERTEX_LAYOUTS_HLSL
