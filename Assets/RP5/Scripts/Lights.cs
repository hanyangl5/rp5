using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.VisualScripting;
using System.Text;
using System.Collections;
using System;

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

        //List<float4> planes_xyz;
        public float4[] planes_xyz = new float4[num_tiles_x + 1 + num_tiles_y + 1 + num_tiles_z + 1];
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

        public ComputeBuffer directional_lights_buffer = new ComputeBuffer((int)MAX_DIRECTIONAL_LIGHT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLight)));
        public ComputeBuffer point_lights_buffer = new ComputeBuffer((int)MAX_POINT_LIGHT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PointLight)));
        public ComputeBuffer spot_lights_buffer = new ComputeBuffer((int)MAX_SPOT_LIGHT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SpotLight)));

        public ComputeBuffer point_light_aabbs_buffer = new ComputeBuffer((int)MAX_POINT_LIGHT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(AABB)));
        public ComputeBuffer spot_light_aabbs_buffer = new ComputeBuffer((int)MAX_SPOT_LIGHT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(AABB)));


        public ComputeBuffer item_list_buffer = new ComputeBuffer(num_tiles_x * num_tiles_y * num_tiles_z * 1024, 4);
        public ComputeBuffer cluster_list_offset_buffer = new ComputeBuffer(1, 4);

        public ComputeBuffer world_space_planes_buffer = new ComputeBuffer(num_tiles_x + num_tiles_y + num_tiles_z + 3, System.Runtime.InteropServices.Marshal.SizeOf(typeof(float4)));
        float sqrt_2 = (float)Math.Sqrt(2.0f);


        public bool InsideFrustum(Frustum frustum, AABB aabb)
        {
            if ((aabb.rt.z < frustum.z_min) || (aabb.ld.z > frustum.z_max))
            {
                return false;
            }

            List<float3> corners = new List<float3>
            {
                new float3(aabb.ld.x, aabb.ld.y, aabb.ld.z), // x y z
                new float3(aabb.rt.x, aabb.ld.y, aabb.ld.z), // X y z
                new float3(aabb.ld.x, aabb.rt.y, aabb.ld.z), // x Y z
                new float3(aabb.rt.x, aabb.rt.y, aabb.ld.z), // X Y z
                new float3(aabb.ld.x, aabb.ld.y, aabb.rt.z), // x y Z
                new float3(aabb.rt.x, aabb.ld.y, aabb.rt.z), // X y Z
                new float3(aabb.ld.x, aabb.rt.y, aabb.rt.z), // x Y Z
                new float3(aabb.rt.x, aabb.rt.y, aabb.rt.z) // X Y Z
            };

            for (int i = 0; i < 4; i++)
            {
                uint result = 0;
                for (int j = 0; j < 8; j++)
                {
                    // neg for all corners
                    if (Vector3.Dot(corners[j], frustum.planes[i]) < 0.0f)
                    {
                        result++;
                    }
                }
                if (result == 8)
                {
                    return false;
                }
            }
            return true;
        }

        //public Frustum GetFrustum(int x, int y, int z)
        //{
        //    Frustum f;
        //    f.planes = new float3[4];
        //    f.z_min = planes_z[z];
        //    f.z_max = planes_z[z + 1];
        //    f.planes[0] = planes_xy[x];
        //    f.planes[1] = -planes_xy[x + 1];
        //    f.planes[2] = planes_xy[32 + 1 + y];
        //    f.planes[3] = -planes_xy[32 + 1 + y + 1];
        //    return f;
        //}

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
            //if (planes_xyz != null)
            //    planes_xyz.Clear();

            ColletSceneLights(scene_constant_data);
            CreateRP5Lights(scene_constant_data);
        }

        public void UploadBuffers(ScriptableRenderContext context)
        {
            directional_lights_buffer.SetData(directional_lights);
            point_lights_buffer.SetData(point_lights);
            spot_lights_buffer.SetData(spot_lights);
            Shader.SetGlobalBuffer("directional_lights", directional_lights_buffer);
            Shader.SetGlobalBuffer("point_lights", point_lights_buffer);
            Shader.SetGlobalBuffer("spot_lights", spot_lights_buffer);

            point_light_aabbs_buffer.SetData(point_light_aabbs);
            spot_light_aabbs_buffer.SetData(spot_light_aabbs);
            Shader.SetGlobalBuffer("point_light_aabbs", point_light_aabbs_buffer);
            Shader.SetGlobalBuffer("spot_light_aabbs", spot_light_aabbs_buffer);

            Shader.SetGlobalBuffer("planes_xyz", world_space_planes_buffer);

            Shader.SetGlobalInteger("num_tiles_x", num_tiles_x);
            Shader.SetGlobalInteger("num_tiles_y", num_tiles_y);
            Shader.SetGlobalInteger("num_tiles_z", num_tiles_z);
            Shader.SetGlobalFloat("z_bias", z_bias);
            Shader.SetGlobalFloat("near_plane", near_plane);
            Shader.SetGlobalFloat("far_plane", far_plane);
            Shader.SetGlobalMatrix("inverse_projection", inverse_projection);
            Shader.SetGlobalMatrix("inverse_view", inverse_view);
            //Shader.SetGlobalTexture("cluster_list", cluster_list);
            Shader.SetGlobalBuffer("item_list", item_list_buffer);
            Shader.SetGlobalBuffer("cluster_list_offset", cluster_list_offset_buffer);

            Shader.SetGlobalFloat("eye_pos_x", eye_pos.x);
            Shader.SetGlobalFloat("eye_pos_y", eye_pos.y);
            Shader.SetGlobalFloat("eye_pos_z", eye_pos.z);
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

                    // world space aabb
                    AABB aabb = new AABB();
                    float offset = sqrt_2 * l.falloff;
                    float3 offset1 = new float3(offset, offset, offset);
                    aabb.rt = l.position + offset1;
                    aabb.ld = l.position - offset1;
                    point_light_aabbs.Add(aabb);
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
                    AABB aabb = new AABB();
                    float offset = sqrt_2 * l.falloff;
                    float3 offset1 = new float3(offset, offset, offset);
                    aabb.rt = l.position + offset1;
                    aabb.ld = l.position - offset1;
                    spot_light_aabbs.Add(aabb);
                }
            }

            scene_constant_data.directional_lights_count = (uint)directional_lights.Count;
            scene_constant_data.point_lights_count = (uint)point_lights.Count;
            scene_constant_data.spot_lights_count = (uint)spot_lights.Count;
        }

        // create more lights
        void CreateRP5Lights(SceneConstants scene_constant_data)
        {

        }
        public RenderTexture cluster_list;
        List<uint> item_list;
        uint cluster_list_offset;
        // TODO(hylu): only rebuild when projectionmatrix/near/far changes
        public void BuildCluster(Camera camera, float t_z_bias = 5.0f, float t_z_end = 500.0f)
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

            for (int z = 0; z <= num_tiles_z; z++)
            {
                float plane_z;
                if (z == 0)
                {
                    plane_z = near_plane;
                }
                else if (z == 1)
                {
                    plane_z = z_bias;
                }
                else
                {
                    plane_z = z_bias * Mathf.Pow(far_plane / z_bias, (float)z / num_tiles_z);
                }
                float4 plane_z1 = inverse_view * new float4(0.0f, 0.0f, 1.0f, -plane_z);

                planes_xyz[num_tiles_x + 1 + num_tiles_y + 1 + z] = plane_z1;

            }



            // build x/y planes

            // left to right
            for (int i = 0; i < num_tiles_x + 1; i++)
            {
                float x = -1.0f + 1.0f / num_tiles_x * i * 2.0f; // ndc space
                float4 clip_x = new float4(x, 0.0f, 0.0f, 1.0f);
                float4 view_x = inverse_projection * clip_x; // clipspace to view space
                view_x /= view_x.w;
                float4 clip_x2 = new float4(x, 1.0f, 1.0f, 1.0f);
                float4 view_x2 = inverse_projection * clip_x2; // clipspace to view space
                view_x2 /= view_x2.w;

                Plane plane = new UnityEngine.Plane(new float3(view_x.x, view_x.y, view_x.z), new float3(view_x2.x, view_x2.y, view_x2.z), new float3(0, 0, 0));
                float3 world_normal = (inverse_view * plane.normal).normalized; // eye is at center
                float distance = -float3.Dot(world_normal, eye_pos);
                planes_xyz[i] = new float4(world_normal.x, world_normal.y, world_normal.z, distance);
            }

            // bottom to up
            for (int j = 0; j < num_tiles_y + 1; j++)
            {
                float y = -1.0f + 1.0f / num_tiles_y * j * 2.0f; // ndc space
                float4 clip_y = new float4(0, y, 0.0f, 1.0f);
                float4 view_y = inverse_projection * clip_y; // clipspace to view space
                view_y /= view_y.w;
                float4 clip_y2 = new float4(1.0f, y, 1.0f, 1.0f);
                float4 view_y2 = inverse_projection * clip_y2; // clipspace to view space
                view_y2 /= view_y2.w;

                Plane plane = new UnityEngine.Plane(new float3(view_y.x, view_y.y, view_y.z), new float3(view_y2.x, view_y2.y, view_y2.z), new float3(0, 0, 0));
                float3 world_normal = (inverse_view * plane.normal).normalized; // eye is at center
                float distance = -float3.Dot(world_normal, eye_pos);
                planes_xyz[num_tiles_x + 1 + j] = new float4(world_normal.x, world_normal.y, world_normal.z, distance);

            }
            int a = 5;
#endif
        }
    }
}
