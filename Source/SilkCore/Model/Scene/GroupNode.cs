/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics;
using HelixToolkit.Logger;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
/// </summary>
[DebuggerDisplay("Name={" + nameof(Name) + "}; Child Count={" + nameof(ItemsCount) + "};")]
public class GroupNode : GroupNodeBase, IHitable {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;
    private IOctreeManager? octreeManager;

    public GroupNode() {
        ChildNodeAdded += NodeGroup_OnAddChildNode;
        ChildNodeRemoved += NodeGroup_OnRemoveChildNode;
        Cleared += NodeGroup_OnClear;
    }

    public IOctreeManager? OctreeManager {
        get => octreeManager;
        set {
            var old = octreeManager;
            if (Set(ref octreeManager, value)) {
                old?.Clear();
                if (octreeManager != null)
                    foreach (var item in ItemsInternal)
                        octreeManager.AddPendingItem(item);
            }
        }
    }

    /// <summary>
    ///     Gets the octree in OctreeManager.
    /// </summary>
    /// <value>
    ///     The octree.
    /// </value>
    public IOctreeBasic? Octree => OctreeManager?.Octree;

    private void NodeGroup_OnClear(object? sender, OnChildNodeChangedArgs e) {
        OctreeManager?.Clear();
        OctreeManager?.RequestRebuild();
    }

    private void NodeGroup_OnRemoveChildNode(object? sender, OnChildNodeChangedArgs e) 
        => OctreeManager?.RemoveItem(e);

    private void NodeGroup_OnAddChildNode(object? sender, OnChildNodeChangedArgs e) 
        => OctreeManager?.AddPendingItem(e);

    /// <summary>
    ///     Updates the not render.
    /// </summary>
    /// <param name="context">The context.</param>
    public override void UpdateNotRender(RenderContext context) {
        base.UpdateNotRender(context);
        if (OctreeManager != null) {
            OctreeManager.ProcessPendingItems();
            if (OctreeManager.RequestUpdateOctree) OctreeManager?.RebuildTree(ItemsInternal);
        }
    }

    /// <summary>
    ///     Called when [hit test].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="totalModelMatrix">The total model matrix.</param>
    /// <param name="hits">The hits.</param>
    /// <returns></returns>
    protected override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    ) {
        bool isHit;
        if (octreeManager != null) {
            isHit = octreeManager.HitTest(context, WrapperSource, totalModelMatrix, ref hits);
            if (isHit && Logger.IsEnabled(LogLevel.Trace))
                Logger.Verbose("Octree hit test, hit at {Value0}", hits[0].PointHit);
        } else {
            isHit = base.OnHitTest(context, totalModelMatrix, ref hits);
        }

        return isHit;
    }
}
