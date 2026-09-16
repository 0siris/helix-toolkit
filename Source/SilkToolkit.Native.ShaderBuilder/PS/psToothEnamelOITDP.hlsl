#ifndef PSTOOTHENAMELOITDP_SHADER_HLSL
#define PSTOOTHENAMELOITDP_SHADER_HLSL

#define MESH
#define PBR
#define CLEARCOAT
#define TOOTH_ENAMEL_PIXEL

#include "psOITDepthPeelingCommon.hlsl"
#include "../Common/ToothEnamel.hlsli"

DDPOutputMRT toothEnamelOITDP(PS_TOOTH_ENAMEL_IN input, bool isFrontFace : SV_IsFrontFace)
{
    float4 color = ShadeToothEnamel(input, isFrontFace);
    if (!isFrontFace)
    {
        color.rgb *= 0.05;
        color.a *= 0.04;
    }

    if (color.a <= 0.001)
        discard;

    return depthPeelPS(input.p, color);
}

#endif
