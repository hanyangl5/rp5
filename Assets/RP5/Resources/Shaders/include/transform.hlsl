#ifndef TRANSFORM_HLSL
#define TRANSFORM_HLSL

float4 ComputeWorldPosFromDepth(float2 uv, float4x4 inverse_vp, float depth) {
    float4 pos_cs = float4(uv.x, uv.y, depth, 1.0);
    pos_cs.y = -pos_cs.y;
    float4 pos_ws = mul(inverse_vp, pos_cs);
    pos_ws /= pos_ws.w;
    return pos_ws;
}

#endif // TRANSFORM_HLSL