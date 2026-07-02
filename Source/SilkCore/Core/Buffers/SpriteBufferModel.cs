using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core
{
    namespace Core
    {
        public sealed class Sprite2DBufferModel : DisposeObject, IGUID, IAttachableBufferModel
        {
            private IElementsBufferProxy indexBuffer;
            public int SpriteCount;

            private DynamicBufferProxy vertextBuffer;

            public Sprite2DBufferModel()
            {
                vertextBuffer = new DynamicBufferProxy(SpriteStruct.SizeInBytes, BindFlags.VertexBuffer);
                VertexBuffer[0] = vertextBuffer;
                indexBuffer = new DynamicBufferProxy(sizeof(int), BindFlags.IndexBuffer);
            }

            public SpriteStruct[] Sprites { get; set; }

            public int[] Indices { get; set; }

            public int IndexCount { get; set; }

            public PrimitiveTopology Topology { get; set; } = PrimitiveTopology.TriangleList;

            public IElementsBufferProxy[] VertexBuffer { get; } = new DynamicBufferProxy[1];

            public IEnumerable<int> VertexStructSize
            {
                get { return VertexBuffer.Select(x => x != null ? x.StructureSize : 0); }
            }

            public IElementsBufferProxy IndexBuffer => indexBuffer;

            public bool AttachBuffers(DeviceContextProxy context, ref int vertexBufferStartSlot,
                IDeviceResources deviceResources)
            {
                if (UpdateBuffers(context, deviceResources))
                {
                    context.SetVertexBuffers(0,
                        new VertexBufferBinding(vertextBuffer.Buffer, vertextBuffer.StructureSize,
                            vertextBuffer.Offset));
                    context.SetIndexBuffer(IndexBuffer.Buffer, Format.FormatR32Uint, IndexBuffer.Offset);
                    return true;
                }

                return false;
            }

            public bool UpdateBuffers(DeviceContextProxy context, IDeviceResources deviceResources)
            {
                if (SpriteCount == 0 || IndexCount == 0 || Sprites == null || Indices == null ||
                    Sprites.Length < SpriteCount || Indices.Length < IndexCount) return false;
                vertextBuffer.UploadDataToBuffer(context, Sprites, SpriteCount);
                IndexBuffer.UploadDataToBuffer(context, Indices, IndexCount);
                return true;
            }

            public Guid GUID { get; } = Guid.NewGuid();

            protected override void OnDispose(bool disposeManagedResources)
            {
                RemoveAndDispose(ref vertextBuffer);
                VertexBuffer[0] = null;
                RemoveAndDispose(ref indexBuffer);
                base.OnDispose(disposeManagedResources);
            }
        }
    }
}