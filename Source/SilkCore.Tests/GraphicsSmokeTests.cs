using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;
using Format = Silk.NET.DXGI.Format;

namespace SilkCore.Tests;
public class GraphicsSmokeTests {
    /// <summary>
    ///     Verifies that shader-state getters preserve the requested slot range on a WARP context.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRoundTripsShaderStateSlots() {
        using var effects = new DefaultEffectsManager(new EffectsManagerConfiguration {
            EnableSoftwareRendering = true
        });
        using var context = new DeviceContextProxy(
            effects.NativeDeviceResources.ImmediateContext,
            effects.NativeDeviceResources.Device);
        using var sampler = effects.StateManager.Register(DefaultSamplers.PointSamplerWrap);
        var bufferDescription = new BufferDescription {
            SizeInBytes = 16,
            BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
            OptionFlags = ResourceOptionFlags.BufferStructured,
            StructureByteStride = 4
        };
        var shaderResourceDescription = new ShaderResourceViewDescription {
            Format = Format.FormatUnknown,
            Dimension = ShaderResourceViewDimension.Buffer,
            Buffer = new ShaderResourceViewDescription.BufferResource {ElementCount = 4}
        };
        var unorderedAccessDescription = new UnorderedAccessViewDescription {
            Format = Format.FormatUnknown,
            Dimension = UnorderedAccessViewDimension.Buffer,
            Buffer = new UnorderedAccessViewDescription.BufferResource {ElementCount = 4}
        };
        using var views = new UavBufferViewProxy(
            context,
            ref bufferDescription,
            ref unorderedAccessDescription,
            ref shaderResourceDescription);
        var shaderResource = views.Srv?.TextureView
                             ?? throw new InvalidOperationException("The test shader-resource view was not created.");

        context.SetShaderResource<VertexShaderType>(0, shaderResource);
        context.SetShaderResource<HullShaderType>(0, shaderResource);
        context.SetShaderResource<DomainShaderType>(0, shaderResource);
        context.SetShaderResource<GeometryShaderType>(0, shaderResource);
        context.SetShaderResource<PixelShaderType>(0, shaderResource);
        context.SetShaderResource<ComputeShaderType>(0, shaderResource);
        context.SetSampler<VertexShaderType>(0, sampler);
        context.SetSampler<HullShaderType>(0, sampler);
        context.SetSampler<DomainShaderType>(0, sampler);
        context.SetSampler<GeometryShaderType>(0, sampler);
        context.SetSampler<PixelShaderType>(0, sampler);
        context.SetSampler<ComputeShaderType>(0, sampler);

        var shaderResources = new[] {
            context.GetShaderResources<VertexShaderType>(0, 2),
            context.GetShaderResources<HullShaderType>(0, 2),
            context.GetShaderResources<DomainShaderType>(0, 2),
            context.GetShaderResources<GeometryShaderType>(0, 2),
            context.GetShaderResources<PixelShaderType>(0, 2),
            context.GetShaderResources<ComputeShaderType>(0, 2)
        };
        var samplers = new[] {
            context.GetSamplers<VertexShaderType>(0, 2),
            context.GetSamplers<HullShaderType>(0, 2),
            context.GetSamplers<DomainShaderType>(0, 2),
            context.GetSamplers<GeometryShaderType>(0, 2),
            context.GetSampler<PixelShaderType>(0, 2),
            context.GetSamplers<ComputeShaderType>(0, 2)
        };
        context.SetUnorderedAccessView<ComputeShaderType>(0, views.Uav);
        var unorderedAccessViews = context.GetUnorderedAccessView<ComputeShaderType>(0, 2);

        Assert.All(shaderResources, resources => {
            Assert.Equal(2, resources.Length);
            Assert.Equal(shaderResource.NativePointer, resources[0]?.NativePointer);
            Assert.Null(resources[1]);
        });
        Assert.All(samplers, states => {
            Assert.Equal(2, states.Length);
            Assert.Equal(sampler.State?.NativePointer, states[0]?.State?.NativePointer);
            Assert.Null(states[1]);
        });
        Assert.Equal(2, unorderedAccessViews.Length);
        Assert.Equal(views.Uav.NativePointer, unorderedAccessViews[0]?.NativePointer);
        Assert.Null(unorderedAccessViews[1]);

        foreach (var resources in shaderResources)
            resources.OfType<ShaderResourceView>().DisposeAll();
        foreach (var states in samplers)
            states.OfType<SamplerStateProxy>().DisposeAll();
        unorderedAccessViews.OfType<UnorderedAccessView>().DisposeAll();
    }

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
            Assert.All(technique.ShaderPassNames, passName => Assert.False(technique[passName].IsNull));
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


        Assert.True(device.IsDisposed);
    }
}
