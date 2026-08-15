using System.Runtime.CompilerServices;
using HelixToolkit.Logger;
using HelixToolkit.SharpDX.Core.Model.Scene;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core.Utilities;

/// <summary>
///     Use to create geometryModel3D octree for groups. Each ItemsModel3D must has its own manager, do not share between
///     two ItemsModel3D
/// </summary>
public sealed class GroupNodeGeometryBoundOctreeManager : OctreeManagerBase {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;
    private readonly Lock lockObj = new();

    private readonly HashSet<SceneNode> nonBoundableItems = [];

    private readonly HashSet<SceneNode> pendingItems = [];

    private void UpdateOctree(BoundableNodeOctree? tree) {
        Octree = tree;
        MOctree = tree;
    }

    /// <summary>
    ///     Rebuilds the tree.
    /// </summary>
    /// <param name="items">The items.</param>
    public override void RebuildTree(IEnumerable<SceneNode>? items) {
        lock (lockObj) {
            RequestUpdateOctree = false;
            if (Enabled) {
                if (items is null) {
                    Clear();
                    return;
                }

                var nodes = items.Where(x => x.HasBound).ToList();
                if (nodes.Count == 0)
                    return;
                
                UpdateOctree(RebuildOctree(nodes));
                if (Octree == null)
                    RequestRebuild();
                else
                    foreach (var item in nodes)
                        nonBoundableItems.Add(item);
            } else {
                Clear();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SubscribeBoundChangeEvent(SceneNode item) {
        item.TransformBoundChanged -= Item_OnBoundChanged;
        item.TransformBoundChanged += Item_OnBoundChanged;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UnsubscribeBoundChangeEvent(SceneNode item) {
        item.TransformBoundChanged -= Item_OnBoundChanged;
    }

    private void Item_OnBoundChanged(object? sender, BoundChangeArgs<BoundingBox> args) {
        if (sender is SceneNode item)
            pendingItems.Add(item);
    }

    /// <summary>
    /// </summary>
    public override void ProcessPendingItems() {
        lock (lockObj) {
            foreach (var item in pendingItems) {
                if (!item.HasBound) {
                    nonBoundableItems.Add(item);
                    continue;
                }

                if (MOctree == null || !item.IsAttached) {
                    UnsubscribeBoundChangeEvent(item);
                    continue;
                }

                var tree = MOctree;
                var node = tree.FindItemByGuid(item.Guid, item, out var index);
                var rootAdd = true;
                if (node is BoundableNodeOctree geoNode) {
                    UpdateOctree(null);
                    var itemBounds = item.BoundsWithTransform;
                    
                    if (geoNode.Bound.Contains(ref itemBounds) == ContainmentType.Contains) {
                        if (geoNode.PushExistingToChild(index)) tree = tree.Shrink() as BoundableNodeOctree;
                        rootAdd = false;
                    } else {
                        geoNode.RemoveAt(index, tree);
                    }

                    UpdateOctree(tree);
                } else if (node is not null) {
                    tree.RemoveByGuid(item.Guid, item, tree);
                }

                if (rootAdd) 
                    AddItem(item);
            }

            pendingItems.Clear();
        }
    }

    private BoundableNodeOctree? RebuildOctree(IEnumerable<SceneNode>? items) {
        Clear();
        if (items == null) return null;
        var tree = new BoundableNodeOctree([.. items], Parameter);
        tree.BuildTree();
        if (tree.TreeBuilt) {
            foreach (var item in items)
                SubscribeBoundChangeEvent(item);
        }

        return tree.TreeBuilt 
                   ? tree 
                   : null;
        
    }

    /// <summary>
    ///     Adds the pending item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns></returns>
    public override bool AddPendingItem(SceneNode? item) {
        lock (lockObj) {
            if (!Enabled || item == null)
                return false;
            
            if (item.HasBound) {
                item.TransformBoundChanged -= GeometryModel3DOctreeManager_OnBoundInitialized;
                item.TransformBoundChanged += GeometryModel3DOctreeManager_OnBoundInitialized;
                pendingItems.Add(item);
            } else {
                nonBoundableItems.Add(item);
            }

            //if (item.Bounds != ZeroBound)
            //{
            //    AddItem(item);
            //}
            return true;

        }
    }

    private void GeometryModel3DOctreeManager_OnBoundInitialized(
        object? sender,
        BoundChangeArgs<BoundingBox> args
    ) {
        if (sender is SceneNode item) {
            item.TransformBoundChanged -= GeometryModel3DOctreeManager_OnBoundInitialized;
            AddItem(item);
        } else {
            LoggerLib.Logger.Warn("Invalid sender type");
        }

    }

    private void AddItem(SceneNode? item) {
        if (!Enabled || item == null)
            return;
        
        
        if (item.HasBound) {
            var tree = MOctree;
            UpdateOctree(null);
            if (tree is null) {
                RequestRebuild();
            } else {
                var succeed = true;
                var counter = 0;
                while (!tree.Add(item)) {
                    var direction = item.Bounds.Minimum + item.Bounds.Maximum
                                    - (tree.Bound.Minimum + tree.Bound.Maximum);
                    if (tree.Expand(ref direction) is not BoundableNodeOctree expandedTree) {
                        succeed = false;
                        break;
                    }
                    tree = expandedTree;
                    ++counter;
                    if (counter > 10) {
#if DEBUG
                        throw new Exception("Expand tree failed");
#else
                            succeed = false;
                            break;
#endif
                    }
                }

                if (succeed) {
                    UpdateOctree(tree);
                    SubscribeBoundChangeEvent(item);
                } else {
                    RequestRebuild();
                }
            }
        } else {
            nonBoundableItems.Add(item);
        }
    }

    /// <summary>
    ///     Removes the item.
    /// </summary>
    /// <param name="item">The item.</param>
    public override void RemoveItem(SceneNode? item) {
        if (!Enabled || Octree == null || item == null)
            return;
        
        lock (lockObj) {
            if (item.HasBound) {
                if (MOctree is not { } tree)
                    return;
                UpdateOctree(null);
                item.TransformBoundChanged -= GeometryModel3DOctreeManager_OnBoundInitialized;
                UnsubscribeBoundChangeEvent(item);
                if (!tree.RemoveByBound(item)) {
                    if (Logger.IsEnabled(LogLevel.Debug)) 
                        Logger.Debug("Remove failed");
                } else {
                    tree = tree.Shrink() as BoundableNodeOctree;
                }

                UpdateOctree(tree);
            } else {
                nonBoundableItems.Remove(item);
            }
        }
    }

    /// <summary>
    ///     Clears this instance.
    /// </summary>
    public override void Clear() {
        lock (lockObj) {
            RequestUpdateOctree = false;
            UpdateOctree(null);
            nonBoundableItems.Clear();
        }
    }

    /// <summary>
    ///     Requests the rebuild.
    /// </summary>
    public override void RequestRebuild() {
        lock (lockObj) {
            Clear();
            RequestUpdateOctree = true;
        }
    }

    public override bool HitTest(
        HitTestContext context,
        object model,
        Matrix modelMatrix,
        ref List<HitTestResult> hits
    ) {
        if (Octree == null) 
            return false;
        
        var hit = Octree.HitTest(context, model, null, modelMatrix, ref hits);
       
        foreach (var item in nonBoundableItems) 
            hit |= item.HitTest(context, ref hits);
        
        return hit;
    }
}
