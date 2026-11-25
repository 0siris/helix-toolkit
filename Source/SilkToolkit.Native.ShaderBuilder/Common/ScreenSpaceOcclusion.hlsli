#ifndef SCREEN_SPACE_OCCLUSION_HLSLI
#define SCREEN_SPACE_OCCLUSION_HLSLI

// Reads the renderer's blurred SSAO map at this pixel. The blur ignores depth, so the first pixels of a body next to
// a nearer one take the unoccluded value of the nearer body and show a light rim along the contact. The minimum over
// the pixel and four neighbours two pixels away pulls the occlusion back into that rim.
float SampleScreenSpaceOcclusion(float4 pixelPosition)
{
    float2 uv = pixelPosition.xy * vResolution.zw;
    float2 offset = 2.0 * vResolution.zw;
    float occlusion = texSSAOMap.SampleLevel(samplerSurface, uv, 0).r;
    occlusion = min(occlusion, texSSAOMap.SampleLevel(samplerSurface, uv + float2(offset.x, 0.0), 0).r);
    occlusion = min(occlusion, texSSAOMap.SampleLevel(samplerSurface, uv - float2(offset.x, 0.0), 0).r);
    occlusion = min(occlusion, texSSAOMap.SampleLevel(samplerSurface, uv + float2(0.0, offset.y), 0).r);
    occlusion = min(occlusion, texSSAOMap.SampleLevel(samplerSurface, uv - float2(0.0, offset.y), 0).r);
    return saturate(occlusion);
}

#endif
