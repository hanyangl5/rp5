using System;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;
using UnityEngine.VFX;

namespace RP5
{

    public class RenderPipeline5 : RenderPipeline
    {

        uint full_width;
        uint full_height;

        struct FullScreenCsThreadGroup
        {

            public int x, y, z;
        };

        FullScreenCsThreadGroup full_screen_cs_thread_group;

        const uint render_target_count = 5;
        RenderTexture depth_rt;
        RenderTexture[] gbuffer_rt = new RenderTexture[render_target_count];
        RenderTargetIdentifier[] gbufferID = new RenderTargetIdentifier[render_target_count];
        RenderTexture shading_rt;
        public ComputeShader post_process;
        ComputeShader auto_exposure;

        ComputeShader bloom_cs;

        ComputeShader dof;

        // post processing pass

        //ComputeShader tone_mapping_cs; // hdr to ldr
        //ComputeShader color_grading_cs; // custuom color correction
        //ComputeShader film_grain_cs; // grain
        //ComputeShader gamma_cs; // gamm setting

        // TODO single pass post process
        // ComputeShader all_in_one_post_process_cs;

        // assets

        SceneConstants scene_constants_data = new SceneConstants();
        //public ComputeBuffer scene_constants_buffer = new ComputeBuffer(1, SceneConstants));

        Lighting lighting = new Lighting();
        // ctor
        public RenderPipeline5()
        {
            depth_rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
            // base color RGBA8888 [8, 8, 8, 8]
            gbuffer_rt[0] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            // normal [10, 10, 10, 2]
            gbuffer_rt[1] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            // motion vector[16, 16]
            gbuffer_rt[2] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
            // [world pos]
            gbuffer_rt[3] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            // metallic roughness
            gbuffer_rt[4] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RG16, RenderTextureReadWrite.Linear);

            shading_rt = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            shading_rt.enableRandomWrite = true;
            // 给纹理 ID 赋值
            for (int i = 0; i < render_target_count; i++)
                gbufferID[i] = gbuffer_rt[i];


            full_width = (uint)Screen.width;
            full_height = (uint)Screen.height;
            full_screen_cs_thread_group.x = (int)(full_width / 8) + 1;
            full_screen_cs_thread_group.y = (int)(full_height / 8) + 1;
            full_screen_cs_thread_group.z = 1;

            Shader.SetGlobalTexture("g_depth", depth_rt);
            for (int i = 0; i < render_target_count; i++)
                Shader.SetGlobalTexture("gbuffer_" + i, gbuffer_rt[i]);
        }

        public static Mesh CreateFullscreenMesh()
        {
            Vector3[] positions =
            {
                new Vector3(-1.0f,  -1.0f, 0.0f),
                new Vector3(1.0f, -1.0f, 0.0f),
                new Vector3(-1.0f,  1.0f, 0.0f),
                new Vector3(1.0f, 1.0f, 0.0f),
            };
            Vector2[] uvs = {
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(0,1),
                new Vector2(1,1),
            };
            int[] indices = { 0, 2, 1, 1, 2, 3 };
            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt16;
            mesh.vertices = positions;
            mesh.triangles = indices;
            mesh.uv = uvs;
            return mesh;
        }

        void LightPass(ScriptableRenderContext context)
        {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "lightpass";
            Material shading_mat = new Material(Shader.Find("Custuom/shading"));

            Mesh _fullScreenMesh = CreateFullscreenMesh();
            cmd.SetRenderTarget(shading_rt);
            cmd.ClearRenderTarget(true, true, Color.black);
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

            // Console.Write("a");
            // shading_mat.SetVector("light_direction", Vector4.one);
            // shading_mat.SetVector("light_color",Vector4.one );
            // shading_mat.SetFloat("light_intensity", 10.0f);

            cmd.DrawMesh(_fullScreenMesh, Matrix4x4.identity, shading_mat, 0, 0);
            context.ExecuteCommandBuffer(cmd);
        }


        void GeometryPasss(ScriptableRenderContext context, Camera camera)
        {

            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "clearBuffer";
            cmd.SetRenderTarget(gbufferID, depth_rt);
            cmd.ClearRenderTarget(true, true, Color.black);
            context.ExecuteCommandBuffer(cmd); // submit to gpu 
            cmd.Clear();

            camera.TryGetCullingParameters(out var cullingParameters);
            var culling_result = context.Cull(ref cullingParameters);

            // config settings
            ShaderTagId shaderTagId = new ShaderTagId("geometry");   // 使用 LightMode 为 gbuffer 的 shader
            SortingSettings sortingSettings = new SortingSettings(camera);
            DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;

            // 绘制
            context.DrawRenderers(culling_result, ref drawingSettings, ref filteringSettings);

        }

        void ShadowPass(ScriptableRenderContext context)
        {

        }

        void SSRPass(ScriptableRenderContext context)
        {

        }

        void SSAOPass(ScriptableRenderContext context)
        {

        }

        void PostProceePass(ScriptableRenderContext context)
        {
            CommandBuffer pp_cmd = new CommandBuffer();
            pp_cmd.name = "post processing";
            post_process = Resources.Load<ComputeShader>("Shaders/post_process");

            int kernel = post_process.FindKernel("PostProcess_CS");
            post_process.SetTexture(kernel, "color_tex", shading_rt);
            //post_process.Dispatch(kernel, full_screen_cs_thread_group.x, full_screen_cs_thread_group.y, full_screen_cs_thread_group.z);
            pp_cmd.DispatchCompute(post_process, kernel, full_screen_cs_thread_group.x, full_screen_cs_thread_group.y, full_screen_cs_thread_group.z);

            context.ExecuteCommandBuffer(pp_cmd);

        }

        void AntiAliasing(ScriptableRenderContext context) { }

        void Blit2Screen(ScriptableRenderContext context)
        {
            // blit
            CommandBuffer blit_cmd = new CommandBuffer();
            blit_cmd.name = "blit to screen";
            blit_cmd.Blit(shading_rt, BuiltinRenderTextureType.CameraTarget);
            context.ExecuteCommandBuffer(blit_cmd);
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            Camera main_cam = cameras[0];
            context.SetupCameraProperties(main_cam);



            lighting.Setup(context, scene_constants_data);

            lighting.UploadBuffers(context);

            Shader.SetGlobalInteger("directional_light_count",(int)scene_constants_data.directional_lights_count);
            Shader.SetGlobalInteger("point_light_count",(int)scene_constants_data.point_lights_count);
            Shader.SetGlobalInteger("spot_light_count",(int)scene_constants_data.spot_lights_count);
            //Shader.SetGlobalConstantBuffer("SceneConstants", scene_constants_buffer, 0, 0);

            ShadowPass(context);

            GeometryPasss(context, main_cam);

            LightPass(context);

            // SSAOPass(context); need ambient light

            SSRPass(context);

            PostProceePass(context);

            Blit2Screen(context);

            context.Submit();
        }
    }
}
