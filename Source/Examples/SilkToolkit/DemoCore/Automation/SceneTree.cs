// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SceneTree.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DemoCore.Automation;

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Utilities;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

/// <summary>
///     JSON-serializable scene node for REST and MCP. Children nest up to the requested depth.
/// </summary>
/// <param name="Id">Stable node id (SceneNode.Guid).</param>
/// <param name="Name">Node name.</param>
/// <param name="Type">CLR type name.</param>
/// <param name="Visible">Visibility flag.</param>
/// <param name="IsRenderable">Renderable state.</param>
/// <param name="IsAttached">Attach state.</param>
/// <param name="ChildCount">Direct child count.</param>
/// <param name="BoundsMin">World bounds minimum as [x, y, z], or null without valid bounds.</param>
/// <param name="BoundsMax">World bounds maximum as [x, y, z], or null without valid bounds.</param>
/// <param name="Children">Nested children.</param>
public sealed record SceneNodeInfo(
    Guid Id,
    string Name,
    string Type,
    bool Visible,
    bool IsRenderable,
    bool IsAttached,
    int ChildCount,
    double[]? BoundsMin,
    double[]? BoundsMax,
    List<SceneNodeInfo> Children);

/// <summary>
///     Builds scene trees and resolves nodes by id. Runs on the UI thread (caller marshals via OnUiAsync).
/// </summary>
internal static class SceneTreeBuilder {
    /// <summary>
    ///     Sentinel vector marking invalid bounds (mirrors FindBoundsInternal).
    /// </summary>
    private static readonly Vector3 MaxVector = new(float.MaxValue, float.MaxValue, float.MaxValue);

    /// <summary>
    ///     Builds the tree over roots with depth and node limits.
    /// </summary>
    /// <param name="roots">The traversal roots.</param>
    /// <param name="depth">Maximum nesting depth (≥1).</param>
    /// <param name="limit">Maximum node count.</param>
    /// <returns>The root infos.</returns>
    internal static List<SceneNodeInfo> Build(IEnumerable<SceneNode> roots, int depth, int limit) {
        roots.ForceUpdateTransformsAndBounds();
        var remaining = limit;
        var result = new List<SceneNodeInfo>();
        foreach (var root in roots) {
            if (remaining <= 0) {
                break;
            }

            result.Add(ToInfo(root, depth, ref remaining));
        }

        return result;
    }

    /// <summary>
    ///     Summarizes a single node without children.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>The summary.</returns>
    internal static SceneNodeInfo Summarize(SceneNode node) {
        var remaining = 1;
        return ToInfo(node, 1, ref remaining);
    }

    /// <summary>
    ///     Finds a node by id by walking the roots.
    /// </summary>
    /// <param name="roots">The traversal roots.</param>
    /// <param name="id">The node id.</param>
    /// <returns>The node or null.</returns>
    internal static SceneNode? Find(IEnumerable<SceneNode> roots, Guid id) {
        foreach (var node in roots.Traverse()) {
            if (node.Guid == id) {
                return node;
            }
        }

        return null;
    }

    /// <summary>
    ///     Converts a node and its children to an info record.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="depth">Remaining depth.</param>
    /// <param name="remaining">Remaining node budget.</param>
    /// <returns>The info.</returns>
    private static SceneNodeInfo ToInfo(SceneNode node, int depth, ref int remaining) {
        remaining--;
        var (boundsMin, boundsMax) = BoundsOf(node);
        var info = new SceneNodeInfo(
            node.Guid,
            node.Name,
            node.GetType().Name,
            node.Visible,
            node.IsRenderable,
            node.IsAttached,
            node.ItemsCount,
            boundsMin,
            boundsMax,
            []);
        if (depth <= 1 || remaining <= 0) {
            return info;
        }

        foreach (var child in node.Items) {
            if (remaining <= 0) {
                break;
            }

            info.Children.Add(ToInfo(child, depth - 1, ref remaining));
        }

        return info;
    }

    /// <summary>
    ///     Reads world bounds guarded by the FindBoundsInternal sentinel checks.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>Min/max triples or nulls.</returns>
    private static (double[]? Min, double[]? Max) BoundsOf(SceneNode node) {
        if (node is not IBoundable boundable || !boundable.HasBound) {
            return (null, null);
        }

        var box = boundable.BoundsWithTransform;
        if (box.Maximum == box.Minimum || box.Maximum == Vector3.Zero || box.Maximum == MaxVector) {
            return (null, null);
        }

        return ([box.Minimum.X, box.Minimum.Y, box.Minimum.Z], [box.Maximum.X, box.Maximum.Y, box.Maximum.Z]);
    }
}
