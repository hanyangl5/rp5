using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace RP5
{
    using float3 = UnityEngine.Vector3;

    public class RP5 : RenderPipeline
    {

        int width = 0;
        int height = 0;

        FullScreenCsThreadGroup full_screen_cs_thread_group;

        const uint gbuffer_render_target_count = 5;
        RenderTexture depth_rt;
        RenderTexture[] gbuffer_rt = new RenderTexture[gbuffer_render_target_count];
        RenderTargetIdentifier[] gbufferID = new RenderTargetIdentifier[gbuffer_render_target_count];

        float3 camera_pos;

        // RenderTexture previous_color_tex;
        // RenderTexture ssr_tex;
        // ComputeShader clear_buffer;
        // ComputeShader build_cluster = Resources.Load<ComputeShader>("Shaders/BuildCluster");
        // ComputeShader transform_geometry = Resources.Load<ComputeShader>("Shaders/ObjectTransform");
        // ComputeShader ssr = Resources.Load<ComputeShader>("Shaders/SSR");
        // ComputeShader composite_shading = Resources.Load<ComputeShader>("Shaders/CompositeShading");
        // RenderTargetIdentifier ssr_rti;
        // ComputeShader auto_exposure;

        // ComputeShader bloom_cs;

        ComputeShader opaque_shading = Resources.Load<ComputeShader>("Shaders/Shading");
        RenderTexture shading_rt;

        // TODO single pass post process
        ComputeShader post_process_cs;

        // assets

        SceneConstants scene_constants_data = new SceneConstants();
        //public ComputeBuffer scene_constants_buffer = new ComputeBuffer(1, SceneConstants));

        LightManager light_manager = new LightManager();

        PostProcess post_process_pipeline = new PostProcess();
        // ctor
        public RP5()
        {
            // Refactored to use constants for screen width and height
            width = Screen.width;
            height = Screen.height;

            depth_rt = new RenderTexture(width, height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
            // base color RGBA8888 [8, 8, 8, 8]
            gbuffer_rt[0] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            // normal [10, 10, 10, 2]
            gbuffer_rt[1] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            // motion vector[16, 16]
            gbuffer_rt[2] = new RenderTexture(width, height, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
            // [world pos]
            gbuffer_rt[3] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            // metallic roughness
            gbuffer_rt[4] = new RenderTexture(width, height, 0, RenderTextureFormat.RG16, RenderTextureReadWrite.Linear);

            shading_rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            shading_rt.enableRandomWrite = true;

            for (int i = 0; i < gbuffer_render_target_count; i++)
                gbufferID[i] = gbuffer_rt[i];
            full_screen_cs_thread_group.z = 1;
            Shader.SetGlobalTexture("gdepth", depth_rt);
            for (int i = 0; i < gbuffer_render_target_count; i++)
                Shader.SetGlobalTexture("gbuffer" + i, gbuffer_rt[i]);

            full_screen_cs_thread_group.x = Utils.AlignUp(width, 8);
            full_screen_cs_thread_group.y = Utils.AlignUp(height, 8);
            full_screen_cs_thread_group.z = 1;
        }

        private void RecreateRenderTargets(int newWidth, int newHeight)
        {
            width = newWidth;
            height = newHeight;


            // clean resource
            if (depth_rt != null)
            {
                depth_rt.Release();
            }
            for (int i = 0; i < gbuffer_render_target_count; i++)
            {
                if (gbuffer_rt[i] != null)
                    gbuffer_rt[i].Release();
            }

            if (shading_rt != null)
            {
                shading_rt.Release();
            }

            depth_rt = new RenderTexture(width, height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
            // base color RGBA8888 [8, 8, 8, 8]
            gbuffer_rt[0] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            // normal [10, 10, 10, 2]
            gbuffer_rt[1] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            // motion vector[16, 16]
            gbuffer_rt[2] = new RenderTexture(width, height, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
            // [world pos]
            gbuffer_rt[3] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            // metallic roughness
            gbuffer_rt[4] = new RenderTexture(width, height, 0, RenderTextureFormat.RG16, RenderTextureReadWrite.Linear);

            shading_rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            shading_rt.enableRandomWrite = true;

            Shader.SetGlobalTexture("shading_rt", shading_rt);

            full_screen_cs_thread_group.x = Utils.AlignUp(width, 8);
            full_screen_cs_thread_group.y = Utils.AlignUp(height, 8);
            full_screen_cs_thread_group.z = 1;
        }


        void LightPass(ScriptableRenderContext context)
        {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "lightpass";
            int kernel = opaque_shading.FindKernel("CSMain");
            opaque_shading.SetVector("world_space_camera_pos", camera_pos);
            opaque_shading.SetTexture(kernel, "shading_rt", shading_rt);
            opaque_shading.SetTexture(kernel, "gdepth", depth_rt);
            opaque_shading.SetTexture(kernel, "gbuffer0", gbuffer_rt[0]);
            opaque_shading.SetTexture(kernel, "gbuffer1", gbuffer_rt[1]);
            opaque_shading.SetTexture(kernel, "gbuffer2", gbuffer_rt[2]);
            opaque_shading.SetTexture(kernel, "gbuffer3", gbuffer_rt[3]);
            opaque_shading.SetTexture(kernel, "gbuffer4", gbuffer_rt[4]);
            cmd.DispatchCompute(opaque_shading, kernel, full_screen_cs_thread_group.x, full_screen_cs_thread_group.y, full_screen_cs_thread_group.z);

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

            // Graphics.SetRandomWriteTarget(0, ssr_tex);
            // Graphics.ClearRandomWriteTargets();

            // CommandBuffer cmd = new CommandBuffer();
            // cmd.name = "ssr";

            // int kernel = ssr.FindKernel("SSR_CS");
            // ssr.SetTexture(kernel, "depth_stencil_tex", depth_rt);
            // ssr.SetTexture(kernel, "color_tex", shading_rt);
            // ssr.SetTexture(kernel, "normal_tex", gbuffer_rt[1]);
            // ssr.SetTexture(kernel, "ssr_tex", ssr_tex);
            // ssr.SetTexture(kernel, "world_pos_tex", gbuffer_rt[3]);
            // //ssr.Dispatch(kernel, full_screen_cs_thread_group.x, full_screen_cs_thread_group.y, full_screen_cs_thread_group.z);
            // cmd.DispatchCompute(ssr, kernel, full_screen_cs_thread_group.x, full_screen_cs_thread_group.y, full_screen_cs_thread_group.z);
            // context.ExecuteCommandBuffer(cmd);
        }

        void SSAOPass(ScriptableRenderContext context)
        {

        }


        void CompositeShading(ScriptableRenderContext context)
        {
            //CommandBuffer cmd = new CommandBuffer();
            //cmd.name = "composite shading";

            //int kernel = composite_shading.FindKernel("CompositeShading");
            //composite_shading.SetTexture(kernel, "color_tex", shading_rt);
            //composite_shading.SetTexture(kernel, "ssr_tex", ssr_tex);
            //cmd.DispatchCompute(composite_shading, kernel, full_screen_cs_thread_group.x, full_screen_cs_thread_group.y, full_screen_cs_thread_group.z);
            //context.ExecuteCommandBuffer(cmd);
        }
        void PostProceePass(ScriptableRenderContext context)
        {
            post_process_pipeline.Dispatch(context, shading_rt, full_screen_cs_thread_group.x, full_screen_cs_thread_group.y, full_screen_cs_thread_group.z);
        }

        void AntiAliasing(ScriptableRenderContext context) { }

        void Blit2Screen(ScriptableRenderContext context)
        {
            // blit
            CommandBuffer blit_cmd = new CommandBuffer();
            blit_cmd.name = "blit final color";
            blit_cmd.Blit(shading_rt, BuiltinRenderTextureType.CameraTarget);

            context.ExecuteCommandBuffer(blit_cmd);
        }


        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            if (cameras.Count() > 0)
            {
                var camera = cameras[0];
                context.SetupCameraProperties(camera);
                camera_pos = camera.transform.position;
                light_manager.Setup(context, scene_constants_data);
                Shader.SetGlobalInteger("width", width);
                Shader.SetGlobalInteger("height", height);
                GeometryPasss(context, camera);

                LightPass(context);
                PostProceePass(context);

                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);

                Blit2Screen(context);
                context.Submit();

            }
        }

    }
}

