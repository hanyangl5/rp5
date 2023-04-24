using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RP5
{

    using float2 = UnityEngine.Vector2;
    public class ScreenSpaceSSS
    {
        public ScreenSpaceSSS()
        {

        }
        ComputeShader sssss = Resources.Load<ComputeShader>("Shaders/SSSSS");
        //public RenderTexture sss_tex;
        float correction = 1.0f;
        float2 step = new float2(0.0f, 0.0f);
        public void Setup(int width, int height)
        {
            // sss_tex = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            // sss_tex.enableRandomWrite = true;
        }

        public void BindResources(RenderTexture depth_tex, RenderTexture history_color, RenderTexture color_tex, RenderTexture material_id_tex)
        {
            int kernel = sssss.FindKernel("SSSSS");
            sssss.SetTexture(kernel, "depth_tex", depth_tex);
            sssss.SetTexture(kernel, "color_tex", history_color);
            sssss.SetTexture(kernel, "sss_tex", color_tex);
            sssss.SetTexture(kernel, "material_id_tex", material_id_tex);
            sssss.SetFloat("correction", correction);
            sssss.SetVector("step", step);
        }

        public void Dispatch(ScriptableRenderContext context, int x, int y, int z)
        {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "screen space subsurface scattering";
            int kernel = sssss.FindKernel("SSSSS");
            // cmd.SetRenderTarget(sss_tex);
            // cmd.ClearRenderTarget(true, true, Color.black);
            cmd.DispatchCompute(sssss, kernel, x, y, z);
            context.ExecuteCommandBuffer(cmd);
        }

    }
}
