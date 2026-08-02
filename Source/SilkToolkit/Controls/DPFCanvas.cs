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
using System.Windows.Media;
using System.Windows.Threading;
using HelixToolkit.Logger;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.Wpf.SharpDX.Controls;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.Wpf.SharpDX;

// ---- BASED ON ORIGNAL CODE FROM -----
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

/// <summary>
/// </summary>
/// <seealso cref="System.Windows.Controls.Image" />
public class DPFCanvas : Image, IRenderCanvas, IDisposable {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;
    private readonly bool belongsToParentWindow;

    private readonly CompositionTargetEx compositionTarget = new();
    private double dpiScale = 1;

    private bool enableDpiScale = true;
    private Window parentWindow;

    private DispatcherOperation resizeOperation;

    /// <summary>
    /// </summary>
    static DPFCanvas() {
        StretchProperty.OverrideMetadata(typeof(DPFCanvas), new FrameworkPropertyMetadata(Stretch.Fill));
    }

    /// <summary>
    /// </summary>
    public DPFCanvas(bool deferredRendering = false, bool attachedToWindow = true) {
        if (deferredRendering)
            RenderHost =
                new DX11ImageSourceRenderHost(device => new DeferredContextRenderer(
                                                  device,
                                                  new AutoRenderTaskScheduler()));
        else
            RenderHost = new DX11ImageSourceRenderHost();
        RenderHost.DpiScale = EnableDpiScale ? (float)DpiScale : 1;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        RenderHost.StartRenderLoop += RenderHost_StartRenderLoop;
        RenderHost.StopRenderLoop += RenderHost_StopRenderLoop;
        RenderHost.ExceptionOccurred += (s, e) => { HandleExceptionOccured(e.Exception); };
        (RenderHost as DX11ImageSourceRenderHost).OnImageSourceChanged += DPFCanvas_OnImageSourceChanged;
        belongsToParentWindow = attachedToWindow;
    }

    /// <summary>
    ///     Gets or sets the render host.
    /// </summary>
    /// <value>
    ///     The render host.
    /// </value>
    public IRenderHost RenderHost { get; }

    public double DpiScale {
        get => dpiScale;
        set {
            dpiScale = value;
            RenderHost?.DpiScale = (float)value;
        }
    }

    public bool EnableDpiScale {
        get => enableDpiScale;
        set {
            enableDpiScale = value;
            RenderHost?.DpiScale = value ? (float)DpiScale : 1;
        }
    }

    /// <summary>
    ///     Fired whenever an exception occurred on this object.
    /// </summary>
    public event EventHandler<RelayExceptionEventArgs> ExceptionOccurred = delegate { };

    private void DPFCanvas_OnImageSourceChanged(object sender, DX11ImageSourceArgs e) {
        Source = e.Source;
    }

    /// <summary>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnLoaded(object sender, RoutedEventArgs e) {
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

    private void ParentWindow_Closed(object sender, EventArgs e) {
        Source = null;
        EndD3D();
    }

    /// <summary>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnUnloaded(object sender, RoutedEventArgs e) {
        if (belongsToParentWindow && parentWindow != null) parentWindow.Closed -= ParentWindow_Closed;
        if (DataContext == null && RenderHost.EffectsManager == null && belongsToParentWindow)
            EndD3D();
        else
            RenderHost.StopRendering();
    }

    /// <summary>
    /// </summary>
    private bool StartD3D() {
        RenderHost.StartD3D((int)ActualWidth, (int)ActualHeight);
        return true;
    }

    private void RenderHost_StopRenderLoop(object sender, EventArgs e) {
        compositionTarget.Rendering -= CompositionTargetEx_Rendering;
    }

    private void RenderHost_StartRenderLoop(object sender, EventArgs e) {
        compositionTarget.Rendering -= CompositionTargetEx_Rendering;
        compositionTarget.Rendering += CompositionTargetEx_Rendering;
    }

    private void CompositionTargetEx_Rendering(object sender, RenderingEventArgs e) {
        RenderHost.UpdateAndRender();
    }

    /// <summary>
    /// </summary>
    private void EndD3D() {
        RenderHost.EndD3D();
    }

    /// <summary>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e) {
        if (Logger.IsEnabled(LogLevel.Debug)) Logger.Debug("OnIsFrontBufferAvailableChanged: {Value0}", (bool)e.NewValue);
        // this fires when the screensaver kicks in, the machine goes into sleep or hibernate
        // and any other catastrophic losses of the d3d device from WPF's point of view
        if (true.Equals(e.NewValue))
            try {
                // Try to recover from DeviceRemoved/DeviceReset
                EndD3D();
                StartD3D();
            } catch (Exception ex) {
                if (!HandleExceptionOccured(ex))
                    MessageBox.Show($"DPFCanvas: Error during rendering: {ex.Message} \n StackTrace: {ex.StackTrace}",
                                    "Error");
            }
    }

    /// <summary>
    /// </summary>
    /// <param name="sizeInfo"></param>
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
        if (resizeOperation != null && resizeOperation.Status == DispatcherOperationStatus.Pending)
            resizeOperation.Abort();
        resizeOperation = Dispatcher.BeginInvoke(DispatcherPriority.Background,
                                                 (Action)(() => {
                                                     if (IsLoaded)
                                                         try {
                                                             RenderHost.Resize(
                                                                 (int)ActualWidth,
                                                                 (int)ActualHeight);
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

        Logger.Error(exception, "Render canvas exception.");
        var args = new RelayExceptionEventArgs(exception);
        ExceptionOccurred(this, args);
        return args.Handled;
    }

    private static bool IsDeviceLost(int hresult) {
        return hresult == unchecked((int)0x887A0005)
               || hresult == unchecked((int)0x887A0006)
               || hresult == unchecked((int)0x887A0007)
               || hresult == unchecked((int)0x887A0026);
    }

    public static T FindVisualAncestor<T>(DependencyObject obj) where T : DependencyObject {
        if (obj != null) {
            var parent = VisualTreeHelper.GetParent(obj);
            while (parent != null) {
                if (parent is T typed) return typed;

                parent = VisualTreeHelper.GetParent(parent);
            }
        }

        return null;
    }

    #region IDisposable Support

    private bool disposedValue; // To detect redundant calls

    protected virtual void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing) {
                if (!belongsToParentWindow)
                    EndD3D();

                compositionTarget.Dispose();
                Source = null;
                // TODO: dispose managed state (managed objects).
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            disposedValue = true;
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~DPFCanvas() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    public void Dispose() {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        // TODO: uncomment the following line if the finalizer is overridden above.
        // GC.SuppressFinalize(this);
    }

    #endregion
}
