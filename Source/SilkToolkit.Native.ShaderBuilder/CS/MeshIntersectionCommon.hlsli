#ifndef MESHINTERSECTIONCOMMON_HLSLI
#define MESHINTERSECTIONCOMMON_HLSLI

// Output element appended by the compute shaders and consumed by the collision-line render pass.
// Coordinates are already in world space when written.
struct Segment {
    float3 origin;
    float3 end;
    int threadId;
};

// CPU broadphase/KD-tree filtering creates one candidate per potentially intersecting triangle
// pair. The fourth int keeps the structured-buffer element aligned with the C# layout.
struct CollisionCandidate {
    int triangleA;
    int triangleB;
    int pairId;
    int padding;
};

// Shared numeric tolerances for the GPU line path. The shader intentionally emits only stable
// transverse cut segments; degenerate triangles, near-coplanar triangles, and point-touch cases are
// filtered out instead of producing unstable zero-length lines.
static const float MeshIntersectEpsilon = 1e-5f;
static const float MeshIntersectEpsilonSq = MeshIntersectEpsilon * MeshIntersectEpsilon;
static const float MinSegmentLengthSq = MeshIntersectEpsilonSq;

// Intersect triangle V with the plane of triangle U.
// Returns the two points where V crosses U's plane. This is only a half-test: callers still need
// to verify that U also crosses V's plane and that both plane-cut segments overlap.
bool Intersects(const float3 U[3], const float3 V[3], out float3 segment0, out float3 segment1)
{
    segment0 = float3(0.0f, 0.0f, 0.0f);
    segment1 = float3(0.0f, 0.0f, 0.0f);

    // Build and normalize the plane normal of U. A zero-area triangle cannot contribute a stable
    // intersection segment.
    float3 edge1 = U[1] - U[0];
    float3 edge2 = U[2] - U[0];
    float3 normalCross = cross(edge1, edge2);
    float normalLengthSq = dot(normalCross, normalCross);
    if (normalLengthSq <= MeshIntersectEpsilonSq)
        return false;

    float3 normal = normalCross * rsqrt(normalLengthSq);

    // Signed distances from V's vertices to U's plane. Vertices inside the epsilon band are snapped
    // to the plane to keep the edge-case branches deterministic.
    float d[3];
    int positive = 0, negative = 0;

    [unroll]
    for (int i = 0; i < 3; ++i) {
        d[i] = dot(normal, V[i] - U[0]);
        if (d[i] > MeshIntersectEpsilon) {
            ++positive;
        }
        else if (d[i] < -MeshIntersectEpsilon) {
            ++negative;
        }
        else {
            d[i] = 0.0f;
        }
    }

    // No strict sign change means V is fully on one side, coplanar, or just touching the plane.
    // The GPU line path skips those cases.
    if (positive <= 0 || negative <= 0)
        return false;

    // Two vertices on one side and one on the other: interpolate along the two crossing edges.
    if (positive == 2) {
        if (d[0] < -MeshIntersectEpsilon) {
            segment0 = (d[1] * V[0] - d[0] * V[1]) / (d[1] - d[0]);
            segment1 = (d[2] * V[0] - d[0] * V[2]) / (d[2] - d[0]);
        }
        else if (d[1] < -MeshIntersectEpsilon) {
            segment0 = (d[0] * V[1] - d[1] * V[0]) / (d[0] - d[1]);
            segment1 = (d[2] * V[1] - d[1] * V[2]) / (d[2] - d[1]);
        }
        else {
            segment0 = (d[0] * V[2] - d[2] * V[0]) / (d[0] - d[2]);
            segment1 = (d[1] * V[2] - d[2] * V[1]) / (d[1] - d[2]);
        }
    }
    // Same topology as above with the signs inverted.
    else if (negative == 2) {
        if (d[0] > MeshIntersectEpsilon) {
            segment0 = (d[1] * V[0] - d[0] * V[1]) / (d[1] - d[0]);
            segment1 = (d[2] * V[0] - d[0] * V[2]) / (d[2] - d[0]);
        }
        else if (d[1] > MeshIntersectEpsilon) {
            segment0 = (d[0] * V[1] - d[1] * V[0]) / (d[0] - d[1]);
            segment1 = (d[2] * V[1] - d[1] * V[2]) / (d[2] - d[1]);
        }
        else {
            segment0 = (d[0] * V[2] - d[2] * V[0]) / (d[0] - d[2]);
            segment1 = (d[1] * V[2] - d[2] * V[1]) / (d[1] - d[2]);
        }
    }
    // One vertex lies on the plane and the opposite edge crosses it.
    else {
        if (abs(d[0]) <= MeshIntersectEpsilon) {
            segment0 = V[0];
            segment1 = (d[2] * V[1] - d[1] * V[2]) / (d[2] - d[1]);
        }
        else if (abs(d[1]) <= MeshIntersectEpsilon) {
            segment0 = V[1];
            segment1 = (d[0] * V[2] - d[2] * V[0]) / (d[0] - d[2]);
        }
        else {
            segment0 = V[2];
            segment1 = (d[1] * V[0] - d[0] * V[1]) / (d[1] - d[0]);
        }
    }

    return true;
}

// Full triangle-triangle intersection for non-coplanar cuts.
// Each triangle cuts the other's plane into a segment. Those two segments should lie on the same
// 3D line; the actual triangle-triangle intersection is their 1D interval overlap on that line.
bool TrianglesIntersect(const float3 U[3], const float3 V[3], out float3 segment0, out float3 segment1)
{
    segment0 = float3(0.0f, 0.0f, 0.0f);
    segment1 = float3(0.0f, 0.0f, 0.0f);

    float3 s0 = float3(0.0f, 0.0f, 0.0f);
    float3 s1 = float3(0.0f, 0.0f, 0.0f);
    float3 t0 = float3(0.0f, 0.0f, 0.0f);
    float3 t1 = float3(0.0f, 0.0f, 0.0f);
    // s = U clipped by V's plane, t = V clipped by U's plane.
    bool intersectsS = Intersects(V, U, s0, s1);
    bool intersectsT = Intersects(U, V, t0, t1);
    if (!intersectsS || !intersectsT)
        return false;

    // Direction of the shared intersection line is cross(normal(U), normal(V)).
    float3 uNormal = cross(U[1] - U[0], U[2] - U[0]);
    float3 vNormal = cross(V[1] - V[0], V[2] - V[0]);
    float uNormalLengthSq = dot(uNormal, uNormal);
    float vNormalLengthSq = dot(vNormal, vNormal);
    if (uNormalLengthSq <= MeshIntersectEpsilonSq || vNormalLengthSq <= MeshIntersectEpsilonSq)
        return false;

    float3 directionCross = cross(uNormal * rsqrt(uNormalLengthSq), vNormal * rsqrt(vNormalLengthSq));
    float directionLengthSq = dot(directionCross, directionCross);
    if (directionLengthSq <= MeshIntersectEpsilonSq)
        return false;

    // Use the average of all clipped endpoints as a stable origin for projecting both segments
    // onto the same 1D coordinate axis.
    float3 lineOrigin = 0.25f * (s0 + s1 + t0 + t1);
    float3 direction = directionCross * rsqrt(directionLengthSq);

    // Project both clipped segments onto the shared line and test interval overlap.
    float sParam0 = dot(direction, s0 - lineOrigin);
    float sParam1 = dot(direction, s1 - lineOrigin);
    float tParam0 = dot(direction, t0 - lineOrigin);
    float tParam1 = dot(direction, t1 - lineOrigin);
    float sMin = min(sParam0, sParam1);
    float sMax = max(sParam0, sParam1);
    float tMin = min(tParam0, tParam1);
    float tMax = max(tParam0, tParam1);
    if (sMax <= tMin + MeshIntersectEpsilon || sMin + MeshIntersectEpsilon >= tMax)
        return false;

    // Convert the overlapping interval back to world-space endpoints.
    float3 p0 = lineOrigin + max(sMin, tMin) * direction;
    float3 p1 = lineOrigin + min(sMax, tMax) * direction;
    if (dot(p1 - p0, p1 - p0) <= MinSegmentLengthSq)
        return false;

    segment0 = p0;
    segment1 = p1;
    return true;
}

#endif
