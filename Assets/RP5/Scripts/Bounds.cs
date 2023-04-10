using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RP5
{
    using float3 = UnityEngine.Vector3;
    using float2 = UnityEngine.Vector3;
    using float4 = UnityEngine.Vector4;
    public struct AABB
    {
        public float3 min;
        public float3 max;
    }

}