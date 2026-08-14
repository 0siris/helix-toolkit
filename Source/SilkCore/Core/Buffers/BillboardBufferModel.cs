/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Core;

/// <summary>
/// </summary>
/// <typeparam name="VertexStruct">The type of the ertex structure.</typeparam>
public abstract class BillboardBufferModel<VertexStruct> : GeometryBufferModel, IBillboardBufferModel
    where VertexStruct : unmanaged {
    private static readonly VertexStruct[] EmptyVerts = [];

    private TextureModel texture;

    /// <summary>
    ///     Use the shared texture resource proxy
    /// </summary>
    private ShaderResourceViewProxy textureView;

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
    ///     Gets the texture view.
    /// </summary>
    /// <value>
    ///     The texture view.
    /// </value>
    public ShaderResourceViewProxy TextureView => textureView;

    /// <summary>
    ///     Gets or sets the type.
    /// </summary>
    /// <value>
    ///     The type.
    /// </value>
    public BillboardType Type { get; private set; }

    /// <summary>
    ///     Called when [create index buffer].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="buffer">The buffer.</param>
    /// <param name="geometry">The geometry.</param>
    /// <param name="deviceResources">The device resources.</param>
    protected override void OnCreateIndexBuffer(
        DeviceContextProxy context,
        IElementsBufferProxy buffer,
        Geometry3D? geometry,
        IDeviceResources deviceResources
    ) { }

    /// <summary>
    ///     Called when [create vertex buffer].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="buffer">The buffer.</param>
    /// <param name="geometry">The geometry.</param>
    /// <param name="deviceResources">The device resources.</param>
    /// <param name="bufferIndex"></param>
    protected override void OnCreateVertexBuffer(
        DeviceContextProxy context,
        IElementsBufferProxy buffer,
        int bufferIndex,
        Geometry3D? geometry,
        IDeviceResources deviceResources
    ) {
        if (geometry is IBillboardText billboardGeometry) {
            billboardGeometry.DrawTexture(deviceResources);
            if (billboardGeometry.BillboardVertices != null && billboardGeometry.BillboardVertices.Count > 0) {
                Type = billboardGeometry.Type;
                buffer.UploadDataToBuffer(context,
                                          billboardGeometry.BillboardVertices,
                                          billboardGeometry.BillboardVertices.Count,
                                          0,
                                          geometry.PreDefinedVertexCount);
                if (texture != billboardGeometry.Texture) {
                    texture = billboardGeometry.Texture;
                    var newView = texture == null
                                      ? null
                                      : deviceResources.MaterialTextureManager.Register(texture);
                    RemoveAndDispose(ref textureView);
                    textureView = newView;
                }
            } else {
                RemoveAndDispose(ref textureView);
                texture = null;
                buffer.UploadDataToBuffer(context, EmptyVerts, 0);
            }
        }
    }

    protected override void OnDispose(bool disposeManagedResources) {
        RemoveAndDispose(ref textureView);
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
