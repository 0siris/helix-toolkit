#ifndef PSGINGIVA_SHADER_HLSL
#define PSGINGIVA_SHADER_HLSL

#define MESH
#define PBR
#define CLEARCOAT
#define GINGIVA_PIXEL
#include "psCommon.hlsl"
#include "../Common/Gingiva.hlsli"

float4 main(PS_GINGIVA_IN input, bool isFrontFace : SV_IsFrontFace) : SV_TARGET
{
    return ShadeGingiva(input, isFrontFace);
}

#endif
