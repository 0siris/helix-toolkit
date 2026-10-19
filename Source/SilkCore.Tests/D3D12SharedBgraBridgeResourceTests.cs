using HelixToolkit.SharpDX.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Xunit;

namespace SilkCore.Tests;

/// <summary>Verifies the production bridge factory uses the actual device's canonical resource tracker.</summary>
public sealed class D3D12SharedBgraBridgeResourceTests {
    /// <summary>Checks BGRA sharing flags, COMMON transitions, validation and balanced tracker ownership.</summary>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public void SharedSourceHasOneOwningWrapperAndOneDeviceStateEntry() {
        using var device = SilkD3D12DeviceFactory.CreateDefault();
        var initialCount = device.ResourceStates.Count;
        Assert.Throws<ArgumentOutOfRangeException>(() => device.CreateSharedBgraRenderTargetTexture2D(0, 6));
        Assert.Throws<ArgumentOutOfRangeException>(() => device.CreateSharedBgraRenderTargetTexture2D(8, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => device.CreateSharedBgraRenderTargetTexture2D(uint.MaxValue, 6));
        Assert.Throws<ArgumentOutOfRangeException>(() => device.CreateSharedBgraRenderTargetTexture2D(8, uint.MaxValue));
        Assert.Equal(initialCount, device.ResourceStates.Count);

        var source = device.CreateSharedBgraRenderTargetTexture2D(8, 6);
        try {
            var description = source.Description;
            Assert.Equal(initialCount + 1, device.ResourceStates.Count);
            Assert.Equal(ResourceStates.Common, device.ResourceStates.GetState(source.NativePointer));
            Assert.Equal(ResourceDimension.Texture2D, description.Dimension);
            Assert.Equal(8UL, description.Width);
            Assert.Equal(6U, description.Height);
            Assert.Equal(Format.FormatB8G8R8A8Unorm, description.Format);
            Assert.Equal((ushort) 1, description.DepthOrArraySize);
            Assert.Equal((ushort) 1, description.MipLevels);
            Assert.Equal(1U, description.SampleDesc.Count);
            Assert.Equal(ResourceFlags.AllowRenderTarget | ResourceFlags.AllowSimultaneousAccess, description.Flags);
            Assert.Equal(HeapType.Default, source.HeapType);
            Assert.Throws<InvalidOperationException>(() =>
                device.ResourceStates.Track(source.NativePointer, ResourceStates.Common));
            using var context = device.CreateCommandContext();
            context.Reset();
            Assert.True(context.Transition(source, ResourceStates.RenderTarget));
            Assert.Equal(ResourceStates.RenderTarget, source.State);
            Assert.True(context.Transition(source, ResourceStates.Common));
            Assert.Equal(ResourceStates.Common, source.State);
            context.Close();
            // This test records but never submits: no GPU owner exists at resource release.
        } finally {
            source.Dispose();
        }

        source.Dispose();
        Assert.True(source.IsDisposed);
        Assert.Equal(initialCount, device.ResourceStates.Count);
        Assert.False(device.IsDisposed);
    }
}
