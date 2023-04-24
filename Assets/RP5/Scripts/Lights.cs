using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.VisualScripting;
using System.Text;
using System.Collections;
using System;
using JetBrains.Annotations;

namespace RP5
{
    // TODO(hylu): global using in c#? 

    using float2 = UnityEngine.Vector2;
    using float3 = UnityEngine.Vector3;
    using float4 = UnityEngine.Vector4;


    enum LIGHT_TYPE
    {
        DIRECTIONAL = 0,
        POINT = 1,
        SPOT = 2,
        CAPSULE = 3,
        DISK = 4,
        RECT = 5
    }


    // 1 bit for shadow
    // 1 bit for affect scene
    // 1 bit for using temperature to derive color
    // uint packed_setting;


    public struct DirectionalLight
    {
        public float3 color;
        public float intensity;
        public float temperature;
        public uint packed_scene_setting;
        public float3 direction;
    };

    public struct SpotLight
    {
        public float3 color;
        public float intensity;
        public float temperature;
        public uint packed_scene_setting;
        public float3 direction;
        public float falloff;
        public float3 position;
        public float inner_cone;
        public float outer_cone;
    };

    public struct PointLight
    {
        public float3 color;
        public float intensity;
        public float temperature;
        public uint packed_scene_setting;
        public float falloff;
        public float3 position;
    };

}
