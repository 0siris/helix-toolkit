using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>
///     Verifies actual hidden HWND hit testing and queued wheel ownership without a renderer or visible window.
/// </summary>
[Collection(WpfCollection.Name)]
public sealed class NativeWheelOwnershipTests {
    /// <summary>
    ///     Keeps wheel input in the native surface instead of propagating it to its WPF owner.
    /// </summary>
    /// <param name="delta">The signed native wheel delta.</param>
    /// <returns>The completion of the hidden-window dispatcher check.</returns>
    [Theory]
    [InlineData(-120)]
    [InlineData(120)]
    [InlineData(240)]
    [Trait("Category", "Wpf")]
    public Task NativeHitAndQueuedWheelHaveSingleOwnerTest(int delta) => StaThread.RunAsync(async () => {
        using var source = new HwndSource(new HwndSourceParameters(nameof(NativeWheelOwnershipTests)) {
            Width = 32, Height = 24, WindowStyle = unchecked((int) 0x80000000)
        });
        using var host = new HwndSwapChainHost();
        var nativeWheels = new List<HwndPointerEventArgs>();
        var parentWheels = 0;
        host.PointerInput += (_, args) => {
            if (args.Action == HwndPointerAction.Wheel)
                nativeWheels.Add(args);
        };
        source.AddHook((nint window, int message, nint word, nint position, ref bool handled) => {
            if (message == 0x020A)
                parentWheels++;
            return 0;
        });
        source.RootVisual = host;
        try {
            host.UpdateLayout();
            var screen = host.PointToScreen(new Point(8, 6));
            var expected = host.PointFromScreen(new Point((ushort) screen.X, (ushort) screen.Y));
            var position = (nint) ((uint) (ushort) screen.X | ((uint) (ushort) screen.Y << 16));
            var hit = SendMessage(host.NativeHandle, 0x0084, 0, position);
            host.IsHitTestVisible = false;
            var noninteractiveHit = SendMessage(host.NativeHandle, 0x0084, 0, position);
            host.IsHitTestVisible = true;
            var posted = PostMessage(host.NativeHandle, 0x020A, (nint) (delta << 16), position);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var received = nativeWheels.ToArray();

            Assert.True(posted);
            Assert.True(hit == 1, $"Native hit={hit}, decoded wheels={received.Length}, parent wheels={parentWheels}");
            Assert.Equal(-1, noninteractiveHit);
            var wheel = Assert.Single(received);
            Assert.Equal(delta, wheel.WheelDelta);
            Assert.Equal(expected.X, wheel.X, 2);
            Assert.Equal(expected.Y, wheel.Y, 2);
            Assert.Equal(0, parentWheels);
        } finally {
            source.RootVisual = null;
        }
    });

    /// <summary>Reads the real native window procedure's result before default handling is lost.</summary>
    /// <param name="window">The receiving child HWND.</param>
    /// <param name="message">The native hit-test message.</param>
    /// <param name="word">The unused native word.</param>
    /// <param name="position">The packed screen point.</param>
    /// <returns>The actual native hit-test result.</returns>
    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern nint SendMessage(nint window, uint message, nint word, nint position);

    /// <summary>Queues a wheel so dispatcher delivery and native default propagation are exercised.</summary>
    /// <param name="window">The receiving child HWND.</param>
    /// <param name="message">The native wheel message.</param>
    /// <param name="word">The packed signed wheel delta.</param>
    /// <param name="position">The packed screen point.</param>
    /// <returns>Whether the message was queued.</returns>
    [DllImport("user32.dll", EntryPoint = "PostMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint window, uint message, nint word, nint position);
}
