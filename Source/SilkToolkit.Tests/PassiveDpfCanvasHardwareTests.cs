using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Exercises actual passive-canvas publication using only the existing explicit native probe harness.</summary>
/// <param name="output">Receives actual native surface identities, operation counts and diagnostic desktop pixels.</param>
[Collection(WpfCollection.Name)]
public sealed class PassiveDpfCanvasHardwareTests(ITestOutputHelper output) {
    /// <summary>Bounds test-only asynchronous completion and image-lock polling.</summary>
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Checks actual binding, immutable old contents, partial publication failure, same-pointer rebind and resize.</summary>
    /// <returns>The explicitly selected hidden hardware proof.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    [Trait("Category", "Wpf")]
    public Task HiddenHardwareRetainsBoundPixelsAndRebindsWithoutSceneWork() => StaThread.RunAsync(async () => {
            using var source = new HwndSource(
                new HwndSourceParameters(nameof(PassiveDpfCanvasHardwareTests)) {
                    Width = 128, Height = 96, WindowStyle = unchecked((int) 0x80000000)
                }
            );
            using var canvas = new DPFCanvas { EnableDpiScale = false, Width = 64, Height = 48 };
            source.RootVisual = canvas;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Single(canvas.Children);
            var image = Assert.IsType<Image>(canvas.Children[0]);
            await using var driver = new SurfaceDriver(canvas, 64, 48, output);
            await driver.PublishColorAsync(1, 0, 0, 1);
            var redSurface = canvas.ImageSource.BoundSurface;
            Assert.NotEqual(nint.Zero, redSurface);
            Assert.Equal(64, canvas.ImageSource.NativeImage.PixelWidth);
            Assert.Equal(48, canvas.ImageSource.NativeImage.PixelHeight);
            AssertSnapshotPixel(image, 255, 0, 0, 255);

            await driver.PrepareColorAsync(0, 1, 0, 0.5f);
            Assert.Equal(redSurface, canvas.ImageSource.BoundSurface);
            Assert.NotEqual(redSurface, driver.ReadySurface);
            AssertSnapshotPixel(image, 255, 0, 0, 255);
            await driver.PublishPreparedAsync();
            AssertSnapshotPixel(image, 0, 128, 0, 128);

            var completed = driver.ReadySurface;
            var counts = driver.Counts;
            await PollAsync(canvas.ImageSource.TryDetach, "explicit WPF detachment");
            Assert.Equal(nint.Zero, canvas.ImageSource.BoundSurface);
            await driver.PublishPreparedAsync();
            Assert.Equal(completed, canvas.ImageSource.BoundSurface);
            Assert.Equal((counts.Submissions, counts.Copies, counts.Publications + 1), driver.Counts);
            AssertSnapshotPixel(image, 0, 128, 0, 128);

            // Force AddDirtyRect to reject a rectangle after a real SetBackBuffer has already changed the pointer.
            await driver.PrepareColorAsync(0, 0, 1, 1);
            await PollAsync(
                () => {
                    try {
                        if (!canvas.ImageSource.TryPublish(driver.ReadySurface, 65, 48))
                            return false;
                        Assert.Fail("An out-of-bounds dirty rectangle unexpectedly succeeded.");
                        return false;
                    } catch (ArgumentException) {
                        return true;
                    }
                }, "partial publication failure after SetBackBuffer"
            );
            Assert.Equal(driver.ReadySurface, canvas.ImageSource.BoundSurface);
            await driver.PublishPreparedAsync();
            AssertSnapshotPixel(image, 0, 0, 255, 255);

            var nativeImage = canvas.ImageSource.NativeImage;
            await driver.ResizeAsync(96, 72);
            canvas.Width = 96;
            canvas.Height = 72;
            canvas.Measure(new Size(96, 72));
            canvas.Arrange(new Rect(0, 0, 96, 72));
            canvas.UpdateLayout();
            Assert.Equal(96u, canvas.PixelWidth);
            Assert.Equal(72u, canvas.PixelHeight);
            Assert.Same(nativeImage, canvas.ImageSource.NativeImage);
            await driver.PublishColorAsync(1, 0, 0, 0);
            Assert.Equal(96, nativeImage.PixelWidth);
            Assert.Equal(72, nativeImage.PixelHeight);
            AssertSnapshotPixel(image, 0, 0, 0, 0);
            await driver.DisposeAsync();
            Assert.Equal(nint.Zero, canvas.ImageSource.BoundSurface);
            source.RootVisual = null;
        }
    );

    /// <summary>Measures actual desktop RGB/alpha composition through the new canvas, then resizes its native surfaces.</summary>
    /// <returns>The explicit interactive, unobscured desktop verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    [Trait("Category", "Wpf")]
    [Trait("Category", "VisibleUI")]
    public Task VisibleHardwareComposesActualCanvasPixelsAndAlpha() => StaThread.RunAsync(async () => {
            Assert.True(Environment.UserInteractive, "An interactive desktop is required for visible canvas sampling.");
            var area = SystemParameters.WorkArea;
            var bounds = new Rect(area.Left + 32, area.Top + 32, 192, 144);
            Assert.True(area.Contains(bounds));
            using var canvas = new DPFCanvas();
            var window = new Window {
                Title = "Passive DPFCanvas hardware proof",
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = bounds.Left,
                Top = bounds.Top,
                Width = bounds.Width,
                Height = bounds.Height,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                ShowActivated = false,
                ShowInTaskbar = false,
                Topmost = true,
                Background = Brushes.White,
                Content = canvas
            };
            try {
                window.Show();
                window.UpdateLayout();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                Assert.Equal(new WindowInteropHelper(window).Handle, canvas.NativeHandle);
                Assert.True(canvas.IsActive);
                await using var driver = new SurfaceDriver(canvas, canvas.PixelWidth, canvas.PixelHeight, output);
                foreach (var color in new[] { (R: 1f, G: 0f, B: 0f), (R: 0f, G: 1f, B: 0f), (R: 0f, G: 0f, B: 1f) }) {
                    foreach (var alpha in new[] { 0f, 0.5f, 1f }) {
                        await driver.PublishColorAsync(color.R, color.G, color.B, alpha);
                        await AssertDesktopPixelAsync(canvas, window, color.R, color.G, color.B, alpha);
                    }
                }

                var counts = driver.Counts;
                var surface = canvas.ImageSource.BoundSurface;
                await PollAsync(canvas.ImageSource.TryDetach, "visible detach before completed-image rebind");
                await driver.PublishPreparedAsync();
                Assert.Equal(surface, canvas.ImageSource.BoundSurface);
                await AssertDesktopPixelAsync(canvas, window, 0, 0, 1, 1);
                Assert.Equal((counts.Submissions, counts.Copies, counts.Publications + 1), driver.Counts);

                window.Width = 240;
                window.Height = 168;
                window.UpdateLayout();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                await driver.ResizeAsync(canvas.PixelWidth, canvas.PixelHeight);
                await driver.PublishColorAsync(0, 1, 0, 0.5f);
                await AssertDesktopPixelAsync(canvas, window, 0, 1, 0, 0.5f);
                Assert.Equal((int) canvas.PixelWidth, canvas.ImageSource.NativeImage.PixelWidth);
                Assert.Equal((int) canvas.PixelHeight, canvas.ImageSource.NativeImage.PixelHeight);
                output.WriteLine($"Final actual submissions/copies/publications={driver.Counts}.");
            } finally {
                window.Content = null;
                window.Close();
            }
        }
    );

    /// <summary>Reads a diagnostic WPF software snapshot of the actual image without supplying presentation pixels.</summary>
    /// <param name="image">The canvas image with software fallback enabled by the production image source.</param>
    /// <param name="red">The expected premultiplied red byte.</param>
    /// <param name="green">The expected premultiplied green byte.</param>
    /// <param name="blue">The expected premultiplied blue byte.</param>
    /// <param name="alpha">The expected coverage byte.</param>
    private static void AssertSnapshotPixel(Image image, int red, int green, int blue, int alpha) {
        image.UpdateLayout();
        var width = (int) Math.Ceiling(image.ActualWidth);
        var height = (int) Math.Ceiling(image.ActualHeight);
        Assert.True(width > 0 && height > 0);
        var snapshot = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        snapshot.Render(image);
        var pixel = new byte[4];
        snapshot.CopyPixels(new Int32Rect(width / 2, height / 2, 1, 1), pixel, 4, 0);
        Assert.InRange((int) pixel[0], Math.Max(0, blue - 24), Math.Min(255, blue + 24));
        Assert.InRange((int) pixel[1], Math.Max(0, green - 24), Math.Min(255, green + 24));
        Assert.InRange((int) pixel[2], Math.Max(0, red - 24), Math.Min(255, red + 24));
        Assert.InRange((int) pixel[3], Math.Max(0, alpha - 24), Math.Min(255, alpha + 24));
    }

    /// <summary>Checks an unobscured physical desktop pixel against premultiplied composition over the white window.</summary>
    /// <param name="canvas">The visible production canvas.</param>
    /// <param name="window">The authorized test window whose native HWND must own the sampled point.</param>
    /// <param name="red">The submitted straight red channel.</param>
    /// <param name="green">The submitted straight green channel.</param>
    /// <param name="blue">The submitted straight blue channel.</param>
    /// <param name="alpha">The submitted coverage alpha.</param>
    /// <returns>The bounded desktop settling and diagnostic pixel comparison.</returns>
    private async Task AssertDesktopPixelAsync(
        DPFCanvas canvas,
        Window window,
        float red,
        float green,
        float blue,
        float alpha
    ) {
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        await Task.Delay(250);
        var center = canvas.PointToScreen(new Point(canvas.ActualWidth / 2, canvas.ActualHeight / 2));
        var point = new NativePoint { X = (int) Math.Round(center.X), Y = (int) Math.Round(center.Y) };
        Assert.Equal(new WindowInteropHelper(window).Handle, WindowFromPoint(point));
        var dc = GetDC(0);
        Assert.NotEqual(nint.Zero, dc);
        uint pixel;
        try {
            pixel = GetPixel(dc, point.X, point.Y);
        } finally {
            _ = ReleaseDC(0, dc);
        }

        Assert.NotEqual(uint.MaxValue, pixel);
        var actualRed = (int) (pixel & 255);
        var actualGreen = (int) ((pixel >> 8) & 255);
        var actualBlue = (int) ((pixel >> 16) & 255);
        output.WriteLine(
            $"Surface=0x{canvas.ImageSource.BoundSurface:X}; screen RGB=({actualRed},{actualGreen}," +
            $"{actualBlue}); submitted RGBA=({red},{green},{blue},{alpha}); DPI={canvas.EffectiveDpiScale}."
        );
        Assert.InRange(Math.Abs(actualRed - 255 * (red * alpha + 1 - alpha)), 0, 24);
        Assert.InRange(Math.Abs(actualGreen - 255 * (green * alpha + 1 - alpha)), 0, 24);
        Assert.InRange(Math.Abs(actualBlue - 255 * (blue * alpha + 1 - alpha)), 0, 24);
    }

    /// <summary>Polls only test-driver work, allowing input dispatch and enforcing a finite deadline.</summary>
    /// <param name="completed">The nonblocking native completion or WPF publication attempt.</param>
    /// <param name="operation">The operation included in a failure report.</param>
    /// <returns>The bounded asynchronous completion task used solely by tests.</returns>
    /// <exception cref="TimeoutException">The operation does not finish within five seconds.</exception>
    private static async Task PollAsync(Func<bool> completed, string operation) {
        var start = Stopwatch.GetTimestamp();
        while (!completed()) {
            if (Stopwatch.GetElapsedTime(start) >= CompletionTimeout)
                throw new TimeoutException($"{operation} did not finish within {CompletionTimeout}.");
            await Task.Delay(10);
        }
    }

    /// <summary>Owns only the existing probe's test surfaces and explicit color/copy calls, never a product renderer.</summary>
    private sealed class SurfaceDriver : IAsyncDisposable {
        /// <summary>Borrows the passive canvas whose image publishes the probe's completed surfaces.</summary>
        private readonly DPFCanvas canvas;

        /// <summary>Reports actual native identities and counts for the explicitly selected test.</summary>
        private readonly ITestOutputHelper output;

        /// <summary>Retains the owning native probe until WPF has successfully detached every surface.</summary>
        private D3DImageInteropProbe? probe;

        /// <summary>Creates real native surfaces using the actual WPF source HWND.</summary>
        /// <param name="canvas">The loaded passive canvas.</param>
        /// <param name="width">The nonzero physical width.</param>
        /// <param name="height">The nonzero physical height.</param>
        /// <param name="output">The diagnostic output destination.</param>
        internal SurfaceDriver(DPFCanvas canvas, uint width, uint height, ITestOutputHelper output) {
            this.canvas = canvas;
            this.output = output;
            probe = new D3DImageInteropProbe(canvas.NativeHandle, width, height);
            output.WriteLine($"Probe DXGI index={probe.AdapterIndex}; LUID=0x{probe.AdapterLuid:X16}; {width}x{height}.");
        }

        /// <summary>Gets the actual completed probe surface without advancing or copying a frame.</summary>
        internal nint ReadySurface => probe!.D3D9Surface;

        /// <summary>Gets actual explicit submissions, copies and successful production-image publications.</summary>
        internal (int Submissions, int Copies, int Publications) Counts { get; private set; }

        /// <summary>Submits one explicit color and finishes its copy without changing the WPF binding.</summary>
        /// <param name="red">The straight red channel.</param>
        /// <param name="green">The straight green channel.</param>
        /// <param name="blue">The straight blue channel.</param>
        /// <param name="alpha">The coverage alpha premultiplied by the existing native probe.</param>
        /// <returns>The native completion task without publication.</returns>
        internal async Task PrepareColorAsync(float red, float green, float blue, float alpha) {
            var current = probe!;
            current.SubmitColor(red, green, blue, alpha);
            Counts = (Counts.Submissions + 1, Counts.Copies, Counts.Publications);
            await PollAsync(() => current.IsRenderComplete, "probe D3D12 clear");
            current.BeginCopyToSurface();
            Counts = (Counts.Submissions, Counts.Copies + 1, Counts.Publications);
            Assert.NotEqual(canvas.ImageSource.BoundSurface, current.D3D9Surface);
            await PollAsync(() => current.IsCopyComplete, "probe copy to detached destination");
        }

        /// <summary>Publishes the already completed surface using only the production image-source API.</summary>
        /// <returns>The bounded short-lock retry task.</returns>
        internal async Task PublishPreparedAsync() {
            var current = probe!;
            Assert.True(current.IsCopyComplete);
            await PollAsync(
                () => canvas.ImageSource.TryPublish(current.D3D9Surface, current.Width, current.Height),
                "production TryPublish"
            );
            Counts = (Counts.Submissions, Counts.Copies, Counts.Publications + 1);
            Assert.Equal(current.D3D9Surface, canvas.ImageSource.BoundSurface);
        }

        /// <summary>Submits, copies and publishes exactly one explicit color through the production canvas.</summary>
        /// <param name="red">The straight red channel.</param>
        /// <param name="green">The straight green channel.</param>
        /// <param name="blue">The straight blue channel.</param>
        /// <param name="alpha">The coverage alpha.</param>
        /// <returns>The bounded single-publication task.</returns>
        internal async Task PublishColorAsync(float red, float green, float blue, float alpha) {
            await PrepareColorAsync(red, green, blue, alpha);
            await PublishPreparedAsync();
        }

        /// <summary>Detaches the actual WPF image before retiring and replacing the probe's native allocations.</summary>
        /// <param name="width">The replacement physical width.</param>
        /// <param name="height">The replacement physical height.</param>
        /// <returns>The finite detach, teardown and recreation task.</returns>
        internal async Task ResizeAsync(uint width, uint height) {
            var adapter = probe!.AdapterIndex;
            var luid = probe.AdapterLuid;
            await DisposeAsync();
            probe = new D3DImageInteropProbe(canvas.NativeHandle, width, height, adapter);
            Assert.Equal(luid, probe.AdapterLuid);
        }

        /// <summary>Releases native resources only after confirmed WPF detachment, retaining ownership on failure.</summary>
        /// <returns>The bounded test-only image-lock retry and native teardown.</returns>
        public async ValueTask DisposeAsync() {
            if (probe is not { } current)
                return;
            await PollAsync(canvas.ImageSource.TryDetach, "detach before native probe teardown");
            Assert.Equal(nint.Zero, canvas.ImageSource.BoundSurface);
            current.Dispose();
            probe = null;
            output.WriteLine($"Detached before native disposal; explicit counts={Counts}.");
        }
    }

    /// <summary>Stores one physical point for the native desktop occlusion check.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint {
        /// <summary>The horizontal physical pixel coordinate.</summary>
        internal int X;

        /// <summary>The vertical physical pixel coordinate.</summary>
        internal int Y;
    }

    /// <summary>Acquires a desktop DC solely for diagnostic reads.</summary>
    /// <param name="window">Zero selects the whole desktop.</param>
    /// <returns>The borrowed diagnostic DC, or zero on failure.</returns>
    [DllImport("user32.dll")]
    private static extern nint GetDC(nint window);

    /// <summary>Releases the diagnostic desktop DC.</summary>
    /// <param name="window">The zero HWND used to acquire the DC.</param>
    /// <param name="dc">The acquired diagnostic DC.</param>
    /// <returns>Whether the DC was released.</returns>
    [DllImport("user32.dll")]
    private static extern int ReleaseDC(nint window, nint dc);

    /// <summary>Reads one already composed desktop pixel without feeding CPU data into presentation.</summary>
    /// <param name="dc">The diagnostic desktop DC.</param>
    /// <param name="x">The physical horizontal pixel coordinate.</param>
    /// <param name="y">The physical vertical pixel coordinate.</param>
    /// <returns>The native COLORREF in low-byte RGB order, or uint.MaxValue on failure.</returns>
    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(nint dc, int x, int y);

    /// <summary>Identifies the actual desktop window at the sampled point to reject obscured measurements.</summary>
    /// <param name="point">The physical screen coordinate.</param>
    /// <returns>The topmost native window at the coordinate.</returns>
    [DllImport("user32.dll")]
    private static extern nint WindowFromPoint(NativePoint point);
}