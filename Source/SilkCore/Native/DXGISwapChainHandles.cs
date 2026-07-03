/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using SilkDXGIFactory2Ptr = Silk.NET.Core.Native.ComPtr<Silk.NET.DXGI.IDXGIFactory2>;
using SilkDXGISwapChain1Ptr = Silk.NET.Core.Native.ComPtr<Silk.NET.DXGI.IDXGISwapChain1>;
using SilkD3D11Texture2DPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Texture2D>;

namespace HelixToolkit.SharpDX.Core;

public enum Usage {
    RenderTargetOutput = 0x20
}

public enum SwapEffect {
    Discard = 0,
    Sequential = 1,
    FlipSequential = 3,
    FlipDiscard = 4
}

public enum Scaling {
    Stretch = 0,
    None = 1,
    AspectRatioStretch = 2
}

[Flags]
public enum SwapChainFlags {
    None = 0,
    AllowModeSwitch = 2
}

[Flags]
public enum PresentFlags {
    None = 0,
    Restart = 4
}

public struct ModeDescription {
    public Format Format;
}

public struct SwapChainDescription {
    public ModeDescription ModeDescription;
    public SwapChainFlags Flags;
}

public struct SwapChainDescription1 {
    public int Width;
    public int Height;
    public Format Format;
    public bool Stereo;
    public SampleDescription SampleDescription;
    public Usage Usage;
    public int BufferCount;
    public SwapEffect SwapEffect;
    public Scaling Scaling;
    public SwapChainFlags Flags;
}

public sealed class PresentParameters { }

public readonly struct PresentResult {
    public PresentResult(bool success) {
        Success = success;
    }

    public bool Success { get; }
}

public unsafe class SwapChain1 : IDisposable {
    private static readonly DXGI DxgiApi = DXGI.GetApi(null);
    private static readonly Guid Factory2Guid = new("50c83a1c-e072-4c48-87b0-3630fa36a6d0");
    private static readonly Guid Texture2DGuid = new("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
    private readonly NativeD3DDevice device;

    private SilkDXGIFactory2Ptr factory;
    private SilkDXGISwapChain1Ptr swapChain;

    protected SwapChain1(SwapChainDescription1 description) {
        Description1 = description;
        Description = new SwapChainDescription {
            ModeDescription = new ModeDescription {Format = description.Format},
            Flags = description.Flags
        };
    }

    public SwapChain1(SwapChainDescription1 description, nint surfacePointer, NativeD3DDevice device) {
        if (surfacePointer == nint.Zero)
            throw new ArgumentException("A valid HWND is required.", nameof(surfacePointer));

        this.device = device ?? throw new ArgumentNullException(nameof(device));
        Description1 = description;
        Description = new SwapChainDescription {
            ModeDescription = new ModeDescription {Format = description.Format},
            Flags = description.Flags
        };
        SurfacePointer = surfacePointer;
        CreateNativeSwapChain();
    }

    public SwapChainDescription1 Description1 { get; private set; }

    public SwapChainDescription Description { get; private set; }

    public nint SurfacePointer { get; }

    public bool IsDisposed { get; private set; }

    public virtual void Dispose() {
        if (IsDisposed) return;

        swapChain.Dispose();
        factory.Dispose();
        IsDisposed = true;
    }

    public virtual PresentResult Present(
        int syncInterval,
        PresentFlags presentFlags,
        PresentParameters presentParameters
    ) {
        ThrowIfDisposed();
        var result = swapChain.Present((uint) syncInterval, (uint) presentFlags);
        Marshal.ThrowExceptionForHR(result);
        return new PresentResult(true);
    }

    public virtual void ResizeBuffers(int bufferCount, int width, int height, Format format, SwapChainFlags flags) {
        ThrowIfDisposed();
        Marshal.ThrowExceptionForHR(swapChain.ResizeBuffers(0,
                                                            (uint) Math.Max(1, width),
                                                            (uint) Math.Max(1, height),
                                                            Format.FormatUnknown,
                                                            0));

        Description1 = new SwapChainDescription1 {
            Width = width,
            Height = height,
            Format = format,
            Stereo = Description1.Stereo,
            SampleDescription = Description1.SampleDescription,
            Usage = Description1.Usage,
            BufferCount = bufferCount,
            SwapEffect = Description1.SwapEffect,
            Scaling = Description1.Scaling,
            Flags = flags
        };
        Description = new SwapChainDescription {
            ModeDescription = new ModeDescription {Format = format},
            Flags = flags
        };
    }

    internal Texture2D GetBackBuffer() {
        ThrowIfDisposed();

        ID3D11Texture2D* texture = null;
        var textureGuid = Texture2DGuid;
        Marshal.ThrowExceptionForHR(swapChain.GetBuffer(0, &textureGuid, (void**) &texture));
        var nativeTexture = new SilkD3D11Texture2DPtr(texture);
        texture->Release();

        var description = new Texture2DDescription {
            Width = Description1.Width,
            Height = Description1.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Description1.Format,
            SampleDescription = Description1.SampleDescription,
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.RenderTarget,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None
        };
        return new Texture2D(nativeTexture, device, description);
    }

    private void CreateNativeSwapChain() {
        IDXGIFactory2* factoryHandle = null;
        var factoryGuid = Factory2Guid;
        Marshal.ThrowExceptionForHR(DxgiApi.CreateDXGIFactory2(0, &factoryGuid, (void**) &factoryHandle));
        factory = new SilkDXGIFactory2Ptr(factoryHandle);
        factoryHandle->Release();

        var description = new SwapChainDesc1 {
            Width = (uint) Math.Max(1, Description1.Width),
            Height = (uint) Math.Max(1, Description1.Height),
            Format = Description1.Format,
            Stereo = false,
            SampleDesc = new SampleDesc((uint) Description1.SampleDescription.Count,
                                        (uint) Description1.SampleDescription.Quality),
            BufferUsage = (uint) Description1.Usage,
            BufferCount = (uint) Description1.BufferCount,
            Scaling = (Silk.NET.DXGI.Scaling) Description1.Scaling,
            SwapEffect = (Silk.NET.DXGI.SwapEffect) Description1.SwapEffect,
            AlphaMode = AlphaMode.Unspecified,
            Flags = (uint) Description1.Flags
        };

        IDXGISwapChain1* swapChainHandle = null;
        Marshal.ThrowExceptionForHR(factory.CreateSwapChainForHwnd((IUnknown*) device.Handle,
                                                                   SurfacePointer,
                                                                   &description,
                                                                   (SwapChainFullscreenDesc*) null,
                                                                   (IDXGIOutput*) null,
                                                                   &swapChainHandle));
        swapChain = new SilkDXGISwapChain1Ptr(swapChainHandle);
        swapChainHandle->Release();
    }

    private void ThrowIfDisposed() {
        if (IsDisposed) throw new ObjectDisposedException(nameof(SwapChain1));
        if (swapChain.Handle == null)
            throw new PlatformNotSupportedException(
                "Composition swap chains are not migrated to the Silk.NET backend.");
    }
}

public sealed class SwapChain2 : SwapChain1 {
    public SwapChain2(SwapChainDescription1 description)
        : base(description) { }
}
