using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Native;

namespace SilkCore.Tests;
public class GraphicsSmokeTests {
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpInitializesEveryTechniqueAndPass() {
        using var effects = new DefaultEffectsManager(new EffectsManagerConfiguration {
            EnableSoftwareRendering = true
        });

        Assert.True(effects.Initialized);
        Assert.Equal(DriverType.Warp, effects.DriverType);
        Assert.NotEmpty(effects.RenderTechniques);

        foreach (var techniqueName in effects.RenderTechniques) {
            var technique = effects[techniqueName];
            Assert.False(technique.IsNull);
            Assert.NotEmpty(technique.ShaderPassNames);
            Assert.All(technique.ShaderPassNames, passName => Assert.False(technique[passName].IsNULL));
        }
    }

    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public void Dx12CreatesAndDisposesBootstrapObjects() {
        using var device = SilkD3D12DeviceFactory.CreateDefault();
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var rootSignature = device.CreateEmptyRootSignature();

        Assert.NotEqual(nint.Zero, device.NativePointer);
        Assert.NotEqual(nint.Zero, queue.NativePointer);
        Assert.NotEqual(nint.Zero, context.CommandListPointer);
        Assert.NotEqual(nint.Zero, fence.NativePointer);
        Assert.NotEqual(nint.Zero, rootSignature.NativePointer);

        context.Reset();
        context.Close();
        Assert.Equal(1UL, queue.Signal(fence));

        rootSignature.Dispose();
        fence.Dispose();
        context.Dispose();
        queue.Dispose();
        device.Dispose();
        Assert.True(device.IsDisposed);
    }
}
