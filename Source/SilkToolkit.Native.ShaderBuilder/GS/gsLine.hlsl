#ifndef GSLINE_HLSL
#define GSLINE_HLSL
#define POINTLINE
#include"..\Common\DataStructs.hlsl"
#include"..\Common\Common.hlsl"
//--------------------------------------------------------------------------------------
// Make a a ribbon line of the specified pixel width from 2 points in the projection frame.
//--------------------------------------------------------------------------------------
/// <summary>Builds the existing fixed-size ribbon, optionally rejecting an invalid projected direction.</summary>
/// <param name="points">Receives the four existing ribbon corners on success.</param>
/// <param name="posA">The first clip-space endpoint.</param>
/// <param name="posB">The second clip-space endpoint.</param>
/// <param name="width">The existing half-width before DPI scaling.</param>
/// <param name="rejectInvalidDirection">Whether the ordinary line path must reject zero or nonfinite direction.</param>
/// <returns>False only when the opted-in projected direction cannot be normalized.</returns>
bool makeLine(out float4 points[4], in float4 posA, in float4 posB, in float width,
              in bool rejectInvalidDirection) {
    // Clipping
    if (posA.w * posB.w < 0) {
        if (posA.w < 0) {
            posA = lerp(posA, posB, -posA.w / (posB.w - posA.w));
        } else {
            posB = lerp(posB, posA, -posB.w / (posA.w - posB.w));
        }
    }
    // Bring A and B in window space
    float2 Aw = projToWindow(posA);
    float2 Bw = projToWindow(posB);

    // Compute tangent and binormal of line AB in window space
    // Binormal is scaled by line width 
    float2 delta = Bw.xy - Aw.xy;
    float directionSquared = dot(delta, delta);
    if (rejectInvalidDirection && (!(directionSquared > 0) || !isfinite(directionSquared))) {
        points[0] = points[1] = points[2] = points[3] = (float4)0;
        return false;
    }
    float2 tangent = normalize(delta);
    float2 binormal = width * DpiScale * float2(tangent.y, -tangent.x);

    // Compute the corners of the ribbon in window space
    float2 A1w = Aw - binormal;
    float2 A2w = Aw + binormal;
    float2 B1w = Bw - binormal;
    float2 B2w = Bw + binormal;

    // bring back corners in projection frame
    points[1] = windowToProj(A1w, posA.z, posA.w);
    points[0] = windowToProj(A2w, posA.z, posA.w);
    points[3] = windowToProj(B1w, posB.z, posB.w);
    points[2] = windowToProj(B2w, posB.z, posB.w);
    return true;
}

/// <summary>Preserves the existing unguarded four-argument entry point used by arrow shader includes.</summary>
/// <param name="points">Receives the existing ribbon corners.</param>
/// <param name="posA">The first clip-space endpoint.</param>
/// <param name="posB">The second clip-space endpoint.</param>
/// <param name="width">The existing half-width before DPI scaling.</param>
void makeLine(out float4 points[4], in float4 posA, in float4 posB, in float width) {
    makeLine(points, posA, posB, width, false);
}

void makeLineNonFixed(out float4 points[4], in float4 posA, in float4 posB, in float width) {
    // Bring A and B in window space
    float2 Aw = posA.xy;
    float2 Bw = posB.xy;

    // Compute tangent and binormal of line AB in window space
    // Binormal is scaled by line width 
    float2 tangent = normalize(Bw.xy - Aw.xy);
    float2 binormal = width * float2(tangent.y, -tangent.x);
    float2 a1w = (Aw - binormal);
    float2 a2w = (Aw + binormal);
    float2 b1w = (Bw - binormal);
    float2 b2w = (Bw + binormal);
    // Compute the corners of the ribbon in window space
    float4 A1w = float4(a1w, posA.z, posA.w);
    float4 A2w = float4(a2w, posA.z, posA.w);
    float4 B1w = float4(b1w, posB.z, posB.w);
    float4 B2w = float4(b2w, posB.z, posB.w);

    // bring back corners in projection frame
    points[1] = mul(A1w, mProjection);
    points[0] = mul(A2w, mProjection);
    points[3] = mul(B1w, mProjection);
    points[2] = mul(B2w, mProjection);
}

/// <summary>Emits the existing line ribbon, rejecting invalid projected direction only for fixed-size lines.</summary>
/// <param name="input">The existing pair of vertex-shader outputs.</param>
/// <param name="outStream">Receives the existing triangle ribbon on an admitted direction.</param>
[maxvertexcount(4)]
void main(line GSInputPS input[2], inout TriangleStream<PSInputPS> outStream) {
    PSInputPS output = (PSInputPS)0;

    float4 lineCorners[4] = {(float4)0, (float4)0, (float4)0, (float4)0};
    float texX = length(input[0].wp.xyz - input[1].wp.xyz) / max(1e-5, pTextureScale);
    if (fixedSize) {
        if (!makeLine(lineCorners, input[0].p, input[1].p, pfParams.x, true)) return;
    } else makeLineNonFixed(lineCorners, mul(input[0].wp, mView), mul(input[1].wp, mView), pfParams.x);
    output.vEye = input[0].vEye;
    output.c = input[0].c;
    output.p = lineCorners[0];
    output.t = float3(1, 1, 1);
    output.tex = float3(texX, 1, 1);
    outStream.Append(output);

    output.p = lineCorners[1];
    output.c = input[0].c;
    output.t = float3(1, -1, 1);
    output.tex = float3(texX, 0, 1);
    outStream.Append(output);

    output.vEye = input[1].vEye;
    output.c = input[1].c;
    output.p = lineCorners[2];
    output.t = float3(-1, 1, 1);
    output.tex = float3(0, 1, 1);
    outStream.Append(output);

    output.p = lineCorners[3];
    output.t = float3(-1, -1, 1);
    output.tex = float3(0, 0, 1);
    outStream.Append(output);

    outStream.RestartStrip();
}
#endif
