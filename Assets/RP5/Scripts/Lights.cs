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
    using static UnityEngine.UI.CanvasScaler;
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
    public struct Frustum
    {
        public float3[] planes; // left, right, bottom, top
        public float z_min;
        public float z_max;
    };
    struct ClusterInfo
    {
        public int x, y, z;
        public List<int> light_list;
    };
    public class Lighting
    {

        public static uint MAX_DIRECTIONAL_LIGHT = 1024, MAX_POINT_LIGHT = 1024, MAX_SPOT_LIGHT = 1024;

        public List<DirectionalLight> directional_lights = new List<DirectionalLight>(0);
        public List<PointLight> point_lights = new List<PointLight>(0);
        public List<SpotLight> spot_lights = new List<SpotLight>(0);


        List<AABB> point_light_aabbs = new List<AABB>(0);
        List<AABB> spot_light_aabbs = new List<AABB>(0);
        List<Matrix4x4> point_light_to_world = new List<Matrix4x4>(0);
        List<Matrix4x4> spot_light_to_world = new List<Matrix4x4>(0);
        float3[] cluster_vertices = new float3[(num_tiles_x + 1) * (num_tiles_y + 1) * (num_tiles_z + 1)];
        static int num_tiles_x = 32;
        static int num_tiles_y = 32;
        static int num_tiles_z = 16;
        float z_bias; // tweak the second z plane
        float z_end; // 
        float near_plane;
        float far_plane;
        Matrix4x4 inverse_projection;
        Matrix4x4 inverse_view;
        float3 eye_pos;

        public ComputeBuffer cluster_vertices_buffer = new ComputeBuffer((num_tiles_x+1) * (num_tiles_y+1)*(num_tiles_z+1), System.Runtime.InteropServices.Marshal.SizeOf(typeof(float3)));

        public ComputeBuffer directional_lights_buffer = new ComputeBuffer((int)MAX_DIRECTIONAL_LIGHT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLight)));
        public ComputeBuffer point_lights_buffer = new ComputeBuffer((int)MAX_POINT_LIGHT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PointLight)));
        public ComputeBuffer spot_lights_buffer = new ComputeBuffer((int)MAX_SPOT_LIGHT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SpotLight)));

        public ComputeBuffer point_light_aabbs_buffer = new ComputeBuffer((int)MAX_POINT_LIGHT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(AABB)));
        public ComputeBuffer spot_light_aabbs_buffer = new ComputeBuffer((int)MAX_SPOT_LIGHT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(AABB)));
        
        public ComputeBuffer point_light_clip_aabbs_buffer = new ComputeBuffer((int)MAX_POINT_LIGHT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(AABB)));
        public ComputeBuffer spot_light_clip_aabbs_buffer = new ComputeBuffer((int)MAX_SPOT_LIGHT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(AABB)));

        public ComputeBuffer point_light_to_world_buffer = new ComputeBuffer((int)MAX_POINT_LIGHT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Matrix4x4)));
        public ComputeBuffer spot_light_to_world_buffer = new ComputeBuffer((int)MAX_SPOT_LIGHT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Matrix4x4)));



        public ComputeBuffer item_list_buffer = new ComputeBuffer(num_tiles_x * num_tiles_y * num_tiles_z * 1024, 4);
        public ComputeBuffer cluster_list_offset_buffer = new ComputeBuffer(1, 4);


        public void Setup(ScriptableRenderContext context, SceneConstants scene_constant_data)
        {
            if (point_lights != null)
                point_lights.Clear();
            if (directional_lights != null)
                directional_lights.Clear();
            if (spot_lights != null)
                spot_lights.Clear();
            if (point_light_aabbs != null)
                point_light_aabbs.Clear();
            if (spot_light_aabbs != null)
                spot_light_aabbs.Clear();
            if(point_light_to_world != null)
                point_light_to_world.Clear();
            if (spot_light_to_world != null)
                spot_light_to_world.Clear();
            //if (planes_xyz != null)
            //    planes_xyz.Clear();

            ColletSceneLights(scene_constant_data);
        }

        public void UploadBuffers(ScriptableRenderContext context)
        {
            directional_lights_buffer.SetData(directional_lights);
            point_lights_buffer.SetData(point_lights);
            spot_lights_buffer.SetData(spot_lights);
            Shader.SetGlobalBuffer("directional_lights", directional_lights_buffer);
            Shader.SetGlobalBuffer("point_lights", point_lights_buffer);
            Shader.SetGlobalBuffer("spot_lights", spot_lights_buffer);

            cluster_vertices_buffer.SetData(cluster_vertices);
            Shader.SetGlobalBuffer("cluster_vertices", cluster_vertices_buffer);

            point_light_aabbs_buffer.SetData(point_light_aabbs);
            spot_light_aabbs_buffer.SetData(spot_light_aabbs);

            Shader.SetGlobalInteger("num_tiles_x", num_tiles_x);
            Shader.SetGlobalInteger("num_tiles_y", num_tiles_y);
            Shader.SetGlobalInteger("num_tiles_z", num_tiles_z);

            point_light_aabbs_buffer.SetData(point_light_aabbs);
            spot_light_aabbs_buffer.SetData(point_light_aabbs);

            Shader.SetGlobalBuffer("point_lights_aabb", point_light_aabbs_buffer);
            Shader.SetGlobalBuffer("spot_lights_aabb", spot_light_aabbs_buffer);
            Shader.SetGlobalBuffer("point_lights_clip_aabb", point_light_clip_aabbs_buffer);
            Shader.SetGlobalBuffer("spot_lights_clip_aabb", spot_light_clip_aabbs_buffer);

            point_light_to_world_buffer.SetData(point_light_to_world);
            spot_light_to_world_buffer.SetData(spot_light_to_world);
            Shader.SetGlobalBuffer("point_light_to_world", point_light_to_world_buffer);
            Shader.SetGlobalBuffer("spot_light_to_world", spot_light_to_world_buffer);

            Shader.SetGlobalTexture("cluster_list", cluster_list);
            Shader.SetGlobalBuffer("item_list", item_list_buffer);
            Shader.SetGlobalBuffer("cluster_list_offset", cluster_list_offset_buffer);
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

                    Bounds bounds = new Bounds(new float3(0.0f,0.0f,0.0f), new float3(light.range, light.range, light.range));
                    // local space aabb
                    AABB aabb = new AABB();
                    aabb.min = bounds.min;
                    aabb.max = bounds.max;
                    point_light_aabbs.Add(aabb);
                    point_light_to_world.Add(light.transform.localToWorldMatrix);
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

                    // TODO: better bounds
                    Bounds bounds = new Bounds(light.transform.position, new float3(light.range, light.range, light.range));
                    // local space aabb
                    AABB aabb = new AABB();
                    aabb.min = bounds.min;
                    aabb.max = bounds.max;
                    spot_light_aabbs.Add(aabb);
                    spot_light_to_world.Add(light.transform.localToWorldMatrix);
                }
            }

            scene_constant_data.directional_lights_count = (uint)directional_lights.Count;
            scene_constant_data.point_lights_count = (uint)point_lights.Count;
            scene_constant_data.spot_lights_count = (uint)spot_lights.Count;
        }

        // create more lights
        //void CreateRP5Lights(SceneConstants scene_constant_data)
        //{

        //}

        public RenderTexture cluster_list;
        List<uint> item_list;
        uint cluster_list_offset;
        // TODO(hylu): only rebuild when projectionmatrix/near/far changes
        public void SetupCluster(Camera camera, float t_z_bias = 5.0f, float t_z_end = 500.0f)
        {
            if (cluster_list == null)
            {
                cluster_list_offset = new int();
                cluster_list = new RenderTexture(num_tiles_x, num_tiles_y, 0, RenderTextureFormat.RGBAUShort, 1);
                cluster_list.enableRandomWrite = true;
                cluster_list.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
                cluster_list.volumeDepth = num_tiles_z;
                cluster_list.filterMode = FilterMode.Point;
            }
            // TODO: cached the grid
            near_plane = camera.nearClipPlane;
            far_plane = camera.farClipPlane;
            inverse_projection = camera.projectionMatrix.inverse;
            z_bias = near_plane + t_z_bias;
            z_end = t_z_end;
            inverse_view = camera.cameraToWorldMatrix.inverse;
            far_plane = far_plane > z_end ? z_end : far_plane; // constaint the max light range
            eye_pos = camera.transform.position;
#if true

            int x_offset = 0;
            int y_offset = num_tiles_x + 1;
            // tweak the first slice to get better depth distribution near near plane

            float[] zplanes = new float[num_tiles_z + 1];
            zplanes[0] = near_plane;
            zplanes[1] = z_bias;
            for (int z = 2; z <= num_tiles_z; z++)
            {
                zplanes[z] = z_bias * Mathf.Pow(far_plane / z_bias, (float)z / num_tiles_z);
                
            }

            // build x/y planes

            // left to right
            for (int i = 0; i < num_tiles_x + 1; i++)
            {
                float x = -1.0f + 1.0f / num_tiles_x * i * 2.0f;
                // bottom to up
                for (int j = 0; j < num_tiles_y + 1; j++)
                {
                    float y = -1.0f + 1.0f / num_tiles_y * j * 2.0f;
                    for (int k = 0; k < num_tiles_z + 1; k++)
                    {
                        int point_index = k * (num_tiles_x + 1) * (num_tiles_y + 1) + j * (num_tiles_x + 1) + i;
                        cluster_vertices[point_index] = new Vector3(x, y, zplanes[k]);
                    }
                }
            }
            int a = 5;

#endif
        }
    }
}
