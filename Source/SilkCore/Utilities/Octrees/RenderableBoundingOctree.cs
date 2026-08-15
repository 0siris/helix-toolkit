using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Model.Scene;

namespace HelixToolkit.SharpDX.Core.Utilities;
public class BoundableNodeOctree : DynamicOctreeBase<SceneNode> {
    /// <summary>
    ///     Only root contains dictionary
    /// </summary>
    private Dictionary<Guid, IDynamicOctree> octantDictionary = [];

    private static BoundableNodeOctree GetRoot(IDynamicOctree node) =>
        FindRoot(node) as BoundableNodeOctree
        ?? throw new InvalidOperationException("The octree root is not a boundable node octree.");

    public BoundableNodeOctree(
        List<SceneNode> objList,
        Stack<(int Key, IDynamicOctree?[] Value)>? queueCache = null
    )
        : this(objList, null, queueCache) { }

    public BoundableNodeOctree(
        List<SceneNode> objList,
        OctreeBuildParameter? paramter,
        Stack<(int Key, IDynamicOctree?[] Value)>? queueCache = null
    )
        : base(null, paramter, queueCache) {
        Objects = objList;
        if (Objects is {Count: > 0}) {
            var bound = GetBoundingBoxFromItem(Objects[0]);
            foreach (var item in Objects) {
                var b = GetBoundingBoxFromItem(item);
                BoundingBox.Merge(ref b, ref bound, out bound);
            }

            Bound = bound;
        }
    }

    protected BoundableNodeOctree(
        BoundingBox bound,
        List<SceneNode> objList,
        IDynamicOctree parent,
        OctreeBuildParameter paramter,
        Stack<(int Key, IDynamicOctree?[] Value)> queueCache
    )
        : base(ref bound, objList, parent, paramter, queueCache) { }

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
        if (!treeBuilt) return false;
        var isHit = false;
        //var bound = Bound.Transform(modelMatrix);// BoundingBox.FromPoints(Bound.GetCorners().Select(x => SilkMath.TransformCoordinate(x, modelMatrix)).ToArray());
        var bound = Bound;
        var tempHits = new List<HitTestResult>();
        if (context is not { } hitContext) return false;
        var rayWs = hitContext.RayWs;
        
        if (rayWs.Intersects(ref bound)) {
            isIntersect = true;
            foreach (var r in Objects) {
                isHit |= r.HitTest(hitContext, ref tempHits);
                hits.AddRange(tempHits);
                tempHits.Clear();
            }
        }

        return isHit;
    }

    protected override BoundingBox GetBoundingBoxFromItem(SceneNode item) 
        => item.BoundsWithTransform;

    protected override IDynamicOctree CreateNodeWithParent(
        ref BoundingBox bound,
        List<SceneNode> objList,
        IDynamicOctree parent
    )
        => new BoundableNodeOctree(bound, objList, parent, parent.Parameter, Stack);

    public override void BuildTree() {
        if (IsRoot) octantDictionary = new Dictionary<Guid, IDynamicOctree>(Objects.Count);
        base.BuildTree();
        if (IsRoot)
            TreeTraversal(this,
                          Stack,
                          null,
                          node => {
                              if (node is DynamicOctreeBase<SceneNode> octree)
                                  foreach (var item in octree.Objects)
                                      octantDictionary.Add(item.Guid, node);
                          });
    }

    public IDynamicOctree? FindItemByGuid(Guid guid, SceneNode item, out int index) {
        var root = GetRoot(this);
        index = -1;
        if (root.octantDictionary.TryGetValue(guid, out var node)) {
            if (node is DynamicOctreeBase<SceneNode> octree) index = octree.Objects.IndexOf(item);
            return node;
        }

        return null;
    }

    public bool RemoveByGuid(Guid guid, SceneNode item) {
        return RemoveByGuid(guid, item, GetRoot(this));
    }

    public bool RemoveByGuid(Guid guid, SceneNode item, BoundableNodeOctree root) {
        if (root.octantDictionary.TryGetValue(guid, out var octant)
            && octant is BoundableNodeOctree boundableOctant) {
            boundableOctant.RemoveSafe(item, root);
            return true;
        }

        return false;
    }

    public override bool Add(SceneNode item, out IDynamicOctree? octant) {
        if (base.Add(item, out octant) && octant is { } octantNode) {
            var root = GetRoot(this);
            if (!root.octantDictionary.ContainsKey(item.Guid)) 
                root.octantDictionary.Add(item.Guid, octantNode);
            
            return true;
        }

        return false;
    }

    public override bool PushExistingToChild(int index, out IDynamicOctree octant) {
        var item = Objects[index];
        if (base.PushExistingToChild(index, out octant)) {
            var root = GetRoot(this);
            root.octantDictionary[item.Guid] = octant;
            return true;
        }

        return false;
    }

    public override bool RemoveSafe(SceneNode item) {
        var root = GetRoot(this);
        return RemoveSafe(item, root);
    }

    public bool RemoveSafe(SceneNode item, IDynamicOctree root) {
        if (base.RemoveSafe(item)) {
            RemoveFromRootDictionary(root, item.Guid);
            return true;
        }

        return false;
    }

    public override bool RemoveAt(int index) {
        var root = GetRoot(this);
        return RemoveAt(index, root);
    }

    public bool RemoveAt(int index, IDynamicOctree root) {
        var id = Objects[index].Guid;
        if (base.RemoveAt(index)) {
            RemoveFromRootDictionary(root, id);
            return true;
        }

        return false;
    }

    public override bool RemoveByBound(SceneNode item, ref BoundingBox bound) {
        var root = GetRoot(this);
        return RemoveByBound(item, ref bound, root);
    }

    public bool RemoveByBound(SceneNode item, ref BoundingBox bound, IDynamicOctree root) {
        if (base.RemoveByBound(item, ref bound)) {
            RemoveFromRootDictionary(root, item.Guid);
            return true;
        }

        return false;
    }

    public override IDynamicOctree Expand(ref Vector3 direction) {
        var root = GetRoot(this);
        var newRoot = Expand(root, ref direction, CreateNodeWithParent);
        if (newRoot is BoundableNodeOctree boundableRoot)
            boundableRoot.TransferOctantDictionary(root, ref root.octantDictionary);
        return newRoot;
    }

    public override IDynamicOctree? Shrink() {
        var root = GetRoot(this);
        var newRoot = Shrink(root);
        if (newRoot is BoundableNodeOctree boundableRoot)
            boundableRoot.TransferOctantDictionary(root, ref root.octantDictionary);
        return newRoot;
    }

    private void TransferOctantDictionary(IDynamicOctree source, ref Dictionary<Guid, IDynamicOctree> dictionary) {
        if (source == this) 
            return;
        
        octantDictionary = dictionary;
        dictionary = [];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveFromRootDictionary(IDynamicOctree node, Guid guid) {
        var root = GetRoot(node);
        root.octantDictionary.Remove(guid);
    }

    public override bool FindNearestPointBySphereExcludeChild(
        HitTestContext context,
        ref BoundingSphere sphere,
        ref List<HitTestResult> points,
        ref bool isIntersect
    )
        => throw new NotImplementedException();
}
