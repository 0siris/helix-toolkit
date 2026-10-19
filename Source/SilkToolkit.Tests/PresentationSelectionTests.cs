using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Verifies presentation selection and idle demand without attaching a native presentation source.</summary>
[Collection(WpfCollection.Name)]
public sealed class PresentationSelectionTests {
    /// <summary>Restores the original public defaults and selects a passive image through the shipped template.</summary>
    /// <returns>The managed STA verification.</returns>
    [Fact]
    public Task DefaultTemplateSelectsPassiveWpfPresentation() => StaThread.RunAsync(() => {
        using var viewport = CreateViewport();
        Assert.False(viewport.EnableSwapChainRendering);
        Assert.True(viewport.EnableDpiScale);
        Assert.True(viewport.ApplyTemplate());
        var canvas = Assert.IsType<DPFCanvas>(Presenter(viewport).Content);
        var image = Assert.IsType<Image>(Assert.Single(canvas.Children.Cast<UIElement>()));
        Assert.False(image.IsHitTestVisible);
        Assert.Equal(Brushes.Transparent, canvas.Background);
        Assert.Null(viewport.D3D12Surface);
        Assert.True(viewport.RenderOnDemand);
        Assert.False(Owner(viewport).IsInitialized);
        Assert.Same(canvas, Owner(viewport).Canvas);
        Assert.Equal(0, viewport.RenderD3D12Frame(TimeSpan.Zero));
        Assert.Equal(0UL, Owner(viewport).SubmittedFrames);
    });

    /// <summary>Rejects direct, current-value and binding changes after the native-success notification.</summary>
    /// <returns>The managed STA verification.</returns>
    [Fact]
    public Task SelectedBackendCannotChangeThroughAnyDependencyPropertyRoute() => StaThread.RunAsync(() => {
        using var viewport = CreateViewport();
        viewport.ApplyTemplate();
        var owner = Owner(viewport);
        // Exercise the successful native initialization boundary without creating GPU resources.
        viewport.MarkPresentationInitialized();
        Assert.Throws<InvalidOperationException>(() => viewport.EnableSwapChainRendering = true);
        Assert.False(viewport.EnableSwapChainRendering);
        Assert.Throws<InvalidOperationException>(() =>
            viewport.SetCurrentValue(Viewport3DX.EnableSwapChainRenderingProperty, true));
        Assert.False(viewport.EnableSwapChainRendering);
        _ = Record.Exception(() => BindingOperations.SetBinding(viewport,
            Viewport3DX.EnableSwapChainRenderingProperty, new Binding { Source = true }));
        Assert.False(viewport.EnableSwapChainRendering);
        Assert.Same(owner, Owner(viewport));
        Assert.False(owner.IsDisposed);
    });

    /// <summary>Allows selection before ownership and keeps explicitly selected HWND rendering continuous.</summary>
    /// <returns>The managed STA verification.</returns>
    [Fact]
    public Task ExplicitHwndSelectionLocksAndRetainsContinuousRendering() => StaThread.RunAsync(() => {
        using var viewport = CreateViewport();
        viewport.EnableSwapChainRendering = true;
        viewport.EnableSwapChainRendering = false;
        viewport.EnableSwapChainRendering = true;
        viewport.ApplyTemplate();
        Assert.Same(viewport.D3D12Surface, Presenter(viewport).Content);
        Assert.NotNull(viewport.D3D12Surface);
        Assert.False(viewport.RenderOnDemand);
        viewport.MarkPresentationInitialized();
        Assert.Throws<InvalidOperationException>(() => viewport.EnableSwapChainRendering = false);
        Assert.True(viewport.EnableSwapChainRendering);
    });

    /// <summary>Retires only presentation objects on template reapplication and preserves scene and manager identity.</summary>
    /// <returns>The managed STA verification.</returns>
    [Fact]
    public Task TemplateReplacementRetiresTransportAndPreservesApplicationObjects() => StaThread.RunAsync(() => {
        using var viewport = CreateViewport();
        using var manager = new DefaultEffectsManager();
        using var model = new MeshGeometryModel3D();
        viewport.EffectsManager = manager;
        viewport.Items.Add(model);
        viewport.ApplyTemplate();
        var previousOwner = Owner(viewport);
        var previousItems = (ItemsControl) viewport.Template.FindName("PART_Items", viewport);
        var previousCube = viewport.Template.FindName("PART_ViewCube", viewport);
        viewport.MarkPresentationInitialized();
        var previousCanvas = previousOwner.Canvas;
        viewport.Template = null;
        viewport.Template = LoadTemplate();
        viewport.ApplyTemplate();
        Assert.True(previousOwner.IsDisposed);
        Assert.Empty(previousCanvas.Children);
        Assert.NotSame(previousOwner, Owner(viewport));
        Assert.DoesNotContain(model, previousItems.Items.Cast<object>());
        Assert.NotSame(previousCube, viewport.Template.FindName("PART_ViewCube", viewport));
        Assert.Same(manager, viewport.EffectsManager);
        Assert.Same(model, Assert.Single(viewport.Items));
        Assert.False(model.SceneNode.IsDisposed);
        Assert.Throws<InvalidOperationException>(() => viewport.EnableSwapChainRendering = true);
    });

    /// <summary>Allows unloaded owners to change backend until native initialization succeeds.</summary>
    /// <returns>The managed STA verification.</returns>
    [Fact]
    public Task UninitializedOwnersDoNotLockBackendSelection() => StaThread.RunAsync(() => {
        using var viewport = CreateViewport();
        viewport.ApplyTemplate();
        var original = Owner(viewport);
        Assert.False(original.IsInitialized);
        Assert.Equal(0, viewport.RenderD3D12Frame(TimeSpan.Zero));
        Assert.Throws<InvalidOperationException>(() => original.CaptureFrame());
        viewport.EnableSwapChainRendering = true;
        Assert.True(original.IsDisposed);
        Assert.NotNull(viewport.D3D12Surface);
        Assert.False(viewport.D3D12Surface.IsInitialized);
        var native = viewport.D3D12Surface;
        viewport.EnableSwapChainRendering = false;
        Assert.True(native.IsDisposed);
        Assert.IsType<DPFCanvas>(Presenter(viewport).Content);
        Assert.False(Owner(viewport).IsInitialized);
    });

    /// <summary>Retains the selected owner and effective backend when canvas retirement reports a failure.</summary>
    /// <returns>The managed STA verification.</returns>
    [Fact]
    public Task FailedRetirementRetainsTheOldOwnerAndBackendForRetry() => StaThread.RunAsync(() => {
        using var viewport = CreateViewport();
        viewport.ApplyTemplate();
        var original = Owner(viewport);
        EventHandler failure = (_, _) => throw new InvalidOperationException("Retirement notification failed.");
        original.Canvas.PresentationChanged += failure;
        try {
            Assert.Throws<InvalidOperationException>(() => viewport.EnableSwapChainRendering = true);
            Assert.False(viewport.EnableSwapChainRendering);
            Assert.Same(original, Owner(viewport));
            Assert.Same(original.Canvas, Presenter(viewport).Content);
            Assert.False(original.IsDisposed);
        } finally {
            original.Canvas.PresentationChanged -= failure;
        }
        viewport.EnableSwapChainRendering = true;
        Assert.True(original.IsDisposed);
        Assert.NotNull(viewport.D3D12Surface);
    });

    /// <summary>Keeps one owner and callback set across unload/reload, then releases both owner and passive canvas.</summary>
    /// <returns>The managed STA verification.</returns>
    [Fact]
    public Task UnloadReloadRetainsOwnershipAndFinalDisposalRetiresCanvas() => StaThread.RunAsync(() => {
        using var viewport = CreateViewport();
        viewport.ApplyTemplate();
        var owner = Owner(viewport);
        var canvas = owner.Canvas;
        var callbacks = typeof(DPFCanvas).GetField("PresentationChanged", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var before = ((Delegate) callbacks.GetValue(canvas)!).GetInvocationList().Length;
        for (var repeat = 0; repeat < 2; repeat++) {
            viewport.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            canvas.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            canvas.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            viewport.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            Assert.Same(owner, Owner(viewport));
            Assert.False(owner.IsDisposed);
            Assert.Equal(before, ((Delegate) callbacks.GetValue(canvas)!).GetInvocationList().Length);
        }
        viewport.Dispose();
        Assert.True(owner.IsDisposed);
        Assert.Empty(canvas.Children);
        Assert.Null(callbacks.GetValue(canvas));
    });

    /// <summary>Rejects template replacement during either recorder before disposing or clearing the selected owner.</summary>
    /// <returns>The managed STA verification.</returns>
    [Fact]
    public Task TemplateChangesAreRejectedDuringSceneRecording() => StaThread.RunAsync(() => {
        using var viewport = CreateViewport();
        viewport.ApplyTemplate();
        var owner = Owner(viewport);
        foreach (var target in new object[] { viewport, owner }) {
            var field = target.GetType().GetField(target == viewport ? "isD3D12FrameUpdating" : "recording",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            field.SetValue(target, true);
            try {
                Assert.Throws<InvalidOperationException>(() => viewport.Template = null);
                Assert.Throws<InvalidOperationException>(() => viewport.OnApplyTemplate());
                Assert.Same(owner, Owner(viewport));
                Assert.False(owner.IsDisposed);
                Assert.Same(owner.Canvas, Presenter(viewport).Content);
            } finally {
                field.SetValue(target, false);
            }
        }
    });

    /// <summary>Forwards effective DPI selection while preserving the viewport's device-independent input extent.</summary>
    /// <returns>The managed STA verification.</returns>
    [Fact]
    public Task DpiOptionChangesResolutionWithoutChangingDipLayout() => StaThread.RunAsync(() => {
        using var viewport = CreateViewport();
        viewport.EnableDpiScale = false;
        viewport.ApplyTemplate();
        var canvas = Owner(viewport).Canvas;
        canvas.Measure(new Size(101.25, 53.5));
        canvas.Arrange(new Rect(0, 0, 101.25, 53.5));
        Assert.False(canvas.EnableDpiScale);
        Assert.Equal(1f, canvas.EffectiveDpiScale);
        Assert.Equal(102U, canvas.PixelWidth);
        Assert.Equal(54U, canvas.PixelHeight);
        viewport.EnableDpiScale = true;
        Assert.True(canvas.EnableDpiScale);
        Assert.Equal(101.25, canvas.ActualWidth);
        Assert.Equal(53.5, canvas.ActualHeight);
    });

    /// <summary>Forwards explicit public and host render DPI while leaving the physical HWND input scale unchanged.</summary>
    /// <param name="swapChain">Whether the managed template selects an HWND transport.</param>
    /// <returns>The managed STA verification.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task PublicDpiScaleControlsRenderResolution(bool swapChain) => StaThread.RunAsync(() => {
        using var viewport = CreateViewport();
        viewport.EnableSwapChainRendering = swapChain;
        viewport.DpiScale = 1.5;
        viewport.ApplyTemplate();
        var host = (IRenderHost) viewport;
        Assert.Equal(1.5f, host.DpiScale);
        host.DpiScale = 2;
        Assert.Equal(2d, viewport.DpiScale);
        Assert.Equal(2f, host.DpiScale);
        viewport.EnableDpiScale = false;
        Assert.Equal(1f, host.DpiScale);
        viewport.EnableDpiScale = true;
        Assert.Equal(2f, host.DpiScale);
        if (swapChain) {
            Assert.Equal(1d, viewport.D3D12Surface!.WindowHost.DpiScale);
            Assert.Equal(2d, viewport.D3D12Surface.EffectiveDpiScale);
        } else {
            var canvas = Owner(viewport).Canvas;
            canvas.Measure(new Size(101.25, 53.5));
            canvas.Arrange(new Rect(0, 0, 101.25, 53.5));
            Assert.Equal(203U, canvas.PixelWidth);
            Assert.Equal(107U, canvas.PixelHeight);
            Assert.Equal(101.25, canvas.ActualWidth);
        }
    });

    /// <summary>Removes Scene2D roots while leaving the independent view cube in the 3D root selection.</summary>
    /// <returns>The managed STA verification.</returns>
    [Fact]
    public Task DisablingScene2DLeavesTheViewCubeSelected() => StaThread.RunAsync(() => {
        using var viewport = CreateViewport();
        viewport.ApplyTemplate();
        var cube = (ViewBoxModel3D) viewport.Template.FindName("PART_ViewCube", viewport);
        Assert.NotEmpty(viewport.D2DRenderables);
        Drain(viewport);
        viewport.EnableD2DRendering = false;
        Assert.Empty(viewport.D2DRenderables);
        Assert.Contains(cube.SceneNode, viewport.Renderables);
        Assert.True(viewport.RenderDemand.HasRenderDemand);
    });

    /// <summary>Wakes drained demand for render state, aliases, camera replacement and Scene2D selection.</summary>
    /// <returns>The managed STA verification.</returns>
    [Fact]
    public Task ImagePropertiesAndOriginalAliasesWakeAnIdleViewport() => StaThread.RunAsync(() => {
        using var viewport = new Viewport3DX { MinimumUpdateCount = 0 };
        (DependencyProperty Property, object Value)[] changes = [
            (Viewport3DX.BackgroundColorProperty, Colors.Red),
            (Viewport3DX.IsShadowMappingEnabledProperty, true),
            (Viewport3DX.EnableRenderFrustumProperty, false),
            (Viewport3DX.FXAALevelProperty, FXAALevel.High),
            (Viewport3DX.MSAAProperty, MSAALevel.Four),
            (Viewport3DX.OITRenderModeProperty, OITRenderType.SinglePassWeighted),
            (Viewport3DX.OITWeightPowerProperty, 2d),
            (Viewport3DX.OITWeightDepthSlopeProperty, 2d),
            (Viewport3DX.OITWeightModeProperty, OITWeightMode.Linear0),
            (Viewport3DX.OITDepthPeelingIterationProperty, 8),
            (Viewport3DX.EnableSSAOProperty, true),
            (Viewport3DX.SSAOSamplingRadiusProperty, 2d),
            (Viewport3DX.SSAOIntensityProperty, 2d),
            (Viewport3DX.SSAOQualityProperty, SSAOQuality.High),
            (Viewport3DX.EnableRenderOrderProperty, true),
            (Viewport3DX.EnableAutoOctreeUpdateProperty, true),
            (Viewport3DX.EnableD2DRenderingProperty, false),
            (Viewport3DX.TitleProperty, "Changed title"),
            (Viewport3DX.ShowViewCubeProperty, false),
            (Viewport3DX.CameraProperty, new PerspectiveCamera())
        ];
        foreach (var (property, value) in changes) {
            Drain(viewport);
            Assert.NotEqual(viewport.GetValue(property), value);
            viewport.SetValue(property, value);
            Assert.True(viewport.RenderDemand.HasRenderDemand, property.Name);
        }
    });

    /// <summary>Leaves a drained scene idle when diagnostics and cursor tracking update.</summary>
    /// <returns>The managed STA verification.</returns>
    [Fact]
    public Task StatusUpdatesDoNotCreateSceneDemand() => StaThread.RunAsync(() => {
        using var viewport = new Viewport3DX { MinimumUpdateCount = 0 };
        Drain(viewport);
        viewport.FrameRate = 60;
        viewport.FrameRateText = "60 FPS";
        viewport.FieldOfViewText = "45 degrees";
        viewport.MessageText = "Status";
        viewport.SetValue(Viewport3DX.CurrentPositionProperty, new System.Windows.Media.Media3D.Point3D(1, 2, 3));
        viewport.SetValue(Viewport3DX.CursorPositionProperty, new System.Windows.Media.Media3D.Point3D(1, 2, 3));
        viewport.MinimumUpdateCount = 5;
        Assert.False(viewport.RenderDemand.HasRenderDemand);
    });

    /// <summary>Loads the actual production style without a Window or PresentationSource.</summary>
    /// <returns>The managed viewport ready for template application.</returns>
    private static Viewport3DX CreateViewport() => new() { Template = LoadTemplate() };

    /// <summary>Reads the shipped control template rather than substituting a test-only canvas.</summary>
    /// <returns>A fresh production template instance.</returns>
    private static ControlTemplate LoadTemplate() {
        var theme = new ResourceDictionary {
            Source = new Uri("/SilkToolkit;component/Themes/Generic.xaml", UriKind.Relative)
        };
        var style = (Style) theme[typeof(Viewport3DX)];
        return (ControlTemplate) style.Setters.OfType<Setter>().Single(s => s.Property == Control.TemplateProperty).Value;
    }

    /// <summary>Finds the actual presentation part after template application.</summary>
    /// <param name="viewport">The templated viewport.</param>
    /// <returns>The real content presenter.</returns>
    private static ContentPresenter Presenter(Viewport3DX viewport) =>
        Assert.IsType<ContentPresenter>(viewport.Template.FindName("PART_Canvas", viewport));

    /// <summary>Reads the selected internal owner without adding a public testing API.</summary>
    /// <param name="viewport">The templated viewport.</param>
    /// <returns>The actual composited owner.</returns>
    private static D3D12WpfPresentation Owner(Viewport3DX viewport) =>
        Assert.IsType<D3D12WpfPresentation>(typeof(Viewport3DX)
            .GetField("wpfPresentation", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(viewport));

    /// <summary>Consumes the bounded managed budget without creating a renderer or GPU frame.</summary>
    /// <param name="viewport">The viewport whose demand is consumed.</param>
    private static void Drain(Viewport3DX viewport) {
        while (viewport.RenderDemand.TryBeginFrame(out _)) viewport.RenderDemand.CompleteFrame(true);
    }
}
