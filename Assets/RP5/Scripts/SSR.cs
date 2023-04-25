using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;

namespace RP5
{
    public class SSR
    {
        RenderTexture ssr_tex;
        ComputeShader ssr = Resources.Load<ComputeShader>("Shaders/SSR");
        int kernel;
        public void Setup(int width, int height) 
        {
            kernel = ssr.FindKernel("SSR_CS");
            ssr_tex = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            ssr_tex.enableRandomWrite = true;
        }
        
        public void BindResources(RenderTexture depth_rt, RenderTexture history_color, RenderTexture normal) {
            ssr.SetTexture(kernel, "depth_stencil_tex", depth_rt);
            ssr.SetTexture(kernel, "color_tex", history_color);
            ssr.SetTexture(kernel, "normal_tex", normal);
            ssr.SetTexture(kernel, "ssr_tex", ssr_tex);
        }

        public void Dispatch(ScriptableRenderContext context, int x, int y, int z) {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "ssr";
            cmd.DispatchCompute(ssr, kernel, x,y,z);
            context.ExecuteCommandBuffer(cmd);
        }
    }

}