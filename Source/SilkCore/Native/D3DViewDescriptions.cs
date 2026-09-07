/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core.Native;

[Flags]
public enum DepthStencilClearFlags {
    Depth = 1,
    Stencil = 2
}

public enum ShaderResourceViewDimension {
    Unknown = 0,
    Buffer = 1,
    Texture1D = 2,
    Texture1DArray = 3,
    Texture2D = 4,
    Texture2DArray = 5,
    Texture2DMultisampled = 6,
    Texture2DMultisampledArray = 7,
    Texture3D = 8,
    TextureCube = 9,
    TextureCubeArray = 10,
    BufferExtended = 11
}

public enum UnorderedAccessViewDimension {
    Unknown = 0,
    Buffer = 1,
    Texture1D = 2,
    Texture1DArray = 3,
    Texture2D = 4,
    Texture2DArray = 5,
    Texture3D = 8
}

public enum RenderTargetViewDimension {
    Unknown = 0,
    Buffer = 1,
    Texture1D = 2,
    Texture1DArray = 3,
    Texture2D = 4,
    Texture2DArray = 5,
    Texture2DMultisampled = 6,
    Texture2DMultisampledArray = 7,
    Texture3D = 8
}

public enum DepthStencilViewDimension {
    Unknown = 0,
    Texture1D = 1,
    Texture1DArray = 2,
    Texture2D = 3,
    Texture2DArray = 4,
    Texture2DMultisampled = 5,
    Texture2DMultisampledArray = 6
}

[Flags]
public enum DepthStencilViewFlags {
    None = 0,
    ReadOnlyDepth = 1,
    ReadOnlyStencil = 2
}

[Flags]
public enum UnorderedAccessViewBufferFlags {
    None = 0,
    Raw = 1,
    Append = 2,
    Counter = 4
}

public struct ShaderResourceViewDescription {
    public Format Format;
    public ShaderResourceViewDimension Dimension;
    public BufferResource Buffer;
    public Texture1DResource Texture1D;
    public Texture2DResource Texture2D;
    public Texture3DResource Texture3D;
    public TextureCubeResource TextureCube;

    public struct BufferResource {
        public int FirstElement;
        public int ElementCount;
    }

    public struct Texture1DResource {
        public int MostDetailedMip;
        public int MipLevels;
    }

    public struct Texture2DResource {
        public int MostDetailedMip;
        public int MipLevels;
    }

    public struct Texture3DResource {
        public int MostDetailedMip;
        public int MipLevels;
    }

    public struct TextureCubeResource {
        public int MostDetailedMip;
        public int MipLevels;
    }
}

public struct UnorderedAccessViewDescription {
    public Format Format;
    public UnorderedAccessViewDimension Dimension;
    public BufferResource Buffer;

    public struct BufferResource {
        public int FirstElement;
        public int ElementCount;
        public UnorderedAccessViewBufferFlags Flags;
    }
}
