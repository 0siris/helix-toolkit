#ifndef PSTOOTHENAMELOIT_SHADER_HLSL
#define PSTOOTHENAMELOIT_SHADER_HLSL

#define MESH
#define PBR
#define CLEARCOAT
#define TOOTH_ENAMEL_PIXEL
#include "psCommon.hlsl"
#include "../Common/ToothEnamel.hlsli"

PSOITOutput toothEnamelOIT(PS_TOOTH_ENAMEL_IN input, bool isFrontFace : SV_IsFrontFace)
{
    float4 color = ShadeToothEnamel(input, isFrontFace);
    if (!isFrontFace)
    {
        color.rgb *= 0.05;
        color.a *= 0.04;
    }

    if (color.a <= 0.001)
        discard;

    return calculateOIT(color, input.vEye.w, input.p.z);
}

#endif
