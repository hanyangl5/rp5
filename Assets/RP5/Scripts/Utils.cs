using System;

namespace RP5
{

    using float3 = System.Numerics.Vector3;
    using float2 = System.Numerics.Vector2;
    using float4 = System.Numerics.Vector4;

    public class SceneConstants
    {
        public uint directional_lights_count = 0;
        public uint point_lights_count = 0;
        public uint spot_lights_count = 0;
    };
}