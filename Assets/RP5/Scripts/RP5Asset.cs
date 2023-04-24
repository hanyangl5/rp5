using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RP5
{
    [CreateAssetMenu(menuName = "Rendering/RP5Asset")]
    public class RP5Asset : RenderPipelineAsset
    {
        protected override RenderPipeline CreatePipeline()
        {
            return new RP5();
        }
    }
}

