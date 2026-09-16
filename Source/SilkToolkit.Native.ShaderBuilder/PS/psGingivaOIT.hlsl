#ifndef PSGINGIVAOIT_SHADER_HLSL
#define PSGINGIVAOIT_SHADER_HLSL

#define MESH
#define PBR
#define CLEARCOAT
#define GINGIVA_PIXEL
#include "psCommon.hlsl"
#include "../Common/Gingiva.hlsli"

PSOITOutput gingivaOIT(PS_GINGIVA_IN input, bool isFrontFace : SV_IsFrontFace)
{
    float4 color = ShadeGingiva(input, isFrontFace);
    if (!isFrontFace)
    {
        color.rgb *= 0.08;
        color.a *= 0.10;
    }

    if (color.a <= 0.001)
        discard;

    return calculateOIT(color, input.vEye.w, input.p.z);
}

#endif
