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

/// <summary>Proves the hardware interop lifecycle and actual WPF desktop composition on an STA dispatcher.</summary>
/// <param name="output">Receives GPU timings, publication counts and diagnostic desktop pixel measurements.</param>
[Collection(WpfCollection.Name)]
public sealed class D3DImageInteropWpfTests(ITestOutputHelper output) {
    /// <summary>Limits each native completion or nonblocking WPF lock acquisition.</summary>
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Allows the native teardown's two five-second queue waits plus a small scheduling margin.</summary>
    private static readonly TimeSpan TeardownTimeout = TimeSpan.FromSeconds(11);

    /// <summary>Checks repeated explicit publication, dispatcher liveness, idle counts and detached recreation.</summary>
    /// <returns>The hidden hardware lifecycle verification.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    [Trait("Category", "Wpf")]
    public Task HiddenHardwarePublishesResizesAndDisposes() => StaThread.RunAsync(async () => {
        using var window = new HwndSource(new HwndSourceParameters(nameof(D3DImageInteropWpfTests)) {
            Width = 64, Height = 48, WindowStyle = unchecked((int)0x80000000)
        });
        ProbeDriver? driver = null;
        try {
            driver = new ProbeDriver(window.Handle, 64, 48, output);
            window.RootVisual = new Image { Source = driver.Image };
            for (var repeat = 0; repeat < 2; repeat++) {
                foreach (var color in new[] { (1f, 0f, 0f), (0f, 1f, 0f), (0f, 0f, 1f) }) {
                    var publication = driver.PublishAsync(color.Item1, color.Item2, color.Item3, 1);
                    var dispatcherRan = false;
                    await Dispatcher.CurrentDispatcher.InvokeAsync(() => dispatcherRan = true, DispatcherPriority.Input);
                    await publication;
                    Assert.True(dispatcherRan, "Input dispatch did not run during asynchronous publication.");
                }
            }

            Assert.Equal((6, 6, 6), driver.Counts);
            Assert.Equal(64, driver.Image.PixelWidth);
            Assert.Equal(48, driver.Image.PixelHeight);
            await AssertDriverIdleAsync(driver);

            var selectedAdapter = driver.AdapterIndex;
            var selectedLuid = driver.AdapterLuid;
            await driver.DisposeAsync();
            Assert.True(driver.IsDisposed);
            window.RootVisual = null;
            driver = new ProbeDriver(window.Handle, 96, 72, output, selectedAdapter);
            Assert.Equal(selectedLuid, driver.AdapterLuid);
            window.RootVisual = new Image { Source = driver.Image };
            await driver.PublishAsync(0, 1, 0, 0.5f);
            Assert.Equal(96, driver.Image.PixelWidth);
            Assert.Equal(72, driver.Image.PixelHeight);
            Assert.Equal((1, 1, 1), driver.Counts);
            await driver.DisposeAsync();
            Assert.True(driver.IsDisposed);
            await driver.DisposeAsync();
        } finally {
            try {
                if (driver is not null)
                    await driver.DisposeAsync();
            } finally {
                window.RootVisual = null;
            }
        }
    });

    /// <summary>
    ///     Samples actual desktop composition for RGB, straight alpha 0/0.5/1, repeat publication and resize.
    ///     Requires an interactive, unobscured desktop; environment obstacles fail with diagnostic evidence.
    /// </summary>
    /// <returns>The explicitly authorized visible hardware proof.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    [Trait("Category", "Wpf")]
    [Trait("Category", "VisibleUI")]
    public Task VisibleHardwareComposesColorsAndAlphaOverWhite() => StaThread.RunAsync(async () => {
        output.WriteLine($"Interactive={Environment.UserInteractive}; remote={SystemParameters.IsRemoteSession}; " +
            $"WPF tier={RenderCapability.Tier >> 16}; work area={SystemParameters.WorkArea}; adapterIndex=-1.");
        Assert.True(Environment.UserInteractive, "The desktop pixel proof requires an interactive Windows session.");
        var area = SystemParameters.WorkArea;
        var bounds = new Rect(area.Left + 24, area.Top + 24, 192, 144);
        Assert.True(area.Contains(bounds), $"The fixed proof window {bounds} does not fit work area {area}.");
        var image = new Image { Stretch = Stretch.Fill };
        var window = new Window {
            Title = "D3DImage hardware composition proof",
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = bounds.Left, Top = bounds.Top, Width = bounds.Width, Height = bounds.Height,
            WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
            ShowActivated = false, ShowInTaskbar = false, Topmost = true,
            Background = Brushes.White, Content = image
        };
        ProbeDriver? driver = null;
        try {
            window.Show();
            window.UpdateLayout();
            await AssertComposedPixelAsync(image, window, 1, 1, 1, 1, "white desktop baseline");
            var hwnd = new WindowInteropHelper(window).Handle;
            driver = new ProbeDriver(hwnd, 64, 48, output);
            image.Source = driver.Image;

            await driver.PublishAsync(1, 0, 0, 1);
            await AssertComposedPixelAsync(image, window, 1, 0, 0, 1, "attached red before detached copy");
            await driver.PublishAsync(0, 1, 0, 1, async () => {
                await AssertComposedPixelAsync(image, window, 1, 0, 0, 1,
                    "reentrant desktop snapshot before green publication retains attached red");
                AssertAttachedRedSnapshot(image);
            });
            await AssertComposedPixelAsync(image, window, 0, 1, 0, 1, "completed green publication");

            // Every sample is a one-off diagnostic read of the composed desktop, never a presentation source.
            foreach (var color in new[] {
                (Name: "red", R: 1f, G: 0f, B: 0f),
                (Name: "green", R: 0f, G: 1f, B: 0f),
                (Name: "blue", R: 0f, G: 0f, B: 1f)
            }) {
                foreach (var alpha in new[] { 1f, 0f, 0.5f, 1f }) {
                    await driver.PublishAsync(color.R, color.G, color.B, alpha);
                    await AssertComposedPixelAsync(image, window, color.R, color.G, color.B, alpha,
                        $"{color.Name}, alpha={alpha}");
                }
            }

            await driver.PublishAsync(1, 0, 0, 1);
            await driver.PublishAsync(1, 0, 0, 1);
            await AssertComposedPixelAsync(image, window, 1, 0, 0, 1, "repeated red publication");
            Assert.Equal((16, 16, 16), driver.Counts);
            await AssertDriverIdleAsync(driver);
            await AssertComposedPixelAsync(image, window, 1, 0, 0, 1, "idle retains red");

            var selectedAdapter = driver.AdapterIndex;
            var selectedLuid = driver.AdapterLuid;
            await driver.DisposeAsync();
            Assert.True(driver.IsDisposed);
            image.Source = null;
            driver = new ProbeDriver(hwnd, 96, 72, output, selectedAdapter);
            Assert.Equal(selectedLuid, driver.AdapterLuid);
            image.Source = driver.Image;
            await driver.PublishAsync(0, 1, 0, 0.5f);
            Assert.Equal(96, driver.Image.PixelWidth);
            Assert.Equal(72, driver.Image.PixelHeight);
            await AssertComposedPixelAsync(image, window, 0, 1, 0, 0.5f, "resized translucent green");
            await driver.DisposeAsync();
            Assert.True(driver.IsDisposed);
            image.Source = null;
            await AssertComposedPixelAsync(image, window, 1, 1, 1, 1, "disposed surface leaves white");
        } finally {
            try {
                if (driver is not null)
                    await driver.DisposeAsync();
            } finally {
                image.Source = null;
                window.Content = null;
                window.Close();
            }
        }
    });

    /// <summary>Checks that this explicit-update test driver performs no submissions, copies or publications at idle.</summary>
    /// <param name="driver">The driver whose completed frame remains attached.</param>
    /// <returns>The bounded idle observation, including an input-priority dispatcher round trip.</returns>
    private async Task AssertDriverIdleAsync(ProbeDriver driver) {
        var counts = driver.Counts;
        var clock = Stopwatch.StartNew();
        await Task.Delay(200);
        await Dispatcher.Yield(DispatcherPriority.Input);
        Assert.Equal(counts, driver.Counts);
        output.WriteLine($"Explicit test driver idle for {clock.Elapsed.TotalMilliseconds:F1} ms: " +
            $"submissions/copies/publications={counts}; no automatic render timer is installed.");
    }

    /// <summary>Forces one diagnostic WPF software snapshot of the old attached image during a detached-slot copy.</summary>
    /// <param name="image">The image still publishing opaque red while the alternate slot receives green.</param>
    private void AssertAttachedRedSnapshot(Image image) {
        var width = checked((int)Math.Ceiling(image.ActualWidth));
        var height = checked((int)Math.Ceiling(image.ActualHeight));
        var snapshot = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        snapshot.Render(image);
        var pixel = new byte[4];
        snapshot.CopyPixels(new Int32Rect(width / 2, height / 2, 1, 1), pixel, 4, 0);
        output.WriteLine($"Reentrant diagnostic WPF snapshot BGRA=({string.Join(",", pixel)}); " +
            "expected attached red=(0,0,255,255), tolerance=24/channel; snapshot is never presented.");
        Assert.InRange((int)pixel[0], 0, 24);
        Assert.InRange((int)pixel[1], 0, 24);
        Assert.InRange((int)pixel[2], 231, 255);
        Assert.InRange((int)pixel[3], 231, 255);
    }

    /// <summary>Allows WPF/DWM to settle, then compares one physical desktop pixel with premultiplied alpha over white.</summary>
    /// <param name="image">The visible WPF image whose center is sampled.</param>
    /// <param name="window">The exact authorized topmost proof window.</param>
    /// <param name="red">The submitted straight red channel.</param>
    /// <param name="green">The submitted straight green channel.</param>
    /// <param name="blue">The submitted straight blue channel.</param>
    /// <param name="alpha">The submitted straight alpha channel.</param>
    /// <param name="label">The measurement description recorded with actual and expected channels.</param>
    /// <returns>The asynchronous compositor settling and diagnostic comparison.</returns>
    private async Task AssertComposedPixelAsync(Image image, Window window,
        float red, float green, float blue, float alpha, string label) {
        window.UpdateLayout();
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        await Task.Delay(250);
        Assert.True(image.ActualWidth > 0 && image.ActualHeight > 0, "The proof image has no visible layout area.");
        var center = image.PointToScreen(new Point(image.ActualWidth / 2, image.ActualHeight / 2));
        var point = new NativePoint { X = (int)Math.Round(center.X), Y = (int)Math.Round(center.Y) };
        var expectedWindow = new WindowInteropHelper(window).Handle;
        var actualWindow = WindowFromPoint(point);
        output.WriteLine($"{label}: screen=({point.X},{point.Y}); owner=0x{actualWindow:X}; " +
            $"expected owner=0x{expectedWindow:X}; DPI={VisualTreeHelper.GetDpi(image)}.");
        Assert.True(actualWindow == expectedWindow,
            $"{label}: screen center is obscured or outside the proof window; " +
            $"WindowFromPoint=0x{actualWindow:X}, expected=0x{expectedWindow:X} at {center}.");

        var dc = GetDC(0);
        Assert.True(dc != 0, $"{label}: GetDC(screen) failed; Win32 error={Marshal.GetLastWin32Error()}.");
        uint pixel;
        try {
            pixel = GetPixel(dc, point.X, point.Y);
        } finally {
            _ = ReleaseDC(0, dc);
        }
        Assert.True(pixel != uint.MaxValue,
            $"{label}: GetPixel returned CLR_INVALID at {center}; desktop may be locked or unavailable.");
        var actual = new[] { (int)(pixel & 255), (int)((pixel >> 8) & 255), (int)((pixel >> 16) & 255) };
        var expected = new[] { red, green, blue }.Select(channel =>
            (int)Math.Round(255 * (channel * alpha + 1 - alpha))).ToArray();
        output.WriteLine($"{label}: desktop RGB=({string.Join(",", actual)}), " +
            $"expected=({string.Join(",", expected)}), tolerance=24/channel.");
        for (var channel = 0; channel < 3; channel++)
            Assert.True(Math.Abs(actual[channel] - expected[channel]) <= 24,
                $"{label}: channel {channel} measured {actual[channel]}, expected {expected[channel]} +/-24; " +
                "check channel order, premultiplication, GPU copy and desktop composition.");
    }

    /// <summary>Polls a nonblocking completion query without starving the STA dispatcher or exceeding its deadline.</summary>
    /// <param name="completed">The nonblocking native query or zero-timeout lock attempt.</param>
    /// <param name="operation">The operation included in timeout failures.</param>
    /// <returns>The completion of the bounded asynchronous poll.</returns>
    /// <exception cref="TimeoutException">The operation has not completed within five seconds.</exception>
    private static async Task PollAsync(Func<bool> completed, string operation) {
        var clock = Stopwatch.StartNew();
        await Dispatcher.Yield(DispatcherPriority.Input);
        while (!completed()) {
            if (clock.Elapsed >= CompletionTimeout)
                throw new TimeoutException($"{operation} did not complete within {CompletionTimeout}.");
            await Task.Delay(10);
        }
    }

    /// <summary>Owns only the explicit test submissions and enforces the fixed native/WPF synchronization contract.</summary>
    private sealed class ProbeDriver : IAsyncDisposable {
        /// <summary>Receives completion timings and exact test-driver publication counts.</summary>
        private readonly ITestOutputHelper output;

        /// <summary>Owns the native surfaces until WPF has detached them under its image lock.</summary>
        private D3DImageInteropProbe? probe;

        /// <summary>Counts test-driver calls to SubmitColor, BeginCopyToSurface and AddDirtyRect respectively.</summary>
        private (int Submissions, int Copies, int Publications) counts;

        /// <summary>Remembers the attached surface so every subsequent native copy must select the detached slot.</summary>
        private nint publishedSurface;

        /// <summary>Creates the real hardware probe and a dispatcher-owned D3DImage without an automatic render loop.</summary>
        /// <param name="hwnd">The real hidden or visible WPF owner HWND.</param>
        /// <param name="width">The native surface width in pixels.</param>
        /// <param name="height">The native surface height in pixels.</param>
        /// <param name="output">The destination for native completion measurements.</param>
        /// <param name="adapterIndex">The explicit DXGI selection, or minus one for toolkit selection.</param>
        internal ProbeDriver(nint hwnd, uint width, uint height, ITestOutputHelper output, int adapterIndex = -1) {
            this.output = output;
            probe = new D3DImageInteropProbe(hwnd, width, height, adapterIndex);
            try {
                Assert.Equal(width, probe.Width);
                Assert.Equal(height, probe.Height);
                Assert.NotEqual(nint.Zero, probe.D3D9Surface);
                AdapterIndex = probe.AdapterIndex;
                AdapterLuid = probe.AdapterLuid;
                Assert.True(AdapterIndex >= 0, $"The native probe reported invalid DXGI index {AdapterIndex}.");
                if (adapterIndex >= 0)
                    Assert.Equal(adapterIndex, AdapterIndex);
                output.WriteLine($"Created hardware probe {width}x{height}; D3D9 surface=0x{probe.D3D9Surface:X}; " +
                    $"requested adapter={adapterIndex}; actual DXGI index={AdapterIndex}; LUID=0x{AdapterLuid:X16}.");
            } catch {
                // Construction has not attached a WPF back buffer, so this native owner can be released directly.
                probe.Dispose();
                probe = null;
                throw;
            }
        }

        /// <summary>Gets the actual DXGI adapter selection, retained after disposal for recreation checks.</summary>
        internal int AdapterIndex { get; }

        /// <summary>Gets the actual adapter identity, retained after disposal for recreation checks.</summary>
        internal long AdapterLuid { get; }

        /// <summary>Gets the image published only after both native completion queries succeed.</summary>
        internal D3DImage Image { get; } = new();

        /// <summary>Gets this test driver's explicit submission, copy and publication counts.</summary>
        internal (int Submissions, int Copies, int Publications) Counts => counts;

        /// <summary>Gets whether the driver has detached WPF and disposed its native probe.</summary>
        internal bool IsDisposed => probe is null;

        /// <summary>Copies into a detached slot, then briefly locks WPF to publish the completed immutable surface.</summary>
        /// <param name="red">The straight red channel passed unchanged to the native probe.</param>
        /// <param name="green">The straight green channel passed unchanged to the native probe.</param>
        /// <param name="blue">The straight blue channel passed unchanged to the native probe.</param>
        /// <param name="alpha">The straight alpha; premultiplication belongs to the native D3D12 clear.</param>
        /// <param name="afterCopyStarted">An optional reentrant observation before the new slot is published.</param>
        /// <returns>The completion of exactly one explicit publication.</returns>
        internal async Task PublishAsync(float red, float green, float blue, float alpha,
            Func<Task>? afterCopyStarted = null) {
            var current = probe ?? throw new ObjectDisposedException(nameof(ProbeDriver));
            var clock = Stopwatch.StartNew();
            current.SubmitColor(red, green, blue, alpha);
            counts.Submissions++;
            await PollAsync(() => current.IsRenderComplete, "D3D12 color render");
            var renderTime = clock.Elapsed;

            // The native owner alternates destinations. The surface WPF still owns must remain immutable,
            // including during reentrant dispatcher work before this completed slot is published.
            current.BeginCopyToSurface();
            counts.Copies++;
            var selectedSurface = current.D3D9Surface;
            Assert.NotEqual(nint.Zero, selectedSurface);
            Assert.NotEqual(publishedSurface, selectedSurface);
            if (afterCopyStarted is not null)
                await afterCopyStarted();
            await PollAsync(() => current.IsCopyComplete, "D3D11 copy into detached D3D9 shared surface");
            var copyTime = clock.Elapsed - renderTime;

            // Complete this publication before the next BeginCopyToSurface can reuse the previously attached slot.
            await PollAsync(() => TryWithImageLock(() => {
                Assert.True(Image.IsFrontBufferAvailable, "WPF has no front buffer in this desktop/session.");
                Image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, selectedSurface,
                    enableSoftwareFallback: true);
                Image.AddDirtyRect(new Int32Rect(0, 0, checked((int)current.Width), checked((int)current.Height)));
                counts.Publications++;
                publishedSurface = selectedSurface;
            }), "D3DImage.TryLock for publication");
            output.WriteLine($"Published RGBA=({red},{green},{blue},{alpha}); render={renderTime.TotalMilliseconds:F1} ms; " +
                $"copy/observation={copyTime.TotalMilliseconds:F1} ms; total={clock.Elapsed.TotalMilliseconds:F1} ms; " +
                $"surface=0x{selectedSurface:X}; test driver counts={counts}.");
        }

        /// <summary>Detaches under a brief WPF lock before bounded native teardown, including after a failed copy.</summary>
        /// <returns>The asynchronous lock acquisition and synchronous bounded native teardown.</returns>
        public async ValueTask DisposeAsync() {
            if (probe is not { } current)
                return;
            await PollAsync(() => TryWithImageLock(() => {
                Image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, 0);
                publishedSurface = 0;
            }), "D3DImage.TryLock for detachment");
            var clock = Stopwatch.StartNew();
            current.Dispose();
            probe = null;
            clock.Stop();
            output.WriteLine($"Detached and disposed native probe in {clock.Elapsed.TotalMilliseconds:F1} ms; " +
                $"final test driver counts={counts}.");
            Assert.True(clock.Elapsed < TeardownTimeout, $"Native teardown took {clock.Elapsed}.");
        }

        /// <summary>Balances every normally returning .NET 10 TryLock, including false, without retaining a lock across awaits.</summary>
        /// <param name="action">The synchronous publication or detachment performed only after a successful lock.</param>
        /// <returns>Whether the lock was obtained and the action completed.</returns>
        private bool TryWithImageLock(Action action) {
            var locked = Image.TryLock(new Duration(TimeSpan.Zero));
            try {
                if (!locked)
                    return false;
                action();
                return true;
            } finally {
                Image.Unlock();
            }
        }
    }

    /// <summary>Represents a physical screen coordinate for the native occlusion check.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint {
        /// <summary>The horizontal physical screen coordinate.</summary>
        internal int X;

        /// <summary>The vertical physical screen coordinate.</summary>
        internal int Y;
    }

    /// <summary>Acquires a screen DC only for one-off diagnostic reads of actual desktop composition.</summary>
    /// <param name="window">Zero to acquire the full screen DC.</param>
    /// <returns>The screen DC, or zero if the desktop is inaccessible.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint GetDC(nint window);

    /// <summary>Releases the screen DC even when a diagnostic pixel read fails.</summary>
    /// <param name="window">The zero HWND used to acquire the screen DC.</param>
    /// <param name="dc">The acquired screen DC.</param>
    /// <returns>Whether the DC was released.</returns>
    [DllImport("user32.dll")]
    private static extern int ReleaseDC(nint window, nint dc);

    /// <summary>Reads one already-composed desktop pixel without supplying any CPU pixels to presentation.</summary>
    /// <param name="dc">The diagnostic screen DC.</param>
    /// <param name="x">The physical horizontal screen coordinate.</param>
    /// <param name="y">The physical vertical screen coordinate.</param>
    /// <returns>The native COLORREF in low-byte RGB order, or CLR_INVALID.</returns>
    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(nint dc, int x, int y);

    /// <summary>Identifies an obscuring window so desktop environment obstacles fail explicitly.</summary>
    /// <param name="point">The physical screen pixel to be sampled.</param>
    /// <returns>The topmost HWND at the sampled point, or zero.</returns>
    [DllImport("user32.dll")]
    private static extern nint WindowFromPoint(NativePoint point);
}
