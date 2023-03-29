using UnityEditor;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Rendering;
using UnityEngine.VFX;

public class RenderPipeline5 : RenderPipeline
{
    const uint render_target_count = 5;
    RenderTexture depth_rt;
    RenderTexture[] gbuffer_rt = new RenderTexture[render_target_count];
    RenderTargetIdentifier[] gbufferID = new RenderTargetIdentifier[render_target_count];
    RenderTexture shading_rt;

    // ctor
    public RenderPipeline5()
    {
        depth_rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        // base color RGBA8888 [8, 8, 8, 8]
        gbuffer_rt[0] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        // normal [10, 10, 10, 2]
        gbuffer_rt[1] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        // motion vector[16, 16]
        gbuffer_rt[2] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RG32, RenderTextureReadWrite.Linear);
        // emissive, [metallic16 roughness16] [32, 32, 32, 32]
        gbuffer_rt[3] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);
        // metallic roughness
        gbuffer_rt[4] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RG16, RenderTextureReadWrite.Linear);
        shading_rt = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        // 给纹理 ID 赋值
        for (int i = 0; i < render_target_count; i++)
            gbufferID[i] = gbuffer_rt[i];

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

    void LightPass(ScriptableRenderContext context, Camera camera)
    {
        //// 使用 Blit
        //CommandBuffer cmd = new CommandBuffer();
        //cmd.name = "lightpass";

        //Material mat = new Material(Shader.Find("Custuom/shading"));
        //cmd.SetRenderTarget(shading_rt);
        //context.ExecuteCommandBuffer(cmd);

        //CommandBuffer cmd2 = new CommandBuffer();

        //cmd2.Blit(shading_rt, BuiltinRenderTextureType.CameraTarget, mat);
        //context.ExecuteCommandBuffer(cmd2);


        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "lightpass";
        Material _lightPassMat = new Material(Shader.Find("Custuom/shading"));
        

        Mesh _fullScreenMesh = CreateFullscreenMesh();
        cmd.SetRenderTarget(shading_rt);
        cmd.ClearRenderTarget(true, true, Color.black);
        cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        cmd.DrawMesh(_fullScreenMesh, Matrix4x4.identity, _lightPassMat, 0, 0);
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


        // 提交绘制命令
        //context.Submit();


    }
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        Camera main_cam = cameras[0];
        context.SetupCameraProperties(main_cam);

        Shader.SetGlobalTexture("g_depth", depth_rt);
        for (int i = 0; i < render_target_count; i++)
            Shader.SetGlobalTexture("gbuffer_" + i, gbuffer_rt[i]);

        GeometryPasss(context, main_cam);

        LightPass(context, main_cam);


        // skybox and Gizmos
        //context.DrawSkybox(main_cam);
        //if (Handles.ShouldRenderGizmos())
        //{
        //    context.DrawGizmos(main_cam, GizmoSubset.PreImageEffects);
        //    context.DrawGizmos(main_cam, GizmoSubset.PostImageEffects);
        //}

        context.Submit();
    }
}
