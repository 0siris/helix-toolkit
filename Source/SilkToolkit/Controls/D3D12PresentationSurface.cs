using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Cameras;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Utilities;
using Silk.NET.Direct3D12;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>
///     Owns the pure-WPF HWND and the single-inflight, fence-safe Direct3D 12 presentation lifecycle.
/// </summary>
public sealed class D3D12PresentationSurface : Grid, IDisposable {
    /// <summary>Gets or sets the actual viewport host without changing construction or rendering signatures.</summary>
    internal IRenderHost? RenderHost { get; set; }

    /// <summary>Gets the actual update context retained until replacement after fence completion or disposal.</summary>
    internal RenderContext? RenderContext => frameRenderer?.RenderContext;

    /// <summary>Gets the renderer-owned hierarchy, or null before native initialization.</summary>
    internal FastList<KeyValuePair<int, SceneNode>>? PerFrameFlattenedScene => frameRenderer?.PerFrameFlattenedScene;

    /// <summary>Gets the shared renderer that owns the productive scene and depth resources.</summary>
    internal D3D12FrameRenderer? FrameRenderer => frameRenderer;

    /// <summary>Gets whether pending retirement or disposal must exclude this generation from scene submission.</summary>
    internal bool IsRetirementPending => retireRequested || disposeRequested || retiringNative;

    /// <summary>The native child-window transport host.</summary>
    private readonly HwndSwapChainHost windowHost = new();

    /// <summary>The coalesced physical resize requests.</summary>
    private readonly D3D12ResizeQueue resizeQueue = new();

    /// <summary>Protects the single reusable command context until its submitted GPU fence completes.</summary>
    private readonly D3D12FrameSubmission _frameSubmission = new();

    /// <summary>Retries the latest queued resize without blocking or spinning while a frame is pending.</summary>
    private readonly DispatcherTimer _resizeRetryTimer;

    /// <summary>Records a requested effects-manager change until the preceding GPU frame has completed.</summary>
    private bool _effectsManagerDirty;

    /// <summary>The requested hardware or WARP adapter type.</summary>
    private readonly SilkDriverType driverType;

    /// <summary>The borrowed current effects manager.</summary>
    private IEffectsManager? effectsManager;

    /// <summary>Protects borrowed renderer, command and target references until the surface frame unwinds.</summary>
    private bool frameActive;

    /// <summary>Tracks an open command list that must be closed before abandoning its native generation.</summary>
    private bool commandsOpen;

    /// <summary>Retains generation retirement requested by unload, a barrier or an abandoned recording.</summary>
    private bool retireRequested;

    /// <summary>Retains whole-surface disposal without releasing the HWND host during a scene callback.</summary>
    private bool disposeRequested;

    /// <summary>Prevents reentrant native retirement or initialization while a bounded drain is in progress.</summary>
    private bool retiringNative;

    /// <summary>Distinguishes unloaded surfaces from standalone surfaces that have not received their first load.</summary>
    private bool suspended;

    /// <summary>Records load permission independently of handle creation and nested routed lifecycle events.</summary>
    private bool loaded;

    /// <summary>Reuses the dispatcher callback for rare deferred retirement without allocating on frame ticks.</summary>
    private readonly Action retireNativeCallback;

    /// <summary>Retains at most one queued retirement callback across nested dispatcher pumps.</summary>
    private DispatcherOperation? retirementOperation;

    /// <summary>Retains explicit capture storage until submission is drained, including exceptional signal failures.</summary>
    private SilkD3D12Resource? captureReadback;

    /// <summary>Gets whether this surface or its owning viewport still borrows the generation on the CPU stack.</summary>
    private bool HasActiveFrame => frameActive || RenderHost is Viewport3DX { IsPreparingPresentationFrame: true };

    /// <summary>Gets live submission permission after all node callbacks and before command execution.</summary>
    private bool CanSubmitFrame => !IsDisposed && !IsRetirementPending && !suspended &&
                                   (RenderHost is not Viewport3DX viewport || viewport.CanSubmitNativeScene);

    /// <summary>Controls render resolution independently of physical native pointer decoding.</summary>
    private bool enableDpiScale = true;

    /// <summary>Retains an explicit render scale until a monitor DPI transition replaces it.</summary>
    private double? dpiScale;

    /// <summary>Gets or sets whether render targets scale with DPI; native input always uses the physical DPI.</summary>
    internal bool EnableDpiScale {
        get => enableDpiScale;
        set {
            Dispatcher.VerifyAccess();
            if (enableDpiScale == value) return;
            enableDpiScale = value;
            QueueResize();
        }
    }

    /// <summary>Gets or sets the desired render scale without changing child-window input coordinates.</summary>
    internal double DpiScale {
        get => dpiScale ?? windowHost.DpiScale;
        set {
            Dispatcher.VerifyAccess();
            if (!(value > 0) || !double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (dpiScale == value) return;
            dpiScale = value;
            QueueResize();
        }
    }

    /// <summary>Gets the render target scale independently of the native window's physical input scale.</summary>
    internal double EffectiveDpiScale => enableDpiScale
        ? DpiScale
        : 1;

    /// <summary>The pending dispatcher operation for a physical resize.</summary>
    private DispatcherOperation? resizeOperation;

    /// <summary>The device owned by the presentation lifetime.</summary>
    private SilkD3D12Device? device;

    /// <summary>The weak registry membership of this native generation.</summary>
    private D3D12PresentationDeviceRecovery? recoveryRegistration;

    /// <summary>The removed singleton whose productive owners must all retire before recreation.</summary>
    private D3D12PresentationDeviceRecovery? recoveryBarrier;

    /// <summary>The retained callback for synchronous native discovery and retirement.</summary>
    private readonly Action retireRemovedDevice;

    /// <summary>The retained error boundary for asynchronous device discovery and peer retirement.</summary>
    private readonly Action queuedDeviceRemoval;

    /// <summary>The queue retained for submission and native completion.</summary>
    private SilkD3D12CommandQueue? commandQueue;

    /// <summary>The single reusable command context protected by the frame submission.</summary>
    private SilkD3D12CommandContext? commandContext;

    /// <summary>The completion fence protecting commands and renderer resources.</summary>
    private SilkD3D12Fence? fence;

    /// <summary>The HWND color buffers and presentation transport.</summary>
    private SilkD3D12SwapChain? swapChain;

    /// <summary>The common scene recorder and its fence-protected native resource owners.</summary>
    private D3D12FrameRenderer? frameRenderer;

    /// <summary>
    ///     Creates a presentation surface using hardware rendering by default.
    /// </summary>
    /// <param name="driverType">The hardware or WARP device type.</param>
    /// <param name="effectsManager">The optional borrowed scene effects manager.</param>
    public D3D12PresentationSurface(
        SilkDriverType driverType = SilkDriverType.Hardware,
        IEffectsManager? effectsManager = null
    ) {
        this.driverType = driverType;
        this.effectsManager = effectsManager;
        retireNativeCallback = ApplyPendingRetirement;
        retireRemovedDevice = OnDeviceRemoval;
        queuedDeviceRemoval = OnQueuedDeviceRemoval;
        _resizeRetryTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher) { Interval = TimeSpan.FromMilliseconds(16) };
        _resizeRetryTimer.Tick += RetryPendingResize;
        Children.Add(windowHost);
        windowHost.HandleCreated += OnHandleCreated;
        windowHost.HandleDestroyed += OnHandleDestroyed;
        windowHost.DpiScaleChanged += OnDpiScaleChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>
    ///     Gets the hosted native window.
    /// </summary>
    public HwndSwapChainHost WindowHost => windowHost;

    /// <summary>
    ///     Gets whether native presentation resources are initialized.
    /// </summary>
    public bool IsInitialized => swapChain is not null;

    /// <summary>
    ///     Gets the current physical back-buffer width.
    /// </summary>
    public uint PixelWidth => swapChain?.Width ?? 0;

    /// <summary>
    ///     Queues a technique-registry replacement for the next completed GPU frame.
    /// </summary>
    /// <param name="value">The current viewport effects manager.</param>
    internal void SetEffectsManager(IEffectsManager? value) {
        Dispatcher.VerifyAccess();
        if (ReferenceEquals(effectsManager, value)) return;

        effectsManager = value;
        _effectsManagerDirty = true;
    }

    /// <summary>Drains and releases this generation while preserving the HWND host and borrowed application manager.</summary>
    /// <remarks>The caller delivers preceding frame completions before requesting manager-resource retirement.</remarks>
    /// <exception cref="TimeoutException">The bounded drain failed; ownership is retained for a controlled retry.</exception>
    internal void RetireSceneResources() {
        Dispatcher.VerifyAccess();
        if (IsDisposed) return;
        retireRequested = true;
        if (HasActiveFrame || retiringNative) return;
        StopNative();
        retireRequested = false;
    }

    /// <summary>Recreates native resources lazily only when load, handle and live host permission allow it.</summary>
    internal void EnsureNativeInitialized() {
        Dispatcher.VerifyAccess();
        if (IsDisposed || disposeRequested || retiringNative || HasActiveFrame || suspended ||
            !(loaded || IsLoaded) || !windowHost.IsHandleCreated) return;
        if (RenderHost is Viewport3DX { CanSubmitNativeScene: false }) return;
        if (ObserveDeviceRemoval()) return;
        if (recoveryBarrier is { CanRecreate: false }) return;
        if (retireRequested) RetireSceneResources();
        StartNative();
    }

    /// <summary>Recognizes removal before completion polling, initialization, or command submission.</summary>
    /// <returns>Whether the current call must cancel its borrowed frame.</returns>
    private bool ObserveDeviceRemoval() {
        if (device is null || (recoveryRegistration is not { IsRemovalRequested: true } &&
                               device.GetDeviceRemovedReason() >= 0)) return false;
        recoveryBarrier ??= recoveryRegistration;
        recoveryRegistration?.RequestRemoval();
        OnDeviceRemoval();
        return true;
    }

    /// <summary>Reports queued retirement failures while retaining failed native ownership for a controlled retry.</summary>
    private void OnQueuedDeviceRemoval() {
        try {
            OnDeviceRemoval();
        } catch (Exception exception) {
            ReportDeferredFailure(exception);
        }
    }

    /// <summary>Reports an automatic failure and contains a second teardown failure without losing native ownership.</summary>
    /// <param name="exception">The failure from queued recovery, retirement, or resize work.</param>
    private void ReportDeferredFailure(Exception exception) {
        try {
            if (DeferredRenderException is { } handler) handler(this, new RelayExceptionEventArgs(exception));
            else LoggerLib.Logger.Error(exception, "Native recovery failed; resources remain owned for retry");
        } catch (Exception retryException) {
            retirementOperation?.Abort();
            retirementOperation = null;
            LoggerLib.Logger.Error(retryException, "Native recovery teardown failed; resources remain owned for retry");
            if (RenderHost is Viewport3DX viewport)
                viewport.SetCurrentValue(Viewport3DX.RenderExceptionProperty, retryException);
        }
    }

    /// <summary>Confirms requested discovery and retires removal when no CPU callback still borrows its context.</summary>
    private void OnDeviceRemoval() {
        Dispatcher.VerifyAccess();
        if (IsDisposed || device is null || recoveryRegistration is null) return;
        if (!recoveryRegistration.IsRemovalRequested) {
            if (device.GetDeviceRemovedReason() >= 0) return;
            recoveryBarrier ??= recoveryRegistration;
            recoveryRegistration.RequestRemoval();
            return;
        }

        recoveryBarrier ??= recoveryRegistration;
        retireRequested = true;
        if (RenderHost is Viewport3DX viewport) {
            viewport.RenderDemand.Request(D3D12RenderInvalidation.SceneGraph);
            viewport.RenderDemand.RequestRecovery();
        }

        if (!HasActiveFrame && !retiringNative) RetireSceneResources();
    }

    /// <summary>Schedules one retained retirement callback after both surface and viewport frame stacks unwind.</summary>
    /// <remarks>A nested callback retains its request without reposting until a frame's finally calls this method.</remarks>
    internal void SchedulePendingRetirement() {
        Dispatcher.VerifyAccess();
        if (IsDisposed || (!retireRequested && !disposeRequested) || HasActiveFrame || retiringNative ||
            retirementOperation is not null || Dispatcher.HasShutdownStarted) return;
        retirementOperation = Dispatcher.BeginInvoke(DispatcherPriority.Background, retireNativeCallback);
    }

    /// <summary>Applies deferred lifetime work once, preserving failed requests and all ownership for an explicit retry.</summary>
    private void ApplyPendingRetirement() {
        retirementOperation = null;
        if (IsDisposed || HasActiveFrame || retiringNative) return;
        try {
            if (disposeRequested) Dispose();
            else if (retireRequested) RetireSceneResources();
        } catch (Exception exception) {
            ReportDeferredFailure(exception);
        }
    }

    /// <summary>Closes unsubmitted commands and prevents reuse of their speculative resource-state tracking.</summary>
    private void DiscardRecordedFrame() {
        retireRequested = true;
        LastCapturedFrame = null;
        if (!commandsOpen) return;
        try {
            commandContext!.Close();
        } catch (System.Runtime.InteropServices.COMException) when (device is not null && device.GetDeviceRemovedReason() < 0) {
            _ = ObserveDeviceRemoval();
        }

        commandsOpen = false;
    }

    /// <summary>Replaces effect pipelines only after no submitted frame can still reference the old catalog.</summary>
    /// <exception cref="InvalidOperationException">A submitted frame still owns the effect pipelines.</exception>
    private void ApplyPendingEffectsManager() {
        if (!_effectsManagerDirty || device is null || swapChain is null) return;
        if (_frameSubmission.IsPending)
            throw new InvalidOperationException("Effect pipelines cannot change while a Direct3D 12 frame is pending");
        var renderer = frameRenderer
                       ?? throw new InvalidOperationException("The Direct3D 12 frame renderer is unavailable.");
        renderer.SetEffectsManager(effectsManager);
        _effectsManagerDirty = false;
    }

    /// <summary>Observes or explicitly drains the preceding frame before any resource mutation or command recording.</summary>
    /// <param name="waitForCompletion">Whether an explicit capture or drain may synchronously await the GPU.</param>
    /// <returns>Whether the single frame allocation is available.</returns>
    /// <exception cref="TimeoutException">The preceding frame exceeded its finite completion deadline.</exception>
    /// <exception cref="InvalidOperationException">The pending frame has lost its native fence or device.</exception>
    internal bool CompletePendingFrame(bool waitForCompletion = false) {
        Dispatcher.VerifyAccess();
        if (ObserveDeviceRemoval()) return false;
        if (device is null && recoveryBarrier is { CanRecreate: false }) return false;
        if (!_frameSubmission.IsPending) return true;
        var currentFence = fence
                           ?? throw new InvalidOperationException("The pending Direct3D 12 frame has no completion fence");
        if (device is null) throw new InvalidOperationException("The pending Direct3D 12 frame has no device");
        var completed = currentFence.CompletedValue;
        if (completed == ulong.MaxValue && ObserveDeviceRemoval()) return false;
        if (completed == ulong.MaxValue)
            throw new InvalidOperationException("The Direct3D 12 device stopped reporting valid frame completion");
        if (_frameSubmission.TryComplete(completed, FrameTimestamp())) return true;
        if (!waitForCompletion) return false;
        try {
            currentFence.Wait(_frameSubmission.PendingFenceValue, _frameSubmission.RemainingWait(FrameTimestamp()));
        } catch (Exception) when (device is not null && device.GetDeviceRemovedReason() < 0) {
            _ = ObserveDeviceRemoval();
            return false;
        }

        return CompletePendingFrame();
    }

    /// <summary>Claims one actually completed presentation notification independently of the draw count.</summary>
    /// <returns>Whether one completion had not yet been reported.</returns>
    internal bool TryTakeCompletedFrame() => _frameSubmission.TryTakeCompletion();

    /// <summary>Reads the monotonic clock used for GPU completion deadlines.</summary>
    /// <returns>The current monotonic timestamp.</returns>
    private static TimeSpan FrameTimestamp() => Stopwatch.GetElapsedTime(0);

    /// <summary>
    ///     Gets the current physical back-buffer height.
    /// </summary>
    public uint PixelHeight => swapChain?.Height ?? 0;

    /// <summary>
    ///     Gets the number of successfully presented frames.
    /// </summary>
    public ulong PresentedFrames { get; private set; }

    /// <summary>
    ///     Gets whether this surface has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>Reports a deferred native resize failure through the viewport's existing renderer-error boundary.</summary>
    internal event EventHandler<RelayExceptionEventArgs>? DeferredRenderException;

    /// <summary>
    ///     Clears and presents one frame, waiting for completion before returning.
    /// </summary>
    /// <param name="clearColor">Four RGBA clear components.</param>
    /// <exception cref="ObjectDisposedException">The presentation surface has been disposed.</exception>
    /// <exception cref="TimeoutException">The preceding frame exceeded its finite completion deadline.</exception>
    public void RenderOnce(ReadOnlySpan<float> clearColor) {
        Dispatcher.VerifyAccess();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (frameActive || !CanSubmitFrame) return;
        if (!IsInitialized) EnsureNativeInitialized();
        if (!IsInitialized || !CanSubmitFrame) return;
        if (!CompletePendingFrame(true)) return;
        ApplyPendingResize();
        if (!IsInitialized || !CanSubmitFrame) return;
        frameActive = true;
        try {
            var (currentContext, currentSwapChain, backBuffer) = BeginFrame();
            var target = CreateFrameTarget(currentSwapChain);
            var renderer = frameRenderer
                           ?? throw new InvalidOperationException("The Direct3D 12 frame renderer is unavailable.");
            renderer.RecordClearFrame(currentContext, in target, clearColor);
            _ = EndFrame(currentContext, currentSwapChain, backBuffer);
        } catch (System.Runtime.InteropServices.COMException) when (device is not null && device.GetDeviceRemovedReason() < 0) {
            _ = ObserveDeviceRemoval();
            DiscardRecordedFrame();
            return;
        } catch {
            DiscardRecordedFrame();
            throw;
        } finally {
            frameActive = false;
            SchedulePendingRetirement();
        }
    }

    /// <summary>
    ///     Records visible existing scene nodes into the current swap-chain back buffer and presents the frame.
    /// </summary>
    /// <param name="clearColor">Four RGBA clear components.</param>
    /// <param name="candidates">The opaque or transparent scene candidates in render order.</param>
    /// <param name="transforms">The current camera and viewport transforms.</param>
    /// <param name="testFrustum">Whether camera-frustum culling is enabled.</param>
    /// <param name="frustum">The current camera frustum.</param>
    /// <param name="lights">The optional shared light model.</param>
    /// <returns>The number of recorded draws.</returns>
    internal int RenderSceneOnce(
        ReadOnlySpan<float> clearColor,
        FastList<SceneNode> candidates,
        in GlobalTransformStruct transforms,
        bool testFrustum,
        ref BoundingFrustum frustum,
        LightsBufferModel? lights = null
    ) {
        Dispatcher.VerifyAccess();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (frameActive || !CanSubmitFrame) return 0;
        if (!IsInitialized) EnsureNativeInitialized();
        if (!IsInitialized || !CanSubmitFrame) return 0;
        if (!CompletePendingFrame(true)) return 0;
        ApplyPendingResize();
        if (!IsInitialized || !CanSubmitFrame) return 0;
        ApplyPendingEffectsManager();
        frameActive = true;
        try {
            var renderer = frameRenderer
                           ?? throw new InvalidOperationException("The Direct3D 12 frame renderer is unavailable.");
            var (currentContext, currentSwapChain, backBuffer) = BeginFrame();
            var target = CreateFrameTarget(currentSwapChain);
            var recorded = renderer.RecordSceneFrame(
                currentContext, in target, clearColor, candidates,
                in transforms, testFrustum, ref frustum, lights
            );
            return EndFrame(currentContext, currentSwapChain, backBuffer)
                ? recorded
                : 0;
        } catch (System.Runtime.InteropServices.COMException) when (device is not null && device.GetDeviceRemovedReason() < 0) {
            _ = ObserveDeviceRemoval();
            DiscardRecordedFrame();
            return 0;
        } catch {
            DiscardRecordedFrame();
            throw;
        } finally {
            frameActive = false;
            SchedulePendingRetirement();
        }
    }

    /// <summary>
    ///     Flattens existing viewport roots, applies the current camera, and presents one ordered scene frame.
    /// </summary>
    /// <param name="clearColor">Four RGBA clear components.</param>
    /// <param name="roots">The current viewport scene roots.</param>
    /// <param name="camera">The current viewport camera.</param>
    /// <param name="testFrustum">Whether camera-frustum culling is enabled.</param>
    /// <param name="enableShadows">Whether a visible shadow-map node may record a depth pass.</param>
    /// <param name="oitRenderType">The transparent composition mode.</param>
    /// <param name="oitDepthPeelingIterations">The positive dual-depth-peeling iteration count.</param>
    /// <param name="oitWeightPower">The weighted-OIT depth power.</param>
    /// <param name="oitWeightDepthSlope">The weighted-OIT depth slope.</param>
    /// <param name="oitWeightMode">The weighted-OIT equation selector.</param>
    /// <param name="fxaaLevel">The final full-screen anti-aliasing quality.</param>
    /// <param name="enableSsao">Whether screen-space ambient occlusion is evaluated.</param>
    /// <param name="ssaoRadius">The SSAO view-space sample radius.</param>
    /// <param name="ssaoIntensity">The SSAO intensity multiplier.</param>
    /// <param name="ssaoQuality">The SSAO target resolution.</param>
    /// <param name="roots2D">The optional Scene2D overlays drawn after post processing.</param>
    /// <param name="captureFrame">Whether to retain the completed frame for the existing capture consumer.</param>
    /// <param name="waitForCompletion">Whether explicit callers synchronously await completed GPU work.</param>
    /// <param name="autoUpdateOctree">Whether existing hit-testable dirty geometry updates its octree.</param>
    /// <param name="enableRenderOrder">Whether the original render-order keys sort the relevant draw lists.</param>
    /// <returns>The number of recorded draws.</returns>
    /// <exception cref="ObjectDisposedException">The presentation surface has been disposed.</exception>
    /// <exception cref="InvalidOperationException">A required native presentation owner is unavailable.</exception>
    internal int RenderViewportOnce(
        ReadOnlySpan<float> clearColor,
        IEnumerable<SceneNode> roots,
        CameraCore camera,
        bool testFrustum,
        bool enableShadows = true,
        OITRenderType oitRenderType = OITRenderType.None,
        int oitDepthPeelingIterations = 4,
        float oitWeightPower = 3,
        float oitWeightDepthSlope = 1,
        OITWeightMode oitWeightMode = OITWeightMode.Linear1,
        FXAALevel fxaaLevel = FXAALevel.None,
        bool enableSsao = false,
        float ssaoRadius = 0.5f,
        float ssaoIntensity = 1,
        SSAOQuality ssaoQuality = SSAOQuality.Low,
        IEnumerable<SceneNode2D>? roots2D = null,
        bool captureFrame = false,
        bool waitForCompletion = true,
        bool autoUpdateOctree = true,
        bool enableRenderOrder = false
    ) {
        Dispatcher.VerifyAccess();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        roots.AsGuardNotNull();
        camera.AsGuardNotNull();
        if (frameActive || !CanSubmitFrame) return 0;
        if (!IsInitialized) EnsureNativeInitialized();
        if (!IsInitialized || !CanSubmitFrame) return 0;
        if (!CompletePendingFrame(waitForCompletion || captureFrame)) return 0;
        ApplyPendingResize();
        if (!IsInitialized || !CanSubmitFrame) return 0;
        ApplyPendingEffectsManager();
        frameActive = true;
        try {
            var renderer = frameRenderer
                           ?? throw new InvalidOperationException("The Direct3D 12 frame renderer is unavailable.");
            var (currentContext, currentSwapChain, backBuffer) = BeginFrame();
            var target = CreateFrameTarget(currentSwapChain);
            var recorded = renderer.RecordViewportFrame(
                currentContext, in target, (float) EffectiveDpiScale,
                clearColor, roots, camera, testFrustum, enableShadows, oitRenderType, oitDepthPeelingIterations,
                oitWeightPower, oitWeightDepthSlope, oitWeightMode, fxaaLevel, enableSsao, ssaoRadius,
                ssaoIntensity, ssaoQuality, roots2D, autoUpdateOctree, enableRenderOrder, RenderHost
            );
            if (!CanSubmitFrame) {
                DiscardRecordedFrame();
                return 0;
            }

            PlacedSubresourceFootprint footprint = default;
            ulong totalBytes = 0;
            if (captureFrame) {
                LastCapturedFrame = null;
                var currentDevice = device
                                    ?? throw new InvalidOperationException("The Direct3D 12 device is unavailable.");
                footprint = currentDevice.GetCopyableFootprint(backBuffer, out totalBytes);
                captureReadback = currentDevice.CreateBuffer(totalBytes, HeapType.Readback);
                currentContext.Transition(backBuffer, ResourceStates.CopySource);
                currentContext.CopyTextureToBuffer(captureReadback, backBuffer, in footprint);
            }

            if (!EndFrame(currentContext, currentSwapChain, backBuffer, waitForCompletion || captureFrame)) return 0;
            if (captureReadback is { } readback) {
                LastCapturedFrame = CreateCapturedFrame(
                    readback, in footprint, totalBytes,
                    currentSwapChain.Width, currentSwapChain.Height
                );
                readback.Dispose();
                captureReadback = null;
            }

            return recorded;
        } catch (System.Runtime.InteropServices.COMException) when (device is not null && device.GetDeviceRemovedReason() < 0) {
            _ = ObserveDeviceRemoval();
            DiscardRecordedFrame();
            return 0;
        } catch {
            DiscardRecordedFrame();
            throw;
        } finally {
            frameActive = false;
            SchedulePendingRetirement();
        }
    }

    /// <summary>
    ///     Gets the last synchronously captured viewport frame.
    /// </summary>
    internal D3D12CapturedFrame? LastCapturedFrame { get; private set; }

    /// <summary>
    ///     Removes native row padding and converts the swap-chain RGBA bytes to WPF BGRA bytes.
    /// </summary>
    /// <param name="readback">The completed readback buffer.</param>
    /// <param name="footprint">The native copy footprint.</param>
    /// <param name="totalBytes">The complete readback allocation size.</param>
    /// <param name="width">The frame width.</param>
    /// <param name="height">The frame height.</param>
    /// <returns>The tightly packed WPF frame.</returns>
    private static D3D12CapturedFrame CreateCapturedFrame(
        SilkD3D12Resource readback,
        in PlacedSubresourceFootprint footprint,
        ulong totalBytes,
        uint width,
        uint height
    ) => CreateCapturedFrame(
        readback.Read(checked((int) totalBytes)),
        footprint.Offset,
        footprint.Footprint.RowPitch,
        width,
        height
    );

    /// <summary>
    ///     Converts a padded RGBA readback allocation to tightly packed WPF BGRA pixels.
    /// </summary>
    /// <param name="padded">The complete native readback allocation.</param>
    /// <param name="offset">The first texture byte in the allocation.</param>
    /// <param name="rowPitch">The padded native row size.</param>
    /// <param name="width">The frame width.</param>
    /// <param name="height">The frame height.</param>
    /// <param name="sourceFormat">The channel order of the readback allocation.</param>
    /// <param name="premultiplied">Whether RGB must be normalized from premultiplied to straight color.</param>
    /// <returns>The tightly packed WPF frame.</returns>
    internal static D3D12CapturedFrame CreateCapturedFrame(
        byte[] padded,
        ulong offset,
        uint rowPitch,
        uint width,
        uint height,
        Silk.NET.DXGI.Format sourceFormat = Silk.NET.DXGI.Format.FormatR8G8B8A8Unorm,
        bool premultiplied = false
    ) {
        padded.AsGuardNotNull();
        if (sourceFormat is not (Silk.NET.DXGI.Format.FormatR8G8B8A8Unorm or
            Silk.NET.DXGI.Format.FormatB8G8R8A8Unorm))
            throw new ArgumentException("Capture requires an RGBA8 or BGRA8 allocation.", nameof(sourceFormat));
        var rowBytes = checked((int) width * 4);
        var pixels = new byte[checked(rowBytes * (int) height)];
        for (var row = 0; row < height; row++) {
            var sourceOffset = checked((int) offset + (int) row * (int) rowPitch);
            var destinationOffset = checked((int) row * rowBytes);
            padded.AsSpan(sourceOffset, rowBytes)
                .CopyTo(pixels.AsSpan(destinationOffset, rowBytes));
        }

        for (var index = 0; index < pixels.Length; index += 4) {
            if (sourceFormat == Silk.NET.DXGI.Format.FormatR8G8B8A8Unorm)
                (pixels[index], pixels[index + 2]) = (pixels[index + 2], pixels[index]);
            if (!premultiplied) continue;
            var alpha = pixels[index + 3];
            for (var channel = 0; channel < 3; channel++)
                pixels[index + channel] = alpha == 0
                    ? (byte) 0
                    : (byte) Math.Min(255, (pixels[index + channel] * 255 + alpha / 2) / alpha);
        }

        return new D3D12CapturedFrame(width, height, pixels);
    }

    /// <summary>
    ///     Creates the renderer constant-buffer values and culling frustum for one physical viewport.
    /// </summary>
    /// <param name="camera">The active camera.</param>
    /// <param name="width">The physical viewport width.</param>
    /// <param name="height">The physical viewport height.</param>
    /// <param name="dpiScale">The WPF-to-physical-pixel scale.</param>
    /// <param name="timeStamp">The frame timestamp in seconds.</param>
    /// <param name="frustum">The resulting camera frustum.</param>
    /// <returns>The complete global transform payload.</returns>
    internal static GlobalTransformStruct CreateViewportTransforms(
        CameraCore camera,
        uint width,
        uint height,
        float dpiScale,
        float timeStamp,
        out BoundingFrustum frustum
    ) => D3D12FrameRenderer.CreateViewportTransforms(camera, width, height, dpiScale, timeStamp, out frustum);

    /// <inheritdoc />
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
        base.OnRenderSizeChanged(sizeInfo);
        QueueResize();
    }

    /// <summary>
    ///     Releases all native and WPF-host resources.
    /// </summary>
    public void Dispose() {
        Dispatcher.VerifyAccess();
        if (IsDisposed) return;
        disposeRequested = retireRequested = true;
        if (HasActiveFrame || retiringNative) return;
        StopNative();

        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;
        windowHost.HandleCreated -= OnHandleCreated;
        windowHost.HandleDestroyed -= OnHandleDestroyed;
        windowHost.DpiScaleChanged -= OnDpiScaleChanged;
        if (resizeOperation is { Status: DispatcherOperationStatus.Pending }) resizeOperation.Abort();
        _resizeRetryTimer.Stop();
        _resizeRetryTimer.Tick -= RetryPendingResize;
        retirementOperation?.Abort();
        retirementOperation = null;
        windowHost.Dispose();
        IsDisposed = true;
        disposeRequested = retireRequested = false;
    }

    /// <summary>
    ///     Starts native presentation when WPF has attached the surface.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="eventArgs">The event arguments.</param>
    private void OnLoaded(object sender, RoutedEventArgs eventArgs) {
        loaded = true;
        suspended = false;
        EnsureNativeInitialized();
    }

    /// <summary>
    ///     Stops native presentation when WPF detaches the surface.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="eventArgs">The event arguments.</param>
    private void OnUnloaded(object sender, RoutedEventArgs eventArgs) {
        loaded = false;
        suspended = true;
        RetireSceneResources();
    }

    /// <summary>
    ///     Starts native presentation as soon as the child HWND exists.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="eventArgs">The event arguments.</param>
    private void OnHandleCreated(object? sender, EventArgs eventArgs) => EnsureNativeInitialized();

    /// <summary>
    ///     Stops native presentation before the child HWND is destroyed.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="eventArgs">The event arguments.</param>
    private void OnHandleDestroyed(object? sender, EventArgs eventArgs) => RetireSceneResources();

    /// <summary>
    ///     Queues a physical resize when WPF changes DPI.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="dpiScale">The new DPI scale.</param>
    private void OnDpiScaleChanged(object? sender, double dpiScale) {
        this.dpiScale = null;
        QueueResize();
    }

    /// <summary>
    ///     Creates the device, queue, command context, fence, and swap chain once.
    /// </summary>
    private void StartNative() {
        Dispatcher.VerifyAccess();
        if (IsDisposed || disposeRequested || retireRequested || retiringNative || HasActiveFrame ||
            swapChain is not null || !windowHost.IsHandleCreated || recoveryBarrier is { CanRecreate: false }) return;

        var size = D3D12PresentationSize.Calculate(ActualWidth, ActualHeight, EffectiveDpiScale);
        try {
            var selectedDriverType = effectsManager?.EnableSoftwareRendering == true
                ? SilkDriverType.Warp
                : driverType;
            device = SilkD3D12DeviceFactory.CreateForAdapter(
                effectsManager?.AdapterIndex ?? -1,
                SilkFeatureLevel.Level110, selectedDriverType
            );
            recoveryRegistration = new D3D12PresentationDeviceRecovery(
                device.NativePointer, Dispatcher, retireRemovedDevice, queuedDeviceRemoval
            );
            if (ObserveDeviceRemoval()) return;
            commandQueue = device.CreateCommandQueue();
            commandContext = device.CreateCommandContext();
            fence = device.CreateFence();
            swapChain = new SilkD3D12SwapChain(
                device,
                commandQueue,
                windowHost.NativeHandle,
                size.Width,
                size.Height
            );
            frameRenderer = new D3D12FrameRenderer(device, swapChain.Format, effectsManager);
            frameRenderer.Resize(size.Width, size.Height);
            _effectsManagerDirty = false;
            recoveryBarrier = null;
            if (RenderHost is Viewport3DX viewport) viewport.MarkPresentationInitialized();
        } catch (System.Runtime.InteropServices.COMException exception) when (device is null &&
                                                                              D3D12PresentationDeviceRecovery.IsRemovalFailure(exception)) {
            D3D12PresentationDeviceRecovery.ProbeForRemoval();
            if (RenderHost is Viewport3DX viewport) viewport.RenderDemand.RequestRecovery();
        } catch (System.Runtime.InteropServices.COMException) when (device is not null && device.GetDeviceRemovedReason() < 0) {
            _ = ObserveDeviceRemoval();
        } catch {
            StopNative();
            throw;
        }
    }

    /// <summary>
    ///     Waits for queued GPU work and releases the presentation stack in dependency order.
    /// </summary>
    private void StopNative() {
        if (HasActiveFrame || retiringNative) {
            retireRequested = true;
            return;
        }

        retiringNative = true;
        try {
            if (device is not null && device.GetDeviceRemovedReason() < 0) {
                recoveryBarrier ??= recoveryRegistration;
                recoveryRegistration?.RequestRemoval();
            }

            _resizeRetryTimer.Stop();
            if (resizeOperation is { Status: DispatcherOperationStatus.Pending }) resizeOperation.Abort();
            resizeOperation = null;
            if (device is not null && commandQueue is not null && fence is not null) {
                try {
                    device.ThrowIfDeviceRemoved();
                    var fenceValue = commandQueue.Signal(fence);
                    fence.Wait(fenceValue, TimeSpan.FromSeconds(5));
                    device.ThrowIfDeviceRemoved();
                    _ = _frameSubmission.TryComplete(fence.CompletedValue, FrameTimestamp());
                } catch (Exception) when (device.GetDeviceRemovedReason() < 0) {
                    recoveryBarrier ??= recoveryRegistration;
                    recoveryRegistration?.RequestRemoval();
                    // Removed queues cannot drain; completed old frames remain claimable, pending work is abandoned.
                    _frameSubmission.AbandonRemovedDeviceSubmission();
                }
            }

            // Keep undelivered completions across retirement; already claimed serials remain claimed.
            captureReadback?.Dispose();
            captureReadback = null;
            LastCapturedFrame = null;
            frameRenderer?.Dispose();
            swapChain?.Dispose();
            commandContext?.Dispose();
            fence?.Dispose();
            commandQueue?.Dispose();
            device?.Dispose();
            frameRenderer = null;
            swapChain = null;
            commandContext = null;
            fence = null;
            commandQueue = null;
            device = null;
            recoveryRegistration?.Unregister();
            recoveryRegistration = null;
            commandsOpen = false;
            if (RenderHost is Viewport3DX viewport) viewport.ObserveNativeRetirement(this);
        } finally {
            retiringNative = false;
            if (disposeRequested) SchedulePendingRetirement();
        }
    }

    /// <summary>
    ///     Resets the command context after completion and borrows the current back buffer.
    /// </summary>
    /// <returns>The open context, current swap chain, and render-target back buffer.</returns>
    /// <exception cref="InvalidOperationException">A frame is pending or a required native owner is unavailable.</exception>
    private (SilkD3D12CommandContext Context, SilkD3D12SwapChain SwapChain, SilkD3D12Resource BackBuffer)
        BeginFrame() {
        if (_frameSubmission.IsPending)
            throw new InvalidOperationException("A pending Direct3D 12 frame still owns the command context");
        var currentSwapChain = swapChain
                               ?? throw new InvalidOperationException("The presentation surface is not attached to a window.");
        var currentContext = commandContext
                             ?? throw new InvalidOperationException("The command context is unavailable.");
        var backBuffer = currentSwapChain.CurrentBackBuffer;
        currentContext.Reset();
        commandsOpen = true;
        return (currentContext, currentSwapChain, backBuffer);
    }

    /// <summary>Describes the current borrowed color target without transferring native ownership.</summary>
    /// <param name="currentSwapChain">The current transport allocation.</param>
    /// <returns>The frame boundary metadata.</returns>
    private static D3D12FrameTarget CreateFrameTarget(SilkD3D12SwapChain currentSwapChain) => new(
        currentSwapChain.CurrentBackBuffer, currentSwapChain.CurrentRenderTargetView,
        currentSwapChain.Width, currentSwapChain.Height, currentSwapChain.Format
    );

    /// <summary>
    ///     Submits and presents one protected frame, waiting only for explicit synchronous consumers.
    /// </summary>
    /// <param name="currentContext">The open command context.</param>
    /// <param name="currentSwapChain">The current swap chain.</param>
    /// <param name="backBuffer">The current back buffer.</param>
    /// <param name="waitForCompletion">Whether this explicit consumer requires completed GPU work.</param>
    /// <exception cref="InvalidOperationException">The native command queue or completion fence is unavailable.</exception>
    /// <exception cref="TimeoutException">The submitted frame exceeded its finite completion deadline.</exception>
    /// <returns>Whether commands were submitted; canceled recording retains retirement without consuming a frame.</returns>
    private bool EndFrame(
        SilkD3D12CommandContext currentContext,
        SilkD3D12SwapChain currentSwapChain,
        SilkD3D12Resource backBuffer,
        bool waitForCompletion = true
    ) {
        if (ObserveDeviceRemoval() || !CanSubmitFrame) {
            DiscardRecordedFrame();
            return false;
        }

        var currentQueue = commandQueue
                           ?? throw new InvalidOperationException("The command queue is unavailable.");
        var currentFence = fence
                           ?? throw new InvalidOperationException("The presentation fence is unavailable.");
        currentContext.Transition(backBuffer, ResourceStates.Present);
        currentContext.Close();
        commandsOpen = false;
        currentQueue.Execute(currentContext);
        currentSwapChain.Present(0);
        var fenceValue = currentQueue.Signal(currentFence);
        _frameSubmission.Submit(fenceValue, FrameTimestamp());
        PresentedFrames++;
        if (waitForCompletion && !CompletePendingFrame(true)) return false;
        return true;
    }

    /// <summary>
    ///     Coalesces WPF size and DPI changes into one background-priority native resize.
    /// </summary>
    private void QueueResize() => Resize(ActualWidth, ActualHeight);

    /// <summary>
    ///     Queues an explicit host size through the existing fence-safe physical resize queue.
    /// </summary>
    /// <param name="width">The device-independent width.</param>
    /// <param name="height">The device-independent height.</param>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is negative or nonfinite.</exception>
    internal void Resize(double width, double height) {
        Dispatcher.VerifyAccess();
        if (IsDisposed) return;
        var size = D3D12PresentationSize.Calculate(width, height, EffectiveDpiScale);
        if (!resizeQueue.Request(size.Width, size.Height)) return;

        resizeOperation = Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action) ApplyDeferredResize);
    }

    /// <summary>
    ///     Applies the newest physical size after polling outstanding GPU work without blocking the dispatcher.
    /// </summary>
    private void ApplyPendingResize() {
        if (IsDisposed || disposeRequested || retireRequested || frameActive || swapChain is null) return;
        if (!CompletePendingFrame()) {
            _resizeRetryTimer.Start();
            return;
        }

        _resizeRetryTimer.Stop();
        if (!resizeQueue.TryTake(out var size)) return;
        if (swapChain.Width == size.Width && swapChain.Height == size.Height) return;
        swapChain.Resize(size.Width, size.Height);
        frameRenderer?.Resize(size.Width, size.Height);
    }

    /// <summary>Retries the latest resize request once the preceding frame may have completed.</summary>
    /// <param name="sender">The resize retry timer.</param>
    /// <param name="args">The timer tick.</param>
    private void RetryPendingResize(object? sender, EventArgs args) {
        _resizeRetryTimer.Stop();
        ApplyDeferredResize();
    }

    /// <summary>Reports deferred resize failures at the same renderer boundary as ordinary composition failures.</summary>
    private void ApplyDeferredResize() {
        if (HasActiveFrame) return;
        try {
            ApplyPendingResize();
        } catch (Exception exception) {
            _resizeRetryTimer.Stop();
            ReportDeferredFailure(exception);
        }
    }
}

/// <summary>
///     Contains one tightly packed straight BGRA viewport frame for WPF bitmap encoding.
/// </summary>
/// <param name="Width">The frame width.</param>
/// <param name="Height">The frame height.</param>
/// <param name="Pixels">The tightly packed BGRA pixels.</param>
internal readonly record struct D3D12CapturedFrame(uint Width, uint Height, byte[] Pixels);

/// <summary>
///     Retains only the newest pending presentation size and requests at most one dispatcher operation.
/// </summary>
internal sealed class D3D12ResizeQueue {
    /// <summary>The most recent requested physical size.</summary>
    private (uint Width, uint Height)? pending;

    /// <summary>
    ///     Stores the newest size.
    /// </summary>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <returns><see langword="true" /> when a dispatcher operation must be scheduled.</returns>
    internal bool Request(uint width, uint height) {
        if (width == 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height == 0) throw new ArgumentOutOfRangeException(nameof(height));

        var schedule = pending is null;
        pending = (width, height);
        return schedule;
    }

    /// <summary>
    ///     Removes the newest pending size.
    /// </summary>
    /// <param name="size">The pending size, if present.</param>
    /// <returns><see langword="true" /> when a size was pending.</returns>
    internal bool TryTake(out (uint Width, uint Height) size) {
        if (pending is not { } value) {
            size = default;
            return false;
        }

        pending = null;
        size = value;
        return true;
    }
}