#include "../MeshMixerPhong.hlsli"

// Native frag_util.frag74-84: evaluate the stripe after interpolation, in input scene coordinates.
float4 meshMixerBackColor(float4 backDiffuse, float4 sourcePosition)
{
    float x = sourcePosition.x + sourcePosition.y + sourcePosition.z;
    float t = 0.5f * (1.0f + sin(x * 100.0f));
    t = 0.75f + 0.25f * t;
    float4 color = (1.0f - t) * float4(1, 1, 1, 1) + t * backDiffuse;
    color.a = 1.0f;
    return color;
}

float4 main(MeshMixerPhongPixel input, bool isFrontFace : SV_IsFrontFace) : SV_TARGET
{
    // Native phong.frag24-46: normalize the normal, reverse it ONCE on the back, then normalize the VS light varying.
    float3 normal = normalize(input.normal);
    float4 diffuse = input.diffuse;
    if (!isFrontFace)
    {
        normal = -normal;
        diffuse = meshMixerBackColor(input.backDiffuse, input.sourcePosition);
    }
    float3 light = normalize(input.lightDirection);

    // Native standard_lightmodel88-109. Ambient is front-derived even on a backface; specular is front-only.
    float4 color = input.ambientGlobal;
    float normalDotLight = dot(normal, light);
    if (normalDotLight <= 0.0f)
    {
        color.a = input.frontAlpha;
        return color;
    }
    float attenuation = 1.0f / (mmAttenuation.x + mmAttenuation.y * input.distance
        + mmAttenuation.z * input.distance * input.distance);
    color += attenuation * (diffuse * normalDotLight + input.ambient);
    float normalDotHalf = max(dot(normal, input.halfVector), 0.0f);
    if (isFrontFace)
        color += attenuation * meshMixerMaterialSpecular() * meshMixerLightSpecular()
            * pow(normalDotHalf, mmShininess);
    // Native phong.frag56-59: front alpha controls BOTH sides. No saturation, gamma or premultiplication is added.
    color.a = input.frontAlpha;
    return color;
}
