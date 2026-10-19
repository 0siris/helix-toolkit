using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using HelixToolkit.Wpf.SharpDX.Elements2D;
using Xunit;
using Color = System.Windows.Media.Color;
using DiffuseMaterial = HelixToolkit.Wpf.SharpDX.DiffuseMaterial;
using MeshGeometry3D = HelixToolkit.SharpDX.Core.MeshGeometry3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace SilkToolkit.Tests;

/// <summary>Exercises the productive common renderer, native bridge and WPF canvas on actual hardware.</summary>
/// <param name="output">Receives measurements after the allocation intervals have ended.</param>
[Collection(WpfCollection.Name)]
public sealed class D3D12WpfPresentationTests(ITestOutputHelper output) {
    /// <summary>Uses a stable camera timestamp for explicit, dispatcher-owned ticks.</summary>
    private static readonly TimeSpan FrameTime = TimeSpan.FromSeconds(1);

    /// <summary>Bounds all test-only asynchronous transport and lifecycle retries.</summary>
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Disposes an uninitialized owner repeatedly without creating native resources or submitting work.</summary>
    /// <returns>The STA verification without GPU initialization.</returns>
    [Fact]
    public Task UninitializedDisposalIsANoOpAndCanBeRepeated() => StaThread.RunAsync(() => {
            using var canvas = new DPFCanvas();
            using var viewport = new Viewport3DX();
            var owner = viewport.InitializeWpfPresentation(canvas);
            viewport.StopRendering();
            Assert.False(owner.IsInitialized);
            Assert.Equal(0, owner.RenderFrame(FrameTime));
            owner.Dispose();
            owner.Dispose();
            viewport.Dispose();
            viewport.Dispose();
            Assert.True(owner.IsDisposed);
            Assert.False(owner.HasPendingTransport);
            Assert.Equal(0UL, owner.Generation);
            Assert.Equal(0UL, owner.SubmittedFrames);
            Assert.Equal(0UL, owner.CompletedFrames);
            Assert.Equal(0UL, owner.CopiedFrames);
            Assert.Equal(0UL, owner.PublishedFrames);
            Assert.Equal(0UL, owner.ReboundFrames);
            Assert.Equal(nint.Zero, canvas.ImageSource.BoundSurface);
            Assert.Equal(0, owner.RenderFrame(FrameTime));
        }
    );

    /// <summary>Proves mesh shader pixels, premultiplied WPF output and straight BGRA capture with FXAA off and on.</summary>
    /// <returns>The explicit hardware pixel verification on one STA.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task MeshClearAndFxaaReachTheActualD3DImage() => StaThread.RunAsync(async () => {
            await using var driver = new FrameDriver();
            await driver.WarmAsync();
            Color[] clears = [
                Color.FromArgb(0, 255, 0, 0),
                Color.FromArgb(128, 0, 255, 0),
                Color.FromArgb(255, 0, 0, 255),
                Color.FromArgb(64, 224, 96, 32),
                Color.FromArgb(4, 192, 64, 128)
            ];
            foreach (var fxaa in new[] { FXAALevel.None, FXAALevel.High }) {
                driver.Viewport.FxaaLevel = fxaa;
                foreach (var clear in clears) {
                    driver.Viewport.BackgroundColor = clear;
                    driver.Viewport.InvalidateRender();
                    await driver.DrainAsync();
                    var captured = driver.Owner.CaptureFrame();
                    await driver.DrainAsync();
                    var image = driver.Snapshot();
                    Assert.NotEqual(nint.Zero, driver.Canvas.ImageSource.BoundSurface);
                    Assert.Equal(driver.Canvas.PixelWidth, captured.Width);
                    Assert.Equal(driver.Canvas.PixelHeight, captured.Height);
                    Assert.Equal(checked((int) (captured.Width * captured.Height * 4)), captured.Pixels.Length);
                    AssertPixel(
                        image, driver.Canvas.PixelWidth, 2, 2,
                        clear.B * clear.A / 255, clear.G * clear.A / 255, clear.R * clear.A / 255, clear.A, 3
                    );
                    // Low-alpha straight colors are quantized by premultiplied BGRA8 storage.
                    var straightTolerance = clear.A == 0
                        ? 0
                        : (int) Math.Ceiling(255d / clear.A) + 2;
                    AssertPixel(
                        captured.Pixels, captured.Width, 2, 2,
                        clear.A == 0
                            ? 0
                            : clear.B, clear.A == 0
                            ? 0
                            : clear.G,
                        clear.A == 0
                            ? 0
                            : clear.R, clear.A, straightTolerance
                    );
                    AssertDominantMeshPixel(image, driver.Canvas.PixelWidth, driver.Canvas.PixelHeight, red: true);
                    AssertDominantMeshPixel(captured.Pixels, captured.Width, captured.Height, red: true);
                }
            }

            // A material change must change shader output, rather than merely reproducing the clear color.
            driver.Model.Material = DiffuseMaterials.Green;
            driver.Viewport.InvalidateRender();
            await driver.DrainAsync();
            AssertDominantMeshPixel(driver.Snapshot(), driver.Canvas.PixelWidth, driver.Canvas.PixelHeight, red: false);
            var green = driver.Owner.CaptureFrame();
            AssertDominantMeshPixel(green.Pixels, green.Width, green.Height, red: false);
            await driver.DrainAsync();
            Assert.True(driver.Owner.PublishedFrames > 0);
            Assert.Same(driver.Owner.RenderContext, driver.Viewport.RenderContext);
            Assert.Same(driver.Owner.PerFrameFlattenedScene, driver.Viewport.PerFrameFlattenedScene);
            Assert.Contains(driver.Viewport.PerFrameFlattenedScene, item => ReferenceEquals(item.Value, driver.Node));
        }
    );

    /// <summary>Checks translucent material blending over a translucent clear before and after FXAA.</summary>
    /// <returns>The actual premultiplied blend and straight capture verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task TranslucentMeshPreservesCoverageAndPremultipliedColor() => StaThread.RunAsync(async () => {
            await using var driver = new FrameDriver();
            driver.Viewport.BackgroundColor = Color.FromArgb(128, 0, 0, 255);
            driver.Viewport.OitRenderMode = OITRenderType.None;
            driver.Model.IsTransparent = true;
            driver.Model.Material =
                new DiffuseMaterial { DiffuseColor = new Silk.NET.Maths.Vector4D<float>(1, 0, 0, 0.5f), EnableUnLit = true };
            await driver.WarmAsync();
            foreach (var fxaa in new[] { FXAALevel.None, FXAALevel.High }) {
                driver.Viewport.FxaaLevel = fxaa;
                driver.Viewport.InvalidateRender();
                await driver.DrainAsync();
                var captured = driver.Owner.CaptureFrame();
                await driver.DrainAsync();
                var x = checked((int) captured.Width / 2);
                var y = checked((int) captured.Height / 2);
                // Source-over: (R=.5, B=.5*.5, A=.5+.5*.5), with one BGRA8 quantization.
                AssertPixel(driver.Snapshot(), captured.Width, x, y, 64, 0, 128, 192, 3);
                AssertPixel(captured.Pixels, captured.Width, x, y, 85, 0, 170, 192, 4);
            }
        }
    );

    /// <summary>Draws real D2D rectangle geometry after FXAA and wakes a drained owner through Scene2D notification.</summary>
    /// <returns>The actual overlay pixel and notification verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task Scene2DGeometryComposesAfterFxaaAndInvalidatesTheOwner() => StaThread.RunAsync(async () => {
            await using var driver = new FrameDriver();
            var overlay = (Overlay) typeof(Viewport3DX)
                .GetProperty("Overlay2D", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(driver.Viewport)!;
            overlay.EnableBitmapCache = false;
            var rectangle = new RectangleModel2D {
                Width = 16,
                Height = 16,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Fill = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255)),
                StrokeThickness = 0,
                EnableBitmapCache = false
            };
            overlay.Children.Add(rectangle);
            try {
                driver.Viewport.FxaaLevel = FXAALevel.High;
                await driver.WarmAsync();
                AssertPixel(driver.Snapshot(), driver.Canvas.PixelWidth, 8, 8, 128, 0, 0, 128, 8);
                Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
                var submitted = driver.Owner.SubmittedFrames;
                rectangle.Fill = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
                rectangle.SceneNode.InvalidateRender();
                Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
                await driver.DrainAsync();
                Assert.True(driver.Owner.SubmittedFrames > submitted);
                AssertPixel(driver.Snapshot(), driver.Canvas.PixelWidth, 8, 8, 0, 128, 0, 128, 8);
                AssertDominantMeshPixel(driver.Snapshot(), driver.Canvas.PixelWidth, driver.Canvas.PixelHeight, red: true);
                rectangle.Width = 24;
                Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
                await driver.DrainAsync();
                AssertPixel(driver.Snapshot(), driver.Canvas.PixelWidth, 20, 8, 0, 128, 0, 128, 8);
                var captured = driver.Owner.CaptureFrame();
                AssertPixel(captured.Pixels, captured.Width, 8, 8, 0, 255, 0, 128, 8);
                await driver.DrainAsync();
                for (var frame = 0; frame < 64; frame++) {
                    driver.Viewport.InvalidateRender();
                    _ = driver.Owner.RenderFrame(FrameTime, waitForCompletion: true);
                    await driver.DrainAsync();
                }

                const int frames = 128;
                var before = driver.Counts;
                var start = GC.GetAllocatedBytesForCurrentThread();
                for (var frame = 0; frame < frames; frame++) {
                    driver.Viewport.InvalidateRender();
                    _ = driver.Owner.RenderFrame(FrameTime, waitForCompletion: true);
                }

                var allocated = GC.GetAllocatedBytesForCurrentThread() - start;
                output.WriteLine(
                    $"Actual WPF mesh + FXAA + Scene2D: {allocated} bytes / {frames} frames = " +
                    $"{(double) allocated / frames:F2} bytes/frame."
                );
                driver.AssertAdvanced(before, frames);
                Assert.InRange(allocated, 0L, 512L * frames);
                AssertPixel(driver.Snapshot(), driver.Canvas.PixelWidth, 8, 8, 0, 128, 0, 128, 8);
            } finally {
                overlay.Children.Remove(rectangle);
                rectangle.Dispose();
            }
        }
    );

    /// <summary>Counts isolated budgets after attachment and proves idle ticks leave the bound mesh image unchanged.</summary>
    /// <returns>The actual six, one, zero-setting and zero-draw budget verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task IsolatedBudgetsPublishExactlyTheirSubmittedFramesAndThenStayIdle() => StaThread.RunAsync(async () => {
            await using var driver = new FrameDriver();
            Assert.Equal(6, driver.Viewport.MinimumUpdateCount);
            await driver.WarmAsync();
            foreach (var budget in new[] { 6, 1, 0 }) {
                driver.Viewport.MinimumUpdateCount = budget;
                Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
                var before = driver.Counts;
                driver.Viewport.InvalidateRender();
                await driver.DrainAsync();
                driver.AssertAdvanced(before, Math.Max(1, budget));
                var image = driver.Snapshot();
                var surface = driver.Canvas.ImageSource.BoundSurface;
                var idle = driver.Counts;
                for (var tick = 0; tick < 1000; tick++)
                    _ = driver.Owner.RenderFrame(FrameTime);
                Assert.Equal(idle, driver.Counts);
                Assert.Equal(surface, driver.Canvas.ImageSource.BoundSurface);
                Assert.Equal(image, driver.Snapshot());
            }

            driver.Model.IsRendering = false;
            await driver.DrainAsync();
            var empty = driver.Counts;
            driver.Viewport.InvalidateRender();
            Assert.Equal(0, driver.Owner.RenderFrame(FrameTime, waitForCompletion: true));
            await driver.DrainAsync();
            driver.AssertAdvanced(empty, 1);
        }
    );

    /// <summary>Preserves one new request from node preparation and from each actual GPU-completion event.</summary>
    /// <returns>The dispatcher-owned invalidation race verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task NodeAndCompletionInvalidationsSurviveTheCurrentReservation() => StaThread.RunAsync(async () => {
            await using var driver = new FrameDriver();
            await driver.WarmAsync();
            for (var origin = 0; origin < 3; origin++) {
                var before = driver.Counts;
                driver.Node.InvalidateOnNextUpdate = origin == 0
                    ? driver.Viewport
                    : null;
                driver.InvalidateFromRendered = origin == 1;
                driver.InvalidateFromOnRendered = origin == 2;
                driver.Viewport.InvalidateRender();
                _ = driver.Owner.RenderFrame(FrameTime, waitForCompletion: true);
                Assert.Equal(before.Submitted + 1, driver.Owner.SubmittedFrames);
                Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
                await driver.DrainAsync();
                driver.AssertAdvanced(before, 2);
                Assert.False(driver.Owner.HasPendingTransport);
            }
        }
    );

    /// <summary>Stalls the actual native queue while input remains responsive and the next scene budget remains intact.</summary>
    /// <returns>The real GPU backpressure and eventual pixel verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task GatedGpuQueueKeepsDispatcherLiveAndDoesNotConsumeNewDemand() => StaThread.RunAsync(async () => {
            await using var driver = new FrameDriver();
            await driver.WarmAsync();
            var before = driver.Counts;
            using (var gate = new NativeQueueGate(driver.Owner)) {
                driver.Viewport.InvalidateRender();
                Assert.True(driver.Owner.RenderFrame(FrameTime) > 0);
                Assert.True(driver.Owner.HasPendingTransport);
                Assert.Equal(before.Submitted + 1, driver.Owner.SubmittedFrames);
                Assert.Equal(before.Completed, driver.Owner.CompletedFrames);
                Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
                driver.Viewport.Camera.Position = new Point3D(0.3, 0, 5);
                driver.Viewport.Camera.LookDirection = new Vector3D(-0.3, 0, -5);
                driver.Viewport.InvalidateRender();
                var gated = driver.Counts;
                var start = Stopwatch.GetTimestamp();
                for (var tick = 0; tick < 128; tick++)
                    Assert.Equal(0, driver.Owner.RenderFrame(FrameTime));
                var inputRan = false;
                await Dispatcher.CurrentDispatcher.InvokeAsync(() => inputRan = true, DispatcherPriority.Input);
                Assert.True(inputRan);
                Assert.True(Stopwatch.GetElapsedTime(start) < TimeSpan.FromSeconds(1));
                Assert.Equal(gated, driver.Counts);
                Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
                gate.Release();
            }

            await driver.DrainAsync();
            driver.AssertAdvanced(before, 2);
            Assert.Equal(new Vector3(0.3f, 0, 5), driver.Node.LastCameraPosition);
            AssertDominantMeshPixel(driver.Snapshot(), driver.Canvas.PixelWidth, driver.Canvas.PixelHeight, red: true);
        }
    );

    /// <summary>Finishes accepted GPU transport after StopRendering while retaining demand for a later restart.</summary>
    /// <param name="stopFromCompletion">Whether the completion listener stops automatic work reentrantly.</param>
    /// <returns>The actual queue stall and stopped automatic-callback verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task StoppedRenderingFinishesAcceptedTransportWithoutSubmittingNewDemand(bool stopFromCompletion) => StaThread.RunAsync(async ()
            => {
            await using var driver = new FrameDriver();
            await driver.WarmAsync();
            var before = driver.Counts;
            var automaticTick = typeof(D3D12WpfPresentation)
                .GetMethod("OnRendering", BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate<EventHandler<RenderingEventArgs>>(driver.Owner);
            using (var gate = new NativeQueueGate(driver.Owner)) {
                if (stopFromCompletion) driver.Viewport.StartRendering();
                driver.Viewport.InvalidateRender();
                Assert.True(driver.Owner.RenderFrame(FrameTime) > 0);
                if (stopFromCompletion)
                    driver.Viewport.Rendered += (_, _) => driver.Viewport.StopRendering();
                else driver.Viewport.StopRendering();
                driver.Viewport.InvalidateRender();
                automaticTick(driver.Canvas, CreateRenderingArgs(TimeSpan.FromSeconds(2)));
                Assert.Equal(before.Completed, driver.Owner.CompletedFrames);
                Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
                gate.Release();
            }

            var timestamp = 2L;
            await PollAsync(
                () => {
                    automaticTick(driver.Canvas, CreateRenderingArgs(TimeSpan.FromSeconds(++timestamp)));
                    return !driver.Owner.HasPendingTransport;
                }, "stopped viewport's accepted transport publication"
            );
            driver.AssertAdvanced(before, 1);
            Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
            await driver.DrainAsync();
            driver.AssertAdvanced(before, 2);
        }
    );

    /// <summary>Retires suspended transport without bypassing StopRendering when a completion listener restores visibility.</summary>
    /// <returns>The actual queue, suspended-service callback and explicit restart verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task SuspendedServiceRespectsStopAfterCompletionRestoresVisibility() => StaThread.RunAsync(async () => {
            await using var driver = new FrameDriver();
            await driver.WarmAsync();
            var before = driver.Counts;
            var service = typeof(D3D12WpfPresentation)
                .GetMethod("ServiceSuspendedTransport", BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate<EventHandler>(driver.Owner);
            using (var gate = new NativeQueueGate(driver.Owner)) {
                driver.Viewport.InvalidateRender();
                Assert.True(driver.Owner.RenderFrame(FrameTime) > 0);
                driver.Viewport.StopRendering();
                driver.Canvas.Visibility = Visibility.Hidden;
                driver.Owner.RequestRecreation();
                driver.Viewport.Rendered += (_, _) => {
                    driver.Canvas.Visibility = Visibility.Visible;
                    driver.Root.UpdateLayout();
                };
                service(null, EventArgs.Empty);
                Assert.Equal(before.Completed, driver.Owner.CompletedFrames);
                gate.Release();
            }

            await PollAsync(
                () => {
                    service(null, EventArgs.Empty);
                    return !driver.Owner.HasPendingTransport && !driver.Owner.IsInitialized;
                }, "stopped suspended transport retirement"
            );
            Assert.True(driver.Canvas.IsActive);
            Assert.Equal(before.Submitted + 1, driver.Owner.SubmittedFrames);
            Assert.Equal(before.Completed + 1, driver.Owner.CompletedFrames);
            Assert.Equal(before.Published, driver.Owner.PublishedFrames);
            Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
            await driver.DrainAsync();
            Assert.True(driver.Owner.SubmittedFrames > before.Submitted + 1);
            AssertDominantMeshPixel(driver.Snapshot(), driver.Canvas.PixelWidth, driver.Canvas.PixelHeight, red: true);
        }
    );

    /// <summary>Discards commands recorded by an automatic scene update that stops rendering reentrantly.</summary>
    /// <returns>The unsubmitted recording and successful explicit retry verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task StopFromNodeUpdateDiscardsAutomaticCommandsAndRetainsDemand() => StaThread.RunAsync(async () => {
            await using var driver = new FrameDriver();
            await driver.WarmAsync();
            var before = driver.Counts;
            driver.Viewport.StartRendering();
            driver.Node.StopOnNextUpdate = driver.Viewport;
            driver.Viewport.InvalidateRender();
            var automaticTick = typeof(D3D12WpfPresentation)
                .GetMethod("OnRendering", BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate<EventHandler<RenderingEventArgs>>(driver.Owner);
            automaticTick(driver.Canvas, CreateRenderingArgs(TimeSpan.FromSeconds(2)));
            Assert.Equal(before.Submitted, driver.Owner.SubmittedFrames);
            Assert.Equal(before.Completed, driver.Owner.CompletedFrames);
            Assert.Equal(before.Copied, driver.Owner.CopiedFrames);
            Assert.Equal(before.Published, driver.Owner.PublishedFrames);
            Assert.Equal(before.Updates + 1, driver.Node.UpdateCount);
            Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
            Assert.False(driver.Node.IsDisposed);
            await driver.DrainAsync();
            // Native recreation can request one additional attachment frame during the explicit retry.
            Assert.InRange(driver.Owner.SubmittedFrames - before.Submitted, 1UL, 2UL);
            Assert.Equal(driver.Owner.SubmittedFrames, driver.Owner.PublishedFrames);
            AssertDominantMeshPixel(driver.Snapshot(), driver.Canvas.PixelWidth, driver.Canvas.PixelHeight, red: true);
        }
    );

    /// <summary>Retries genuine zero-duration WPF lock failures without replacing the ready frame or bound surface.</summary>
    /// <returns>The actual lock contention, retained transport and single-publication verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task FailedImageLocksRetainOneReadyFrameUntilPublicationSucceeds() => StaThread.RunAsync(async () => {
            await using var driver = new FrameDriver();
            await driver.WarmAsync();
            var before = driver.Counts;
            var bridge = PrivateField<D3D12ImageInteropBridge>(driver.Owner, "bridge");
            var copies = bridge.CopyCount;
            var bound = driver.Canvas.ImageSource.BoundSurface;
            nint ready = 0;
            WithBlockedImageWrites(
                driver.Canvas.ImageSource.NativeImage, () => {
                    driver.Viewport.InvalidateRender();
                    PollWithoutDispatcher(
                        () => {
                            _ = driver.Owner.RenderFrame(FrameTime);
                            return bridge.HasReadySurface;
                        }, "real GPU and interop copy completion with WPF publication blocked"
                    );
                    ready = bridge.ReadySurface;
                    Assert.NotEqual(nint.Zero, ready);
                    Assert.NotEqual(bound, ready);
                    Assert.Equal(before.Submitted + 1, bridge.ReadySerial);
                    Assert.Equal(before.Submitted + 1, driver.Owner.SubmittedFrames);
                    Assert.Equal(before.Completed + 1, driver.Owner.CompletedFrames);
                    Assert.Equal(before.Copied + 1, driver.Owner.CopiedFrames);
                    Assert.Equal(copies + 1, bridge.CopyCount);
                    Assert.Equal(before.Published, driver.Owner.PublishedFrames);
                    Assert.Equal(before.Updates + 1, driver.Node.UpdateCount);
                    Assert.Equal(before.Maintenance + 1, driver.Node.MaintenanceCount);
                    Assert.Equal(before.Rendered + 1, driver.RenderedCount);
                    Assert.Equal(before.OnRendered + 1, driver.OnRenderedCount);
                    var blocked = driver.Counts;
                    for (var tick = 0; tick < 256; tick++) {
                        Assert.True(driver.Canvas.ImageSource.IsFrontBufferAvailable);
                        Assert.Equal(0, driver.Owner.RenderFrame(FrameTime));
                        Assert.Equal(blocked, driver.Counts);
                        Assert.True(bridge.HasReadySurface);
                        Assert.Equal(ready, bridge.ReadySurface);
                        Assert.Equal(before.Submitted + 1, bridge.ReadySerial);
                        Assert.Equal(bound, driver.Canvas.ImageSource.BoundSurface);
                        Assert.Equal(copies + 1, bridge.CopyCount);
                        Assert.True(driver.Owner.HasPendingTransport);
                        Assert.True(driver.Viewport.RenderDemand.HasPublicationDemand);
                        Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
                    }

                    var image = driver.Canvas.ImageSource.NativeImage;
                    var start = GC.GetAllocatedBytesForCurrentThread();
                    for (var tick = 0; tick < 1000; tick++) {
                        _ = image.TryLock(new Duration(TimeSpan.Zero));
                        image.Unlock();
                    }

                    var frameworkAllocated = GC.GetAllocatedBytesForCurrentThread() - start;
                    start = GC.GetAllocatedBytesForCurrentThread();
                    for (var tick = 0; tick < 1000; tick++)
                        _ = driver.Owner.RenderFrame(FrameTime);
                    var allocated = GC.GetAllocatedBytesForCurrentThread() - start;
                    output.WriteLine($"WPF failed-lock retries: framework={frameworkAllocated}, owner={allocated} bytes / 1000 attempts.");
                    Assert.Equal(frameworkAllocated, allocated);
                    Assert.Equal(blocked, driver.Counts);
                }
            );

            await driver.DrainAsync();
            driver.AssertAdvanced(before, 1);
            Assert.Equal(before.Rebound, driver.Owner.ReboundFrames);
            Assert.Equal(copies + 1, bridge.CopyCount);
            Assert.Equal(ready, driver.Canvas.ImageSource.BoundSurface);
            Assert.False(bridge.HasReadySurface);
            Assert.Equal(0UL, bridge.ReadySerial);
            var settled = driver.Counts;
            for (var tick = 0; tick < 256; tick++)
                Assert.Equal(0, driver.Owner.RenderFrame(FrameTime));
            Assert.Equal(settled, driver.Counts);
        }
    );

    /// <summary>Rejects a blocked capture instead of returning a prior readback, then captures a newly accepted serial.</summary>
    /// <returns>The actual publication backpressure and fresh capture identity verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task PublicationBlockedCaptureRejectsCachedPixelsAndRecoversWithAFreshSerial() => StaThread.RunAsync(async () => {
            await using var driver = new FrameDriver();
            await driver.WarmAsync();
            var original = driver.Owner.CaptureFrame();
            var originalSerial = driver.Owner.SubmittedFrames;
            Assert.Equal(originalSerial, driver.Owner.CapturedSerial);
            Assert.NotNull(driver.Owner.LastCapturedFrame);
            AssertDominantMeshPixel(original.Pixels, original.Width, original.Height, red: true);
            await driver.DrainAsync();
            var before = driver.Counts;
            var bridge = PrivateField<D3D12ImageInteropBridge>(driver.Owner, "bridge");
            var bound = driver.Canvas.ImageSource.BoundSurface;
            driver.Model.Material = DiffuseMaterials.Green;
            WithBlockedImageWrites(
                driver.Canvas.ImageSource.NativeImage, () => {
                    driver.Viewport.InvalidateRender();
                    PollWithoutDispatcher(
                        () => {
                            _ = driver.Owner.RenderFrame(FrameTime);
                            return bridge.HasReadySurface;
                        }, "new scene completion before the blocked capture attempt"
                    );
                    var ready = bridge.ReadySurface;
                    var serial = bridge.ReadySerial;
                    var blocked = driver.Counts;
                    Assert.Equal(before.Submitted + 1, serial);
                    Assert.True(serial > originalSerial);
                    Assert.Equal(before.Published, driver.Owner.PublishedFrames);
                    for (var attempt = 0; attempt < 3; attempt++) {
                        Assert.True(driver.Canvas.ImageSource.IsFrontBufferAvailable);
                        var failure = Assert.Throws<InvalidOperationException>(() => driver.Owner.CaptureFrame());
                        Assert.Equal("The WPF transport is not available for a new captured scene.", failure.Message);
                        Assert.Null(driver.Owner.LastCapturedFrame);
                        Assert.Equal(0UL, driver.Owner.CapturedSerial);
                        Assert.Equal(blocked, driver.Counts);
                        Assert.True(bridge.HasReadySurface);
                        Assert.Equal(serial, bridge.ReadySerial);
                        Assert.Equal(ready, bridge.ReadySurface);
                        Assert.Equal(bound, driver.Canvas.ImageSource.BoundSurface);
                    }
                }
            );

            await driver.DrainAsync();
            driver.AssertAdvanced(before, 1);
            var recovered = driver.Counts;
            var requestedSerial = driver.Owner.SubmittedFrames + 1;
            var captured = driver.Owner.CaptureFrame();
            Assert.Equal(requestedSerial, driver.Owner.SubmittedFrames);
            Assert.Equal(requestedSerial, driver.Owner.CapturedSerial);
            Assert.True(driver.Owner.CapturedSerial > originalSerial);
            Assert.NotSame(original.Pixels, captured.Pixels);
            Assert.NotNull(driver.Owner.LastCapturedFrame);
            AssertDominantMeshPixel(captured.Pixels, captured.Width, captured.Height, red: false);
            await driver.DrainAsync();
            driver.AssertAdvanced(recovered, 1);
            Assert.Equal(requestedSerial, driver.Owner.CapturedSerial);
        }
    );

    /// <summary>Retains native and application owners when genuine WPF lock contention blocks disposal preflight.</summary>
    /// <returns>The actual detachment timeout and successful disposal retry verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task DisposalPreflightLockFailureRetainsOwnershipForRetry() => StaThread.RunAsync(async () => {
            await using var driver = new FrameDriver();
            await driver.WarmAsync();
            var before = driver.Counts;
            var bridge = PrivateField<D3D12ImageInteropBridge>(driver.Owner, "bridge");
            var device = PrivateField<SilkD3D12Device>(driver.Owner, "device");
            var renderer = driver.Owner.RenderContext;
            var bound = driver.Canvas.ImageSource.BoundSurface;
            var camera = driver.Viewport.Camera;
            var disposingCount = 0;
            driver.Viewport.Disposing += (_, _) => disposingCount++;
            WithBlockedImageWrites(
                driver.Canvas.ImageSource.NativeImage, () => {
                    for (var attempt = 0; attempt < 3; attempt++) {
                        var failure = Assert.Throws<TimeoutException>(() => driver.Viewport.Dispose());
                        Assert.Equal(
                            "The WPF image is still bound; native resources remain owned for disposal retry.",
                            failure.Message
                        );
                        Assert.False(driver.Viewport.IsDisposed);
                        Assert.False(driver.Owner.IsDisposed);
                        Assert.True(driver.Owner.IsInitialized);
                        Assert.False(bridge.IsDisposed);
                        Assert.False(device.IsDisposed);
                        Assert.Same(driver.Owner, PrivateField<D3D12WpfPresentation>(driver.Viewport, "wpfPresentation"));
                        Assert.Same(bridge, PrivateField<D3D12ImageInteropBridge>(driver.Owner, "bridge"));
                        Assert.Same(device, PrivateField<SilkD3D12Device>(driver.Owner, "device"));
                        Assert.Same(renderer, driver.Owner.RenderContext);
                        Assert.Same(driver.Effects, driver.Viewport.EffectsManager);
                        Assert.Same(camera, driver.Viewport.Camera);
                        Assert.Contains(driver.Model, driver.Viewport.Items);
                        Assert.False(driver.Node.IsDisposed);
                        Assert.False(driver.Effects.IsDisposed);
                        Assert.Equal(0, disposingCount);
                        Assert.Equal(bound, driver.Canvas.ImageSource.BoundSurface);
                        Assert.Equal(before, driver.Counts);
                    }
                }
            );

            driver.Viewport.Dispose();
            Assert.True(driver.Viewport.IsDisposed);
            Assert.True(driver.Owner.IsDisposed);
            Assert.True(bridge.IsDisposed);
            Assert.True(device.IsDisposed);
            Assert.True(driver.Node.IsDisposed);
            Assert.False(driver.Effects.IsDisposed);
            Assert.Equal(1, disposingCount);
            Assert.Equal(nint.Zero, driver.Canvas.ImageSource.BoundSurface);
            Assert.Null(PrivateField<D3D12WpfPresentation?>(driver.Viewport, "wpfPresentation"));
            Assert.Null(driver.Viewport.EffectsManager);
            Assert.Empty(driver.Viewport.Items);
            driver.Viewport.Dispose();
            driver.Owner.Dispose();
            Assert.Equal(1, disposingCount);
            Assert.Equal(before, driver.Counts);
        }
    );

    /// <summary>Replaces actual resized and explicitly invalidated generations without disposing application scene owners.</summary>
    /// <returns>The native replacement and shader pixel verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task ResizeAndRequestedRecreationReplaceGenerationsAndRetainScene() => StaThread.RunAsync(async () => {
            await using var driver = new FrameDriver();
            await driver.WarmAsync();
            var generation = driver.Owner.Generation;
            var context = driver.Owner.RenderContext;
            var nativeImage = driver.Canvas.ImageSource.NativeImage;
            driver.Canvas.Width = 96;
            driver.Canvas.Height = 72;
            driver.Root.UpdateLayout();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            await driver.DrainAsync();
            Assert.Equal(generation + 1, driver.Owner.Generation);
            Assert.NotSame(context, driver.Owner.RenderContext);
            Assert.Same(nativeImage, driver.Canvas.ImageSource.NativeImage);
            Assert.Equal(96, nativeImage.PixelWidth);
            Assert.Equal(72, nativeImage.PixelHeight);
            Assert.False(driver.Node.IsDisposed);
            Assert.False(driver.Effects.IsDisposed);

            generation = driver.Owner.Generation;
            // A genuinely pending old serial may be retired instead of published during recreation.
            using (var gate = new NativeQueueGate(driver.Owner)) {
                driver.Viewport.InvalidateRender();
                _ = driver.Owner.RenderFrame(FrameTime);
                Assert.True(driver.Owner.HasPendingTransport);
                driver.Owner.RequestRecreation();
                Assert.Equal(0, driver.Owner.RenderFrame(FrameTime));
                Assert.Equal(generation, driver.Owner.Generation);
                gate.Release();
            }

            await driver.DrainAsync();
            Assert.Equal(generation + 1, driver.Owner.Generation);
            Assert.False(driver.Owner.HasPendingTransport);
            Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
            Assert.Same(driver.Model, driver.Viewport.Items.Last());
            Assert.Same(driver.Effects, driver.Viewport.EffectsManager);
            Assert.False(driver.Node.IsDisposed);
            Assert.False(driver.Effects.IsDisposed);
            AssertDominantMeshPixel(driver.Snapshot(), driver.Canvas.PixelWidth, driver.Canvas.PixelHeight, red: true);
            var captured = driver.Owner.CaptureFrame();
            Assert.Equal(96u, captured.Width);
            Assert.Equal(72u, captured.Height);
            AssertDominantMeshPixel(captured.Pixels, captured.Width, captured.Height, red: true);
            await driver.DrainAsync();
        }
    );

    /// <summary>Uses genuine unload and reload on the same HWND and dimensions to rebind the retained image only.</summary>
    /// <returns>The detachment, scene ownership and no-render rebind verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task UnloadReloadRebindsTheSameImageWithoutSceneOrCopyWork() => StaThread.RunAsync(async () => {
            await using var driver = new FrameDriver();
            await driver.WarmAsync();
            var before = driver.Counts;
            var generation = driver.Owner.Generation;
            var context = driver.Owner.RenderContext;
            var handle = driver.Canvas.NativeHandle;
            var surface = driver.Canvas.ImageSource.BoundSurface;
            var image = driver.Snapshot();
            driver.Root.Children.Remove(driver.Canvas);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.False(driver.Canvas.IsActive);
            await PollAsync(
                () => {
                    _ = driver.Owner.RenderFrame(FrameTime);
                    return driver.Canvas.ImageSource.BoundSurface == 0;
                }, "actual canvas unload detachment"
            );
            Assert.Equal(nint.Zero, driver.Canvas.NativeHandle);
            Assert.False(driver.Node.IsDisposed);
            Assert.False(driver.Effects.IsDisposed);
            driver.Root.Children.Add(driver.Canvas);
            driver.Root.UpdateLayout();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            await driver.DrainAsync();
            Assert.True(driver.Canvas.IsActive);
            Assert.Equal(handle, driver.Canvas.NativeHandle);
            Assert.Equal(surface, driver.Canvas.ImageSource.BoundSurface);
            Assert.Equal(generation, driver.Owner.Generation);
            Assert.Same(context, driver.Owner.RenderContext);
            Assert.Equal(before.Submitted, driver.Owner.SubmittedFrames);
            Assert.Equal(before.Completed, driver.Owner.CompletedFrames);
            Assert.Equal(before.Copied, driver.Owner.CopiedFrames);
            Assert.Equal(before.Published, driver.Owner.PublishedFrames);
            Assert.Equal(before.Updates, driver.Node.UpdateCount);
            Assert.Equal(before.Maintenance, driver.Node.MaintenanceCount);
            Assert.Equal(before.Rendered, driver.RenderedCount);
            Assert.Equal(before.OnRendered, driver.OnRenderedCount);
            Assert.True(driver.Owner.ReboundFrames > before.Rebound);
            Assert.Equal(image, driver.Snapshot());
            Assert.Same(driver.Effects, driver.Viewport.EffectsManager);
            Assert.False(driver.Node.IsDisposed);
            Assert.False(driver.Effects.IsDisposed);
        }
    );

    /// <summary>Recovers one actual mesh-preparation exception without accepting a frame or losing requested work.</summary>
    /// <returns>The fault, replacement generation and recovered publication verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task ThrowsOnceDuringPreparationThenPublishesTheRetainedScene() => StaThread.RunAsync(async () => {
            await using var driver = new FrameDriver();
            await driver.WarmAsync();
            var before = driver.Counts;
            var generation = driver.Owner.Generation;
            driver.Node.ThrowOnNextUpdate = true;
            driver.Viewport.InvalidateRender();
            var failure = Assert.Throws<InvalidOperationException>(() => driver.Owner.RenderFrame(FrameTime));
            Assert.Equal("Injected WPF mesh preparation failure.", failure.Message);
            Assert.Equal(before.Submitted, driver.Owner.SubmittedFrames);
            Assert.Equal(before.Completed, driver.Owner.CompletedFrames);
            Assert.Equal(before.Copied, driver.Owner.CopiedFrames);
            Assert.Equal(before.Published, driver.Owner.PublishedFrames);
            Assert.Equal(before.Rendered, driver.RenderedCount);
            Assert.Equal(before.OnRendered, driver.OnRenderedCount);
            Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
            await driver.DrainAsync();
            Assert.Equal(generation + 1, driver.Owner.Generation);
            Assert.True(driver.Owner.PublishedFrames > before.Published);
            Assert.False(driver.Node.IsDisposed);
            Assert.False(driver.Effects.IsDisposed);
            AssertDominantMeshPixel(driver.Snapshot(), driver.Canvas.PixelWidth, driver.Canvas.PixelHeight, red: true);
        }
    );

    /// <summary>Aborts unsubmitted recording when node update requests disposal, then completes disposal on the dispatcher.</summary>
    /// <returns>The actual deferred-disposal and detached native pointer verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task DisposeFromNodeUpdateDefersCleanupAndSubmitsNothing() => StaThread.RunAsync(async () => {
            await using var driver = new FrameDriver();
            await driver.WarmAsync();
            var before = driver.Counts;
            driver.Node.DisposeOnNextUpdate = driver.Viewport;
            driver.Viewport.InvalidateRender();
            Assert.Equal(0, driver.Owner.RenderFrame(FrameTime));
            Assert.False(driver.Node.WasDisposedInsideUpdate);
            Assert.False(driver.Viewport.IsDisposed);
            Assert.Equal(before.Submitted, driver.Owner.SubmittedFrames);
            Assert.Equal(before.Copied, driver.Owner.CopiedFrames);
            Assert.Equal(before.Published, driver.Owner.PublishedFrames);
            await PollAsync(() => driver.Viewport.IsDisposed && driver.Owner.IsDisposed, "deferred viewport disposal");
            Assert.Equal(nint.Zero, driver.Canvas.ImageSource.BoundSurface);
            Assert.False(driver.Owner.HasPendingTransport);
            Assert.True(driver.Node.IsDisposed);
            Assert.False(driver.Effects.IsDisposed);
            Assert.Equal(before.Rendered, driver.RenderedCount);
            Assert.Equal(before.OnRendered, driver.OnRenderedCount);
            Assert.Equal(0, driver.Owner.RenderFrame(FrameTime));
            driver.Owner.Dispose();
            driver.Viewport.Dispose();
        }
    );

    /// <summary>Disposes immediately from a completed-scene callback after native recording has closed.</summary>
    /// <returns>The actual completion-event teardown verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task DisposeFromCompletionDetachesAndIsIdempotent() => StaThread.RunAsync(async () => {
            await using var driver = new FrameDriver();
            await driver.WarmAsync();
            var submitted = driver.Owner.SubmittedFrames;
            driver.Viewport.Rendered += (_, _) => driver.Viewport.Dispose();
            driver.Viewport.InvalidateRender();
            _ = driver.Owner.RenderFrame(FrameTime, waitForCompletion: true);
            Assert.Equal(submitted + 1, driver.Owner.SubmittedFrames);
            Assert.True(driver.Viewport.IsDisposed);
            Assert.True(driver.Owner.IsDisposed);
            Assert.Equal(nint.Zero, driver.Canvas.ImageSource.BoundSurface);
            Assert.False(driver.Owner.HasPendingTransport);
            driver.Owner.Dispose();
            driver.Viewport.Dispose();
            Assert.Equal(0, driver.Owner.RenderFrame(FrameTime));
        }
    );

    /// <summary>Measures one thousand actual warmed idle calls on the same STA with no scene or transport activity.</summary>
    /// <returns>The zero-allocation coordination verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task ThousandWarmedIdleCallsAllocateNothingAndChangeNoCounters() => StaThread.RunAsync(async () => {
            await using var driver = new FrameDriver();
            await driver.WarmAsync();
            var owner = driver.Owner;
            for (var tick = 0; tick < 1000; tick++)
                _ = owner.RenderFrame(FrameTime);
            var counts = driver.Counts;
            var context = owner.RenderContext;
            var flattened = owner.PerFrameFlattenedScene;
            var timestamp = context!.TimeStamp;
            var image = driver.Snapshot();
            _ = GC.GetAllocatedBytesForCurrentThread();

            var start = GC.GetAllocatedBytesForCurrentThread();
            for (var tick = 0; tick < 1000; tick++)
                _ = owner.RenderFrame(FrameTime);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - start;

            output.WriteLine($"Actual WPF idle coordination: {allocated} bytes / 1000 STA calls.");
            Assert.Equal(0L, allocated);
            Assert.Equal(counts, driver.Counts);
            Assert.Same(context, owner.RenderContext);
            Assert.Same(flattened, owner.PerFrameFlattenedScene);
            Assert.Equal(timestamp, context.TimeStamp);
            Assert.False(owner.HasPendingTransport);
            Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
            Assert.Equal(image, driver.Snapshot());
        }
    );

    /// <summary>Measures actual warmed mesh recording, GPU completion, interop copy and WPF publication on one STA.</summary>
    /// <returns>The bounded managed allocation and actual 128-frame pipeline verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task WarmedMeshFramesPublishWithinTheMeasuredHeapCeiling() => StaThread.RunAsync(async () => {
            await using var driver = new FrameDriver();
            await driver.WarmAsync();
            for (var frame = 0; frame < 64; frame++) {
                driver.Viewport.InvalidateRender();
                _ = driver.Owner.RenderFrame(FrameTime, waitForCompletion: true);
                await driver.DrainAsync();
            }

            const int frames = 128;
            var owner = driver.Owner;
            var viewport = driver.Viewport;
            var counts = driver.Counts;
            var draws = 0;
            _ = GC.GetAllocatedBytesForCurrentThread();

            var start = GC.GetAllocatedBytesForCurrentThread();
            for (var frame = 0; frame < frames; frame++) {
                viewport.InvalidateRender();
                draws += owner.RenderFrame(FrameTime, waitForCompletion: true);
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - start;

            output.WriteLine(
                $"Actual WPF mesh pipeline: {allocated} bytes / {frames} frames = " +
                $"{(double) allocated / frames:F2} bytes/frame."
            );
            // Check before any drain: missed publication cannot masquerade as inexpensive completed work.
            driver.AssertAdvanced(counts, frames);
            Assert.False(owner.HasPendingTransport);
            Assert.False(viewport.RenderDemand.HasRenderDemand);
            Assert.True(draws >= frames);
            Assert.InRange(allocated, 0L, 256L * frames);
            AssertDominantMeshPixel(driver.Snapshot(), driver.Canvas.PixelWidth, driver.Canvas.PixelHeight, red: true);
            var captured = owner.CaptureFrame();
            AssertDominantMeshPixel(captured.Pixels, captured.Width, captured.Height, red: true);
            await driver.DrainAsync();
        }
    );

    /// <summary>Checks one tightly packed BGRA pixel with explicit alpha and quantization tolerance.</summary>
    /// <param name="pixels">The diagnostic WPF snapshot or explicit capture bytes.</param>
    /// <param name="width">The tightly packed image width.</param>
    /// <param name="x">The sampled column.</param>
    /// <param name="y">The sampled row.</param>
    /// <param name="blue">The expected blue channel.</param>
    /// <param name="green">The expected green channel.</param>
    /// <param name="red">The expected red channel.</param>
    /// <param name="alpha">The expected alpha channel.</param>
    /// <param name="tolerance">The allowed absolute channel error.</param>
    private static void AssertPixel(
        byte[] pixels,
        uint width,
        int x,
        int y,
        int blue,
        int green,
        int red,
        int alpha,
        int tolerance
    ) {
        var offset = checked((y * (int) width + x) * 4);
        Assert.InRange((int) pixels[offset], Math.Max(0, blue - tolerance), Math.Min(255, blue + tolerance));
        Assert.InRange((int) pixels[offset + 1], Math.Max(0, green - tolerance), Math.Min(255, green + tolerance));
        Assert.InRange((int) pixels[offset + 2], Math.Max(0, red - tolerance), Math.Min(255, red + tolerance));
        var alphaTolerance = Math.Min(tolerance, 3);
        Assert.InRange((int) pixels[offset + 3], Math.Max(0, alpha - alphaTolerance), Math.Min(255, alpha + alphaTolerance));
    }

    /// <summary>Proves the actual triangle material shader produced the expected dominant color at its interior.</summary>
    /// <param name="pixels">The diagnostic premultiplied snapshot or straight capture.</param>
    /// <param name="width">The actual image width.</param>
    /// <param name="height">The actual image height.</param>
    /// <param name="red">Whether the mesh material is red rather than green.</param>
    private static void AssertDominantMeshPixel(byte[] pixels, uint width, uint height, bool red) {
        var offset = checked((int) ((height / 2 * width + width / 2) * 4));
        var dominant = pixels[offset + (red
            ? 2
            : 1)];
        Assert.True(dominant > 32, "The mesh interior did not contain the expected material color.");
        Assert.True(dominant > pixels[offset] + 16, "The mesh interior unexpectedly contained dominant blue.");
        Assert.True(
            dominant > pixels[offset + (red
                ? 1
                : 2)] + 16, "The mesh material channels were incorrect."
        );
        Assert.InRange((int) pixels[offset + 3], 240, 255);
    }

    /// <summary>Borrows WPF's real write event and restores it before any dispatcher continuation or fixture cleanup.</summary>
    /// <param name="image">The initialized, genuinely bound native WPF image.</param>
    /// <param name="verify">The synchronous STA assertions to perform while real image writes are blocked.</param>
    /// <remarks>No dispatcher pumping is permitted while the borrowed event is reset; the event is never disposed.</remarks>
    private static void WithBlockedImageWrites(D3DImage image, Action verify) {
        image.Dispatcher.VerifyAccess();
        var field = typeof(D3DImage).GetField("_canWriteEvent", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var canWrite = Assert.IsAssignableFrom<EventWaitHandle>(field.GetValue(image));
        try {
            canWrite.Reset();
            AssertImageWriteLockFails(image);
            verify();
            AssertImageWriteLockFails(image);
            Assert.False(canWrite.WaitOne(0), "The compositor signaled the borrowed write event during the blocked interval.");
        } finally {
            canWrite.Set();
        }
    }

    /// <summary>Proves a genuine zero-duration TryLock failure and balances its normally returned lock count.</summary>
    /// <param name="image">The actual WPF image whose write event is currently reset.</param>
    private static void AssertImageWriteLockFails(D3DImage image) {
        Assert.True(image.IsFrontBufferAvailable, "The test requires available WPF frontbuffer publication.");
        var locked = image.TryLock(new Duration(TimeSpan.Zero));
        try {
            Assert.False(
                locked,
                "Resetting D3DImage._canWriteEvent did not force a real false TryLock; lock contention was not established."
            );
        } finally {
            image.Unlock();
        }
    }

    /// <summary>Polls actual native progress on the STA without allowing compositor dispatcher callbacks to run.</summary>
    /// <param name="completed">The genuine nonblocking transport operation and completion predicate.</param>
    /// <param name="operation">The operation to identify if native progress stalls.</param>
    /// <exception cref="TimeoutException">The real operation fails to complete within five seconds.</exception>
    private static void PollWithoutDispatcher(Func<bool> completed, string operation) {
        var start = Stopwatch.GetTimestamp();
        var spin = new SpinWait();
        while (!completed()) {
            if (Stopwatch.GetElapsedTime(start) >= CompletionTimeout)
                throw new TimeoutException($"{operation} did not finish within {CompletionTimeout}.");
            spin.SpinOnce();
        }
    }

    /// <summary>Creates the framework's timestamp arguments through its nonpublic constructor for callback sequencing.</summary>
    /// <param name="timestamp">The distinct automatic frame timestamp.</param>
    /// <returns>The real WPF rendering arguments used only by the lifecycle test.</returns>
    private static RenderingEventArgs CreateRenderingArgs(TimeSpan timestamp) =>
        (RenderingEventArgs) Activator.CreateInstance(
            typeof(RenderingEventArgs),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [timestamp], null
        )!;

    /// <summary>Retries only test-owned asynchronous work, preserving dispatcher responsiveness and a finite deadline.</summary>
    /// <param name="completed">The actual nonblocking completion attempt.</param>
    /// <param name="operation">The operation to identify if progress stalls.</param>
    /// <returns>The bounded test-only polling task.</returns>
    /// <exception cref="TimeoutException">The real operation fails to complete within five seconds.</exception>
    private static async Task PollAsync(Func<bool> completed, string operation) {
        var start = Stopwatch.GetTimestamp();
        while (!completed()) {
            if (Stopwatch.GetElapsedTime(start) >= CompletionTimeout)
                throw new TimeoutException($"{operation} did not finish within {CompletionTimeout}.");
            await TestOnlyTaskDelay();
        }
    }

    /// <summary>Yields only the test driver; never participates in productive ticks or allocation measurements.</summary>
    /// <returns>A short delay whose continuation returns to the same STA dispatcher.</returns>
    private static Task TestOnlyTaskDelay() => Task.Delay(1);

    /// <summary>Reads a real native ownership field without inserting fake scheduling or counters.</summary>
    /// <typeparam name="T">The actual field type.</typeparam>
    /// <param name="owner">The productive owner.</param>
    /// <param name="name">The existing ownership field name.</param>
    /// <returns>The actual field value.</returns>
    private static T PrivateField<T>(object owner, string name) =>
        (T) owner.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

    /// <summary>Owns an actual viewport and passive canvas under a never-shown WPF HWND, using hardware defaults.</summary>
    private sealed class FrameDriver : IAsyncDisposable {
        /// <summary>Owns the real hidden HWND used by the native bridge.</summary>
        private readonly HwndSource source;

        /// <summary>Prevents duplicate fixture cleanup after explicit lifecycle tests.</summary>
        private bool disposed;

        /// <summary>Creates genuine scene geometry and initializes the actual WPF presentation integration.</summary>
        internal FrameDriver() {
            source = new HwndSource(
                new HwndSourceParameters(nameof(D3D12WpfPresentationTests)) {
                    Width = 128, Height = 96, WindowStyle = unchecked((int) 0x80000000)
                }
            );
            Canvas = new DPFCanvas {
                Width = 64,
                Height = 48,
                EnableDpiScale = false,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            Effects = new DefaultEffectsManager();
            Viewport = new Viewport3DX {
                EffectsManager = Effects,
                BelongsToParentWindow = false,
                BackgroundColor = Colors.Transparent,
                FxaaLevel = FXAALevel.None,
                Camera = new HelixToolkit.Wpf.SharpDX.OrthographicCamera {
                    Position = new Point3D(0, 0, 5), LookDirection = new Vector3D(0, 0, -5), UpDirection = new Vector3D(0, 1, 0), Width = 3
                }
            };
            Viewport.Items.Add(new AmbientLight3D { Color = Color.FromRgb(0x51, 0x51, 0x51) });
            Viewport.Items.Add(new DirectionalLight3D { Color = Colors.LightGray, Direction = new Vector3D(0, 0, -1) });
            Model = new ObservedMeshModel {
                Material = DiffuseMaterials.Red,
                Geometry = new MeshGeometry3D {
                    Positions = new Vector3Collection([new(-0.5f, 0.5f, 0), new(0.5f, 0.5f, 0), new(0, -0.5f, 0)]),
                    Normals = new Vector3Collection([Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ]),
                    Indices = new IntCollection([0, 2, 1])
                }
            };
            Viewport.Items.Add(Model);
            Node = (ObservedMeshNode) Model.SceneNode;
            Owner = Viewport.InitializeWpfPresentation(Canvas);
            Viewport.StopRendering();
            Viewport.Rendered += HandleRendered;
            Viewport.OnRendered += HandleOnRendered;
            Root.Children.Add(Canvas);
            source.RootVisual = Root;
            Root.UpdateLayout();
        }

        /// <summary>Gets the stable WPF root used for genuine visual-tree unload and reload.</summary>
        internal Grid Root { get; } = new();

        /// <summary>Gets the real passive canvas and its D3DImage.</summary>
        internal DPFCanvas Canvas { get; }

        /// <summary>Gets the application-owned manager borrowed by the presentation owner.</summary>
        internal DefaultEffectsManager Effects { get; }

        /// <summary>Gets the actual viewport with its original camera, scene and event APIs.</summary>
        internal Viewport3DX Viewport { get; }

        /// <summary>Gets the actual common-renderer and native-bridge presentation owner.</summary>
        internal D3D12WpfPresentation Owner { get; }

        /// <summary>Gets the ordinary productive mesh model.</summary>
        internal ObservedMeshModel Model { get; }

        /// <summary>Gets the real node observing productive update and maintenance work.</summary>
        internal ObservedMeshNode Node { get; }

        /// <summary>Gets the number of host GPU-completion notifications.</summary>
        internal int RenderedCount { get; private set; }

        /// <summary>Gets the number of viewport GPU-completion notifications.</summary>
        internal int OnRenderedCount { get; private set; }

        /// <summary>Gets or sets a one-shot invalidation from the host completion callback.</summary>
        internal bool InvalidateFromRendered { get; set; }

        /// <summary>Gets or sets a one-shot invalidation from the viewport completion callback.</summary>
        internal bool InvalidateFromOnRendered { get; set; }

        /// <summary>Gets actual owner, node and event counters without allocating reference objects.</summary>
        internal (ulong Submitted, ulong Completed, ulong Copied, ulong Published, ulong Rebound,
            int Updates, int Maintenance, int Rendered, int OnRendered) Counts =>
            (Owner.SubmittedFrames, Owner.CompletedFrames, Owner.CopiedFrames, Owner.PublishedFrames,
                Owner.ReboundFrames, Node.UpdateCount, Node.MaintenanceCount, RenderedCount, OnRenderedCount);

        /// <summary>Allows real WPF loading and initial attachment invalidations to settle before isolated assertions.</summary>
        /// <returns>The dispatcher-owned initialization and transport drain.</summary>
        internal async Task WarmAsync() {
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.True(Canvas.IsActive, "The hidden canvas did not acquire a real loaded WPF presentation source.");
            Assert.Equal(source.Handle, Canvas.NativeHandle);
            await DrainAsync();
            Viewport.MinimumUpdateCount = 1;
            Assert.True(Owner.IsInitialized);
            Assert.NotEqual(nint.Zero, Canvas.ImageSource.BoundSurface);
            Assert.False(Owner.HasPendingTransport);
            Assert.False(Viewport.RenderDemand.HasRenderDemand);
        }

        /// <summary>Drains real scene, GPU, copy and publication work without assuming every old serial is published.</summary>
        /// <returns>The bounded test-only dispatcher drain.</returns>
        internal Task DrainAsync() => PollAsync(
            () => {
                _ = Owner.RenderFrame(FrameTime);
                var demand = Viewport.RenderDemand;
                return !Owner.HasPendingTransport && !demand.HasRenderDemand &&
                       !demand.HasPublicationDemand && !demand.HasRecoveryDemand;
            }, "productive WPF scene and transport drain"
        );

        /// <summary>Reads a diagnostic software snapshot of the real bound image; never supplies presentation pixels.</summary>
        /// <returns>The tightly packed premultiplied BGRA image.</returns>
        internal byte[] Snapshot() {
            Assert.Single(Canvas.Children);
            var image = Assert.IsType<Image>(Canvas.Children[0]);
            image.UpdateLayout();
            var width = checked((int) Canvas.PixelWidth);
            var height = checked((int) Canvas.PixelHeight);
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(image);
            var pixels = new byte[checked(width * height * 4)];
            bitmap.CopyPixels(pixels, width * 4, 0);
            return pixels;
        }

        /// <summary>Checks every productive milestone advanced by the actual expected number of frames.</summary>
        /// <param name="before">The actual counters before the isolated interval.</param>
        /// <param name="frames">The expected number of scene submissions, completions, copies and publications.</param>
        internal void AssertAdvanced(
            (ulong Submitted, ulong Completed, ulong Copied, ulong Published, ulong Rebound,
                int Updates, int Maintenance, int Rendered, int OnRendered) before,
            int frames
        ) {
            Assert.Equal(before.Submitted + (ulong) frames, Owner.SubmittedFrames);
            Assert.Equal(before.Completed + (ulong) frames, Owner.CompletedFrames);
            Assert.Equal(before.Copied + (ulong) frames, Owner.CopiedFrames);
            Assert.Equal(before.Published + (ulong) frames, Owner.PublishedFrames);
            Assert.Equal(before.Updates + frames, Node.UpdateCount);
            Assert.Equal(before.Maintenance + frames, Node.MaintenanceCount);
            Assert.Equal(before.Rendered + frames, RenderedCount);
            Assert.Equal(before.OnRendered + frames, OnRenderedCount);
        }

        /// <summary>Counts host completions and optionally requests exactly one subsequent scene frame.</summary>
        /// <param name="sender">The actual viewport host.</param>
        /// <param name="args">The actual completion event arguments.</param>
        private void HandleRendered(object? sender, EventArgs args) {
            RenderedCount++;
            if (!InvalidateFromRendered) return;
            InvalidateFromRendered = false;
            Viewport.InvalidateRender();
        }

        /// <summary>Counts viewport completions and optionally requests exactly one subsequent scene frame.</summary>
        /// <param name="sender">The actual passive canvas.</param>
        /// <param name="args">The actual completion event arguments.</param>
        private void HandleOnRendered(object? sender, EventArgs args) {
            OnRenderedCount++;
            if (!InvalidateFromOnRendered) return;
            InvalidateFromOnRendered = false;
            Viewport.InvalidateRender();
        }

        /// <summary>Retries real detachment before releasing borrowed native and application ownership in order.</summary>
        /// <returns>The bounded fixture-only teardown.</returns>
        public async ValueTask DisposeAsync() {
            if (disposed) return;
            Viewport.Rendered -= HandleRendered;
            Viewport.OnRendered -= HandleOnRendered;
            await PollAsync(
                () => {
                    try {
                        Viewport.Dispose();
                        Owner.Dispose();
                        Canvas.Dispose();
                        return true;
                    } catch (TimeoutException) {
                        return false;
                    }
                }, "actual WPF owner detachment and disposal"
            );
            source.RootVisual = null;
            source.Dispose();
            Effects.Dispose();
            disposed = true;
        }
    }

    /// <summary>Uses the ordinary production mesh node with test-only preparation observers.</summary>
    private sealed class ObservedMeshModel : MeshGeometryModel3D {
        /// <summary>Creates an ordinary mesh scene node with counters and one-shot callbacks.</summary>
        /// <returns>The actual rendering node.</returns>
        protected override SceneNode OnCreateSceneNode() => new ObservedMeshNode();
    }

    /// <summary>Retains productive mesh rendering while observing real scene preparation.</summary>
    private sealed class ObservedMeshNode : MeshNode {
        /// <summary>Gets the number of actual scene updates.</summary>
        internal int UpdateCount { get; private set; }

        /// <summary>Gets the number of actual non-render maintenance calls.</summary>
        internal int MaintenanceCount { get; private set; }

        /// <summary>Gets the camera position consumed by the last actual update.</summary>
        internal Vector3 LastCameraPosition { get; private set; }

        /// <summary>Gets or sets the viewport invalidated exactly once from its next actual update.</summary>
        internal Viewport3DX? InvalidateOnNextUpdate { get; set; }

        /// <summary>Gets or sets the viewport stopped once from its next scene update.</summary>
        internal Viewport3DX? StopOnNextUpdate { get; set; }

        /// <summary>Gets or sets the viewport requesting disposal from its next actual update.</summary>
        internal Viewport3DX? DisposeOnNextUpdate { get; set; }

        /// <summary>Gets whether the application node was disposed before its requesting update returned.</summary>
        internal bool WasDisposedInsideUpdate { get; private set; }

        /// <summary>Gets or sets whether to fail the next actual preparation before submission.</summary>
        internal bool ThrowOnNextUpdate { get; set; }

        /// <summary>Runs the ordinary mesh update, then performs a selected one-shot test callback.</summary>
        /// <param name="context">The actual common-renderer context.</param>
        /// <exception cref="InvalidOperationException">The requested one-shot preparation failure occurs.</exception>
        public override void Update(RenderContext context) {
            UpdateCount++;
            LastCameraPosition = context.Camera!.Position;
            base.Update(context);
            if (ThrowOnNextUpdate) {
                ThrowOnNextUpdate = false;
                throw new InvalidOperationException("Injected WPF mesh preparation failure.");
            }

            var invalidate = InvalidateOnNextUpdate;
            InvalidateOnNextUpdate = null;
            invalidate?.InvalidateRender();
            var stop = StopOnNextUpdate;
            StopOnNextUpdate = null;
            stop?.StopRendering();
            var dispose = DisposeOnNextUpdate;
            DisposeOnNextUpdate = null;
            if (dispose is null) return;
            dispose.Dispose();
            WasDisposedInsideUpdate = IsDisposed;
        }

        /// <summary>Counts actual maintenance while retaining the productive implementation.</summary>
        /// <param name="context">The actual common-renderer context.</param>
        public override void UpdateNotRender(RenderContext context) {
            MaintenanceCount++;
            base.UpdateNotRender(context);
        }
    }

    /// <summary>Holds the productive queue at a CPU-signaled native fence without changing owner scheduling.</summary>
    private sealed class NativeQueueGate : IDisposable {
        /// <summary>Owns the genuine gate fence until productive queue work is drained.</summary>
        private readonly SilkD3D12Fence gateFence;

        /// <summary>Borrows the actual presentation queue.</summary>
        private readonly SilkD3D12CommandQueue queue;

        /// <summary>Borrows the actual completion fence for safe gate teardown.</summary>
        private readonly SilkD3D12Fence completionFence;

        /// <summary>Retains the CPU fence-signal entry point.</summary>
        private readonly SignalFence signal;

        /// <summary>Prevents signaling the native gate twice.</summary>
        private bool released;

        /// <summary>Inserts a real ID3D12CommandQueue::Wait before the next productive frame.</summary>
        /// <param name="owner">The initialized actual hardware presentation owner.</param>
        internal NativeQueueGate(D3D12WpfPresentation owner) {
            var device = PrivateField<SilkD3D12Device>(owner, "device");
            queue = PrivateField<SilkD3D12CommandQueue>(owner, "queue");
            completionFence = PrivateField<SilkD3D12Fence>(owner, "fence");
            gateFence = device.CreateFence();
            // Stable COM ABI slots: ID3D12CommandQueue::Wait = 15; ID3D12Fence::Signal = 10.
            var wait = NativeMethod<WaitQueue>(queue.NativePointer, 15);
            signal = NativeMethod<SignalFence>(gateFence.NativePointer, 10);
            Marshal.ThrowExceptionForHR(wait(queue.NativePointer, gateFence.NativePointer, 1));
        }

        /// <summary>Releases the genuine queue gate exactly once from the CPU.</summary>
        internal void Release() {
            if (released) return;
            Marshal.ThrowExceptionForHR(signal(gateFence.NativePointer, 1));
            released = true;
        }

        /// <summary>Unblocks and drains productive queue use before disposing the gate fence.</summary>
        public void Dispose() {
            if (gateFence.IsDisposed) return;
            Release();
            var drain = queue.Signal(completionFence);
            completionFence.Wait(drain, CompletionTimeout);
            gateFence.Dispose();
        }

        /// <summary>Reads a stable native COM method without enabling unsafe code in the test project.</summary>
        /// <typeparam name="T">The exact native entry-point delegate type.</typeparam>
        /// <param name="instance">The actual native COM pointer.</param>
        /// <param name="slot">The stable native vtable slot.</param>
        /// <returns>The native entry-point delegate.</returns>
        private static T NativeMethod<T>(nint instance, int slot) where T : Delegate =>
            Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance), slot * IntPtr.Size));

        /// <summary>Calls the native queue-wait entry point.</summary>
        /// <param name="queue">The actual productive queue pointer.</param>
        /// <param name="fence">The gate fence pointer.</param>
        /// <param name="value">The completion value required to unblock the queue.</param>
        /// <returns>The native HRESULT.</returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int WaitQueue(nint queue, nint fence, ulong value);

        /// <summary>Calls the native CPU fence-signal entry point.</summary>
        /// <param name="fence">The actual gate fence pointer.</param>
        /// <param name="value">The CPU completion value.</param>
        /// <returns>The native HRESULT.</returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SignalFence(nint fence, ulong value);
    }
}