using System.Linq;
using Unity.Mathematics;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace RP5
{
    using float3 = UnityEngine.Vector3;
    using float2 = UnityEngine.Vector2;
    using static UnityEditor.Timeline.TimelinePlaybackControls;

    public class RP5 : RenderPipeline
    {

        int width = 0;
        int height = 0;

        FullScreenCsThreadGroup tg;

        const uint gbuffer_render_target_count = 6;
        RenderTexture depth_rt;
        RenderTexture[] gbuffer_rt = new RenderTexture[gbuffer_render_target_count];
        RenderTargetIdentifier[] gbufferID = new RenderTargetIdentifier[gbuffer_render_target_count];

        float3 camera_pos;

        Matrix4x4 view_projection_prev;
        Matrix4x4 view_projection;
        float2 jitter_offset_prev = new float2(0.0f, 0.0f);
        float2 jitter_offset = new float2(0.0f, 0.0f);

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

        // constant resources
        RenderTexture history_color;
        RenderTexture history_depth;
        //

        LightManager light_manager = new LightManager();

        TAA taa_pass = new TAA();

        MotionVector mv_pass = new MotionVector();
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
            // motion vector
            gbuffer_rt[2] = new RenderTexture(width, height, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
            // [world pos]
            gbuffer_rt[3] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            // metallic roughness
            gbuffer_rt[4] = new RenderTexture(width, height, 0, RenderTextureFormat.RG16, RenderTextureReadWrite.Linear);

            // tangent, anisotropy
            gbuffer_rt[5] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

            shading_rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            shading_rt.enableRandomWrite = true;

            history_color = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            history_depth = new RenderTexture(width, height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);

            for (int i = 0; i < gbuffer_render_target_count; i++)
                gbufferID[i] = gbuffer_rt[i];
            tg.z = 1;
            Shader.SetGlobalTexture("gdepth", depth_rt);
            for (int i = 0; i < gbuffer_render_target_count; i++)
                Shader.SetGlobalTexture("gbuffer" + i, gbuffer_rt[i]);

            tg.x = Utils.AlignUp(width, 8);
            tg.y = Utils.AlignUp(height, 8);
            tg.z = 1;

            mv_pass.SetUp(width, height);
        }

        // private void RecreateRenderTargets(int newWidth, int newHeight)
        // {
        //     width = newWidth;
        //     height = newHeight;


        //     // clean resource
        //     if (depth_rt != null)
        //     {
        //         depth_rt.Release();
        //     }
        //     for (int i = 0; i < gbuffer_render_target_count; i++)
        //     {
        //         if (gbuffer_rt[i] != null)
        //             gbuffer_rt[i].Release();
        //     }

        //     if (shading_rt != null)
        //     {
        //         shading_rt.Release();
        //     }

        //     depth_rt = new RenderTexture(width, height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        //     //depth_rt.depthStencilFormat = GraphicsFormat.D32_SFloat_S8_UInt;
        //     // base color RGBA8888 [8, 8, 8, 8]
        //     gbuffer_rt[0] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        //     // normal [10, 10, 10, 2]
        //     gbuffer_rt[1] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        //     // motion vector[16, 16]
        //     gbuffer_rt[2] = new RenderTexture(width, height, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
        //     // [world pos]
        //     gbuffer_rt[3] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        //     // metallic roughness
        //     gbuffer_rt[4] = new RenderTexture(width, height, 0, RenderTextureFormat.RG16, RenderTextureReadWrite.Linear);

        //     shading_rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        //     shading_rt.enableRandomWrite = true;

        //     Shader.SetGlobalTexture("shading_rt", shading_rt);

        //     tg.x = Utils.AlignUp(width, 8);
        //     tg.y = Utils.AlignUp(height, 8);
        //     tg.z = 1;
        // }


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
            opaque_shading.SetTexture(kernel, "gbuffer5", gbuffer_rt[5]);
            cmd.DispatchCompute(opaque_shading, kernel, tg.x, tg.y, tg.z);

            context.ExecuteCommandBuffer(cmd);
        }

        // Geometry pass function
        void GeometryPasss(ScriptableRenderContext context, Camera camera)
        {
            camera.TryGetCullingParameters(out var cullingParameters);
            var culling_result = context.Cull(ref cullingParameters);

            // config settings
            ShaderTagId shaderTagId = new ShaderTagId("OpaqueGeometry");   // 使用 LightMode 为 gbuffer 的 shader
            SortingSettings sortingSettings = new SortingSettings(camera);
            DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;
            // Set viewprojection and jitter offsets

            // Calculate view projection matrix
            Matrix4x4 view = camera.worldToCameraMatrix;
            Matrix4x4 projection = GL.GetGPUProjectionMatrix(camera.nonJitteredProjectionMatrix, true);
            float2 jitter_offset = taa_pass.GetJitterOffset()  - new float2(0.5f, 0.5f); // [-1, 1]

            jitter_offset.x = (jitter_offset.x) / width * 2;;
            jitter_offset.y = (jitter_offset.y) / height * 2;;
            //jitter_offset = jitter_offset_prev = new float2(0.0f, 0.0f);
            // offset the projection matrix// divide resolution, offset inside one pixel [-0.5, 0.5]
            projection[0, 2] += (jitter_offset.x);
            projection[1, 2] += (jitter_offset.y);

            jitter_offset = jitter_offset_prev = new float2(0.0f, 0.0f);

            Matrix4x4 view_projection = projection * view;
            //camera.previousViewProjectionMatrix;
            Shader.SetGlobalMatrix("view_projection_prev", view_projection_prev);
            Shader.SetGlobalMatrix("view_projection", view_projection);
            Shader.SetGlobalVector("jitter_offset_prev", jitter_offset_prev);
            Shader.SetGlobalVector("jitter_offset", jitter_offset);
            Shader.SetGlobalMatrix("inverse_view_projection", view_projection.inverse);
            view_projection_prev = view_projection;
            jitter_offset_prev = jitter_offset;

            // Draw the renderers
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


        void MotionVectorPass(ScriptableRenderContext context)
        {
            mv_pass.BindResources(depth_rt);
            mv_pass.Dispatch(context, tg.x, tg.y, tg.z);
        }

        void PostProceePass(ScriptableRenderContext context)
        {
            post_process_pipeline.Dispatch(context, shading_rt, tg.x, tg.y, tg.z);
        }

        void AntiAliasing(ScriptableRenderContext context) {
            //taa_pass.BindResources(shading_rt, history_color, mv_pass.mv_tex);
            //taa_pass.Dispatch(context, tg.x, tg.y, tg.z);
        }

        void Blit2Screen(ScriptableRenderContext context)
        {
            // blit
            CommandBuffer blit_cmd = new CommandBuffer();
            blit_cmd.name = "blit final color";
            blit_cmd.Blit(shading_rt, BuiltinRenderTextureType.CameraTarget);

            context.ExecuteCommandBuffer(blit_cmd);
        }


        private void Setup(ScriptableRenderContext context)
        {

            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "clearBuffer";
            cmd.SetRenderTarget(gbufferID, depth_rt);
            cmd.ClearRenderTarget(true, true, Color.clear);
            context.ExecuteCommandBuffer(cmd); // submit to gpu 
            cmd.SetRenderTarget(shading_rt);
            cmd.ClearRenderTarget(true, true, Color.black);
            Shader.SetGlobalInteger("width", width);
            Shader.SetGlobalInteger("height", height);

        }

        //ComputeShader build_hzb = Resources.Load<ComputeShader>("Shaders/BuildHzb");
        private void Misc(ScriptableRenderContext context) {
            CommandBuffer cmd = new CommandBuffer();
            cmd.Blit(shading_rt, history_color);
            context.ExecuteCommandBuffer(cmd);
        }

        ComputeShader bloom = Resources.Load<ComputeShader>("Shaders/Bloom");
        private void Bloom(ScriptableRenderContext context)
        {
            CommandBuffer cmd = new CommandBuffer();
            int kernel = bloom.FindKernel("ApplyBloom");
            bloom.SetTexture(kernel, "Result", shading_rt);
            cmd.DispatchCompute(bloom, kernel, tg.x, tg.y, tg.z);
            context.ExecuteCommandBuffer(cmd);
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {

            if (cameras.Count() > 0)
            {
                if (cameras[0].cameraType == CameraType.SceneView)
                {
                    var camera = cameras[0];
                    context.SetupCameraProperties(camera);
                    camera_pos = camera.transform.position;
                    Setup(context);
                    light_manager.Setup(context, scene_constants_data);

                    GeometryPasss(context, camera);

                    LightPass(context);

                    //Bloom(context);

                    // copy shading result before postprocessing for some effects using history color buffer. TAA/SSR
                    {
                        CommandBuffer cmd = new CommandBuffer();
                        cmd.CopyTexture(shading_rt, history_color);
                        //cmd.Blit(shading_rt, history_color);
                        context.ExecuteCommandBuffer(cmd);
                    }

                    AntiAliasing(context);

                    //PostProceePass(context);

                    //Misc(context);

                    context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                    context.DrawGizmos(camera, GizmoSubset.PostImageEffects);


                    Blit2Screen(context);
                    context.Submit();

                }
            }
        }

    }
}

