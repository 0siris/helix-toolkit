#ifndef PSTOOTHENAMEL_SHADER_HLSL
#define PSTOOTHENAMEL_SHADER_HLSL

#define MESH
#define PBR
#define CLEARCOAT
#define TOOTH_ENAMEL_PIXEL
#include "psCommon.hlsl"
#include "../Common/ToothEnamel.hlsli"

float4 main(PS_TOOTH_ENAMEL_IN input, bool isFrontFace : SV_IsFrontFace) : SV_TARGET
{
    return ShadeToothEnamel(input, isFrontFace);
}

#endif
