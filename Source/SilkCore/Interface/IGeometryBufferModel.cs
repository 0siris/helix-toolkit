/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Interface;

public interface IAttachableBufferModel : IGuid, IDisposable {
    /// <summary>
    ///     Gets or sets the topology.
    /// </summary>
    /// <value>
    ///     The topology.
    /// </value>
    PrimitiveTopology Topology { get; set; }

    /// <summary>
    ///     Gets the vertex buffer.
    /// </summary>
    /// <value>
    ///     The vertex buffer.
    /// </value>
    IElementsBufferProxy?[] VertexBuffer { get; } //TODO check why we allow null values in the array

    /// <summary>
    ///     Gets the size of the vertex structure.
    /// </summary>
    /// <value>
    ///     The size of the vertex structure.
    /// </value>
    IEnumerable<int> VertexStructSize { get; }

    /// <summary>
    ///     Gets the index buffer.
    /// </summary>
    /// <value>
    ///     The index buffer.
    /// </value>
    IElementsBufferProxy? IndexBuffer { get; }
}

/// <summary>
/// </summary>
public interface IGeometryBufferModel : IAttachableBufferModel {
    /// <summary>
    ///     Gets or sets the effects manager.
    /// </summary>
    /// <value>
    ///     The effects manager.
    /// </value>
    IEffectsManager? EffectsManager { get; set; }

    /// <summary>
    ///     Gets or sets the geometry.
    /// </summary>
    /// <value>
    ///     The geometry.
    /// </value>
    Geometry3D? Geometry { get; set; }

    event EventHandler? VertexBufferUpdated;
    event EventHandler? IndexBufferUpdated;
}

/// <summary>
/// </summary>
public interface IBillboardBufferModel : IDisposable {

    /// <summary>
    ///     Gets the billboard type.
    /// </summary>
    /// <value>
    ///     The type.
    /// </value>
    BillboardType Type { get; }
}

/// <summary>
/// </summary>
public interface IBoneSkinMeshBufferModel : IGeometryBufferModel {
    IElementsBufferProxy BoneIdBuffer { get; }

    event EventHandler? BoneIdBufferUpdated;
}

/// <summary>
/// </summary>
public interface IBoneSkinPreComputehBufferModel {
    bool CanPreCompute { get; }
}
