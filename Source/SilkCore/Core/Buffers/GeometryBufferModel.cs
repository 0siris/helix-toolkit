/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.ComponentModel;
using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core.Buffers;

/// <summary>
///     General Geometry Buffer Model.
/// </summary>
public abstract class GeometryBufferModel : DisposeObject, IGuid, IGeometryBufferModel {
    private static readonly IElementsBufferProxy[] EmptyBuffers = [];

    private Geometry3D? geometry;

    /// <summary>
    ///     Gets or sets a value indicating whether [index changed].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [index changed]; otherwise, <c>false</c>.
    /// </value>
    protected volatile bool IndexChanged = true;

    /// <summary>
    ///     change flags
    /// </summary>
    protected volatile uint VertexChanged;
    public event EventHandler? VertexBufferUpdated;
    public event EventHandler? IndexBufferUpdated;

    /// <summary>
    ///     Gets or sets the vertex buffer.
    /// </summary>
    /// <value>
    ///     The vertex buffer.
    /// </value>
    /// TODO max len = 32, otherwise the uint:VertexChanged bitfield will collapse
    public IElementsBufferProxy?[] VertexBuffer { get; private set; } = EmptyBuffers;

    /// <summary>
    ///     Gets the size of the vertex structure.
    /// </summary>
    /// <value>
    ///     The size of the vertex structure.
    /// </value>
    public IEnumerable<int> VertexStructSize => VertexBuffer.Select(x => x?.StructureSize ?? 0);

    /// <summary>
    ///     Gets or sets the index buffer.
    /// </summary>
    /// <value>
    ///     The index buffer.
    /// </value>
    public IElementsBufferProxy? IndexBuffer { get; private set; }

    /// <summary>
    ///     Gets or sets the topology.
    /// </summary>
    /// <value>
    ///     The topology.
    /// </value>
    public PrimitiveTopology Topology { get; set; }

    /// <summary>
    ///     Gets or sets the geometry.
    /// </summary>
    /// <value>
    ///     The geometry.
    /// </value>
    public Geometry3D? Geometry {
        get => geometry;
        set {
            if (geometry == value) 
                return;
            
            geometry?.PropertyChanged -= Geometry_PropertyChanged;
            geometry = value;
            geometry?.PropertyChanged += Geometry_PropertyChanged;
            
            for (var i = 0; i < VertexBuffer.Length; ++i) {
                // Thread-safe flag setting: Mark vertex buffer i as changed by setting bit i
                // Uses Interlocked.Or to atomically set the bit without race conditions
                Interlocked.Or(ref VertexChanged, 1u << i);
            }
            
            IndexChanged = true;
            InvalidateRenderer();
        }
    }

    public IEffectsManager? EffectsManager { get; set; }

    /// <summary>
    ///     Gets the unique identifier.
    /// </summary>
    /// <value>
    ///     The unique identifier.
    /// </value>
    public Guid Guid { get; } = Guid.NewGuid();


    private void Geometry_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
        var vertChanged = false;
        for (var i = 0; i < VertexBuffer.Length; ++i)
            if (IsVertexBufferChanged(e.PropertyName, i)) {
                // Thread-safe flag setting: Set the bit at position i in VertexChanged to mark buffer i as changed
                // Uses Interlocked.Or to atomically set bit i without race conditions in multi-threaded scenarios
                Interlocked.Or(ref VertexChanged, 1u << i);
                InvalidateRenderer();
                vertChanged = true;
                break;
            }

        if (!vertChanged && IsIndexBufferChanged(e.PropertyName)) {
            IndexChanged = true;
            InvalidateRenderer();
        }
    }

    /// <summary>
    ///     Invalidates the renderer.
    /// </summary>
    protected void InvalidateRenderer() 
        => EffectsManager?.RaiseInvalidateRender();

    /// <summary>
    ///     Determines whether [is vertex buffer changed] [the specified property name].
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <param name="vertexBufferIndex"></param>
    /// <returns>
    ///     <c>true</c> if [is vertex buffer changed] [the specified property name]; otherwise, <c>false</c>.
    /// </returns>
    protected virtual bool IsVertexBufferChanged(string? propertyName, int vertexBufferIndex) =>
        string.Equals(propertyName, Geometry3D.VertexBuffer, StringComparison.Ordinal) ||
        string.Equals(propertyName, nameof(Geometry3D.Positions), StringComparison.Ordinal);

    /// <summary>
    ///     Determines whether [is index buffer changed] [the specified property name].
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns>
    ///     <c>true</c> if [is index buffer changed] [the specified property name]; otherwise, <c>false</c>.
    /// </returns>
    protected virtual bool IsIndexBufferChanged(string? propertyName) =>
        string.Equals(propertyName, Geometry3D.TriangleBuffer, StringComparison.Ordinal) ||
        string.Equals(propertyName, nameof(Geometry3D.Indices), StringComparison.Ordinal);


    /// <summary>
    ///     Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposeManagedResources">
    ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to
    ///     release only unmanaged resources.
    /// </param>
    protected override void OnDispose(bool disposeManagedResources) {
        geometry?.PropertyChanged -= Geometry_PropertyChanged;
        geometry = null;
        foreach (var t in VertexBuffer)
            t?.Dispose();

        VertexBuffer = EmptyBuffers;
        IndexBuffer?.Dispose();
        IndexBuffer = null;
        base.OnDispose(disposeManagedResources);
    }

#region Constructors

    /// <summary>
    ///     Initializes a new instance of the <see cref="GeometryBufferModel" /> class.
    /// </summary>
    /// <param name="topology">The topology.</param>
    /// <param name="vertexBuffer">The vertex buffer.</param>
    /// <param name="indexBuffer">The index buffer.</param>
    protected GeometryBufferModel(
        PrimitiveTopology topology,
        IElementsBufferProxy? vertexBuffer,
        IElementsBufferProxy? indexBuffer
    ) {
        Topology = topology;
        VertexBuffer = vertexBuffer != null 
                           ? [vertexBuffer] 
                           : EmptyBuffers;
        
        VertexChanged = 1u;
        IndexBuffer = indexBuffer;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="GeometryBufferModel" /> class.
    /// </summary>
    /// <param name="topology">The topology.</param>
    /// <param name="vertexBuffer">The vertex buffer.</param>
    /// <param name="indexBuffer">The index buffer.</param>
    protected GeometryBufferModel(
        PrimitiveTopology topology,
        IElementsBufferProxy[]? vertexBuffer,
        IElementsBufferProxy? indexBuffer
    ) {
        Topology = topology;
        if (vertexBuffer is not null) {
            for (var i = 0; i < vertexBuffer.Length; ++i) 
                Interlocked.Or(ref VertexChanged, 1u << i);
            
            VertexBuffer = vertexBuffer;
        }

        IndexBuffer = indexBuffer;
    }

#endregion
}
