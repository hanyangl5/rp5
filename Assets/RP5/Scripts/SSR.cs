using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;

namespace RP5
{
    using float3 = UnityEngine.Vector3;
    using float2 = UnityEngine.Vector2;
    public class SSR
    {
        public RenderTexture ssr_tex;
        ComputeShader ssr = Resources.Load<ComputeShader>("Shaders/SSR");
        int kernel;


        public void Setup(int width, int height)
        {
            kernel = ssr.FindKernel("SSR_CS");
            ssr_tex = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            ssr_tex.enableRandomWrite = true;
        }

        public void BindResources(RenderTexture depth_rt, RenderTexture history_color, RenderTexture normal, SRPSetting setting)
        {

            ssr.SetTexture(kernel, "color_tex", history_color);
            ssr.SetTexture(kernel, "normal_tex", normal);
            ssr.SetTexture(kernel, "ssr_tex", ssr_tex);

            ssr.SetFloat("cb_zThickness", setting.cb_zThickness);
            ssr.SetFloat("cb_stride", setting.cb_stride);
            ssr.SetFloat("cb_maxSteps", setting.cb_maxSteps);
            ssr.SetFloat("cb_strideZCutoff", setting.cb_strideZCutoff);
            ssr.SetFloat("cb_maxDistance", setting.cb_maxDistance);
            ssr.SetFloat("cb_numMips", setting.cb_numMips);
            ssr.SetFloat("cb_fadeStart", setting.cb_fadeStart);
            ssr.SetFloat("cb_fadeEnd", setting.cb_fadeEnd);

        }

        public void Dispatch(ScriptableRenderContext context, int x, int y, int z)
        {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "ssr";
            cmd.DispatchCompute(ssr, kernel, x, y, z);
            context.ExecuteCommandBuffer(cmd);
        }
    }

}