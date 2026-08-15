using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HelixToolkit.Logger;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core;

/// <summary>
///     Base class template implementation for <see cref="IDynamicOctree" />
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class DynamicOctreeBase<T> : IDynamicOctree {
    /// <summary>
    /// </summary>
    /// <param name="bound"></param>
    /// <param name="objects"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public delegate IDynamicOctree CreateNodeDelegate(ref BoundingBox bound, List<T> objects, IDynamicOctree parent);

    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;

    private static readonly Vector3 Epsilon = new(float.Epsilon, float.Epsilon, float.Epsilon);

    private readonly List<BoundingBox> hitPathBoundingBoxes = [];

    /// <summary>
    ///     internal stack for tree traversal
    /// </summary>
    protected readonly Stack<(int Key, IDynamicOctree?[] Value)> Stack;

    private BoundingBox bound;

    protected List<HitTestResult> ModelHits = [];

    protected bool treeBuilt; //there is no pre-existing tree yet.
    
    
    /// <summary>
    ///     Creates an oct tree which encloses the given region and contains the provided objects.
    /// </summary>
    /// <param name="bound">The bounding region for the oct tree.</param>
    /// <param name="objList">The list of objects contained within the bounding region</param>
    /// <param name="parent"></param>
    /// <param name="parameter"></param>
    /// <param name="stackCache"></param>
    protected DynamicOctreeBase(
        ref BoundingBox bound,
        List<T> objList,
        IDynamicOctree parent,
        OctreeBuildParameter parameter,
        Stack<(int Key, IDynamicOctree?[] Value)>? stackCache
    ) : this(parameter, stackCache) 
    {
        Bound = bound;
        Objects = objList;
        Parent = parent;
    }

    /// <summary>
    ///     Creates an octTree with a suggestion for the bounding region containing the items.
    /// </summary>
    /// <param name="bound">
    ///     The suggested dimensions for the bounding region.
    ///     Note: if items are outside this region, the region will be automatically resized.
    /// </param>
    /// <param name="parent"></param>
    /// <param name="parameter"></param>
    /// <param name="stackCache"></param>
    protected DynamicOctreeBase(
        ref BoundingBox bound,
        IDynamicOctree? parent,
        OctreeBuildParameter parameter,
        Stack<(int, IDynamicOctree?[])>? stackCache
    ) : this(parent, parameter, stackCache)
    {
        Bound = bound;
    }
    
    /// <summary>
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="parameter"></param>
    /// <param name="stackCache"></param>
    protected DynamicOctreeBase(
        IDynamicOctree? parent,
        OctreeBuildParameter? parameter,
        Stack<(int Key, IDynamicOctree?[] Value)>? stackCache
    ) : this(parameter, stackCache) 
    {
        Parent = parent;
    }

    private DynamicOctreeBase(OctreeBuildParameter? parameter, Stack<(int Key, IDynamicOctree?[] Value)>? stackCache) {
        SelfArray = [this];
        Stack = stackCache ?? new (64);

        if (stackCache == null && Logger.IsEnabled(LogLevel.Trace)) 
            Logger.Verbose("Stack cache is null");
        
        Parameter = parameter ?? new OctreeBuildParameter();
        
        Objects ??= [];
        Bound = new BoundingBox(Vector3.Zero, Vector3.Zero);
    }


    /// <summary>
    ///     The minumum size for enclosing region is a 1x1x1 cube.
    /// </summary>
    public float MinSize => Parameter.MinimumOctantSize;

    /// <summary>
    ///     <see cref="DynamicOctreeBase{T}.Objects" />
    /// </summary>
    public List<T> Objects { get; protected set; }

    /// <summary>
    /// </summary>
    public event EventHandler<EventArgs>? Hit;

    /// <summary>
    ///     <see cref="IOctreeBasic.TreeBuilt" />
    /// </summary>
    public bool TreeBuilt => treeBuilt;

    /// <summary>
    ///     <see cref="IOctreeBasic.Parameter" />
    /// </summary>
    public OctreeBuildParameter Parameter { get; }

    /// <summary>
    ///     <see cref="IOctreeBasic.Bound" />
    /// </summary>
  
    public BoundingBox Bound {
        get => bound;
        
        protected set {
            if (bound == value) {
                return;
            }
            bound = value;
            Octants = CreateOctants(ref value, MinSize);
        }
    }

    /// <summary>
    ///     <see cref="IOctreeBasic.HitPathBoundingBoxes" />
    /// </summary>
    public IList<BoundingBox> HitPathBoundingBoxes => hitPathBoundingBoxes.AsReadOnly();

    /// <summary>
    ///     <see cref="IDynamicOctree.ChildNodes" />
    /// </summary>
    public IDynamicOctree?[] ChildNodes { get; } = new IDynamicOctree[8];

    /// <summary>
    ///     <see cref="IDynamicOctree.ActiveNodes" />
    /// </summary>
    public byte ActiveNodes { get; set; }

    /// <summary>
    ///     <see cref="IDynamicOctree.Parent" />
    /// </summary>
    public IDynamicOctree? Parent { get; set; }

    /// <summary>
    ///     <see cref="IDynamicOctree.Octants" />
    /// </summary>
    public BoundingBox[] Octants { get; private set; } = [];

    /// <summary>
    ///     Gets the self array.
    /// </summary>
    /// <value>
    ///     The self array.
    /// </value>
    public IDynamicOctree?[] SelfArray { get; }

    /// <summary>
    ///     Delete the octant if there is no object or child octant inside it.
    /// </summary>
    public bool AutoDeleteIfEmpty {
        get => Parameter.AutoDeleteIfEmpty;
        set => Parameter.AutoDeleteIfEmpty = value;
    }

    /// <summary>
    ///     Build the octree
    /// </summary>
    public virtual void BuildTree() {
        if (Bound.Maximum == Bound.Minimum || !CheckDimension()) {
            treeBuilt = false;
            return;
        }

        if (Parameter.Cubify) 
            Bound = FindEnclosingCube(ref bound);
        
        BuildTree(this, Stack);
    }

    /// <summary>
    /// </summary>
    public void BuildCurrentNodeOnly() {
        /*I think I can just directly insert items into the tree instead of using a stack.*/
        if (treeBuilt)
            return;
        
        //terminate the recursion if we're a leaf node
        if (Objects.Count <= 1) //doubt: is this really right? needs testing.
        {
            treeBuilt = true;
            return;
        }

        BuildSubTree();
        treeBuilt = true;
    }

    /// <summary>
    ///     <see cref="IDynamicOctree.Clear" />
    /// </summary>
    public virtual void Clear() {
        Objects.Clear();
        foreach (var t in ChildNodes)
            t?.Clear();

        Array.Clear(ChildNodes, 0, ChildNodes.Length);
    }

    /// <summary>
    /// </summary>
    /// <param name="context"></param>
    /// <param name="model"></param>
    /// <param name="geometry"></param>
    /// <param name="modelMatrix"></param>
    /// <param name="hits"></param>
    /// <returns></returns>
    public bool HitTest(
        HitTestContext context,
        object model,
        Geometry3D? geometry,
        Matrix modelMatrix,
        ref List<HitTestResult> hits
    )
        => HitTest(context, model, geometry, modelMatrix, ref hits, 0);

    /// <summary>
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="model">The model.</param>
    /// <param name="geometry">The geometry.</param>
    /// <param name="modelMatrix">The model matrix.</param>
    /// <param name="returnMultiple">Not used</param>
    /// <param name="hits">The hits.</param>
    /// <returns></returns>
    public bool HitTest(
        HitTestContext context,
        object model,
        Geometry3D geometry,
        Matrix modelMatrix,
        bool returnMultiple,
        ref List<HitTestResult> hits
    )
        => HitTest(context, model, geometry, modelMatrix, ref hits, 0);

    /// <summary>
    /// </summary>
    /// <param name="context"></param>
    /// <param name="model"></param>
    /// <param name="geometry"></param>
    /// <param name="modelMatrix"></param>
    /// <param name="hits"></param>
    /// <param name="hitThickness"></param>
    /// <returns></returns>
    public virtual bool HitTest(
        HitTestContext context,
        object model,
        Geometry3D? geometry,
        Matrix modelMatrix,
        ref List<HitTestResult> hits,
        float hitThickness
    ) {
        List<HitTestResult>? nullableHits = hits;
        var result = HitTest(context, model, geometry, modelMatrix, false, ref nullableHits, hitThickness);
        if (nullableHits is not null)
            hits = nullableHits;
        return result;
    }

    /// <summary>
    /// </summary>
    /// <param name="context"></param>
    /// <param name="model"></param>
    /// <param name="geometry"></param>
    /// <param name="modelMatrix"></param>
    /// <param name="returnMultiple"></param>
    /// <param name="hits"></param>
    /// <param name="hitThickness"></param>
    /// <returns></returns>
    public virtual bool HitTest(
        HitTestContext context,
        object model,
        Geometry3D? geometry,
        Matrix modelMatrix,
        bool returnMultiple,
        [NotNullWhen(true)]
        ref List<HitTestResult>? hits,
        float hitThickness
    ) {
        hits ??= [];
        hitPathBoundingBoxes.Clear();
        var hitStack = Stack;
        var isHit = false;
        ModelHits.Clear();
        
        var modelInv = modelMatrix.Inverted();
        if (modelInv == default) 
            return false; //Cannot be inverted
        
        var rayWs = context.RayWs;
        var rayModel = new Ray(SilkMath.TransformCoordinate(rayWs.Position, modelInv),
                               SilkMath.Normalize(SilkMath.TransformNormal(rayWs.Direction, modelInv)));
        
        var treeArray = SelfArray;
        var i = -1;
        while (true) {
            while (++i < treeArray.Length) {
                var node = treeArray[i];
                if (node == null) 
                    continue;
                
                var isIntersect = false;
                var nodeHit = node.HitTestCurrentNodeExcludeChild(context,
                                                                  model,
                                                                  geometry,
                                                                  modelMatrix,
                                                                  ref rayModel,
                                                                  ref ModelHits,
                                                                  ref isIntersect,
                                                                  hitThickness);
                isHit |= nodeHit;
                if (isIntersect && node.HasChildren) {
                    hitStack.Push(new (i, treeArray));
                    treeArray = node.ChildNodes;
                    i = -1;
                }

                if (Parameter.RecordHitPathBoundingBoxes && nodeHit) {
                    var n = node;
                    while (n != null) {
                        hitPathBoundingBoxes.Add(n.Bound);
                        n = n.Parent;
                    }
                }
            }

            if (hitStack.Count == 0) 
                break;
            
            (i, treeArray) = hitStack.Pop();
 
        }

        if (!isHit) {
            hitPathBoundingBoxes.Clear();
        } else {
            hits.AddRange(ModelHits);
            Hit?.Invoke(this, EventArgs.Empty);
        }

        return isHit;
    }

    /// <summary>
    ///     Hit test for current node.
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
    public abstract bool HitTestCurrentNodeExcludeChild(
        HitTestContext? context,
        object model,
        Geometry3D? geometry,
        Matrix modelMatrix,
        ref Ray rayModel,
        ref List<HitTestResult> hits,
        ref bool isIntersect,
        float hitThickness
    );

    /// <summary>
    /// </summary>
    /// <param name="context"></param>
    /// <param name="sphere"></param>
    /// <param name="points"></param>
    /// <returns></returns>
    public virtual bool FindNearestPointBySphere(
        HitTestContext context,
        ref BoundingSphere sphere,
        ref List<HitTestResult>? points
    ) {
        points ??= [];
        var hitStack = Stack;
        var isHit = false;
        var treeArray = SelfArray;
        var i = -1;
        while (true) {
            while (++i < treeArray.Length) {
                var node = treeArray[i];
                if (node == null) 
                    continue;
                
                var isIntersect = false;
                var nodeHit = node.FindNearestPointBySphereExcludeChild(context, ref sphere, ref points, ref isIntersect);
                isHit |= nodeHit;
                
                if (isIntersect && node.HasChildren) {
                    hitStack.Push(new (i, treeArray));
                    treeArray = node.ChildNodes;
                    i = -1;
                }
            }

            if (hitStack.Count == 0) 
                break;
            
            var pair = hitStack.Pop();
            i = pair.Key;
            treeArray = pair.Value;
        }

        return isHit;
    }

    /// <summary>
    /// </summary>
    /// <param name="context"></param>
    /// <param name="point"></param>
    /// <param name="results"></param>
    /// <param name="heuristicSearchFactor"></param>
    /// <returns></returns>
    public virtual bool FindNearestPointFromPoint(
        HitTestContext context,
        ref Vector3 point,
        ref List<HitTestResult>? results,
        float heuristicSearchFactor = 1f
    ) {
        results ??= [];
        var hitStack = Stack;

        var sphere = new BoundingSphere(point, float.MaxValue);
        var isIntersect = false;
        var isHit = false;
        heuristicSearchFactor = Math.Min(1.0f, Math.Max(0.1f, heuristicSearchFactor));
        var treeArray = SelfArray;
        var i = -1;
        while (true) {
            while (++i < treeArray.Length) {
                var node = treeArray[i];
                if (node == null) 
                    continue;
                
                isHit |= node.FindNearestPointBySphereExcludeChild(context, ref sphere, ref results, ref isIntersect);

                if (!isIntersect)
                    continue;
                
                if (results.Count > 0) sphere.Radius = (float)results[0].Distance * heuristicSearchFactor;
                if (node.HasChildren) {
                    hitStack.Push(new (i, treeArray));
                    treeArray = node.ChildNodes;
                    i = -1;
                }
            }

            if (hitStack.Count == 0) 
                break;
            
            (i, treeArray) = hitStack.Pop();

        }

        return isHit;
    }

    /// <summary>
    ///     Find nearest point by sphere on current node only.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="sphere"></param>
    /// <param name="points"></param>
    /// <param name="isIntersect"></param>
    /// <returns></returns>
    public abstract bool FindNearestPointBySphereExcludeChild(
        HitTestContext context,
        ref BoundingSphere sphere,
        ref List<HitTestResult> points,
        ref bool isIntersect
    );

    /// <summary>
    /// </summary>
    public virtual void RemoveSelf() {
        if (Parent == null) 
            return;

        Clear();
        Parent.RemoveChild(this);
        Parent = null;
    }

    /// <summary>
    /// </summary>
    /// <param name="child"></param>
    public void RemoveChild(IDynamicOctree child) {
        for (var i = 0; i < ChildNodes.Length; ++i)
            if (ChildNodes[i] == child) {
                ChildNodes[i] = null;
                ActiveNodes ^= (byte)(1 << i);
                break;
            }

        if (IsEmpty && AutoDeleteIfEmpty)
            RemoveSelf();
    }

    /// <summary>
    /// </summary>
    /// <param name="context"></param>
    /// <param name="point"></param>
    /// <param name="radius"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public bool FindNearestPointByPointAndSearchRadius(
        HitTestContext context,
        ref Vector3 point,
        float radius,
        ref List<HitTestResult>? result
    ) {
        var sphere = new BoundingSphere(point, radius);
        return FindNearestPointBySphere(context, ref sphere, ref result);
    }

    public LineGeometry3D CreateOctreeLineModel() 
        => OctreeHelper.CreateOctreeLineModel(this);

    private IDynamicOctree CreateNode(ref BoundingBox bounds, List<T> objList) 
        => CreateNodeWithParent(ref bounds, objList, this);

    /// <summary>
    /// </summary>
    /// <param name="bound"></param>
    /// <param name="objList"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    protected abstract IDynamicOctree CreateNodeWithParent(
        ref BoundingBox bound,
        List<T> objList,
        IDynamicOctree parent
    );

    /// <summary>
    /// </summary>
    /// <param name="bounds"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    protected IDynamicOctree CreateNode(ref BoundingBox bounds, T item) 
        => CreateNode(ref bounds, [item]);

    /// <summary>
    /// </summary>
    /// <param name="root"></param>
    /// <param name="stack"></param>
    public void BuildTree(IDynamicOctree root, Stack<(int, IDynamicOctree?[])> stack) {
#if DEBUG
        var now = Stopwatch.GetTimestamp();
#endif
        TreeTraversal(root, stack, null, node => { node.BuildCurrentNodeOnly(); }, null, Parameter.EnableParallelBuild);
#if DEBUG
        var elapsed = Stopwatch.GetTimestamp() - now;
        if (Logger.IsEnabled(LogLevel.Debug))
            Logger.Debug("Buildtree time = {Value0} ms", elapsed * 1e3 / Stopwatch.Frequency);
#endif
    }

    /// <summary>
    ///     Common function to traverse the tree
    /// </summary>
    /// <param name="root"></param>
    /// <param name="stack"></param>
    /// <param name="criteria"></param>
    /// <param name="process"></param>
    /// <param name="breakCriteria"></param>
    /// <param name="useParallel"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TreeTraversal(
        IDynamicOctree root,
        Stack<(int, IDynamicOctree?[])> stack,
        Func<IDynamicOctree, bool>? criteria,
        Action<IDynamicOctree> process,
        Func<bool>? breakCriteria = null,
        bool useParallel = false
    ) {
        if (useParallel) {
            if (criteria == null || criteria(root)) {
                process(root);
                if (breakCriteria != null && breakCriteria()) return;
                if (root.HasChildren)
                    Parallel.ForEach(root.ChildNodes,
                                     subTree => {
                                         if (subTree == null) 
                                             return;
                                         
                                         TreeTraversal(subTree,
                                                       new (),
                                                       criteria,
                                                       process,
                                                       breakCriteria);
                                     });
            }
        } else {
            var i = -1;
            var treeArray = root.SelfArray;
            while (true) {
                while (++i < treeArray.Length) {
                    var tree = treeArray[i];
                    if (tree != null && (criteria == null || criteria(tree))) {
                        process(tree);
                        if (breakCriteria != null && breakCriteria()) break;
                        if (tree.HasChildren) {
                            stack.Push(new (i, treeArray));
                            treeArray = tree.ChildNodes;
                            i = -1;
                        }
                    }
                }

                if (stack.Count == 0) break;
                var pair = stack.Pop();
                i = pair.Item1;
                treeArray = pair.Item2;
            }
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="box"></param>
    /// <param name="minSize"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BoundingBox[] CreateOctants(ref BoundingBox box, float minSize) {
        var dimensions = box.Maximum - box.Minimum;
        if (dimensions == Vector3.Zero || (dimensions.X < minSize && dimensions.Y < minSize && dimensions.Z < minSize))
            return [];
        var half = dimensions / 2.0f;
        var center = box.Minimum + half;
        var minimum = box.Minimum;
        var maximum = box.Maximum;
        //Create subdivided regions for each octant
        return [
            new(minimum, center),
            new(new Vector3(center.X, minimum.Y, minimum.Z), new Vector3(maximum.X, center.Y, center.Z)),
            new(new Vector3(center.X, minimum.Y, center.Z), new Vector3(maximum.X, center.Y, maximum.Z)),
            new(new Vector3(minimum.X, minimum.Y, center.Z), new Vector3(center.X, center.Y, maximum.Z)),
            new(new Vector3(minimum.X, center.Y, minimum.Z), new Vector3(center.X, maximum.Y, center.Z)),
            new(new Vector3(center.X, center.Y, minimum.Z), new Vector3(maximum.X, maximum.Y, center.Z)),
            new(center, maximum),
            new(new Vector3(minimum.X, center.Y, center.Z), new Vector3(center.X, maximum.Y, maximum.Z))
        ];
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckDimension() {
        var dimensions = Bound.Maximum - Bound.Minimum;

        if (dimensions == Vector3.Zero) 
            Bound = FindEnclosingBox();
        
        dimensions = Bound.Maximum - Bound.Minimum;
        
        //Check to see if the dimensions of the box are greater than the minimum dimensions
        if (dimensions.X < MinSize && dimensions.Y < MinSize && dimensions.Z < MinSize) 
            return false;

        return true;
    }

    /// <summary>
    ///     Build sub tree nodes
    /// </summary>
    protected virtual void BuildSubTree() {
        if (!CheckDimension() || Objects.Count < Parameter.MinObjectSizeToSplit) {
            treeBuilt = true;
            return;
        }

        //This will contain all of our objects which fit within each respective octant.
        var octList = new List<T>[8];
        for (var i = 0; i < 8; ++i)
            octList[i] = new List<T>(Objects.Count / 8);

        var count = Objects.Count;
        for (var i = Objects.Count - 1; i >= 0; --i) {
            var obj = Objects[i];
            for (var x = 0; x < 8; ++x)
                if (IsContains(Octants[x], obj)) {
                    octList[x].Add(obj);
                    Objects[i] =
                        Objects[--count]; //Disard the existing object from location i, replaced with last valid object.
                    break;
                }
        }

        Objects.RemoveRange(count, Objects.Count - count);
        Objects.TrimExcess();

        //Create child nodes where there are items contained in the bounding region
        for (var i = 0; i < 8; ++i)
            if (octList[i].Count != 0) {
                ChildNodes[i] = CreateNode(ref Octants[i], octList[i]);
                ActiveNodes |= (byte)(1 << i);
            }
    }

    /// <summary>
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    protected abstract BoundingBox GetBoundingBoxFromItem(T item);

    /// <summary>
    ///     This finds the dimensions of the bounding box necessary to tightly enclose all items in the object list.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected BoundingBox FindEnclosingBox() {
        if (Objects.Count == 0) 
            return Bound;
        
        var b = GetBoundingBoxFromItem(Objects[0]);
        
        foreach (var t in Objects) {
            var boundingBox = GetBoundingBoxFromItem(t);
            BoundingBox.Merge(ref b, ref boundingBox, out b);
        }

        return b;
    }

    /// <summary>
    ///     This finds the smallest enclosing cube which is a power of 2, for all objects in the list.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BoundingBox FindEnclosingCube(ref BoundingBox bound) {
        var v = (bound.Maximum - bound.Minimum) / 2 + bound.Minimum;
        bound = new BoundingBox(bound.Minimum - v, bound.Maximum - v);
        var max = Math.Max(bound.Maximum.X, Math.Max(bound.Maximum.Y, bound.Maximum.Z));
        return new BoundingBox(new Vector3(-max, -max, -max) + v, new Vector3(max, max, max) + v);
    }

    /// <summary>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static int SigBit(int x) {
        if (x >= 0) 
            return (int)Math.Pow(2, Math.Ceiling(Math.Log(x) / Math.Log(2)));

        x = Math.Abs(x);
        return -(int)Math.Pow(2, Math.Ceiling(Math.Log(x) / Math.Log(2)));
    }

    /// <summary>
    ///     <see cref="DynamicOctreeBase{T}.Add(T)" />
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Add(T item) => Add(item, out _);

    /// <summary>
    ///     <see cref="DynamicOctreeBase{T}.Add(T, out IDynamicOctree)" />
    /// </summary>
    /// <param name="item"></param>
    /// <param name="octant"></param>
    /// <returns></returns>
    public virtual bool Add(T item, out IDynamicOctree? octant) {
        var bounds = GetBoundingBoxFromItem(item);
        var node = FindSmallestNodeContainsBoundingBox(ref bounds, item);
        octant = node;
        if (node == null) 
            return false;

        var nodeBase = (DynamicOctreeBase<T>) node;
        nodeBase.Objects.Add(item);
        if (nodeBase.Objects.Count > Parameter.MinObjectSizeToSplit) {
            var index = ((DynamicOctreeBase<T>) node).Objects.Count - 1;
            PushExistingToChild(nodeBase, index, IsContains, CreateNodeWithParent, out var childOctant);
            octant = childOctant ?? node;
        }

        return true;
    }

    /// <summary>
    ///     Push one of object belongs to current node into its child octant
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool PushExistingToChild(int index) => PushExistingToChild(index, out _);

    /// <summary>
    ///     Push one of object belongs to current node into its child octant
    /// </summary>
    /// <param name="index"></param>
    /// <param name="octant"></param>
    /// <returns></returns>
    public virtual bool PushExistingToChild(int index, out IDynamicOctree octant) {
        octant = this;
        if (Objects.Count > Parameter.MinObjectSizeToSplit) {
            var pushed = PushExistingToChild(this, index, IsContains, CreateNodeWithParent, out var childOctant);
            octant = childOctant ?? this;
            return pushed;
        }

        return false;
    }

    /// <summary>
    ///     Push existing item into child
    /// </summary>
    /// <param name="node"></param>
    /// <param name="index"></param>
    /// <param name="isContains"></param>
    /// <param name="createNodeFunc"></param>
    /// <param name="octant"></param>
    /// <returns>True: Pushed into child. Otherwise false.</returns>
    public static bool PushExistingToChild(
        DynamicOctreeBase<T> node,
        int index,
        Func<BoundingBox, T, bool> isContains,
        CreateNodeDelegate createNodeFunc,
        out IDynamicOctree? octant
    ) {
        var item = node.Objects[index];
        octant = node;
        var pushToChild = false;
        for (var i = 0; i < node.Octants.Length; ++i)
            if (isContains(node.Octants[i], item)) {
                node.Objects.RemoveAt(index);
                ref var childNodes = ref node.ChildNodes[i]; //TODO check if referencing works  correct
                
                if (childNodes is not null) {
                    if(childNodes is DynamicOctreeBase<T> dynTree)
                        dynTree.Objects.Add(item);
                    else
                        throw new Exception("Invalid child node type");
                    
                    octant = childNodes;
                } else {
                    childNodes = createNodeFunc(ref node.Octants[i], [item], node);
                    node.ActiveNodes |= (byte)(1 << i);
                    childNodes.BuildTree();

                    octant = ((DynamicOctreeBase<T>)childNodes).FindChildByItemBound(item, out _) ?? childNodes;
                }

                pushToChild = true;
                break;
            }

        return pushToChild;
    }

    /// <summary>
    /// Determines whether the specified bounding box contains the target object based on its bounding box representation.
    /// </summary>
    /// <param name="source">The bounding box to check for containment.</param>
    /// <param name="targetObj">The target object whose bounding box representation is compared against the source.</param>
    /// <returns>True if the source bounding box fully contains the target object's bounding box; otherwise, false.</returns>
    protected bool IsContains(BoundingBox source, T targetObj) {
        var bounds = GetBoundingBoxFromItem(targetObj);
        return IsContains(source, bounds, targetObj);
    }

    /// <summary>
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <param name="targetObj"></param>
    /// <returns></returns>
    protected virtual bool IsContains(BoundingBox source, BoundingBox target, T targetObj) 
        => source.Contains(ref target) == ContainmentType.Contains;

    /// <summary>
    ///     Return new root
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public virtual IDynamicOctree Expand(ref Vector3 direction) 
        => Expand(this, ref direction, CreateNodeWithParent);

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //private static float CorrectFloatError(float value)
    //{
    //    if (value > 0)
    //    {
    //        return value + float.Epsilon;
    //    }
    //    else if (value < 0)
    //    {
    //        return value - float.Epsilon;
    //    }else
    //    {
    //        return value;
    //    }
    //}
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //private static void CorrectFloatError(ref Vector3 v)
    //{
    //    v.X = CorrectFloatError(v.X);
    //    v.Y = CorrectFloatError(v.Y);
    //    v.Z = CorrectFloatError(v.Z);
    //}
    /// <summary>
    ///     Return new root
    /// </summary>
    /// <param name="oldRoot"></param>
    /// <param name="direction"></param>
    /// <param name="createNodeFunc"></param>
    /// <returns></returns>
    public static IDynamicOctree Expand(
        IDynamicOctree oldRoot,
        ref Vector3 direction,
        CreateNodeDelegate createNodeFunc
    ) {
        if (oldRoot.Parent != null) 
            throw new ArgumentException("Input node is not root node");
        
        var rootBound = oldRoot.Bound;
        var xDirection = direction.X >= 0 ? 1 : -1;
        var yDirection = direction.Y >= 0 ? 1 : -1;
        var zDirection = direction.Z >= 0 ? 1 : -1;
        var dimension = rootBound.Maximum - rootBound.Minimum;
        var half = dimension / 2 + Epsilon;
        var center = rootBound.Minimum + half;
        var newCenter = center + new Vector3(xDirection * Math.Abs(half.X),
                                             yDirection * Math.Abs(half.Y),
                                             zDirection * Math.Abs(half.Z));
        
        var bound = new BoundingBox(newCenter - dimension, newCenter + dimension);
        BoundingBox.Merge(ref rootBound, ref bound, out bound);
        var newRoot = createNodeFunc(ref bound, [], oldRoot);
        newRoot.Parent = null;
        newRoot.BuildTree();
        var succ = false;
        if (!oldRoot.IsEmpty) {
            var idx = -1;
            var diff = float.MaxValue;
            for (var i = 0; i < newRoot.Octants.Length; ++i) {
                var d = (newRoot.Octants[i].Minimum - rootBound.Minimum).LengthSquared();
                if (d < diff) {
                    diff = d;
                    idx = i;
                    if (diff < 10e-8) break;
                }
            }

            if (idx >= 0 && idx < newRoot.Octants.Length) {
                newRoot.ChildNodes[idx] = oldRoot;
                newRoot.Octants[idx] = oldRoot.Bound;
                newRoot.ActiveNodes |= (byte)(1 << idx);
                oldRoot.Parent = newRoot;
                succ = true;
            }

            if (!succ) 
                throw new Exception("Expand failed.");
            
        }

        return newRoot;
    }

    /// <summary>
    ///     Shrink the root bound to contains all items inside, return new root
    /// </summary>
    /// <returns></returns>
    public virtual IDynamicOctree? Shrink() => Shrink(this);

    /// <summary>
    ///     Shrink the root bound to contains all items inside
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    public static IDynamicOctree? Shrink(IDynamicOctree root) {
        if (root.Parent is not null) 
            throw new ArgumentException("Input node is not a root node.");
        
        if (root.IsEmpty) 
            return root;

        if (((DynamicOctreeBase<T>) root).Objects.Count == 0 && (root.ActiveNodes & (root.ActiveNodes - 1)) == 0) {
            for (var i = 0; i < root.ChildNodes.Length; ++i) {
                if (root.ChildNodes[i] is { } newRoot) {
                    newRoot.Parent = null;
                    root.ChildNodes[i] = null;
                    return newRoot;
                }
            }
            return null;
        }

        return root;
    }

    /// <summary>
    /// </summary>
    /// <param name="bounds"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    public IDynamicOctree? FindSmallestNodeContainsBoundingBox(ref BoundingBox bounds, T item) 
        => FindSmallestNodeContainsBoundingBox(bounds, item, IsContains, this, Stack);

    /// <summary>
    /// </summary>
    /// <typeparam name="E"></typeparam>
    /// <param name="bound"></param>
    /// <param name="item"></param>
    /// <param name="isContains"></param>
    /// <param name="root"></param>
    /// <param name="stackCache"></param>
    /// <returns></returns>
    private static IDynamicOctree? FindSmallestNodeContainsBoundingBox<E>(
        BoundingBox bound,
        E item,
        Func<BoundingBox, E, bool> isContains,
        DynamicOctreeBase<E> root,
        Stack<(int, IDynamicOctree?[])> stackCache
    ) {
        IDynamicOctree? result = null;
        TreeTraversal(root,
                      stackCache,
                      node => isContains(node.Bound, item),
                      node => { result = node; });
        return result;
    }

    /// <summary>
    /// </summary>
    /// <param name="item"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public IDynamicOctree? FindChildByItem(T item, out int index) 
        => FindChildByItem(item, this, Stack, out index);

    /// <summary>
    /// </summary>
    /// <typeparam name="E"></typeparam>
    /// <param name="item"></param>
    /// <param name="root"></param>
    /// <param name="stackCache"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static IDynamicOctree? FindChildByItem<E>(
        E item,
        DynamicOctreeBase<E> root,
        Stack<(int, IDynamicOctree?[])> stackCache,
        out int index
    ) {
        IDynamicOctree? result = null;
        var idx = -1;
        TreeTraversal(root,
                      stackCache,
                      null,
                      node => {
                          idx = ((DynamicOctreeBase<E>) node).Objects.IndexOf(item);
                          result = idx != -1 ? node : null;
                      },
                      () => idx != -1);
        index = idx;
        return result;
    }

    /// <summary>
    /// </summary>
    /// <param name="item"></param>
    /// <param name="bounds"></param>
    /// <returns></returns>
    public virtual bool RemoveByBound(T item, ref BoundingBox bounds) {
        int index;
        var node = FindChildByItemBound(item, ref bounds, out index);
        if (node == null) {
#if DEBUG
            if (!RemoveSafe(item)) 
                throw new Exception("item not found using bound.");
            
            return true;
#else
                return RemoveSafe(item);
#endif
        }

        var nodeBase = (DynamicOctreeBase<T>) node;
        nodeBase.Objects.RemoveAt(index);
        if (nodeBase is {IsEmpty: true, AutoDeleteIfEmpty: true}) 
            nodeBase.RemoveSelf();
        
        return true;
    }

    /// <summary>
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public virtual bool RemoveByBound(T item) {
        var bounds = GetBoundingBoxFromItem(item);
        return RemoveByBound(item, ref bounds);
    }

    /// <summary>
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public virtual bool RemoveSafe(T item) {
        if (Logger.IsEnabled(LogLevel.Debug)) 
            Logger.Debug("Remove safe");
        
        var node = FindChildByItem(item, out var index);
        if (node == null)
            return false;
        
        ((DynamicOctreeBase<T>) node).Objects.RemoveAt(index);
        if (node is {IsEmpty: true, AutoDeleteIfEmpty: true}) 
            node.RemoveSelf();
            
        return true;

    }

    /// <summary>
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public virtual bool RemoveAt(int index) {
        if (index < 0 || index >= Objects.Count) return false;
        Objects.RemoveAt(index);
        if (IsEmpty && AutoDeleteIfEmpty) RemoveSelf();
        return true;
    }

    /// <summary>
    /// </summary>
    /// <param name="item"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public virtual IDynamicOctree? FindChildByItemBound(T item, out int index) 
        => FindChildByItemBound(item, ref bound, out index);

    /// <summary>
    /// </summary>
    /// <param name="item"></param>
    /// <param name="bounds"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public virtual IDynamicOctree? FindChildByItemBound(T item, ref BoundingBox bounds, out int index) 
        => FindChildByItemBound(item, bounds, IsContains, this, Stack, out index);

    /// <summary>
    /// </summary>
    /// <typeparam name="E"></typeparam>
    /// <param name="item"></param>
    /// <param name="bound"></param>
    /// <param name="isContains"></param>
    /// <param name="root"></param>
    /// <param name="stackCache"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static IDynamicOctree? FindChildByItemBound<E>(
        E item,
        BoundingBox bound,
        Func<BoundingBox, BoundingBox, E, bool> isContains,
        DynamicOctreeBase<E> root,
        Stack<(int, IDynamicOctree?[])> stackCache,
        out int index
    ) {
        var idx = -1;
        IDynamicOctree? result = null;
        DynamicOctreeBase<E>? lastNode = null;
        TreeTraversal(root,
                      stackCache,
                      node => isContains(node.Bound, bound, item),
                      node => {
                          lastNode = (DynamicOctreeBase<E>) node;
                          idx = lastNode.Objects.IndexOf(item);
                          
                          result = idx != -1 
                                       ? node 
                                       : null;
                          
                      },
                      () => idx != -1);
        index = idx;
        //If not found, traverse from bottom to top to find the item.
        if (result != null)
            return result;
        
        while (lastNode != null) {
            index = lastNode.Objects.IndexOf(item);
            if (index == -1) {
                lastNode = lastNode.Parent as DynamicOctreeBase<E>;
            } else {
                result = lastNode;
                break;
            }
        }

        return result;
    }

    /// <summary>
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public static IDynamicOctree FindRoot(IDynamicOctree node) {
        while (node.Parent != null) 
            node = node.Parent;
        
        return node;
    }

#region Accessors

    /// <summary>
    ///     <see cref="IDynamicOctree.IsRoot" />
    /// </summary>
    public bool IsRoot =>
        //The root node is the only node without a parent.
        Parent is null;

    /// <summary>
    ///     <see cref="IDynamicOctree.HasChildren" />
    /// </summary>
    public bool HasChildren => ActiveNodes != 0;

    /// <summary>
    ///     <see cref="IDynamicOctree.IsEmpty" />
    /// </summary>
    public bool IsEmpty => !HasChildren && Objects.Count == 0;

#endregion
}
