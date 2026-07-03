/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Render;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using SilkD3D11BufferPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Buffer>;
using SilkD3D11ResourcePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Resource>;
using SilkD3D11Texture1DPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Texture1D>;
using SilkD3D11Texture2DPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Texture2D>;
using SilkD3D11Texture3DPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Texture3D>;

namespace HelixToolkit.SharpDX.Core {
    [Flags]
    public enum BindFlags {
        None = 0,
        VertexBuffer = 1,
        IndexBuffer = 2,
        ConstantBuffer = 4,
        ShaderResource = 8,
        StreamOutput = 16,
        RenderTarget = 32,
        DepthStencil = 64,
        UnorderedAccess = 128
    }

    [Flags]
    public enum CpuAccessFlags {
        None = 0,
        Write = 0x10000,
        Read = 0x20000
    }

    public enum ResourceUsage {
        Default = 0,
        Immutable = 1,
        Dynamic = 2,
        Staging = 3
    }

    public enum DriverType {
        Unknown = 0,
        Hardware = 1,
        Reference = 2,
        Null = 3,
        Software = 4,
        Warp = 5
    }

    [Flags]
    public enum ResourceOptionFlags {
        None = 0,
        GenerateMipMaps = 1,
        Shared = 2,
        TextureCube = 4,
        DrawIndirectArguments = 16,
        BufferAllowRawViews = 32,
        BufferStructured = 64,
        ResourceClamp = 128,
        SharedKeyedmutex = 256,
        GdiCompatible = 512
    }

    public enum MapMode {
        Read = 1,
        Write = 2,
        ReadWrite = 3,
        WriteDiscard = 4,
        WriteNoOverwrite = 5
    }

    [Flags]
    public enum MapFlags {
        None = 0,
        DoNotWait = 0x100000
    }

    public struct BufferDescription {
        public int SizeInBytes;
        public BindFlags BindFlags;
        public CpuAccessFlags CpuAccessFlags;
        public ResourceOptionFlags OptionFlags;
        public ResourceUsage Usage;
        public int StructureByteStride;
    }

    public struct VertexBufferBinding {
        public VertexBufferBinding(Buffer buffer, int stride, int offset) {
            Buffer = buffer;
            Stride = stride;
            Offset = offset;
        }

        public Buffer Buffer;
        public int Stride;
        public int Offset;
    }

    public struct SampleDescription {
        public int Count;
        public int Quality;

        public SampleDescription(int count, int quality) {
            Count = count;
            Quality = quality;
        }
    }

    public struct Texture1DDescription {
        public int Width;
        public int MipLevels;
        public int ArraySize;
        public Format Format;
        public BindFlags BindFlags;
        public CpuAccessFlags CpuAccessFlags;
        public ResourceOptionFlags OptionFlags;
        public ResourceUsage Usage;
    }

    public struct Texture2DDescription {
        public int Width;
        public int Height;
        public int MipLevels;
        public int ArraySize;
        public Format Format;
        public SampleDescription SampleDescription;
        public BindFlags BindFlags;
        public CpuAccessFlags CpuAccessFlags;
        public ResourceOptionFlags OptionFlags;
        public ResourceUsage Usage;
    }

    public struct Texture3DDescription {
        public int Width;
        public int Height;
        public int Depth;
        public int MipLevels;
        public Format Format;
        public BindFlags BindFlags;
        public CpuAccessFlags CpuAccessFlags;
        public ResourceOptionFlags OptionFlags;
        public ResourceUsage Usage;
    }

    public struct RenderTargetViewDescription {
        public Format Format;
        public RenderTargetViewDimension Dimension;
        public Texture2DResource Texture2D;
        public Texture2DArrayResource Texture2DArray;

        public struct Texture2DResource {
            public int MipSlice;
        }

        public struct Texture2DArrayResource {
            public int MipSlice;
            public int FirstArraySlice;
            public int ArraySize;
        }
    }

    public struct DepthStencilViewDescription {
        public Format Format;
        public DepthStencilViewDimension Dimension;
        public DepthStencilViewFlags Flags;
        public Texture2DResource Texture2D;
        public Texture2DArrayResource Texture2DArray;

        public struct Texture2DResource {
            public int MipSlice;
        }

        public struct Texture2DArrayResource {
            public int MipSlice;
            public int FirstArraySlice;
            public int ArraySize;
        }
    }

    public struct DataBox {
        public DataBox(nint dataPointer, int rowPitch, int slicePitch) {
            DataPointer = dataPointer;
            RowPitch = rowPitch;
            SlicePitch = slicePitch;
        }

        public nint DataPointer;
        public int RowPitch;
        public int SlicePitch;
        public bool IsEmpty => DataPointer == nint.Zero;
    }

    public struct Half4 {
        public Half4(float x, float y, float z, float w) {
            X = (Half) x;
            Y = (Half) y;
            Z = (Half) z;
            W = (Half) w;
        }

        public Half X;
        public Half Y;
        public Half Z;
        public Half W;

        public static implicit operator Half4(Vector4 value) {
            return new Half4(value.X, value.Y, value.Z, value.W);
        }
    }

    public struct ResourceRegion {
        public int Left;
        public int Top;
        public int Front;
        public int Right;
        public int Bottom;
        public int Back;

        internal Box ToSilkBox() {
            return new Box {
                Left = (uint) Left,
                Top = (uint) Top,
                Front = (uint) Front,
                Right = (uint) Right,
                Bottom = (uint) Bottom,
                Back = (uint) Back
            };
        }
    }

    public sealed class DataStream : IDisposable {
        public DataStream(nint dataPointer, int length, bool canRead, bool canWrite) {
            DataPointer = dataPointer;
            Length = length;
            CanRead = canRead;
            CanWrite = canWrite;
        }

        public nint DataPointer { get; }

        public int Length { get; }

        public int Position { get; set; }

        public bool CanRead { get; }

        public bool CanWrite { get; }

        public bool IsDisposed { get; private set; }

        public void Dispose() {
            IsDisposed = true;
        }

        public unsafe T Read<T>()
            where T : unmanaged {
            if (DataPointer == nint.Zero) return default;

            var value = *(T*) (DataPointer + Position);
            Position += sizeof(T);
            return value;
        }
    }

    public unsafe class Resource : IDisposable {
        private SilkD3D11ResourcePtr nativeResource;

        protected Resource() { }

        internal Resource(SilkD3D11ResourcePtr nativeResource) {
            this.nativeResource = nativeResource;
        }

        public virtual nint NativePointer => (nint) nativeResource.Handle;

        internal virtual ID3D11Resource* Handle => nativeResource.Handle;

        internal ref SilkD3D11ResourcePtr NativeResource => ref nativeResource;

        public bool IsDisposed { get; private set; }

        public virtual void Dispose() {
            if (IsDisposed) return;

            if (nativeResource.Handle != null) nativeResource.Dispose();
            IsDisposed = true;
        }
    }

    public sealed unsafe class Buffer : Resource {
        private SilkD3D11BufferPtr nativeBuffer;

        internal Buffer(SilkD3D11BufferPtr nativeBuffer, NativeD3DDevice device, BufferDescription description) {
            this.nativeBuffer = nativeBuffer;
            Device = device;
            Description = description;
        }

        public Buffer(DeviceContextProxy context, BufferDescription description)
            : this(context.NativeDevice.CreateBuffer(description), context.NativeDevice, description) { }

        public Buffer(DeviceContextProxy context, nint initialData, BufferDescription description)
            : this(context.NativeDevice.CreateBuffer(description, initialData), context.NativeDevice, description) { }

        internal ID3D11Buffer* BufferHandle => nativeBuffer.Handle;

        public override nint NativePointer => (nint) nativeBuffer.Handle;

        internal override ID3D11Resource* Handle => (ID3D11Resource*) nativeBuffer.Handle;

        internal ref SilkD3D11BufferPtr NativeBuffer => ref nativeBuffer;

        internal NativeD3DDevice Device { get; }

        public BufferDescription Description { get; }

        public static Buffer Create<T>(DeviceContextProxy context, T[] data, BufferDescription description)
            where T : unmanaged {
            fixed (T* dataPtr = data) {
                return new Buffer(context, (nint) dataPtr, description);
            }
        }

        public override void Dispose() {
            if (IsDisposed) return;

            nativeBuffer.Dispose();
            base.Dispose();
        }
    }

    public sealed unsafe class Texture1D : Resource {
        private SilkD3D11Texture1DPtr nativeTexture;

        internal Texture1D(
            SilkD3D11Texture1DPtr nativeTexture,
            NativeD3DDevice device,
            Texture1DDescription description
        ) {
            this.nativeTexture = nativeTexture;
            Device = device;
            Description = description;
        }

        internal ID3D11Texture1D* TextureHandle => nativeTexture.Handle;

        public override nint NativePointer => (nint) nativeTexture.Handle;

        internal override ID3D11Resource* Handle => (ID3D11Resource*) nativeTexture.Handle;

        internal NativeD3DDevice Device { get; }

        public Texture1DDescription Description { get; }

        public override void Dispose() {
            if (IsDisposed) return;

            nativeTexture.Dispose();
            base.Dispose();
        }
    }

    public sealed unsafe class Texture2D : Resource {
        private static readonly Guid DxgiResourceGuid = new("035f3ab4-482e-4e50-b41f-8a7f8bd8960b");
        private SilkD3D11Texture2DPtr nativeTexture;

        internal Texture2D(
            SilkD3D11Texture2DPtr nativeTexture,
            NativeD3DDevice device,
            Texture2DDescription description
        ) {
            this.nativeTexture = nativeTexture;
            Device = device;
            Description = description;
        }

        internal ID3D11Texture2D* TextureHandle => nativeTexture.Handle;

        public override nint NativePointer => (nint) nativeTexture.Handle;

        internal override ID3D11Resource* Handle => (ID3D11Resource*) nativeTexture.Handle;

        internal NativeD3DDevice Device { get; }

        public Texture2DDescription Description { get; }

        public nint GetSharedHandle() {
            IDXGIResource* resource = null;
            var resourceGuid = DxgiResourceGuid;
            SilkMarshal.ThrowHResult(Handle->QueryInterface(&resourceGuid, (void**) &resource));
            try {
                void* sharedHandle = null;
                SilkMarshal.ThrowHResult(resource->GetSharedHandle(&sharedHandle));
                return (nint) sharedHandle;
            } finally {
                resource->Release();
            }
        }

        public override void Dispose() {
            if (IsDisposed) return;

            nativeTexture.Dispose();
            base.Dispose();
        }
    }

    public sealed unsafe class Texture3D : Resource {
        private SilkD3D11Texture3DPtr nativeTexture;

        internal Texture3D(
            SilkD3D11Texture3DPtr nativeTexture,
            NativeD3DDevice device,
            Texture3DDescription description
        ) {
            this.nativeTexture = nativeTexture;
            Device = device;
            Description = description;
        }

        internal ID3D11Texture3D* TextureHandle => nativeTexture.Handle;

        public override nint NativePointer => (nint) nativeTexture.Handle;

        internal override ID3D11Resource* Handle => (ID3D11Resource*) nativeTexture.Handle;

        internal NativeD3DDevice Device { get; }

        public Texture3DDescription Description { get; }

        public override void Dispose() {
            if (IsDisposed) return;

            nativeTexture.Dispose();
            base.Dispose();
        }
    }

    namespace Native {
        internal static class D3DResourceConversions {
            public static BufferDesc ToSilkDesc(this BufferDescription description) {
                return new BufferDesc {
                    ByteWidth = (uint) description.SizeInBytes,
                    Usage = (Silk.NET.Direct3D11.Usage) description.Usage,
                    BindFlags = (uint) description.BindFlags,
                    CPUAccessFlags = (uint) description.CpuAccessFlags,
                    MiscFlags = (uint) description.OptionFlags,
                    StructureByteStride = (uint) description.StructureByteStride
                };
            }

            public static Texture1DDesc ToSilkDesc(this Texture1DDescription description) {
                return new Texture1DDesc {
                    Width = (uint) description.Width,
                    MipLevels = (uint) description.MipLevels,
                    ArraySize = (uint) description.ArraySize,
                    Format = description.Format,
                    Usage = (Silk.NET.Direct3D11.Usage) description.Usage,
                    BindFlags = (uint) description.BindFlags,
                    CPUAccessFlags = (uint) description.CpuAccessFlags,
                    MiscFlags = (uint) description.OptionFlags
                };
            }

            public static Texture2DDesc ToSilkDesc(this Texture2DDescription description) {
                return new Texture2DDesc {
                    Width = (uint) description.Width,
                    Height = (uint) description.Height,
                    MipLevels = (uint) description.MipLevels,
                    ArraySize = (uint) description.ArraySize,
                    Format = description.Format,
                    SampleDesc = new SampleDesc((uint) description.SampleDescription.Count,
                                                (uint) description.SampleDescription.Quality),
                    Usage = (Silk.NET.Direct3D11.Usage) description.Usage,
                    BindFlags = (uint) description.BindFlags,
                    CPUAccessFlags = (uint) description.CpuAccessFlags,
                    MiscFlags = (uint) description.OptionFlags
                };
            }

            public static Texture3DDesc ToSilkDesc(this Texture3DDescription description) {
                return new Texture3DDesc {
                    Width = (uint) description.Width,
                    Height = (uint) description.Height,
                    Depth = (uint) description.Depth,
                    MipLevels = (uint) description.MipLevels,
                    Format = description.Format,
                    Usage = (Silk.NET.Direct3D11.Usage) description.Usage,
                    BindFlags = (uint) description.BindFlags,
                    CPUAccessFlags = (uint) description.CpuAccessFlags,
                    MiscFlags = (uint) description.OptionFlags
                };
            }

            public static Map ToSilkMap(this MapMode mode) {
                return (Map) mode;
            }

            public static uint ToSilkMapFlags(this MapFlags flags) {
                return (uint) flags;
            }

            public static unsafe DataBox ToDataBox(this MappedSubresource mapped) {
                return new DataBox((nint) mapped.PData, (int) mapped.RowPitch, (int) mapped.DepthPitch);
            }
        }
    }
}
