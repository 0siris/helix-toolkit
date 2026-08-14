// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DX11ImageSource.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Silk.NET.Direct3D9;
using SilkD3D9DevicePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D9.IDirect3DDevice9Ex>;
using SilkD3D9Ptr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D9.IDirect3D9Ex>;
using SilkD3D9SurfacePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D9.IDirect3DSurface9>;
using SilkD3D9TexturePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D9.IDirect3DTexture9>;

namespace HelixToolkit.Wpf.SharpDX;

public sealed class DX11ImageSource : D3DImage, IDisposable {
    private readonly D3D9ImageSourceInterop interop;
    private NativeD3DTexture2D? renderTarget;

    public DX11ImageSource(int adapterIndex = 0) {
        interop = new D3D9ImageSourceInterop(adapterIndex);
    }

    public void InvalidateD3DImage() {
        if (renderTarget is null) return;

        SetBackBuffer(interop.SurfacePointer);
    }

    public void SetRenderTargetDX11(NativeD3DTexture2D? target) {
        EndD3D(false);
        if (target is null || target.IsDisposed) return;

        renderTarget = target;
        interop.OpenSharedTexture(target);
        SetBackBuffer(interop.SurfacePointer);
    }

    private void EndD3D(bool disposeDevices) {
        Lock();
        try {
            base.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
        } finally {
            Unlock();
        }

        renderTarget = null;
        interop.CloseTexture();
    }

    public bool IsDeviceStateOk() => interop.IsDeviceStateOk();

    private void SetBackBuffer(IntPtr surfacePointer) {
        Lock();
        try {
            base.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surfacePointer, true);
            if (PixelWidth > 0 && PixelHeight > 0) AddDirtyRect(new Int32Rect(0, 0, PixelWidth, PixelHeight));
        } finally {
            Unlock();
        }
    }

    #region IDisposable Support

    private bool disposedValue; // To detect redundant calls

    [SuppressMessage("Microsoft.Usage",
        "CA2213: Disposable fields should be disposed",
        Justification = "False positive.")]
    private void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing) {
                EndD3D(true);
                interop.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose() {
        Dispose(true);
    }

    #endregion
}

internal sealed unsafe class D3D9ImageSourceInterop : IDisposable {
    [Obsolete] private static readonly D3D9 D3D9Api = D3D9.GetApi();
    private SilkD3D9Ptr context;
    private SilkD3D9DevicePtr device;
    private bool disposed;
    private SilkD3D9SurfacePtr surface;
    private SilkD3D9TexturePtr texture;

    public D3D9ImageSourceInterop(int adapterIndex) {
        IDirect3D9Ex* contextHandle = null;
        Marshal.ThrowExceptionForHR(D3D9Api.Direct3DCreate9Ex(D3D9.SdkVersion, &contextHandle));
        context = new SilkD3D9Ptr(contextHandle);
        contextHandle->Release();

        var presentParameters = new PresentParameters {
            Windowed = true,
            SwapEffect = Swapeffect.Discard,
            PresentationInterval = D3D9.PresentIntervalDefault,
            BackBufferHeight = 1,
            BackBufferWidth = 1,
            BackBufferFormat = Silk.NET.Direct3D9.Format.Unknown
        };
        var createFlags =
            (uint) (D3D9.CreateHardwareVertexprocessing | D3D9.CreateMultithreaded | D3D9.CreateFpuPreserve);
        IDirect3DDevice9Ex* deviceHandle = null;
        Marshal.ThrowExceptionForHR(context.CreateDeviceEx((uint) Math.Max(0, adapterIndex),
            Devtype.Hal,
            IntPtr.Zero,
            createFlags,
            &presentParameters,
            (Displaymodeex*) null,
            &deviceHandle));
        device = new SilkD3D9DevicePtr(deviceHandle);
        deviceHandle->Release();
    }

    public IntPtr SurfacePointer => (IntPtr) surface.Handle;

    public void Dispose() {
        if (disposed) return;

        CloseTexture();
        device.Dispose();
        context.Dispose();
        disposed = true;
    }

    public void OpenSharedTexture(NativeD3DTexture2D sharedTexture) {
        if (sharedTexture == null || sharedTexture.IsDisposed) throw new ArgumentNullException(nameof(sharedTexture));
        if ((sharedTexture.Description.OptionFlags & ResourceOptionFlags.Shared) == 0)
            throw new ArgumentException("Texture must be created with ResourceOptionFlags.Shared.",
                nameof(sharedTexture));

        CloseTexture();
        var format = TranslateFormat(sharedTexture.Description.Format);
        var sharedHandle = (void*) sharedTexture.GetSharedHandle();
        if (sharedHandle == null)
            throw new InvalidOperationException("The D3D11 texture did not expose a shared handle.");

        IDirect3DTexture9* textureHandle = null;
        Marshal.ThrowExceptionForHR(device.CreateTexture((uint) sharedTexture.Description.Width,
            (uint) sharedTexture.Description.Height,
            1,
            D3D9.UsageRendertarget,
            format,
            Pool.Default,
            &textureHandle,
            &sharedHandle));
        texture = new SilkD3D9TexturePtr(textureHandle);
        textureHandle->Release();

        IDirect3DSurface9* surfaceHandle = null;
        Marshal.ThrowExceptionForHR(texture.GetSurfaceLevel(0, &surfaceHandle));
        surface = new SilkD3D9SurfacePtr(surfaceHandle);
        surfaceHandle->Release();
    }

    public void CreateRenderTarget(int width, int height) {
        CloseTexture();
        IDirect3DTexture9* textureHandle = null;
        Marshal.ThrowExceptionForHR(device.CreateTexture((uint) Math.Max(1, width),
            (uint) Math.Max(1, height),
            1,
            D3D9.UsageRendertarget,
            Silk.NET.Direct3D9.Format.A8R8G8B8,
            Pool.Default,
            &textureHandle,
            null));
        texture = new SilkD3D9TexturePtr(textureHandle);
        textureHandle->Release();

        IDirect3DSurface9* surfaceHandle = null;
        Marshal.ThrowExceptionForHR(texture.GetSurfaceLevel(0, &surfaceHandle));
        surface = new SilkD3D9SurfacePtr(surfaceHandle);
        surfaceHandle->Release();
    }

    public bool IsDeviceStateOk() => !disposed && device.Handle != null && device.CheckDeviceState(IntPtr.Zero) >= 0;

    public void CloseTexture() {
        surface.Dispose();
        surface = default;
        texture.Dispose();
        texture = default;
    }

    private static Silk.NET.Direct3D9.Format TranslateFormat(Format format) {
        return format switch {
            Format.FormatR10G10B10A2Unorm => Silk.NET.Direct3D9.Format.A2B10G10R10,
            Format.FormatR16G16B16A16Float => Silk.NET.Direct3D9.Format.A16B16G16R16f,
            Format.FormatB8G8R8A8Unorm => Silk.NET.Direct3D9.Format.A8R8G8B8,
            _ => throw new ArgumentException($"Texture format {format} is not compatible with D3D9Ex sharing.",
                nameof(format))
        };
    }
}