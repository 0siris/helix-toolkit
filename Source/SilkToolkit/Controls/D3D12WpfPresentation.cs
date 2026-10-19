using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Media;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;
using Silk.NET.Direct3D12;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>Owns one demand-driven D3D12 scene and its nonblocking WPF publication transport.</summary>
/// <remarks>The canvas, application scene and external effects manager are borrowed.</remarks>
internal sealed unsafe class D3D12WpfPresentation : IDisposable {
    /// <summary>Bounds explicit drains and actual GPU/copy progress.</summary>
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(5);

    /// <summary>The borrowed actual viewport and scene-demand consumer.</summary>
    private readonly Viewport3DX viewport;

    /// <summary>The borrowed WPF publication boundary.</summary>
    private readonly DPFCanvas canvas;

    /// <summary>The hardware policy retained across native recreation.</summary>
    private readonly SilkDriverType driverType;

    /// <summary>The shared thread-safe demand coordinator.</summary>
    private readonly D3D12RenderDemand demand;

    /// <summary>Drains pending ownership while the canvas has no composition subscription.</summary>
    private readonly DispatcherTimer serviceTimer;

    /// <summary>The cached Scene2D repaint notification.</summary>
    private readonly Action invalidateScene2D;

    /// <summary>Reuses the callback for disposal requested from inside a scene update.</summary>
    private readonly Action disposeViewport;

    /// <summary>Retains at most one deferred disposal operation after recording unwinds.</summary>
    private DispatcherOperation? deferredDisposal;

    /// <summary>The currently subscribed Scene2D roots.</summary>
    private readonly FastList<SceneNode2D> roots2D = [];

    /// <summary>The sole owned D3D12 device.</summary>
    private SilkD3D12Device? device;

    /// <summary>The sole owned native submission queue.</summary>
    private SilkD3D12CommandQueue? queue;

    /// <summary>The reusable owned command allocator/list.</summary>
    private SilkD3D12CommandContext? commands;

    /// <summary>The fence protecting every renderer allocation.</summary>
    private SilkD3D12Fence? fence;

    /// <summary>The owned interop allocations borrowing the same device.</summary>
    private D3D12ImageInteropBridge? bridge;

    /// <summary>The owned common scene recorder.</summary>
    private D3D12FrameRenderer? renderer;

    /// <summary>The last actual presentation HWND used by the D3D9 device.</summary>
    private nint nativeHandle;

    /// <summary>The allocated source width.</summary>
    private uint width;

    /// <summary>The allocated source height.</summary>
    private uint height;

    /// <summary>The effective scale of the last requested scene.</summary>
    private float dpiScale;

    /// <summary>The native fence that must complete before command/resource reuse.</summary>
    private ulong pendingFence;

    /// <summary>Whether that fence belongs to an accepted scene submission.</summary>
    private bool pendingScene;

    /// <summary>The accepted serial currently in transport, or zero when idle.</summary>
    private ulong transportSerial;

    /// <summary>Whether the current serial has entered its D3D11 copy.</summary>
    private bool copyStarted;

    /// <summary>The start of the outstanding GPU stage.</summary>
    private TimeSpan stageStarted;

    /// <summary>The last fully published surface, retained for frontbuffer rebinding.</summary>
    private nint publishedSurface;

    /// <summary>The serial of that retained image.</summary>
    private ulong publishedSerial;

    /// <summary>The completions already delivered to the viewport.</summary>
    private ulong notifiedCompletions;

    /// <summary>Marks resources whose recorded state must be discarded before another frame.</summary>
    private bool recreateRequested;

    /// <summary>Protects allocations after execution without a successful completion signal.</summary>
    private bool unfencedExecution;

    /// <summary>Rejects nested frame preparation and completion callbacks.</summary>
    private bool updating;

    /// <summary>Prevents application/native disposal while node hooks still record into borrowed resources.</summary>
    private bool recording;

    /// <summary>Retains a disposal request until the current CPU recording stack has unwound.</summary>
    private bool disposeRequested;

    /// <summary>Stops new submissions during an explicit, retryable teardown.</summary>
    private bool stopping;

    /// <summary>Records successful drain, image unlock/detachment and bridge retirement for disposal retry.</summary>
    private bool disposalPrepared;

    /// <summary>The last automatic WPF timestamp already processed.</summary>
    private TimeSpan lastRenderingTime;

    /// <summary>Whether an automatic timestamp has been observed in this active interval.</summary>
    private bool hasRenderingTime;

    /// <summary>The optional explicit-capture allocation retained through native completion.</summary>
    private SilkD3D12Resource? captureReadback;

    /// <summary>The native row layout of the explicit capture.</summary>
    private PlacedSubresourceFootprint captureFootprint;

    /// <summary>The native byte count of the explicit capture.</summary>
    private ulong captureBytes;

    /// <summary>Gets the borrowed publication canvas.</summary>
    internal DPFCanvas Canvas => canvas;

    /// <summary>Gets whether native resources have been initialized.</summary>
    internal bool IsInitialized => bridge is not null;

    /// <summary>Gets whether an accepted frame still owns rendering, copying or unpublished output.</summary>
    internal bool HasPendingTransport => pendingFence != 0 || unfencedExecution || transportSerial != 0;

    /// <summary>Gets the current native generation.</summary>
    internal ulong Generation { get; private set; }

    /// <summary>Gets the number of accepted scene submissions across generations.</summary>
    internal ulong SubmittedFrames { get; private set; }

    /// <summary>Gets the number of GPU-completed scene frames across generations.</summary>
    internal ulong CompletedFrames { get; private set; }

    /// <summary>Gets the number of native copy submissions across generations.</summary>
    internal ulong CopiedFrames { get; private set; }

    /// <summary>Gets the number of new images published, excluding rebinding.</summary>
    internal ulong PublishedFrames { get; private set; }

    /// <summary>Gets the number of retained-image rebindings.</summary>
    internal ulong ReboundFrames { get; private set; }

    /// <summary>Gets the actual retained scene context.</summary>
    internal RenderContext? RenderContext => renderer?.RenderContext;

    /// <summary>Gets the actual prepared hierarchy when initialized.</summary>
    internal FastList<KeyValuePair<int, SceneNode>>? PerFrameFlattenedScene => renderer?.PerFrameFlattenedScene;

    /// <summary>Gets the canonical straight-BGRA capture produced only by an explicit request.</summary>
    internal D3D12CapturedFrame? LastCapturedFrame { get; private set; }

    /// <summary>Gets the accepted serial whose explicit readback completed, or zero after a new capture attempt.</summary>
    internal ulong CapturedSerial { get; private set; }

    /// <summary>Gets whether final native teardown completed.</summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>Requests generation replacement at a quiescent and detached native boundary.</summary>
    internal void RequestRecreation() {
        canvas.Dispatcher.VerifyAccess();
        ObjectDisposedException.ThrowIf(IsDisposed || stopping, this);
        recreateRequested = true;
        demand.RequestRecovery();
    }

    /// <summary>Connects retained canvas callbacks without creating a second render loop or native device.</summary>
    /// <param name="viewport">The borrowed viewport supplying actual scene preparation.</param>
    /// <param name="canvas">The borrowed WPF canvas.</param>
    /// <param name="driverType">The selected native device policy.</param>
    internal D3D12WpfPresentation(
        Viewport3DX viewport,
        DPFCanvas canvas,
        SilkDriverType driverType = SilkDriverType.Hardware
    ) {
        this.viewport = viewport;
        this.canvas = canvas;
        this.driverType = driverType;
        demand = viewport.RenderDemand;
        invalidateScene2D = viewport.InvalidateRender;
        disposeViewport = FinishViewportDisposal;
        serviceTimer = new DispatcherTimer(DispatcherPriority.Background, canvas.Dispatcher) { Interval = TimeSpan.FromMilliseconds(16) };
        serviceTimer.Tick += ServiceSuspendedTransport;
        canvas.Rendering += OnRendering;
        canvas.PresentationChanged += OnPresentationChanged;
        canvas.ImageSource.FrontBufferChanged += OnFrontBufferChanged;
        demand.RequestRecovery();
    }

    /// <summary>Refreshes root notification ownership without allocating on an unchanged frame.</summary>
    /// <param name="roots">The actual viewport overlay roots.</param>
    internal void SetScene2DRoots(IList<SceneNode2D> roots) {
        var same = roots.Count == roots2D.Count;
        for (var index = 0; same && index < roots.Count; index++)
            same = ReferenceEquals(roots[index], roots2D[index]);
        if (same) return;
        foreach (var root in roots2D) root.RenderInvalidated -= invalidateScene2D;
        roots2D.Clear();
        for (var index = 0; index < roots.Count; index++) {
            var root = roots[index];
            roots2D.Add(root);
            root.RenderInvalidated += invalidateScene2D;
        }
    }

    /// <summary>Claims one GPU completion independently of copying, publishing, or empty draw counts.</summary>
    /// <returns>Whether a previously undelivered scene completion was claimed.</returns>
    internal bool TryTakeCompletedFrame() {
        if (notifiedCompletions == CompletedFrames) return false;
        notifiedCompletions++;
        return true;
    }

    /// <summary>Services transport before considering another demand-reserved scene frame.</summary>
    /// <param name="renderingTime">The WPF timestamp used by lightweight camera updates.</param>
    /// <param name="captureFrame">Whether this explicit caller requires a new captured scene.</param>
    /// <param name="waitForCompletion">Whether an explicit caller permits bounded native waits.</param>
    /// <param name="automaticFrame">Whether new submissions must respect the live automatic-rendering permission.</param>
    /// <returns>The actual scene draw count, or zero when idle, suspended, or transport-owned.</returns>
    internal int RenderFrame(
        TimeSpan renderingTime,
        bool captureFrame = false,
        bool waitForCompletion = false,
        bool automaticFrame = false
    ) {
        canvas.Dispatcher.VerifyAccess();
        if (IsDisposed || stopping) return 0;
        if (updating) {
            demand.Request(D3D12RenderInvalidation.Render);
            return 0;
        }

        updating = true;
        var reserved = false;
        var accepted = false;
        try {
            _ = demand.TryTakeRecoveryRequest();
            DetectPresentationChanges();
            if (!ServiceTransport(waitForCompletion || captureFrame)) {
                if ((!automaticFrame || viewport.WpfRenderingEnabled) && canvas.IsActive)
                    viewport.UpdateWpfCamera(renderingTime);
                return 0;
            }

            viewport.RaiseCompletedWpfFrames(this);
            if ((automaticFrame && !viewport.WpfRenderingEnabled) || IsDisposed || stopping ||
                !viewport.CanRenderWpfScene || !canvas.IsActive) return 0;
            if (bridge is null) CreateNative();
            if (captureFrame) demand.Request(D3D12RenderInvalidation.Render);
            reserved = demand.TryBeginFrame(out _);
            viewport.UpdateWpfCamera(renderingTime);
            if ((automaticFrame && !viewport.WpfRenderingEnabled) || IsDisposed || stopping ||
                !viewport.CanRenderWpfScene) return 0;
            if (!reserved && !demand.TryBeginFrame(out _)) return 0;
            reserved = true;
            if (captureFrame) {
                LastCapturedFrame = null;
                CapturedSerial = 0;
            }

            var currentCommands = commands!;
            var target = bridge!.Target;
            currentCommands.Reset();
            recording = true;
            try {
                var draws = viewport.RecordWpfScene(renderer!, currentCommands, in target, canvas.EffectiveDpiScale);
                if (stopping) {
                    currentCommands.Close();
                    recording = false;
                    recreateRequested = true;
                    return 0;
                }

                if (captureFrame) {
                    captureFootprint = device!.GetCopyableFootprint(target.ColorResource, out captureBytes);
                    captureReadback = device!.CreateBuffer(captureBytes, HeapType.Readback);
                    currentCommands.Transition(target.ColorResource, ResourceStates.CopySource);
                    currentCommands.CopyTextureToBuffer(captureReadback, target.ColorResource, in captureFootprint);
                }

                currentCommands.Transition(target.ColorResource, ResourceStates.Common);
                currentCommands.Close();
                recording = false;
                unfencedExecution = true;
                queue!.Execute(currentCommands);
                pendingFence = queue!.Signal(fence!);
                unfencedExecution = false;
                pendingScene = true;
                stageStarted = Now();
                transportSerial = ++SubmittedFrames;
                copyStarted = false;
                accepted = true;
                if (waitForCompletion || captureFrame) _ = ServiceTransport(true);
                viewport.RaiseCompletedWpfFrames(this);
                return draws;
            } catch {
                recreateRequested = true;
                demand.RequestRecovery();
                if (recording) currentCommands.Close();
                throw;
            }
        } catch {
            recreateRequested = true;
            demand.RequestRecovery();
            throw;
        } finally {
            recording = false;
            if (reserved) demand.CompleteFrame(accepted);
            updating = false;
            UpdateServiceTimer();
            if (disposeRequested && deferredDisposal is null && !canvas.Dispatcher.HasShutdownStarted)
                deferredDisposal = canvas.Dispatcher.BeginInvoke(DispatcherPriority.Send, disposeViewport);
        }
    }

    /// <summary>Returns a new explicit capture, rejecting a publication-blocked request rather than returning stale pixels.</summary>
    /// <returns>The newly completed, normalized scene pixels.</returns>
    internal D3D12CapturedFrame CaptureFrame() {
        var requestedSerial = SubmittedFrames + 1;
        LastCapturedFrame = null;
        CapturedSerial = 0;
        _ = RenderFrame(TimeSpan.Zero, true, true);
        if (CapturedSerial != requestedSerial || LastCapturedFrame is not { } captured)
            throw new InvalidOperationException("The WPF transport is not available for a new captured scene.");
        return captured;
    }

    /// <summary>Coalesces dimensions, handle identity and effective DPI before native mutation.</summary>
    private void DetectPresentationChanges() {
        if (bridge is not null && canvas.NativeHandle != 0 &&
            (nativeHandle != canvas.NativeHandle || width != canvas.PixelWidth || height != canvas.PixelHeight))
            recreateRequested = true;
        if (canvas.IsActive && dpiScale != canvas.EffectiveDpiScale) {
            dpiScale = canvas.EffectiveDpiScale;
            demand.Request(D3D12RenderInvalidation.SceneGraph);
        }
    }

    /// <summary>Advances each owned stage once, preserving the source and ready slot under backpressure.</summary>
    /// <param name="wait">Whether an explicit caller permits bounded GPU and query waits.</param>
    /// <returns>Whether transport is idle and scene recording may be considered.</returns>
    private bool ServiceTransport(bool wait) {
        if (device is null) return canvas.IsActive && canvas.NativeHandle != 0 && canvas.PixelWidth != 0 && canvas.PixelHeight != 0;
        if (device.Handle->GetDeviceRemovedReason() < 0) {
            if (bridge is { IsCopyPending: true }) {
                try {
                    if (!bridge.IsCopyComplete) return false;
                } catch (System.Runtime.InteropServices.COMException) {
                    // Bridge disposal separately confirms whether its own D3D11 device was removed.
                }
            }

            if (!TryDetach()) return false;
            ReleaseNative();
            recreateRequested = false;
            demand.Request(D3D12RenderInvalidation.SceneGraph);
            return canvas.IsActive;
        }

        if (unfencedExecution) {
            pendingFence = queue!.Signal(fence!);
            pendingScene = false;
            unfencedExecution = false;
            stageStarted = Now();
        }

        if (pendingFence != 0) {
            device.ThrowIfDeviceRemoved();
            if (fence!.CompletedValue < pendingFence) {
                CheckDeadline();
                if (!wait) return false;
                fence.Wait(pendingFence, CompletionTimeout - (Now() - stageStarted));
            }

            pendingFence = 0;
            var completedScene = pendingScene;
            if (pendingScene) CompletedFrames++;
            pendingScene = false;
            if (captureReadback is not null) {
                if (completedScene) {
                    LastCapturedFrame = D3D12PresentationSurface.CreateCapturedFrame(
                        captureReadback.Read(checked((int) captureBytes)), captureFootprint.Offset,
                        captureFootprint.Footprint.RowPitch, width, height, Format.FormatB8G8R8A8Unorm, true
                    );
                    CapturedSerial = transportSerial;
                }

                captureReadback.Dispose();
                captureReadback = null;
            }
        }

        viewport.RaiseCompletedWpfFrames(this);
        if (IsDisposed || stopping) return false;
        if (bridge!.IsCopyPending && !CompleteCopy(wait)) return false;
        if (recreateRequested) {
            if (!TryDetach()) return false;
            ReleaseNative();
            recreateRequested = false;
            demand.Request(D3D12RenderInvalidation.SceneGraph);
            return canvas.IsActive && canvas.NativeHandle != 0;
        }

        if (transportSerial != 0 && !copyStarted) {
            bridge.BeginCopy(transportSerial);
            copyStarted = true;
            CopiedFrames++;
            stageStarted = Now();
            if (!CompleteCopy(wait)) return false;
        }

        viewport.RaiseCompletedWpfFrames(this);
        if (IsDisposed || stopping) return false;
        if (!canvas.IsActive) {
            _ = TryDetach();
            return false;
        }

        if (bridge.HasReadySurface) {
            demand.RequestPublication();
            _ = demand.TryTakePublicationRequest();
            var surface = bridge.ReadySurface;
            var serial = bridge.ReadySerial;
            if (!canvas.ImageSource.IsFrontBufferAvailable || !canvas.ImageSource.TryPublish(surface, width, height)) {
                demand.RequestPublication();
                return false;
            }

            bridge.ConfirmPublication(serial);
            publishedSurface = surface;
            publishedSerial = serial;
            PublishedFrames++;
            transportSerial = 0;
            copyStarted = false;
        } else if (demand.TryTakePublicationRequest() && publishedSurface != 0) {
            if (!canvas.ImageSource.IsFrontBufferAvailable || !canvas.ImageSource.TryPublish(publishedSurface, width, height)) {
                demand.RequestPublication();
                return false;
            }

            bridge.ConfirmPublication(publishedSerial);
            ReboundFrames++;
        }

        return transportSerial == 0;
    }

    /// <summary>Polls or explicitly drains D3D11 without allowing an overlapping write to its source.</summary>
    /// <param name="wait">Whether this explicit caller permits a bounded query loop.</param>
    /// <returns>Whether the copy stage has completed.</returns>
    private bool CompleteCopy(bool wait) {
        while (!bridge!.IsCopyComplete) {
            CheckDeadline();
            if (!wait) return false;
            Thread.Yield();
        }

        return true;
    }

    /// <summary>Rejects a stalled native stage without applying that timeout to WPF lock contention.</summary>
    private void CheckDeadline() {
        if (Now() - stageStarted >= CompletionTimeout)
            throw new TimeoutException("The WPF GPU or interop copy did not complete within five seconds.");
    }

    /// <summary>Detaches the actual WPF pointer before acknowledging that both native destinations are unbound.</summary>
    /// <returns>Whether detachment completed or no native allocation is bound.</returns>
    private bool TryDetach() {
        if (!canvas.ImageSource.TryDetach()) {
            demand.RequestRecovery();
            return false;
        }

        bridge?.ConfirmDetached();
        if (publishedSurface != 0) demand.RequestPublication();
        return true;
    }

    /// <summary>Creates one complete submission/recording lifetime over the bridge's canonical target.</summary>
    private void CreateNative() {
        nativeHandle = canvas.NativeHandle;
        width = canvas.PixelWidth;
        height = canvas.PixelHeight;
        if (nativeHandle == 0 || width == 0 || height == 0)
            throw new InvalidOperationException("The canvas has no native presentation size.");
        try {
            device = SilkD3D12DeviceFactory.CreateDefault(driverType: driverType);
            queue = device.CreateCommandQueue();
            commands = device.CreateCommandContext();
            fence = device.CreateFence();
            bridge = new D3D12ImageInteropBridge(device, nativeHandle, width, height);
            renderer = new D3D12FrameRenderer(
                device, Format.FormatB8G8R8A8Unorm, viewport.EffectsManager,
                D3D12ColorOutputMode.Premultiplied
            );
            Generation++;
        } catch {
            ReleaseNative();
            throw;
        }
    }

    /// <summary>Releases a quiescent and detached generation without disposing application-owned objects.</summary>
    private void ReleaseNative() {
        captureReadback?.Dispose();
        captureReadback = null;
        renderer?.Dispose();
        renderer = null;
        bridge?.Dispose();
        bridge = null;
        commands?.Dispose();
        commands = null;
        fence?.Dispose();
        fence = null;
        queue?.Dispose();
        queue = null;
        device?.Dispose();
        device = null;
        pendingFence = transportSerial = publishedSerial = 0;
        publishedSurface = 0;
        copyStarted = pendingScene = unfencedExecution = false;
    }

    /// <summary>Drains and detaches before the viewport begins irreversible application-scene disposal.</summary>
    internal void PrepareForDisposal() {
        canvas.Dispatcher.VerifyAccess();
        if (IsDisposed || disposalPrepared) return;
        stopping = true;
        if (device is not null) {
            if (device.Handle->GetDeviceRemovedReason() >= 0) {
                var drain = queue!.Signal(fence!);
                fence!.Wait(drain, CompletionTimeout);
            }

            pendingFence = 0;
            unfencedExecution = false;
        }

        if (!TryDetach()) throw new TimeoutException("The WPF image is still bound; native resources remain owned for disposal retry.");
        bridge?.Dispose();
        bridge = null;
        disposalPrepared = true;
    }

    /// <summary>Defers viewport/application disposal until a reentrant scene-recording request has unwound.</summary>
    /// <returns>Whether final disposal must be deferred.</returns>
    internal bool DeferViewportDisposalIfRecording() {
        if (!recording) return false;
        stopping = disposeRequested = true;
        return true;
    }

    /// <summary>Completes the retained disposal request after no node hook can access borrowed commands.</summary>
    private void FinishViewportDisposal() {
        deferredDisposal = null;
        if (!IsDisposed) viewport.Dispose();
    }

    /// <summary>Completes bounded native teardown; a failed detach/drain retains the owner for explicit retry.</summary>
    public void Dispose() {
        if (IsDisposed) return;
        if (DeferViewportDisposalIfRecording()) return;
        PrepareForDisposal();
        deferredDisposal?.Abort();
        deferredDisposal = null;
        disposeRequested = false;
        serviceTimer.Stop();
        serviceTimer.Tick -= ServiceSuspendedTransport;
        canvas.Rendering -= OnRendering;
        canvas.PresentationChanged -= OnPresentationChanged;
        canvas.ImageSource.FrontBufferChanged -= OnFrontBufferChanged;
        foreach (var root in roots2D) root.RenderInvalidated -= invalidateScene2D;
        roots2D.Clear();
        ReleaseNative();
        IsDisposed = true;
    }

    /// <summary>Forwards each automatic timestamp once through the same productive frame consumer.</summary>
    /// <param name="sender">The canvas forwarding the original WPF tick.</param>
    /// <param name="args">The original WPF frame timestamp.</param>
    private void OnRendering(object? sender, RenderingEventArgs args) {
        if (IsDisposed || stopping ||
            (hasRenderingTime && args.RenderingTime == lastRenderingTime)) return;
        hasRenderingTime = true;
        lastRenderingTime = args.RenderingTime;
        try {
            _ = RenderFrame(args.RenderingTime, automaticFrame: true);
        } catch (Exception exception) {
            viewport.StopRendering();
            viewport.HandleRenderException(canvas, new RelayExceptionEventArgs(exception));
        }
    }

    /// <summary>Requests lifecycle work without invalidating an otherwise unchanged scene.</summary>
    /// <param name="sender">The changed canvas.</param>
    /// <param name="args">The retained lifecycle notification.</param>
    private void OnPresentationChanged(object? sender, EventArgs args) {
        if (IsDisposed || stopping) return;
        hasRenderingTime = false;
        demand.RequestRecovery();
        if (publishedSurface != 0) demand.RequestPublication();
        UpdateServiceTimer();
    }

    /// <summary>Requests retained-image rebinding independently of the scene budget.</summary>
    /// <param name="sender">The changed D3DImage owner.</param>
    /// <param name="args">The frontbuffer notification.</param>
    private void OnFrontBufferChanged(object? sender, EventArgs args) {
        if (IsDisposed || stopping) return;
        demand.RequestRecovery();
        if (publishedSurface != 0) demand.RequestPublication();
    }

    /// <summary>Runs only while an inactive canvas retains actual pending GPU/copy/binding ownership.</summary>
    private void UpdateServiceTimer() {
        if (!IsDisposed && !stopping && !canvas.IsActive &&
            (pendingFence != 0 || unfencedExecution || bridge is { IsCopyPending: true } || canvas.ImageSource.BoundSurface != 0))
            serviceTimer.Start();
        else serviceTimer.Stop();
    }

    /// <summary>Services suspended transport without submitting another scene frame.</summary>
    /// <param name="sender">The bounded retirement timer.</param>
    /// <param name="args">The retained timer event arguments.</param>
    private void ServiceSuspendedTransport(object? sender, EventArgs args) {
        try {
            _ = RenderFrame(TimeSpan.Zero);
        } catch (Exception exception) {
            serviceTimer.Stop();
            viewport.HandleRenderException(canvas, new RelayExceptionEventArgs(exception));
        }
    }

    /// <summary>Reads monotonic time without allocating a stopwatch.</summary>
    /// <returns>The current native-stage timestamp.</returns>
    private static TimeSpan Now() => Stopwatch.GetElapsedTime(0);
}