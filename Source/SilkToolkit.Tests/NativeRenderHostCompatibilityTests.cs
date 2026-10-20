using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;

namespace SilkToolkit.Tests;

/// Verifies original host contracts against real viewports, contexts and completed native frames.
[Collection(WpfCollection.Name)]
public sealed class NativeRenderHostCompatibilityTests {
    /// <summary>
    ///     Resolves the original host binding path without creating a window or native surface.
    /// </summary>
    /// <returns>The STA binding verification.</returns>
    [Fact]
    [Trait("Category", "Wpf")]
    public Task ViewportIsItsActualHostAndExposesLiveBindings() => StaThread.RunAsync(() => {
            using var viewport = new Viewport3DX { EnableSwapChainRendering = true };
            using var other = new Viewport3DX();
            var host = viewport.RenderHost;
            var target = new Border();
            target.SetBinding(FrameworkElement.TagProperty, new Binding("RenderHost.Viewport") { Source = viewport });
            host.Viewport = viewport;
            var rendered = host.UpdateAndRender();
            Assert.Same(viewport, host);
            Assert.Same(viewport, host.Viewport);
            Assert.Same(viewport, target.Tag);
            Assert.Null(host.RenderContext);
            Assert.Empty(host.PerFrameFlattenedScene);
            Assert.False(rendered);
            Assert.Throws<NotSupportedException>(() => host.Viewport = other);
            host.DpiScale = 1.5f;
            Assert.Equal(1.5, viewport.DpiScale);
            using var effects = new DefaultEffectsManager();
            host.EffectsManager = effects;
            Assert.Same(effects, viewport.EffectsManager);
            host.StartRendering();
            host.StartRendering();
            var composition = PrivateField<CompositionTargetEx>(viewport, "d3d12CompositionTarget");
            var handlerCount = PrivateField<Delegate>(composition, "FrameUpdating")
                .GetInvocationList()
                .Length;
            Assert.Equal(1, handlerCount);
            host.StopRendering();
            var stoppedHandler = PrivateField<Delegate?>(composition, "FrameUpdating");
            Assert.Null(stoppedHandler);
            host.InvalidateRender();
            host.InvalidateSceneGraph();
            host.InvalidatePerFrameRenderables();
        }
    );

    /// <summary>
    ///     Retains an attached geometry placeholder in the real frame without publishing a render exception.
    /// </summary>
    /// <returns>The hidden native startup verification.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public Task EmptyMeshPlaceholderDoesNotBreakViewportStartup() => StaThread.RunAsync(() => {
            using var window = CreateWindow();
            using var surface = CreateSurface(window);
            using var viewport = CreateViewport(surface);
            using var effects = new DefaultEffectsManager();
            viewport.EffectsManager = effects;
            var placeholder = new MeshGeometryModel3D { Material = PhongMaterials.Red };
            viewport.Items.Add(placeholder);
            var rendered = viewport.UpdateAndRender();
            Assert.True(rendered);
            Assert.True(placeholder.SceneNode.IsAttached);
            Assert.True(string.IsNullOrEmpty(viewport.MessageText));
            Assert.Contains(viewport.PerFrameFlattenedScene, entry => ReferenceEquals(entry.Value, placeholder.SceneNode));
            placeholder.Geometry = new global::HelixToolkit.SharpDX.Core.MeshGeometry3D {
                Positions = [new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)], Indices = [0, 1, 2]
            };
            var draws = viewport.RenderD3D12Frame(TimeSpan.FromSeconds(1));
            Assert.Equal(1, draws);
            Assert.True(string.IsNullOrEmpty(viewport.MessageText));
        }
    );

    /// <summary>
    ///     Counts valid empty frames and retains the actual update context until a fence-safe replacement.
    /// </summary>
    /// <returns>The hidden WARP verification.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public Task EmptyFramesCompleteAndActualContextSurvivesUntilReplacement() => StaThread.RunAsync(() => {
            using var window = CreateWindow();
            using var surface = CreateSurface(window);
            using var viewport = CreateViewport(surface);
            var host = viewport.RenderHost;
            var notifications = 0;
            host.Rendered += (_, _) => notifications++;
            Assert.Null(host.RenderContext);
            var first = host.UpdateAndRender();
            var context = host.RenderContext;
            Assert.True(first);
            Assert.Equal(1, notifications);
            Assert.NotNull(context);
            Assert.Same(viewport.CameraCore, context.Camera);
            Assert.Same(surface.RenderContext, context);
            var timestamp = context.TimeStamp;
            var second = host.UpdateAndRender();
            Assert.True(second);
            Assert.Equal(2, notifications);
            Assert.Same(context, host.RenderContext);
            Assert.True(context.TimeStamp >= timestamp);
            host.Resize(80, 60);
            var resized = host.UpdateAndRender();
            Assert.True(resized);
            Assert.NotSame(context, host.RenderContext);
            Assert.Null(context.Camera);
            Assert.Equal((float) surface.PixelWidth, host.RenderContext!.ActualWidth);
            Assert.Equal((float) surface.PixelHeight, host.RenderContext.ActualHeight);
            Assert.Equal((float) surface.WindowHost.DpiScale, host.DpiScale);
            var lastContext = host.RenderContext;
            viewport.Dispose();
            Assert.Null(lastContext.Camera);
            Assert.Null(host.RenderContext);
            Assert.Empty(host.PerFrameFlattenedScene);
        }
    );

    /// <summary>
    ///     Retains the exact snapshot and context while a real native queue fence prevents preparation.
    /// </summary>
    /// <returns>The hidden native fence verification.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public Task PendingFenceDoesNotPublishAnotherSceneOrDisposeItsContext() => StaThread.RunAsync(() => {
            using var window = CreateWindow();
            using var surface = CreateSurface(window);
            using var viewport = CreateViewport(surface);
            var model = new ObservedGroupModel();
            viewport.Items.Add(model);
            var host = viewport.RenderHost;
            var notifications = 0;
            host.Rendered += (_, _) => notifications++;
            var initial = host.UpdateAndRender();
            var observed = (ObservedGroup) model.SceneNode;
            var context = host.RenderContext;
            Assert.True(initial);
            Assert.Same(host, observed.HostDuringAttach);
            Assert.Same(context, observed.LastContext);
            Assert.Same(host, observed.RenderHost);
            using var gate = new NativeQueueGate(surface);
            _ = viewport.RenderD3D12Frame(TimeSpan.FromSeconds(2), waitForCompletion: false);
            var snapshot = host.PerFrameFlattenedScene.ToArray();
            var updateCount = observed.UpdateCount;
            viewport.Items.Add(new GroupModel3D());
            viewport.Camera = new HelixToolkit.Wpf.SharpDX.PerspectiveCamera {
                Position = new Point3D(1, 0, 5), LookDirection = new Vector3D(-1, 0, -5)
            };
            var skipped = viewport.RenderD3D12Frame(TimeSpan.FromSeconds(3), waitForCompletion: false);
            Assert.Equal(0, skipped);
            Assert.Equal(1, notifications);
            Assert.Equal(updateCount, observed.UpdateCount);
            Assert.Equal(snapshot, host.PerFrameFlattenedScene.ToArray());
            Assert.Same(context, host.RenderContext);
            Assert.NotNull(context!.Camera);
            gate.Release();
            var resumed = host.UpdateAndRender();
            Assert.True(resumed);
            Assert.Equal(3, notifications);
            Assert.True(observed.UpdateCount > updateCount);
            Assert.NotSame(context, host.RenderContext);
            Assert.Null(context.Camera);
            Assert.Contains(host.PerFrameFlattenedScene, pair => ReferenceEquals(pair.Value, viewport.Items[1].SceneNode));
            observed.Detach();
            Assert.Null(observed.RenderHost);
        }
    );

    /// <summary>
    ///     Assigns the real owner before original recursive effects-manager attachment callbacks.
    /// </summary>
    /// <returns>The hidden original-attachment verification.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public Task RecursiveOriginalAttachmentSeesTheActualHost() => StaThread.RunAsync(() => {
            using var window = CreateWindow();
            using var surface = CreateSurface(window);
            using var viewport = CreateViewport(surface);
            using var effects = new DefaultEffectsManager();
            var root = new ObservedGroupModel();
            var child = new ObservedGroupModel();
            root.Children.Add(child);
            viewport.Items.Add(root);
            viewport.EffectsManager = effects;
            var rendered = viewport.UpdateAndRender();
            var observedRoot = (ObservedGroup) root.SceneNode;
            var observedChild = (ObservedGroup) child.SceneNode;
            Assert.True(rendered);
            Assert.Same(viewport, observedRoot.HostDuringAttach);
            Assert.Same(viewport, observedChild.HostDuringAttach);
            Assert.Same(viewport.RenderContext, observedChild.LastContext);
            Assert.Same(effects, observedChild.RenderHost!.EffectsManager);
        }
    );

    /// <summary>
    ///     Keeps independent actual viewport hierarchies and contexts under independent native owners.
    /// </summary>
    /// <returns>The hidden multiview verification.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public Task ViewportsKeepTheirOwnActualFrameOwners() => StaThread.RunAsync(() => {
            using var firstWindow = CreateWindow();
            using var secondWindow = CreateWindow();
            using var firstSurface = CreateSurface(firstWindow);
            using var secondSurface = CreateSurface(secondWindow);
            using var first = CreateViewport(firstSurface);
            using var second = CreateViewport(secondSurface);
            var firstModel = new ObservedGroupModel();
            var secondModel = new ObservedGroupModel();
            first.Items.Add(firstModel);
            second.Items.Add(secondModel);
            var firstRendered = first.UpdateAndRender();
            var secondRendered = second.UpdateAndRender();
            Assert.True(firstRendered);
            Assert.True(secondRendered);
            Assert.Same(first, firstModel.SceneNode.RenderHost);
            Assert.Same(second, secondModel.SceneNode.RenderHost);
            Assert.NotSame(first.RenderContext, second.RenderContext);
            Assert.NotSame(first.PerFrameFlattenedScene, second.PerFrameFlattenedScene);
            Assert.DoesNotContain(first.PerFrameFlattenedScene, pair => ReferenceEquals(pair.Value, secondModel.SceneNode));
            first.Dispose();
            Assert.Null(firstModel.SceneNode.RenderHost);
            Assert.Same(second, secondModel.SceneNode.RenderHost);
            Assert.NotNull(second.RenderContext!.Camera);
        }
    );

    /// <summary>
    ///     Creates a never-shown native parent without WS_VISIBLE.
    /// </summary>
    /// <returns>The hidden native owner.</returns>
    private static HwndSource CreateWindow() => new(
        new HwndSourceParameters(nameof(NativeRenderHostCompatibilityTests)) {
            Width = 48, Height = 32, WindowStyle = unchecked((int) 0x80000000)
        }
    );

    /// <summary>
    ///     Attaches a genuine WARP presentation surface under the hidden parent.
    /// </summary>
    /// <param name="window">The hidden parent.</param>
    /// <returns>The initialized product surface.</returns>
    private static D3D12PresentationSurface CreateSurface(HwndSource window) {
        var surface = new D3D12PresentationSurface(SilkDriverType.Warp) { Width = 48, Height = 32 };
        window.RootVisual = surface;
        surface.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, () => { });
        return surface;
    }

    /// <summary>
    ///     Uses the actual viewport frame consumer without automatic visible composition.
    /// </summary>
    /// <param name="surface">The real hidden surface.</param>
    /// <returns>The actual viewport owner.</returns>
    private static Viewport3DX CreateViewport(D3D12PresentationSurface surface) {
        var viewport = new Viewport3DX { EnableSwapChainRendering = true };
        typeof(Viewport3DX).GetField("d3d12Surface", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(viewport, surface);
        surface.RenderHost = viewport;
        return viewport;
    }

    /// Observes actual native attachment and update callbacks without replacing a host or context.
    private sealed class ObservedGroup : GroupNode {
        /// Gets the actual host observed during native attachment.
        internal IRenderHost? HostDuringAttach { get; private set; }

        /// Gets the actual most recent update context.
        internal RenderContext? LastContext { get; private set; }

        /// Gets the number of actual updates.
        internal int UpdateCount { get; private set; }

        internal override bool Attach() {
            HostDuringAttach = RenderHost;
            return base.Attach();
        }

        protected override bool OnAttach(IEffectsManager effectsManager) {
            HostDuringAttach = RenderHost;
            return base.OnAttach(effectsManager);
        }

        public override void Update(RenderContext context) {
            LastContext = context;
            UpdateCount++;
            base.Update(context);
        }
    }

    /// Creates an ordinary group with observable native callbacks.
    private sealed class ObservedGroupModel : GroupModel3D {
        protected override SceneNode OnCreateSceneNode() => new ObservedGroup();
    }

    /// <summary>
    ///     Reads the actual native owner for queue-fence regression control.
    /// </summary>
    /// <typeparam name="T">The native owner type.</typeparam>
    /// <param name="owner">The product instance.</param>
    /// <param name="name">The existing field name.</param>
    /// <returns>The actual field value.</returns>
    private static T PrivateField<T>(object owner, string name) =>
        (T) owner.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

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

        /// <summary>
        ///     Inserts an actual ID3D12CommandQueue::Wait before the next productive frame submission.
        /// </summary>
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

        public void Dispose() {
            if (_fence.IsDisposed) return;
            Release();
            var drain = _queue.Signal(_completionFence);
            _completionFence.Wait(drain, TimeSpan.FromSeconds(5));
            _fence.Dispose();
        }

        /// <summary>
        ///     Reads an existing native COM method without requiring unsafe test project settings.
        /// </summary>
        /// <typeparam name="T">The exact native entry-point delegate type.</typeparam>
        /// <param name="instance">The actual COM object pointer.</param>
        /// <param name="slot">The method's stable COM vtable slot.</param>
        /// <returns>The native entry point.</returns>
        private static T NativeMethod<T>(nint instance, int slot) where T : Delegate {
            var table = Marshal.ReadIntPtr(instance);
            var address = Marshal.ReadIntPtr(table, slot * IntPtr.Size);
            return Marshal.GetDelegateForFunctionPointer<T>(address);
        }

        /// <summary>
        ///     Calls the exact native queue-wait entry point.
        /// </summary>
        /// <param name="queue">The actual queue COM pointer.</param>
        /// <param name="fence">The actual gate fence COM pointer.</param>
        /// <param name="value">The value the queue must observe before continuing.</param>
        /// <returns>The native HRESULT.</returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int WaitQueue(nint queue, nint fence, ulong value);

        /// <summary>
        ///     Calls the exact native CPU fence-signal entry point.
        /// </summary>
        /// <param name="fence">The actual fence COM pointer.</param>
        /// <param name="value">The CPU completion value.</param>
        /// <returns>The native HRESULT.</returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SignalFence(nint fence, ulong value);
    }
}