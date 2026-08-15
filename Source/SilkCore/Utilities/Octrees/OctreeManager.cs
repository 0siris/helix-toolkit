// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OctreeManager.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


using System.Diagnostics.CodeAnalysis;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;

namespace HelixToolkit.SharpDX.Core.Utilities;
/// <summary>
/// </summary>
public abstract class OctreeManagerBase : ObservableObject, IOctreeManager {
    /// <summary>
    ///     The m octree
    /// </summary>
    protected BoundableNodeOctree? MOctree;

    private volatile bool mRequestUpdateOctree;

    /// <summary>
    ///     Occurs when [on octree created].
    /// </summary>
    public event EventHandler<OctreeArgs>? OnOctreeCreated;

    /// <summary>
    ///     Gets or sets the octree.
    /// </summary>
    /// <value>
    ///     The octree.
    /// </value>
    public IOctreeBasic? Octree {
        get;
        protected set {
            if (Set(ref field, value)) 
                OnOctreeCreated?.Invoke(this, new OctreeArgs(value));
        }
    }

    /// <summary>
    ///     Gets or sets the parameter.
    /// </summary>
    /// <value>
    ///     The parameter.
    /// </value>
    public OctreeBuildParameter Parameter { get; set; } = new();

    /// <summary>
    ///     Gets or sets a value indicating whether this <see cref="OctreeManagerBase" /> is enabled.
    /// </summary>
    /// <value>
    ///     <c>true</c> if enabled; otherwise, <c>false</c>.
    /// </value>
    public bool Enabled {
        get;
        set {
            field = value;
            if (!field) 
                Clear();
        }
    } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether [request update octree].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [request update octree]; otherwise, <c>false</c>.
    /// </value>
    public bool RequestUpdateOctree {
        get => mRequestUpdateOctree;
        protected set => mRequestUpdateOctree = value;
    }

    /// <summary>
    ///     Adds the pending item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns></returns>
    public abstract bool AddPendingItem(SceneNode? item);

    /// <summary>
    ///     Clears this instance.
    /// </summary>
    public abstract void Clear();

    /// <summary>
    ///     Rebuilds the tree. 
    /// </summary>
    /// <param name="items">The items.</param>
    [MemberNotNull(nameof(Octree))]
    public abstract void RebuildTree(IEnumerable<SceneNode>? items);

    /// <summary>
    ///     Removes the item.
    /// </summary>
    /// <param name="item">The item.</param>
    public abstract void RemoveItem(SceneNode? item);

    /// <summary>
    ///     Requests the rebuild.
    /// </summary>
    public abstract void RequestRebuild();

    /// <summary>
    ///     Processes the pending items.
    /// </summary>
    public abstract void ProcessPendingItems();

    /// <summary>
    ///     Normal hit test from top to bottom
    /// </summary>
    /// <param name="context"></param>
    /// <param name="model"></param>
    /// <param name="modelMatrix"></param>
    /// <param name="hits"></param>
    /// <returns></returns>
    public virtual bool HitTest(
        HitTestContext context,
        object model,
        Matrix modelMatrix,
        ref List<HitTestResult> hits
    ) {
        if (Octree is not { } octree)
            return false;

        return octree.HitTest(context, model, null, modelMatrix, ref hits);
    }
}
