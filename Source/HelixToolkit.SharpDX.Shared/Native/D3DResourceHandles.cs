/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using SilkD3D11BufferPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Buffer>;
using SilkD3D11ResourcePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Resource>;

#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX
#else
#if CORE
namespace HelixToolkit.SharpDX.Core
#else
namespace HelixToolkit.UWP
#endif
#endif
{
    [Flags]
    public enum BindFlags
    {
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
    public enum CpuAccessFlags
    {
        None = 0,
        Write = 0x10000,
        Read = 0x20000
    }

    public enum ResourceUsage
    {
        Default = 0,
        Immutable = 1,
        Dynamic = 2,
        Staging = 3
    }

    [Flags]
    public enum ResourceOptionFlags
    {
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

    public enum MapMode
    {
        Read = 1,
        Write = 2,
        ReadWrite = 3,
        WriteDiscard = 4,
        WriteNoOverwrite = 5
    }

    [Flags]
    public enum MapFlags
    {
        None = 0,
        DoNotWait = 0x100000
    }

    public struct BufferDescription
    {
        public int SizeInBytes;
        public BindFlags BindFlags;
        public CpuAccessFlags CpuAccessFlags;
        public ResourceOptionFlags OptionFlags;
        public ResourceUsage Usage;
        public int StructureByteStride;
    }

    public struct SampleDescription
    {
        public int Count;
        public int Quality;
    }

    public struct Texture1DDescription
    {
        public int Width;
        public int MipLevels;
        public int ArraySize;
        public Format Format;
        public BindFlags BindFlags;
        public CpuAccessFlags CpuAccessFlags;
        public ResourceOptionFlags OptionFlags;
        public ResourceUsage Usage;
    }

    public struct Texture2DDescription
    {
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

    public struct Texture3DDescription
    {
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

    public struct RenderTargetViewDescription
    {
        public Format Format;
    }

    public struct DepthStencilViewDescription
    {
        public Format Format;
    }

    public struct DataBox
    {
        public DataBox(IntPtr dataPointer, int rowPitch, int slicePitch)
        {
            DataPointer = dataPointer;
            RowPitch = rowPitch;
            SlicePitch = slicePitch;
        }

        public IntPtr DataPointer;
        public int RowPitch;
        public int SlicePitch;
        public bool IsEmpty => DataPointer == IntPtr.Zero;
    }

    public struct ResourceRegion
    {
        public int Left;
        public int Top;
        public int Front;
        public int Right;
        public int Bottom;
        public int Back;

        internal Box ToSilkBox()
        {
            return new Box
            {
                Left = (uint)Left,
                Top = (uint)Top,
                Front = (uint)Front,
                Right = (uint)Right,
                Bottom = (uint)Bottom,
                Back = (uint)Back
            };
        }
    }

    public sealed class DataStream : IDisposable
    {
        public DataStream(IntPtr dataPointer, int length, bool canRead, bool canWrite)
        {
            DataPointer = dataPointer;
            Length = length;
            CanRead = canRead;
            CanWrite = canWrite;
        }

        public IntPtr DataPointer { get; }

        public int Length { get; }

        public int Position { get; set; }

        public bool CanRead { get; }

        public bool CanWrite { get; }

        public bool IsDisposed { get; private set; }

        public unsafe T Read<T>()
            where T : unmanaged
        {
            if (DataPointer == IntPtr.Zero)
            {
                return default;
            }

            var value = *(T*)(DataPointer + Position);
            Position += sizeof(T);
            return value;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    public unsafe class Resource : IDisposable
    {
        private SilkD3D11ResourcePtr nativeResource;

        protected Resource()
        {
        }

        internal Resource(SilkD3D11ResourcePtr nativeResource)
        {
            this.nativeResource = nativeResource;
        }

        public virtual IntPtr NativePointer => (IntPtr)nativeResource.Handle;

        internal virtual ID3D11Resource* Handle => nativeResource.Handle;

        internal ref SilkD3D11ResourcePtr NativeResource => ref nativeResource;

        public bool IsDisposed { get; private set; }

        public virtual void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            if (nativeResource.Handle != null)
            {
                nativeResource.Dispose();
            }
            IsDisposed = true;
        }
    }

    public unsafe sealed class Buffer : Resource
    {
        private SilkD3D11BufferPtr nativeBuffer;

        internal Buffer(SilkD3D11BufferPtr nativeBuffer, Native.SilkD3DDevice device, BufferDescription description)
        {
            this.nativeBuffer = nativeBuffer;
            Device = device;
            Description = description;
        }

        public Buffer(Render.DeviceContextProxy context, BufferDescription description)
            : this(context.NativeDevice.CreateBuffer(description), context.NativeDevice, description)
        {
        }

        public Buffer(Render.DeviceContextProxy context, IntPtr initialData, BufferDescription description)
            : this(context.NativeDevice.CreateBuffer(description, initialData), context.NativeDevice, description)
        {
        }

        internal ID3D11Buffer* BufferHandle => nativeBuffer.Handle;

        public override IntPtr NativePointer => (IntPtr)nativeBuffer.Handle;

        internal override ID3D11Resource* Handle => (ID3D11Resource*)nativeBuffer.Handle;

        internal ref SilkD3D11BufferPtr NativeBuffer => ref nativeBuffer;

        internal Native.SilkD3DDevice Device { get; }

        public BufferDescription Description { get; }

        public static Buffer Create<T>(Render.DeviceContextProxy context, T[] data, BufferDescription description)
            where T : unmanaged
        {
            unsafe
            {
                fixed (T* dataPtr = data)
                {
                    return new Buffer(context, (IntPtr)dataPtr, description);
                }
            }
        }

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            nativeBuffer.Dispose();
            base.Dispose();
        }
    }

    namespace Native
    {
        internal static class D3DResourceConversions
        {
            public static BufferDesc ToSilkDesc(this BufferDescription description)
            {
                return new BufferDesc
                {
                    ByteWidth = (uint)description.SizeInBytes,
                    Usage = (Usage)description.Usage,
                    BindFlags = (uint)description.BindFlags,
                    CPUAccessFlags = (uint)description.CpuAccessFlags,
                    MiscFlags = (uint)description.OptionFlags,
                    StructureByteStride = (uint)description.StructureByteStride
                };
            }

            public static Map ToSilkMap(this MapMode mode)
            {
                return (Map)mode;
            }

            public static uint ToSilkMapFlags(this MapFlags flags)
            {
                return (uint)flags;
            }

            public static DataBox ToDataBox(this MappedSubresource mapped)
            {
                return new DataBox((IntPtr)mapped.PData, (int)mapped.RowPitch, (int)mapped.DepthPitch);
            }
        }
    }
}
