using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class SRPSetting : MonoBehaviour
{
    public bool enable_ssr = true;
    public bool enable_taa = true;
    public bool enable_post_processsing = true;

    // ssr settings

    public float cb_zThickness = 1.0f; // thickness to ascribe to each pixel in the depth buffer
                                       //public float cb_nearPlaneZ; // the camera's near z plane
    public float cb_stride = 1.0f; // Step in horizontal or vertical pixels between samples. This is a float
                                   // because integer math is slow on GPUs, but should be set to an integer >= 1.
    public float cb_maxSteps = 100.0f; // Maximum number of iterations. Higher gives better images but may be slow.
    public float cb_maxDistance = 100.0f; // Maximum camera-space distance to trace before returning a miss.
    public float cb_strideZCutoff = 1.0f; // More distant pixels are smaller in screen space. This value tells at what point to
                                          // start relaxing the stride to give higher quality reflections for objects far from
                                          // the camera.
    public float cb_numMips = 1.0f; // the number of mip levels in the convolved color buffer
    public float cb_fadeStart = 1.0f; // determines where to start screen edge fading of effect
    public float cb_fadeEnd = 100.0f; // determines where to end screen edge fading of effect

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }
}
