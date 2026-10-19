using Color = System.Windows.Media.Color;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;
using MeshGeometry3D = HelixToolkit.SharpDX.Core.MeshGeometry3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

namespace SilkToolkit.Tests;

/// <summary>Exercises the real WARP presentation consumer with an intentionally stalled native queue and hidden HWND.</summary>
[Collection(WpfCollection.Name)]
public sealed class NonblockingFramePresentationTests {
    /// <summary>Retains actual borrowed native resources through reentrant unload or disposal during scene recording.</summary>
    /// <param name="disposeSurface">Whether to dispose the native surface instead of unloading it.</param>
    /// <param name="disposeViewport">Whether to dispose the owning viewport instead of unloading its surface.</param>
    /// <returns>The hidden WARP recording and dispatcher verification.</returns>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [Trait("Category", "Warp")]
    public Task RecordingRetirementWaitsForBorrowedResourcesAndCancelsSubmission(
        bool disposeSurface,
        bool disposeViewport
    ) => StaThread.RunAsync(() => {
            using var window = CreateWindow();
            using var surface = CreateSurface(window);
            using var viewport = CreateViewport(surface);
            surface.RenderHost = viewport;
            viewport.BelongsToParentWindow = false;
            var model = CreateModel();
            viewport.Items.Add(model);
            var node = (ObservedMeshNode) model.SceneNode;
            var rendered = 0;
            var onRendered = 0;
            var disposing = 0;
            viewport.Rendered += (_, _) => rendered++;
            viewport.OnRendered += (_, _) => onRendered++;
            viewport.Disposing += (_, _) => disposing++;
            Assert.True(viewport.RenderD3D12Frame(TimeSpan.FromSeconds(1)) > 0);
            Assert.Equal(1, rendered);
            Assert.Equal(1, onRendered);

            var renderer = surface.FrameRenderer!;
            var context = surface.RenderContext!;
            var commands = PrivateField<SilkD3D12CommandContext>(surface, "commandContext");
            var swapChain = PrivateField<SilkD3D12SwapChain>(surface, "swapChain");
            var backBuffer = swapChain.CurrentBackBuffer;
            var device = PrivateField<SilkD3D12Device>(surface, "device");
            var fence = PrivateField<SilkD3D12Fence>(surface, "fence");
            var submission = PrivateField<D3D12FrameSubmission>(surface, "_frameSubmission");
            var presented = surface.PresentedFrames;
            var signaled = fence.CurrentValue;
            var handle = surface.WindowHost.NativeHandle;
            var callbacks = 0;
            node.DuringNextUpdate = actualContext => {
                callbacks++;
                Assert.Same(context, actualContext);
                if (disposeViewport) viewport.Dispose();
                else if (disposeSurface) surface.Dispose();
                else window.RootVisual = null;

                PumpDispatcher();
                Assert.False(viewport.IsDisposed);
                Assert.False(surface.IsDisposed);
                Assert.False(node.IsDisposed);
                Assert.False(viewport.CanSubmitNativeScene);
                if (disposeSurface || disposeViewport) Assert.Equal(handle, surface.WindowHost.NativeHandle);
                Assert.Equal(0, disposing);
                Assert.Same(renderer, surface.FrameRenderer);
                Assert.False(renderer.IsDisposed);
                Assert.Same(context, surface.RenderContext);
                Assert.NotNull(actualContext.Camera);
                Assert.False(commands.IsDisposed);
                Assert.False(swapChain.IsDisposed);
                Assert.False(backBuffer.IsDisposed);
                Assert.False(device.IsDisposed);
                Assert.False(fence.IsDisposed);
                Assert.True(PrivateField<bool>(surface, "commandsOpen"));
                Assert.Equal(0, viewport.RenderD3D12Frame(TimeSpan.FromSeconds(2), waitForCompletion: false));
                Assert.Equal(presented, surface.PresentedFrames);
                Assert.Equal(signaled, fence.CurrentValue);
            };

            Assert.Equal(0, viewport.RenderD3D12Frame(TimeSpan.FromSeconds(2)));
            Assert.Equal(1, callbacks);
            Assert.Equal(presented, surface.PresentedFrames);
            Assert.Equal(signaled, fence.CurrentValue);
            Assert.False(submission.IsPending);
            Assert.False(PrivateField<bool>(surface, "commandsOpen"));
            Assert.False(renderer.IsDisposed);
            Assert.Equal(1, rendered);
            Assert.Equal(1, onRendered);
            var retirement = Stopwatch.StartNew();
            PumpDispatcher();
            Assert.True(
                retirement.Elapsed < TimeSpan.FromSeconds(5),
                $"Deferred native retirement exceeded its bounded drain: {retirement.Elapsed}."
            );
            Assert.True(renderer.IsDisposed);
            Assert.True(commands.IsDisposed);
            Assert.True(swapChain.IsDisposed);
            Assert.True(backBuffer.IsDisposed);
            Assert.True(device.IsDisposed);
            Assert.True(fence.IsDisposed);
            Assert.Null(context.Camera);
            Assert.False(surface.IsInitialized);
            Assert.False(surface.TryTakeCompletedFrame());
            Assert.Equal(1, rendered);
            Assert.Equal(1, onRendered);

            if (disposeSurface || disposeViewport) {
                Assert.True(surface.IsDisposed);
                Assert.False(surface.WindowHost.IsHandleCreated);
                Assert.Equal(disposeViewport, viewport.IsDisposed);
                Assert.Equal(disposeViewport, node.IsDisposed);
                Assert.Equal(
                    disposeViewport
                        ? 1
                        : 0, disposing
                );
                viewport.Dispose();
                surface.Dispose();
                Assert.Equal(1, disposing);
                Assert.True(node.IsDisposed);
            } else {
                Assert.False(surface.IsDisposed);
                Assert.False(surface.IsRetirementPending);
                window.RootVisual = surface;
                surface.UpdateLayout();
                PumpDispatcher();
                Assert.True(surface.IsInitialized);
                Assert.NotSame(renderer, surface.FrameRenderer);
                Assert.True(viewport.RenderD3D12Frame(TimeSpan.FromSeconds(3)) > 0);
                Assert.Same(node, model.SceneNode);
                Assert.NotNull(surface.RenderContext!.Camera);
                Assert.Equal(presented + 1, surface.PresentedFrames);
                Assert.Equal(2, rendered);
                Assert.Equal(2, onRendered);
                Assert.False(surface.TryTakeCompletedFrame());
            }
        }
    );

    /// <summary>Drains a genuinely blocked GPU submission on unload before freeing resources or claiming completion.</summary>
    /// <returns>The hidden WARP queue-retirement verification.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public Task UnloadRetainsPendingGpuResourcesAndDeliversCompletionOnce() => StaThread.RunAsync(() => {
            using var window = CreateWindow();
            using var surface = CreateSurface(window);
            using var viewport = CreateViewport(surface);
            surface.RenderHost = viewport;
            viewport.Items.Add(CreateModel());
            var rendered = 0;
            var onRendered = 0;
            viewport.Rendered += (_, _) => rendered++;
            viewport.OnRendered += (_, _) => onRendered++;
            Assert.True(viewport.RenderD3D12Frame(TimeSpan.FromSeconds(1)) > 0);
            using var gate = new NativeQueueGate(surface);
            Assert.True(viewport.RenderD3D12Frame(TimeSpan.FromSeconds(2), waitForCompletion: false) > 0);
            var renderer = surface.FrameRenderer!;
            var context = surface.RenderContext!;
            var commands = PrivateField<SilkD3D12CommandContext>(surface, "commandContext");
            var swapChain = PrivateField<SilkD3D12SwapChain>(surface, "swapChain");
            var device = PrivateField<SilkD3D12Device>(surface, "device");
            var fence = PrivateField<SilkD3D12Fence>(surface, "fence");
            var submitted = fence.CurrentValue;
            Assert.True(fence.CompletedValue < submitted);
            Assert.Equal(1, rendered);
            Assert.Equal(1, onRendered);
            using var retiring = new ManualResetEventSlim();
            var release = Task.Run(() => {
                    try {
                        Assert.True(retiring.Wait(TimeSpan.FromSeconds(2)));
                        Assert.True(
                            SpinWait.SpinUntil(() => fence.CurrentValue > submitted, TimeSpan.FromSeconds(2)),
                            "Unload did not submit its bounded retirement drain."
                        );
                        Assert.True(fence.CompletedValue < submitted);
                        Assert.False(renderer.IsDisposed);
                        Assert.NotNull(context.Camera);
                        Assert.False(commands.IsDisposed);
                        Assert.False(swapChain.IsDisposed);
                        Assert.False(device.IsDisposed);
                        Assert.False(fence.IsDisposed);
                    } finally {
                        gate.Release();
                    }
                }
            );
            try {
                retiring.Set();
                surface.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            } finally {
                release.GetAwaiter()
                    .GetResult();
            }

            Assert.True(renderer.IsDisposed);
            Assert.True(commands.IsDisposed);
            Assert.True(swapChain.IsDisposed);
            Assert.True(device.IsDisposed);
            Assert.True(fence.IsDisposed);
            Assert.Null(context.Camera);
            Assert.False(surface.IsInitialized);
            Assert.Equal(2UL, surface.PresentedFrames);
            Assert.Equal(2, rendered);
            Assert.Equal(2, onRendered);
            Assert.False(surface.TryTakeCompletedFrame());
            surface.RetireSceneResources();
            PumpDispatcher();
            Assert.Equal(2, rendered);
            Assert.Equal(2, onRendered);
            Assert.False(surface.TryTakeCompletedFrame());
        }
    );

    /// <summary>Processes queued lifecycle callbacks while preserving the caller's active recording stack.</summary>
    private static void PumpDispatcher() {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(
            DispatcherPriority.ApplicationIdle,
            new Action(() => frame.Continue = false)
        );
        Dispatcher.PushFrame(frame);
    }

    /// <summary>
    ///     Verifies a pending GPU frame leaves the dispatcher live and skips scene updates and octree maintenance.
    /// </summary>
    /// <returns>The hidden native queue and dispatcher verification.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public Task DelayedFenceKeepsDispatcherLiveAndRendersLatestCameraAfterCompletion() => StaThread.RunAsync(async () => {
            using var window = CreateWindow();
            using var surface = CreateSurface(window);
            using var viewport = CreateViewport(surface);
            var model = CreateModel();
            viewport.Items.Add(model);
            var notifications = 0;
            viewport.OnRendered += (_, _) => notifications++;
            var firstDraws = viewport.RenderD3D12Frame(TimeSpan.FromSeconds(1));
            var node = (ObservedMeshNode) model.SceneNode;
            Assert.True(firstDraws > 0, $"The initial native frame recorded {firstDraws} draws");
            Assert.Equal(1, notifications);

            using var gate = new NativeQueueGate(surface);
            var submittedDraws = viewport.RenderD3D12Frame(TimeSpan.FromSeconds(2), waitForCompletion: false);
            var updateCount = node.UpdateCount;
            var maintenanceCount = node.MaintenanceCount;
            var geometry = Assert.IsType<MeshGeometry3D>(model.Geometry);
            Assert.NotNull(geometry.Octree);
            geometry.Positions = [.. geometry.Positions!];
            Assert.Null(geometry.Octree);
            Assert.True(geometry.OctreeDirty);
            var submittedFrames = surface.PresentedFrames;
            var nativeFence = PrivateField<SilkD3D12Fence>(surface, "fence");
            Assert.True(submittedDraws > 0, $"The gated native frame recorded {submittedDraws} draws");
            Assert.True(
                nativeFence.CompletedValue < nativeFence.CurrentValue,
                $"The native queue gate did not hold fence {nativeFence.CurrentValue}: completed {nativeFence.CompletedValue}"
            );

            var position = new Point3D(0.3, 0, 5);
            viewport.Camera.Position = position;
            viewport.Camera.LookDirection = new Vector3D(-0.3, 0, -5);
            var elapsed = Stopwatch.StartNew();
            var skippedDraws = viewport.RenderD3D12Frame(TimeSpan.FromSeconds(3), waitForCompletion: false);
            var dispatcherRan = false;
            await Dispatcher.CurrentDispatcher.InvokeAsync(() => dispatcherRan = true, DispatcherPriority.Input);
            elapsed.Stop();

            Assert.Equal(0, skippedDraws);
            Assert.True(dispatcherRan, "The dispatcher input callback did not run while the GPU queue was gated");
            Assert.True(
                elapsed.Elapsed < TimeSpan.FromSeconds(1),
                $"The pending-frame dispatcher round trip took {elapsed.Elapsed}"
            );
            Assert.Equal(updateCount, node.UpdateCount);
            Assert.Equal(maintenanceCount, node.MaintenanceCount);
            Assert.Null(geometry.Octree);
            Assert.True(geometry.OctreeDirty);
            Assert.Equal(submittedFrames, surface.PresentedFrames);
            Assert.Equal(1, notifications);

            gate.Release();
            var frame = viewport.CaptureD3D12Frame();
            var expectedPosition = new Vector3(0.3f, 0, 5);
            var coloredPixel = Enumerable.Range(0, frame.Pixels.Length / 4)
                .Any(index =>
                    frame.Pixels[index * 4] != 0 || frame.Pixels[index * 4 + 1] != 0 || frame.Pixels[index * 4 + 2] != 0
                );

            Assert.Equal(expectedPosition, node.LastCameraPosition);
            Assert.True(node.UpdateCount > updateCount, "The completed capture did not prepare the actual scene again");
            Assert.True(node.MaintenanceCount > maintenanceCount);
            Assert.NotNull(geometry.Octree);
            Assert.False(geometry.OctreeDirty);
            Assert.Equal(3, notifications);
            Assert.True(coloredPixel, "The completed native frame contained only the black clear color");
            _ = viewport.RenderD3D12Frame(TimeSpan.FromSeconds(4), waitForCompletion: false);
            Assert.Equal(3, notifications);
        }
    );

    /// <summary>Verifies resize and effect-catalog destruction remain deferred until the actual prior GPU fence completes.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public Task ResizeAndEffectsWaitForCompletionWithoutBlockingTheDispatcher() => StaThread.RunAsync(() => {
            using var window = CreateWindow();
            using var surface = CreateSurface(window);
            using var viewport = CreateViewport(surface);
            using var replacementEffects = new DefaultEffectsManager();
            viewport.Items.Add(CreateModel());
            _ = viewport.RenderD3D12Frame(TimeSpan.FromSeconds(1));
            var originalCatalog = surface.FrameRenderer!.PassCatalog;
            var originalWidth = surface.PixelWidth;
            using var gate = new NativeQueueGate(surface);
            _ = viewport.RenderD3D12Frame(TimeSpan.FromSeconds(2), waitForCompletion: false);

            surface.SetEffectsManager(replacementEffects);
            viewport.EffectsManager = replacementEffects;
            surface.Width = 80;
            surface.Height = 60;
            surface.UpdateLayout();
            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, () => { });

            Assert.Equal(originalWidth, surface.PixelWidth);
            Assert.Same(originalCatalog, surface.FrameRenderer!.PassCatalog);
            var disposedBeforeCompletion = originalCatalog.IsDisposed;
            Assert.False(disposedBeforeCompletion);

            gate.Release();
            var frame = viewport.CaptureD3D12Frame();
            var expected = D3D12PresentationSize.Calculate(80, 60, surface.WindowHost.DpiScale);
            var disposedAfterCompletion = originalCatalog.IsDisposed;

            Assert.Equal(expected, (frame.Width, frame.Height));
            Assert.NotSame(originalCatalog, surface.FrameRenderer!.PassCatalog);
            Assert.True(disposedAfterCompletion);
        }
    );

    /// <summary>Verifies deferred resize timeout reaches the renderer error boundary instead of escaping the dispatcher.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public Task DeferredResizeReportsPendingFenceTimeout() => StaThread.RunAsync(() => {
            using var window = CreateWindow();
            using var surface = CreateSurface(window);
            using var viewport = CreateViewport(surface);
            _ = viewport.RenderD3D12Frame(TimeSpan.FromSeconds(1));
            using var gate = new NativeQueueGate(surface);
            _ = viewport.RenderD3D12Frame(TimeSpan.FromSeconds(2), waitForCompletion: false);
            var submission = PrivateField<D3D12FrameSubmission>(surface, "_frameSubmission");
            SetPrivateField(submission, "_submittedAt", Stopwatch.GetElapsedTime(0) - TimeSpan.FromSeconds(6));
            Exception? reported = null;
            surface.DeferredRenderException += (_, args) => reported = args.Exception;

            surface.Width = 80;
            surface.UpdateLayout();
            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, () => { });

            Assert.IsType<TimeoutException>(reported);
            Assert.True(submission.IsPending);
            gate.Release();
        }
    );

    /// <summary>Creates a never-shown native owner without the WS_VISIBLE flag.</summary>
    /// <returns>The hidden native presentation owner.</returns>
    private static HwndSource CreateWindow() => new(
        new HwndSourceParameters(nameof(NonblockingFramePresentationTests)) {
            Width = 48, Height = 32, WindowStyle = unchecked((int) 0x80000000)
        }
    );

    /// <summary>Initializes the actual WARP surface beneath a hidden native owner.</summary>
    /// <param name="window">The hidden native owner.</param>
    /// <returns>The initialized presentation surface.</returns>
    private static D3D12PresentationSurface CreateSurface(HwndSource window) {
        var surface = new D3D12PresentationSurface(SilkDriverType.Warp) { Width = 48, Height = 32 };
        window.RootVisual = surface;
        surface.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, () => { });
        return surface;
    }

    /// <summary>Uses the real viewport frame consumer without subscribing an automatic visible composition host.</summary>
    /// <param name="surface">The actual hidden WARP surface.</param>
    /// <returns>The viewport with a deterministic camera and black clear color.</returns>
    private static Viewport3DX CreateViewport(D3D12PresentationSurface surface) {
        var viewport = new Viewport3DX {
            EnableSwapChainRendering = true,
            BackgroundColor = Colors.Black,
            Camera = new HelixToolkit.Wpf.SharpDX.OrthographicCamera {
                Position = new Point3D(0, 0, 5), LookDirection = new Vector3D(0, 0, -5), UpDirection = new Vector3D(0, 1, 0), Width = 3
            }
        };
        viewport.Items.Add(new AmbientLight3D { Color = Color.FromRgb(0x51, 0x51, 0x51) });
        viewport.Items.Add(new DirectionalLight3D { Color = Colors.LightGray, Direction = new Vector3D(0, 0, -1) });
        SetPrivateField(viewport, "d3d12Surface", surface);
        return viewport;
    }

    /// <summary>Creates genuine triangle geometry whose productive scene-update calls can be observed.</summary>
    /// <returns>The renderable triangle model.</returns>
    private static ObservedMeshModel CreateModel() => new() {
        Material = HelixToolkit.Wpf.SharpDX.DiffuseMaterials.Red,
        Geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([new(-0.5f, 0.5f, 0), new(0.5f, 0.5f, 0), new(0, -0.5f, 0)]),
            Normals = new Vector3Collection([Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ]),
            Indices = new IntCollection([0, 2, 1])
        }
    };

    /// <summary>Reads an existing native owner without adding product-only test injection APIs.</summary>
    /// <typeparam name="T">The expected field value type.</typeparam>
    /// <param name="owner">The product object.</param>
    /// <param name="name">The private field name.</param>
    /// <returns>The exact product field value.</returns>
    private static T PrivateField<T>(object owner, string name) =>
        (T) owner.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

    /// <summary>Assigns an existing private ownership or deterministic-clock field for the isolated integration fixture.</summary>
    /// <param name="owner">The product object.</param>
    /// <param name="name">The existing private field name.</param>
    /// <param name="value">The fixture value.</param>
    private static void SetPrivateField(object owner, string name, object value) =>
        owner.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(owner, value);

    /// <summary>Creates the ordinary mesh node with a scene-update observer.</summary>
    private sealed class ObservedMeshModel : MeshGeometryModel3D {
        /// <summary>Creates the ordinary production mesh with an observable recording callback.</summary>
        /// <returns>The mesh node used by the real frame recorder.</returns>
        protected override SceneNode OnCreateSceneNode() => new ObservedMeshNode();
    }

    /// <summary>Observes actual scene preparation while retaining all original mesh rendering behavior.</summary>
    private sealed class ObservedMeshNode : MeshNode {
        /// <summary>Gets or sets one callback invoked from the next productive scene update.</summary>
        internal Action<RenderContext>? DuringNextUpdate { get; set; }

        /// Gets the number of productive scene-update calls.
        internal int UpdateCount { get; private set; }

        /// Gets the number of productive non-render maintenance calls.
        internal int MaintenanceCount { get; private set; }

        /// Gets the camera position consumed by the last productive scene update.
        internal Vector3 LastCameraPosition { get; private set; }

        /// <summary>Observes productive preparation and invokes an optional one-shot reentrant lifecycle operation.</summary>
        /// <param name="context">The actual borrowed frame context.</param>
        public override void Update(RenderContext context) {
            UpdateCount++;
            LastCameraPosition = context.Camera!.Position;
            var callback = DuringNextUpdate;
            DuringNextUpdate = null;
            callback?.Invoke(context);
            base.Update(context);
        }

        /// <summary>Counts actual non-render maintenance without changing its behavior.</summary>
        /// <param name="context">The actual borrowed frame context.</param>
        public override void UpdateNotRender(RenderContext context) {
            MaintenanceCount++;
            base.UpdateNotRender(context);
        }
    }

    /// <summary>Holds the real native queue at a CPU-signaled fence without touching production scheduling logic.</summary>
    private sealed class NativeQueueGate : IDisposable {
        /// The real gate fence owned for the duration of the blocked queue.
        private readonly SilkD3D12Fence _fence;

        /// The productive queue drained before the gate fence is released.
        private readonly SilkD3D12CommandQueue _queue;

        /// The existing completion fence used to drain the productive queue before gate disposal.
        private readonly SilkD3D12Fence _completionFence;

        /// The native CPU fence-signal entry point.
        private readonly SignalFence _signal;

        /// Records whether the queue gate has already been released.
        private bool _released;

        /// <summary>Inserts an actual ID3D12CommandQueue::Wait before the next productive frame submission.</summary>
        /// <param name="surface">The real initialized product surface.</param>
        internal NativeQueueGate(D3D12PresentationSurface surface) {
            var device = PrivateField<SilkD3D12Device>(surface, "device");
            var queue = PrivateField<SilkD3D12CommandQueue>(surface, "commandQueue");
            _queue = queue;
            _completionFence = PrivateField<SilkD3D12Fence>(surface, "fence");
            _fence = device.CreateFence();
            // ID3D12CommandQueue::Wait is slot 15; ID3D12Fence::Signal is slot 10 in their stable COM ABI.
            var wait = NativeMethod<WaitQueue>(queue.NativePointer, 15);
            _signal = NativeMethod<SignalFence>(_fence.NativePointer, 10);
            Marshal.ThrowExceptionForHR(wait(queue.NativePointer, _fence.NativePointer, 1));
        }

        /// <summary>Releases the real queue gate exactly once from the CPU.</summary>
        internal void Release() {
            if (_released) return;
            Marshal.ThrowExceptionForHR(_signal(_fence.NativePointer, 1));
            _released = true;
        }

        /// <summary>Releases the gate and drains a surviving queue before releasing the independent gate fence.</summary>
        public void Dispose() {
            if (_fence.IsDisposed) return;
            Release();
            if (!_queue.IsDisposed) {
                var drain = _queue.Signal(_completionFence);
                _completionFence.Wait(drain, TimeSpan.FromSeconds(5));
            }

            _fence.Dispose();
        }

        /// <summary>Reads an existing native COM method without requiring unsafe test project settings.</summary>
        /// <typeparam name="T">The exact native entry-point delegate type.</typeparam>
        /// <param name="instance">The actual COM object pointer.</param>
        /// <param name="slot">The method's stable COM vtable slot.</param>
        /// <returns>The native entry point.</returns>
        private static T NativeMethod<T>(nint instance, int slot) where T : Delegate {
            var table = Marshal.ReadIntPtr(instance);
            var address = Marshal.ReadIntPtr(table, slot * IntPtr.Size);
            return Marshal.GetDelegateForFunctionPointer<T>(address);
        }

        /// <summary>Calls the exact native queue-wait entry point.</summary>
        /// <param name="queue">The actual queue COM pointer.</param>
        /// <param name="fence">The actual gate fence COM pointer.</param>
        /// <param name="value">The value the queue must observe before continuing.</param>
        /// <returns>The native HRESULT.</returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int WaitQueue(nint queue, nint fence, ulong value);

        /// <summary>Calls the exact native CPU fence-signal entry point.</summary>
        /// <param name="fence">The actual fence COM pointer.</param>
        /// <param name="value">The CPU completion value.</param>
        /// <returns>The native HRESULT.</returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SignalFence(nint fence, ulong value);
    }
}