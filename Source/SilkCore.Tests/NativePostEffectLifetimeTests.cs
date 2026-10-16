using Silk.NET.Direct3D12;

namespace HelixToolkit.SharpDX.Core.Native;

/// Verifies physical resource generations remain valid until their recorded native work completes.
public sealed class NativePostEffectLifetimeTests {
    /// Checks same-command-list resize retirement and completed-fence cleanup on WARP.
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpResizeRetirementTest() => VerifyRetirement(SilkDriverType.Warp);

    /// Checks same-command-list resource and descriptor retirement on hardware.
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    public void HardwareResizeRetirementTest() => VerifyRetirement(SilkDriverType.Hardware);

    /// <summary>
    ///     Records clears and readbacks of an old generation before allocating differently sized resources.
    /// </summary>
    /// <param name="driver">The physical adapter selection.</param>
    private static void VerifyRetirement(SilkDriverType driver) {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: driver);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var resources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1024, true);
        using var samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 128, true);
        using var post = new SilkD3D12PostProcessResources(device, resources, samplers);
        using var stencil = new D3D12PostEffectStencil(device);
        context.Reset();
        var oldView = stencil.Begin(context, 16, 16);
        var oldDepth = stencil.Resource!;
        var oldColor = post.BeginTarget(context, 0, 16, 16, oldView);
        var colorFootprint = device.GetCopyableFootprint(oldColor, out var colorBytes);
        var depthFootprint = device.GetCopyableFootprint(oldDepth, out var depthBytes);
        using var colorReadback = device.CreateBuffer(colorBytes, HeapType.Readback);
        using var depthReadback = device.CreateBuffer(depthBytes, HeapType.Readback);
        context.Transition(oldColor, ResourceStates.CopySource);
        context.CopyTextureToBuffer(colorReadback, oldColor, in colorFootprint);
        context.Transition(oldDepth, ResourceStates.CopySource);
        context.CopyTextureToBuffer(depthReadback, oldDepth, in depthFootprint);
        var nextView = stencil.Begin(context, 32, 32);
        post.BeginTarget(context, 0, 32, 32, nextView);
        Assert.False(oldColor.IsDisposed);
        Assert.False(oldDepth.IsDisposed);
        Assert.False(oldView.IsDisposed);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(10));
        var expectedColorRow = new byte[64];
        for (var y = 0; y < 16; y++) {
            var colorRow = colorReadback.Read(64, colorFootprint.Offset + (ulong)y * colorFootprint.Footprint.RowPitch);
            var depthRow = depthReadback.Read(64, depthFootprint.Offset + (ulong)y * depthFootprint.Footprint.RowPitch);
            Assert.Equal(expectedColorRow, colorRow);
            for (var x = 0; x < 16; x++) Assert.Equal(1f, BitConverter.ToSingle(depthRow, x * 4));
        }
        post.BeginFrame();
        stencil.BeginFrame();
        Assert.True(oldColor.IsDisposed);
        Assert.True(oldDepth.IsDisposed);
        Assert.True(oldView.IsDisposed);
        Assert.False(post.First.IsDisposed);
        Assert.False(stencil.Resource!.IsDisposed);
        post.Dispose();
        stencil.Dispose();
        Assert.Equal(0, resources.Count);
        Assert.Equal(0, samplers.Count);
        device.ThrowIfDeviceRemoved();
    }
}
