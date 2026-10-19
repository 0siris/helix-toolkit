using System.Reflection;
using System.Windows.Threading;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Elements2D;
using Xunit;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkToolkit.Tests;

/// <summary>Checks productive Scene2D callback cancellation and fresh pixels on both presentation backends.</summary>
[Collection(WpfCollection.Name)]
public sealed class P4Scene2DResourceLifecycleTests {
    /// <summary>Disposes and restarts the same manager from the original overlay update with a new dirty child.</summary>
    /// <param name="hwnd">Whether to select the HWND backend instead of WPF composition.</param>
    /// <returns>The real callback, canceled submission, recovery and subsequent overlay pixel verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task OverlayUpdateResourceEpochStopsDirtyChildAndLaterPresentsNewPixels(bool hwnd) =>
        StaThread.RunAsync(async () => {
            using var effects = new DefaultEffectsManager();
            await using var driver = new P4PresentationDriver(hwnd, effects);
            driver.Viewport.EnableD2DRendering = true;
            using var drawing = new D2DDeviceContext();
            using var green = new SolidColorBrush(drawing, new Vector4(0, 1, 0, 1));
            using var red = new SolidColorBrush(drawing, new Vector4(1, 0, 0, 1));
            using var child = new ObservedRectangle {
                Fill = green, Width = 12, Height = 12, EnableBitmapCache = false,
                HorizontalAlignment = HelixToolkit.SharpDX.Core.Model.Scene2D.HorizontalAlignment.Left,
                VerticalAlignment = HelixToolkit.SharpDX.Core.Model.Scene2D.VerticalAlignment.Top
            };
            var overlay = Assert.IsType<Overlay>(typeof(Viewport3DX)
                .GetProperty("Overlay2D", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(driver.Viewport));
            var root = Assert.IsType<OverlayNode2D>(overlay.SceneNode);
            await driver.WarmAsync();
            var context = driver.Context!;
            var before = driver.Counts;
            var calls = 0;
            EventHandler<SceneNode2D.UpdateEventArgs>? transition = null;
            transition = (_, args) => {
                root.UpdateRequested -= transition;
                calls++;
                Assert.True(driver.Viewport.IsPreparingPresentationFrame);
                Assert.Same(effects, args.Context.DeviceResources);
                Assert.True(root.AddChildNode(child));
                Assert.True(child.IsAttached);
                effects.DisposeAllResources();
                Assert.False(driver.Viewport.CanSubmitNativeScene);
                effects.Reinitialize();
                Assert.False(driver.Viewport.CanSubmitNativeScene);
                Assert.Same(context, driver.Context);
                Assert.NotNull(context.Camera);
            };
            root.UpdateRequested += transition;
            try {
                driver.Viewport.InvalidateRender();
                Assert.Equal(0, driver.Tick(wait: true));
                Assert.Equal(1, calls);
                Assert.Equal(0, child.Updates);
                driver.AssertAdvanced(before, 0);
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                await driver.DrainAsync();
                Assert.Null(context.Camera);
                Assert.NotSame(context, driver.Context);
                Assert.Same(effects, driver.Viewport.EffectsManager);
                Assert.True(effects.Initialized);
                Assert.True(child.IsAttached);
                Assert.True(child.Updates > 0);
                P4PresentationDriver.AssertPixel(await driver.CaptureAsync(), 6, 6, 0, 255, 0, 255);
                child.Fill = red;
                await driver.DrainAsync();
                P4PresentationDriver.AssertPixel(await driver.CaptureAsync(), 6, 6, 0, 0, 255, 255);
                Assert.Equal(driver.Submitted, driver.Completed);
                Assert.Equal((ulong)driver.RenderedCount, driver.Completed);
                Assert.Equal(driver.RenderedCount, driver.OnRenderedCount);
            } finally {
                root.UpdateRequested -= transition;
                root.RemoveChildNode(child);
            }
        });

    /// <summary>Counts original shape updates without replacing the resource-sensitive update implementation.</summary>
    private sealed class ObservedRectangle : RectangleNode2D {
        /// <summary>Counts entry before the production shape update executes.</summary>
        internal int Updates;

        /// <inheritdoc />
        public override void Update(RenderContext2D context) {
            Updates++;
            base.Update(context);
        }
    }
}
