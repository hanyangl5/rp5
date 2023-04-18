using System;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using static UnityEditor.Timeline.TimelinePlaybackControls;

namespace RP5
{
    using float2 = UnityEngine.Vector2;
    using float3 = UnityEngine.Vector3;
    using float4 = UnityEngine.Vector4;

    public class RP5 : RenderPipeline
    {

        uint width;
        uint height;

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
        // ctor
        public RP5()
        {
            // Refactored to use constants for screen width and height
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;

            depth_rt = new RenderTexture(screenWidth, screenHeight, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
            // base color RGBA8888 [8, 8, 8, 8]
            gbuffer_rt[0] = new RenderTexture(screenWidth, screenHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            // normal [10, 10, 10, 2]
            gbuffer_rt[1] = new RenderTexture(screenWidth, screenHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            // motion vector[16, 16]
            gbuffer_rt[2] = new RenderTexture(screenWidth, screenHeight, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
            // [world pos]
            gbuffer_rt[3] = new RenderTexture(screenWidth, screenHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            // metallic roughness
            gbuffer_rt[4] = new RenderTexture(screenWidth, screenHeight, 0, RenderTextureFormat.RG16, RenderTextureReadWrite.Linear);

            shading_rt = new RenderTexture(screenWidth, screenHeight, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            shading_rt.enableRandomWrite = true;

            // 给纹理 ID 赋值
            for (int i = 0; i < gbuffer_render_target_count; i++)
                gbufferID[i] = gbuffer_rt[i];
            full_screen_cs_thread_group.z = 1;
            Shader.SetGlobalTexture("gdepth", depth_rt);
            for (int i = 0; i < gbuffer_render_target_count; i++)
                Shader.SetGlobalTexture("gbuffer" + i, gbuffer_rt[i]);

            width = (uint)screenWidth;
            height = (uint)screenHeight;
            full_screen_cs_thread_group.x = (int)(width / 8) + 1;
            full_screen_cs_thread_group.y = (int)(height / 8) + 1;


        }



        void LightPass(ScriptableRenderContext context)
        {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "lightpass";
            //Material shading_mat = new Material(Shader.Find("Custuom/shading"));

            //Mesh _fullScreenMesh = CreateFullscreenMesh();
            //cmd.SetRenderTarget(shading_rt);
            //cmd.ClearRenderTarget(true, true, Color.black);
            //cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

            //cmd.DrawMesh(_fullScreenMesh, Matrix4x4.identity, shading_mat, 0, 0);
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
            //CommandBuffer pp_cmd = new CommandBuffer();
            //pp_cmd.name = "post processing";

            //int kernel = post_process.FindKernel("PostProcess_CS");
            //post_process.SetTexture(kernel, "color_tex", shading_rt);
            ////post_process.Dispatch(kernel, full_screen_cs_thread_group.x, full_screen_cs_thread_group.y, full_screen_cs_thread_group.z);
            //pp_cmd.DispatchCompute(post_process, kernel, full_screen_cs_thread_group.x, full_screen_cs_thread_group.y, full_screen_cs_thread_group.z);

            //context.ExecuteCommandBuffer(pp_cmd);

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
            Camera main_cam = cameras[0];
            context.SetupCameraProperties(main_cam);
            camera_pos = main_cam.transform.position;
            light_manager.Setup(context, scene_constants_data);
            
            GeometryPasss(context, main_cam);

            LightPass(context);

            context.DrawGizmos(main_cam, GizmoSubset.PreImageEffects);
            context.DrawGizmos(main_cam, GizmoSubset.PostImageEffects);

            Blit2Screen(context);
            context.Submit();
        }

    }
}

