using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Model.Geometry;

namespace HelixToolkit.SharpDX.Core.Utilities.Octrees;

/// <summary>
///     Octree for instancing
/// </summary>
[Obsolete("Please use StaticInstancingOctree for better performance")]
public class InstancingModel3DOctree : DynamicOctreeBase<KeyValuePair<int, BoundingBox>> {
    private readonly IList<Matrix> instanceMatrix;

    /// <summary>
    /// </summary>
    /// <param name="instanceMatrix"></param>
    /// <param name="geometryBound"></param>
    /// <param name="parameter"></param>
    /// <param name="stackCache"></param>
    public InstancingModel3DOctree(
        IList<Matrix> instanceMatrix,
        BoundingBox geometryBound,
        OctreeBuildParameter parameter,
        Stack<(int Key, IDynamicOctree?[] Value)>? stackCache = null
    ) : base(ref geometryBound, null, parameter, stackCache) 
    {
        this.instanceMatrix = instanceMatrix;
        var counter = 0;
        var totalBound =
            BoundingBoxExtensions.Transform(geometryBound, instanceMatrix
                                        [0]); // BoundingBox.FromPoints(geometryBound.GetCorners().Select(x => SilkMath.TransformCoordinate(x, instanceMatrix[0])).ToArray());
        for (var i = 0; i < instanceMatrix.Count; ++i) {
            var b = BoundingBoxExtensions.Transform(geometryBound, instanceMatrix
                                                [i]); // BoundingBox.FromPoints(geometryBound.GetCorners().Select(x => SilkMath.TransformCoordinate(x, m)).ToArray());
            Objects.Add(new KeyValuePair<int, BoundingBox>(counter, b));
            BoundingBox.Merge(ref totalBound, ref b, out totalBound);
            ++counter;
        }

        Bound = totalBound;
    }

    /// <summary>
    /// </summary>
    /// <param name="bound"></param>
    /// <param name="instanceMatrix"></param>
    /// <param name="objects"></param>
    /// <param name="parent"></param>
    /// <param name="parameter"></param>
    /// <param name="stackCache"></param>
    protected InstancingModel3DOctree(
        ref BoundingBox bound,
        IList<Matrix> instanceMatrix,
        List<KeyValuePair<int, BoundingBox>> objects,
        IDynamicOctree parent,
        OctreeBuildParameter parameter,
        Stack<(int Key, IDynamicOctree?[] Value)>? stackCache = null
    ) : base(ref bound, objects, parent, parameter, stackCache)
        => this.instanceMatrix = instanceMatrix;

    /// <summary>
    ///     <see
    ///         cref="DynamicOctreeBase{T}.FindNearestPointBySphereExcludeChild(HitTestContext, ref global::SharpDX.BoundingSphere, ref List{HitTestResult}, ref bool)" />
    /// </summary>
    /// <param name="context"></param>
    /// <param name="sphere"></param>
    /// <param name="points"></param>
    /// <param name="isIntersect"></param>
    /// <returns></returns>
    public override bool FindNearestPointBySphereExcludeChild(
        HitTestContext? context,
        ref BoundingSphere sphere,
        ref List<HitTestResult> points,
        ref bool isIntersect
    )
        => false;

    /// <summary>
    ///     <see
    ///         cref="DynamicOctreeBase{T}.HitTestCurrentNodeExcludeChild" />
    /// </summary>
    /// <param name="context"></param>
    /// <param name="model"></param>
    /// <param name="geometry"></param>
    /// <param name="modelMatrix"></param>
    /// <param name="rayModel"></param>
    /// <param name="hits"></param>
    /// <param name="isIntersect"></param>
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
        if (!treeBuilt || context is null) return false;
        var isHit = false;
        var
            bound = BoundingBoxExtensions.Transform(Bound, modelMatrix); // BoundingBox.FromPoints(Bound.GetCorners().Select(x => SilkMath.TransformCoordinate(x, modelMatrix)).ToArray());
        var rayWs = context.RayWs;
        if (!rayWs.Intersects(ref bound))
            return isHit;
        
        isIntersect = true;
        foreach (var keyValuePair in Objects) {
            var b = BoundingBoxExtensions.Transform(keyValuePair.Value, modelMatrix); // BoundingBox.FromPoints(t.Item2.GetCorners().Select(x => SilkMath.TransformCoordinate(x, modelMatrix)).ToArray());
            if (b.Intersects(ref rayWs)) {
                var result = new HitTestResult {
                    Tag = keyValuePair.Key
                };
                hits.Add(result);
                isHit = true;
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
        List<KeyValuePair<int, BoundingBox>> objList,
        IDynamicOctree parent
    ) => new InstancingModel3DOctree(ref bound, instanceMatrix, objList, parent, Parameter, Stack);

    /// <summary>
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    protected override BoundingBox GetBoundingBoxFromItem(KeyValuePair<int, BoundingBox> item)
        => item.Value;
}
