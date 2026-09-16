#ifndef VSCOLLISIONSEGMENTLINE_HLSL
#define VSCOLLISIONSEGMENTLINE_HLSL
#define POINTLINE
#include"..\Common\DataStructs.hlsl"
#include"..\Common\CommonBuffers.hlsl"
#include"..\Common\Common.hlsl"

// Must match ComputeSegment in MeshIntersectionComputeCore and Segment in MeshIntersectionCommon.
// The compute shader appends one element per detected triangle-triangle cut segment.
struct Segment {
    float3 origin;
    float3 end;
    int threadId;
};

// SRV over the compute append buffer. DrawInstancedIndirect renders one instance per appended
// segment, with two vertices per instance.
StructuredBuffer<Segment> sbSegments: register(t0);

GSInputPS main(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    GSInputPS output = (GSInputPS) 0;

    // InstanceId selects the segment. VertexId selects origin/end, producing a LineList pair that
    // can continue through the standard Helix point/line geometry-shader and pixel-shader path.
    Segment segment = sbSegments[instanceId];
    float3 position = vertexId == 0 ? segment.origin : segment.end;

    // Fill the same payload expected by Helix line rendering: world position, eye vector, clip
    // position, and material color from the line material constant buffer.
    output.wp = float4(position, 1.0f);
    float3 vEye = vEyePos - output.wp.xyz;
    output.vEye = float4(normalize(vEye), length(vEye));
    output.p = mul(output.wp, mViewProjection);
    output.c = pColor;
    return output;
}

#endif
