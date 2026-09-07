/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core.Buffers;

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
}

/// <summary>
/// </summary>
public sealed class DynamicBillboardBufferModel : BillboardBufferModel<BillboardVertex> {
    /// <summary>
    ///     Initializes a new instance of the <see cref="DynamicBillboardBufferModel" /> class.
    /// </summary>
    public DynamicBillboardBufferModel() : base(BillboardVertex.SizeInBytes, true) { }
}
