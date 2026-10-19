//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------
#include"..\Common\CommonBuffers.hlsl"

float4 main(ScreenDupVS_INPUT input) : SV_Target {
    float4 color = texDiffuseMap.Sample(samplerSurface, input.Tex);
    // Desktop duplication alpha does not describe window/scene coverage.
    if (IsPremultipliedOutput() && (OITWeightMode & 0x20000000) != 0) color.a = 1;
    return color;
}
