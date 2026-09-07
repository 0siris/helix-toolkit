using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core.Buffers;

public sealed class Sprite2DBufferModel : DisposeObject, IGuid, IAttachableBufferModel {
    [System.Diagnostics.CodeAnalysis.AllowNull]
    private IElementsBufferProxy indexBuffer = new DynamicBufferProxy(sizeof(int), BindFlags.IndexBuffer);
    public int SpriteCount;

    [System.Diagnostics.CodeAnalysis.AllowNull]
    private DynamicBufferProxy vertextBuffer = new(SpriteStruct.SizeInBytes, BindFlags.VertexBuffer);
    public SpriteStruct[]? Sprites { get; set; }

    public int[]? Indices { get; set; }

    public int IndexCount { get; set; }

    public PrimitiveTopology Topology { get; set; } = PrimitiveTopology.TriangleList;

    public IElementsBufferProxy[] VertexBuffer => [vertextBuffer];

    public IEnumerable<int> VertexStructSize => [vertextBuffer.StructureSize];

    public IElementsBufferProxy IndexBuffer => indexBuffer;

    public Sprite2DBufferModel(){}

    public Guid Guid { get; } = Guid.NewGuid();

    protected override void OnDispose(bool disposeManagedResources) {
        RemoveAndDispose(ref vertextBuffer);
        RemoveAndDispose(ref indexBuffer);
        base.OnDispose(disposeManagedResources);
    }
}
