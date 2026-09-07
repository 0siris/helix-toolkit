/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core.Native;

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

public struct BufferDescription {
    public int SizeInBytes;
    public BindFlags BindFlags;
    public CpuAccessFlags CpuAccessFlags;
    public ResourceOptionFlags OptionFlags;
    public ResourceUsage Usage;
    public int StructureByteStride;
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
        X = (Half)x;
        Y = (Half)y;
        Z = (Half)z;
        W = (Half)w;
    }

    public Half X;
    public Half Y;
    public Half Z;
    public Half W;

    public static implicit operator Half4(Vector4 value) => new(value.X, value.Y, value.Z, value.W);
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

        var value = *(T*)(DataPointer + Position);
        Position += sizeof(T);
        return value;
    }
}
