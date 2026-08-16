using System;
using System.Windows;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render.RenderHost;

namespace HelixToolkit.Wpf.SharpDX.Controls;

public sealed class DX11ImageSourceArgs : EventArgs {
    public readonly DX11ImageSource Source;

    public DX11ImageSourceArgs(DX11ImageSource source) {
        Source = source;
    }
}

public sealed class DX11ImageSourceRenderHost : DefaultRenderHost {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;

    private bool frontBufferChange;
    private bool hasBackBuffer;

    private bool lastSurfaceD3DIsFrontBufferAvailable;

    private DX11ImageSource? surfaceD3D;

    public DX11ImageSourceRenderHost(Func<IDevice3DResources, IRenderer> createRenderer) :
        base(createRenderer) {
        OnNewRenderTargetTexture += DX11ImageSourceRenderer_OnNewBufferCreated;
    }

    public DX11ImageSourceRenderHost() {
        OnNewRenderTargetTexture += DX11ImageSourceRenderer_OnNewBufferCreated;
    }

    public event EventHandler<DX11ImageSourceArgs>? OnImageSourceChanged;

    protected override void PostRender() {
        if (!hasBackBuffer || surfaceD3D is null) {
            Logger.Warn("Back buffer is not set");
            return;
        }

        surfaceD3D.InvalidateD3DImage();
        base.PostRender();
    }

    protected override void DisposeBuffers() {
        Logger.Info("Dispose buffers");
        if (surfaceD3D is not null) {
            hasBackBuffer = false;
            surfaceD3D.SetRenderTargetDX11(null);
            if (!frontBufferChange)
                surfaceD3D.IsFrontBufferAvailableChanged -= SurfaceD3D_IsFrontBufferAvailableChanged;
            RemoveAndDispose(ref surfaceD3D);
        }

        base.DisposeBuffers();
    }

    private void DX11ImageSourceRenderer_OnNewBufferCreated(object? sender, Texture2DArgs e) {
        try {
            if (surfaceD3D is null) {
                Logger.Info("Create new D3DImageSource");
                surfaceD3D = new DX11ImageSource(EffectsManager.AssertNotNull()
                    .AdapterIndex);
                surfaceD3D.IsFrontBufferAvailableChanged += SurfaceD3D_IsFrontBufferAvailableChanged;
            }

            if (e.Texture.Resource is Texture2D tex2D)
                surfaceD3D.SetRenderTargetDX11(tex2D);
        } catch (Exception ex) {
            Logger.Error("Failed to create surfaceD3D. Ex: {Value0}", ex.Message);
            hasBackBuffer = false;
            surfaceD3D?.IsFrontBufferAvailableChanged -= SurfaceD3D_IsFrontBufferAvailableChanged;
            
            RemoveAndDispose(ref surfaceD3D);
            hasBackBuffer = false;
            EndD3D();
            ReinitializeEffectsManager();
            return;
        }

        hasBackBuffer = e.Texture.Resource is Texture2D;
        OnImageSourceChanged?.Invoke(this,
            new DX11ImageSourceArgs(
                surfaceD3D.AssertNotNull("Image source must be initialized.")));
        Logger.Info("{Message}", hasBackBuffer
            ? "New back buffer is set"
            : "Set back buffer failed");
    }

    private void SurfaceD3D_IsFrontBufferAvailableChanged(object? sender, DependencyPropertyChangedEventArgs e) {
        var newValue = (bool) e.NewValue;
        if (EffectsManager == null || newValue == lastSurfaceD3DIsFrontBufferAvailable) return;

        Logger.Warn("SurfaceD3D front buffer changed. Value = {Value0}, last value {Value1}",
            newValue,
            lastSurfaceD3DIsFrontBufferAvailable);
        if (surfaceD3D is not null) {
            hasBackBuffer = false;
            surfaceD3D.SetRenderTargetDX11(null);
            surfaceD3D.IsFrontBufferAvailableChanged -= SurfaceD3D_IsFrontBufferAvailableChanged;
            RemoveAndDispose(ref surfaceD3D);
        }

        if (newValue) {
            frontBufferChange = false;
            try {
                if (surfaceD3D?.IsDeviceStateOk() == true) {
                    Restart(true);
                } else {
                    EndD3D();
                    ReinitializeEffectsManager();
                }
            } catch (Exception ex) {
                Logger.Error("{ExceptionMessage}",ex.Message);
            }
        } else {
            frontBufferChange = true;
            if (surfaceD3D?.IsDeviceStateOk() != true) {
                hasBackBuffer = false;
                EndD3D();
            }
        }

        lastSurfaceD3DIsFrontBufferAvailable = newValue;
    }

    protected override void OnDispose(bool disposeManagedResources) {
        OnImageSourceChanged = null;
        if (surfaceD3D is not null) {
            hasBackBuffer = false;
            surfaceD3D.SetRenderTargetDX11(null);
            surfaceD3D.IsFrontBufferAvailableChanged -= SurfaceD3D_IsFrontBufferAvailableChanged;
            RemoveAndDispose(ref surfaceD3D);
        }

        base.OnDispose(disposeManagedResources);
    }
}