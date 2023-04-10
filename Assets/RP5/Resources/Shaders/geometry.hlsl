#ifndef _GEOMETRY_H_
#define _GEOMETRY_H_

struct Frustum {
    float4 planes[6]; // left, right, bottom, top
};

struct AABB {
    float3 min;
    float3 max;
};

bool AABBsIntersect(AABB a, AABB b) {
    // on each axis, check to see if the boxes are not overlapping
    if (a.max.x < b.min.x || a.min.x > b.max.x)
        return false;
    if (a.max.y < b.min.y || a.min.y > b.max.y)
        return false;
    if (a.max.z < b.min.z || a.min.z > b.max.z)
        return false;
    // if all checks pass, the boxes are intersecting
    return true;
}


bool AABBIntersectFrustum(Frustum frustum, AABB aabb) {

    float3 corners[8] = {
        {aabb.min.x, aabb.min.y, aabb.min.z}, // x y z
        {aabb.max.x, aabb.min.y, aabb.min.z}, // X y z
        {aabb.min.x, aabb.max.y, aabb.min.z}, // x Y z
        {aabb.max.x, aabb.max.y, aabb.min.z}, // X Y z

        {aabb.min.x, aabb.min.y, aabb.max.z}, // x y Z
        {aabb.max.x, aabb.min.y, aabb.max.z}, // X y Z
        {aabb.min.x, aabb.max.y, aabb.max.z}, // x Y Z
        {aabb.max.x, aabb.max.y, aabb.max.z}, // X Y Z
    };
    

    for (uint i = 0; i < 6; i++) {
        uint result = 0;
        for (uint j = 0; j < 8; j++) {
            // neg for all corners
            if (dot(corners[j], frustum.planes[i]) < 0.0f) {
                result++;
            }
        }
        if (result == 8) {
            return false;
        }
    }
    return true;
}

#endif