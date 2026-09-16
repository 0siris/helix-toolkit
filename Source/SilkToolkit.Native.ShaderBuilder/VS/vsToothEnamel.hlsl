#ifndef VSTOOTHENAMEL_SHADER_HLSL
#define VSTOOTHENAMEL_SHADER_HLSL

#define MESH
#include "../Commons.hlsli"
#include "../Common/ToothEnamel.hlsli"
#pragma pack_matrix(row_major)

PS_TOOTH_ENAMEL_IN main(VSEInput input)
{
    PS_TOOTH_ENAMEL_IN output = (PS_TOOTH_ENAMEL_IN)0;

    float4 inputp = input.p;
    float3 inputn = input.n;
    float3 inputt1 = input.t1;
    float3 inputt2 = input.t2;

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
        if (bHasNormalMap && !bAutoTengent)
        {
            inputt1 = mul(inputt1, (float3x3)mInstance);
            inputt2 = mul(inputt2, (float3x3)mInstance);
        }
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
    output.curv = input.curv;
    output.hints = input.hints;

    return output;
}

#endif
