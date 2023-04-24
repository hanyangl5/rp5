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

    public class LightManager
    {
        public void Resize(int w, int h) {
            
        }
        public static uint MAX_DIRECTIONAL_LIGHT = 1024, MAX_POINT_LIGHT = 1024, MAX_SPOT_LIGHT = 1024;

        public List<DirectionalLight> directional_lights = new List<DirectionalLight>(0);
        public List<PointLight> point_lights = new List<PointLight>(0);
        public List<SpotLight> spot_lights = new List<SpotLight>(0);

        Matrix4x4 inverse_view;
        float3 eye_pos;

        public ComputeBuffer directional_lights_buffer = new ComputeBuffer((int)MAX_DIRECTIONAL_LIGHT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLight)));
        public ComputeBuffer point_lights_buffer = new ComputeBuffer((int)MAX_POINT_LIGHT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PointLight)));
        public ComputeBuffer spot_lights_buffer = new ComputeBuffer((int)MAX_SPOT_LIGHT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SpotLight)));


        public void Setup(ScriptableRenderContext context, SceneConstants scene_constant_data)
        {
            if (point_lights != null)
                point_lights.Clear();
            if (directional_lights != null)
                directional_lights.Clear();
            if (spot_lights != null)
                spot_lights.Clear();

            //if (planes_xyz != null)
            //    planes_xyz.Clear();

            ColletSceneLights(scene_constant_data);
            UploadBuffers(context, scene_constant_data);
        }

        public void UploadBuffers(ScriptableRenderContext context, SceneConstants scene_constant_data)
        {
            directional_lights_buffer.SetData(directional_lights);
            point_lights_buffer.SetData(point_lights);
            spot_lights_buffer.SetData(spot_lights);
            Shader.SetGlobalBuffer("directional_lights", directional_lights_buffer);
            Shader.SetGlobalBuffer("point_lights", point_lights_buffer);
            Shader.SetGlobalBuffer("spot_lights", spot_lights_buffer);
            Shader.SetGlobalInteger("directional_light_count", (int)scene_constant_data.directional_lights_count);
            Shader.SetGlobalInteger("point_light_count", (int)scene_constant_data.point_lights_count);
            Shader.SetGlobalInteger("spot_light_count", (int)scene_constant_data.spot_lights_count);
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
                    directional_lights.Add(l);
                }
                else if (light.type == LightType.Point)
                {
                    PointLight l = new PointLight();
                    l.position = trans.position;
                    l.intensity = light.intensity;
                    l.color = new float3(light.color.r, light.color.g, light.color.b);
                    l.temperature = light.colorTemperature;
                    l.falloff = light.range;
                    point_lights.Add(l);
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
                    spot_lights.Add(l);
                }
            }

            scene_constant_data.directional_lights_count = (uint)directional_lights.Count;
            scene_constant_data.point_lights_count = (uint)point_lights.Count;
            scene_constant_data.spot_lights_count = (uint)spot_lights.Count;
        }

    }
}