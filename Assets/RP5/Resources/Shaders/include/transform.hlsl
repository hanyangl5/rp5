#ifndef TRANSFORM_HLSL
#define TRANSFORM_HLSL

// uv is from [0, 1]
float4 ComputePositionWsFromDepth(float2 uv, float4x4 inverse_vp, float depth) {
    float4 pos_cs = float4(uv * 2.0 - 1.0, depth, 1.0);
    pos_cs.y = -pos_cs.y;
    float4 pos_ws = mul(inverse_vp, pos_cs);
    pos_ws /= pos_ws.w;
    return pos_ws;
}

float4 ComputePositionVsFromDepth(float2 uv, float4x4 inverse_p, float depth) {
    float4 pos_cs = float4(uv * 2.0 - 1.0, depth, 1.0);
    pos_cs.y = -pos_cs.y;
    float4 pos_ws = mul(inverse_p, pos_cs);
    pos_ws /= pos_ws.w;
    return pos_ws;
}

#endif // TRANSFORM_HLSL