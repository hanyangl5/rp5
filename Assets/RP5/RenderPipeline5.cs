using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

public class RenderPipeline5 : RenderPipeline
{
    const uint render_target_count = 5;
    RenderTexture depth_rt;
    RenderTexture[] color_rts = new RenderTexture[render_target_count];
    RenderTargetIdentifier[] gbufferID = new RenderTargetIdentifier[render_target_count];


    // ctor
    public RenderPipeline5()
    {
        depth_rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        // base color RGBA8888 [8, 8, 8, 8]
        color_rts[0] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        // normal [10, 10, 10, 2]
        color_rts[1] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        // motion vector[16, 16]
        color_rts[2] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RG32, RenderTextureReadWrite.Linear);
        // emissive, [metallic16 roughness16] [32, 32, 32, 32]
        color_rts[3] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);

        color_rts[4] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RG16, RenderTextureReadWrite.Linear);
        // 给纹理 ID 赋值
        for (int i = 0; i < render_target_count; i++)
            gbufferID[i] = color_rts[i];

    }

    void LightPass(ScriptableRenderContext context, Camera camera)
    {
        // 使用 Blit
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "lightpass";

        Material mat = new Material(Shader.Find("Custuom/shading"));
        cmd.Blit(gbufferID[0], BuiltinRenderTextureType.CameraTarget, mat);
        context.ExecuteCommandBuffer(cmd);
        context.Submit();
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


        // 提交绘制命令
        context.Submit();


    }
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        Camera main_cam = cameras[0];
        context.SetupCameraProperties(main_cam);

        Shader.SetGlobalTexture("_gdepth", depth_rt);
        for (int i = 0; i < render_target_count; i++)
            Shader.SetGlobalTexture("_GT" + i, color_rts[i]);

        GeometryPasss(context, main_cam);

        LightPass(context, main_cam);


        // skybox and Gizmos
        //context.DrawSkybox(main_cam);
        //if (Handles.ShouldRenderGizmos())
        //{
        //    context.DrawGizmos(main_cam, GizmoSubset.PreImageEffects);
        //    context.DrawGizmos(main_cam, GizmoSubset.PostImageEffects);
        //}

        //context.Submit();
    }
}
