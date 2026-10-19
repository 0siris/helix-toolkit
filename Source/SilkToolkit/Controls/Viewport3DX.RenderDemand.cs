using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.Wpf.SharpDX.Controls;
using Visibility = System.Windows.Visibility;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>Coordinates demand and nonblocking frame consumption for the actual viewport host.</summary>
public partial class Viewport3DX {
    /// <summary>Retains scene demand independently of GPU completion and presentation recovery.</summary>
    private readonly D3D12RenderDemand renderDemand = new();

    /// <summary>Reuses the frame root snapshot without invoking the public iterator property.</summary>
    private readonly FastList<SceneNode> frameRoots = [];

    /// <summary>Reuses the Scene2D root snapshot for the actual frame being recorded.</summary>
    private readonly FastList<SceneNode2D> frameRoots2D = [];

    /// <summary>Reuses the dispatcher callback for worker-thread visual invalidation.</summary>
    private readonly Action invalidateVisualCallback;

    /// <summary>Coalesces outstanding worker-thread visual notifications into one dispatcher operation.</summary>
    private int visualInvalidationQueued;

    /// <summary>Protects scene snapshots and completion callbacks from nested frame recording.</summary>
    private bool isD3D12FrameUpdating;

    /// <summary>Gets or sets the internal demand consumer; existing HWND callers retain continuous behavior.</summary>
    internal bool RenderOnDemand { get; set; }

    /// <summary>Gets the coordinator shared by future WPF recovery/publication and actual frame preparation.</summary>
    internal D3D12RenderDemand RenderDemand => renderDemand;

    /// <summary>Records demand immediately, then performs or coalesces dispatcher-owned visual notifications.</summary>
    /// <param name="kind">The scene changes to preserve for the next prepared frame.</param>
    private void RequestD3D12Render(D3D12RenderInvalidation kind) {
        if (IsDisposed || Dispatcher.HasShutdownStarted) return;
        renderDemand.Request(kind);
        if (Dispatcher.CheckAccess()) {
            ApplyRenderVisualInvalidation();
        } else if (Interlocked.CompareExchange(ref visualInvalidationQueued, 1, 0) == 0) {
            Dispatcher.BeginInvoke(DispatcherPriority.Render, invalidateVisualCallback);
        }
    }

    /// <summary>Applies visual notifications only on the dispatcher, releasing the coalesced callback slot first.</summary>
    private void ApplyRenderVisualInvalidation() {
        Interlocked.Exchange(ref visualInvalidationQueued, 0);
        if (IsDisposed || Dispatcher.HasShutdownStarted) return;
        InvalidateVisual();
        d3d12Surface?.InvalidateVisual();
        d3d12Surface?.WindowHost.InvalidateVisual();
    }

    /// <summary>Applies the public budget setting to future invalidations without creating demand.</summary>
    /// <param name="owner">The viewport receiving the new setting.</param>
    /// <param name="args">The coerced frame-budget change.</param>
    private static void MinimumUpdateCountChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) =>
        ((Viewport3DX) owner).renderDemand.MinimumUpdateCount = (int) args.NewValue;

    /// <summary>Retains the historical nonnegative public frame-budget setting.</summary>
    /// <param name="owner">The viewport whose metadata is being evaluated.</param>
    /// <param name="value">The requested frame budget.</param>
    /// <returns>The requested budget clamped to zero.</returns>
    private static object CoerceMinimumUpdateCount(DependencyObject owner, object value) => Math.Max(0, (int) value);

    /// <summary>Updates lightweight camera state and records only when frame ownership and demand permit it.</summary>
    /// <param name="renderingTime">The WPF rendering timestamp.</param>
    /// <param name="captureFrame">Whether an explicit caller requests synchronous readback.</param>
    /// <param name="waitForCompletion">Whether an explicit caller may drain the pending GPU frame.</param>
    /// <returns>The recorded scene draw count; zero also represents idle, busy, or empty scene work.</returns>
    internal int RenderD3D12Frame(TimeSpan renderingTime, bool captureFrame = false, bool waitForCompletion = true) {
        var surface = d3d12Surface;
        var camera = CameraCore;
        if (surface is not { IsInitialized: true } || camera is null || Visibility != Visibility.Visible) return 0;
        if (isD3D12FrameUpdating) {
            renderDemand.Request(D3D12RenderInvalidation.Render);
            return 0;
        }

        isD3D12FrameUpdating = true;
        var reserved = false;
        var beforeSubmission = surface.PresentedFrames;
        var onDemand = RenderOnDemand;
        try {
            if (!onDemand) cameraController.OnCompositionTargetRendering(renderingTime.Ticks);
            if (!surface.CompletePendingFrame(waitForCompletion || captureFrame)) {
                if (onDemand) cameraController.OnCompositionTargetRendering(renderingTime.Ticks);
                return 0;
            }

            RaiseCompletedD3D12Frames(surface);
            if (!ReferenceEquals(d3d12Surface, surface) || surface.IsDisposed) return 0;
            beforeSubmission = surface.PresentedFrames;
            if (onDemand) {
                if (captureFrame) renderDemand.Request(D3D12RenderInvalidation.Render);
                reserved = renderDemand.TryBeginFrame(out _);
            }

            if (onDemand) cameraController.OnCompositionTargetRendering(renderingTime.Ticks);
            if (!ReferenceEquals(d3d12Surface, surface) || surface.IsDisposed || IsDisposed) return 0;
            if (onDemand && !reserved && !renderDemand.TryBeginFrame(out _)) return 0;
            reserved |= onDemand;
            var recorded = RecordD3D12ViewportFrame(surface, camera, captureFrame, waitForCompletion);
            RaiseCompletedD3D12Frames(surface);
            return recorded;
        } finally {
            if (reserved) renderDemand.CompleteFrame(surface.PresentedFrames != beforeSubmission);
            frameRoots.Clear();
            frameRoots2D.Clear();
            isD3D12FrameUpdating = false;
        }
    }

    /// <summary>Fills reusable roots immediately before preparation while retaining all public enumeration APIs.</summary>
    private void FillD3D12FrameRoots() {
        frameRoots.Clear();
        frameRoots2D.Clear();
        for (var index = 0; index < Items.Count; index++) frameRoots.Add(Items[index].SceneNode);
        if (EnableSharedModelMode && SharedModelContainerInternal is { } shared) {
            if (shared is ModelContainer3DX container) {
                for (var index = 0; index < container.Items.Count; index++)
                    if (container.Items[index] is Element3D element)
                        frameRoots.Add(element.SceneNode);
            } else {
                foreach (var root in shared.Renderables) frameRoots.Add(root);
            }
        }

        if (viewCube is { } cube) frameRoots.Add(cube.SceneNode);
        if (coordinateView is { } coordinate) frameRoots.Add(coordinate.SceneNode);
        frameRoots2D.Add(Overlay2D.SceneNode);
        if (frameStatisticModel is { } statistics) frameRoots2D.Add(statistics.SceneNode);
    }
}
