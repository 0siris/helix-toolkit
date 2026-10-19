using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Checks the passive canvas's managed geometry, notification and image lifetime on a hidden STA.</summary>
[Collection(WpfCollection.Name)]
public sealed class PassiveDpfCanvasTests
{
    /// <summary>Checks real 200-percent root DPI, fractional layout rounding and unchanged DIP coordinates.</summary>
    /// <returns>The headless layout verification.</returns>
    [Fact]
    [Trait("Category", "Wpf")]
    public Task PhysicalDpiChangesPixelsWithoutChangingDipLayout() => StaThread.RunAsync(() =>
    {
        using var canvas = new DPFCanvas();
        Assert.Single(canvas.Children);
        var image = Assert.IsType<Image>(canvas.Children[0]);
        Assert.Same(canvas.ImageSource.NativeImage, image.Source);
        Assert.Equal(Stretch.Fill, image.Stretch);
        Assert.True(canvas.EnableDpiScale);
        Assert.False(canvas.IsActive);
        Assert.Equal(nint.Zero, canvas.NativeHandle);

        var inputTarget = new Border
        {
            Width = 16,
            Height = 12,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        canvas.Children.Add(inputTarget);

        VisualTreeHelper.SetRootDpi(canvas, new DpiScale(2, 2));
        var size = new Size(63.25, 47.25);
        canvas.Measure(size);
        canvas.Arrange(new Rect(size));
        Assert.Equal(2f, canvas.EffectiveDpiScale);
        Assert.Equal(127u, canvas.PixelWidth);
        Assert.Equal(95u, canvas.PixelHeight);
        Assert.Equal(size, canvas.RenderSize);
        var input = new Point(12.5, 9.25);
        Assert.Equal(input, inputTarget.TranslatePoint(input, canvas));

        canvas.EnableDpiScale = false;
        Assert.Equal(1f, canvas.EffectiveDpiScale);
        Assert.Equal(64u, canvas.PixelWidth);
        Assert.Equal(48u, canvas.PixelHeight);
        Assert.Equal(size, canvas.RenderSize);
        Assert.Equal(input, inputTarget.TranslatePoint(input, canvas));

        canvas.EnableDpiScale = true;
        VisualTreeHelper.SetRootDpi(canvas, new DpiScale(1.5, 1.5));
        Assert.Equal(1.5f, canvas.EffectiveDpiScale);
        Assert.Equal(95u, canvas.PixelWidth);
        Assert.Equal(71u, canvas.PixelHeight);
        Assert.Equal(size, canvas.RenderSize);
    });

    /// <summary>Checks coalesced changes, actual WPF HWND identity and cancellation across unload/reload.</summary>
    /// <returns>The hidden-source lifecycle verification without native rendering.</returns>
    [Fact]
    [Trait("Category", "Wpf")]
    public Task HiddenSourceCoalescesChangesAndCancelsUnloadedCallbacks() => StaThread.RunAsync(async () =>
    {
        using var source = CreateHiddenSource();
        using var canvas = new DPFCanvas();
        var changes = 0;
        source.RootVisual = canvas;
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Assert.True(canvas.IsLoaded);
        Assert.Equal(source.Handle, canvas.NativeHandle);
        Assert.True(canvas.IsActive);
        canvas.PresentationChanged += (_, _) => changes++;

        canvas.Width = 48;
        canvas.Height = 32;
        canvas.UpdateLayout();
        canvas.Width = 50;
        canvas.Height = 34;
        canvas.UpdateLayout();
        canvas.EnableDpiScale = false;
        Assert.Equal(0, changes);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Assert.Equal(1, changes);
        Assert.Equal(50u, canvas.PixelWidth);
        Assert.Equal(34u, canvas.PixelHeight);

        canvas.Visibility = Visibility.Hidden;
        Assert.False(canvas.IsActive);
        canvas.Visibility = Visibility.Visible;
        Assert.True(canvas.IsActive);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Assert.Equal(2, changes);

        canvas.Width = 52;
        canvas.UpdateLayout();
        canvas.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
        Assert.False(canvas.IsActive);
        Assert.Equal(nint.Zero, canvas.NativeHandle);
        Assert.Equal(nint.Zero, canvas.ImageSource.BoundSurface);
        var unloadedChanges = changes;
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Assert.Equal(unloadedChanges, changes);

        canvas.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
        canvas.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Assert.Equal(unloadedChanges + 1, changes);
        Assert.Equal(source.Handle, canvas.NativeHandle);
        Assert.True(canvas.IsActive);

        canvas.Dispose();
        Assert.False(canvas.IsActive);
        Assert.Empty(canvas.Children);
        var disposedChanges = changes;
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Assert.Equal(disposedChanges, changes);
        source.RootVisual = null;
    });

    /// <summary>Checks original event identity, duplicate timestamps and warmed allocation-free tick forwarding.</summary>
    /// <returns>The explicit hidden-source event verification.</returns>
    [Fact]
    [Trait("Category", "Wpf")]
    public Task RenderingForwardsOriginalArgumentsOnceWithoutWarmAllocations() => StaThread.RunAsync(async () =>
    {
        using var source = CreateHiddenSource();
        using var canvas = new DPFCanvas();
        source.RootVisual = canvas;
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Assert.True(canvas.IsActive);
        var callback = typeof(DPFCanvas).GetMethod("OnRendering", BindingFlags.NonPublic | BindingFlags.Instance)!
            .CreateDelegate<EventHandler>(canvas);
        var forwarded = 0;
        RenderingEventArgs? last = null;
        canvas.Rendering += (_, args) =>
        {
            forwarded++;
            last = args;
        };
        var original = CreateRenderingArgs(TimeSpan.FromDays(1));
        callback(null, original);
        callback(null, original);
        Assert.Equal(1, forwarded);
        Assert.Same(original, last);

        var timestamps = new RenderingEventArgs[1024];
        for (var index = 0; index < timestamps.Length; index++)
            timestamps[index] = CreateRenderingArgs(TimeSpan.FromDays(2) + TimeSpan.FromTicks(index));
        for (var repeat = 0; repeat < 4; repeat++)
        {
            foreach (var args in timestamps)
                callback(null, args);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        foreach (var args in timestamps)
        {
            callback(null, args);
            callback(null, args);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0L, allocated);
        Assert.Equal(1 + timestamps.Length * 5, forwarded);
        Assert.Same(timestamps[^1], last);

        canvas.Visibility = Visibility.Hidden;
        callback(null, original);
        Assert.Equal(1 + timestamps.Length * 5, forwarded);
        canvas.Visibility = Visibility.Visible;
        canvas.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
        callback(null, original);
        Assert.Equal(1 + timestamps.Length * 5, forwarded);
        source.RootVisual = null;
    });

    /// <summary>Checks pointer validation, repeatable empty detachment and stable D3DImage identity after disposal.</summary>
    /// <returns>The headless image-source ownership verification.</returns>
    [Fact]
    [Trait("Category", "Wpf")]
    public Task EmptyImageDetachesAndRetainsItsInspectableNativeImage() => StaThread.RunAsync(() =>
    {
        using var image = new D3D12ImageSource();
        var nativeImage = image.NativeImage;
        Assert.Equal(nint.Zero, image.BoundSurface);
        Assert.Equal(nativeImage.IsFrontBufferAvailable, image.IsFrontBufferAvailable);
        Assert.Throws<ArgumentException>(() => image.TryPublish(0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => image.TryPublish(1, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => image.TryPublish(1, 1, uint.MaxValue));
        Assert.True(image.TryDetach());
        Assert.True(image.TryDetach());
        image.Dispose();
        image.Dispose();
        Assert.True(image.TryDetach());
        Assert.Same(nativeImage, image.NativeImage);
        Assert.Equal(nint.Zero, image.BoundSurface);
        Assert.Throws<ObjectDisposedException>(() => image.TryPublish(1, 1, 1));
    });

    /// <summary>Creates an actual WPF source HWND without making a desktop window visible.</summary>
    /// <returns>The caller-owned hidden source used only for lifecycle and handle checks.</returns>
    private static HwndSource CreateHiddenSource() => new(new HwndSourceParameters(nameof(PassiveDpfCanvasTests))
    {
        Width = 64,
        Height = 48,
        WindowStyle = unchecked((int)0x80000000)
    });

    /// <summary>Creates explicit timestamps through WPF's nonpublic event-argument constructor, outside measured work.</summary>
    /// <param name="timestamp">The desired distinct or duplicate composition timestamp.</param>
    /// <returns>The event object passed unchanged through the canvas callback.</returns>
    private static RenderingEventArgs CreateRenderingArgs(TimeSpan timestamp) =>
        (RenderingEventArgs)Activator.CreateInstance(typeof(RenderingEventArgs),
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, [timestamp], null)!;
}
