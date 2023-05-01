#ifndef MOTION_VECTOR_HLSL
#define MOTION_VECTOR_HLSL

// Existing code goes here
// using the current and previous view projection matrices 
float2 ComputeMotionVector(float4 position_ws, float4x4 vp, float4x4 vp_prev)
{
    // Compute the position of the current pixel in the previous frame
    float4 position_cs_prev = mul(vp_prev, position_ws);
    float2 position_uv_prev = (position_cs_prev.xy / position_cs_prev.w); // [0, 1]
    position_uv_prev = position_uv_prev;

    // Compute the position of the current pixel in the current frame
    float4 position_cs = mul(vp, position_ws);
    float2 position_uv = (position_cs.xy / position_cs.w); // [0, 1]
    position_uv = position_uv;
    // Compute the motion vector between the two positions
    float2 motion_vector = position_uv - position_uv_prev;
    
    return motion_vector * 0.5; // map to uv
}

#endif // MOTION_VECTOR_HLSL