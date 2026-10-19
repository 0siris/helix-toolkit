#define BORDEREFFECTS
#include"..\Common\CommonBuffers.hlsl"
#pragma pack_matrix( row_major )

// Pixel shader: bloom (extract)
float4 psBloomExtract(MeshOutlinePS_INPUT pin) : SV_Target0 {
    float4 c = texDiffuseMap.Sample(samplerSurface, pin.Tex);
    if (IsPremultipliedOutput()) {
        float3 straight = c.a > 0 ? c.rgb / c.a : 0;
        float3 glow = saturate((straight - Color.rgb) / max(1 - Color.rgb, 1e-5) * Param._m00) * c.a;
        return float4(glow, max(glow.r, max(glow.g, glow.b)));
    }
    return saturate((c - Color) / (1 - Color) * Param._m00); //Color as threshold
}
