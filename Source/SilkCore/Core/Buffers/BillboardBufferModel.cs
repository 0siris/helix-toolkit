using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Utilities;
/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Native;

namespace HelixToolkit.SharpDX.Core.Core;

/// <summary>
/// </summary>
/// <typeparam name="VertexStruct">The type of the ertex structure.</typeparam>
public abstract class BillboardBufferModel<VertexStruct> : GeometryBufferModel, IBillboardBufferModel
    where VertexStruct : unmanaged {
    private static readonly VertexStruct[] EmptyVerts = [];

    private TextureModel? texture;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BillboardBufferModel{VertexStruct}" /> class.
    /// </summary>
    /// <param name="structSize">Size of the structure.</param>
    /// <param name="dynamic"></param>
    public BillboardBufferModel(int structSize, bool dynamic = false)
        : base(PrimitiveTopology.PointList,
            dynamic
                ? new DynamicBufferProxy(structSize, BindFlags.VertexBuffer)
                : new ImmutableBufferProxy(structSize, BindFlags.VertexBuffer),
            null) { }

    /// <summary>
    ///     Gets or sets the type.
    /// </summary>
    /// <value>
    ///     The type.
    /// </value>
    public BillboardType Type { get; private set; }

    protected override void OnDispose(bool disposeManagedResources) {
        base.OnDispose(disposeManagedResources);
    }
}

/// <summary>
/// </summary>
public sealed class DefaultBillboardBufferModel : BillboardBufferModel<BillboardVertex> {
    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultBillboardBufferModel" /> class.
    /// </summary>
    public DefaultBillboardBufferModel() : base(BillboardVertex.SizeInBytes) { }

    /// <summary>Prepares the current geometry before its texture or vertices are consumed.</summary>
    /// <returns>The initialized geometry owning both the texture and vertex generation.</returns>
    internal BillboardBase PrepareForD3D12() {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (Geometry is not BillboardBase geometry)
            throw new InvalidOperationException("The buffer model must contain billboard geometry");
        if (!geometry.TryPrepareVerticesForD3D12())
            geometry.DrawTexture(EffectsManager
                ?? throw new InvalidOperationException("Billboard geometry requires an effects manager"));
        if (!geometry.IsInitialized)
            throw new InvalidOperationException("Billboard geometry could not be initialized");
        return geometry;
    }
}

/// <summary>
/// </summary>
public sealed class DynamicBillboardBufferModel : BillboardBufferModel<BillboardVertex> {
    /// <summary>
    ///     Initializes a new instance of the <see cref="DynamicBillboardBufferModel" /> class.
    /// </summary>
    public DynamicBillboardBufferModel() : base(BillboardVertex.SizeInBytes, true) { }
}
