using System.Reflection;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Native;
using Silk.NET.Direct3D12;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkCore.Tests;

/// <summary>Checks real particle texture registrations across repeated attachment and completed-GPU cleanup.</summary>
public sealed class NativeParticleTextureLifetimeTests {
    /// <summary>Releases an unchanged texture after each detach, including after reuse of the same particle resources.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void ReattachedParticleTextureRetainsItsOwnerGuard() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var commands = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var descriptors = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 512, true);
        using var samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 64, true);
        using var resources = new SilkD3D12ResourceManager(device, descriptors);
        using var particles = new SilkD3D12ParticleResources(device, descriptors, samplers, 8);
        using var core = new ParticleRenderCore { ParticleTexture = new TextureModel([Vector4.One], 1, 1) };
        var bind = typeof(SilkD3D12ParticleResources)
            .GetMethod("BindTexture", BindingFlags.Instance | BindingFlags.NonPublic)!
            .CreateDelegate<Action<SilkD3D12CommandContext, SilkD3D12ResourceManager, ParticleRenderCore>>(particles);
        for (var cycle = 0; cycle < 2; cycle++) {
            core.Attach();
            commands.Reset();
            bind(commands, resources, core);
            commands.Close();
            queue.Execute(commands);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));
            device.ThrowIfDeviceRemoved();
            Assert.Equal(1, resources.TextureCount);
            core.Detach();
            particles.ReleaseDisposedTextures();
            resources.ReleaseDisposedResources();
            Assert.Equal(0, resources.TextureCount);
        }
    }
}