#ifndef CSMESHINTERSECTSHAREDRENDERBUFFER_HLSL
#define CSMESHINTERSECTSHAREDRENDERBUFFER_HLSL
#pragma pack_matrix( row_major )

#include "MeshIntersectionCommon.hlsli"

// Compute entry point used by MeshIntersectionComputeCore when both meshes expose the Orca shared
// render-buffer feature. It avoids MeshBufferProxy's triangle-index buffer by reading the render
// index buffer as a flat uint stream, while positions come from the Orca position sidecar SRV.
cbuffer MeshTransforms: register(b0) {
    // Position buffers store mesh-local float4 vertices. Transform to world space here so appended
    // segments can be rendered directly by the collision-line pass.
    matrix mWorldA;
    matrix mWorldB;

    // Number of valid CollisionCandidate elements. The candidate SRV can have larger capacity
    // because MeshIntersectionComputeCore reuses and grows the upload buffer between frames.
    uint candidateCount;
    uint paddingCandidateCount;
    float2 padding;
}

AppendStructuredBuffer<Segment> segmentOutputBuffer: register(u0);

// Binding contract for the shared Orca path:
// t0/t2 = flat render index buffers exposed as typed Buffer<uint> SRVs.
// t1/t3 = compute-readable position sidecar buffers, one float4 per mesh vertex.
// t4    = CPU-filtered triangle-pair candidates.
// u0    = append buffer consumed later by the collision line rendering pass.
Buffer<uint> sbIndicesA: register(t0);
StructuredBuffer<float4> sbPositionsA: register(t1);
Buffer<uint> sbIndicesB: register(t2);
StructuredBuffer<float4> sbPositionsB: register(t3);
StructuredBuffer<CollisionCandidate> sbCandidates: register(t4);

// Flat render indices are stored as three consecutive uints per triangle.
void LoadTriangleA(uint triangleIndex, out float3 p0, out float3 p1, out float3 p2) {
    const uint indexBase = triangleIndex * 3;
    p0 = mul(sbPositionsA[sbIndicesA[indexBase]], mWorldA).xyz;
    p1 = mul(sbPositionsA[sbIndicesA[indexBase + 1]], mWorldA).xyz;
    p2 = mul(sbPositionsA[sbIndicesA[indexBase + 2]], mWorldA).xyz;
}

void LoadTriangleB(uint triangleIndex, out float3 p0, out float3 p1, out float3 p2) {
    const uint indexBase = triangleIndex * 3;
    p0 = mul(sbPositionsB[sbIndicesB[indexBase]], mWorldB).xyz;
    p1 = mul(sbPositionsB[sbIndicesB[indexBase + 1]], mWorldB).xyz;
    p2 = mul(sbPositionsB[sbIndicesB[indexBase + 2]], mWorldB).xyz;
}

[numthreads(512, 1, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID) {
    // Dispatch uses ceil(candidateCount / 512). Guard the unused tail threads in the last group.
    if (dispatchThreadId.x >= candidateCount)
        return;

    // One thread handles exactly one CPU-selected triangle pair.
    const CollisionCandidate candidate = sbCandidates[dispatchThreadId.x];
    float3 u0[3];
    float3 u1[3];
    LoadTriangleA(candidate.triangleA, u0[0], u0[1], u0[2]);
    LoadTriangleB(candidate.triangleB, u1[0], u1[1], u1[2]);

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
