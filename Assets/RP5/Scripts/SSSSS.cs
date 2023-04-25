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
        public RenderTexture sss_tex;
        float correction = 0.2f;
        float2[] step = new float2[4] { new float2(1.0f, 0.0f), new float2(-1.0f, 0.0f), new float2(0.0f, 1.0f), new float2(0.0f, -1.0f) };
        
        
        //float2[] step = new float2[4] { new float2(1.0f, 1.0f), new float2(1.0f, 1.0f), new float2(1.0f, 1.0f), new float2(1.0f, 1.0f) };
        // step = sssStrength * gaussianWidth * pixelSize * dir
        public void Setup(int width, int height)
        {
            sss_tex = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            sss_tex.enableRandomWrite = true;
        }

        public void BindResources(RenderTexture depth_tex, RenderTexture history_color, RenderTexture color_tex, RenderTexture material_id_tex)
        {

            int kernel = sssss.FindKernel("SSSSS");
            sssss.SetTexture(kernel, "depth_tex", depth_tex);
            sssss.SetTexture(kernel, "color_tex", history_color);
            sssss.SetTexture(kernel, "sss_tex", sss_tex);
            sssss.SetTexture(kernel, "material_id_tex", material_id_tex);
            sssss.SetFloat("correction", correction);
        }

        public void CompositeSSS(RenderTexture color_tex, ScriptableRenderContext context, int x, int y, int z)
        {
            int kernel = sssss.FindKernel("Composite");
            sssss.SetTexture(kernel, "dst_tex", color_tex);
            sssss.SetTexture(kernel, "src_tex", sss_tex);

            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "SSS Composite";
            cmd.DispatchCompute(sssss, kernel, x, y, z);

            context.ExecuteCommandBuffer(cmd);
        }

        public void Blur()
        {
            //multipass pass blur
            //for (int k = 1; k <= blurIterations; k++)
            //{
            //    buffer.SetGlobalFloat("_BlurStr", Mathf.Clamp01(scatterDistance * 0.08f - k * 0.022f * scatterDistance));
            //    buffer.SetGlobalVector("_BlurVec", new Vector4(1, 0, 0, 0));
            //    buffer.Blit(blurRT2, blurRT1, material, 0);
            //    buffer.SetGlobalVector("_BlurVec", new Vector4(0, 1, 0, 0));
            //    buffer.Blit(blurRT1, blurRT2, material, 0);

            //    buffer.SetGlobalVector("_BlurVec", new Vector4(1, 1, 0, 0).normalized);
            //    buffer.Blit(blurRT2, blurRT1, material, 0);
            //    buffer.SetGlobalVector("_BlurVec", new Vector4(-1, 1, 0, 0).normalized);
            //    buffer.Blit(blurRT1, blurRT2, material, 0);
            //}
        }
        public void Dispatch(ScriptableRenderContext context, int x, int y, int z)
        {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "screen space subsurface scattering";
            int kernel = sssss.FindKernel("SSSSS");
            cmd.SetRenderTarget(sss_tex);
            cmd.ClearRenderTarget(true, true, Color.clear);
            for (int i = 0; i < 4; i++)
            {
                cmd.DispatchCompute(sssss, kernel, x, y, z);
                sssss.SetVector("step", step[i]);
            }
            context.ExecuteCommandBuffer(cmd);
        }

    }
}
