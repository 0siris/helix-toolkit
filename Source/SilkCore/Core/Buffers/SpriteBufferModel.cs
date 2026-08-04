using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Core;

public sealed class Sprite2DBufferModel : DisposeObject, IGUID, IAttachableBufferModel {
    private IElementsBufferProxy indexBuffer = new DynamicBufferProxy(sizeof(int), BindFlags.IndexBuffer);
    public int SpriteCount;

    private DynamicBufferProxy vertextBuffer = new(SpriteStruct.SizeInBytes, BindFlags.VertexBuffer);
    public SpriteStruct[]? Sprites { get; set; }

    public int[]? Indices { get; set; }

    public int IndexCount { get; set; }

    public PrimitiveTopology Topology { get; set; } = PrimitiveTopology.TriangleList;

    public IElementsBufferProxy[] VertexBuffer => [vertextBuffer];

    public IEnumerable<int> VertexStructSize => [vertextBuffer.StructureSize];

    public IElementsBufferProxy IndexBuffer => indexBuffer;

    public Sprite2DBufferModel(){}
    
    public bool AttachBuffers(
        DeviceContextProxy context,
        ref int vertexBufferStartSlot,
        IDeviceResources deviceResources
    ) {
        if (!UpdateBuffers(context, deviceResources)) 
            return false;
        
        context.SetVertexBuffers(0,
                                 new VertexBufferBinding(vertextBuffer.Buffer,
                                                         vertextBuffer.StructureSize,
                                                         vertextBuffer.Offset));
        context.SetIndexBuffer(IndexBuffer.Buffer, Format.FormatR32Uint, IndexBuffer.Offset);
        return true;

    }

    public bool UpdateBuffers(DeviceContextProxy context, IDeviceResources deviceResources) {
        if (SpriteCount == 0 || IndexCount == 0 || Sprites == null || Indices == null ||
            Sprites.Length < SpriteCount || Indices.Length < IndexCount) 
            return false;
        
        vertextBuffer.UploadDataToBuffer(context, Sprites, SpriteCount);
        IndexBuffer.UploadDataToBuffer(context, Indices, IndexCount);
        return true;
    }

    public Guid GUID { get; } = Guid.NewGuid();

    protected override void OnDispose(bool disposeManagedResources) {
        RemoveAndDispose(ref vertextBuffer);
        VertexBuffer[0] = null;
        RemoveAndDispose(ref indexBuffer);
        base.OnDispose(disposeManagedResources);
    }
}