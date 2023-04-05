using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Timeline;
using UnityEngine.UIElements.Experimental;

namespace RP5
{
    // TODO(hylu): global using in c#? 

    using float3 = UnityEngine.Vector3;
    using float2 = UnityEngine.Vector3;
    using float4 = UnityEngine.Vector3;


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


    public class Lighting
    {

        public static uint MAX_DIRECTIONAL_LIGHT = 1024, MAX_POINT_LIGHT = 1024, MAX_SPOT_LIGHT = 1024;

        public List<DirectionalLight> scene_directional_lights = new List<DirectionalLight>(0);
        public List<PointLight> scene_point_lights = new List<PointLight>(0);
        public List<SpotLight> scene_spot_lights = new List<SpotLight>(0);



        public ComputeBuffer directional_lights = new ComputeBuffer((int)MAX_DIRECTIONAL_LIGHT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLight)));
        public ComputeBuffer point_lights = new ComputeBuffer((int)MAX_POINT_LIGHT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PointLight)));
        public ComputeBuffer spot_lights = new ComputeBuffer((int)MAX_SPOT_LIGHT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SpotLight)));

        public void Setup(ScriptableRenderContext context, SceneConstants scene_constant_data)
        {
            scene_point_lights.Clear();
            scene_directional_lights.Clear();
            scene_spot_lights.Clear();

            ColletSceneLights(scene_constant_data);
            CreateRP5Lights(scene_constant_data);
        }

        public void UploadBuffers(ScriptableRenderContext context)
        {
            directional_lights.SetData(scene_directional_lights);
            point_lights.SetData(scene_point_lights);
            spot_lights.SetData(scene_spot_lights);
            Shader.SetGlobalBuffer("directional_lights", directional_lights);
            Shader.SetGlobalBuffer("point_lights", point_lights);
            Shader.SetGlobalBuffer("spot_lights", spot_lights);
        }
        void ColletSceneLights(SceneConstants scene_constant_data)
        {
            var scene_lights = UnityEngine.RenderSettings.FindObjectsOfType<Light>();
            foreach (Light light in scene_lights)
            {
                if (light.enabled == false) continue;

                var trans = light.GetComponent<Transform>();
                if (light.type == LightType.Directional)
                {
                    DirectionalLight l = new DirectionalLight();
                    l.direction = trans.forward;
                    l.intensity = light.intensity;
                    l.color = new float3(light.color.r, light.color.g, light.color.b);
                    l.temperature = light.colorTemperature;
                    scene_directional_lights.Add(l);
                }
                else if (light.type == LightType.Point)
                {
                    PointLight l = new PointLight();
                    l.position = trans.position;
                    l.intensity = light.intensity;
                    l.color = new float3(light.color.r, light.color.g, light.color.b);
                    l.temperature = light.colorTemperature;
                    l.falloff = light.range;
                    scene_point_lights.Add(l);
                }
                else if (light.type == LightType.Spot)
                {
                    SpotLight l = new SpotLight();
                    l.position = trans.position;
                    l.intensity = light.intensity;
                    l.color = new float3(light.color.r, light.color.g, light.color.b);
                    l.temperature = light.colorTemperature;
                    l.falloff = light.range;
                    l.direction = trans.forward;
                    l.inner_cone = light.innerSpotAngle * UnityEngine.Mathf.PI / 180.0f;
                    l.outer_cone = light.spotAngle * UnityEngine.Mathf.PI / 180.0f;
                    scene_spot_lights.Add(l);
                }
            }

            scene_constant_data.directional_lights_count = (uint)scene_directional_lights.Count();
            scene_constant_data.point_lights_count = (uint)scene_point_lights.Count();
            scene_constant_data.spot_lights_count = (uint)scene_spot_lights.Count();
        }

        // create more lights
        void CreateRP5Lights(SceneConstants scene_constant_data)
        {
            
        }
    }
}
