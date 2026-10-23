#include "../MeshMixerPhong.hlsli"

MeshMixerPhongPixel main(MeshMixerPhongVertex input)
{
    MeshMixerPhongPixel output;
    // Native phong.vert20-45: preserve canonical gl_Vertex for the fragment-space back stripe.
    output.sourcePosition = input.position;
    output.normal = normalize(mul(input.normal, (float3x3)mmNormalMatrix));
    float4 eyePosition = mul(input.position, mmModelView);
    float4 clipPosition = mul(input.position, mul(mmModelView, mmProjection));
    // Explicit OpenGL [-w,w] -> Direct3D [0,w] depth bridge. X/Y/W retain the source projection convention.
    output.position = float4(clipPosition.xy, 0.5f * (clipPosition.z + clipPosition.w), clipPosition.w);
    output.ambientGlobal = mmLightModelAmbient * mmBufferedFrontAmbient;
    float3 auxiliary = (mmLightPositionEye - eyePosition).xyz;
    output.lightDirection = normalize(auxiliary);
    output.distance = length(auxiliary);
    output.halfVector = normalize(mmHalfVectorEye.xyz);
    output.diffuse = input.frontColor * meshMixerLightDiffuse();
    output.backDiffuse = input.backColor * meshMixerLightDiffuse();
    output.ambient = input.frontColor * mmLightAmbient;
    output.frontAlpha = input.frontColor.a;
    return output;
}
