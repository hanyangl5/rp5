using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.VisualScripting;
using System.Text;
using System.Collections;
using System;
using JetBrains.Annotations;

namespace RP5
{
    // TODO(hylu): global using in c#? 

    using float2 = UnityEngine.Vector2;
    using float3 = UnityEngine.Vector3;
    using float4 = UnityEngine.Vector4;
    // kernels
    public class PostProcess
    {
        public int motion_blur_sample_count = 8;
        public float vignette_strength = 0.0f;
        public float vignette_size = 0.8f;
        public float focus_distance = 1.0f;
        public float focus_range = 10.0f;
        public float aperture = 8.6f;
        public float grain_strength = 0.0f;
        //public float grain_size = 540.0f;

        uint lut_size = 32;

        public PostProcess()
        {
            tone_mapping = post_process.FindKernel("ToneMapping");

            color_grading = post_process.FindKernel("ColorGrading");

            motion_blur = post_process.FindKernel("MotionBlur");

            vignette = post_process.FindKernel("Vignette");

            depth_of_field = post_process.FindKernel("DepthOfField");

            film_grain = post_process.FindKernel("FilmGrain");

            // TODO(hyl5): load from file or create by curve

            // default color grading lut
            color_grading_lut = new Texture3D((int)lut_size, (int)lut_size, (int)lut_size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[lut_size * lut_size * lut_size];

            for (int i = 0; i < lut_size; i++)
            {
                for (int j = 0; j < lut_size; j++)
                {
                    for (int k = 0; k < lut_size; k++)
                    {
                        float denom = (float)lut_size - 1.0f;
                        var color = new Color((float)i / denom, (float)j / denom, (float)k / denom);
                        // Set the color value into the colors array
                        colors[k * lut_size * lut_size + j * lut_size + i] = color;
                    }
                }
            }
            color_grading_lut.SetPixels(colors);
            color_grading_lut.Apply();

        }
        public void Dispatch(ScriptableRenderContext context, RenderTexture color_tex, int x, int y, int z)
        {
            //post_process.SetTexture()
            // Set values for shader variables
            post_process.SetFloat("motion_blur_sample_count", motion_blur_sample_count);
            post_process.SetFloat("vignette_strength", vignette_strength);
            post_process.SetFloat("vignette_size", vignette_size);
            post_process.SetFloat("focus_distance", focus_distance);
            post_process.SetFloat("focus_range", focus_range);
            post_process.SetFloat("aperture", aperture);
            post_process.SetFloat("grain_strength", grain_strength);
            //post_process.SetFloat("grain_size", grain_size);
            post_process.SetTexture(color_grading, "color_grading_lut", color_grading_lut);
            post_process.SetTexture(depth_of_field, "color_tex", color_tex);
            post_process.SetTexture(motion_blur, "color_tex", color_tex);
            post_process.SetTexture(vignette, "color_tex", color_tex);
            post_process.SetTexture(film_grain, "color_tex", color_tex);
            post_process.SetTexture(tone_mapping, "color_tex", color_tex);
            post_process.SetTexture(color_grading, "color_tex", color_tex);
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "post process";
            //cmd.DispatchCompute(post_process, depth_of_field, x, y, z);
            //cmd.DispatchCompute(post_process, motion_blur, x, y, z);
            cmd.DispatchCompute(post_process, vignette, x, y, z);
            cmd.DispatchCompute(post_process, film_grain, x, y, z);
            cmd.DispatchCompute(post_process, tone_mapping, x, y, z);
            cmd.DispatchCompute(post_process, color_grading, x, y, z);
            context.ExecuteCommandBuffer(cmd);

        }

        ComputeShader post_process = Resources.Load<ComputeShader>("Shaders/PostProcess");

        int tone_mapping;
        int color_grading;
        int motion_blur;
        int vignette;
        int depth_of_field;
        int film_grain;
        Texture3D color_grading_lut;

//Texture3D<float4> color_grading_lut;

    }
}