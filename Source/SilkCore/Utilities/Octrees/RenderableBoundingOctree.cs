using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Model.Scene;

namespace HelixToolkit.SharpDX.Core.Utilities;
public class BoundableNodeOctree : DynamicOctreeBase<SceneNode> {
    /// <summary>
    ///     Only root contains dictionary
    /// </summary>
    private Dictionary<Guid, IDynamicOctree> octantDictionary;

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
        HitTestContext context,
        object model,
        Geometry3D geometry,
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
        var rayWs = context.RayWs;
        
        if (rayWs.Intersects(ref bound)) {
            isIntersect = true;
            foreach (var r in Objects) {
                isHit |= r.HitTest(context, ref tempHits);
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
                              foreach (var item in (node as DynamicOctreeBase<SceneNode>).Objects)
                                  octantDictionary.Add(item.Guid, node);
                          });
    }

    public IDynamicOctree? FindItemByGuid(Guid guid, SceneNode item, out int index) {
        var root = FindRoot(this) as BoundableNodeOctree;
        index = -1;
        if (root.octantDictionary.ContainsKey(guid)) {
            var node = root.octantDictionary[guid];
            index = (node as DynamicOctreeBase<SceneNode>).Objects.IndexOf(item);
            return root.octantDictionary[guid];
        }

        return null;
    }

    public bool RemoveByGuid(Guid guid, SceneNode item) {
        var root = FindRoot(this);
        return RemoveByGuid(guid, item, root as BoundableNodeOctree);
    }

    public bool RemoveByGuid(Guid guid, SceneNode item, BoundableNodeOctree root) {
        if (root.octantDictionary.ContainsKey(guid)) {
            (octantDictionary[guid] as BoundableNodeOctree).RemoveSafe(item, root);
            return true;
        }

        return false;
    }

    public override bool Add(SceneNode item, out IDynamicOctree octant) {
        if (base.Add(item, out octant)) {
            if (octant == null) throw new Exception("Output octant is null");
            
            var root = FindRoot(this) as BoundableNodeOctree;
            if (!root.octantDictionary.ContainsKey(item.Guid)) 
                root.octantDictionary.Add(item.Guid, octant);
            
            return true;
        }

        return false;
    }

    public override bool PushExistingToChild(int index, out IDynamicOctree octant) {
        var item = Objects[index];
        if (base.PushExistingToChild(index, out octant)) {
            var root = FindRoot(this) as BoundableNodeOctree;
            root.octantDictionary[item.Guid] = octant;
            return true;
        }

        return false;
    }

    public override bool RemoveSafe(SceneNode item) {
        var root = FindRoot(this);
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
        var root = FindRoot(this);
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
        var root = FindRoot(this);
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
        var root = this;
        if (!IsRoot) root = FindRoot(this) as BoundableNodeOctree;
        var newRoot = Expand(root, ref direction, CreateNodeWithParent);
        (newRoot as BoundableNodeOctree).TransferOctantDictionary(root,
                                                                  ref root
                                                                      .octantDictionary); //Transfer the dictionary to new root
        return newRoot;
    }

    public override IDynamicOctree? Shrink() {
        var root = this;
        if (!IsRoot) root = FindRoot(this) as BoundableNodeOctree;
        var newRoot = Shrink(root);
        (newRoot as BoundableNodeOctree).TransferOctantDictionary(root,
                                                                  ref root
                                                                      .octantDictionary); //Transfer the dictionary to new root
        return newRoot;
    }

    private void TransferOctantDictionary(
        IDynamicOctree source,
        ref Dictionary<Guid, IDynamicOctree>? dictionary
    ) {
        if (source == this) 
            return;
        
        octantDictionary = dictionary;
        dictionary = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveFromRootDictionary(IDynamicOctree node, Guid guid) {
        node = FindRoot(node);
        var root = node as BoundableNodeOctree;
        if (root.octantDictionary.ContainsKey(guid)) 
            root.octantDictionary.Remove(guid);
    }

    public override bool FindNearestPointBySphereExcludeChild(
        HitTestContext context,
        ref BoundingSphere sphere,
        ref List<HitTestResult> points,
        ref bool isIntersect
    ) {
        throw new NotImplementedException();
    }
}
