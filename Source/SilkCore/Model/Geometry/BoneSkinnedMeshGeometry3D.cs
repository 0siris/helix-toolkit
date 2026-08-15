/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Model.Animations;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Geometry;

public class BoneSkinnedMeshGeometry3D : MeshGeometry3D {
    public BoneSkinnedMeshGeometry3D() { }

    public BoneSkinnedMeshGeometry3D(MeshGeometry3D mesh) {
        mesh.AssignTo(this);
    }

    /// <summary>
    ///     Gets or sets the vertex bone ids and bone weights.
    /// </summary>
    /// <value>
    ///     The vertex bone ids.
    /// </value>
    public IList<BoneIds>? VertexBoneIds {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Creates the node based bone matrices.
    /// </summary>
    /// <param name="bones">The bones.</param>
    /// <param name="rootInvTransform">The root inv transform.</param>
    /// <returns></returns>
    public static Matrix[] CreateNodeBasedBoneMatrices(IList<Bone> bones, ref Matrix rootInvTransform) {
        var m = new Matrix[bones.Count];
        CreateNodeBasedBoneMatrices(bones, ref rootInvTransform, ref m);
        return m;
    }

    /// <summary>
    ///     Creates the node based bone matrices.
    /// </summary>
    /// <param name="bones">The bones.</param>
    /// <param name="matrices"></param>
    /// <param name="rootInvTransform"></param>
    /// <returns></returns>
    public static void CreateNodeBasedBoneMatrices(
        IList<Bone> bones,
        ref Matrix rootInvTransform,
        ref Matrix[] matrices
    ) {
        for (var i = 0; i < bones.Count; ++i)
            if (bones[i].Node is { } node)
                matrices[i] = bones[i].InvBindPose * node.TotalModelMatrixInternal * rootInvTransform;
            else
                matrices[i] = Matrix.Identity;
    }


    public static BoneSkinnedMeshGeometry3D CreateSkeletonMesh(IList<Bone> bones, float scale = 1f) {
        var builder = new MeshBuilder(true, false);
        builder.AddPyramid(new Vector3(0, scale / 2, 0), Vector3.UnitZ, Vector3.UnitY, scale, 0, true);
        var singleBone = builder.ToMesh();
        if (singleBone.Positions is not { } singleBonePositions || singleBone.Indices is not { } singleBoneIndices)
            throw new InvalidOperationException("The skeleton source mesh has no positions or indices.");
        var boneIds = new List<BoneIds>();
        var positions = new Vector3Collection(bones.Count * singleBonePositions.Count);
        var tris = new IntCollection(bones.Count * singleBoneIndices.Count);

        var offset = 0;

        for (var i = 0; i < bones.Count; ++i)
            if (bones[i].ParentIndex >= 0) {
                tris.AddRange(singleBoneIndices.Select(x => x + offset));
                var j = 0;
                for (; j < singleBonePositions.Count - 6; j += 3) {
                    positions.Add(SilkMath.TransformCoordinate(singleBonePositions[j],
                                                               bones[bones[i].ParentIndex].BindPose));
                    positions.Add(SilkMath.TransformCoordinate(singleBonePositions[j + 1],
                                                               bones[bones[i].ParentIndex].BindPose));
                    positions.Add(new Vector3(bones[i].BindPose.M41,
                                              bones[i].BindPose.M42,
                                              bones[i].BindPose.M43));
                    boneIds.Add(new BoneIds { Bone1 = bones[i].ParentIndex, Weights = new Vector4(1, 0, 0, 0) });
                    boneIds.Add(new BoneIds { Bone1 = bones[i].ParentIndex, Weights = new Vector4(1, 0, 0, 0) });
                    boneIds.Add(new BoneIds { Bone1 = i, Weights = new Vector4(1, 0, 0, 0) });
                }

                for (; j < singleBonePositions.Count; ++j) {
                    positions.Add(SilkMath.TransformCoordinate(singleBonePositions[j],
                                                               bones[bones[i].ParentIndex].BindPose));
                    boneIds.Add(new BoneIds { Bone1 = bones[i].ParentIndex, Weights = new Vector4(1, 0, 0, 0) });
                }

                offset += singleBonePositions.Count;
            }

        builder = new MeshBuilder(true, false);
        for (var i = 0; i < bones.Count; ++i) {
            var currPos = builder.Positions.Count;
            builder.AddSphere(Vector3.Zero, scale / 2, 12, 12);
            for (var j = currPos; j < builder.Positions.Count; ++j) {
                builder.Positions[j] = SilkMath.TransformCoordinate(builder.Positions[j], bones[i].BindPose);
                boneIds.Add(new BoneIds { Bone1 = i, Weights = new Vector4(1, 0, 0, 0) });
            }
        }

        positions.AddRange(builder.Positions);
        tris.AddRange(builder.TriangleIndices.Select(x => x + offset));
        var mesh = new BoneSkinnedMeshGeometry3D { Positions = positions, Indices = tris, VertexBoneIds = boneIds };
        mesh.Normals = mesh.CalculateNormals();
        return mesh;
    }

    private static IEnumerable<Triangle> SkinnedTriangles(Vector3[] skinnedVertices, IList<int> indices) {
        for (var i = 0; i < indices.Count; i += 3)
            yield return new Triangle {
                P0 = skinnedVertices[indices[i]], P1 = skinnedVertices[indices[i + 1]],
                P2 = skinnedVertices[indices[i + 2]]
            };
    }

    public virtual bool HitTestWithSkinnedVertices(
        HitTestContext context,
        Vector3[] skinnedVertices,
        Matrix modelMatrix,
        ref List<HitTestResult> hits,
        object originalSource
    ) {
        if (skinnedVertices.Length == 0 || Indices is not { Count: > 0 } indices)
            return false;
        var isHit = false;
        var result = new HitTestResult {
            Distance = double.MaxValue
        };
        var modelInvert = modelMatrix.Inverted();
        if (modelInvert == default) //Check if model matrix can be inverted.
            return false;
        var rayWs = context.RayWs;
        //transform ray into model coordinates
        var rayModel = new Ray(SilkMath.TransformCoordinate(rayWs.Position, modelInvert),
                               SilkMath.Normalize(SilkMath.TransformNormal(rayWs.Direction, modelInvert)));

        var index = 0;
        var minDistance = float.MaxValue;
        foreach (var t in SkinnedTriangles(skinnedVertices, indices)) {
            // Used when geometry size is really small, causes hit test failure due to SharpDX.MathUtils.ZeroTolerance.
            var scaling = 1f;
            var rayScaled = rayModel;
            if (EnableSmallTriangleHitTestScaling)
                if ((t.P0 - t.P1).LengthSquared() < SmallTriangleEdgeLengthSquare
                    || (t.P1 - t.P2).LengthSquared() < SmallTriangleEdgeLengthSquare
                    || (t.P2 - t.P0).LengthSquared() < SmallTriangleEdgeLengthSquare) {
                    scaling = SmallTriangleHitTestScaling;
                    rayScaled = new Ray(rayModel.Position * scaling, rayModel.Direction);
                }

            var v0 = t.P0 * scaling;
            var v1 = t.P1 * scaling;
            var v2 = t.P2 * scaling;

            if (Collision.RayIntersectsTriangle(ref rayScaled, ref v0, ref v1, ref v2, out float d))
                if (d >= 0 && d < minDistance) // If d is NaN, the condition is false.
                {
                    minDistance = d;
                    result.IsValid = true;
                    result.ModelHit = originalSource;
                    var pointWorld =
                        SilkMath.TransformCoordinate(rayModel.Position + rayModel.Direction * d, modelMatrix);
                    result.PointHit = pointWorld;
                    result.Distance = (rayWs.Position - pointWorld).Length;
                    var p0 = SilkMath.TransformCoordinate(v0, modelMatrix);
                    var p1 = SilkMath.TransformCoordinate(v1, modelMatrix);
                    var p2 = SilkMath.TransformCoordinate(v2, modelMatrix);
                    var n = SilkMath.Cross(p1 - p0, p2 - p0);
                    n.Normalize();
                    // transform hit-info to world space now:
                    result.NormalAtHit = n; // SilkMath.TransformNormal(n, m).ToVector3D();
                    result.TriangleIndices =
                        new Tuple<int, int, int>(indices[index], indices[index + 1], indices[index + 2]);
                    result.Tag = index / 3;
                    result.Geometry = this;
                    isHit = true;
                    if (ReturnMultipleHitsOnHitTest) {
                        hits.Add(result);
                        result = new HitTestResult();
                    }
                }

            index += 3;
        }

        if (isHit) hits.Add(result);
        return isHit;
    }
}
