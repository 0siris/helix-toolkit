/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using SilkD3D11DepthStencilViewPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11DepthStencilView>;
using SilkD3D11RenderTargetViewPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11RenderTargetView>;
using SilkD3D11ShaderResourceViewPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11ShaderResourceView>;
using SilkD3D11UnorderedAccessViewPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11UnorderedAccessView>;

namespace HelixToolkit.SharpDX.Core;

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

public sealed unsafe class RenderTargetView : IDisposable {
    private SilkD3D11RenderTargetViewPtr nativeView;

    internal RenderTargetView(SilkD3D11RenderTargetViewPtr nativeView, Resource? resource = null) {
        this.nativeView = nativeView;
        Resource = resource;
    }

    public nint NativePointer => (nint)nativeView.Handle;

    public Resource Resource { get; }

    internal ID3D11RenderTargetView* Handle => nativeView.Handle;

    public bool IsDisposed { get; private set; }

    public void Dispose() {
        if (IsDisposed) return;

        nativeView.Dispose();
        IsDisposed = true;
    }
}

public sealed unsafe class DepthStencilView : IDisposable {
    private SilkD3D11DepthStencilViewPtr nativeView;

    internal DepthStencilView(SilkD3D11DepthStencilViewPtr nativeView) {
        this.nativeView = nativeView;
    }

    public nint NativePointer => (nint)nativeView.Handle;

    internal ID3D11DepthStencilView* Handle => nativeView.Handle;

    public bool IsDisposed { get; private set; }

    public void Dispose() {
        if (IsDisposed) return;

        nativeView.Dispose();
        IsDisposed = true;
    }
}

public sealed unsafe class ShaderResourceView : IDisposable {
    private SilkD3D11ShaderResourceViewPtr nativeView;

    internal ShaderResourceView(SilkD3D11ShaderResourceViewPtr nativeView) {
        this.nativeView = nativeView;
    }

    internal ShaderResourceView(
        SilkD3D11ShaderResourceViewPtr nativeView,
        ShaderResourceViewDescription description
    )
        : this(nativeView) {
        Description = description;
    }

    public nint NativePointer => (nint)nativeView.Handle;

    internal ID3D11ShaderResourceView* Handle => nativeView.Handle;

    public ShaderResourceViewDescription Description { get; }

    public bool IsDisposed { get; private set; }

    public void Dispose() {
        if (IsDisposed) return;

        nativeView.Dispose();
        IsDisposed = true;
    }
}

public sealed unsafe class UnorderedAccessView : IDisposable {
    private SilkD3D11UnorderedAccessViewPtr nativeView;

    internal UnorderedAccessView(SilkD3D11UnorderedAccessViewPtr nativeView) {
        this.nativeView = nativeView;
    }

    internal UnorderedAccessView(
        SilkD3D11UnorderedAccessViewPtr nativeView,
        UnorderedAccessViewDescription description
    )
        : this(nativeView) {
        Description = description;
    }

    public nint NativePointer => (nint)nativeView.Handle;

    internal ID3D11UnorderedAccessView* Handle => nativeView.Handle;

    public UnorderedAccessViewDescription Description { get; }

    public bool IsDisposed { get; private set; }

    public void Dispose() {
        if (IsDisposed) return;

        nativeView.Dispose();
        IsDisposed = true;
    }
}
