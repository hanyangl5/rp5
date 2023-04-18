#ifndef _POST_PROCESS_HELPER_H_
#define _POST_PROCESS_HELPER_H_

// HDR -> LDR
// Function to perform ACES tonemapping on an HDR image
// x: the input color
float3 _TonemapACES(float3 x)
{
	const float A = 2.51f;
	const float B = 0.03f;
	const float C = 2.43f;
	const float D = 0.59f;
	const float E = 0.14f;
	return (x * (A * x + B)) / (x * (C * x + D) + E);
}

// Function to apply gamma correction to an image
// x: the input color
// gamma: the gamma value to use for correction
float3 GammaCorrection(float3 x, float gamma) {
	return pow(x, (1.0f / gamma));
}

// HDR color grading
// Function to apply color grading to an HDR image using a 3D texture lookup table
// x: the input color
// lut: the 3D texture lookup table
// linear_sampler: the sampler state to use for linear sampling
// lut_size: the size of the lookup table
float3 _ColorGrading(float3 x, Texture3D<float4> lut, SamplerState sampler, uint lut_size) {
	float3 uvw = x * ((lut_size - 1) / lut_size) + (0.5f / lut_size); // calculate the UVW coordinates for the lookup table
	float3 result = lut.SampleLevel(sampler, uvw, 0).rgb; // sample the lookup table and return the RGB value
	return result;
}

// Function to add film grain effect to an image
float3 _FilmGrain(float3 color, float grain_strength, float grain_size, float2 uv, float2 random_offset)
{
    // Generate random value for grain
    float noise = frac(sin(dot(uv + random_offset, float2(12.9898, 78.233))) * 43758.5453);
    
    // Calculate grain color
    float3 grain_color = float3(noise, noise, noise);
    grain_color *= grain_strength * (1.0 - smoothstep(0.0, 1.0, grain_size));
    
    // Add grain to original color
    return color + grain_color;
}

// Function to apply vignette effect to an image
// x: the input color
// vignette_strength: the strength of the vignette effect
// vignette_size: the size of the vignette effect
// Function to apply vignette effect to an image
// x: the input color
// vignette_strength: the strength of the vignette effect
// vignette_size: the size of the vignette effect
float3 _Vignette(float3 x, float vignette_strength, float vignette_size)
{
    // Calculate the UV coordinates
    float2 uv = x.xy;
    // Calculate the center of the image
    float2 center = float2(0.5, 0.5);
    // Calculate the distance from the center to the current pixel
    float distance = length(uv - center);
    // Calculate the vignette value using smoothstep
    float vignette = smoothstep(vignette_size, 0.0, distance);
    // Apply the vignette effect to the input color
    return x * (1.0 - vignette_strength * vignette);
} 

// Function to apply depth of field effect to an image
// x: the input color
// focus_distance: the distance to the focal point
// focus_range: the range of distances in focus
// aperture: the size of the aperture
float3 _DepthOfField(float3 x, float focus_distance, float focus_range, float aperture)
{
    // Calculate the UV coordinates
    float2 uv = x.xy;
    // Calculate the center of the image
    float2 center = float2(0.5, 0.5);
    // Calculate the distance from the center to the current pixel
    float distance = length(uv - center);
    // Calculate the depth of field value using smoothstep
    float dof = smoothstep(focus_distance - focus_range, focus_distance + focus_range, distance);
    // Calculate the blur amount based on the distance from the focal point and the size of the aperture
    float blur_amount = (distance - focus_distance) / aperture;
    // Apply the depth of field effect to the input color
    return lerp(x, blur_amount * x, dof);
} 


// Function to apply motion blur effect to an image
// x: the input color
// velocity: the velocity vector for motion blur
// sample_count: the number of samples to take for motion blur
float3 _MotionBlur(float3 x, float2 velocity, int sample_count, Texture2D<float4> color_tex, SamplerState sampler)
{
    // Calculate the UV coordinates
    float2 uv = x.xy;
    // Calculate the step size for each sample
    float2 step_size = velocity / sample_count;
    // Initialize the result color
    float3 result = float3(0.0, 0.0, 0.0);
    // Loop over the samples and accumulate the color
    for (int i = 0; i < sample_count; i++)
    {
        // Calculate the current sample position
        float2 sample_pos = uv - (i + 0.5) * step_size;
        // Sample the color at the current position
        float3 sample_color = color_tex.SampleLevel(sampler, sample_pos, 0).rgb;
        // Accumulate the color
        result += sample_color;
    }
    // Normalize the accumulated color by the number of samples
    result /= sample_count;
    // Return the motion blurred color
    return result;
}

#endif // _POST_PROCESS_HELPER_H_


