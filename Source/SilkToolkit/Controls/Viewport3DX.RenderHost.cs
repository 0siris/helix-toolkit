using System;
using System.Collections.Generic;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Controls;

namespace HelixToolkit.Wpf.SharpDX;

public partial class Viewport3DX : IRenderHost {
    /// Gets the actual viewport frame owner.
    public IRenderHost RenderHost => this;

    /// Gets this host's actual viewport; assigning a foreign viewport is unsupported.
    public IViewport3DX Viewport {
        get => this;
        set {
            if (!ReferenceEquals(value, this))
                throw new NotSupportedException("A viewport render host cannot change its owning viewport");
        }
    }

    /// Gets the actual native hierarchy; no scene is prepared before the first native frame.
    public FastList<KeyValuePair<int, SceneNode>> PerFrameFlattenedScene =>
        wpfPresentation?.PerFrameFlattenedScene ?? d3d12Surface?.PerFrameFlattenedScene ?? emptyFlattenedScene;

    /// The empty pre-initialization result; it never owns or prepares scene nodes.
    private readonly FastList<KeyValuePair<int, SceneNode>> emptyFlattenedScene = [];

    /// Gets the retained actual native update context, or null before the first frame.
    public RenderContext? RenderContext => wpfPresentation?.RenderContext ?? d3d12Surface?.RenderContext;

    float IRenderHost.DpiScale {
        get => wpfPresentation?.Canvas.EffectiveDpiScale ??
               (float) (d3d12Surface?.EffectiveDpiScale ?? (EnableDpiScale
                   ? DpiScale
                   : 1));
        set {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            DpiScale = value;
            InvalidateRender();
        }
    }

    /// Occurs only at the completed native frame notification point.
    public event EventHandler? Rendered;

    /// <summary>
    ///     Updates and renders one real frame, waiting for GPU completion.
    /// </summary>
    /// <returns>Whether a real frame was submitted or completed, including zero-draw frames.</returns>
    public bool UpdateAndRender() {
        if (wpfPresentation is { } composited) {
            var submitted = composited.SubmittedFrames;
            var completedCount = completedD3D12Frames;
            _ = composited.RenderFrame(
                TimeSpan.FromSeconds(
                    (double) System.Diagnostics.Stopwatch.GetTimestamp() /
                    System.Diagnostics.Stopwatch.Frequency
                ), waitForCompletion: true
            );
            return composited.SubmittedFrames != submitted || completedD3D12Frames != completedCount;
        }

        var surface = d3d12Surface;
        var presented = surface?.PresentedFrames ?? 0;
        var completed = completedD3D12Frames;
        _ = RenderD3D12Frame(
            TimeSpan.FromSeconds(
                (double) System.Diagnostics.Stopwatch.GetTimestamp() /
                System.Diagnostics.Stopwatch.Frequency
            ), waitForCompletion: true
        );
        return completedD3D12Frames != completed || (surface is not null && surface.PresentedFrames != presented);
    }

    /// Counts completed frame notifications independently of draw counts and surface replacement.
    private ulong completedD3D12Frames;

    /// <summary>
    ///     Queues the host size through the actual native physical resize queue.
    /// </summary>
    /// <param name="width">The device-independent width.</param>
    /// <param name="height">The device-independent height.</param>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is negative.</exception>
    public void Resize(int width, int height) {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);
        d3d12Surface?.Resize(width, height);
        if (wpfPresentation is { } composited) {
            composited.Canvas.Width = width;
            composited.Canvas.Height = height;
        }

        InvalidateRender();
    }

    /// Starts the existing WPF composition subscription, retaining one handler.
    public void StartRendering() {
        renderingEnabled = true;
        d3d12CompositionTarget.Rendering -= D3D12CompositionTargetRendering;
        if (EnableSwapChainRendering) d3d12CompositionTarget.Rendering += D3D12CompositionTargetRendering;
        InvalidateRender();
    }

    /// Stops the existing WPF composition subscription.
    public void StopRendering() {
        renderingEnabled = false;
        d3d12CompositionTarget.Rendering -= D3D12CompositionTargetRendering;
    }

    /// Whether the actual composition rendering subscription is enabled across template and load transitions.
    private bool renderingEnabled = true;

    /// Requests preparation of current per-frame renderables on the next real frame.
    public void InvalidatePerFrameRenderables() => RequestD3D12Render(
        D3D12RenderInvalidation.PerFrameRenderables | D3D12RenderInvalidation.Render
    );
}
