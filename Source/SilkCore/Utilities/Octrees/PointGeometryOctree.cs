using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Model.Geometry;

namespace HelixToolkit.SharpDX.Core.Utilities.Octrees;

/// <summary>
///     Octree for points
/// </summary>
[Obsolete("Please use StaticPointGeometryOctree for better performance")]
public class PointGeometryOctree : DynamicOctreeBase<int> {
    private static readonly Vector3 BoundOffset = new(0.0001f);
    private readonly IList<Vector3> positions;

    /// <summary>
    /// </summary>
    /// <param name="positions"></param>
    /// <param name="stackCache"></param>
    public PointGeometryOctree(IList<Vector3> positions, Stack<(int Key, IDynamicOctree?[] Value)>? stackCache = null)
        : this(positions, null, stackCache) { }

    /// <summary>
    /// </summary>
    /// <param name="positions"></param>
    /// <param name="parameter"></param>
    /// <param name="stackCache"></param>
    public PointGeometryOctree(
        IList<Vector3> positions,
        OctreeBuildParameter? parameter,
        Stack<(int Key, IDynamicOctree?[] Value)>? stackCache = null
    ) : base(null, parameter, stackCache) 
    {
        this.positions = positions;
        Bound = BoundingBoxExtensions.FromPoints(positions);
        Objects = new List<int>(this.positions.Count);
        
        for (var i = 0; i < this.positions.Count; ++i) 
            Objects.Add(i);
    }

    /// <summary>
    /// </summary>
    /// <param name="bound"></param>
    /// <param name="positions"></param>
    /// <param name="list"></param>
    /// <param name="parent"></param>
    /// <param name="parameter"></param>
    /// <param name="stackCache"></param>
    protected PointGeometryOctree(
        BoundingBox bound,
        IList<Vector3> positions,
        List<int> list,
        IDynamicOctree parent,
        OctreeBuildParameter parameter,
        Stack<(int Key, IDynamicOctree?[] Value)>? stackCache
    ) : base(ref bound, list, parent, parameter, stackCache) {
        this.positions = positions;
    }

    /// <summary>
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    protected override bool IsContains(BoundingBox source, BoundingBox target, int obj) 
        => source.Contains(positions[obj]) != ContainmentType.Disjoint;

    /// <summary>
    ///     Get the distance from ray to a point
    /// </summary>
    /// <param name="r"></param>
    /// <param name="p"></param>
    /// <returns></returns>
    public static double DistanceRayToPoint(ref Ray r, ref Vector3 p) {
        var v = r.Direction;
        var w = p - r.Position;

        var c1 = SilkMath.Dot(w, v);
        var c2 = SilkMath.Dot(v, v);
        var b = c1 / c2;

        var pb = r.Position + v * b;
        return (p - pb).Length;
    }

    /// <summary>
    ///     Return nearest point it gets hit. And the distance from ray origin to the point it gets hit
    /// </summary>
    /// <param name="model"></param>
    /// <param name="geometry"></param>
    /// <param name="modelMatrix"></param>
    /// <param name="hits"></param>
    /// <param name="isIntersect"></param>
    /// <param name="context"></param>
    /// <param name="rayModel"></param>
    /// <param name="hitThickness"></param>
    /// <returns></returns>
    public override bool HitTestCurrentNodeExcludeChild(
        HitTestContext? context,
        object model,
        Geometry3D? geometry,
        Matrix modelMatrix,
        ref Ray rayModel,
        ref List<HitTestResult> hits,
        ref bool isIntersect,
        float hitThickness
    ) {
        isIntersect = false;
        if (!treeBuilt || context == null) return false;
        var isHit = false;

        var bound = Bound;

        if (rayModel.Intersects(ref bound)) {
            isIntersect = true;
            if (Objects.Count == 0) return false;
            var result = new HitTestResult {
                Distance = double.MaxValue
            };
            var svpm = context.RenderMatrices.ScreenViewProjectionMatrix;
            var smvpm = modelMatrix * svpm;
            var clickPoint3 = context.HitPointSp.ToVector3() * context.RenderMatrices.DpiScale;
            var rayWs = context.RayWs;
            var pos3 = rayWs.Position;
            SilkMath.TransformCoordinate(ref clickPoint3, ref svpm, out var clickPoint);
            SilkMath.TransformCoordinate(ref pos3, ref svpm, out pos3);

            var dist = hitThickness;
            for (var i = 0; i < Objects.Count; ++i) {
                var v0 = positions[Objects[i]];
                var p0 = SilkMath.TransformCoordinate(v0, smvpm);
                var pv = p0 - clickPoint;
                var d = pv.Length;
                if (d < dist) // If d is NaN, the condition is false.
                {
                    dist = d;
                    result.IsValid = true;
                    result.ModelHit = model;
                    var px = SilkMath.TransformCoordinate(v0, modelMatrix);
                    result.PointHit = px;
                    result.Distance = (rayWs.Position - px).Length;
                    result.Tag = Objects[i];
                    result.Geometry = geometry;
                    isHit = true;
                }
            }

            if (isHit) {
                isHit = false;
                if (hits.Count > 0) {
                    if (hits[0].Distance > result.Distance) {
                        hits[0] = result;
                        isHit = true;
                    }
                } else {
                    hits.Add(result);
                    isHit = true;
                }
            }
        }

        return isHit;
    }

    /// <summary>
    /// </summary>
    /// <param name="bound"></param>
    /// <param name="objList"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    protected override IDynamicOctree CreateNodeWithParent(
        ref BoundingBox bound,
        List<int> objList,
        IDynamicOctree parent
    )
        => new PointGeometryOctree(bound, positions, objList, parent, Parameter, Stack);

    /// <summary>
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    protected override BoundingBox GetBoundingBoxFromItem(int item) => new(positions[item] - BoundOffset, positions[item] + BoundOffset);

    /// <summary>
    ///     <see
    ///         cref="DynamicOctreeBase{T}.FindNearestPointBySphereExcludeChild(HitTestContext, ref global::SharpDX.BoundingSphere, ref List{HitTestResult}, ref bool)" />
    /// </summary>
    /// <param name="context"></param>
    /// <param name="sphere"></param>
    /// <param name="result"></param>
    /// <param name="isIntersect"></param>
    /// <returns></returns>
    public override bool FindNearestPointBySphereExcludeChild(
        HitTestContext context,
        ref BoundingSphere sphere,
        ref List<HitTestResult> result,
        ref bool isIntersect
    ) {
        var isHit = false;
        var containment = Bound.Contains(ref sphere);
        if (containment == ContainmentType.Contains || containment == ContainmentType.Intersects) {
            isIntersect = true;
            if (Objects.Count == 0) return false;
            var resultTemp = new HitTestResult {
                Distance = float.MaxValue
            };
            for (var i = 0; i < Objects.Count; ++i) {
                var p = positions[Objects[i]];
                containment = BoundingSphereExtensions.Contains(sphere, p);
                if (containment == ContainmentType.Contains || containment == ContainmentType.Intersects) {
                    var d = (p - sphere.Center).Length;
                    if (resultTemp.Distance > d) {
                        resultTemp.Distance = d;
                        resultTemp.IsValid = true;
                        resultTemp.PointHit = p;
                        resultTemp.Tag = Objects[i];
                        isHit = true;
                    }
                }
            }

            if (isHit) {
                isHit = false;
                if (result.Count > 0) {
                    if (result[0].Distance > resultTemp.Distance) {
                        result[0] = resultTemp;
                        isHit = true;
                    }
                } else {
                    result.Add(resultTemp);
                    isHit = true;
                }
            }
        } else {
            isIntersect = false;
        }

        return isHit;
    }
}
