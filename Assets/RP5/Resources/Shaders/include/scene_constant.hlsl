#ifndef SCENE_CONSTANT_HLSL
#define SCENE_CONSTANT_HLSL

#define DECLARE_SCENE_CONSTANTS \
float4x4 projection_non_jittered; \
float4x4 view_projection_non_jittered; \
float4x4 view_projection_prev_non_jittered; \
float4x4 projection; \ 
float4x4 view_projection; \
float4x4 projection_inv_non_jittered; \
float4x4 projection_inv; \ 
float4x4 world_to_camera; \
float4x4 camera_to_world; \
float3 camera_pos_ws; \
int width; \
int height; \
float2 camera_nf;  

#endif //SCENE_CONSTANT_HLSL
