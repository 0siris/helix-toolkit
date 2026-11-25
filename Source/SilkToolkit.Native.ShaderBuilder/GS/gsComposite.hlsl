#ifndef GSCOMPOSITE_SHADER_HLSL
#define GSCOMPOSITE_SHADER_HLSL

#define MESH
#include "../Commons.hlsli"
#include "../Common/ToothEnamel.hlsli"

// Low-poly appliances share vertex normals across hard edges. Per triangle, a vertex normal that leaves the face by
// more than about 30 degrees belongs to a crease and is replaced by the face normal; gentle bevels keep their smooth
// normals. Deciding per triangle avoids the contour lines a per-pixel threshold draws along the triangulation.
static const float CompositeCreaseCosine = 0.866;

[maxvertexcount(3)]
void main(triangle PS_TOOTH_ENAMEL_IN input[3], inout TriangleStream<PS_TOOTH_ENAMEL_IN> outStream)
{
    float3 faceNormal = cross(input[1].posWS - input[0].posWS, input[2].posWS - input[0].posWS);
    float faceLength = length(faceNormal);
    faceNormal = faceLength > 1e-12 ? faceNormal / faceLength : normalize(input[0].n);
    if (dot(faceNormal, input[0].n + input[1].n + input[2].n) < 0.0)
        faceNormal = -faceNormal;

    [unroll]
    for (int i = 0; i < 3; i++)
    {
        PS_TOOTH_ENAMEL_IN output = input[i];
        float3 vertexNormal = normalize(input[i].n);
        output.n = dot(vertexNormal, faceNormal) < CompositeCreaseCosine ? faceNormal : vertexNormal;
        outStream.Append(output);
    }

    outStream.RestartStrip();
}

#endif
