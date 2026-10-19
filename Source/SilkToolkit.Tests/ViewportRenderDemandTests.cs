using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;
using MeshGeometry3D = HelixToolkit.SharpDX.Core.MeshGeometry3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

namespace SilkToolkit.Tests;

/// <summary>Exercises actual hidden WARP viewport submissions, demand races, and warmed managed allocations.</summary>
/// <param name="output">Receives allocation measurements after the measured rendering intervals.</param>
[Collection(WpfCollection.Name)]
public sealed class ViewportRenderDemandTests(ITestOutputHelper output) {
    /// <summary>Retries actual HWND rendering after a failed preparation without consuming scene demand.</summary>
    /// <returns>The STA native cancellation, retry, and pixel verification.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public Task FailedPreparationDiscardsRecordingAndPreservesDemand() => StaThread.RunAsync(() => {
        using var driver = new FrameDriver();
        _ = driver.Drain();
        var oldRenderer = driver.Surface.FrameRenderer!;
        var presented = driver.Surface.PresentedFrames;
        var completed = driver.RenderedCount;
        driver.Node.ThrowOnNextUpdate = true;
        driver.Viewport.InvalidateRender();
        Assert.Throws<InvalidOperationException>(() => driver.Viewport.RenderD3D12Frame(FrameTime));
        Assert.Equal(presented, driver.Surface.PresentedFrames);
        Assert.Equal(completed, driver.RenderedCount);
        Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
        Assert.True(oldRenderer.IsDisposed);
        Assert.NotSame(oldRenderer, driver.Surface.FrameRenderer);
        AssertImage(driver.Viewport.CaptureD3D12Frame(), driver.Surface);
        _ = driver.Drain();
    });

    /// <summary>Uses one stable timestamp without allocating frame inputs or advancing the camera.</summary>
    private static readonly TimeSpan FrameTime = TimeSpan.FromSeconds(1);

    /// <summary>Counts actual empty submissions under the public budget, including historical negative coercion.</summary>
    /// <param name="requested">The public dependency-property setting.</param>
    /// <param name="coerced">The expected public and coordinator setting.</param>
    /// <param name="frames">The total submissions per isolated invalidation.</param>
    /// <returns>The hidden STA verification.</returns>
    [Theory]
    [InlineData(6, 6, 6)]
    [InlineData(1, 1, 1)]
    [InlineData(0, 0, 1)]
    [InlineData(-4, 0, 1)]
    [Trait("Category", "Warp")]
    public Task EmptyFramesConsumeThePublicBudget(int requested, int coerced, int frames) => StaThread.RunAsync(() => {
            using var driver = new FrameDriver(drawMesh: false);
            Assert.Equal(6, driver.Viewport.MinimumUpdateCount);
            // Initial attachment can request a fresh budget from inside the first node update.
            _ = driver.Drain();
            driver.Viewport.MinimumUpdateCount = requested;
            Assert.Equal(coerced, driver.Viewport.MinimumUpdateCount);
            Assert.Equal(coerced, driver.Viewport.RenderDemand.MinimumUpdateCount);
            Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
            var presented = driver.Surface.PresentedFrames;
            var updates = driver.Node.UpdateCount;
            var rendered = driver.RenderedCount;
            var onRendered = driver.OnRenderedCount;

            driver.Viewport.InvalidateRender();
            for (var index = 0; index < frames; index++) {
                Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
                Assert.Equal(0, driver.Viewport.RenderD3D12Frame(FrameTime));
                Assert.Equal(presented + (ulong) index + 1, driver.Surface.PresentedFrames);
            }

            Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
            Assert.Equal(updates + frames, driver.Node.UpdateCount);
            Assert.Equal(rendered + frames, driver.RenderedCount);
            Assert.Equal(onRendered + frames, driver.OnRenderedCount);
            var context = driver.Viewport.RenderContext;
            var timestamp = context!.TimeStamp;
            Assert.Equal(0, driver.Viewport.RenderD3D12Frame(FrameTime));
            Assert.Equal(presented + (ulong) frames, driver.Surface.PresentedFrames);
            Assert.Equal(updates + frames, driver.Node.UpdateCount);
            Assert.Equal(rendered + frames, driver.RenderedCount);
            Assert.Equal(onRendered + frames, driver.OnRenderedCount);
            Assert.Same(context, driver.Viewport.RenderContext);
            Assert.Equal(timestamp, context.TimeStamp);
        }
    );

    /// <summary>Applies runtime property changes to the next request without changing an existing budget or waking idle.</summary>
    /// <returns>The hidden STA verification.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public Task RuntimeBudgetChangesAffectOnlySubsequentInvalidations() => StaThread.RunAsync(() => {
            using var driver = new FrameDriver(drawMesh: false);
            driver.Drain();
            driver.Viewport.InvalidateRender();
            _ = driver.Viewport.RenderD3D12Frame(FrameTime);
            driver.Viewport.MinimumUpdateCount = 1;
            Assert.Equal(5UL, driver.Drain());
            driver.Viewport.InvalidateRender();
            Assert.Equal(1UL, driver.Drain());

            driver.Viewport.MinimumUpdateCount = -7;
            Assert.Equal(0, driver.Viewport.MinimumUpdateCount);
            Assert.Equal(0, driver.Viewport.RenderDemand.MinimumUpdateCount);
            Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
            Assert.Equal(0UL, driver.Drain());
            driver.Viewport.InvalidateRender();
            Assert.Equal(1UL, driver.Drain());

            driver.Viewport.MinimumUpdateCount = 6;
            Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
            driver.Viewport.InvalidateRender();
            Assert.Equal(6UL, driver.Drain());
        }
    );

    /// <summary>Connects each public invalidation entry point to its distinct preparation reasons and a real frame.</summary>
    /// <param name="kind">Selects render, renderables, or scene-graph invalidation.</param>
    /// <returns>The hidden STA verification.</returns>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [Trait("Category", "Warp")]
    public Task PublicInvalidationsRetainDistinctPreparationReasons(int kind) => StaThread.RunAsync(() => {
            using var driver = new FrameDriver();
            driver.Drain();
            driver.Viewport.MinimumUpdateCount = 1;
            var expected = D3D12RenderInvalidation.Render;
            switch (kind) {
                case 0:
                    driver.Viewport.InvalidateRender();
                    break;
                case 1:
                    driver.Viewport.InvalidatePerFrameRenderables();
                    expected |= D3D12RenderInvalidation.PerFrameRenderables;
                    break;
                default:
                    driver.Viewport.InvalidateSceneGraph();
                    expected |= D3D12RenderInvalidation.PerFrameRenderables | D3D12RenderInvalidation.SceneGraph;
                    break;
            }

            Assert.True(driver.Viewport.RenderDemand.TryBeginFrame(out var reasons));
            Assert.Equal(expected, reasons);
            driver.Viewport.RenderDemand.CompleteFrame(submitted: false);
            var updates = driver.Node.UpdateCount;
            Assert.Equal(1UL, driver.Drain());
            Assert.Equal(updates + 1, driver.Node.UpdateCount);
        }
    );

    /// <summary>Preserves an invalidation raised inside the actual mesh update after reserving the older request.</summary>
    /// <returns>The hidden STA verification.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public Task InvalidationInsideNodeUpdateRequestsOneFollowingFrame() => StaThread.RunAsync(() => {
            using var driver = new FrameDriver();
            driver.Drain();
            driver.Viewport.MinimumUpdateCount = 1;
            var presented = driver.Surface.PresentedFrames;
            var updates = driver.Node.UpdateCount;
            driver.Node.InvalidateOnNextUpdate = driver.Viewport;

            driver.Viewport.InvalidateRender();
            _ = driver.Viewport.RenderD3D12Frame(FrameTime);

            Assert.Equal(presented + 1, driver.Surface.PresentedFrames);
            Assert.Equal(updates + 1, driver.Node.UpdateCount);
            Assert.Null(driver.Node.InvalidateOnNextUpdate);
            Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
            Assert.Equal(1UL, driver.Drain());
            Assert.Equal(presented + 2, driver.Surface.PresentedFrames);
            Assert.Equal(updates + 2, driver.Node.UpdateCount);
        }
    );

    /// <summary>Preserves one request from either completion event without duplicate notifications or extra frames.</summary>
    /// <param name="useOnRendered">Whether to request from OnRendered rather than the original host Rendered event.</param>
    /// <returns>The hidden STA verification.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Warp")]
    public Task CompletionInvalidationRequestsExactlyOneFollowingFrame(bool useOnRendered) => StaThread.RunAsync(() => {
            using var driver = new FrameDriver();
            driver.Drain();
            driver.Viewport.MinimumUpdateCount = 1;
            var rendered = driver.RenderedCount;
            var onRendered = driver.OnRenderedCount;
            driver.InvalidateFromRendered = !useOnRendered;
            driver.InvalidateFromOnRendered = useOnRendered;

            driver.Viewport.InvalidateRender();
            _ = driver.Viewport.RenderD3D12Frame(FrameTime);

            Assert.Equal(rendered + 1, driver.RenderedCount);
            Assert.Equal(onRendered + 1, driver.OnRenderedCount);
            Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
            Assert.Equal(1UL, driver.Drain());
            Assert.Equal(rendered + 2, driver.RenderedCount);
            Assert.Equal(onRendered + 2, driver.OnRenderedCount);
            Assert.Equal(0, driver.Viewport.RenderD3D12Frame(FrameTime));
            Assert.Equal(rendered + 2, driver.RenderedCount);
            Assert.Equal(onRendered + 2, driver.OnRenderedCount);
        }
    );

    /// <summary>Rejects nested recording during synchronous and previously pending completion callbacks without losing work.</summary>
    /// <param name="pendingCompletion">Whether the callback is raised while draining an older asynchronous submission.</param>
    /// <returns>The hidden STA verification.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Warp")]
    public Task ReentrantCompletionReturnsZeroAndRetainsTheFollowingFrame(bool pendingCompletion) => StaThread.RunAsync(() => {
            using var driver = new FrameDriver();
            driver.Drain();
            driver.Viewport.MinimumUpdateCount = 1;
            var presented = driver.Surface.PresentedFrames;
            var updates = driver.Node.UpdateCount;
            var rendered = driver.RenderedCount;
            var onRendered = driver.OnRenderedCount;
            driver.ReenterFromRendered = true;
            using var gate = pendingCompletion
                ? new NativeQueueGate(driver.Surface)
                : null;

            driver.Viewport.InvalidateRender();
            _ = driver.Viewport.RenderD3D12Frame(FrameTime, waitForCompletion: !pendingCompletion);
            if (pendingCompletion) {
                Assert.Equal(rendered, driver.RenderedCount);
                gate!.Release();
                _ = driver.Viewport.RenderD3D12Frame(FrameTime);
            } else {
                Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
                Assert.Equal(1UL, driver.Drain());
            }

            Assert.Equal(1, driver.ReentrantCalls);
            Assert.Equal(0, driver.ReentrantDraws);
            Assert.Equal(0, driver.UpdatesDuringReentry);
            Assert.Equal(0UL, driver.SubmissionsDuringReentry);
            Assert.Equal(presented + 2, driver.Surface.PresentedFrames);
            Assert.Equal(updates + 2, driver.Node.UpdateCount);
            Assert.Equal(rendered + 2, driver.RenderedCount);
            Assert.Equal(onRendered + 2, driver.OnRenderedCount);
            Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
        }
    );

    /// <summary>Skips demand consumption and node preparation while the real GPU queue is gated, then renders the new request.</summary>
    /// <returns>The hidden STA verification.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public Task BusyGpuCallsPreservePendingInvalidationWithoutPreparingNodes() => StaThread.RunAsync(() => {
            using var driver = new FrameDriver();
            driver.Drain();
            driver.Viewport.MinimumUpdateCount = 1;
            var rendered = driver.RenderedCount;
            var onRendered = driver.OnRenderedCount;
            using var gate = new NativeQueueGate(driver.Surface);
            driver.Viewport.InvalidateRender();
            var draws = driver.Viewport.RenderD3D12Frame(FrameTime, waitForCompletion: false);
            var presented = driver.Surface.PresentedFrames;
            var updates = driver.Node.UpdateCount;
            var maintenance = driver.Node.MaintenanceCount;
            var fence = PrivateField<SilkD3D12Fence>(driver.Surface, "fence");
            Assert.True(draws > 0);
            Assert.True(fence.CompletedValue < fence.CurrentValue);
            Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
            driver.Viewport.InvalidateSceneGraph();

            for (var index = 0; index < 8; index++) {
                Assert.Equal(0, driver.Viewport.RenderD3D12Frame(FrameTime, waitForCompletion: false));
                Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
            }

            Assert.Equal(presented, driver.Surface.PresentedFrames);
            Assert.Equal(updates, driver.Node.UpdateCount);
            Assert.Equal(maintenance, driver.Node.MaintenanceCount);
            Assert.Equal(rendered, driver.RenderedCount);
            Assert.Equal(onRendered, driver.OnRenderedCount);
            gate.Release();
            Assert.Equal(1UL, driver.Drain());
            Assert.Equal(updates + 1, driver.Node.UpdateCount);
            Assert.Equal(maintenance + 1, driver.Node.MaintenanceCount);
            Assert.Equal(rendered + 2, driver.RenderedCount);
            Assert.Equal(onRendered + 2, driver.OnRenderedCount);
        }
    );

    /// <summary>Records worker requests before any queued dispatcher visual notification can run and coalesces their budget.</summary>
    /// <returns>The hidden STA verification.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public Task WorkerBurstRecordsDemandBeforeDispatcherQueueIsPumped() => StaThread.RunAsync(() => {
            using var driver = new FrameDriver();
            driver.Drain();
            var presented = driver.Surface.PresentedFrames;
            var updates = driver.Node.UpdateCount;
            var worker = Task.Run(() => {
                    for (var index = 0; index < 128; index++) {
                        driver.Viewport.InvalidateRender();
                        driver.Viewport.InvalidatePerFrameRenderables();
                        driver.Viewport.InvalidateSceneGraph();
                    }
                }
            );
            Assert.True(worker.Wait(TimeSpan.FromSeconds(5)), "Worker invalidation blocked on the unpumped STA dispatcher.");
            worker.GetAwaiter()
                .GetResult();

            Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
            Assert.Equal(presented, driver.Surface.PresentedFrames);
            Assert.Equal(updates, driver.Node.UpdateCount);
            Assert.Equal(6UL, driver.Drain());
            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, static () => { });
            Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
            Assert.Equal(0, driver.Viewport.RenderD3D12Frame(FrameTime));
            Assert.Equal(presented + 6, driver.Surface.PresentedFrames);
            Assert.Equal(updates + 6, driver.Node.UpdateCount);
        }
    );

    /// <summary>Forces a physical capture submission from idle and validates real colored GPU pixels.</summary>
    /// <returns>The hidden STA verification.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public Task ExplicitCaptureWhileIdleSubmitsAndReadsANewFrame() => StaThread.RunAsync(() => {
            using var driver = new FrameDriver();
            driver.Drain();
            driver.Viewport.MinimumUpdateCount = 1;
            var presented = driver.Surface.PresentedFrames;
            var updates = driver.Node.UpdateCount;
            var rendered = driver.RenderedCount;
            var onRendered = driver.OnRenderedCount;

            var frame = driver.Viewport.CaptureD3D12Frame();

            Assert.Equal(presented + 1, driver.Surface.PresentedFrames);
            Assert.Equal(updates + 1, driver.Node.UpdateCount);
            Assert.Equal(rendered + 1, driver.RenderedCount);
            Assert.Equal(onRendered + 1, driver.OnRenderedCount);
            Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
            AssertImage(frame, driver.Surface);
        }
    );

    /// <summary>Measures one thousand actual idle viewport calls on the same warmed STA without preparation or allocations.</summary>
    /// <returns>The hidden STA allocation verification.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public Task WarmedIdleViewportCallsAllocateZeroBytesAndSubmitNothing() => StaThread.RunAsync(() => {
            using var driver = new FrameDriver();
            driver.Drain();
            for (var index = 0; index < 1000; index++)
                _ = driver.Viewport.RenderD3D12Frame(FrameTime);
            var presented = driver.Surface.PresentedFrames;
            var updates = driver.Node.UpdateCount;
            var maintenance = driver.Node.MaintenanceCount;
            var rendered = driver.RenderedCount;
            var onRendered = driver.OnRenderedCount;
            var context = driver.Viewport.RenderContext;
            var timestamp = context!.TimeStamp;
            _ = GC.GetAllocatedBytesForCurrentThread();

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < 1000; index++)
                _ = driver.Viewport.RenderD3D12Frame(FrameTime);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            output.WriteLine($"Idle viewport: {allocated} bytes for 1000 actual STA calls.");
            Assert.Equal(0L, allocated);
            Assert.Equal(presented, driver.Surface.PresentedFrames);
            Assert.Equal(updates, driver.Node.UpdateCount);
            Assert.Equal(maintenance, driver.Node.MaintenanceCount);
            Assert.Equal(rendered, driver.RenderedCount);
            Assert.Equal(onRendered, driver.OnRenderedCount);
            Assert.Same(context, driver.Viewport.RenderContext);
            Assert.Equal(timestamp, context.TimeStamp);
            Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
        }
    );

    /// <summary>Measures real continuous HWND mesh frames after warm-up, retaining an initial bounded allocation ceiling.</summary>
    /// <returns>The hidden STA allocation and capture verification.</returns>
    [Fact]
    [Trait("Category", "Warp")]
    public Task WarmedDefaultHwndFramesSubmitAndReportManagedAllocations() => StaThread.RunAsync(() => {
            using var driver = new FrameDriver(renderOnDemand: false);
            Assert.False(driver.Viewport.RenderOnDemand);
            for (var index = 0; index < 64; index++)
                _ = driver.Viewport.RenderD3D12Frame(FrameTime);
            const int frames = 128;
            var presented = driver.Surface.PresentedFrames;
            var updates = driver.Node.UpdateCount;
            var maintenance = driver.Node.MaintenanceCount;
            var rendered = driver.RenderedCount;
            var onRendered = driver.OnRenderedCount;
            var viewport = driver.Viewport;
            var draws = 0;
            _ = GC.GetAllocatedBytesForCurrentThread();

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < frames; index++)
                draws += viewport.RenderD3D12Frame(FrameTime);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            output.WriteLine($"Active HWND mesh: {allocated} bytes / {frames} frames = {(double) allocated / frames:F2} bytes/frame.");
            Assert.Equal(presented + frames, driver.Surface.PresentedFrames);
            Assert.Equal(updates + frames, driver.Node.UpdateCount);
            Assert.Equal(maintenance + frames, driver.Node.MaintenanceCount);
            Assert.Equal(rendered + frames, driver.RenderedCount);
            Assert.Equal(onRendered + frames, driver.OnRenderedCount);
            Assert.True(draws >= frames);
        Assert.InRange(allocated, 0L, 2048L * frames);
            AssertImage(viewport.CaptureD3D12Frame(), driver.Surface);
        }
    );

    /// <summary>Checks tightly packed native BGRA pixels contain the red mesh rather than only the black clear color.</summary>
    /// <param name="frame">The explicitly captured native frame, outside allocation measurements.</param>
    /// <param name="surface">The actual presentation owner defining physical dimensions.</param>
    private static void AssertImage(D3D12CapturedFrame frame, D3D12PresentationSurface surface) {
        Assert.Equal(surface.PixelWidth, frame.Width);
        Assert.Equal(surface.PixelHeight, frame.Height);
        Assert.Equal(checked((int) (frame.Width * frame.Height * 4)), frame.Pixels.Length);
        var redPixel = false;
        for (var offset = 0; offset < frame.Pixels.Length; offset += 4)
            if (frame.Pixels[offset + 2] > frame.Pixels[offset] && frame.Pixels[offset + 2] > 16 &&
                frame.Pixels[offset + 3] > 0) {
                redPixel = true;
                break;
            }

        Assert.True(redPixel, "The actual GPU capture did not contain the red triangle over the black background.");
    }

    /// <summary>Reads stable native ownership fields without introducing production test hooks.</summary>
    /// <typeparam name="T">The existing field type.</typeparam>
    /// <param name="owner">The actual product owner.</param>
    /// <param name="name">The stable private ownership field name.</param>
    /// <returns>The actual field value.</returns>
    private static T PrivateField<T>(object owner, string name) =>
        (T) owner.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

    /// <summary>Owns a hidden WARP surface and actual viewport without installing an automatic composition loop.</summary>
    private sealed class FrameDriver : IDisposable {
        /// <summary>Owns the never-shown native presentation parent.</summary>
        private readonly HwndSource window;

        /// <summary>Initializes the native presentation owner, stable camera, and observable ordinary mesh node.</summary>
        /// <param name="drawMesh">Whether to draw the triangle; false observes an attached hidden node in zero-draw frames.</param>
        /// <param name="renderOnDemand">Whether to enable the internal demand consumer.</param>
        internal FrameDriver(bool drawMesh = true, bool renderOnDemand = true) {
            window = new HwndSource(
                new HwndSourceParameters(nameof(ViewportRenderDemandTests)) {
                    Width = 48, Height = 32, WindowStyle = unchecked((int) 0x80000000)
                }
            );
            Surface = new D3D12PresentationSurface(SilkDriverType.Warp) { Width = 48, Height = 32 };
            window.RootVisual = Surface;
            Surface.UpdateLayout();
            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, static () => { });
            Assert.True(Surface.IsInitialized);
            Viewport = new Viewport3DX {
                BackgroundColor = Colors.Black,
                FxaaLevel = FXAALevel.None,
                Camera = new HelixToolkit.Wpf.SharpDX.OrthographicCamera {
                    Position = new Point3D(0, 0, 5), LookDirection = new Vector3D(0, 0, -5), UpDirection = new Vector3D(0, 1, 0), Width = 3
                }
            };
            Assert.False(Viewport.RenderOnDemand);
            Viewport.RenderOnDemand = renderOnDemand;
            typeof(Viewport3DX).GetField("d3d12Surface", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(Viewport, Surface);
            Surface.RenderHost = Viewport;
            var model = new ObservedMeshModel {
                Material = DiffuseMaterials.Red,
                IsRendering = drawMesh,
                Geometry = new MeshGeometry3D {
                    Positions = new Vector3Collection([new(-0.5f, 0.5f, 0), new(0.5f, 0.5f, 0), new(0, -0.5f, 0)]),
                    Normals = new Vector3Collection([Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ]),
                    Indices = new IntCollection([0, 2, 1])
                }
            };
            if (drawMesh) {
                Viewport.Items.Add(new AmbientLight3D { Color = System.Windows.Media.Color.FromRgb(0x51, 0x51, 0x51) });
                Viewport.Items.Add(new DirectionalLight3D { Color = Colors.LightGray, Direction = new Vector3D(0, 0, -1) });
            }

            Viewport.Items.Add(model);
            Node = (ObservedMeshNode) model.SceneNode;
            Viewport.Rendered += HandleRendered;
            Viewport.OnRendered += HandleOnRendered;
        }

        /// <summary>Gets the actual hidden native presentation surface.</summary>
        internal D3D12PresentationSurface Surface { get; }

        /// <summary>Gets the actual production frame consumer.</summary>
        internal Viewport3DX Viewport { get; }

        /// <summary>Gets the node observing real scene preparation.</summary>
        internal ObservedMeshNode Node { get; }

        /// <summary>Gets the original host completion notification count.</summary>
        internal int RenderedCount { get; private set; }

        /// <summary>Gets the viewport completion notification count.</summary>
        internal int OnRenderedCount { get; private set; }

        /// <summary>Gets or sets a one-shot request from the original host completion event.</summary>
        internal bool InvalidateFromRendered { get; set; }

        /// <summary>Gets or sets a one-shot request from the viewport completion event.</summary>
        internal bool InvalidateFromOnRendered { get; set; }

        /// <summary>Gets or sets one attempted nested frame from the original host completion event.</summary>
        internal bool ReenterFromRendered { get; set; }

        /// <summary>Gets the number of attempted nested frames.</summary>
        internal int ReentrantCalls { get; private set; }

        /// <summary>Gets the draw count returned by the attempted nested frame.</summary>
        internal int ReentrantDraws { get; private set; }

        /// <summary>Gets the actual node updates performed inside the attempted nested frame.</summary>
        internal int UpdatesDuringReentry { get; private set; }

        /// <summary>Gets the actual submissions performed inside the attempted nested frame.</summary>
        internal ulong SubmissionsDuringReentry { get; private set; }

        /// <summary>Drains actual requested frames synchronously, with a bounded guard against an accidental render loop.</summary>
        /// <returns>The number of physical submissions, including zero-draw frames.</returns>
        internal ulong Drain() {
            var presented = Surface.PresentedFrames;
            for (var index = 0; index < 64 && Viewport.RenderDemand.HasRenderDemand; index++)
                _ = Viewport.RenderD3D12Frame(FrameTime);
            Assert.False(Viewport.RenderDemand.HasRenderDemand, "Render demand did not settle within 64 actual viewport calls.");
            return Surface.PresentedFrames - presented;
        }

        /// <summary>Counts actual host completion and optionally makes one invalidation or nested frame attempt.</summary>
        /// <param name="sender">The actual viewport host.</param>
        /// <param name="args">The completed-frame notification.</param>
        private void HandleRendered(object? sender, EventArgs args) {
            RenderedCount++;
            if (InvalidateFromRendered) {
                InvalidateFromRendered = false;
                Viewport.InvalidateRender();
            }

            if (!ReenterFromRendered) return;
            ReenterFromRendered = false;
            var updates = Node.UpdateCount;
            var presented = Surface.PresentedFrames;
            ReentrantCalls++;
            ReentrantDraws = Viewport.RenderD3D12Frame(FrameTime);
            UpdatesDuringReentry = Node.UpdateCount - updates;
            SubmissionsDuringReentry = Surface.PresentedFrames - presented;
        }

        /// <summary>Counts actual viewport completion and optionally invalidates exactly once.</summary>
        /// <param name="sender">The actual presentation surface.</param>
        /// <param name="args">The completed-frame notification.</param>
        private void HandleOnRendered(object? sender, EventArgs args) {
            OnRenderedCount++;
            if (!InvalidateFromOnRendered) return;
            InvalidateFromOnRendered = false;
            Viewport.InvalidateRender();
        }

        /// <summary>Unsubscribes observers and releases the actual viewport, surface, and hidden parent in ownership order.</summary>
        public void Dispose() {
            Viewport.Rendered -= HandleRendered;
            Viewport.OnRendered -= HandleOnRendered;
            Viewport.Dispose();
            Surface.Dispose();
            window.Dispose();
        }
    }

    /// <summary>Creates a production mesh node with observers for actual update and maintenance calls.</summary>
    private sealed class ObservedMeshModel : MeshGeometryModel3D {
        /// <summary>Creates the ordinary mesh node used for productive rendering.</summary>
        /// <returns>The observable mesh node.</returns>
        protected override SceneNode OnCreateSceneNode() => new ObservedMeshNode();
    }

    /// <summary>Retains ordinary mesh behavior while counting scene work and supporting one update-time invalidation.</summary>
    private sealed class ObservedMeshNode : MeshNode {
        /// <summary>Gets or sets whether the next real node update throws once before recording draws.</summary>
        internal bool ThrowOnNextUpdate { get; set; }

        /// <summary>Gets the number of actual productive scene updates.</summary>
        internal int UpdateCount { get; private set; }

        /// <summary>Gets the number of actual productive maintenance calls.</summary>
        internal int MaintenanceCount { get; private set; }

        /// <summary>Gets or sets the viewport to invalidate once from its next actual node update.</summary>
        internal Viewport3DX? InvalidateOnNextUpdate { get; set; }

        /// <summary>Performs the actual mesh update before optionally requesting another frame.</summary>
        /// <param name="context">The production frame context.</param>
        public override void Update(RenderContext context) {
            UpdateCount++;
            base.Update(context);
            if (ThrowOnNextUpdate) {
                ThrowOnNextUpdate = false;
                throw new InvalidOperationException("Injected preparation failure.");
            }
            var viewport = InvalidateOnNextUpdate;
            InvalidateOnNextUpdate = null;
            viewport?.InvalidateRender();
        }

        /// <summary>Counts and retains the actual non-render maintenance path.</summary>
        /// <param name="context">The production frame context.</param>
        public override void UpdateNotRender(RenderContext context) {
            MaintenanceCount++;
            base.UpdateNotRender(context);
        }
    }

    /// <summary>Stalls the actual GPU queue at a CPU-signaled fence without modifying renderer scheduling.</summary>
    private sealed class NativeQueueGate : IDisposable {
        /// <summary>Owns the real gate fence.</summary>
        private readonly SilkD3D12Fence gateFence;

        /// <summary>Retains the productive native queue for safe teardown.</summary>
        private readonly SilkD3D12CommandQueue queue;

        /// <summary>Retains the surface completion fence for draining productive work before gate disposal.</summary>
        private readonly SilkD3D12Fence completionFence;

        /// <summary>Retains the native CPU signal entry point.</summary>
        private readonly SignalFence signal;

        /// <summary>Prevents signaling the gate twice.</summary>
        private bool released;

        /// <summary>Enqueues ID3D12CommandQueue::Wait before the next actual frame submission.</summary>
        /// <param name="surface">The initialized real WARP surface with stable native ownership fields.</param>
        internal NativeQueueGate(D3D12PresentationSurface surface) {
            var device = PrivateField<SilkD3D12Device>(surface, "device");
            queue = PrivateField<SilkD3D12CommandQueue>(surface, "commandQueue");
            completionFence = PrivateField<SilkD3D12Fence>(surface, "fence");
            gateFence = device.CreateFence();
            // Stable COM ABI slots: ID3D12CommandQueue::Wait = 15; ID3D12Fence::Signal = 10.
            var wait = NativeMethod<WaitQueue>(queue.NativePointer, 15);
            signal = NativeMethod<SignalFence>(gateFence.NativePointer, 10);
            Marshal.ThrowExceptionForHR(wait(queue.NativePointer, gateFence.NativePointer, 1));
        }

        /// <summary>Releases the native queue gate exactly once from the CPU.</summary>
        internal void Release() {
            if (released) return;
            Marshal.ThrowExceptionForHR(signal(gateFence.NativePointer, 1));
            released = true;
        }

        /// <summary>Unblocks and drains productive GPU work before disposing the gate fence.</summary>
        public void Dispose() {
            if (gateFence.IsDisposed) return;
            Release();
            var drain = queue.Signal(completionFence);
            completionFence.Wait(drain, TimeSpan.FromSeconds(5));
            gateFence.Dispose();
        }

        /// <summary>Reads a stable COM method without requiring unsafe test project settings.</summary>
        /// <typeparam name="T">The exact native entry-point delegate type.</typeparam>
        /// <param name="instance">The actual COM object pointer.</param>
        /// <param name="slot">The stable native vtable slot.</param>
        /// <returns>The native entry-point delegate.</returns>
        private static T NativeMethod<T>(nint instance, int slot) where T : Delegate =>
            Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance), slot * IntPtr.Size));

        /// <summary>Calls the actual native queue-wait entry point.</summary>
        /// <param name="queue">The actual command queue COM pointer.</param>
        /// <param name="fence">The actual gate fence COM pointer.</param>
        /// <param name="value">The required fence value.</param>
        /// <returns>The native HRESULT.</returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int WaitQueue(nint queue, nint fence, ulong value);

        /// <summary>Calls the actual CPU fence-signal entry point.</summary>
        /// <param name="fence">The actual gate fence COM pointer.</param>
        /// <param name="value">The released fence value.</param>
        /// <returns>The native HRESULT.</returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SignalFence(nint fence, ulong value);
    }
}
