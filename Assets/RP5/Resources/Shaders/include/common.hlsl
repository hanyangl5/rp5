#ifndef _COMMON_H
#define _COMMON_H

#define FULL_SCREEN_CS_THREAD_X 8
#define FULL_SCREEN_CS_THREAD_Y 8
#define FULL_SCREEN_CS_THREAD_Z 1

// #define WORK_GROUP_SIZE 32
// #define SCREEN_SPACE_PASS_WORK_GROUP_X 8
// #define SCREEN_SPACE_PASS_WORK_GROUP_Y 4

#define MAX_POINT_LIGHT 1024
#define MAX_SPOT_LIGHT 1024
#define MAX_POINT_LIGHT 1024

#define MAX_LIGHT_PER_CELL  64

#define FLT_MIN -3.4028235e-38
#define FLT_MAX 3.4028235e+38

float Luminance(float3 color) { return 0.2126 * color.r + 0.7152 * color.g + 0.0722 * color.b; }

#endif