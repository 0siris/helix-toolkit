#ifndef CSMESHINTERSECT_HLSL
#define CSMESHINTERSECT_HLSL
#pragma pack_matrix( row_major )

#include "MeshIntersectionCommon.hlsli"

// Compute entry point used by MeshIntersectionComputeCore for the generic mesh-buffer path.
// The CPU broadphase has already selected triangle pairs; this shader only validates those
// candidates and appends one world-space segment for every transverse triangle intersection.
cbuffer MeshTransforms: register(b0) {
    // Mesh-local vertices are transformed to world space before testing so the output segment can
    // be rendered directly by the collision-line draw path.
    matrix mWorldA;
    matrix mWorldB;

    // Number of valid CollisionCandidate elements in sbCandidates. The candidate buffer may be
    // larger because it is reused between frames and only grows when capacity is insufficient.
    uint candidateCount;
    uint paddingCandidateCount;
    float2 padding;
}

// MeshBufferProxy stores one structured element per triangle. The fourth int keeps the layout
// 16-byte aligned and matches the C# Int4 upload format.
struct TriangleIndices {
    int a;
    int b;
    int c;
    int padding;
};

AppendStructuredBuffer<Segment> segmentOutputBuffer: register(u0);

// Binding contract:
// t0/t1 = mesh A triangle-index and position buffers.
// t2/t3 = mesh B triangle-index and position buffers.
// t4    = CPU-filtered triangle-pair candidates.
// u0    = append buffer consumed later by the collision line rendering pass.
StructuredBuffer<TriangleIndices> sbIndicesA: register(t0);
StructuredBuffer<float4> sbPositionsA: register(t1);
StructuredBuffer<TriangleIndices> sbIndicesB: register(t2);
StructuredBuffer<float4> sbPositionsB: register(t3);
StructuredBuffer<CollisionCandidate> sbCandidates: register(t4);

[numthreads(512, 1, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID) {
    // Dispatch uses ceil(candidateCount / 512). Guard the unused tail threads in the last group.
    if (dispatchThreadId.x >= candidateCount)
        return;

    // One thread handles exactly one CPU-selected triangle pair.
    const CollisionCandidate candidate = sbCandidates[dispatchThreadId.x];

    const TriangleIndices triA = sbIndicesA[candidate.triangleA];
    const float3 u0[3] = {
        mul(sbPositionsA[triA.a], mWorldA).xyz,
        mul(sbPositionsA[triA.b], mWorldA).xyz,
        mul(sbPositionsA[triA.c], mWorldA).xyz,
    };

    const TriangleIndices triB = sbIndicesB[candidate.triangleB];
    const float3 u1[3] = {
        mul(sbPositionsB[triB.a], mWorldB).xyz,
        mul(sbPositionsB[triB.b], mWorldB).xyz,
        mul(sbPositionsB[triB.c], mWorldB).xyz,
    };

    float3 intersectOrigin;
    float3 intersectEnd;
    if (TrianglesIntersect(u0, u1, intersectOrigin, intersectEnd)) {
        // pairId is propagated for diagnostics/correlation; normal rendering uses origin/end only.
        Segment segment;
        segment.origin = intersectOrigin;
        segment.end = intersectEnd;
        segment.threadId = candidate.pairId;
        segmentOutputBuffer.Append(segment);
    }
}

#endif
