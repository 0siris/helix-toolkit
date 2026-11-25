#ifndef VSCOMPOSITE_SHADER_HLSL
#define VSCOMPOSITE_SHADER_HLSL

#define MESH
#include "../Commons.hlsli"
#include "../Common/ToothEnamel.hlsli"
#pragma pack_matrix(row_major)

// Standard mesh vertices for appliance composite: no curvature or shading-hint stream is required.
PS_TOOTH_ENAMEL_IN main(VSInput input)
{
    PS_TOOTH_ENAMEL_IN output = (PS_TOOTH_ENAMEL_IN)0;

    float4 inputp = input.p;
    float3 inputn = input.n;

    if (bInvertNormal)
        inputn = -inputn;

    if (bHasInstances)
    {
        matrix mInstance =
        {
            input.mr0,
            input.mr1,
            input.mr2,
            input.mr3
        };
        inputp = mul(input.p, mInstance);
        inputn = mul(inputn, (float3x3)mInstance);
    }

    output.posOS = inputp.xyz;
    float4 worldPosition = mul(inputp, mWorld);
    float3 vEye = vEyePos - worldPosition.xyz;
    output.vEye = float4(normalize(vEye), length(vEye));
    output.posWS = worldPosition.xyz;
    output.wp = worldPosition;
    output.p = mul(worldPosition, mViewProjection);
    output.n = normalize(mul(inputn, (float3x3)mWorld));
    output.uv = input.t;
    output.curv = input.c;

    return output;
}

#endif
