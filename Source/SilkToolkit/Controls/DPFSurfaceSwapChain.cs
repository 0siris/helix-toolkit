// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DPFCanvas.cs" company="Helix Toolkit">
//   Copyright (c) 2018 Helix Toolkit contributors
// </copyright>
// <summary>
//
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Render.Renderer;
using HelixToolkit.SharpDX.Core.Render.RenderHost;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>
/// </summary>
/// <seealso cref="System.Windows.Controls.Image" />
public class DPFSurfaceSwapChain : Grid, IRenderCanvas, IDisposable {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;
    private readonly CompositionTargetEx compositionTarget = new();

    private readonly Image image = new() {
        Width = 1,
        Height = 1
    };

    private readonly WinformHostExtend winformHost = new();
    private bool belongsToParentWindow;

    private D3DImageExt? image3D;
    private Window? parentWindow;

    private DispatcherOperation? resizeOperation;

    public DPFSurfaceSwapChain(bool deferredRendering = false, bool attachedToWindow = true) {
        var surfaceD3D1 = SetupVisual(attachedToWindow);
        RenderHost = SetupRenderHost(deferredRendering
            ? new SwapChainRenderHost(surfaceD3D1.Handle,
                device => new DeferredContextRenderer(
                    device,
                    new AutoRenderTaskScheduler()))
            : new SwapChainRenderHost(surfaceD3D1.Handle));
        SetupImage();
    }

    public DPFSurfaceSwapChain(Func<IntPtr, IRenderHost> createRenderHost, bool attachedToWindow = true) {
        var surfaceD3D1 = SetupVisual(attachedToWindow);
        RenderHost = SetupRenderHost(createRenderHost(surfaceD3D1.Handle));
        SetupImage();
    }

    public bool IncreaseFps { get; set; } = true;

    /// <summary>
    ///     Gets or sets the render host.
    /// </summary>
    /// <value>
    ///     The render host.
    /// </value>
    public IRenderHost RenderHost { get; }

    /// <summary>
    ///     Fired whenever an exception occurred on this object.
    /// </summary>
    public event EventHandler<RelayExceptionEventArgs> ExceptionOccurred = delegate { };

    public bool EnableDpiScale {
        get;
        set {
            field = value;
            RenderHost.DpiScale = value
                ? (float) DpiScale
                : 1;
        }
    } = true;

    public double DpiScale {
        get => winformHost.DpiScale;
        set => winformHost.DpiScale = value;
    }

    private void DPFSurfaceSwapChain_DpiScaleChanged(object? sender, double e) {
        RenderHost.DpiScale = EnableDpiScale
            ? (float) e
            : 1;
    }

    private RenderControl SetupVisual(bool attachedToWindow) {
        Children.Add(image);
        Children.Add(winformHost);
        winformHost.DpiScaleChanged += DPFSurfaceSwapChain_DpiScaleChanged;
        var renderControl = new RenderControl(this);
        winformHost.Child = renderControl;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        belongsToParentWindow = attachedToWindow;
        return renderControl;
    }

    private IRenderHost SetupRenderHost(IRenderHost host) {
        host.DpiScale = EnableDpiScale
            ? (float) DpiScale
            : 1;
        host.StartRenderLoop += RenderHost_StartRenderLoop;
        host.StopRenderLoop += RenderHost_StopRenderLoop;
        host.ExceptionOccurred += (_, e) => { HandleExceptionOccured(e.Exception); };
        host.EffectsManagerChanged += (_, _) => { SetupImage(); };
        return host;
    }

    private void SetupImage() {
        var effectsManager = RenderHost.EffectsManager;
        if (image3D is null || (effectsManager is not null && effectsManager.AdapterIndex != image3D.AdapterIndex)) {
            image.Source = null;
            image3D?.Dispose();
            if (effectsManager is not null) {
                image3D = new D3DImageExt(effectsManager.AdapterIndex);
                image.Source = image3D;
            } else {
                image3D = null;
            }
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnLoaded(object? sender, RoutedEventArgs e) {
        try {
            if (belongsToParentWindow) {
                parentWindow = FindVisualAncestor<Window>(this);
                if (parentWindow != null) {
                    parentWindow.Closed -= ParentWindow_Closed;
                    parentWindow.Closed += ParentWindow_Closed;
                }
            }

            StartD3D();
        } catch (Exception ex) {
            // Exceptions in the Loaded event handler are silently swallowed by WPF.
            // https://social.msdn.microsoft.com/Forums/vstudio/en-US/9ed3d13d-0b9f-48ac-ae8d-daf0845c9e8f/bug-in-wpf-windowloaded-exception-handling?forum=wpf
            // http://stackoverflow.com/questions/19140593/wpf-exception-thrown-in-eventhandler-is-swallowed
            // tl;dr: M$ says it's "by design" and "working as indended" but may change in the future :).

            if (!HandleExceptionOccured(ex))
                MessageBox.Show(
                    $"DPFCanvas: Error while starting rendering: {ex.Message} \n StackTrace: {ex.StackTrace}",
                    "Error");
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnUnloaded(object? sender, RoutedEventArgs e) {
        if (belongsToParentWindow && parentWindow is not null) parentWindow.Closed -= ParentWindow_Closed;
        if (DataContext == null && RenderHost.EffectsManager == null && belongsToParentWindow)
            EndD3D();
        else
            RenderHost.StopRendering();
    }

    private void ParentWindow_Closed(object? sender, EventArgs e) {
        EndD3D();
    }

    /// <summary>
    /// </summary>
    private bool StartD3D() {
        RenderHost.StartD3D((int) ActualWidth, (int) ActualHeight);
        return true;
    }

    private void RenderHost_StopRenderLoop(object? sender, EventArgs e) {
        compositionTarget.Rendering -= CompositionTarget_Rendering;
    }

    private void RenderHost_StartRenderLoop(object? sender, EventArgs e) {
        compositionTarget.Rendering -= CompositionTarget_Rendering;
        compositionTarget.Rendering += CompositionTarget_Rendering;
    }


    private void CompositionTarget_Rendering(object? sender, RenderingEventArgs e) {
        if (RenderHost.UpdateAndRender() && IncreaseFps) image3D?.InvalidateD3DImage();
    }

    /// <summary>
    /// </summary>
    private void EndD3D() {
        RenderHost.EndD3D();
    }

    /// <summary>
    /// </summary>
    /// <param name="sizeInfo"></param>
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
        if (resizeOperation is {Status: DispatcherOperationStatus.Pending})
            resizeOperation.Abort();
        resizeOperation = Dispatcher.BeginInvoke(DispatcherPriority.Background,
            (Action) (() => {
                if (IsLoaded)
                    try {
                        RenderHost.Resize(
                            (int) ActualWidth,
                            (int) ActualHeight);
                    } catch (Exception ex) {
                        if (!HandleExceptionOccured(ex))
                            MessageBox.Show(
                                $"DPFCanvas: Error during rendering: {ex.Message} \n StackTrace: {ex.StackTrace}",
                                "Error");
                    }
            }));
    }

    /// <summary>
    ///     Invoked whenever an exception occurs. Stops rendering, frees resources and throws
    /// </summary>
    /// <param name="exception">The exception that occured.</param>
    /// <returns><c>true</c> if the exception has been handled, <c>false</c> otherwise.</returns>
    private bool HandleExceptionOccured(Exception exception) {
        EndD3D();

        if (exception is COMException comException && IsDeviceLost(comException.HResult)) {
            // Try to recover from DeviceRemoved/DeviceReset
            StartD3D();
            return true;
        }

        Logger.Error(exception, "Render canvas exception");
        var args = new RelayExceptionEventArgs(exception);
        ExceptionOccurred(this, args);
        return args.Handled;
    }

    private static bool IsDeviceLost(int hresult) => hresult is
        unchecked((int) 0x887A0005) or
        unchecked((int) 0x887A0006) or
        unchecked((int) 0x887A0007) or
        unchecked((int) 0x887A0026);

    public static T? FindVisualAncestor<T>(DependencyObject? obj) where T : DependencyObject {
        if (obj != null) {
            var parent = VisualTreeHelper.GetParent(obj);
            while (parent != null) {
                if (parent is T typed) return typed;

                parent = VisualTreeHelper.GetParent(parent);
            }
        }

        return null;
    }

    /// <summary>
    ///     Use a fake D3DImage to bump up frame rate.
    /// </summary>
    private sealed class D3DImageExt : D3DImage, IDisposable {
        private readonly D3D9ImageSourceInterop interop;

        public D3DImageExt(int adapterIndex = 0) {
            AdapterIndex = adapterIndex;
            interop = new D3D9ImageSourceInterop(adapterIndex);
            interop.CreateRenderTarget(1, 1);
            Lock();
            try {
                SetBackBuffer(D3DResourceType.IDirect3DSurface9, interop.SurfacePointer, true);
                AddDirtyRect(new Int32Rect(0, 0, 1, 1));
            } finally {
                Unlock();
            }
        }

        public int AdapterIndex { get; }

        public void Dispose() {
            Lock();
            try {
                SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
            } finally {
                Unlock();
            }

            interop.Dispose();
        }

        public void InvalidateD3DImage() {
            Lock();
            try {
                AddDirtyRect(new Int32Rect(0, 0, 1, 1));
            } finally {
                Unlock();
            }
        }

        public bool IsDeviceStateOk() => interop.IsDeviceStateOk();
    }

    #region IDisposable Support

    private bool disposedValue; // To detect redundant calls

    protected void Dispose(bool disposing) {
        winformHost.Dispose();
        image.Source = null;
        image3D?.Dispose();
        compositionTarget.Dispose();
        if (!disposedValue) {
            if (disposing)
                if (!belongsToParentWindow)
                    EndD3D();
            // TODO: dispose managed state (managed objects).
            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.
            disposedValue = true;
        }
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}