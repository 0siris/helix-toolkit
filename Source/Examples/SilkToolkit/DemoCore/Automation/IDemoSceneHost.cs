// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDemoSceneHost.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DemoCore.Automation;

using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.Wpf.SharpDX.Controls;
using HelixToolkit.Wpf.SharpDX.Element3D;

/// <summary>
///     Scene-root adapter owned by the demo. Demos without a group node pass no host.
/// </summary>
public interface IDemoSceneHost {
    /// <summary>
    ///     Clears the scene and disposes removed nodes.
    /// </summary>
    void Clear();

    /// <summary>
    ///     Adds a scene node to the scene root.
    /// </summary>
    /// <param name="node">The node to add.</param>
    void Add(SceneNode node);

    /// <summary>
    ///     Gets the current scene roots.
    /// </summary>
    /// <returns>The roots.</returns>
    IReadOnlyList<SceneNode> GetRoots();
}

/// <summary>
///     Default <see cref="IDemoSceneHost" /> over a <see cref="SceneNodeGroupModel3D" /> (FileLoadDemo pattern).
/// </summary>
public sealed class ViewportSceneHost : IDemoSceneHost {
    /// <summary>
    ///     The viewport owning the group model.
    /// </summary>
    private readonly Viewport3DX viewport;

    /// <summary>
    ///     The group model holding the scene.
    /// </summary>
    private readonly SceneNodeGroupModel3D groupModel;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ViewportSceneHost" /> class.
    /// </summary>
    /// <param name="viewport">The viewport.</param>
    /// <param name="groupModel">The group model.</param>
    public ViewportSceneHost(Viewport3DX viewport, SceneNodeGroupModel3D groupModel) {
        ArgumentNullException.ThrowIfNull(viewport);
        ArgumentNullException.ThrowIfNull(groupModel);
        this.viewport = viewport;
        this.groupModel = groupModel;
    }

    /// <summary>
    ///     Gets the viewport owning the group model. Reserved for future viewport-scoped operations.
    /// </summary>
    public Viewport3DX Viewport => viewport;

    /// <inheritdoc />
    public void Clear() {
        var removed = groupModel.GroupNode.Items.ToArray();
        groupModel.Clear(false);
        _ = Task.Run(() => {
            foreach (var node in removed) {
                node.Dispose();
            }
        });
    }

    /// <inheritdoc />
    public void Add(SceneNode node) {
        ArgumentNullException.ThrowIfNull(node);
        groupModel.AddNode(node);
    }

    /// <inheritdoc />
    public IReadOnlyList<SceneNode> GetRoots() => [groupModel.GroupNode];
}
