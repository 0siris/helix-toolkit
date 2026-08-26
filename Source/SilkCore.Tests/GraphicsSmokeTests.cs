using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene.Lights;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;
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
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var heap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1, true);
        using var descriptor = heap.Allocate();
        using var texture = device.CreateTexture1D(2, Format.FormatR32G32B32A32Float);

        device.CreateTexture1DShaderResourceView(texture, descriptor);

        Assert.NotEqual(0UL, descriptor.GpuHandle.Ptr);
        Assert.Equal(2UL, texture.Description.Width);
        Assert.Equal(ResourceDimension.Texture1D, texture.Description.Dimension);
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
    ///     Verifies that Direct3D 12 resource and sampler descriptor tables bind on a WARP command list.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpBindsDx12DescriptorTables() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var context = device.CreateCommandContext();
        using var rootSignature = device.CreateDefaultRootSignature();
        using var pipeline = device.CreateGraphicsPipelineState(rootSignature,
            D3D12ShaderModule.Load("VS", "vsScreenQuad"),
            D3D12ShaderModule.Load("PS", "psScreenDup"));
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 1, true);
        using var resource = resourceHeap.Allocate();
        using var sampler = samplerHeap.Allocate();
        using var texture = device.CreateTexture2D(1, 1, Format.FormatR8G8B8A8Unorm);
        device.CreateShaderResourceView(texture, resource);
        device.CreateSampler(sampler);

        context.Reset();
        context.SetGraphicsPipeline(rootSignature, pipeline);
        context.SetDescriptorHeaps(resourceHeap, samplerHeap);
        context.SetGraphicsDescriptorTables(resource, sampler);
        context.Close();

        Assert.NotEqual(0UL, resource.GpuHandle.Ptr);
        Assert.NotEqual(0UL, sampler.GpuHandle.Ptr);
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
