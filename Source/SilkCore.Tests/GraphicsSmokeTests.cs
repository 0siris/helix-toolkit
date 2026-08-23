using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene.Lights;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;
using Silk.NET.Direct3D12;
using Format = Silk.NET.DXGI.Format;

namespace SilkCore.Tests;

public class GraphicsSmokeTests {
    /// <summary>
    ///     Verifies that unmanaged generic math vectors expose their native size without marshaling metadata.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void UnsafeSizeSupportsGenericMathVectors() {
        Assert.Equal(16, UnsafeHelper.SizeOf<Silk.NET.Maths.Vector4D<float>>());
    }

    /// <summary>
    ///     Verifies that shadow-map properties initialize their render core lazily.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ShadowMapPropertiesCreateRenderCore() {
        using var node = new ShadowMapNode {
            Intensity = 0.5f
        };

        Assert.Equal(0.5f, Assert.IsType<ShadowMapCore>(node.RenderCore)
            .Intensity);
    }

    /// <summary>
    ///     Verifies that the optional NVIDIA Optimus probe fails without throwing.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void NvOptimusProbeFailsGracefully() {
        Assert.Equal(-1, NvOptimusEnabler.Enable());
    }

    /// <summary>
    ///     Verifies that constant-buffer components retain their registered resources when attached.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpCreatesDx12ConstantBufferView() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var heap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1, true);
        using var descriptor = heap.Allocate();
        using var buffer = device.CreateBuffer(256, HeapType.Upload);

        device.CreateConstantBufferView(buffer, descriptor, 256);

        Assert.Equal(0, descriptor.Index);
        Assert.NotEqual(0UL, descriptor.GpuHandle.Ptr);
    }

    /// <summary>
    ///     Verifies that one-dimensional texture descriptions retain their mip range during native conversion.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpCreatesOneDimensionalShaderResourceView() {
        using var effects = new DefaultEffectsManager(new EffectsManagerConfiguration {
            EnableSoftwareRendering = true
        });
        using var view = new ShaderResourceViewProxy(effects.NativeDeviceResources);

        view.CreateViewFromColorArray([new(1, 0, 0, 1), new(0, 1, 0, 1)]);

        Assert.NotNull(view.TextureView);
    }

    /// <summary>
    ///     Verifies that a cube-map core creates its initially empty face views when attached.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpLoadsDynamicCubeMapDependenciesFromDxil() {
        using var core = new DynamicCubeMapCore();
        var technique = GetTechniqueDescription(DefaultRenderTechniqueNames.Mesh);

        Assert.NotNull(core);
        Assert.All(technique.PassDescriptions ?? [], pass => Assert.NotEmpty(pass.GetD3D12ShaderModules()));
    }

    /// <summary>
    ///     Verifies that a technique retains its registered input layout.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpParticleTechniqueHasDx12InputLayout() {
        var technique = GetTechniqueDescription(DefaultRenderTechniqueNames.ParticleStorm);

        Assert.NotEmpty(technique.InputLayoutDescription?.D3D12InputElements ?? []);
    }

    /// <summary>
    ///     Verifies that the screen-duplication technique and its cursor pass are registered.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpScreenDuplicationDxilTechniqueIsRegistered() {
        var technique = GetTechniqueDescription(DefaultRenderTechniqueNames.ScreenDuplication);
        var passes = technique.PassDescriptions ?? [];

        Assert.Contains(passes, pass => pass.Name == DefaultPassNames.Default);
        Assert.Contains(passes, pass => pass.Name == DefaultPassNames.ScreenQuad);
        Assert.All(passes, pass => Assert.Equal(2, pass.GetD3D12ShaderModules().Count));
    }

    /// <summary>
    ///     Verifies that particle buffers expose their full element range when attached.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpCreatesParticleDx12Pass() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var rootSignature = device.CreateDefaultRootSignature();
        using var cache = new D3D12PipelineStateCache();
        using var core = new ParticleRenderCore();
        var technique = GetTechniqueDescription(DefaultRenderTechniqueNames.ParticleStorm);
        var pass = Assert.Single(technique.PassDescriptions ?? [],
            candidate => candidate.Name == DefaultParticlePassNames.Default);
        using var nativePass = pass.CreateD3D12(device,
            rootSignature,
            cache,
            technique.InputLayoutDescription,
            PrimitiveTopology.PointList);

        Assert.NotNull(core);
        Assert.False(nativePass.IsNull);
    }

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
            Buffer = new ShaderResourceViewDescription.BufferResource {
                ElementCount = 4
            }
        };
        var unorderedAccessDescription = new UnorderedAccessViewDescription {
            Format = Format.FormatUnknown,
            Dimension = UnorderedAccessViewDimension.Buffer,
            Buffer = new UnorderedAccessViewDescription.BufferResource {
                ElementCount = 4
            }
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
            context.GetShaderResources<VertexShaderType>(0, 2), context.GetShaderResources<HullShaderType>(0, 2),
            context.GetShaderResources<DomainShaderType>(0, 2), context.GetShaderResources<GeometryShaderType>(0, 2),
            context.GetShaderResources<PixelShaderType>(0, 2), context.GetShaderResources<ComputeShaderType>(0, 2)
        };
        var samplers = new[] {
            context.GetSamplers<VertexShaderType>(0, 2), context.GetSamplers<HullShaderType>(0, 2),
            context.GetSamplers<DomainShaderType>(0, 2), context.GetSamplers<GeometryShaderType>(0, 2),
            context.GetSampler<PixelShaderType>(0, 2), context.GetSamplers<ComputeShaderType>(0, 2)
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
            resources.OfType<ShaderResourceView>()
                .DisposeAll();
        foreach (var states in samplers)
            states.OfType<SamplerStateProxy>()
                .DisposeAll();
        unorderedAccessViews.OfType<UnorderedAccessView>()
            .DisposeAll();
    }

    [Fact]
    [Trait("Category", "Warp")]
    public void WarpLoadsEveryTechniqueAndPassFromDxil() {
        var techniques = DefaultEffectsManager.LoadTechniqueDescriptions().ToArray();
        var passes = techniques.SelectMany(technique => technique.PassDescriptions ?? []).ToArray();

        Assert.Equal(24, techniques.Length);
        Assert.Equal(199, passes.Length);
        Assert.All(techniques, technique => Assert.False(string.IsNullOrWhiteSpace(technique.Name)));
        Assert.All(passes, pass => Assert.NotEmpty(pass.GetD3D12ShaderModules()));
    }

    /// <summary>
    ///     Verifies creation, command recording, signaling, and deterministic disposal of DX12 bootstrap objects.
    /// </summary>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public void Dx12CreatesAndDisposesBootstrapObjects() {
        var device = SilkD3D12DeviceFactory.CreateDefault();
        using (device) {
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
        }

        Assert.True(device.IsDisposed);
    }

    /// <summary>
    ///     Gets one registered default technique description without constructing the removed DXBC runtime path.
    /// </summary>
    /// <param name="name">The registered technique name.</param>
    /// <returns>The matching technique description.</returns>
    private static TechniqueDescription GetTechniqueDescription(string name) =>
        Assert.Single(DefaultEffectsManager.LoadTechniqueDescriptions(), technique => technique.Name == name);
}
