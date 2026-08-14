/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.ComponentModel;
using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Core;

/// <summary>
///     General Geometry Buffer Model.
/// </summary>
public abstract class GeometryBufferModel : DisposeObject, IGuid, IGeometryBufferModel {
    private static readonly IElementsBufferProxy[] EmptyBuffers = [];
    private static readonly VertexBufferBinding[] EmptyBinding = [];

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

    protected VertexBufferBinding[] VertexBufferBindings { get; private set; } = EmptyBinding;
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
    ///     Attaches the buffers.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="vertexBufferStartSlot">The vertex buffer slot.</param>
    /// <param name="deviceResources">The device resources.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AttachBuffers(
        DeviceContextProxy context,
        ref int vertexBufferStartSlot,
        IDeviceResources deviceResources
    ) {
        UpdateBuffers(context, deviceResources);
        return OnAttachBuffer(context, ref vertexBufferStartSlot);
    }

    /// <summary>
    ///     Updates the buffers.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="deviceResources">The device resources.</param>
    /// <returns></returns>
    public virtual bool UpdateBuffers(DeviceContextProxy context, IDeviceResources deviceResources) {
        var bufferUpdated = false;
        if (VertexChanged != 0) {
            lock (VertexBuffer) {
                var updateVBinding = false;
                if (VertexChanged != 0)
                    for (var i = 0; i < VertexBuffer.Length && VertexChanged != 0; ++i)
                        if ((VertexChanged & (1u << i)) != 0) {
                            if (VertexBuffer[i] is { } buffer)
                                OnCreateVertexBuffer(context, buffer, i, Geometry, deviceResources);

                            // Thread-safe bit clearing: Clear the flag for buffer index i after it has been updated
                            // Uses Interlocked.And to atomically clear bit i in VertexChanged without race conditions
                            Interlocked.And(ref VertexChanged, ~(1u << i));
                            updateVBinding = true;
                        }

                if (updateVBinding) {
                    VertexBufferBindings = OnCreateVertexBufferBinding();
                    VertexBufferUpdated?.Invoke(this, EventArgs.Empty);
                    bufferUpdated = true;
                }
            }
        }

        if (IndexChanged && IndexBuffer != null)
            lock (IndexBuffer) {
                if (IndexChanged) {
                    OnCreateIndexBuffer(context, IndexBuffer, Geometry, deviceResources);
                    bufferUpdated = true;
                }

                IndexChanged = false;
                IndexBufferUpdated?.Invoke(this, EventArgs.Empty);
            }

        if (bufferUpdated && Geometry is {IsTransient: true})
            Geometry.ClearAllGeometryData();
        
        return bufferUpdated;
    }

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
    protected virtual bool IsVertexBufferChanged(string propertyName, int vertexBufferIndex) =>
        propertyName.Equals(Geometry3D.VertexBuffer, StringComparison.Ordinal) ||
        propertyName.Equals(nameof(Geometry3D.Positions), StringComparison.Ordinal);

    /// <summary>
    ///     Determines whether [is index buffer changed] [the specified property name].
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns>
    ///     <c>true</c> if [is index buffer changed] [the specified property name]; otherwise, <c>false</c>.
    /// </returns>
    protected virtual bool IsIndexBufferChanged(string propertyName) =>
        propertyName.Equals(Geometry3D.TriangleBuffer, StringComparison.Ordinal) ||
        propertyName.Equals(nameof(Geometry3D.Indices), StringComparison.Ordinal);

    protected virtual VertexBufferBinding[] OnCreateVertexBufferBinding() => 
    [.. VertexBuffer.Select(x =>
            x is not null
                ? new VertexBufferBinding(x.Buffer, x.StructureSize, x.Offset)
                : new VertexBufferBinding())];

    /// <summary>
    ///     Called when [create vertex buffer].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="buffer">The buffer.</param>
    /// <param name="geometry">The geometry.</param>
    /// <param name="deviceResources">The device resources.</param>
    /// <param name="bufferIndex"></param>
    protected abstract void OnCreateVertexBuffer(
        DeviceContextProxy context,
        IElementsBufferProxy buffer,
        int bufferIndex,
        Geometry3D? geometry,
        IDeviceResources deviceResources
    );

    /// <summary>
    ///     Called when [create index buffer].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="buffer">The buffer.</param>
    /// <param name="geometry">The geometry.</param>
    /// <param name="deviceResources">The device resources.</param>
    protected abstract void OnCreateIndexBuffer(
        DeviceContextProxy context,
        IElementsBufferProxy buffer,
        Geometry3D? geometry,
        IDeviceResources deviceResources
    );

    /// <summary>
    ///     Called when [attach buffer].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="vertexBufferStartSlot">
    ///     The vertex buffer start slot. It will be changed to the next available slot after
    ///     binding
    /// </param>
    /// <returns></returns>
    protected virtual bool OnAttachBuffer(DeviceContextProxy context, ref int vertexBufferStartSlot) {
        if (VertexBuffer.Length > 0) {
            if (VertexBuffer.Length == VertexBufferBindings.Length) {
                context.SetVertexBuffers(vertexBufferStartSlot, VertexBufferBindings);
                vertexBufferStartSlot += VertexBuffer.Length;
            } else {
                return false;
            }
        }

        if (IndexBuffer != null)
            context.SetIndexBuffer(IndexBuffer.Buffer, Format.FormatR32Uint, IndexBuffer.Offset);
        else
            context.SetIndexBuffer(null, Format.FormatUnknown, 0);
        context.PrimitiveTopology = Topology;
        return true;
    }


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
        VertexBufferBindings = EmptyBinding;
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