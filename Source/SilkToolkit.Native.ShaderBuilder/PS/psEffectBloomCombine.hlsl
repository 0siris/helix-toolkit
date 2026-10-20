#define BORDEREFFECTS
#include"..\Common\CommonBuffers.hlsl"
#pragma pack_matrix( row_major )


// Pixel shader: bloom (combine)
float4 AdjustSaturation(float4 color, float saturation) {
    float3 grayscale = float3(0.2125f, 0.7154f, 0.0721f);
    float gray = dot(color.rgb, grayscale);
    return lerp(gray, color, saturation);
}

float4 main(MeshOutlinePS_INPUT pin) : SV_Target {
    // Uses sampleWeights[0].x as base saturation, sampleWeights[0].y as bloom saturation
    float4 bloom = texDiffuseMap.Sample(samplerSurface, pin.Tex);
    if (IsPremultipliedOutput()) {
        float gray = dot(bloom.rgb, float3(0.2125, 0.7154, 0.0721));
        float3 glow = saturate(lerp(gray, bloom.rgb, Param._m02) * Param._m03);
        return float4(glow, max(glow.r, max(glow.g, glow.b)));
    }
    bloom = AdjustSaturation(bloom, Param._m02) * Param._m03;
    return bloom;
}
