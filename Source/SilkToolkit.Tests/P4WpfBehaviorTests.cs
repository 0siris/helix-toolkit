using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using HelixToolkit.Wpf.SharpDX.Elements2D;
using Xunit;
using Color = System.Windows.Media.Color;
using MeshGeometry3D = HelixToolkit.SharpDX.Core.MeshGeometry3D;
using OrthographicCamera = HelixToolkit.Wpf.SharpDX.OrthographicCamera;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

namespace SilkToolkit.Tests;

/// <summary>Checks Phase 4 public WPF behavior, separating managed contracts from actual hardware evidence.</summary>
/// <param name="output">Receives measured native counters and pixel values.</param>
[Collection(WpfCollection.Name)]
public sealed class P4WpfBehaviorTests(ITestOutputHelper output) {
    /// <summary>Loads public backend/DPI attributes through the original XAML namespace before native initialization.</summary>
    /// <param name="attributes">The public attributes under test.</param>
    /// <param name="swapChain">The expected selected backend.</param>
    /// <param name="dpi">The expected resolution policy.</param>
    /// <returns>The managed STA verification without native device creation.</returns>
    [Theory]
    [InlineData("", false, true)]
    [InlineData("EnableSwapChainRendering='False' EnableDpiScale='False'", false, false)]
    [InlineData("EnableSwapChainRendering='True' EnableDpiScale='True'", true, true)]
    public Task PublicXamlSelectsBackendBeforeInitialization(string attributes, bool swapChain, bool dpi) =>
        StaThread.RunAsync(() => {
            using var viewport = (Viewport3DX) XamlReader.Parse(
                "<h:Viewport3DX xmlns:h='http://helix-toolkit.org/wpf/SharpDX' " + attributes + " />");
            Assert.Equal(swapChain, viewport.EnableSwapChainRendering);
            Assert.Equal(dpi, viewport.EnableDpiScale);
            viewport.Template = P4WpfHarness.LoadTemplate();
            viewport.ApplyTemplate();
            Assert.Equal(swapChain, viewport.D3D12Surface is not null);
            Assert.Null(P4WpfHarness.Field<bool?>(viewport, "selectedSwapChainRendering"));
            viewport.EnableSwapChainRendering = !swapChain;
            Assert.Equal(!swapChain, viewport.EnableSwapChainRendering);
        });

    /// <summary>Preserves derived WPF preview/bubble overrides and suppresses promoted mouse selection during touch.</summary>
    /// <returns>The managed input check; no synthetic native success is used.</returns>
    [Fact]
    public Task DerivedViewportReceivesOneRouteAndTouchSelectionIsNotPromotedTwice() => StaThread.RunAsync(() => {
        using var viewport = new InputViewport();
        var selections = 0;
        viewport.MouseDown3D += (_, _) => selections++;
        var device = new TestTouchDevice();
        viewport.DeliverTouchDown(new TouchEventArgs(device, 1));
        Assert.Equal(1, selections);
        Assert.Same(device, P4WpfHarness.Field<TouchDevice>(viewport, "touchDownDevice"));
        var promoted = new MouseButtonEventArgs(Mouse.PrimaryDevice, 2, MouseButton.Left) {
            RoutedEvent = Mouse.PreviewMouseDownEvent
        };
        viewport.RaiseEvent(promoted);
        promoted.RoutedEvent = Mouse.MouseDownEvent;
        viewport.RaiseEvent(promoted);
        Assert.Equal(1, viewport.PreviewCount);
        Assert.Equal(1, viewport.BubbleCount);
        Assert.Equal(1, selections);
        viewport.DeliverTouchUp(new TouchEventArgs(device, 3));
        Assert.Null(P4WpfHarness.Field<TouchDevice?>(viewport, "touchDownDevice"));
        viewport.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 4, MouseButton.Left) {
            RoutedEvent = Mouse.PreviewMouseDownEvent
        });
        Assert.Equal(2, selections);
    });

    /// <summary>Completes a real managed touch selection when WPF finishes manipulation without delivering TouchUp.</summary>
    /// <returns>The managed missing-release regression, including the following independent contact.</returns>
    [Fact]
    public Task ManipulationCompletionCleansMissingTouchUpAndAllowsTheNextContact() => StaThread.RunAsync(() => {
        using var viewport = new InputViewport();
        var downs = 0;
        var ups = 0;
        MouseUp3DEventArgs? released = null;
        viewport.MouseDown3D += (_, _) => downs++;
        viewport.MouseUp3D += (_, args) => { ups++; released = (MouseUp3DEventArgs) args; };
        viewport.DeliverTouchDown(new TouchEventArgs(new TestTouchDevice(), 1));
        // WPF exposes no public constructor; instantiate its ordinary event arguments rather than altering viewport state.
        var completed = (ManipulationCompletedEventArgs) Activator.CreateInstance(
            typeof(ManipulationCompletedEventArgs), BindingFlags.Instance | BindingFlags.NonPublic, null,
            [null, 2, viewport, new Point(12, 14),
                new ManipulationDelta(new Vector(3, 4), 0, new Vector(1, 1), new Vector()),
                new ManipulationVelocities(new Vector(), 0, new Vector()), false], null)!;
        viewport.DeliverManipulationCompleted(completed);
        Assert.Equal(1, ups);
        Assert.NotNull(released);
        Assert.Equal(new Point(15, 18), released.Position);
        Assert.Same(completed, released.OriginalInputEventArgs);
        Assert.Null(P4WpfHarness.Field<TouchDevice?>(viewport, "touchDownDevice"));
        var next = new TestTouchDevice();
        viewport.DeliverTouchDown(new TouchEventArgs(next, 3));
        Assert.Equal(2, downs);
        viewport.DeliverTouchUp(new TouchEventArgs(next, 4));
        Assert.Equal(2, ups);
    });

    /// <summary>Verifies original target/zoom adorners retain DIP geometry through DPI and layout changes and clean up.</summary>
    /// <returns>The managed real-AdornerLayer verification without GPU initialization.</returns>
    [Fact]
    public Task GestureAdornersKeepDipPositionsAndAreRemovedAtGestureEnd() => StaThread.RunAsync(() => {
        using var viewport = P4WpfHarness.CreateViewport();
        viewport.ShowCameraTarget = true;
        var decorator = new AdornerDecorator { Child = viewport };
        decorator.Measure(new Size(320, 240));
        decorator.Arrange(new Rect(0, 0, 320, 240));
        decorator.UpdateLayout();
        viewport.ApplyTemplate();
        var presenter = (UIElement) viewport.Template.FindName("PART_Canvas", viewport);
        var layer = Assert.IsType<AdornerLayer>(AdornerLayer.GetAdornerLayer(presenter));
        var point = new Point(40, 30);
        var rect = new Rect(20, 10, 80, 60);
        viewport.ShowTargetAdorner(point);
        viewport.ShowZoomRectangle(rect);
        var adorners = layer.GetAdorners(presenter)!;
        Assert.Equal(2, adorners.Length);
        Assert.Equal(point, Assert.Single(adorners.OfType<TargetSymbolAdorner>()).Position);
        Assert.Equal(rect, Assert.Single(adorners.OfType<RectangleAdorner>()).Rectangle);
        viewport.DpiScale = 2;
        viewport.EnableDpiScale = false;
        decorator.Arrange(new Rect(0, 0, 400, 300));
        decorator.UpdateLayout();
        Assert.Equal(point, Assert.Single(layer.GetAdorners(presenter)!.OfType<TargetSymbolAdorner>()).Position);
        var changed = new Rect(30, 25, 100, 70);
        viewport.ShowZoomRectangle(changed);
        Assert.Equal(2, layer.GetAdorners(presenter)!.Length);
        Assert.Equal(changed, Assert.Single(layer.GetAdorners(presenter)!.OfType<RectangleAdorner>()).Rectangle);
        viewport.HideTargetAdorner();
        viewport.HideZoomRectangle();
        Assert.True(layer.GetAdorners(presenter) is not { Length: > 0 });
        decorator.Child = null;
    });

    /// <summary>Warms actual managed property paths and measures bounded demand/status overhead without assertions inside.</summary>
    /// <returns>The managed allocation interval and positive allocation control.</returns>
    [Fact]
    public Task WarmImageAliasStatusAndDemandPathsHaveBoundedAllocation() => StaThread.RunAsync(() => {
        using var viewport = new Viewport3DX { MinimumUpdateCount = 0 };
        for (var index = 0; index < 256; index++) ExerciseManagedChanges(viewport, index);
        const int iterations = 1024;
        var start = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < iterations; index++) ExerciseManagedChanges(viewport, index);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - start;
        var controlStart = GC.GetAllocatedBytesForCurrentThread();
        var control = new byte[1024];
        var controlBytes = GC.GetAllocatedBytesForCurrentThread() - controlStart;
        GC.KeepAlive(control);
        output.WriteLine($"Warm image alias/status/demand: {allocated} bytes / {iterations}; control={controlBytes}.");
        // WPF SetValue boxes enum values and allocates its effective-value notifications; measure a bounded cost.
        Assert.InRange(allocated, 0L, 256L * iterations);
        Assert.True(controlBytes >= 1024);
        Assert.False(viewport.RenderDemand.HasRenderDemand);
    });

    /// <summary>Leaves backend selection unlocked after an actual device-creation attempt throws.</summary>
    /// <returns>The explicit native-boundary test, which deliberately fails before a GPU device is created.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task FailedNativeInitializationDoesNotLatchSelection() => StaThread.RunAsync(async () => {
        await using var harness = new P4WpfHarness((SilkDriverType) int.MaxValue);
        await harness.LoadAsync();
        var owner = harness.Owner;
        Assert.Throws<ArgumentOutOfRangeException>(() => owner.RenderFrame(P4WpfHarness.FrameTime));
        Assert.False(owner.IsInitialized);
        Assert.Equal(0UL, owner.Generation);
        Assert.Equal(0UL, owner.SubmittedFrames);
        Assert.Null(P4WpfHarness.Field<bool?>(harness.Viewport, "selectedSwapChainRendering"));
        harness.Root.Children.Remove(harness.Viewport);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        harness.Viewport.EnableSwapChainRendering = true;
        Assert.True(owner.IsDisposed);
        Assert.True(harness.Viewport.EnableSwapChainRendering);
        Assert.NotNull(harness.Viewport.D3D12Surface);
    });

    /// <summary>Uses real successful initialization, all DP routes, a new template and actual visual-tree reload.</summary>
    /// <returns>The explicit hardware lifecycle verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task ActualNativeSuccessLocksBackendAcrossRetemplateAndReload() => StaThread.RunAsync(async () => {
        await using var harness = new P4WpfHarness();
        await harness.WarmAsync();
        var viewport = harness.Viewport;
        var original = harness.Owner;
        Assert.True(original.Generation > 0);
        Assert.True(original.PublishedFrames > 0);
        AssertLocked(viewport, original);
        viewport.Template = null;
        viewport.Template = P4WpfHarness.LoadTemplate();
        viewport.ApplyTemplate();
        harness.Root.UpdateLayout();
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Assert.True(original.IsDisposed);
        Assert.NotSame(original, harness.Owner);
        await harness.DrainAsync();
        Assert.True(harness.Owner.PublishedFrames > 0);
        AssertLocked(viewport, harness.Owner);
        var renderingCallbacks = CallbackCount(harness.Canvas, "Rendering");
        var presentationCallbacks = CallbackCount(harness.Canvas, "PresentationChanged");
        for (var reload = 0; reload < 2; reload++) {
            await harness.ReloadAsync();
            Assert.Equal(renderingCallbacks, CallbackCount(harness.Canvas, "Rendering"));
            Assert.Equal(presentationCallbacks, CallbackCount(harness.Canvas, "PresentationChanged"));
            Assert.True(P4WpfHarness.Field<bool>(harness.Canvas, "renderingSubscribed"));
            Assert.True(P4WpfHarness.Field<bool>(harness.Canvas, "sourceSubscribed"));
        }
        AssertLocked(viewport, harness.Owner);
        output.WriteLine($"Real latch: generation={harness.Owner.Generation}, published={harness.Owner.PublishedFrames}.");
    });

    /// <summary>Counts actual accepted scene submissions for historical budgets and a subsequent runtime change.</summary>
    /// <param name="requested">The requested public frame budget.</param>
    /// <param name="expected">The total accepted frames for one isolated invalidation.</param>
    /// <returns>The explicit hardware frame-count verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(6, 6)]
    [InlineData(1, 1)]
    [InlineData(0, 1)]
    [InlineData(-4, 1)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task PublicMinimumUpdateCountControlsActualFramesAndRuntimeChanges(int requested, int expected) =>
        StaThread.RunAsync(async () => {
            await using var harness = new P4WpfHarness();
            Assert.Equal(6, harness.Viewport.MinimumUpdateCount);
            await harness.WarmAsync();
            harness.Viewport.MinimumUpdateCount = requested;
            Assert.Equal(Math.Max(0, requested), harness.Viewport.MinimumUpdateCount);
            var before = harness.Owner.SubmittedFrames;
            harness.Viewport.InvalidateRender();
            await harness.DrainAsync();
            Assert.Equal(before + (ulong) expected, harness.Owner.SubmittedFrames);
            harness.Viewport.MinimumUpdateCount = 3;
            Assert.False(harness.Viewport.RenderDemand.HasRenderDemand);
            before = harness.Owner.SubmittedFrames;
            harness.Viewport.InvalidateRender();
            await harness.DrainAsync();
            Assert.Equal(before + 3, harness.Owner.SubmittedFrames);
            harness.AssertIdle();
            output.WriteLine($"Budget={requested}, actual={expected}, runtime budget=3; {harness.Counters}.");
        });

    /// <summary>Checks real capture dimensions at 200 percent and identical scene picking in DIP coordinates.</summary>
    /// <returns>The explicit hardware DPI/picking regression.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task PhysicalRenderResolutionChangesWhileDipPickingRemainsStable() => StaThread.RunAsync(async () => {
        await using var harness = new P4WpfHarness();
        harness.Viewport.DpiScale = 2;
        harness.Viewport.EnableDpiScale = true;
        await harness.WarmAsync();
        var point = new Point(80, 60);
        var first = harness.Viewport.FindNearestPoint(point);
        Assert.NotNull(first);
        var scaled = harness.Owner.CaptureFrame();
        Assert.Equal(320U, scaled.Width);
        Assert.Equal(240U, scaled.Height);
        harness.Viewport.EnableDpiScale = false;
        await harness.DrainAsync();
        var unscaled = harness.Owner.CaptureFrame();
        await harness.DrainAsync();
        Assert.Equal(160U, unscaled.Width);
        Assert.Equal(120U, unscaled.Height);
        Assert.Equal(160d, harness.Viewport.ActualWidth);
        Assert.Equal(120d, harness.Viewport.ActualHeight);
        var second = harness.Viewport.FindNearestPoint(point);
        Assert.NotNull(second);
        Assert.Equal(first.Value.X, second.Value.X, 5);
        Assert.Equal(first.Value.Y, second.Value.Y, 5);
        Assert.Equal(first.Value.Z, second.Value.Z, 5);
        output.WriteLine($"DPI=2: {scaled.Width}x{scaled.Height}; disabled: {unscaled.Width}x{unscaled.Height}; pick={second}.");
    });

    /// <summary>Checks Scene2D pixels and capture while disabled, reenabled, reloaded and changed after becoming idle.</summary>
    /// <returns>The explicit actual-output and restored-notification verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task Scene2DSelectionControlsPixelsCaptureAndRestoredIdleSubscriptions() => StaThread.RunAsync(async () => {
        await using var harness = new P4WpfHarness();
        using var rectangle = new RectangleModel2D {
            Width = 24, Height = 24, HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top, Fill = Brushes.Blue, StrokeThickness = 0,
            EnableBitmapCache = false
        };
        harness.Viewport.Content2D = rectangle;
        await harness.WarmAsync();
        P4WpfHarness.AssertPixel(harness.SnapshotCanvas(), 160, 8, 8, Colors.Blue);
        harness.Viewport.EnableD2DRendering = false;
        await harness.DrainAsync();
        P4WpfHarness.AssertPixel(harness.SnapshotCanvas(), 160, 8, 8, Colors.Black);
        var disabled = harness.Owner.CaptureFrame();
        await harness.DrainAsync();
        P4WpfHarness.AssertPixel(disabled.Pixels, disabled.Width, 8, 8, Colors.Black);
        harness.Viewport.EnableD2DRendering = true;
        await harness.DrainAsync();
        var enabled = harness.Owner.CaptureFrame();
        await harness.DrainAsync();
        P4WpfHarness.AssertPixel(enabled.Pixels, enabled.Width, 8, 8, Colors.Blue);
        await harness.ReloadAsync();
        harness.AssertIdle();
        var before = harness.Owner.SubmittedFrames;
        rectangle.Fill = Brushes.Lime;
        rectangle.SceneNode.InvalidateRender();
        Assert.True(harness.Viewport.RenderDemand.HasRenderDemand);
        await harness.DrainAsync();
        Assert.True(harness.Owner.SubmittedFrames > before);
        P4WpfHarness.AssertPixel(harness.SnapshotCanvas(), 160, 8, 8, Colors.Lime);
        harness.AssertIdle();
        output.WriteLine($"Scene2D off/on capture and reload dirty notification: {harness.Counters}.");
    });

    /// <summary>Checks an actual Scene2D hit consumes input before scene picking even when scene selection is disabled.</summary>
    /// <returns>The explicit hardware-backed overlay hit-test verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task Scene2DHitConsumesInputBeforeScenePickingAndCamera() => StaThread.RunAsync(async () => {
        await using var harness = new P4WpfHarness();
        using var rectangle = new RectangleModel2D {
            Width = 160, Height = 120, Fill = Brushes.Blue, StrokeThickness = 0, EnableBitmapCache = false
        };
        harness.Viewport.Content2D = rectangle;
        await harness.WarmAsync();
        var hits2D = 0;
        var hits3D = 0;
        rectangle.MouseDown2D += (_, _) => hits2D++;
        harness.Viewport.MouseDown3D += (_, _) => hits3D++;
        var position = harness.Viewport.Camera.Position;
        var look = harness.Viewport.Camera.LookDirection;
        foreach (var scenePicking in new[] { true, false }) {
            harness.Viewport.EnableMouseButtonHitTest = scenePicking;
            var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right);
            typeof(Viewport3DX).GetMethod("MouseDownHitTest", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(harness.Viewport, [new Point(80, 60), args]);
            Assert.True(args.Handled);
        }
        Assert.Equal(2, hits2D);
        Assert.Equal(0, hits3D);
        Assert.Equal(position, harness.Viewport.Camera.Position);
        Assert.Equal(look, harness.Viewport.Camera.LookDirection);
        output.WriteLine($"Actual Scene2D hits={hits2D}; scene hits={hits3D}; consumed before camera routing.");
    });

    /// <summary>Requires the real centered view cube to win over scene geometry after the frontmost 2D overlay is removed.</summary>
    /// <returns>The explicit hardware-backed three-level picking-priority verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task Scene2DThenViewCubeThenScenePickingRetainsPriority() => StaThread.RunAsync(async () => {
        await using var harness = new P4WpfHarness();
        var viewport = harness.Viewport;
        viewport.ShowViewCube = true;
        viewport.ViewCubeHorizontalPosition = 0;
        viewport.ViewCubeVerticalPosition = 0;
        viewport.ViewCubeSize = 0.4;
        using var overlay = new RectangleModel2D {
            Width = 160, Height = 120, Fill = Brushes.Blue, StrokeThickness = 0, EnableBitmapCache = false
        };
        viewport.Content2D = overlay;
        await harness.WarmAsync();
        var cube = (ViewBoxModel3D) viewport.Template.FindName("PART_ViewCube", viewport);
        var overlayHits = 0;
        var cubeHits = 0;
        var sceneHits = 0;
        overlay.MouseDown2D += (_, _) => overlayHits++;
        cube.MouseDown3D += (_, _) => cubeHits++;
        viewport.MouseDown3D += (_, args) => {
            if (((MouseDown3DEventArgs) args).HitTestResult?.ModelHit is MeshGeometryModel3D) sceneHits++;
        };
        var down = typeof(Viewport3DX).GetMethod("MouseDownHitTest", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var point = new Point(80, 60);
        viewport.EnableMouseButtonHitTest = false;
        var input = new MouseButtonEventArgs(Mouse.PrimaryDevice, 1, MouseButton.Left);
        down.Invoke(viewport, [point, input]);
        Assert.True(input.Handled);
        Assert.Equal(1, overlayHits);
        Assert.Equal(0, cubeHits);
        Assert.Equal(0, sceneHits);
        viewport.Content2D = null;
        await harness.DrainAsync();
        input = new MouseButtonEventArgs(Mouse.PrimaryDevice, 2, MouseButton.Left);
        down.Invoke(viewport, [point, input]);
        Assert.True(input.Handled);
        Assert.Equal(1, cubeHits);
        Assert.Equal(0, sceneHits);
        viewport.ShowViewCube = false;
        viewport.EnableMouseButtonHitTest = true;
        await harness.DrainAsync();
        input = new MouseButtonEventArgs(Mouse.PrimaryDevice, 3, MouseButton.Left);
        down.Invoke(viewport, [point, input]);
        Assert.Equal(1, overlayHits);
        Assert.Equal(1, cubeHits);
        Assert.Equal(1, sceneHits);
        viewport.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 4, MouseButton.Left) {
            RoutedEvent = Mouse.PreviewMouseUpEvent
        });
        output.WriteLine($"Actual picking priority counts: 2D={overlayHits}, cube={cubeHits}, scene={sceneHits}.");
    });

    /// <summary>Requires status changes to preserve actual idle counters and image aliases to submit bounded new work.</summary>
    /// <returns>The explicit hardware idle/property regression.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task ImageAliasesWakeActualRenderingAndStatusUpdatesRemainIdle() => StaThread.RunAsync(async () => {
        await using var harness = new P4WpfHarness();
        await harness.WarmAsync();
        harness.Viewport.MinimumUpdateCount = 1;
        var before = harness.Owner.SubmittedFrames;
        harness.Viewport.FrameRate = 60;
        harness.Viewport.FrameRateText = "60 FPS";
        harness.Viewport.MessageText = "Idle status";
        harness.Viewport.FieldOfViewText = "45";
        harness.AssertIdle();
        Assert.Equal(before, harness.Owner.SubmittedFrames);
        harness.Viewport.FXAALevel = FXAALevel.High;
        await harness.DrainAsync();
        Assert.Equal(before + 1, harness.Owner.SubmittedFrames);
        before = harness.Owner.SubmittedFrames;
        harness.Viewport.EnableSSAO = true;
        await harness.DrainAsync();
        Assert.Equal(before + 1, harness.Owner.SubmittedFrames);
        before = harness.Owner.SubmittedFrames;
        harness.Viewport.BackgroundColor = Colors.Navy;
        await harness.DrainAsync();
        Assert.Equal(before + 1, harness.Owner.SubmittedFrames);
        P4WpfHarness.AssertPixel(harness.SnapshotCanvas(), 160, 8, 8, Colors.Navy);
        harness.AssertIdle();
        output.WriteLine($"Alias/image wake and status idle: {harness.Counters}.");
    });

    /// <summary>Changes real material, geometry, camera and light state after each idle drain and checks changed pixels.</summary>
    /// <returns>The explicit hardware image-invalidation regression through public scene APIs.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task MaterialGeometryCameraAndLightChangesWakeTheIdleProductionViewport() => StaThread.RunAsync(async () => {
        await using var harness = new P4WpfHarness();
        await harness.WarmAsync();
        harness.Viewport.MinimumUpdateCount = 1;
        var mesh = Assert.Single(harness.Viewport.Items.OfType<MeshGeometryModel3D>());
        var material = Assert.IsType<HelixToolkit.Wpf.SharpDX.DiffuseMaterial>(mesh.Material);
        material.DiffuseColor = new Silk.NET.Maths.Vector4D<float>(0, 0, 1, 1);
        Assert.True(harness.Viewport.RenderDemand.HasRenderDemand);
        await harness.DrainAsync();
        P4WpfHarness.AssertPixel(harness.SnapshotCanvas(), 160, 80, 60, Colors.Blue);
        harness.AssertIdle();

        var geometry = mesh.Geometry;
        mesh.Geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([new(3, 1, 0), new(5, 1, 0), new(4, -1, 0)]),
            Normals = new Vector3Collection([Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ]),
            Indices = new IntCollection([0, 2, 1])
        };
        Assert.True(harness.Viewport.RenderDemand.HasRenderDemand);
        await harness.DrainAsync();
        P4WpfHarness.AssertPixel(harness.SnapshotCanvas(), 160, 80, 60, Colors.Black);
        harness.AssertIdle();
        mesh.Geometry = geometry;
        await harness.DrainAsync();

        harness.Viewport.Camera.Position = new Point3D(3, 0, 5);
        Assert.True(harness.Viewport.RenderDemand.HasRenderDemand);
        await harness.DrainAsync();
        P4WpfHarness.AssertPixel(harness.SnapshotCanvas(), 160, 80, 60, Colors.Black);
        harness.AssertIdle();
        harness.Viewport.Camera.Position = new Point3D(0, 0, 5);
        await harness.DrainAsync();

        material.EnableUnLit = false;
        harness.Viewport.Items.Add(new AmbientLight3D { Color = Colors.White });
        await harness.DrainAsync();
        P4WpfHarness.AssertPixel(harness.SnapshotCanvas(), 160, 80, 60, Colors.Blue);
        var light = Assert.Single(harness.Viewport.Items.OfType<AmbientLight3D>());
        light.Color = Colors.Black;
        Assert.True(harness.Viewport.RenderDemand.HasRenderDemand);
        await harness.DrainAsync();
        P4WpfHarness.AssertPixel(harness.SnapshotCanvas(), 160, 80, 60, Colors.Black);
        harness.AssertIdle();
        output.WriteLine($"Real material/geometry/camera/light changes altered captured pixels; {harness.Counters}.");
    });

    /// <summary>Changes cached WPF enum/string values and consumes only managed demand to measure callback overhead.</summary>
    /// <param name="viewport">The uninitialized viewport whose property callbacks are real.</param>
    /// <param name="iteration">Alternates two existing property values.</param>
    private static void ExerciseManagedChanges(Viewport3DX viewport, int iteration) {
        viewport.FXAALevel = (iteration & 1) == 0 ? FXAALevel.High : FXAALevel.None;
        viewport.FrameRateText = (iteration & 1) == 0 ? "60 FPS" : "30 FPS";
        while (viewport.RenderDemand.TryBeginFrame(out _)) viewport.RenderDemand.CompleteFrame(true);
    }

    /// <summary>Counts retained callback registrations without replacing or directly invoking them.</summary>
    /// <param name="canvas">The real loaded passive canvas.</param>
    /// <param name="name">The backing event field.</param>
    /// <returns>The number of actual registered callbacks.</returns>
    private static int CallbackCount(DPFCanvas canvas, string name) =>
        P4WpfHarness.Field<Delegate?>(canvas, name)?.GetInvocationList().Length ?? 0;

    /// <summary>Asserts the effective DP value and native owner stay stable for every supported value source.</summary>
    /// <param name="viewport">The genuinely initialized viewport.</param>
    /// <param name="owner">The owner whose real native generation must survive coercion.</param>
    private static void AssertLocked(Viewport3DX viewport, D3D12WpfPresentation owner) {
        Assert.Throws<InvalidOperationException>(() => viewport.EnableSwapChainRendering = true);
        viewport.SetValue(Viewport3DX.EnableSwapChainRenderingProperty, true);
        Assert.False(viewport.EnableSwapChainRendering);
        viewport.SetCurrentValue(Viewport3DX.EnableSwapChainRenderingProperty, true);
        Assert.False(viewport.EnableSwapChainRendering);
        BindingOperations.SetBinding(viewport, Viewport3DX.EnableSwapChainRenderingProperty, new Binding { Source = true });
        Assert.False(viewport.EnableSwapChainRendering);
        viewport.ClearValue(Viewport3DX.EnableSwapChainRenderingProperty);
        Assert.False(viewport.EnableSwapChainRendering);
        Assert.Same(owner, P4WpfHarness.OwnerOf(viewport));
        Assert.False(owner.IsDisposed);
        Assert.True(owner.IsInitialized);
    }

    /// <summary>Observes the actual overridable WPF route and exposes touch delivery without native device simulation.</summary>
    private sealed class InputViewport : Viewport3DX {
        /// <summary>Gets the preview override count.</summary>
        internal int PreviewCount { get; private set; }
        /// <summary>Gets the bubble override count.</summary>
        internal int BubbleCount { get; private set; }
        /// <summary>Delivers one touch event to the productive selection route.</summary>
        /// <param name="args">The test contact.</param>
        internal void DeliverTouchDown(TouchEventArgs args) => OnTouchDown(args);
        /// <summary>Delivers the matching contact release.</summary>
        /// <param name="args">The released contact.</param>
        internal void DeliverTouchUp(TouchEventArgs args) => OnTouchUp(args);
        /// <summary>Delivers the framework's completion event to the actual manipulation cleanup route.</summary>
        /// <param name="args">The completed manipulation whose contact never released.</param>
        internal void DeliverManipulationCompleted(ManipulationCompletedEventArgs args) => OnManipulationCompleted(args);
        /// <inheritdoc />
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e) {
            PreviewCount++;
            base.OnPreviewMouseDown(e);
        }
        /// <inheritdoc />
        protected override void OnMouseDown(MouseButtonEventArgs e) {
            BubbleCount++;
            base.OnMouseDown(e);
        }
    }

    /// <summary>Supplies one managed contact for the WPF touch-versus-promoted-mouse selection guard.</summary>
    private sealed class TestTouchDevice() : TouchDevice(1) {
        /// <inheritdoc />
        public override TouchPoint GetTouchPoint(IInputElement? relativeTo) =>
            new(this, new Point(12, 14), new Rect(12, 14, 1, 1), TouchAction.Down);
        /// <inheritdoc />
        public override TouchPointCollection GetIntermediateTouchPoints(IInputElement? relativeTo) =>
            new() { GetTouchPoint(relativeTo) };
    }
}

/// <summary>Reuses the P3 hidden-source pattern with the shipped template and public Viewport wiring.</summary>
internal sealed class P4WpfHarness : IAsyncDisposable {
    /// <summary>Provides a fixed camera timestamp so camera integration cannot introduce timed motion.</summary>
    internal static readonly TimeSpan FrameTime = TimeSpan.FromSeconds(1);
    /// <summary>Owns the never-shown WPF HWND; null when an explicit visible Window owns the visual tree.</summary>
    private readonly HwndSource? source;

    /// <summary>Creates a template-backed viewport and an optional hidden presentation source.</summary>
    /// <param name="driverType">The actual device policy, including an explicit invalid policy for failure testing.</param>
    /// <param name="hiddenSource">Whether this fixture owns the hidden HWND.</param>
    internal P4WpfHarness(SilkDriverType driverType = SilkDriverType.Hardware, bool hiddenSource = true) {
        Viewport = CreateViewport();
        Viewport.EffectsManager = Effects;
        Viewport.D3D12DriverType = driverType;
        Viewport.Width = 160;
        Viewport.Height = 120;
        Viewport.EnableDpiScale = false;
        Viewport.ShowViewCube = false;
        Viewport.ShowCoordinateSystem = false;
        Viewport.BackgroundColor = Colors.Black;
        Viewport.BelongsToParentWindow = false;
        Viewport.Camera = new OrthographicCamera {
            Position = new Point3D(0, 0, 5), LookDirection = new Vector3D(0, 0, -5),
            UpDirection = new Vector3D(0, 1, 0), Width = 4
        };
        Viewport.Items.Add(new MeshGeometryModel3D {
            Geometry = new MeshGeometry3D {
                Positions = new Vector3Collection([new(-1, 1, 0), new(1, 1, 0), new(0, -1, 0)]),
                Normals = new Vector3Collection([Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ]),
                Indices = new IntCollection([0, 2, 1])
            },
            Material = new HelixToolkit.Wpf.SharpDX.DiffuseMaterial {
                DiffuseColor = new Silk.NET.Maths.Vector4D<float>(1, 0, 0, 1), EnableUnLit = true
            }
        });
        Viewport.StopRendering();
        Root.Children.Add(Viewport);
        if (hiddenSource) {
            source = new HwndSource(new HwndSourceParameters(nameof(P4WpfHarness)) {
                Width = 400, Height = 300, WindowStyle = unchecked((int) 0x80000000)
            });
            source.RootVisual = Root;
        }
        Viewport.ApplyTemplate();
        Root.UpdateLayout();
    }

    /// <summary>Gets the real root, also used for actual unload/reload and visible acceptance.</summary>
    internal Grid Root { get; } = new();
    /// <summary>Gets the externally owned manager borrowed by this viewport.</summary>
    internal DefaultEffectsManager Effects { get; } = new();
    /// <summary>Gets the public production viewport.</summary>
    internal Viewport3DX Viewport { get; }
    /// <summary>Gets the current productive presentation owner; reevaluated after template replacement.</summary>
    internal D3D12WpfPresentation Owner => OwnerOf(Viewport);
    /// <summary>Gets the passive canvas selected by the actual public template.</summary>
    internal DPFCanvas Canvas => Owner.Canvas;
    /// <summary>Gets real counters for failure diagnostics without representing them as GPU proof on their own.</summary>
    internal string Counters => $"submitted={Owner.SubmittedFrames}, completed={Owner.CompletedFrames}, " +
        $"copied={Owner.CopiedFrames}, published={Owner.PublishedFrames}, generation={Owner.Generation}";

    /// <summary>Loads and lays out WPF without invoking any explicit device initialization.</summary>
    /// <returns>The dispatcher-owned load completion.</returns>
    internal async Task LoadAsync() {
        Root.UpdateLayout();
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Root.UpdateLayout();
        Assert.True(Canvas.IsActive);
    }

    /// <summary>Initializes the actual hardware owner and drains initial attachment demand.</summary>
    /// <returns>The bounded actual transport completion.</returns>
    internal async Task WarmAsync() {
        await LoadAsync();
        await DrainAsync();
        Assert.True(Owner.IsInitialized);
        Assert.NotEqual(nint.Zero, Canvas.ImageSource.BoundSurface);
        Assert.True(Owner.PublishedFrames > 0);
    }

    /// <summary>Explicitly drives accepted scene/GPU/copy/publication work with a five-second deadline.</summary>
    /// <returns>The test-only asynchronous drain.</returns>
    internal async Task DrainAsync() {
        var start = Stopwatch.GetTimestamp();
        do {
            _ = Owner.RenderFrame(FrameTime, waitForCompletion: true);
            var demand = Viewport.RenderDemand;
            if (!Owner.HasPendingTransport && !demand.HasRenderDemand &&
                !demand.HasPublicationDemand && !demand.HasRecoveryDemand) return;
            if (Stopwatch.GetElapsedTime(start) >= TimeSpan.FromSeconds(5))
                throw new TimeoutException($"Actual WPF work did not settle: {Counters}.");
            await Task.Delay(10);
        } while (true);
    }

    /// <summary>Checks many real idle polls preserve all productive milestones.</summary>
    internal void AssertIdle() {
        Assert.False(Viewport.RenderDemand.HasRenderDemand);
        var before = (Owner.SubmittedFrames, Owner.CompletedFrames, Owner.CopiedFrames, Owner.PublishedFrames);
        for (var index = 0; index < 32; index++) Assert.Equal(0, Owner.RenderFrame(FrameTime));
        Assert.Equal(before, (Owner.SubmittedFrames, Owner.CompletedFrames, Owner.CopiedFrames, Owner.PublishedFrames));
    }

    /// <summary>Unloads and reloads through the real visual tree and restores a clean native presentation.</summary>
    /// <returns>The bounded lifecycle and transport work.</returns>
    internal async Task ReloadAsync() {
        Root.Children.Remove(Viewport);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Assert.False(Canvas.IsActive);
        Root.Children.Add(Viewport);
        Root.UpdateLayout();
        await LoadAsync();
        await DrainAsync();
        Assert.NotEqual(nint.Zero, Canvas.ImageSource.BoundSurface);
    }

    /// <summary>Reads the real bound image at one pixel per DIP; CPU pixels never enter the presentation path.</summary>
    /// <returns>The premultiplied BGRA snapshot.</returns>
    internal byte[] SnapshotCanvas() => Snapshot(Canvas, 160, 120);

    /// <summary>Captures the actual WPF visual with a diagnostic RenderTargetBitmap.</summary>
    /// <param name="visual">The already composed visual.</param>
    /// <param name="width">The diagnostic pixel width.</param>
    /// <param name="height">The diagnostic pixel height.</param>
    /// <returns>The premultiplied BGRA pixels.</returns>
    internal static byte[] Snapshot(Visual visual, int width, int height) {
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var pixels = new byte[checked(width * height * 4)];
        bitmap.CopyPixels(pixels, width * 4, 0);
        return pixels;
    }

    /// <summary>Checks one diagnostic pixel with a small native conversion tolerance.</summary>
    /// <param name="pixels">Actual captured pixels.</param>
    /// <param name="width">The row width.</param>
    /// <param name="x">The column.</param>
    /// <param name="y">The row.</param>
    /// <param name="expected">The expected opaque output.</param>
    internal static void AssertPixel(byte[] pixels, uint width, int x, int y, Color expected) {
        var offset = checked((y * (int) width + x) * 4);
        Assert.InRange(Math.Abs(pixels[offset] - expected.B), 0, 8);
        Assert.InRange(Math.Abs(pixels[offset + 1] - expected.G), 0, 8);
        Assert.InRange(Math.Abs(pixels[offset + 2] - expected.R), 0, 8);
        Assert.InRange(Math.Abs(pixels[offset + 3] - expected.A), 0, 8);
    }

    /// <summary>Loads the shipped template with no replacement canvas or renderer.</summary>
    /// <returns>The production template.</returns>
    internal static ControlTemplate LoadTemplate() {
        var theme = new ResourceDictionary { Source = new Uri("/SilkToolkit;component/Themes/Generic.xaml", UriKind.Relative) };
        return (ControlTemplate) ((Style) theme[typeof(Viewport3DX)]).Setters.OfType<Setter>()
            .Single(setter => setter.Property == Control.TemplateProperty).Value;
    }

    /// <summary>Creates a managed viewport using only the shipped presentation template.</summary>
    /// <returns>The uninitialized public viewport.</returns>
    internal static Viewport3DX CreateViewport() => new() { Template = LoadTemplate() };

    /// <summary>Inspects the actual selected owner without faking native initialization.</summary>
    /// <param name="viewport">The public viewport.</param>
    /// <returns>The current productive owner.</returns>
    internal static D3D12WpfPresentation OwnerOf(Viewport3DX viewport) =>
        Field<D3D12WpfPresentation>(viewport, "wpfPresentation");

    /// <summary>Inspects a retained private field, including private base fields on derived viewports.</summary>
    /// <typeparam name="T">The declared result type.</typeparam>
    /// <param name="target">The actual object being inspected.</param>
    /// <param name="name">The private field name.</param>
    /// <returns>The actual stored value.</returns>
    /// <exception cref="MissingFieldException">The inspected implementation contract changed.</exception>
    internal static T Field<T>(object target, string name) {
        for (var type = target.GetType(); type is not null; type = type.BaseType)
            if (type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) is { } field)
                return (T) field.GetValue(target)!;
        throw new MissingFieldException(target.GetType().FullName, name);
    }

    /// <summary>Drains/detaches before disposing the viewport, external manager and hidden source in ownership order.</summary>
    /// <returns>The bounded fixture cleanup.</returns>
    public async ValueTask DisposeAsync() {
        var start = Stopwatch.GetTimestamp();
        while (true) {
            try {
                Viewport.Dispose();
                break;
            } catch (TimeoutException) when (Stopwatch.GetElapsedTime(start) < TimeSpan.FromSeconds(5)) {
                await Task.Delay(10);
            }
        }
        if (source is not null) source.RootVisual = null;
        Root.Children.Clear();
        Effects.Dispose();
        source?.Dispose();
    }
}
