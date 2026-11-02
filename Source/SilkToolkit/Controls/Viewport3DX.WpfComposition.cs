using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX.Controls;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>Connects the productive WPF presentation owner to the selected viewport backend.</summary>
public partial class Viewport3DX {
    /// <summary>The composited owner selected by the public template or an internal presentation harness.</summary>
    private D3D12WpfPresentation? wpfPresentation;

    /// <summary>Retains one permission delegate for native preparation across application node callbacks.</summary>
    private Func<bool>? scenePreparationPermission;

    /// <summary>Gets the cached live permission predicate without allocating during warmed scene preparation.</summary>
    internal Func<bool> ScenePreparationPermission => scenePreparationPermission ??= IsScenePreparationPermitted;

    /// <summary>Rejects further scene access after a callback requests manager retirement, hiding or disposal.</summary>
    /// <returns>Whether both presentation lifetimes still permit borrowing scene resources.</returns>
    private bool IsScenePreparationPermitted() => CanRenderWpfScene && CanSubmitNativeScene;

    /// <summary>Gets whether automatic rendering is enabled for this actual host.</summary>
    internal bool WpfRenderingEnabled => renderingEnabled;

    /// <summary>Gets whether a new WPF scene frame may update the actual viewport.</summary>
    internal bool CanRenderWpfScene => !IsDisposed && CanRenderWithEffectsManager &&
                                       Visibility == System.Windows.Visibility.Visible && CameraCore is not null;

    /// <summary>Attaches one internal composited owner before any HWND owner has been initialized.</summary>
    /// <param name="canvas">The actual borrowed WPF presentation canvas.</param>
    /// <param name="driverType">The native driver selection; interop support must be established explicitly.</param>
    /// <returns>The productive owner using this viewport's existing scene and demand.</returns>
    internal D3D12WpfPresentation InitializeWpfPresentation(
        DPFCanvas canvas,
        SilkDriverType driverType = SilkDriverType.Hardware
    ) {
        Dispatcher.VerifyAccess();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (EnableSwapChainRendering)
            throw new InvalidOperationException("WPF presentation requires EnableSwapChainRendering to be false.");
        if (d3d12Surface is not null || wpfPresentation is not null)
            throw new InvalidOperationException("A viewport presentation owner has already been selected.");
        d3d12CompositionTarget.Rendering -= D3D12CompositionTargetRendering;
        canvas.EnableDpiScale = EnableDpiScale;
        canvas.DpiScale = DpiScale;
        wpfPresentation = new D3D12WpfPresentation(this, canvas, driverType);
        RenderOnDemand = true;
        canvas.PresentationChanged += CanvasPresentationChanged;
        return wpfPresentation;
    }

    /// <summary>Prepares the selected WPF owner before the caller releases application scene ownership.</summary>
    /// <returns>Completion after bounded image-lock retries without disposing application nodes or their manager.</returns>
    public async Task PrepareForDisposalAsync() {
        Dispatcher.VerifyAccess();
        if (IsDisposed) return;
        VerifyPresentationReplacement();
        var owner = wpfPresentation;
        if (owner is null) return;
        owner.BeginDisposalPreparation();
        var started = Stopwatch.GetTimestamp();
        while (!owner.TryFinishDisposalPreparation()) {
            if (Stopwatch.GetElapsedTime(started) >= D3D12WpfPresentation.DisposalTimeout)
                throw new TimeoutException("The WPF image is still bound; native resources remain owned for disposal retry.");
            if (Dispatcher.HasShutdownStarted)
                throw new InvalidOperationException("The dispatcher is shutting down before WPF detachment completed");
            await Task.Delay(16);
            if (IsDisposed) return;
            if (!ReferenceEquals(wpfPresentation, owner))
                throw new InvalidOperationException("The WPF presentation owner changed during disposal preparation");
            VerifyPresentationReplacement();
        }
    }

    /// <summary>Completes queued disposal after recording unwinds and reports failures through the retained owner.</summary>
    /// <param name="owner">The exact presentation owner that retained the disposal request.</param>
    internal async void FinishWpfViewportDisposalAsync(D3D12WpfPresentation owner) {
        try {
            Dispatcher.VerifyAccess();
            if (IsDisposed || owner.IsDisposed || !OwnsWpfPresentation(owner)) return;
            await PrepareForDisposalAsync();
            if (IsDisposed || owner.IsDisposed || !OwnsWpfPresentation(owner)) return;
            Dispose();
        } catch (Exception exception) {
            if (OwnsWpfPresentation(owner))
                owner.ReportAutomaticRenderFailure(exception);
            else
                LoggerLib.Logger.Error(exception, "Deferred WPF viewport disposal failed after its owner was retired");
        }
    }

    /// <summary>Runs the existing lightweight camera controller after a frame reservation, or to discover camera demand.</summary>
    /// <param name="renderingTime">The original WPF timestamp.</param>
    internal void UpdateWpfCamera(TimeSpan renderingTime) =>
        cameraController.OnCompositionTargetRendering(renderingTime.Ticks);

    /// <summary>Records the same scene/pass sequence into the bridge target using retained root snapshots.</summary>
    /// <param name="renderer">The owned common recorder, with preceding GPU work complete.</param>
    /// <param name="commands">The caller-owned open command context.</param>
    /// <param name="target">The borrowed shared color allocation and RTV.</param>
    /// <param name="dpiScale">The actual canvas render scale.</param>
    /// <returns>The number of actual recorded native draws.</returns>
    internal int RecordWpfScene(
        D3D12FrameRenderer renderer,
        SilkD3D12CommandContext commands,
        in D3D12FrameTarget target,
        float dpiScale
    ) {
        FillD3D12FrameRoots();
        try {
            wpfPresentation!.SetScene2DRoots(frameRoots2D);
            renderer.SetEffectsManager(EffectsManager);
            var clear = BackgroundColor.ToColor4();
            return renderer.RecordViewportFrame(
                commands, in target, dpiScale,
                [clear.X, clear.Y, clear.Z, clear.W], frameRoots, CameraCore!, EnableRenderFrustum,
                IsShadowMappingEnabled, OitRenderMode, OitDepthPeelingIteration, (float) OitWeightPower,
                (float) OitWeightDepthSlope, OitWeightMode, FxaaLevel, EnableSsao, (float) SsaoSamplingRadius,
                (float) SsaoIntensity, SsaoQuality, frameRoots2D, EnableAutoOctreeUpdate, EnableRenderOrder, this
            );
        } finally {
            frameRoots.Clear();
            frameRoots2D.Clear();
        }
    }

    /// <summary>Delivers GPU completion once and rejects reentrant owner replacement or disposal between listeners.</summary>
    /// <param name="owner">The actual composited generation owner.</param>
    internal void RaiseCompletedWpfFrames(D3D12WpfPresentation owner) {
        while (owner.TryTakeCompletedFrame()) {
            completedD3D12Frames++;
            Rendered?.Invoke(this, EventArgs.Empty);
            if (!ReferenceEquals(wpfPresentation, owner) || owner.IsDisposed || IsDisposed) return;
            OnRendered?.Invoke(owner.Canvas, EventArgs.Empty);
            if (!ReferenceEquals(wpfPresentation, owner) || owner.IsDisposed || IsDisposed) return;
        }
    }
}