using System.Windows.Interop;
using HelixToolkit.SharpDX.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>
///     Verifies the Direct3D 12 flip-model presentation path against WARP and a raw HWND.
/// </summary>
[Collection(WpfCollection.Name)]
public sealed class D3D12SwapChainTests {
    /// <summary>
    ///     Verifies clear, readback, present, resize, and back-buffer rotation on a three-buffer swap chain.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    [Trait("Category", "Wpf")]
    public Task WarpClearsPresentsAndResizesSwapChain() {
        return StaThread.RunAsync(() => {
            using var window = new HwndSource(new HwndSourceParameters(nameof(D3D12SwapChainTests)) {
                Width = 32,
                Height = 24,
                WindowStyle = unchecked((int) 0x80000000)
            });
            using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110,
                SilkDriverType.Warp);
            using var queue = device.CreateCommandQueue();
            using var context = device.CreateCommandContext();
            using var fence = device.CreateFence();
            using var swapChain = new SilkD3D12SwapChain(device, queue, window.Handle, 32, 24);
            var initialIndex = swapChain.CurrentBackBufferIndex;
            var backBuffer = swapChain.CurrentBackBuffer;
            var footprint = device.GetCopyableFootprint(backBuffer, out var totalBytes);
            using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);

            context.Reset();
            Assert.True(context.Transition(backBuffer, ResourceStates.RenderTarget));
            context.ClearRenderTarget(backBuffer, swapChain.CurrentRenderTargetView, [0, 0, 1, 1]);
            Assert.True(context.Transition(backBuffer, ResourceStates.CopySource));
            context.CopyTextureToBuffer(readback, backBuffer, in footprint);
            Assert.True(context.Transition(backBuffer, ResourceStates.Present));
            context.Close();

            queue.Execute(context);
            var fenceValue = queue.Signal(fence);
            fence.Wait(fenceValue, TimeSpan.FromSeconds(5));
            Assert.Equal([0, 0, 255, 255], readback.Read(4));

            swapChain.Present(0);
            Assert.NotEqual(initialIndex, swapChain.CurrentBackBufferIndex);
            fenceValue = queue.Signal(fence);
            fence.Wait(fenceValue, TimeSpan.FromSeconds(5));

            var beforeNoOpResize = swapChain.CurrentBackBuffer.NativePointer;
            swapChain.Resize(32, 24);
            Assert.Equal(beforeNoOpResize, swapChain.CurrentBackBuffer.NativePointer);
            Assert.Throws<ArgumentOutOfRangeException>(() => swapChain.Resize(0, 24));

            swapChain.Resize(80, 60);
            Assert.Equal(80U, swapChain.Width);
            Assert.Equal(60U, swapChain.Height);
            Assert.Equal(80UL, swapChain.CurrentBackBuffer.Description.Width);
            Assert.Equal(60U, swapChain.CurrentBackBuffer.Description.Height);

            context.Reset();
            Assert.True(context.Transition(swapChain.CurrentBackBuffer, ResourceStates.RenderTarget));
            context.ClearRenderTarget(swapChain.CurrentBackBuffer,
                swapChain.CurrentRenderTargetView,
                [0, 1, 0, 1]);
            Assert.True(context.Transition(swapChain.CurrentBackBuffer, ResourceStates.Present));
            context.Close();
            queue.Execute(context);
            fenceValue = queue.Signal(fence);
            fence.Wait(fenceValue, TimeSpan.FromSeconds(5));
            swapChain.Present(0);
            device.ThrowIfDeviceRemoved();
        });
    }

    /// <summary>
    ///     Verifies swap-chain construction and disposed calls reject invalid lifecycle use.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    [Trait("Category", "Wpf")]
    public Task WarpRejectsInvalidSwapChainLifecycle() {
        return StaThread.RunAsync(() => {
            using var window = new HwndSource(new HwndSourceParameters(nameof(D3D12SwapChainTests)) {
                Width = 8,
                Height = 8,
                WindowStyle = unchecked((int) 0x80000000)
            });
            using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110,
                SilkDriverType.Warp);
            using var queue = device.CreateCommandQueue();

            Assert.Throws<ArgumentException>(() => new SilkD3D12SwapChain(device, queue, 0, 8, 8));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SilkD3D12SwapChain(device, queue, window.Handle, 0, 8));

            var swapChain = new SilkD3D12SwapChain(device, queue, window.Handle, 8, 8);
            swapChain.Dispose();

            Assert.True(swapChain.IsDisposed);
            Assert.Throws<ObjectDisposedException>(() => swapChain.Present(0));
            Assert.Throws<ObjectDisposedException>(() => swapChain.Resize(16, 16));
        });
    }
}
