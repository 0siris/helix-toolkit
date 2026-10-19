#ifndef PSPARTICLE_HLSL
#define PSPARTICLE_HLSL
#define PARTICLE
#include"..\Common\CommonBuffers.hlsl"
#include"..\Common\DataStructs.hlsl"


float4 main(in ParticlePS_INPUT input) : SV_Target {
    float4 color = input.color * input.opacity;
    // The profile blends straight shader color once; opacity contributes to coverage only.
    if (IsPremultipliedOutput()) color = float4(input.color.rgb, input.color.a * input.opacity);
    if (bHasTexture) {
        color *= texParticle.Sample(samplerParticle, input.texcoords);
    }
    return color;
}

#endif
