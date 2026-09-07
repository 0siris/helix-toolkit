//#define OutputBuildTime

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core.Batching;

public interface IBatchedGeometry {
    Geometry3D Geometry { get; }

    Matrix ModelTransform { get; }
}

public abstract class StaticGeometryBatchingBufferBase<BatchedGeometry, VertStruct> : DisposeObject,
    IAttachableBufferModel
    where BatchedGeometry : struct, IBatchedGeometry where VertStruct : unmanaged {
    private static readonly VertStruct[] EmptyArray = [];
    private static readonly int[] EmptyIntArray = [];

    private IElementsBufferProxy? indexBuffer;
    private bool isGeometryChanged = true;

    public StaticGeometryBatchingBufferBase(
        PrimitiveTopology topology,
        IElementsBufferProxy vertexBuffer,
        IElementsBufferProxy indexBuffer
    ) {
        Topology = topology;
        VertexBuffer = [vertexBuffer];
        this.indexBuffer = indexBuffer;
    }

    public BatchedGeometry[]? Geometries {
        get;
        set {
            if (Set(ref field, value))
                InvalidateGeometries();
        }
    }

    public Guid Guid { get; } = Guid.NewGuid();

    /// <summary>
    ///     Gets or sets the vertex buffer.
    /// </summary>
    /// <value>
    ///     The vertex buffer.
    /// </value>
    public IElementsBufferProxy[] VertexBuffer { get; }

    public IEnumerable<int> VertexStructSize
        => VertexBuffer.Select(x => x.StructureSize);

    /// <summary>
    ///     Gets or sets the index buffer.
    /// </summary>
    /// <value>
    ///     The index buffer.
    /// </value>
    public IElementsBufferProxy? IndexBuffer => indexBuffer;

    /// <summary>
    ///     Gets or sets the topology.
    /// </summary>
    /// <value>
    ///     The topology.
    /// </value>
    public PrimitiveTopology Topology { get; set; }

    public event EventHandler<EventArgs>? InvalidateRender;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InvalidateGeometries() {
        isGeometryChanged = true;
        InvalidateRender?.Invoke(this, EventArgs.Empty);
    }


    protected abstract void OnFillVertArray(
        VertStruct[] array,
        int offset,
        ref BatchedGeometry geometry,
        ref Matrix transform
    );

    protected override void OnDispose(bool disposeManagedResources) {
        for (var i = 0; i < VertexBuffer.Length; ++i)
            RemoveAndDispose(ref VertexBuffer[i]);

        RemoveAndDispose(ref indexBuffer);
        base.OnDispose(disposeManagedResources);
        InvalidateRender = null;
    }
}
