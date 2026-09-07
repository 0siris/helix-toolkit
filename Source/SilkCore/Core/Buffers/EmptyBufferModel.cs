/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core.Buffers;

/// <summary>
/// </summary>
public sealed class EmptyGeometryBufferModel : IGeometryBufferModel {
    public static readonly IGeometryBufferModel Empty = new EmptyGeometryBufferModel();

    /// <summary>
    ///     Gets or sets the geometry.
    /// </summary>
    /// <value>
    ///     The geometry.
    /// </value>
    public Geometry3D? Geometry { get; set; }

    /// <summary>
    ///     Gets the unique identifier.
    /// </summary>
    /// <value>
    ///     The unique identifier.
    /// </value>
    public Guid Guid { get; } = Guid.NewGuid();

    /// <summary>
    ///     Gets the index buffer.
    /// </summary>
    /// <value>
    ///     The index buffer.
    /// </value>
    public IElementsBufferProxy? IndexBuffer => null;

    /// <summary>
    ///     Gets or sets the topology.
    /// </summary>
    /// <value>
    ///     The topology.
    /// </value>
    public PrimitiveTopology Topology {
        get => PrimitiveTopology.Undefined;
        set { }
    }

    /// <summary>
    ///     Gets the vertex buffer.
    /// </summary>
    /// <value>
    ///     The vertex buffer.
    /// </value>
    public IElementsBufferProxy[] VertexBuffer { get; } = [];

    /// <summary>
    ///     Gets the size of the vertex structure.
    /// </summary>
    /// <value>
    ///     The size of the vertex structure.
    /// </value>
    public IEnumerable<int> VertexStructSize {
        get { yield return 0; }
    }

    /// <summary>
    ///     Gets or sets the effects manager.
    /// </summary>
    /// <value>
    ///     The effects manager.
    /// </value>
    public IEffectsManager? EffectsManager { get; set; }

    /// <summary>
    ///     Releases unmanaged and - optionally - managed resources.
    /// </summary>
    public void Dispose() { }

    /// <summary>
    ///     Attaches this instance.
    /// </summary>
    public void Attach() { }


    /// <summary>
    ///     Detaches this instance.
    /// </summary>
    public void Detach() { }
    public event EventHandler? VertexBufferUpdated;
    public event EventHandler? IndexBufferUpdated;
}
