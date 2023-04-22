using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RP5
{

    using float2 = UnityEngine.Vector2;
    public class MotionVector
    {
        public MotionVector()
        {

        }

        ComputeShader motion_vector = Resources.Load<ComputeShader>("Shaders/MotionVector");
        public RenderTexture mv_tex;

        public void SetUp(int width, int height)
        {
            mv_tex = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            mv_tex.enableRandomWrite = true;
        }

        public void BindResources(RenderTexture depth_tex)
        {
            int kernel = motion_vector.FindKernel("MotionVector");
            motion_vector.SetTexture(kernel, "depth_tex", depth_tex);
            motion_vector.SetTexture(kernel, "mv_tex", mv_tex);
        }

        public void Dispatch(ScriptableRenderContext context, int x, int y, int z)
        {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "motion vector";
            int kernel = motion_vector.FindKernel("MotionVector");
            cmd.SetRenderTarget(mv_tex);
            cmd.ClearRenderTarget(true, true, Color.black);
            cmd.DispatchCompute(motion_vector, kernel, x, y, z);
            context.ExecuteCommandBuffer(cmd);
        }

    }
}
