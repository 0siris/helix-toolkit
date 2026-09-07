/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core.Buffers;


/// <summary>
/// </summary>
public sealed class BoneSkinPreComputeBufferModel : DisposeObject, IAttachableBufferModel, IBoneSkinPreComputehBufferModel {
    private IBoneSkinMeshBufferModel meshBuffer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BoneSkinPreComputeBufferModel" /> class.
    /// </summary>
    /// <param name="meshBuffer">The mesh buffer.</param>
    /// <param name="structSize">Size of the structure.</param>
    public BoneSkinPreComputeBufferModel(IBoneSkinMeshBufferModel meshBuffer, int structSize) {
        this.meshBuffer = meshBuffer;
    }

    public PrimitiveTopology Topology {
        get => meshBuffer.Topology;
        set => meshBuffer.Topology = value;
    }

    public IElementsBufferProxy?[] VertexBuffer => meshBuffer.VertexBuffer;

    public IEnumerable<int> VertexStructSize => VertexBuffer.Select(x => x.StructureSize);

    public IElementsBufferProxy? IndexBuffer => meshBuffer.IndexBuffer;

    public Guid Guid { get; } = new();

    public bool CanPreCompute => meshBuffer.BoneIdBuffer.ElementCount != 0;

    /// <summary>
    ///     Gets the existing default mesh model used as the renderer-independent DX12 source.
    /// </summary>
    internal DefaultMeshGeometryBufferModel SourceMeshBuffer => meshBuffer as DefaultMeshGeometryBufferModel
        ?? throw new InvalidOperationException("Bone skinning requires a default mesh buffer model.");

    protected override void OnDispose(bool disposeManagedResources) {
        var meshBufferToDispose = meshBuffer;
        RemoveAndDispose(ref meshBufferToDispose);
        base.OnDispose(disposeManagedResources);
    }
}
