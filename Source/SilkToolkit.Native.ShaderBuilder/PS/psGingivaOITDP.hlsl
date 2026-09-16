#ifndef PSGINGIVAOITDP_SHADER_HLSL
#define PSGINGIVAOITDP_SHADER_HLSL

#define MESH
#define PBR
#define CLEARCOAT
#define GINGIVA_PIXEL

#include "psOITDepthPeelingCommon.hlsl"
#include "../Common/Gingiva.hlsli"

DDPOutputMRT gingivaOITDP(PS_GINGIVA_IN input, bool isFrontFace : SV_IsFrontFace)
{
    float4 color = ShadeGingiva(input, isFrontFace);
    if (!isFrontFace)
    {
        color.rgb *= 0.08;
        color.a *= 0.10;
    }

    if (color.a <= 0.001)
        discard;

    return depthPeelPS(input.p, color);
}

#endif
