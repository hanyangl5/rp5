using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RP5
{
    
    using float2 = UnityEngine.Vector2;
    public class TAA
    {
        public TAA() {
            
        }

        public float2 GetJitterOffset() {
            float2 result = halton_samples[idx];
            idx = (idx + 1) % halton_samples.Length;
            return result;
        }
        int idx = 0;
        public float2[] halton_samples = new float2[] {new float2(0.500000f, 0.333333f), new float2(0.250000f, 0.666667f), new float2(0.750000f, 0.111111f),
                            new float2(0.125000f, 0.444444f), new float2(0.625000f, 0.777778f), new float2(0.375000f, 0.222222f),
                            new float2(0.875000f, 0.555556f), new float2(0.062500f, 0.888889f), new float2(0.562500f, 0.037037f),
                            new float2(0.312500f, 0.370370f), new float2(0.812500f, 0.703704f), new float2(0.187500f, 0.148148f),
                            new float2(0.687500f, 0.481481f), new float2(0.437500f, 0.814815f), new float2(0.937500f, 0.259259f),
                            new float2(0.031250f, 0.592593f)};

    }
}
