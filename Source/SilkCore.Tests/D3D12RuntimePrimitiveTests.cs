using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.Buffers;
using HelixToolkit.SharpDX.Core.Core2D;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Lights;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Model.Scene.Lights;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector2 = Silk.NET.Maths.Vector2D<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkCore.Tests;

/// <summary>
///     Verifies deterministic Direct3D 12 runtime bookkeeping without requiring graphics hardware.
/// </summary>
public class D3D12RuntimePrimitiveTests {
    /// <summary>
    ///     Verifies adapter ranking ignores software and unsupported devices and prefers dedicated memory.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void AdapterSelectionPicksStrongestSupportedHardware() {
        D3D12AdapterCandidate[] candidates = [
            new(0, false, 2_000, true),
            new(1, true, 8_000, true),
            new(2, false, 16_000, false),
            new(3, false, 4_000, true)
        ];

        Assert.Equal(3, D3D12AdapterSelector.Select(candidates));
    }

    /// <summary>
    ///     Verifies adapter ranking reports the absence of suitable hardware.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void AdapterSelectionRejectsMissingHardware() {
        D3D12AdapterCandidate[] candidates = [new(0, true, 8_000, true), new(1, false, 4_000, false)];

        Assert.Throws<PlatformNotSupportedException>(() => D3D12AdapterSelector.Select(candidates));
    }

    /// <summary>
    ///     Verifies the shared root signature covers every register used by current shaders.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void DefaultRootSignatureCoversCurrentRegisterRanges() {
        Assert.Collection(D3D12DefaultRootSignatureLayout.ResourceRanges,
            range => Assert.Equal(new D3D12DescriptorRangeDescription(DescriptorRangeType.Cbv, 0, 10), range),
            range => Assert.Equal(new D3D12DescriptorRangeDescription(DescriptorRangeType.Srv, 0, 103), range),
            range => Assert.Equal(new D3D12DescriptorRangeDescription(DescriptorRangeType.Uav, 0, 5), range));
        Assert.Equal(new D3D12DescriptorRangeDescription(DescriptorRangeType.Sampler, 0, 10),
            D3D12DefaultRootSignatureLayout.SamplerRange);
    }

    /// <summary>
    ///     Verifies shared root-signature ranges are non-empty and do not overlap within a register class.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void DefaultRootSignatureRangesDoNotCollide() {
        var ranges = D3D12DefaultRootSignatureLayout.ResourceRanges
            .Append(D3D12DefaultRootSignatureLayout.SamplerRange)
            .ToArray();

        Assert.All(ranges, range => Assert.NotEqual(0u, range.DescriptorCount));
        foreach (var group in ranges.GroupBy(range => range.Type)) {
            var ordered = group.OrderBy(range => range.BaseShaderRegister).ToArray();
            for (var index = 1; index < ordered.Length; index++)
                Assert.True(ordered[index - 1].BaseShaderRegister + ordered[index - 1].DescriptorCount <=
                            ordered[index].BaseShaderRegister);
        }
    }

    /// <summary>
    ///     Verifies unsupported Direct3D 12 driver and feature-level requests fail before native creation.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void DeviceFactoryRejectsUnsupportedRequests() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Reference));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Unknown));
    }

    /// <summary>
    ///     Verifies the shared default-mesh vertex preparation supplies absent optional streams and rejects
    ///     incomplete streams.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void DefaultMeshVertexPreparationValidatesOptionalStreams() {
        var geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([new Vector3(1, 2, 3), new Vector3(4, 5, 6)])
        };

        var vertices = DefaultMeshGeometryBufferModel.BuildVertexArray(geometry);

        Assert.Equal(new Vector4(1, 2, 3, 1), vertices[0].Position);
        Assert.Equal(Vector3.Zero, vertices[0].Normal);
        Assert.Equal(Vector3.Zero, vertices[0].Tangent);
        Assert.Equal(Vector3.Zero, vertices[0].BiTangent);

        geometry.Normals = new Vector3Collection([Vector3.UnitY]);
        var exception = Assert.Throws<ArgumentException>(() =>
            DefaultMeshGeometryBufferModel.BuildVertexArray(geometry));
        Assert.Contains(nameof(geometry.Normals), exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies line and point preparation supplies default colors and rejects incomplete color streams.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void PointLineVertexPreparationValidatesColors() {
        var line = new LineGeometry3D {
            Positions = new Vector3Collection([new Vector3(1, 2, 3), new Vector3(4, 5, 6)])
        };
        var point = new PointGeometry3D {
            Positions = new Vector3Collection([Vector3.Zero, Vector3.One]),
            Colors = new Color4Collection([new Vector4(1, 0, 0, 1), new Vector4(0, 1, 0, 1)])
        };

        var lineVertices = DefaultLineGeometryBufferModel.BuildVertexArray(line);
        var pointVertices = DefaultPointGeometryBufferModel.BuildVertexArray(point);

        Assert.Equal(new Vector4(1, 2, 3, 1), lineVertices[0].Position);
        Assert.Equal(Vector4.One, lineVertices[0].Color);
        Assert.Equal(new Vector4(0, 1, 0, 1), pointVertices[1].Color);

        line.Colors = new Color4Collection([Vector4.One]);
        point.Colors = new Color4Collection([Vector4.One]);
        Assert.Throws<ArgumentException>(() => DefaultLineGeometryBufferModel.BuildVertexArray(line));
        Assert.Throws<ArgumentException>(() => DefaultPointGeometryBufferModel.BuildVertexArray(point));
    }

    /// <summary>
    ///     Verifies sequential descriptor allocation and deterministic index reuse.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void DescriptorIndicesAllocateAndReuseReleasedSlots() {
        var allocator = new D3D12DescriptorIndexAllocator(3);

        Assert.Equal(0, allocator.Allocate());
        Assert.Equal(1, allocator.Allocate());
        Assert.Equal(2, allocator.Count);

        allocator.Release(0);

        Assert.Equal(0, allocator.Allocate());
        Assert.Equal(2, allocator.Count);
    }

    /// <summary>
    ///     Verifies descriptor capacity, exhaustion, invalid release, and duplicate release guards.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void DescriptorIndicesRejectInvalidOperations() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new D3D12DescriptorIndexAllocator(0));

        var allocator = new D3D12DescriptorIndexAllocator(1);
        Assert.Equal(0, allocator.Allocate());
        Assert.Throws<InvalidOperationException>(() => allocator.Allocate());
        Assert.Throws<ArgumentOutOfRangeException>(() => allocator.Release(1));

        allocator.Release(0);
        Assert.Throws<InvalidOperationException>(() => allocator.Release(0));
    }

    /// <summary>
    ///     Verifies contiguous descriptor ranges survive reuse and reject fragmented or oversized requests.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void DescriptorIndicesAllocateContiguousRanges() {
        var allocator = new D3D12DescriptorIndexAllocator(6);

        Assert.Equal(0, allocator.AllocateRange(3));
        Assert.Equal(3, allocator.Allocate());
        allocator.Release(1);
        allocator.Release(2);
        Assert.Equal(1, allocator.AllocateRange(2));
        Assert.Equal(4, allocator.Count);
        Assert.Throws<InvalidOperationException>(() => allocator.AllocateRange(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => allocator.AllocateRange(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => allocator.AllocateRange(7));
    }

    /// <summary>
    ///     Verifies state changes and redundant transition suppression.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ResourceStatesReturnOnlyRequiredTransitions() {
        var tracker = new D3D12ResourceStateTracker();
        var resource = (nint) 42;
        tracker.Track(resource, ResourceStates.Common);

        Assert.False(tracker.TryTransition(resource, ResourceStates.Common, out _));
        Assert.True(tracker.TryTransition(resource, ResourceStates.CopyDest, out var transition));
        Assert.Equal(resource, transition.Resource);
        Assert.Equal(ResourceStates.Common, transition.Before);
        Assert.Equal(ResourceStates.CopyDest, transition.After);
        Assert.False(tracker.TryTransition(resource, ResourceStates.CopyDest, out _));
    }

    /// <summary>
    ///     Verifies state tracking rejects invalid, duplicate, and unknown resources.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ResourceStatesRejectInvalidTrackingOperations() {
        var tracker = new D3D12ResourceStateTracker();
        var resource = (nint) 42;

        Assert.Throws<ArgumentException>(() => tracker.Track(nint.Zero, ResourceStates.Common));
        tracker.Track(resource, ResourceStates.Common);
        Assert.Throws<InvalidOperationException>(() => tracker.Track(resource, ResourceStates.Common));
        Assert.True(tracker.Untrack(resource));
        Assert.False(tracker.Untrack(resource));
        Assert.Throws<InvalidOperationException>(() =>
            tracker.TryTransition(resource, ResourceStates.CopySource, out _));
    }

    /// <summary>
    ///     Verifies constant-buffer alignment, mip counts, and subresource indexing at their boundaries.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ResourceLayoutsFollowD3D12AlignmentAndSubresourceRules() {
        Assert.Equal(256UL, D3D12ResourceLayout.AlignConstantBufferSize(1));
        Assert.Equal(256UL, D3D12ResourceLayout.AlignConstantBufferSize(256));
        Assert.Equal(512UL, D3D12ResourceLayout.AlignConstantBufferSize(257));
        Assert.Throws<ArgumentOutOfRangeException>(() => D3D12ResourceLayout.AlignConstantBufferSize(0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            D3D12ResourceLayout.AlignConstantBufferSize(ulong.MaxValue));

        Assert.Equal(1U, D3D12ResourceLayout.CalculateMipLevels(1));
        Assert.Equal(4U, D3D12ResourceLayout.CalculateMipLevels(8, 4, 2));
        Assert.Equal(11U, D3D12ResourceLayout.CalculateMipLevels(1920, 1080));
        Assert.Throws<ArgumentOutOfRangeException>(() => D3D12ResourceLayout.CalculateMipLevels(0));

        Assert.Equal(0U, D3D12ResourceLayout.CalculateSubresource(0, 0, 0, 4, 3));
        Assert.Equal(6U, D3D12ResourceLayout.CalculateSubresource(2, 1, 0, 4, 3));
        Assert.Equal(18U, D3D12ResourceLayout.CalculateSubresource(2, 1, 1, 4, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            D3D12ResourceLayout.CalculateSubresource(4, 0, 0, 4, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            D3D12ResourceLayout.CalculateSubresource(0, 3, 0, 4, 3));
    }

    /// <summary>
    ///     Verifies resources are disposed only after their protecting fence completes.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void DeferredReleaseWaitsForCompletedFence() {
        using var queue = new D3D12DeferredReleaseQueue();
        var first = new TrackedDisposable();
        var second = new TrackedDisposable();
        queue.Enqueue(2, first);
        queue.Enqueue(4, second);

        Assert.Equal(0, queue.ReleaseCompleted(1));
        Assert.False(first.IsDisposed);
        Assert.Equal(1, queue.ReleaseCompleted(2));
        Assert.True(first.IsDisposed);
        Assert.False(second.IsDisposed);
        Assert.Equal(1, queue.Count);
    }

    /// <summary>
    ///     Verifies fence ordering and queue disposal safeguards.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void DeferredReleaseRejectsRegressingFencesAndDisposesRemainder() {
        var queue = new D3D12DeferredReleaseQueue();
        var resource = new TrackedDisposable();
        queue.Enqueue(2, resource);

        Assert.Throws<ArgumentOutOfRangeException>(() => queue.Enqueue(1, new TrackedDisposable()));

        queue.Dispose();
        Assert.True(resource.IsDisposed);
        Assert.Equal(0, queue.Count);
    }

    /// <summary>
    ///     Verifies frame slots retain their protecting fences across ring rotation.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void FrameFencesReturnValueProtectingNextSlot() {
        var frames = new D3D12FrameFenceTracker(3);

        Assert.Equal(0UL, frames.Advance(1));
        Assert.Equal(1, frames.CurrentIndex);
        Assert.Equal(0UL, frames.Advance(2));
        Assert.Equal(1UL, frames.Advance(3));
        Assert.Equal(2UL, frames.Advance(4));
        Assert.Equal(1, frames.CurrentIndex);
    }

    /// <summary>
    ///     Verifies custom DXIL modules are immutable, content-addressed, and stage validated.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ShaderModulesValidateAndHashCustomDxil() {
        byte[] byteCode = [1, 2, 3, 4];
        var first = new D3D12ShaderModule("cs", "custom", "main", byteCode);
        var second = new D3D12ShaderModule("CS", "custom-copy", "main", byteCode);
        byteCode[0] = 9;

        Assert.Equal("CS", first.Stage);
        Assert.Equal([1, 2, 3, 4], first.ByteCode.ToArray());
        Assert.Equal(first.ContentHash, second.ContentHash);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new D3D12ShaderModule("XX", "custom", "main", byteCode));
        Assert.Throws<ArgumentException>(() => new D3D12ShaderModule("CS", "custom", "main", []));
    }

    /// <summary>
    ///     Verifies manifest lookup resolves non-default entry points and rejects unknown declarations.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ShaderManifestResolvesExplicitEntryPoints() {
        Assert.Equal("main", D3D12ShaderManifest.ResolveEntryPoint("VS", "vsScreenQuad"));
        Assert.Equal("mainArrowHead", D3D12ShaderManifest.ResolveEntryPoint("gs", "gsLineArrowHead"));
        Assert.Equal("billboardTextOIT",
            D3D12ShaderManifest.ResolveEntryPoint("PS", "psBillboardTextOITDP"));
        Assert.Throws<FileNotFoundException>(() =>
            D3D12ShaderManifest.ResolveEntryPoint("CS", "missing-shader"));
    }

    /// <summary>
    ///     Verifies every built-in shader description resolves to immutable embedded SM6 DXIL.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void EveryBuiltInShaderDescriptionResolvesDxilModule() {
        var descriptions = typeof(DefaultVsShaderDescriptions).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(DefaultVsShaderDescriptions).Namespace
                           && type.Name.EndsWith("ShaderDescriptions", StringComparison.Ordinal))
            .SelectMany(type => type.GetFields(System.Reflection.BindingFlags.Public
                                                | System.Reflection.BindingFlags.Static))
            .Where(field => field.FieldType == typeof(ShaderDescription))
            .Select(field => Assert.IsType<ShaderDescription>(field.GetValue(null)))
            .ToArray();

        Assert.NotEmpty(descriptions);
        Assert.All(descriptions, description => {
            var module = Assert.IsType<D3D12ShaderModule>(description.D3D12Module);
            Assert.Equal(description.ByteCode, module.ByteCode.ToArray());
            Assert.Equal(description.ShaderType switch {
                ShaderStage.Vertex => "VS",
                ShaderStage.Pixel => "PS",
                ShaderStage.Geometry => "GS",
                ShaderStage.Hull => "HS",
                ShaderStage.Domain => "DS",
                ShaderStage.Compute => "CS",
                _ => throw new InvalidOperationException()
            }, module.Stage);
        });
    }

    /// <summary>
    ///     Verifies custom precompiled DXIL uses the same immutable shader-description contract.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ShaderDescriptionAcceptsCustomDxil() {
        byte[] byteCode = [1, 2, 3, 4];
        var description = new ShaderDescription("custom", ShaderStage.Compute, byteCode);
        var module = Assert.IsType<D3D12ShaderModule>(description.D3D12Module);
        byteCode[0] = 9;

        Assert.Equal("CS", module.Stage);
        Assert.Equal("main", module.EntryPoint);
        Assert.Equal([1, 2, 3, 4], module.ByteCode.ToArray());
    }

    /// <summary>
    ///     Verifies all default technique and pass descriptions resolve DXIL stages and DX12 input layouts.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void DefaultTechniqueCatalogHasCompleteDx12ShaderContracts() {
        var techniques = DefaultEffectsManager.LoadTechniqueDescriptions().ToArray();
        var passes = techniques.SelectMany(technique => technique.PassDescriptions ?? []).ToArray();

        Assert.Equal(24, techniques.Length);
        Assert.Equal(199, passes.Length);
        Assert.All(passes, pass => {
            var modules = pass.GetD3D12ShaderModules();
            Assert.NotEmpty(modules);
            Assert.Equal(modules.Count, modules.Keys.Distinct().Count());
        });
        Assert.All(techniques.Where(technique => technique.InputLayoutDescription is not null), technique => {
            var layout = Assert.IsType<InputLayoutDescription>(technique.InputLayoutDescription);
            Assert.Equal(layout.InputElements.Length, layout.D3D12InputElements.Count);
        });
    }

    /// <summary>
    ///     Verifies pass contracts reject duplicate stages and mixed compute/graphics pipelines.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void Dx12ShaderPassContractsRejectInvalidStageSets() {
        var vertex = new ShaderDescription("vertex", ShaderStage.Vertex, [1]);
        var duplicateVertex = new ShaderDescription("vertex2", ShaderStage.Vertex, [2]);
        var compute = new ShaderDescription("compute", ShaderStage.Compute, [3]);

        Assert.Throws<InvalidOperationException>(() => new ShaderPassDescription("Duplicate") {
            ShaderList = [vertex, duplicateVertex]
        }.GetD3D12ShaderModules());
        Assert.Throws<InvalidOperationException>(() => new ShaderPassDescription("Mixed") {
            ShaderList = [vertex, compute]
        }.GetD3D12ShaderModules());
    }

    /// <summary>
    ///     Verifies every pipeline-key component participates in value equality.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void PipelineKeysIncludeEveryImmutableComponent() {
        var compute = new D3D12ShaderModule("CS", "custom", "main", [1, 2, 3, 4]);
        var key = D3D12PipelineStateKey.Compute(compute);

        Assert.Equal(key, D3D12PipelineStateKey.Compute(compute));
        Assert.NotEqual(key, key with { InputLayout = "layout" });
        Assert.NotEqual(key, key with { Topology = PrimitiveTopologyType.Triangle });
        Assert.NotEqual(key, key with { BlendState = "blend" });
        Assert.NotEqual(key, key with { RasterizerState = "rasterizer" });
        Assert.NotEqual(key, key with { DepthStencilState = "depth" });
        Assert.NotEqual(key, key with { RenderTargetFormats = Format.FormatR8G8B8A8Unorm.ToString() });
        Assert.NotEqual(key, key with { DepthStencilFormat = Format.FormatD32Float });

        var vertex = D3D12ShaderModule.Load("VS", "vsScreenQuad");
        var pixel = D3D12ShaderModule.Load("PS", "psScreenDup");
        var graphics = D3D12PipelineStateKey.Graphics(vertex, pixel);
        Assert.Equal(vertex.ContentHash, graphics.VertexShader);
        Assert.Equal(pixel.ContentHash, graphics.PixelShader);
        Assert.Throws<ArgumentException>(() => D3D12PipelineStateKey.Graphics(pixel, pixel));
    }

    /// <summary>
    ///     Verifies equivalent fixed-function descriptions share keys and every changed value separates them.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void PipelineKeysUseCompleteFixedFunctionStateValues() {
        var vertex = D3D12ShaderModule.Load("VS", "vsScreenQuad");
        var pixel = D3D12ShaderModule.Load("PS", "psScreenDup");
        var blend = DefaultBlendStateDescriptions.BsAlphaBlend;
        var blendCopy = blend;
        blendCopy.RenderTarget = [.. blend.RenderTarget];
        var rasterizer = DefaultRasterDescriptions.RsSolidNoMsaa;
        var depth = DefaultDepthStencilDescriptions.DssDepthLessEqual;
        D3D12InputElementDescription[] layout = [
            new("POSITION", 0, Format.FormatR32G32B32Float, 0, 0)
        ];
        var first = D3D12PipelineStateKey.Graphics(vertex,
            pixel,
            inputLayout: layout,
            blendState: blend,
            rasterizerState: rasterizer,
            depthStencilState: depth,
            sampleMask: 7,
            renderTargetFormats: [Format.FormatR8G8B8A8Unorm, Format.FormatR16G16B16A16Float],
            depthStencilFormat: Format.FormatD32Float);
        var equivalent = D3D12PipelineStateKey.Graphics(vertex,
            pixel,
            inputLayout: [.. layout],
            blendState: blendCopy,
            rasterizerState: rasterizer,
            depthStencilState: depth,
            sampleMask: 7,
            renderTargetFormats: [Format.FormatR8G8B8A8Unorm, Format.FormatR16G16B16A16Float],
            depthStencilFormat: Format.FormatD32Float);

        Assert.Equal(first, equivalent);
        blendCopy.RenderTarget[0].IsBlendEnabled = !blendCopy.RenderTarget[0].IsBlendEnabled;
        Assert.NotEqual(first, D3D12PipelineStateKey.Graphics(vertex,
            pixel,
            inputLayout: layout,
            blendState: blendCopy,
            rasterizerState: rasterizer,
            depthStencilState: depth,
            sampleMask: 7,
            renderTargetFormats: [Format.FormatR8G8B8A8Unorm, Format.FormatR16G16B16A16Float],
            depthStencilFormat: Format.FormatD32Float));
        Assert.NotEqual(first, first with { SampleMask = 8 });
    }

    /// <summary>
    ///     Verifies missing embedded shaders and invalid compute-key stages fail with clear contracts.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ShaderLoadingRejectsMissingAndIncompatibleModules() {
        Assert.Throws<FileNotFoundException>(() => D3D12ShaderModule.Load("CS", "missing-shader"));
        var vertex = D3D12ShaderModule.Load("VS", "vsScreenQuad");
        Assert.Throws<ArgumentException>(() => D3D12PipelineStateKey.Compute(vertex));
    }

    /// <summary>
    ///     Verifies native WARP descriptor heaps expose stable CPU and optional GPU handles.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpAllocatesNativeDescriptorHandles() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var rtvHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 2);
        using var shaderHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 2, true);
        using var rootSignature = device.CreateDefaultRootSignature();
        using var rtv = rtvHeap.Allocate();
        using var shaderDescriptor = shaderHeap.Allocate();

        Assert.NotEqual(nuint.Zero, rtv.CpuHandle.Ptr);
        Assert.Equal(0UL, rtv.GpuHandle.Ptr);
        Assert.NotEqual(nuint.Zero, shaderDescriptor.CpuHandle.Ptr);
        Assert.NotEqual(0UL, shaderDescriptor.GpuHandle.Ptr);
        Assert.NotEqual(nint.Zero, rootSignature.NativePointer);
        Assert.Equal(1, rtvHeap.Count);
        Assert.Equal(1, shaderHeap.Count);
        Assert.True(device.GetDeviceRemovedReason() >= 0);
        device.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Verifies a WARP command list copies bytes through upload, default, and readback resources.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpCopiesBufferThroughGpu() {
        byte[] expected = [1, 3, 5, 7, 9, 11, 13, 15];
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var upload = device.CreateBuffer((ulong) expected.Length, HeapType.Upload);
        using var gpu = device.CreateBuffer((ulong) expected.Length);
        using var readback = device.CreateBuffer((ulong) expected.Length, HeapType.Readback);
        upload.Write(expected);

        context.Reset();
        Assert.True(context.Transition(gpu, ResourceStates.CopyDest));
        context.CopyBuffer(gpu, 0, upload, 0, (ulong) expected.Length);
        Assert.True(context.Transition(gpu, ResourceStates.CopySource));
        context.CopyBuffer(readback, 0, gpu, 0, (ulong) expected.Length);
        context.Close();

        queue.Execute(context);
        var fenceValue = queue.Signal(fence);
        fence.Wait(fenceValue, TimeSpan.FromSeconds(5));

        Assert.Equal(expected, readback.Read(expected.Length));
    }

    /// <summary>
    ///     Verifies WARP clears a render-target texture and copies its pixels through a padded readback footprint.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpClearsRenderTargetAndReadsBackPixel() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var heap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var descriptor = heap.Allocate();
        using var texture = device.CreateRenderTargetTexture2D(4, 4, Format.FormatR8G8B8A8Unorm);
        device.CreateRenderTargetView(texture, descriptor);
        var footprint = device.GetCopyableFootprint(texture, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);

        context.Reset();
        context.ClearRenderTarget(texture, descriptor, [1.0f, 0.0f, 0.0f, 1.0f]);
        Assert.True(context.Transition(texture, ResourceStates.CopySource));
        context.CopyTextureToBuffer(readback, texture, in footprint);
        context.Close();

        queue.Execute(context);
        var fenceValue = queue.Signal(fence);
        fence.Wait(fenceValue, TimeSpan.FromSeconds(5));

        Assert.Equal(256U, footprint.Footprint.RowPitch);
        Assert.Equal([255, 0, 0, 255], readback.Read(4));
    }

    /// <summary>
    ///     Verifies texture SRV and sampler descriptors feed a repository DXIL sampling pass on WARP.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpSamplesUploadedTextureThroughDescriptors() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var rootSignature = device.CreateDefaultRootSignature();
        using var pipeline = device.CreateGraphicsPipelineState(rootSignature,
            D3D12ShaderModule.Load("VS", "vsMeshOutlineScreenQuad"),
            D3D12ShaderModule.Load("PS", "psScreenDup"));
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 118, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 10, true);
        var resourceDescriptors = Enumerable.Range(0, 14).Select(_ => resourceHeap.Allocate()).ToArray();
        using var sampler = samplerHeap.Allocate();
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var texture = device.CreateTexture2D(1, 1, Format.FormatR8G8B8A8Unorm);
        using var upload = device.CreateTextureUploadBuffer(texture,
            [0, 255, 0, 255],
            4,
            out var uploadFootprint);
        using var arrayTexture = device.CreateTexture2D(4, 4, Format.FormatR8G8B8A8Unorm,
            arraySize: 4,
            mipLevels: 2);
        using var cubeTexture = device.CreateTexture2D(4, 4, Format.FormatR8G8B8A8Unorm,
            arraySize: 6,
            mipLevels: 2);
        using var uavTexture = device.CreateTexture2D(4,
            4,
            Format.FormatR8G8B8A8Unorm,
            ResourceFlags.AllowUnorderedAccess,
            ResourceStates.UnorderedAccess,
            mipLevels: 2);
        using var renderTarget = device.CreateRenderTargetTexture2D(4, 4, Format.FormatR8G8B8A8Unorm);

        device.CreateShaderResourceView(texture, resourceDescriptors[10]);
        device.CreateShaderResourceView(arrayTexture, resourceDescriptors[11]);
        device.CreateShaderResourceView(cubeTexture, resourceDescriptors[12], true);
        device.CreateUnorderedAccessView(uavTexture, resourceDescriptors[13], 1);
        device.CreateSampler(sampler);
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        var readbackFootprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);

        try {
            context.Reset();
            context.CopyBufferToTexture(texture, upload, in uploadFootprint);
            Assert.True(context.Transition(texture, ResourceStates.PixelShaderResource));
            context.ClearRenderTarget(renderTarget, renderTargetView, [1, 0, 0, 1]);
            context.SetRenderTarget(renderTargetView);
            context.SetViewport(4, 4);
            context.SetGraphicsPipeline(rootSignature, pipeline);
            context.SetDescriptorHeaps(resourceHeap, samplerHeap);
            context.SetGraphicsDescriptorTables(resourceDescriptors[0], sampler);
            context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
            context.DrawInstanced(4);
            Assert.True(context.Transition(renderTarget, ResourceStates.CopySource));
            context.CopyTextureToBuffer(readback, renderTarget, in readbackFootprint);
            context.Close();

            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

            Assert.Equal([0, 255, 0, 255], readback.Read(4));
            device.ThrowIfDeviceRemoved();
        } finally {
            foreach (var descriptor in resourceDescriptors) descriptor.Dispose();
        }
    }

    /// <summary>
    ///     Verifies texture-view, sampler, array, cube, mip, and heap validation fails before native use.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRejectsInvalidTextureDescriptorRequests() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 2);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 1);
        using var rtvHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var resourceDescriptor = resourceHeap.Allocate();
        using var samplerDescriptor = samplerHeap.Allocate();
        using var rtvDescriptor = rtvHeap.Allocate();
        using var plainTexture = device.CreateTexture2D(1, 1, Format.FormatR8G8B8A8Unorm);
        using var arrayTexture = device.CreateTexture2D(1, 1, Format.FormatR8G8B8A8Unorm, arraySize: 4);
        using var buffer = device.CreateBuffer(4);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            device.CreateTexture2D(1, 1, Format.FormatR8G8B8A8Unorm, arraySize: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            device.CreateTexture2D(1, 1, Format.FormatR8G8B8A8Unorm, mipLevels: 0));
        Assert.Throws<ArgumentException>(() => device.CreateShaderResourceView(buffer, resourceDescriptor));
        Assert.Throws<ArgumentException>(() => device.CreateShaderResourceView(plainTexture, rtvDescriptor));
        Assert.Throws<ArgumentException>(() => device.CreateShaderResourceView(arrayTexture,
            resourceDescriptor,
            true));
        Assert.Throws<ArgumentException>(() => device.CreateUnorderedAccessView(plainTexture,
            resourceDescriptor));
        Assert.Throws<ArgumentException>(() => device.CreateSampler(resourceDescriptor));
        Assert.Throws<ArgumentOutOfRangeException>(() => device.CreateSampler(samplerDescriptor,
            maxAnisotropy: 0));
    }

    /// <summary>
    ///     Verifies a descriptor-bound repository shader draws through the productive graphics command path.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpDrawsDescriptorBoundQuadAndReadsBackPixel() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var rootSignature = device.CreateDefaultRootSignature();
        using var pipeline = device.CreateGraphicsPipelineState(rootSignature,
            D3D12ShaderModule.Load("VS", "vsMeshOutlineScreenQuad"),
            D3D12ShaderModule.Load("PS", "psEffectOutlineQuadStencil"),
            rasterizerState: DefaultRasterDescriptions.RsOutline);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 118, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 10, true);
        var resourceDescriptors = Enumerable.Range(0, 7).Select(_ => resourceHeap.Allocate()).ToArray();
        using var samplerTable = samplerHeap.Allocate();
        using var constantBuffer = device.CreateBuffer(256, HeapType.Upload);
        using var meshModel = new DefaultMeshGeometryBufferModel {
            Geometry = new MeshGeometry3D {
                Positions = new Vector3Collection([
                    new Vector3(-1, 1, 0),
                    new Vector3(1, 1, 0),
                    new Vector3(-1, -1, 0),
                    new Vector3(1, -1, 0)
                ]),
                Indices = new IntCollection([0, 1, 2, 2, 1, 3])
            }
        };
        using var meshBuffers = SilkD3D12DefaultMeshBuffers.Create(device, meshModel);
        using var instanceModel = new MatrixInstanceBufferModel {
            Elements = [Matrix.Identity, Matrix.Identity]
        };
        using var instanceBuffer = SilkD3D12ElementsBuffer<Matrix>.Create(device, instanceModel);
        using var renderTarget = device.CreateRenderTargetTexture2D(4, 4, Format.FormatR8G8B8A8Unorm);
        var color = new byte[256];
        System.Buffer.BlockCopy(new[] {1.0f, 0.0f, 0.0f, 1.0f}, 0, color, 0, 16);
        constantBuffer.Write(color);
        device.CreateConstantBufferView(constantBuffer, resourceDescriptors[6], 256);
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);

        try {
            context.Reset();
            context.ClearRenderTarget(renderTarget, renderTargetView, [0.0f, 0.0f, 0.0f, 1.0f]);
            context.SetRenderTarget(renderTargetView);
            context.SetViewport(4, 4);
            context.SetGraphicsPipeline(rootSignature, pipeline);
            context.SetDescriptorHeaps(resourceHeap, samplerHeap);
            context.SetGraphicsDescriptorTables(resourceDescriptors[0], samplerTable);
            Assert.Equal(4u, GeometryRenderCore.DrawIndexed(context, meshBuffers, instanceBuffer));
            Assert.True(context.Transition(renderTarget, ResourceStates.CopySource));
            context.CopyTextureToBuffer(readback, renderTarget, in footprint);
            context.Close();

            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

            Assert.Equal([255, 0, 0, 255], readback.Read(4));
            device.ThrowIfDeviceRemoved();
        } finally {
            foreach (var descriptor in resourceDescriptors) descriptor.Dispose();
        }
    }

    /// <summary>
    ///     Verifies static mesh creation rejects absent, incomplete, and out-of-range geometry data before upload.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRejectsInvalidStaticMeshData() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var model = new DefaultMeshGeometryBufferModel();

        Assert.Throws<InvalidOperationException>(() => SilkD3D12DefaultMeshBuffers.Create(device, model));

        model.Geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([Vector3.Zero]),
            Indices = new IntCollection([1])
        };
        Assert.Throws<ArgumentException>(() => SilkD3D12DefaultMeshBuffers.Create(device, model));

        model.Geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([Vector3.Zero, Vector3.One]),
            Indices = new IntCollection([0, 1]),
            TextureCoordinates = new Vector2Collection([Vector2.Zero])
        };
        Assert.Throws<ArgumentException>(() => SilkD3D12DefaultMeshBuffers.Create(device, model));

        model.Geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([Vector3.Zero, Vector3.One]),
            Indices = new IntCollection([0, 1]),
            Colors = new Color4Collection([Vector4.One])
        };
        Assert.Throws<ArgumentException>(() => SilkD3D12DefaultMeshBuffers.Create(device, model));
    }

    /// <summary>
    ///     Verifies empty optional mesh streams receive one default value per position before upload.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpTreatsEmptyOptionalMeshStreamsAsAbsent() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var model = new DefaultMeshGeometryBufferModel {
            Geometry = new MeshGeometry3D {
                Positions = new Vector3Collection([Vector3.Zero, Vector3.UnitX, Vector3.UnitY]),
                Indices = new IntCollection([0, 1, 2]),
                TextureCoordinates = [],
                Colors = []
            }
        };

        using var buffers = SilkD3D12DefaultMeshBuffers.Create(device, model);

        Assert.Equal(3u, buffers.VertexCount);
    }

    /// <summary>
    ///     Verifies default mesh buffers update in place when capacity is sufficient and replace atomically on growth.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpUpdatesAndRecreatesDefaultMeshBuffers() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var model = new DefaultMeshGeometryBufferModel {
            Geometry = new MeshGeometry3D {
                Positions = new Vector3Collection([Vector3.Zero, Vector3.UnitX, Vector3.UnitY]),
                Indices = new IntCollection([0, 1, 2])
            }
        };
        using var buffers = SilkD3D12DefaultMeshBuffers.Create(device, model);
        model.Geometry.Indices = new IntCollection([2, 1, 0]);

        buffers.Update(model);
        using var readback = device.CreateBuffer(12, HeapType.Readback);
        context.Reset();
        context.CopyBuffer(readback, 0, buffers.IndexBuffer, 0, 12);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        Assert.Equal(new[] {2, 1, 0}, MemoryMarshal.Cast<byte, int>(readback.Read(12)).ToArray());
        Assert.Equal(3u, buffers.IndexCount);

        model.Geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([Vector3.Zero, Vector3.UnitX, Vector3.UnitY, Vector3.One]),
            Indices = new IntCollection([0, 1, 2, 2, 1, 3])
        };
        Assert.Throws<InvalidOperationException>(() => buffers.Update(model));
        using var replacement = buffers.Recreate(device, model);

        Assert.True(buffers.IsDisposed);
        Assert.Equal(6u, replacement.IndexCount);
        Assert.False(replacement.IsDisposed);
    }

    /// <summary>
    ///     Verifies element-model streams update in place and replace only after successful growth allocation.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpUpdatesAndRecreatesElementBuffers() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var model = new MatrixInstanceBufferModel();
        Assert.Throws<InvalidOperationException>(() => SilkD3D12ElementsBuffer<Matrix>.Create(device, model));

        model.Elements = [Matrix.Identity, Matrix.Identity];
        using var buffer = SilkD3D12ElementsBuffer<Matrix>.Create(device, model);
        Assert.Equal(64u, SilkD3D12ElementsBuffer<Matrix>.StrideInBytes);
        Assert.Equal(2u, buffer.ElementCount);

        model.Elements = [Matrix.Identity];
        buffer.Update(model);
        Assert.Equal(1u, buffer.ElementCount);

        model.Elements = [Matrix.Identity, Matrix.Identity, Matrix.Identity];
        Assert.Throws<InvalidOperationException>(() => buffer.Update(model));
        using var replacement = buffer.Recreate(device, model);

        Assert.True(buffer.IsDisposed);
        Assert.Equal(3u, replacement.ElementCount);
        Assert.False(replacement.IsDisposed);
    }

    /// <summary>
    ///     Verifies line and point models create, update, replace, bind, and record their correct draw forms.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpCreatesUpdatesAndDrawsPointLineBuffers() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var rootSignature = device.CreateDefaultRootSignature();
        using var linePipeline = device.CreateGraphicsPipelineState(rootSignature,
            D3D12ShaderModule.Load("VS", "vsScreenQuad"),
            D3D12ShaderModule.Load("PS", "psEffectOutlineQuadStencil"),
            topology: PrimitiveTopologyType.Line);
        using var pointPipeline = device.CreateGraphicsPipelineState(rootSignature,
            D3D12ShaderModule.Load("VS", "vsScreenQuad"),
            D3D12ShaderModule.Load("PS", "psEffectOutlineQuadStencil"),
            topology: PrimitiveTopologyType.Point);
        using var lineModel = new DefaultLineGeometryBufferModel {
            Geometry = new LineGeometry3D {
                Positions = new Vector3Collection([Vector3.Zero, Vector3.UnitX]),
                Indices = new IntCollection([0, 1])
            }
        };
        using var pointModel = new DefaultPointGeometryBufferModel {
            Geometry = new PointGeometry3D {
                Positions = new Vector3Collection([Vector3.Zero, Vector3.One])
            }
        };
        using var lineBuffers = SilkD3D12PointLineBuffers.Create(device, lineModel);
        using var pointBuffers = SilkD3D12PointLineBuffers.Create(device, pointModel);
        using var instanceModel = new MatrixInstanceBufferModel {Elements = [Matrix.Identity, Matrix.Identity]};
        using var instanceBuffer = SilkD3D12ElementsBuffer<Matrix>.Create(device, instanceModel);

        Assert.Equal(PrimitiveTopology.LineList, lineBuffers.Topology);
        Assert.Equal(2u, lineBuffers.IndexCount);
        Assert.Equal(PrimitiveTopology.PointList, pointBuffers.Topology);
        Assert.Equal(2u, pointBuffers.VertexCount);
        Assert.Null(pointBuffers.IndexBuffer);

        lineModel.Geometry!.Indices = new IntCollection([1, 0]);
        pointModel.Geometry!.Positions = new Vector3Collection([Vector3.UnitY]);
        lineBuffers.Update(lineModel);
        pointBuffers.Update(pointModel);
        Assert.Equal(1u, pointBuffers.VertexCount);

        using var indexReadback = device.CreateBuffer(8, HeapType.Readback);
        using var vertexReadback = device.CreateBuffer(PointsVertex.SizeInBytes, HeapType.Readback);
        context.Reset();
        context.CopyBuffer(indexReadback, 0, lineBuffers.IndexBuffer!, 0, 8);
        context.CopyBuffer(vertexReadback, 0, pointBuffers.VertexBuffer, 0, PointsVertex.SizeInBytes);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        Assert.Equal(new[] {1, 0}, MemoryMarshal.Cast<byte, int>(indexReadback.Read(8)).ToArray());
        Assert.Equal(new[] {0f, 1f, 0f, 1f},
            MemoryMarshal.Cast<byte, float>(vertexReadback.Read(16)).ToArray());

        context.Reset();
        context.SetGraphicsPipeline(rootSignature, linePipeline);
        Assert.Equal(2u, GeometryRenderCore.Draw(context, lineBuffers, instanceBuffer));
        context.SetGraphicsPipeline(rootSignature, pointPipeline);
        Assert.Equal(1u, GeometryRenderCore.Draw(context, pointBuffers));
        context.Close();

        pointModel.Geometry.Positions = new Vector3Collection([Vector3.Zero, Vector3.UnitX, Vector3.UnitY]);
        Assert.Throws<InvalidOperationException>(() => pointBuffers.Update(pointModel));
        using var replacement = pointBuffers.Recreate(device, pointModel);
        Assert.True(pointBuffers.IsDisposed);
        Assert.Equal(3u, replacement.VertexCount);
    }

    /// <summary>
    ///     Verifies line and point creation rejects missing, mismatched, and out-of-range source data.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRejectsInvalidPointLineData() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var lineModel = new DefaultLineGeometryBufferModel();
        using var pointModel = new DefaultPointGeometryBufferModel();

        Assert.Throws<InvalidOperationException>(() => SilkD3D12PointLineBuffers.Create(device, lineModel));
        Assert.Throws<InvalidOperationException>(() => SilkD3D12PointLineBuffers.Create(device, pointModel));

        lineModel.Geometry = new LineGeometry3D {
            Positions = new Vector3Collection([Vector3.Zero]),
            Indices = new IntCollection([0, 1])
        };
        pointModel.Geometry = new PointGeometry3D {
            Positions = new Vector3Collection([Vector3.Zero, Vector3.One]),
            Colors = new Color4Collection([Vector4.One])
        };

        Assert.Throws<ArgumentException>(() => SilkD3D12PointLineBuffers.Create(device, lineModel));
        Assert.Throws<ArgumentException>(() => SilkD3D12PointLineBuffers.Create(device, pointModel));
    }

    /// <summary>
    ///     Verifies native render-target creation rejects invalid dimensions and descriptor heap types.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRejectsInvalidRenderTargetRequests() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            device.CreateRenderTargetTexture2D(0, 1, Format.FormatR8G8B8A8Unorm));
        Assert.Throws<ArgumentException>(() =>
            device.CreateBuffer(4, HeapType.Upload, ResourceFlags.AllowUnorderedAccess));

        using var context = device.CreateCommandContext();
        using var texture = device.CreateRenderTargetTexture2D(1, 1, Format.FormatR8G8B8A8Unorm);
        using var buffer = device.CreateBuffer(4);
        using var constantBuffer = device.CreateBuffer(256, HeapType.Upload);
        using var heap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1);
        using var descriptor = heap.Allocate();
        using var rtvHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var rtv = rtvHeap.Allocate();
        using var dsvHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        using var dsv = dsvHeap.Allocate();
        using var depthStencil = device.CreateDepthStencilTexture2D(1,
            1,
            Format.FormatD32FloatS8X24Uint);

        Assert.Throws<ArgumentException>(() => device.CreateRenderTargetView(texture, descriptor));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            device.CreateDepthStencilTexture2D(1, 1, Format.FormatR8G8B8A8Unorm));
        Assert.Throws<ArgumentException>(() => device.CreateDepthStencilView(texture, dsv));
        Assert.Throws<ArgumentException>(() => device.CreateDepthStencilView(depthStencil, rtv));
        Assert.Throws<ArgumentException>(() => device.CreateConstantBufferView(constantBuffer, rtv, 256));
        Assert.Throws<ArgumentOutOfRangeException>(() => device.CreateConstantBufferView(constantBuffer,
            descriptor,
            255));
        Assert.Throws<ArgumentException>(() => device.GetCopyableFootprint(buffer, out _));
        Assert.Throws<ArgumentException>(() => context.UavBarrier(buffer));
        Assert.Throws<ArgumentException>(() => context.SetRenderTarget(descriptor));
        Assert.Throws<ArgumentException>(() => context.SetRenderTargets(descriptor, dsv));
        Assert.Throws<ArgumentException>(() => context.SetRenderTargets(rtv, descriptor));
        Assert.Throws<ArgumentException>(() => context.ClearDepthStencil(texture, dsv));
        Assert.Throws<ArgumentException>(() => context.ClearDepthStencil(depthStencil, rtv));
        Assert.Throws<ArgumentOutOfRangeException>(() => context.ClearDepthStencil(depthStencil, dsv, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => context.SetViewport(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => context.SetViewport(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => context.SetVertexBuffer(0, constantBuffer, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => context.SetVertexBuffer(0, constantBuffer, 4, 257));
        Assert.Throws<InvalidOperationException>(() => context.SetVertexBuffer(0, buffer, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => context.SetIndexBuffer(constantBuffer,
            Format.FormatUnknown));
        Assert.Throws<ArgumentOutOfRangeException>(() => context.SetIndexBuffer(constantBuffer,
            Format.FormatR16Uint,
            257));
        Assert.Throws<InvalidOperationException>(() => context.SetIndexBuffer(buffer, Format.FormatR16Uint));
        Assert.Throws<ArgumentOutOfRangeException>(() => context.DrawIndexedInstanced(0));
        Assert.Throws<InvalidOperationException>(() => context.DrawIndexedInstanced(3));
        Assert.Throws<ArgumentException>(() =>
            device.CreateTextureUploadBuffer(texture, [1, 2, 3], 4, out _));
    }

    /// <summary>
    ///     Verifies WARP uploads tightly packed texture rows through an aligned footprint and reads them back.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpUploadsTextureSubresourceAndReadsBackRows() {
        byte[] expected = [
            255, 0, 0, 255, 0, 255, 0, 255,
            0, 0, 255, 255, 255, 255, 255, 255
        ];
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var texture = device.CreateTexture2D(2, 2, Format.FormatR8G8B8A8Unorm);
        using var upload = device.CreateTextureUploadBuffer(texture, expected, 8, out var footprint);
        _ = device.GetCopyableFootprint(texture, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);

        context.Reset();
        context.CopyBufferToTexture(texture, upload, in footprint);
        Assert.True(context.Transition(texture, ResourceStates.CopySource));
        context.CopyTextureToBuffer(readback, texture, in footprint);
        context.Close();

        queue.Execute(context);
        var fenceValue = queue.Signal(fence);
        fence.Wait(fenceValue, TimeSpan.FromSeconds(5));

        var actual = readback.Read((int) totalBytes);
        Assert.Equal(expected.AsSpan(0, 8).ToArray(), actual.AsSpan(0, 8).ToArray());
        Assert.Equal(expected.AsSpan(8, 8).ToArray(),
            actual.AsSpan((int) footprint.Footprint.RowPitch, 8).ToArray());
    }

    /// <summary>
    ///     Verifies WARP uploads every depth slice of a 3D texture, creates its SRV, and reads the volume back.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpUploadsTexture3DAndReadsBackDepthSlices() {
        byte[] expected = [1, 2, 3, 4, 5, 6, 7, 8];
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var texture = device.CreateTexture3D(2, 2, 2, Format.FormatR8Unorm);
        using var upload = device.CreateTextureUploadBuffer(texture,
            [new D3D12SubresourceData(expected, 2, 4)],
            out var footprints);
        using var heap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1);
        using var descriptor = heap.Allocate();
        device.CreateTexture3DShaderResourceView(texture, descriptor);
        _ = device.GetCopyableFootprint(texture, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);

        context.Reset();
        context.CopyBufferToTexture(texture, upload, footprints);
        Assert.True(context.Transition(texture, ResourceStates.CopySource));
        context.CopyTextureToBuffer(readback, texture, in footprints[0]);
        context.Close();

        queue.Execute(context);
        var fenceValue = queue.Signal(fence);
        fence.Wait(fenceValue, TimeSpan.FromSeconds(5));

        var actual = readback.Read((int) totalBytes);
        var rowPitch = checked((int) footprints[0].Footprint.RowPitch);
        var slicePitch = checked(rowPitch * 2);
        Assert.Equal(expected.AsSpan(0, 2).ToArray(), actual.AsSpan(0, 2).ToArray());
        Assert.Equal(expected.AsSpan(2, 2).ToArray(), actual.AsSpan(rowPitch, 2).ToArray());
        Assert.Equal(expected.AsSpan(4, 2).ToArray(), actual.AsSpan(slicePitch, 2).ToArray());
        Assert.Equal(expected.AsSpan(6, 2).ToArray(), actual.AsSpan(slicePitch + rowPitch, 2).ToArray());
        Assert.Equal(ResourceDimension.Texture3D, texture.Description.Dimension);
        Assert.Equal(ResourceStates.CopySource, texture.State);
    }

    /// <summary>
    ///     Verifies WARP accepts a UAV state transition and ordering barrier on an unordered-access buffer.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRecordsUavBarrier() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var buffer = device.CreateBuffer(256,
            HeapType.Default,
            ResourceFlags.AllowUnorderedAccess);

        context.Reset();
        Assert.True(context.Transition(buffer, ResourceStates.UnorderedAccess));
        context.UavBarrier(buffer);
        context.Close();

        queue.Execute(context);
        var fenceValue = queue.Signal(fence);
        fence.Wait(fenceValue, TimeSpan.FromSeconds(5));

        Assert.Equal(ResourceStates.UnorderedAccess, buffer.State);
        device.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Verifies structured buffer views and stream-output binding use validated native D3D12 resources.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpBindsStructuredResourcesAndStreamOutputTarget() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1);
        using var descriptor = resourceHeap.Allocate();
        using var structured = device.CreateBuffer(4 * sizeof(float), HeapType.Upload);
        using var output = device.CreateBuffer(256);
        using var filledSize = device.CreateBuffer(sizeof(uint));

        device.CreateStructuredBufferShaderResourceView(structured, descriptor, 4, sizeof(float));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            device.CreateStructuredBufferShaderResourceView(structured, descriptor, 5, sizeof(float)));
        Assert.Throws<InvalidOperationException>(() => context.SetStreamOutputTarget(structured));
        Assert.Throws<InvalidOperationException>(() => context.SetStreamOutputTarget(output, 128));
        Assert.Throws<InvalidOperationException>(() =>
            context.SetStreamOutputTarget(output, filledSize, 128));

        context.Reset();
        Assert.True(context.Transition(output, ResourceStates.StreamOut));
        Assert.Throws<InvalidOperationException>(() =>
            context.SetStreamOutputTarget(output, filledSize, 128));
        Assert.True(context.Transition(filledSize, ResourceStates.StreamOut));
        context.SetStreamOutputTarget(output, filledSize, 128);
        context.SetStreamOutputTarget(null);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            context.SetStreamOutputTarget(output, filledSize, 257));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            context.SetStreamOutputTarget(output, filledSize, 128, 4));
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        Assert.Equal(ResourceStates.StreamOut, output.State);
        Assert.Equal(ResourceStates.StreamOut, filledSize.State);
        device.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Verifies the complete b9/t40/t60-t62 skinning table, output lifecycle, and uploaded byte layouts on WARP.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpBuildsBoneSkinningBindingsAndOutputBuffer() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 118, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 10, true);
        using var bindings = new SilkD3D12MeshBindings(device, resourceHeap, samplerHeap);
        using var morphTargets = new MorphTargetUploaderCore();
        var translated = Matrix.Identity;
        translated.M41 = 2;
        translated.M42 = 3;
        translated.M43 = 4;
        Matrix[] matrices = [Matrix.Identity, translated];
        BoneIds[] boneIds = [
            new() {Bone1 = 0, Weights = Vector4.UnitX},
            new() {Bone1 = 1, Weights = Vector4.UnitX}
        ];
        var targets = new[] {
            new MorphTargetVertex {deltaPosition = new Vector3(2, 0, 0)},
            new MorphTargetVertex(),
            new MorphTargetVertex(),
            new MorphTargetVertex {deltaPosition = new Vector3(0, 4, 0)}
        };
        Assert.True(morphTargets.InitializeMorphTargets(targets, 2));
        Assert.Throws<InvalidOperationException>(() =>
            bindings.UpdateBoneSkinning(boneIds, matrices, morphTargets, 2));
        morphTargets.MorphTargetWeights = [0.5f, 0.25f];
        Assert.Throws<InvalidOperationException>(() =>
            bindings.UpdateBoneSkinning(boneIds, matrices, morphTargets, 3));
        var invalidBoneIds = boneIds.ToArray();
        invalidBoneIds[0].Bone1 = 2;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            bindings.UpdateBoneSkinning(invalidBoneIds, matrices, morphTargets, 2));

        var skinning = bindings.UpdateBoneSkinning(boneIds, matrices, morphTargets, 2);

        Assert.Equal(9, SilkD3D12BoneSkinResources.MorphTargetConstantRegister);
        Assert.Equal(40, SilkD3D12BoneSkinResources.BoneMatrixRegister);
        Assert.Equal(60, SilkD3D12BoneSkinResources.MorphTargetWeightRegister);
        Assert.Equal(61, SilkD3D12BoneSkinResources.MorphTargetDeltaRegister);
        Assert.Equal(62, SilkD3D12BoneSkinResources.MorphTargetOffsetRegister);
        Assert.Equal((ulong) (2 * DefaultVertex.SizeInBytes), skinning.OutputSizeInBytes);
        Assert.Equal(ResourceStates.Common, skinning.Output.State);

        var boneResource = skinning.BoneMatrixResource
            ?? throw new InvalidOperationException("The bone-matrix upload was not created.");
        var weightResource = skinning.MorphTargetWeightResource
            ?? throw new InvalidOperationException("The morph-target-weight upload was not created.");
        var deltaResource = skinning.MorphTargetDeltaResource
            ?? throw new InvalidOperationException("The morph-target-delta upload was not created.");
        var offsetResource = skinning.MorphTargetOffsetResource
            ?? throw new InvalidOperationException("The morph-target-offset upload was not created.");
        var boneIdBytes = checked((int) skinning.BoneIdResource.SizeInBytes);
        var boneBytes = checked((int) boneResource.SizeInBytes);
        var weightBytes = checked((int) weightResource.SizeInBytes);
        var deltaBytes = checked((int) deltaResource.SizeInBytes);
        var offsetBytes = checked((int) offsetResource.SizeInBytes);
        const int constantBytes = 16;
        var totalBytes = boneIdBytes + boneBytes + weightBytes + deltaBytes + offsetBytes + constantBytes;
        using var readback = device.CreateBuffer((ulong) totalBytes, HeapType.Readback);
        context.Reset();
        skinning.BindOutput(context);
        skinning.UnbindOutput(context);
        ulong destinationOffset = 0;
        context.CopyBuffer(readback, destinationOffset, skinning.BoneIdResource, 0, (ulong) boneIdBytes);
        destinationOffset += (ulong) boneIdBytes;
        context.CopyBuffer(readback, destinationOffset, boneResource, 0, (ulong) boneBytes);
        destinationOffset += (ulong) boneBytes;
        context.CopyBuffer(readback, destinationOffset, weightResource, 0, (ulong) weightBytes);
        destinationOffset += (ulong) weightBytes;
        context.CopyBuffer(readback, destinationOffset, deltaResource, 0, (ulong) deltaBytes);
        destinationOffset += (ulong) deltaBytes;
        context.CopyBuffer(readback, destinationOffset, offsetResource, 0, (ulong) offsetBytes);
        destinationOffset += (ulong) offsetBytes;
        context.CopyBuffer(readback,
            destinationOffset,
            skinning.MorphTargetConstantResource,
            0,
            constantBytes);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        var bytes = readback.Read(totalBytes);
        var sourceOffset = 0;
        Assert.Equal(boneIds,
            MemoryMarshal.Cast<byte, BoneIds>(bytes.AsSpan(sourceOffset, boneIdBytes)).ToArray());
        sourceOffset += boneIdBytes;
        Assert.Equal(matrices,
            MemoryMarshal.Cast<byte, Matrix>(bytes.AsSpan(sourceOffset, boneBytes)).ToArray());
        sourceOffset += boneBytes;
        Assert.Equal(new[] {0.5f, 0.25f},
            MemoryMarshal.Cast<byte, float>(bytes.AsSpan(sourceOffset, weightBytes)).ToArray());
        sourceOffset += weightBytes;
        var deltas = MemoryMarshal.Cast<byte, Vector3>(bytes.AsSpan(sourceOffset, deltaBytes));
        Assert.Equal(Vector3.Zero, deltas[0]);
        Assert.Equal(new Vector3(2, 0, 0), deltas[3]);
        Assert.Equal(new Vector3(0, 4, 0), deltas[6]);
        sourceOffset += deltaBytes;
        Assert.Equal(new[] {3, 0, 0, 6},
            MemoryMarshal.Cast<byte, int>(bytes.AsSpan(sourceOffset, offsetBytes)).ToArray());
        sourceOffset += offsetBytes;
        Assert.Equal(new[] {2, 2, 0, 0},
            MemoryMarshal.Cast<byte, int>(bytes.AsSpan(sourceOffset, constantBytes)).ToArray());
        Assert.Equal(ResourceStates.VertexAndConstantBuffer, skinning.Output.State);
        device.ThrowIfDeviceRemoved();

        using var emptyMorphTargets = new MorphTargetUploaderCore();
        var previousOutput = skinning.Output;
        BoneIds[] fourBoneIds = [boneIds[0], boneIds[1], boneIds[0], boneIds[1]];
        Assert.Same(skinning, bindings.UpdateBoneSkinning(fourBoneIds, matrices, emptyMorphTargets, 4));
        Assert.True(previousOutput.IsDisposed);
        Assert.Null(skinning.MorphTargetWeightResource);
        Assert.Null(skinning.MorphTargetDeltaResource);
        Assert.Null(skinning.MorphTargetOffsetResource);
        Assert.Equal((ulong) (4 * DefaultVertex.SizeInBytes), skinning.OutputSizeInBytes);

        bindings.Dispose();
        Assert.True(skinning.IsDisposed);
        Assert.Equal(0, resourceHeap.Count);
        Assert.Equal(0, samplerHeap.Count);
    }

    /// <summary>
    ///     Verifies disposed native wrappers fail safely and a fresh WARP device can be created afterwards.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpDisposesAndRecreatesDeviceDeterministically() {
        var firstDevice = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        var context = firstDevice.CreateCommandContext();

        context.Dispose();
        Assert.Throws<ObjectDisposedException>(() => context.Reset());
        Assert.Throws<ObjectDisposedException>(() => context.Close());

        firstDevice.Dispose();
        Assert.True(firstDevice.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => firstDevice.CreateFence());
        Assert.Throws<ObjectDisposedException>(() => firstDevice.GetDeviceRemovedReason());

        using var secondDevice = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110,
            SilkDriverType.Warp);
        using var secondFence = secondDevice.CreateFence();

        Assert.NotEqual(nint.Zero, secondDevice.NativePointer);
        Assert.Equal(0UL, secondFence.CompletedValue);
        secondDevice.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Verifies WARP creates SM6 compute PSOs and the complete-key cache reuses and owns them.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpCreatesAndCachesComputePipelineStates() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var rootSignature = device.CreateDefaultRootSignature();
        using var cache = new D3D12PipelineStateCache();
        var insertShader = D3D12ShaderModule.Load("CS", "csParticleInsert");
        var updateShader = D3D12ShaderModule.Load("CS", "csParticleUpdate");
        var insertKey = D3D12PipelineStateKey.Compute(insertShader);
        var updateKey = D3D12PipelineStateKey.Compute(updateShader);
        var factoryCalls = 0;

        var first = cache.GetOrCreate(insertKey, () => {
            factoryCalls++;
            return device.CreateComputePipelineState(rootSignature, insertShader);
        });
        var reused = cache.GetOrCreate(insertKey, () => throw new InvalidOperationException("Cache miss."));
        var second = cache.GetOrCreate(updateKey, () => {
            factoryCalls++;
            return device.CreateComputePipelineState(rootSignature, updateShader);
        });

        Assert.Same(first, reused);
        Assert.NotSame(first, second);
        Assert.Equal(2, factoryCalls);
        Assert.Equal(2, cache.Count);
        Assert.NotEqual(nint.Zero, first.NativePointer);
        Assert.NotEqual(nint.Zero, second.NativePointer);

        cache.Dispose();
        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => cache.GetOrCreate(insertKey,
            () => device.CreateComputePipelineState(rootSignature, insertShader)));
    }

    /// <summary>
    ///     Verifies a WARP command list binds the shared root signature, PSOs, heaps, tables, and topology.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpBindsDx12PassStateToCommandList() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var rootSignature = device.CreateDefaultRootSignature();
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 118, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 10, true);
        using var invalidHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var resourceTable = resourceHeap.Allocate();
        using var samplerTable = samplerHeap.Allocate();
        using var computePipeline = device.CreateComputePipelineState(rootSignature,
            D3D12ShaderModule.Load("CS", "csParticleInsert"));
        using var graphicsPipeline = device.CreateGraphicsPipelineState(rootSignature,
            D3D12ShaderModule.Load("VS", "vsScreenQuad"),
            D3D12ShaderModule.Load("PS", "psScreenDup"));
        var computePass = ShaderPass.CreateD3D12("Compute", rootSignature, computePipeline, true);
        var graphicsPass = ShaderPass.CreateD3D12("Graphics",
            rootSignature,
            graphicsPipeline,
            topology: PrimitiveTopology.TriangleList);

        context.Reset();
        Assert.Throws<ArgumentException>(() => context.SetDescriptorHeaps(invalidHeap, samplerHeap));
        Assert.Throws<ArgumentOutOfRangeException>(() => context.SetPrimitiveTopology(PrimitiveTopology.Undefined));
        Assert.Throws<ArgumentOutOfRangeException>(() => context.DrawInstanced(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => context.Dispatch(1, 0, 1));
        Assert.Throws<InvalidOperationException>(() => context.DrawInstanced(3));
        Assert.Throws<InvalidOperationException>(() => context.Dispatch(1, 1, 1));
        Assert.Throws<InvalidOperationException>(() => ShaderPass.NullPass.BindShader(context));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ShaderPass.CreateD3D12("Invalid", rootSignature, graphicsPipeline,
                topology: PrimitiveTopology.Undefined));
        context.SetDescriptorHeaps(resourceHeap, samplerHeap);
        Assert.Throws<InvalidOperationException>(() => context.SetGraphicsDescriptorTables(resourceTable, samplerTable));
        Assert.Throws<InvalidOperationException>(() => context.SetComputeDescriptorTables(resourceTable, samplerTable));
        computePass.BindShader(context);
        context.SetComputeDescriptorTables(resourceTable, samplerTable);
        graphicsPass.BindShader(context);
        context.SetGraphicsDescriptorTables(resourceTable, samplerTable);
        context.Close();

        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));
        Assert.True(computePass.IsD3D12);
        Assert.True(graphicsPass.IsD3D12);
        device.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Verifies representative default graphics and compute descriptions create and reuse native DX12 passes.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpCreatesDefaultShaderPassDescriptionsThroughCache() {
        var techniques = DefaultEffectsManager.LoadTechniqueDescriptions().ToArray();
        var graphicsTechnique = techniques.Single(technique =>
            technique.Name == DefaultRenderTechniqueNames.ScreenQuad);
        var graphicsDescription = Assert.Single(graphicsTechnique.PassDescriptions!);
        var computeDescription = techniques.SelectMany(technique => technique.PassDescriptions ?? [])
            .First(pass => pass.GetD3D12ShaderModules().ContainsKey(ShaderStage.Compute));
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var rootSignature = device.CreateDefaultRootSignature();
        using var cache = new D3D12PipelineStateCache();
        using var context = device.CreateCommandContext();
        using var queue = device.CreateCommandQueue();
        using var fence = device.CreateFence();

        var graphicsPass = graphicsDescription.CreateD3D12(device,
            rootSignature,
            cache,
            graphicsTechnique.InputLayoutDescription,
            PrimitiveTopology.TriangleStrip);
        var reusedGraphicsPass = graphicsDescription.CreateD3D12(device,
            rootSignature,
            cache,
            graphicsTechnique.InputLayoutDescription,
            PrimitiveTopology.TriangleStrip);
        var computePass = computeDescription.CreateD3D12(device, rootSignature, cache);

        Assert.Same(graphicsPass.D3D12PipelineState, reusedGraphicsPass.D3D12PipelineState);
        Assert.Equal(2, cache.Count);
        context.Reset();
        graphicsPass.BindShader(context);
        computePass.BindShader(context);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));
        device.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Verifies every default pass description can create its required native Direct3D 12 pipeline state.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpCreatesEveryDefaultPassPipelineState() {
        var techniques = DefaultEffectsManager.LoadTechniqueDescriptions().ToArray();
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var rootSignature = device.CreateDefaultRootSignature();
        using var cache = new D3D12PipelineStateCache();
        var createdPasses = 0;

        foreach (var technique in techniques)
        foreach (var description in technique.PassDescriptions ?? []) {
            var inputLayout = description.InputLayoutDescription ?? technique.InputLayoutDescription;
            var geometryShader = (description.ShaderList ?? []).FirstOrDefault(shader =>
                shader.ShaderType == ShaderStage.Geometry)?.ByteCodeName;
            var fallbackTopology = geometryShader is null
                ? PrimitiveTopology.TriangleList
                : geometryShader.StartsWith("gsLine", StringComparison.Ordinal)
                    ? PrimitiveTopology.LineList
                    : PrimitiveTopology.PointList;
            try {
                var pass = description.CreateD3D12(device,
                    rootSignature,
                    cache,
                    inputLayout,
                    fallbackTopology,
                    depthStencilFormat: Format.FormatD24UnormS8Uint);
                Assert.True(pass.IsD3D12);
                createdPasses++;
            } catch (Exception exception) {
                throw new InvalidOperationException($"DX12 PSO creation failed for {technique.Name}/{description.Name}.",
                    exception);
            }
        }

        Assert.Equal(199, createdPasses);
        Assert.InRange(cache.Count, 1, createdPasses);
        device.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Verifies WARP creates graphics PSOs covering vertex, pixel, geometry, hull, and domain stages.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpCreatesGraphicsPipelinesForEveryUsedStage() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var rootSignature = device.CreateDefaultRootSignature();
        var screenVertex = D3D12ShaderModule.Load("VS", "vsScreenQuad");
        var screenPixel = D3D12ShaderModule.Load("PS", "psScreenDup");
        var pointVertex = D3D12ShaderModule.Load("VS", "vsPoint");
        var pointGeometry = D3D12ShaderModule.Load("GS", "gsPoint");
        var pointPixel = D3D12ShaderModule.Load("PS", "psPoint");
        var tessellationVertex = D3D12ShaderModule.Load("VS", "vsMeshTessellation");
        var tessellationHull = D3D12ShaderModule.Load("HS", "hsMeshTriTessellation");
        var tessellationDomain = D3D12ShaderModule.Load("DS", "dsMeshTriTessellation");
        var meshPixel = D3D12ShaderModule.Load("PS", "psMeshBlinnPhong");
        D3D12InputElementDescription[] pointLayout = [
            new("POSITION", 0, Format.FormatR32G32B32A32Float, 0, 0),
            new("COLOR", 0, Format.FormatR32G32B32A32Float, 0, 16),
            new("TEXCOORD", 0, Format.FormatR32G32B32A32Float, 0, 32),
            new("TEXCOORD", 1, Format.FormatR32G32B32A32Float, 0, 48),
            new("TEXCOORD", 2, Format.FormatR32G32B32A32Float, 0, 64),
            new("TEXCOORD", 3, Format.FormatR32G32B32A32Float, 0, 80)
        ];
        D3D12InputElementDescription[] meshLayout = [
            new("POSITION", 0, Format.FormatR32G32B32A32Float, 0, 0),
            new("NORMAL", 0, Format.FormatR32G32B32Float, 0, 16),
            new("TANGENT", 0, Format.FormatR32G32B32Float, 0, 28),
            new("BINORMAL", 0, Format.FormatR32G32B32Float, 0, 40),
            new("TEXCOORD", 0, Format.FormatR32G32Float, 0, 52),
            new("COLOR", 0, Format.FormatR32G32B32A32Float, 0, 60),
            new("TEXCOORD", 1, Format.FormatR32G32B32A32Float, 0, 76),
            new("TEXCOORD", 2, Format.FormatR32G32B32A32Float, 0, 92),
            new("TEXCOORD", 3, Format.FormatR32G32B32A32Float, 0, 108),
            new("TEXCOORD", 4, Format.FormatR32G32B32A32Float, 0, 124)
        ];

        using var vertexPixelPipeline = device.CreateGraphicsPipelineState(rootSignature,
            screenVertex,
            screenPixel);
        using var geometryPipeline = device.CreateGraphicsPipelineState(rootSignature,
            pointVertex,
            pointPixel,
            pointGeometry,
            topology: PrimitiveTopologyType.Point,
            inputElements: pointLayout);
        using var tessellationPipeline = device.CreateGraphicsPipelineState(rootSignature,
            tessellationVertex,
            meshPixel,
            hullShader: tessellationHull,
            domainShader: tessellationDomain,
            topology: PrimitiveTopologyType.Patch,
            inputElements: meshLayout);

        Assert.NotEqual(nint.Zero, vertexPixelPipeline.NativePointer);
        Assert.NotEqual(nint.Zero, geometryPipeline.NativePointer);
        Assert.NotEqual(nint.Zero, tessellationPipeline.NativePointer);
        device.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Verifies WARP rejects shader bytecode whose declared resources are absent from the root signature.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRejectsIncompatibleRootSignature() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var rootSignature = device.CreateEmptyRootSignature();
        var computeShader = D3D12ShaderModule.Load("CS", "csParticleInsert");

        Assert.Throws<ArgumentException>(() => device.CreateComputePipelineState(rootSignature, computeShader));
    }

    /// <summary>
    ///     Verifies the shared resource manager reuses, updates, replaces, removes, and disposes geometry buffers.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpManagesSharedGeometryResources() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var heap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1, true);
        using var manager = new SilkD3D12ResourceManager(device, heap);
        using var mesh = new DefaultMeshGeometryBufferModel {
            Geometry = new MeshGeometry3D {
                Positions = new Vector3Collection([Vector3.Zero, Vector3.UnitX, Vector3.UnitY]),
                Indices = new IntCollection([0, 1, 2])
            }
        };
        using var line = new DefaultLineGeometryBufferModel {
            Geometry = new LineGeometry3D {
                Positions = new Vector3Collection([Vector3.Zero, Vector3.UnitX]),
                Indices = new IntCollection([0, 1])
            }
        };
        using var point = new DefaultPointGeometryBufferModel {
            Geometry = new PointGeometry3D {Positions = new Vector3Collection([Vector3.Zero])}
        };
        using var instances = new MatrixInstanceBufferModel {Elements = [Matrix.Identity, Matrix.Identity]};

        var firstMesh = manager.GetOrCreate(mesh);
        Assert.Same(firstMesh, manager.GetOrCreate(mesh));
        mesh.Geometry!.Indices = new IntCollection([2, 1, 0]);
        Assert.Same(firstMesh, manager.GetOrCreate(mesh));

        mesh.Geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([Vector3.Zero, Vector3.UnitX, Vector3.UnitY, Vector3.One]),
            Indices = new IntCollection([0, 1, 2, 2, 1, 3])
        };
        var replacementMesh = manager.GetOrCreate(mesh);
        var lineBuffers = manager.GetOrCreate(line);
        var pointBuffers = manager.GetOrCreate(point);

        Assert.NotSame(firstMesh, replacementMesh);
        Assert.True(firstMesh.IsDisposed);
        Assert.Equal(6u, replacementMesh.IndexCount);
        Assert.Same(lineBuffers, manager.GetOrCreate(line));
        Assert.Same(pointBuffers, manager.GetOrCreate(point));
        var firstInstances = manager.GetOrCreate(instances);
        Assert.Same(firstInstances, manager.GetOrCreate(instances));
        instances.Elements = [Matrix.Identity];
        Assert.Same(firstInstances, manager.GetOrCreate(instances));
        instances.Elements = [Matrix.Identity, Matrix.Identity, Matrix.Identity];
        var replacementInstances = manager.GetOrCreate(instances);
        Assert.NotSame(firstInstances, replacementInstances);
        Assert.True(firstInstances!.IsDisposed);
        Assert.Equal(1, manager.InstanceCount);
        instances.Elements = [];
        Assert.Null(manager.GetOrCreate(instances));
        Assert.True(replacementInstances!.IsDisposed);
        Assert.Equal(0, manager.InstanceCount);
        Assert.Equal(3, manager.GeometryCount);
        Assert.True(manager.Remove(mesh));
        Assert.False(manager.Remove(mesh));
        Assert.True(replacementMesh.IsDisposed);
        Assert.Equal(2, manager.GeometryCount);

        manager.Dispose();
        Assert.True(lineBuffers.IsDisposed);
        Assert.True(pointBuffers.IsDisposed);
        Assert.True(manager.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => manager.GetOrCreate(line));
    }

    /// <summary>
    ///     Verifies complete root tables allocate contiguously, map registers, reuse ranges, and reject bad heaps.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpAllocatesGraphicsBindingTables() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 236, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 20, true);
        var first = new SilkD3D12GraphicsBindings(resourceHeap, samplerHeap);
        using var second = new SilkD3D12GraphicsBindings(resourceHeap, samplerHeap);

        Assert.Equal(0, first.ConstantBuffer(0).Index);
        Assert.Equal(9, first.ConstantBuffer(9).Index);
        Assert.Equal(10, first.ShaderResource(0).Index);
        Assert.Equal(112, first.ShaderResource(102).Index);
        Assert.Equal(113, first.UnorderedAccess(0).Index);
        Assert.Equal(117, first.UnorderedAccess(4).Index);
        Assert.Equal(0, first.Sampler(0).Index);
        Assert.Equal(9, first.Sampler(9).Index);
        Assert.Equal(118, second.ConstantBuffer(0).Index);
        Assert.Equal(10, second.Sampler(0).Index);
        Assert.Throws<ArgumentOutOfRangeException>(() => first.ConstantBuffer(10));
        Assert.Throws<ArgumentOutOfRangeException>(() => first.ShaderResource(103));
        Assert.Throws<ArgumentOutOfRangeException>(() => first.UnorderedAccess(5));
        Assert.Throws<ArgumentOutOfRangeException>(() => first.Sampler(10));

        first.Dispose();
        using var reused = new SilkD3D12GraphicsBindings(resourceHeap, samplerHeap);
        Assert.Equal(0, reused.ConstantBuffer(0).Index);
        Assert.Equal(0, reused.Sampler(0).Index);

        using var shortResourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 117, true);
        using var oneTableResourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 118, true);
        using var shortSamplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 9, true);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SilkD3D12GraphicsBindings(shortResourceHeap, samplerHeap));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SilkD3D12GraphicsBindings(oneTableResourceHeap, shortSamplerHeap));
        Assert.Equal(0, oneTableResourceHeap.Count);
    }

    /// <summary>
    ///     Verifies aligned constant buffers preserve structure bytes, offsets, zero fill, and disposal through WARP.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpWritesGraphicsConstantBuffers() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 118, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 10, true);
        using var bindings = new SilkD3D12GraphicsBindings(resourceHeap, samplerHeap);
        using var constantBuffer = new SilkD3D12ConstantBuffer(device,
            bindings.ConstantBuffer(1),
            PhongPbrMaterialStruct.SizeInBytes);
        var model = new ModelStruct {
            World = Matrix.Identity,
            HasInstances = 1,
            Color = new Vector4(1, 0, 0, 1)
        };
        constantBuffer.Write(in model);
        var marker = 42u;
        constantBuffer.Write(in marker, ModelStruct.SizeInBytes);
        using var readback = device.CreateBuffer((ulong) PhongPbrMaterialStruct.SizeInBytes, HeapType.Readback);
        context.Reset();
        context.CopyBuffer(readback,
            0,
            constantBuffer.Resource,
            0,
            (ulong) PhongPbrMaterialStruct.SizeInBytes);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));
        var bytes = readback.Read(PhongPbrMaterialStruct.SizeInBytes);

        Assert.Equal(1f, MemoryMarshal.Cast<byte, float>(bytes)[0]);
        Assert.Equal(1, MemoryMarshal.Cast<byte, int>(bytes)[17]);
        Assert.Equal(42u, MemoryMarshal.Cast<byte, uint>(bytes)[ModelStruct.SizeInBytes / 4]);
        Assert.All(bytes.AsSpan(ModelStruct.SizeInBytes + sizeof(uint)).ToArray(), value => Assert.Equal(0, value));
        Assert.Equal(512UL, constantBuffer.Resource.SizeInBytes);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            constantBuffer.Write(in marker, PhongPbrMaterialStruct.SizeInBytes));
        constantBuffer.Dispose();
        Assert.True(constantBuffer.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => constantBuffer.Write(in marker));
        device.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Verifies the existing light model maps byte-for-byte onto the shared b3 constant buffer.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpWritesExistingLightBuffer() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 118, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 10, true);
        using var bindings = new SilkD3D12MeshBindings(device, resourceHeap, samplerHeap);
        var lightData = new LightsBufferModel {
            AmbientLight = new Vector4(0.1f, 0.2f, 0.3f, 1),
            HasEnvironmentMap = true,
            EnvironmentMapMipLevels = 7
        };
        lightData.Lights[0] = new LightStruct {
            LightType = 3,
            LightDir = new Vector4(1, 2, 3, 4),
            LightColor = new Vector4(0.4f, 0.5f, 0.6f, 1),
            LightView = Matrix.Identity,
            LightProj = Matrix.Identity
        };
        lightData.IncrementLightCount();
        bindings.UpdateLights(lightData);
        using var readback = device.CreateBuffer((ulong) LightsBufferModel.SizeInBytes, HeapType.Readback);
        context.Reset();
        context.CopyBuffer(readback,
            0,
            bindings.LightResource,
            0,
            (ulong) LightsBufferModel.SizeInBytes);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));
        var bytes = readback.Read(LightsBufferModel.SizeInBytes);
        var tailOffset = LightStruct.SizeInBytes * Constants.MaxLights;

        Assert.Equal(LightStruct.SizeInBytes, Marshal.SizeOf<LightStruct>());
        Assert.Equal(3, MemoryMarshal.Read<int>(bytes));
        Assert.Equal(1f, MemoryMarshal.Read<float>(bytes.AsSpan(16)));
        Assert.Equal(0.4f, MemoryMarshal.Read<float>(bytes.AsSpan(80)));
        Assert.Equal(0.1f, MemoryMarshal.Read<float>(bytes.AsSpan(tailOffset)));
        Assert.Equal(1, MemoryMarshal.Read<int>(bytes.AsSpan(tailOffset + 16)));
        Assert.Equal(1, MemoryMarshal.Read<int>(bytes.AsSpan(tailOffset + 20)));
        Assert.Equal(7, MemoryMarshal.Read<int>(bytes.AsSpan(tailOffset + 24)));
        Assert.Equal(0, MemoryMarshal.Read<int>(bytes.AsSpan(tailOffset + 28)));
        device.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Verifies an existing geometry render core selects, caches, binds, draws, and reads back through DX12.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpTraversesExistingGeometryRenderCore() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var rootSignature = device.CreateDefaultRootSignature();
        using var pipeline = device.CreateGraphicsPipelineState(rootSignature,
            D3D12ShaderModule.Load("VS", "vsMeshOutlineScreenQuad"),
            D3D12ShaderModule.Load("PS", "psEffectOutlineQuadStencil"),
            rasterizerState: DefaultRasterDescriptions.RsOutline);
        using var pass = ShaderPass.CreateD3D12("CoreTraversal", rootSignature, pipeline);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 118, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 10, true);
        using var resources = new SilkD3D12ResourceManager(device, resourceHeap);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var samplerTable = samplerHeap.Allocate();
        var resourceDescriptors = Enumerable.Range(0, 7).Select(_ => resourceHeap.Allocate()).ToArray();
        using var constantBuffer = device.CreateBuffer(256, HeapType.Upload);
        using var renderTarget = device.CreateRenderTargetTexture2D(4, 4, Format.FormatR8G8B8A8Unorm);
        using var geometry = new DefaultMeshGeometryBufferModel {
            Geometry = new MeshGeometry3D {
                Positions = new Vector3Collection([
                    new Vector3(-1, 1, 0),
                    new Vector3(1, 1, 0),
                    new Vector3(-1, -1, 0),
                    new Vector3(1, -1, 0)
                ]),
                Indices = new IntCollection([0, 1, 2, 2, 1, 3])
            }
        };
        using var instances = new MatrixInstanceBufferModel {Elements = [Matrix.Identity, Matrix.Identity]};
        using var core = new TraversalGeometryRenderCore {GeometryBuffer = geometry, InstanceBuffer = instances};
        var color = new byte[256];
        System.Buffer.BlockCopy(new[] {1.0f, 0.0f, 0.0f, 1.0f}, 0, color, 0, 16);
        constantBuffer.Write(color);
        device.CreateConstantBufferView(constantBuffer, resourceDescriptors[6], 256);
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);

        try {
            context.Reset();
            Assert.False(core.TryRenderD3D12(context, resources, pass));
            core.AttachD3D12();
            Assert.True(core.IsD3D12Attached);
            Assert.Throws<ArgumentException>(() =>
                core.TryRenderD3D12(context, resources, pass, resourceDescriptors[0]));
            context.ClearRenderTarget(renderTarget, renderTargetView, [0, 0, 0, 1]);
            context.SetRenderTarget(renderTargetView);
            context.SetViewport(4, 4);
            context.SetDescriptorHeaps(resourceHeap, samplerHeap);
            Assert.True(core.TryRenderD3D12(context,
                resources,
                pass,
                resourceDescriptors[0],
                samplerTable));
            Assert.Equal(1, resources.GeometryCount);
            Assert.Equal(1, resources.InstanceCount);
            context.Transition(renderTarget, ResourceStates.CopySource);
            context.CopyTextureToBuffer(readback, renderTarget, in footprint);
            context.Close();
            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

            Assert.Equal([255, 0, 0, 255], readback.Read(4));
            core.DetachD3D12();
            Assert.False(core.IsD3D12Attached);
            device.ThrowIfDeviceRemoved();
        } finally {
            foreach (var descriptor in resourceDescriptors) descriptor.Dispose();
        }
    }

    /// <summary>
    ///     Verifies diffuse, Phong, and PBR cores map exactly onto the fixed cbMesh DX12 payload.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void MeshMaterialsCreateCompleteD3D12Payloads() {
        var model = new ModelStruct {World = Matrix.Identity, HasInstances = 1};
        var texture = new TextureModel(Guid.NewGuid(), new ManagerTextureLoader(TextureInfo.Null));
        var diffuse = new DiffuseMaterialCore {
            DiffuseColor = new Vector4(0.1f, 0.2f, 0.3f, 0.4f),
            DiffuseMap = texture,
            EnableUnLit = true,
            EnableFlatShading = true,
            VertexColorBlendingFactor = 0.25f,
            UvTransform = new UvTransform(0, 2, 3, 4, 5)
        };
        var phong = new PhongMaterialCore {
            DiffuseColor = new Vector4(0.2f, 0.3f, 0.4f, 1),
            AmbientColor = new Vector4(0.1f, 0.1f, 0.1f, 1),
            EmissiveColor = new Vector4(0.5f, 0.4f, 0.3f, 1),
            SpecularColor = new Vector4(0.6f, 0.7f, 0.8f, 1),
            ReflectiveColor = new Vector4(0.9f, 0.8f, 0.7f, 1),
            DiffuseMap = texture,
            DiffuseAlphaMap = texture,
            NormalMap = texture,
            SpecularColorMap = texture,
            DisplacementMap = texture,
            EmissiveMap = texture,
            EnableAutoTangent = true,
            EnableFlatShading = true,
            SpecularShininess = 42,
            VertexColorBlendingFactor = 0.5f
        };
        var pbr = new PbrMaterialCore {
            AlbedoColor = new Vector4(0.8f, 0.7f, 0.6f, 1),
            EmissiveColor = new Vector4(0.1f, 0.2f, 0.3f, 1),
            AmbientOcclusionFactor = 0.2f,
            RoughnessFactor = 0.3f,
            MetallicFactor = 0.4f,
            ReflectanceFactor = 0.5f,
            ClearCoatStrength = 0.6f,
            ClearCoatRoughness = 0.7f,
            AlbedoMap = texture,
            NormalMap = texture,
            RoughnessMetallicMap = texture,
            AmbientOcculsionMap = texture,
            DisplacementMap = texture,
            EmissiveMap = texture,
            IrradianceMap = texture,
            EnableAutoTangent = true,
            EnableFlatShading = true,
            VertexColorBlendingFactor = 0.75f
        };

        var diffuseData = D3D12MeshMaterialData.Create(in model, diffuse);
        var phongData = D3D12MeshMaterialData.Create(in model, phong);
        var pbrData = D3D12MeshMaterialData.Create(in model, pbr);

        Assert.Equal(PhongPbrMaterialStruct.SizeInBytes, Marshal.SizeOf<PhongPbrMaterialStruct>());
        Assert.Equal(0, Marshal.OffsetOf<PhongPbrMaterialStruct>(nameof(PhongPbrMaterialStruct.Model)).ToInt32());
        Assert.Equal(160,
            Marshal.OffsetOf<PhongPbrMaterialStruct>(nameof(PhongPbrMaterialStruct.Diffuse)).ToInt32());
        Assert.Equal(240,
            Marshal.OffsetOf<PhongPbrMaterialStruct>(nameof(PhongPbrMaterialStruct.HasDiffuseMap)).ToInt32());
        Assert.Equal(336,
            Marshal.OffsetOf<PhongPbrMaterialStruct>(nameof(PhongPbrMaterialStruct.VertexColorBlendingAndPadding))
                .ToInt32());
        Assert.Equal(model.World, diffuseData.Model.World);
        Assert.Equal(diffuse.DiffuseColor, diffuseData.Diffuse);
        Assert.Equal(1, diffuseData.HasDiffuseMap);
        Assert.Equal(1, diffuseData.HasNormalMap);
        Assert.Equal(1, diffuseData.RenderFlatFlag);
        Assert.Equal(0.25f, diffuseData.VertexColorBlendingAndPadding.X);
        Assert.Equal(new Vector4(2, 0, 0, 4), diffuseData.UvTransformRow1);
        Assert.Equal(new Vector4(0, 3, 0, 5), diffuseData.UvTransformRow2);
        Assert.Equal(phong.SpecularColor, phongData.SpecularOrPbrFactors);
        Assert.Equal(phong.ReflectiveColor, phongData.ReflectOrClearCoat);
        Assert.Equal(1, phongData.HasDiffuseMap);
        Assert.Equal(1, phongData.HasNormalMap);
        Assert.Equal(1, phongData.HasAlphaOrRoughnessMetallicMap);
        Assert.Equal(1, phongData.HasSpecularOrIrradianceMap);
        Assert.Equal(1, phongData.HasDisplacementMap);
        Assert.Equal(1, phongData.HasEmissiveMap);
        Assert.Equal(42, phongData.Shininess);
        Assert.Equal(new Vector4(0.2f, 0.3f, 0.4f, 0.5f), pbrData.SpecularOrPbrFactors);
        Assert.Equal(new Vector4(0.6f, 0.7f, 0, 1), pbrData.ReflectOrClearCoat);
        Assert.Equal(1, pbrData.RenderPbrFlag);
        Assert.Equal(1, pbrData.HasAlphaOrRoughnessMetallicMap);
        Assert.Equal(1, pbrData.HasSpecularOrIrradianceMap);
        Assert.Equal(DefaultPassNames.Diffuse, D3D12MeshMaterialData.GetPassName(diffuse));
        Assert.Equal(DefaultPassNames.Default, D3D12MeshMaterialData.GetPassName(phong));
        Assert.Equal(DefaultPassNames.Pbr, D3D12MeshMaterialData.GetPassName(pbr));
        phong.EnableTessellation = true;
        pbr.EnableTessellation = true;
        Assert.Equal(DefaultPassNames.MeshTriTessellation, D3D12MeshMaterialData.GetPassName(phong));
        Assert.Equal(DefaultPassNames.MeshPbrTriTessellation, D3D12MeshMaterialData.GetPassName(pbr));
        Assert.Equal(DefaultPassNames.Colors, D3D12MeshMaterialData.GetPassName(ColorMaterialCore.Core));
        Assert.Equal(Color.White, D3D12MeshMaterialData.Create(in model, ColorMaterialCore.Core).Diffuse);
        Assert.Throws<InvalidOperationException>(() => D3D12MeshMaterialData.GetPassName(null));
        Assert.Throws<NotSupportedException>(() =>
            D3D12MeshMaterialData.Create(in model, new BillboardMaterialCore()));
        using var node = new MeshNode {Material = diffuse};
        var nodeCore = Assert.IsType<MeshRenderCore>(node.RenderCore);
        Assert.Same(diffuse, nodeCore.D3D12Material);
        Assert.Equal(DefaultPassNames.Diffuse, nodeCore.D3D12MaterialPassName);
    }

    /// <summary>
    ///     Verifies existing line and point materials map exactly onto cbPointLineModel and their scene cores.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void PointLineMaterialsCreateCompleteD3D12Payloads() {
        var model = new PointLineModelStruct {World = Matrix.Identity, HasInstances = 1};
        var texture = new TextureModel(Guid.NewGuid(), new ManagerTextureLoader(TextureInfo.Null));
        var line = new LineMaterialCore {
            Thickness = 3,
            Smoothness = 0.5f,
            LineColor = new Vector4(1, 0, 0, 1),
            EnableDistanceFading = true,
            FadingNearDistance = 4,
            FadingFarDistance = 8,
            FixedSize = false,
            Texture = texture,
            TextureScale = 2,
            AlphaThreshold = 0.25f
        };
        var point = new PointMaterialCore {
            Width = 5,
            Height = 6,
            Figure = PointFigure.Ellipse,
            FigureRatio = 0.75f,
            PointColor = new Vector4(0, 1, 0, 1),
            EnableDistanceFading = true,
            FadingNearDistance = 7,
            FadingFarDistance = 9,
            FixedSize = false,
            EnableColorBlending = true,
            BlendingFactor = 0.4f
        };
        var billboard = new BillboardMaterialCore {FixedSize = true, Type = BillboardType.Image};

        var lineData = D3D12PointLineMaterialData.Create(in model, line);
        var pointData = D3D12PointLineMaterialData.Create(in model, point);
        var billboardData = D3D12PointLineMaterialData.Create(in model, billboard);

        Assert.Equal(PointLineMaterialStruct.SizeInBytes, Marshal.SizeOf<PointLineMaterialStruct>());
        Assert.Equal(0,
            Marshal.OffsetOf<PointLineMaterialStruct>(nameof(PointLineMaterialStruct.Model)).ToInt32());
        Assert.Equal(80,
            Marshal.OffsetOf<PointLineMaterialStruct>(nameof(PointLineMaterialStruct.Parameters)).ToInt32());
        Assert.Equal(112,
            Marshal.OffsetOf<PointLineMaterialStruct>(nameof(PointLineMaterialStruct.FixedSizeFlag)).ToInt32());
        Assert.Equal(144,
            Marshal.OffsetOf<PointLineMaterialStruct>(nameof(PointLineMaterialStruct.TextureScale)).ToInt32());
        Assert.Equal(new Vector4(3, 0.5f, 0, 0), lineData.Parameters);
        Assert.Equal(line.LineColor, lineData.Color);
        Assert.Equal(0, lineData.FixedSizeFlag);
        Assert.Equal(1, lineData.EnableDistanceFadingFlag);
        Assert.Equal(1, lineData.HasTexture);
        Assert.Equal(2, lineData.TextureScale);
        Assert.Equal(0.25f, lineData.AlphaThreshold);
        Assert.Equal(new Vector4(5, 6, (int) PointFigure.Ellipse, 0.75f), pointData.Parameters);
        Assert.Equal(point.PointColor, pointData.Color);
        Assert.Equal(1, pointData.EnableBlending);
        Assert.Equal(0.4f, pointData.BlendingFactor);
        Assert.Equal(new Vector4((int) BillboardType.Image, 0, 0, 0), billboardData.Parameters);
        Assert.Equal(1, billboardData.FixedSizeFlag);
        Assert.Equal(1, billboardData.HasTexture);
        Assert.Throws<NotSupportedException>(() =>
            D3D12PointLineMaterialData.Create(in model, new DiffuseMaterialCore()));
        using var lineNode = new LineNode {Material = line};
        using var pointNode = new PointNode {Material = point};
        using var billboardNode = new BillboardNode {Material = billboard};
        Assert.Same(line, Assert.IsType<PointLineRenderCore>(lineNode.RenderCore).D3D12Material);
        Assert.Same(point, Assert.IsType<PointLineRenderCore>(pointNode.RenderCore).D3D12Material);
        Assert.Same(billboard,
            Assert.IsType<PointLineRenderCore>(billboardNode.RenderCore).D3D12Material);
        var singleImage = new BillboardSingleImage3D(texture, 8, 6);
        var multipleImages = new BillboardImage3D(texture);
        multipleImages.ImageInfos.Add(new ImageInfo {Position = Vector3.One, Width = 4, Height = 2});
        Assert.True(singleImage.TryPrepareVerticesForD3D12());
        Assert.Single(singleImage.BillboardVertices);
        Assert.True(multipleImages.TryPrepareVerticesForD3D12());
        Assert.Single(multipleImages.BillboardVertices);
        Assert.False(new BillboardSingleText3D().TryPrepareVerticesForD3D12());
    }

    /// <summary>
    ///     Verifies CPU skinning matches the shader's weighted transform contract and validates bone indices.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void BoneSkinningPreparesWeightedDx12Vertices() {
        var vertex = new DefaultVertex {
            Position = new Vector4(0, 0, 0, 1),
            Normal = Vector3.UnitX,
            Tangent = Vector3.UnitY,
            BiTangent = Vector3.UnitZ
        };
        var boneIds = new BoneIds {Bone1 = 0, Bone2 = 1, Weights = new Vector4(0.25f, 0.75f, 0, 0)};
        var first = Matrix.Identity;
        first.M41 = -2;
        var second = Matrix.Identity;
        second.M41 = 2;

        var result = SilkD3D12DefaultMeshBuffers.SkinVertex(in vertex, in boneIds, [first, second]);

        Assert.Equal(new Vector4(1, 0, 0, 1), result.Position);
        Assert.Equal(Vector3.UnitX, result.Normal);
        Assert.Equal(Vector3.UnitY, result.Tangent);
        Assert.Equal(Vector3.UnitZ, result.BiTangent);
        boneIds.Bone1 = 2;
        boneIds.Weights = new Vector4(1, 0, 0, 0);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SilkD3D12DefaultMeshBuffers.SkinVertex(in vertex, in boneIds, [first, second]));
        using var node = new BoneSkinMeshNode {Material = new DiffuseMaterialCore()};
        Assert.Same(node.Material, Assert.IsType<BoneSkinRenderCore>(node.RenderCore).D3D12Material);
        node.BoneMatrices = [Matrix.Identity];
        var frustum = default(BoundingFrustum);
        Assert.True(node.TestViewFrustum(ref frustum));
    }

    /// <summary>
    ///     Verifies morph-target indexing, weights, basis reconstruction, and malformed payload rejection.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void MorphTargetsPrepareDx12VerticesBeforeSkinning() {
        using var morphTargets = new MorphTargetUploaderCore();
        var vertices = new[] {
            new DefaultVertex {
                Position = new Vector4(0, 0, 0, 1),
                Normal = Vector3.UnitX,
                Tangent = Vector3.UnitY,
                BiTangent = Vector3.UnitZ
            },
            new DefaultVertex {
                Position = new Vector4(0, 0, 0, 1),
                Normal = Vector3.UnitX,
                Tangent = Vector3.UnitY,
                BiTangent = Vector3.UnitZ
            }
        };
        var targets = new[] {
            new MorphTargetVertex {deltaPosition = new Vector3(2, 0, 0)},
            new MorphTargetVertex(),
            new MorphTargetVertex(),
            new MorphTargetVertex {deltaPosition = new Vector3(0, 4, 0)}
        };
        morphTargets.MorphTargetWeights = [0.5f, 0.25f];
        Assert.True(morphTargets.InitializeMorphTargets(targets, 2));

        Assert.True(morphTargets.HasMorphTarget);
        Assert.Equal(2, morphTargets.MorphTargetCount);
        Assert.Equal(2, morphTargets.MorphTargetPitch);
        Assert.Equal(new[] {0.5f, 0.25f}, morphTargets.D3D12Weights.ToArray());
        Assert.Equal(new[] {3, 0, 0, 6}, morphTargets.D3D12Offsets.ToArray());
        Assert.Equal(9, morphTargets.D3D12Deltas.Length);

        Assert.True(morphTargets.ApplyD3D12MorphTargets(vertices));

        Assert.Equal(new Vector4(1, 0, 0, 1), vertices[0].Position);
        Assert.Equal(new Vector4(0, 1, 0, 1), vertices[1].Position);
        Assert.All(vertices, vertex => {
            Assert.Equal(Vector3.UnitX, vertex.Normal);
            Assert.Equal(Vector3.UnitY, vertex.Tangent);
            Assert.Equal(Vector3.UnitZ, vertex.BiTangent);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            morphTargets.InitializeMorphTargets([new MorphTargetVertex()], 0));
        Assert.Throws<ArgumentException>(() =>
            morphTargets.InitializeMorphTargets([new MorphTargetVertex(), new MorphTargetVertex(),
                new MorphTargetVertex()], 2));
    }

    /// <summary>
    ///     Verifies the shared scene selector preserves camera culling and the disabled-frustum contract.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void SceneNodeSelectorAppliesCameraFrustumToDx12Candidates() {
        using var inside = CreateFrustumTestNode(0);
        using var outside = CreateFrustumTestNode(10);
        var candidates = new FastList<SceneNode>(2) {inside, outside};
        var visible = new FastList<SceneNode>(2);
        var frustum = new BoundingFrustum(Matrix.Identity);

        SceneNodeFrustumSelector.AppendVisible(candidates, visible, true, ref frustum);

        Assert.Collection(visible, node => Assert.Same(inside, node));
        Assert.True(inside.IsInFrustum);
        Assert.False(outside.IsInFrustum);
        visible.Clear();
        SceneNodeFrustumSelector.AppendVisible(candidates, visible, false, ref frustum);
        Assert.Collection(visible,
            node => Assert.Same(inside, node),
            node => Assert.Same(outside, node));
        Assert.All(visible, node => Assert.True(node.IsInFrustum));
    }

    /// <summary>
    ///     Verifies scene-node types and existing materials select the expected productive DX12 technique and pass.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ScenePassCatalogMapsExistingGeometryNodes() {
        using var mesh = new MeshNode {Material = new ColorMaterialCore()};
        using var bone = new BoneSkinMeshNode {Material = new DiffuseMaterialCore()};
        using var line = new LineNode {Material = new LineMaterialCore()};
        using var point = new PointNode {Material = new PointMaterialCore()};
        using var billboard = new BillboardNode {Material = new BillboardMaterialCore()};
        using var environment = new EnvironmentMapNode();
        using var volume = new VolumeTextureNode {
            Material = new VolumeTextureRawDataMaterialCore {
                VolumeTexture = new VolumeTextureParams([255], 1, 1, 1, Format.FormatR8Unorm)
            }
        };
        using var particle = new ParticleStormNode();

        Assert.Equal((DefaultRenderTechniqueNames.Mesh, DefaultPassNames.Colors),
            D3D12ScenePassCatalog.GetSelection(mesh));
        Assert.Equal((DefaultRenderTechniqueNames.Mesh, DefaultPassNames.Diffuse),
            D3D12ScenePassCatalog.GetSelection(bone));
        Assert.Equal((DefaultRenderTechniqueNames.Lines, DefaultPassNames.Default),
            D3D12ScenePassCatalog.GetSelection(line));
        Assert.Equal((DefaultRenderTechniqueNames.Points, DefaultPassNames.Default),
            D3D12ScenePassCatalog.GetSelection(point));
        Assert.Equal((DefaultRenderTechniqueNames.BillboardText, DefaultPassNames.Default),
            D3D12ScenePassCatalog.GetSelection(billboard));
        Assert.Equal((DefaultRenderTechniqueNames.Volume3D, DefaultPassNames.Default),
            D3D12ScenePassCatalog.GetSelection(volume));
        Assert.Equal((DefaultRenderTechniqueNames.ParticleStorm, DefaultParticlePassNames.Default),
            D3D12ScenePassCatalog.GetSelection(particle));
        Assert.Equal((null, null), D3D12ScenePassCatalog.GetSelection(environment));
    }

    /// <summary>
    ///     Verifies the particle CPU payload retains its exact layout, capacity limit, and insertion cadence.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ParticleCoreCreatesCompleteD3D12Payloads() {
        using var core = new ParticleRenderCore {
            ParticleCount = 16,
            ParticleSize = new Vector2(0.5f),
            EmitterLocation = Vector3.One,
            ParticleBlendColor = new Vector4(1, 0, 0, 1)
        };
        core.AttachD3D12();

        Assert.True(core.PrepareD3D12(1,
            out var frame,
            out var insert,
            out var model));
        Assert.Equal(ParticlePerFrame.SizeInBytes, Marshal.SizeOf<ParticlePerFrame>());
        Assert.Equal(ParticleInsertParameters.SizeInBytes, Marshal.SizeOf<ParticleInsertParameters>());
        Assert.Equal(ParticleModelStruct.SizeInBytes, Marshal.SizeOf<ParticleModelStruct>());
        Assert.Equal(16u, frame.MaxParticles);
        Assert.Equal(new Vector2(0.5f), frame.ParticleSize);
        Assert.Equal(Vector3.One, insert.EmitterLocation);
        Assert.Equal(new Vector4(1, 0, 0, 1), insert.ParticleBlendColor);
        Assert.Equal(Matrix.Identity, model.World);
        Assert.False(core.PrepareD3D12(1, out _, out _, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => core.PrepareD3D12(double.NaN,
            out _,
            out _,
            out _));
    }

    /// <summary>
    ///     Verifies repository compute and geometry shaders insert eight particles and draw them indirectly on WARP.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpSimulatesAndDrawsExistingParticleNode() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var catalog = new D3D12ScenePassCatalog(device,
            Format.FormatR8G8B8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 256, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 20, true);
        using var renderer = new SilkD3D12SceneRenderer(device, resourceHeap, samplerHeap);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var renderTarget = device.CreateRenderTargetTexture2D(16, 16, Format.FormatR8G8B8A8Unorm);
        using var depthHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        using var depthView = depthHeap.Allocate();
        using var depth = device.CreateDepthStencilTexture2D(16, 16, Format.FormatD32FloatS8X24Uint);
        using var node = new ParticleStormNode {
            ParticleCount = 16,
            ParticleSize = new Vector2(0.5f),
            EmitterRadius = 0,
            InitialVelocity = 0,
            BlendColor = new Vector4(1, 0, 0, 1)
        };
        var nodes = new FastList<SceneNode>(1) {node};
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity,
            Projection = Matrix.Identity,
            ViewProjection = Matrix.Identity,
            Viewport = new Vector4(16, 16, 1f / 16, 1f / 16),
            Resolution = new Vector4(16, 16, 1f / 16, 1f / 16),
            EyePos = new Vector3(0, 0, 2),
            IsPerspective = true,
            TimeStamp = 1,
            DpiScale = 1
        };
        var frustum = new BoundingFrustum(Matrix.Identity);
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        device.CreateDepthStencilView(depth, depthView);
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var pixelReadback = device.CreateBuffer(totalBytes, HeapType.Readback);
        using var counterReadback = device.CreateBuffer(sizeof(uint), HeapType.Readback);
        using var argumentReadback = device.CreateBuffer(ParticleCountIndirectArgs.SizeInBytes, HeapType.Readback);
        context.Reset();
        context.ClearRenderTarget(renderTarget, renderTargetView, [0, 0, 1, 1]);
        context.ClearDepthStencil(depth, depthView);
        context.SetRenderTargets(renderTargetView, depthView);
        context.SetViewport(16, 16);

        var recorded = renderer.RenderParticles(context,
            nodes,
            catalog.ResolveParticleUpdate,
            catalog.ResolveParticleInsert,
            catalog.ResolveParticle,
            in transforms,
            false,
            ref frustum);
        var particle = Assert.IsType<SilkD3D12ParticleResources>(
            renderer.FindParticleResources((ParticleRenderCore) node.RenderCore));
        context.CopyBuffer(counterReadback, 0, particle.RenderCounter, 0, sizeof(uint));
        context.Transition(particle.Arguments, ResourceStates.CopySource);
        context.CopyBuffer(argumentReadback, 0, particle.Arguments, 0, ParticleCountIndirectArgs.SizeInBytes);
        context.Transition(renderTarget, ResourceStates.CopySource);
        context.CopyTextureToBuffer(pixelReadback, renderTarget, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        device.ThrowIfDeviceRemoved();
        Assert.Equal(1, recorded);
        Assert.Equal(8u, MemoryMarshal.Read<uint>(counterReadback.Read(sizeof(uint))));
        var arguments = MemoryMarshal.Read<ParticleCountIndirectArgs>(
            argumentReadback.Read(ParticleCountIndirectArgs.SizeInBytes));
        Assert.Equal(8u, arguments.VertexCount);
        Assert.Equal(1u, arguments.InstanceCount);
        var pixel = pixelReadback.Read(4, footprint.Offset + 8UL * footprint.Footprint.RowPitch + 8UL * 4);
        Assert.True(pixel[0] > 200, $"Expected red particles, got [{string.Join(',', pixel)}].");
        Assert.True(pixel[2] < 40, $"Expected particles to replace the blue clear, got [{string.Join(',', pixel)}].");
        renderer.Dispose();
        Assert.False(node.RenderCore.IsD3D12Attached);
        Assert.Equal(0, resourceHeap.Count);
        Assert.Equal(0, samplerHeap.Count);
    }

    /// <summary>
    ///     Verifies raw and gradient volume materials map to the exact b4 layout and existing pass contracts.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void VolumeMaterialsCreateDx12PayloadAndPassSelection() {
        var material = new VolumeTextureRawDataMaterialCore {
            VolumeTexture = new VolumeTextureParams([1, 2, 3, 4, 5, 6, 7, 8],
                2,
                2,
                2,
                Format.FormatR8Unorm),
            Color = new Vector4(1, 0.5f, 0.25f, 0.75f),
            SampleDistance = 0.5,
            IterationOffset = 3,
            MaxIterations = 17,
            IsoValue = 0.25,
            EnablePlaneAlignment = true,
            TransferMap = [new Vector4(1, 0, 0, 1), new Vector4(0, 1, 0, 1)]
        };
        var model = Matrix.Identity;
        model.M41 = 4;

        var data = D3D12VolumeMaterialData.Create(in model, material, 2, 4, 8);

        Assert.Equal(D3D12VolumeParams.SizeInBytes, Marshal.SizeOf<D3D12VolumeParams>());
        Assert.Equal(model, data.World);
        Assert.Equal(-4, data.WorldInverse.M41);
        Assert.Equal(material.Color, data.Color);
        Assert.Equal(0.0625f, data.StepSize);
        Assert.Equal(3u, data.IterationOffset);
        Assert.Equal(1, data.EnablePlaneAlignment);
        Assert.Equal(17u, data.MaxIterations);
        Assert.Equal(1, data.HasGradientMap);
        Assert.Equal(0.25f, data.IsoValue);
        Assert.Equal(0.5f, data.ActualSampleDistance);
        using var node = new VolumeTextureNode { Material = material };
        Assert.Equal(DefaultPassNames.Default, ((VolumeRenderCore) node.RenderCore).D3D12MaterialPassName);
        node.Material = new VolumeTextureDiffuseMaterialCore {
            VolumeTexture = new VolumeTextureGradientParams([new Half4()], 1, 1, 1)
        };
        Assert.Equal(DefaultPassNames.Diffuse, ((VolumeRenderCore) node.RenderCore).D3D12MaterialPassName);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            D3D12VolumeMaterialData.Create(in model, material, 0, 1, 1));
    }

    /// <summary>
    ///     Verifies an existing raw-volume node records backface and ray-march passes through repository DXIL on WARP.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRendersExistingRawVolumeNode() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var catalog = new D3D12ScenePassCatalog(device,
            Format.FormatR8G8B8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 256, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 20, true);
        using var renderer = new SilkD3D12SceneRenderer(device, resourceHeap, samplerHeap);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var renderTarget = device.CreateRenderTargetTexture2D(16, 16, Format.FormatR8G8B8A8Unorm);
        using var depthStencilHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        using var depthStencilView = depthStencilHeap.Allocate();
        using var depthStencil = device.CreateDepthStencilTexture2D(16,
            16,
            Format.FormatD32FloatS8X24Uint);
        using var volume = new VolumeTextureNode {
            Material = new VolumeTextureRawDataMaterialCore {
                VolumeTexture = new VolumeTextureParams(Enumerable.Repeat((byte) 255, 8).ToArray(),
                    2,
                    2,
                    2,
                    Format.FormatR8Unorm),
                Color = new Vector4(1, 0, 0, 1),
                SampleDistance = 0.25,
                MaxIterations = 32,
                IsoValue = 0
            }
        };
        var volumes = new FastList<SceneNode>(1) { volume };
        var opaque = new FastList<SceneNode>();
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity,
            Projection = Matrix.Identity,
            ViewProjection = Matrix.Identity,
            Viewport = new Vector4(16, 16, 1f / 16, 1f / 16),
            Resolution = new Vector4(16, 16, 1f / 16, 1f / 16),
            EyePos = new Vector3(0, 0, 2),
            IsPerspective = true,
            DpiScale = 1
        };
        var frustum = new BoundingFrustum(Matrix.Identity);
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        device.CreateDepthStencilView(depthStencil, depthStencilView);
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        context.Reset();
        context.ClearRenderTarget(renderTarget, renderTargetView, [0, 0, 1, 1]);
        context.ClearDepthStencil(depthStencil, depthStencilView);
        context.SetRenderTargets(renderTargetView, depthStencilView);
        context.SetViewport(16, 16);

        var recorded = renderer.RenderVolumes(context,
            volumes,
            opaque,
            catalog.ResolveVolumeBack,
            catalog.ResolveVolume,
            catalog.ResolveVolumePositions,
            catalog.ResolveBoneSkinning,
            in transforms,
            false,
            ref frustum,
            renderTargetView,
            depthStencilView,
            16,
            16);
        var backPositions = Assert.IsType<SilkD3D12Resource>(renderer.VolumeBackPositions);
        var backFootprint = device.GetCopyableFootprint(backPositions, out var backBytes);
        using var backReadback = device.CreateBuffer(backBytes, HeapType.Readback);
        context.Transition(backPositions, ResourceStates.CopySource);
        context.CopyTextureToBuffer(backReadback, backPositions, in backFootprint);
        context.Transition(renderTarget, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, renderTarget, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        device.ThrowIfDeviceRemoved();
        Assert.Equal(1, recorded);
        Assert.True(volume.RenderCore.IsD3D12Attached);
        var backPixel = backReadback.Read(8,
            backFootprint.Offset + 8UL * backFootprint.Footprint.RowPitch + 8UL * 8);
        var backZ = BitConverter.UInt16BitsToHalf(BitConverter.ToUInt16(backPixel, 4));
        Assert.True(backZ < (Half) (-0.4f),
            $"Expected the cube backface at negative Z, got {backZ} from [{string.Join(',', backPixel)}].");
        var pixel = readback.Read(4, footprint.Offset + 8UL * footprint.Footprint.RowPitch + 8UL * 4);
        Assert.True(pixel[0] > 200, $"Expected red volume output, got [{string.Join(',', pixel)}].");
        Assert.True(pixel[2] < 40, $"Expected the opaque red volume to replace the blue clear, got [{string.Join(',', pixel)}].");
        renderer.Dispose();
        Assert.False(volume.RenderCore.IsD3D12Attached);
        Assert.Equal(0, resourceHeap.Count);
        Assert.Equal(0, samplerHeap.Count);
    }

    /// <summary>
    ///     Verifies existing ambient, directional, point, and spot nodes create the exact shared DX12 light model.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void SceneRendererCollectsExistingLightNodesIntoSharedPayload() {
        using var ambient = new AmbientLightNode {Color = new Vector4(0.1f, 0.2f, 0.3f, 1)};
        using var directional = new DirectionalLightNode {
            Color = new Vector4(1, 0, 0, 1),
            Direction = Vector3.UnitZ
        };
        var pointTransform = Matrix.Identity;
        pointTransform.M41 = 4;
        pointTransform.M42 = 5;
        pointTransform.M43 = 6;
        using var point = new PointLightNode {
            Color = new Vector4(0, 1, 0, 1),
            Position = new Vector3(1, 2, 3),
            Attenuation = new Vector3(1, 0.5f, 0.25f),
            Range = 42,
            ModelMatrix = pointTransform
        };
        using var spot = new SpotLightNode {
            Color = new Vector4(0, 0, 1, 1),
            Position = new Vector3(2, 3, 4),
            Direction = -Vector3.UnitZ,
            Attenuation = new Vector3(1, 0, 0),
            Range = 84,
            InnerAngle = 10,
            OuterAngle = 40,
            FallOff = 2
        };
        var overflow = Enumerable.Range(0, Constants.MaxLights)
            .Select(_ => new DirectionalLightNode {Direction = Vector3.UnitX})
            .ToArray();
        var nodes = new FastList<SceneNode>(4 + overflow.Length) {ambient, directional, point, spot};
        nodes.AddRange(overflow);
        var lights = new LightsBufferModel();
        try {
            SilkD3D12SceneRenderer.UpdateLights(nodes, lights);

            Assert.Equal(Constants.MaxLights, lights.LightCount);
            Assert.Equal(ambient.Color, lights.AmbientLight);
            Assert.Equal((int) LightType.Directional, lights.Lights[0].LightType);
            Assert.Equal(new Vector4(0, 0, -1, 0), lights.Lights[0].LightDir);
            Assert.Equal(directional.Color, lights.Lights[0].LightColor);
            Assert.Equal((int) LightType.Point, lights.Lights[1].LightType);
            Assert.Equal(new Vector4(5, 7, 9, 1), lights.Lights[1].LightPos);
            Assert.Equal(new Vector4(1, 0.5f, 0.25f, 42), lights.Lights[1].LightAtt);
            Assert.Equal((int) LightType.Spot, lights.Lights[2].LightType);
            Assert.Equal(new Vector4(2, 3, 4, 1), lights.Lights[2].LightPos);
            Assert.Equal(new Vector4(0, 0, -1, 0), lights.Lights[2].LightDir);
            Assert.Equal(2, lights.Lights[2].LightSpot.Z);
        } finally {
            foreach (var node in overflow) node.Dispose();
        }
    }

    /// <summary>
    ///     Verifies visible existing scene nodes attach without DX11 and record through the shared DX12 traversal.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRendersOnlyVisibleMeshSceneNodesThroughDx12Traversal() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var catalog = new D3D12ScenePassCatalog(device,
            Format.FormatR8G8B8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 236, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 20, true);
        using var renderer = new SilkD3D12SceneRenderer(device, resourceHeap, samplerHeap);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var renderTarget = device.CreateRenderTargetTexture2D(8, 8, Format.FormatR8G8B8A8Unorm);
        using var depthStencilHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        using var depthStencilView = depthStencilHeap.Allocate();
        using var depthStencil = device.CreateDepthStencilTexture2D(8,
            8,
            Format.FormatD32FloatS8X24Uint);
        using var near = CreateColoredQuadNode(0, new Vector4(0, 1, 0, 1), 0.25f);
        using var far = CreateColoredQuadNode(0, new Vector4(1, 0, 0, 1), 0.75f);
        using var outside = CreateColoredQuadNode(10, new Vector4(0, 0, 1, 1));
        var candidates = new FastList<SceneNode>(3) {near, far, outside};
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity,
            Projection = Matrix.Identity,
            ViewProjection = Matrix.Identity,
            Viewport = new Vector4(8, 8, 0.125f, 0.125f),
            Resolution = new Vector4(8, 8, 0.125f, 0.125f),
            EyePos = new Vector3(0, 0, 2),
            DpiScale = 1
        };
        var frustum = new BoundingFrustum(Matrix.Identity);
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        device.CreateDepthStencilView(depthStencil, depthStencilView);
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        context.Reset();
        context.ClearRenderTarget(renderTarget, renderTargetView, [0, 0, 1, 1]);
        context.ClearDepthStencil(depthStencil, depthStencilView);
        context.SetRenderTargets(renderTargetView, depthStencilView);
        context.SetViewport(8, 8);

        var recorded = renderer.RenderVisible(context,
            candidates,
            catalog.Resolve,
            in transforms,
            true,
            ref frustum);
        context.Transition(renderTarget, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, renderTarget, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        device.ThrowIfDeviceRemoved();
        Assert.Equal(2, renderer.VisibleCount);
        Assert.Equal(2, recorded);
        Assert.True(near.RenderCore.IsD3D12Attached);
        Assert.True(far.RenderCore.IsD3D12Attached);
        Assert.False(outside.RenderCore.IsD3D12Attached);
        var centerPixelOffset = footprint.Offset + 4UL * footprint.Footprint.RowPitch + 4UL * 4;
        Assert.Equal([0, 255, 0, 255], readback.Read(4, centerPixelOffset));
        renderer.Dispose();
        Assert.False(near.RenderCore.IsD3D12Attached);
        Assert.False(far.RenderCore.IsD3D12Attached);
        Assert.Equal(0, resourceHeap.Count);
        Assert.Equal(0, samplerHeap.Count);
    }

    /// <summary>
    ///     Verifies transparent material families select their exact weighted and depth-peeling repository passes.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TransparencyPassSelectionCoversMaterialFamiliesAndStages() {
        using var diffuse = CreateDiffuseQuadNode(new Vector4(1, 0, 0, 0.5f));
        using var phong = CreateDiffuseQuadNode(Vector4.One);
        using var pbr = CreateDiffuseQuadNode(Vector4.One);
        phong.Material = new PhongMaterialCore();
        pbr.Material = new PbrMaterialCore {EnableTessellation = true};

        Assert.Equal(DefaultPassNames.DiffuseOit,
            D3D12ScenePassCatalog.GetTransparencyPassName(diffuse, false, false));
        Assert.Equal(DefaultPassNames.DiffuseOitdp,
            D3D12ScenePassCatalog.GetTransparencyPassName(diffuse, true, false));
        Assert.Equal(DefaultPassNames.OitDepthPeelingInit,
            D3D12ScenePassCatalog.GetTransparencyPassName(diffuse, true, true));
        Assert.Equal(DefaultPassNames.OitPass,
            D3D12ScenePassCatalog.GetTransparencyPassName(phong, false, false));
        Assert.Equal(DefaultPassNames.MeshPbrTriTessellationOitdp,
            D3D12ScenePassCatalog.GetTransparencyPassName(pbr, true, false));
        using var unsupported = CreateColoredQuadNode(0, Vector4.One);
        Assert.Null(D3D12ScenePassCatalog.GetTransparencyPassName(unsupported, false, false));
    }

    /// <summary>
    ///     Verifies weighted OIT records a transparent existing mesh, composites stable alpha, resizes, and releases tables.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpCompositesWeightedTransparencyAndResizesTargets() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var catalog = new D3D12ScenePassCatalog(device,
            Format.FormatB8G8R8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 512, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 40, true);
        using var renderer = new SilkD3D12SceneRenderer(device, resourceHeap, samplerHeap);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var renderTarget = device.CreateRenderTargetTexture2D(8, 8, Format.FormatB8G8R8A8Unorm);
        using var depthHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        using var depthView = depthHeap.Allocate();
        using var depth = device.CreateDepthStencilTexture2D(8, 8, Format.FormatD32FloatS8X24Uint);
        using var node = CreateDiffuseQuadNode(new Vector4(1, 0, 0, 0.5f));
        var nodes = new FastList<SceneNode>(1) {node};
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity,
            Projection = Matrix.Identity,
            ViewProjection = Matrix.Identity,
            Viewport = new Vector4(8, 8, 0.125f, 0.125f),
            Resolution = new Vector4(8, 8, 0.125f, 0.125f),
            EyePos = new Vector3(0, 0, 2),
            OITWeightPower = 3,
            OITWeightDepthSlope = 1,
            OITWeightMode = (int) OitWeightMode.Linear1,
            DpiScale = 1
        };
        var frustum = new BoundingFrustum(Matrix.Identity);
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        device.CreateDepthStencilView(depth, depthView);
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        context.Reset();
        context.ClearRenderTarget(renderTarget, renderTargetView, [0, 0, 1, 1]);
        context.ClearDepthStencil(depth, depthView);
        context.SetRenderTargets(renderTargetView, depthView);
        context.SetViewport(8, 8);

        var recorded = renderer.RenderWeightedTransparency(context,
            nodes,
            catalog.ResolveWeightedTransparency,
            catalog.ResolveWeightedComposition(),
            in transforms,
            false,
            ref frustum,
            renderTargetView,
            depthView,
            8,
            8,
            null,
            null,
            catalog.ResolveBoneSkinning,
            null);
        context.Transition(renderTarget, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, renderTarget, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        device.ThrowIfDeviceRemoved();
        Assert.Equal(1, recorded);
        var pixel = readback.Read(4, footprint.Offset + 4UL * footprint.Footprint.RowPitch + 4UL * 4);
        Assert.InRange(pixel[2], 120, 135);
        Assert.InRange(pixel[0], 120, 135);
        Assert.Equal(255, pixel[3]);
        renderer.Dispose();
        Assert.Equal(0, resourceHeap.Count);
        Assert.Equal(0, samplerHeap.Count);
    }

    /// <summary>
    ///     Verifies dual-depth peeling preserves the opaque frame and composites a stable transparent layer.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpCompositesDepthPeelingLayers() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var catalog = new D3D12ScenePassCatalog(device,
            Format.FormatB8G8R8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 512, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 40, true);
        using var renderer = new SilkD3D12SceneRenderer(device, resourceHeap, samplerHeap);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var renderTarget = device.CreateRenderTargetTexture2D(8, 8, Format.FormatB8G8R8A8Unorm);
        using var depthHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        using var depthView = depthHeap.Allocate();
        using var depth = device.CreateDepthStencilTexture2D(8, 8, Format.FormatD32FloatS8X24Uint);
        using var node = CreateDiffuseQuadNode(new Vector4(1, 0, 0, 0.5f));
        var nodes = new FastList<SceneNode>(1) {node};
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity,
            Projection = Matrix.Identity,
            ViewProjection = Matrix.Identity,
            Viewport = new Vector4(8, 8, 0.125f, 0.125f),
            Resolution = new Vector4(8, 8, 0.125f, 0.125f),
            EyePos = new Vector3(0, 0, 2),
            DpiScale = 1
        };
        var frustum = new BoundingFrustum(Matrix.Identity);
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        device.CreateDepthStencilView(depth, depthView);
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        context.Reset();
        context.ClearRenderTarget(renderTarget, renderTargetView, [0, 0, 1, 1]);
        context.ClearDepthStencil(depth, depthView);
        context.SetRenderTargets(renderTargetView, depthView);
        context.SetViewport(8, 8);

        var recorded = renderer.RenderDepthPeeling(context,
            nodes,
            catalog.ResolveDepthPeelingInitialization,
            catalog.ResolveDepthPeeling,
            catalog.ResolveDepthPeelingComposition(),
            2,
            in transforms,
            false,
            ref frustum,
            renderTarget,
            renderTargetView,
            depthView,
            8,
            8,
            null,
            null,
            catalog.ResolveBoneSkinning,
            null);
        context.Transition(renderTarget, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, renderTarget, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        device.ThrowIfDeviceRemoved();
        Assert.Equal(2, recorded);
        var pixel = readback.Read(4, footprint.Offset + 4UL * footprint.Footprint.RowPitch + 4UL * 4);
        Assert.True(pixel[2] is >= 120 and <= 135, $"Depth-peeling center pixel: {string.Join(',', pixel)}");
        Assert.InRange(pixel[0], 120, 135);
        Assert.Equal(255, pixel[3]);
        renderer.Dispose();
        Assert.Equal(0, resourceHeap.Count);
        Assert.Equal(0, samplerHeap.Count);
    }

    /// <summary>
    ///     Verifies the scene catalog resolves and caches the bone-skinning stream-output pass only for bone nodes.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpResolvesBoneSkinningScenePass() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var catalog = new D3D12ScenePassCatalog(device,
            Format.FormatR8G8B8A8Unorm,
            Format.FormatD32Float);
        using var boneNode = new BoneSkinMeshNode();
        using var meshNode = new MeshNode();

        var pass = catalog.ResolveBoneSkinning(boneNode);

        Assert.NotNull(pass);
        Assert.Same(pass, catalog.ResolveBoneSkinning(boneNode));
        Assert.Equal(DefaultPassNames.PreComputeMeshBoneSkinned, pass.Name);
        Assert.Equal(PrimitiveTopology.PointList, pass.Topology);
        Assert.Null(catalog.ResolveBoneSkinning(meshNode));
        device.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Verifies an existing shadow node resolves directional-light camera state into the exact b5 payload.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ShadowMapNodeCreatesDx12LightPayload() {
        using var shadow = new ShadowMapNode {
            Resolution = new Size2(32, 16),
            Bias = 0.01f,
            Intensity = 0.75f,
            Distance = 4,
            OrthoWidth = 6
        };
        using var light = new DirectionalLightNode {Direction = -Vector3.UnitZ};
        using var caster = CreateColoredQuadNode(0, Vector4.One, 0.25f);
        caster.IsThrowingShadow = true;
        var lights = new FastList<SceneNode>(1) {light};
        var opaque = new FastList<SceneNode>(1) {caster};

        Assert.True(shadow.TryCreateD3D12Parameters(lights,
            opaque,
            out var parameters,
            out var frustum));

        Assert.Equal(new Vector2(32, 16), parameters.ShadowMapSize);
        Assert.Equal(1, parameters.HasShadowMap);
        Assert.Equal(0.75f, parameters.ShadowMapInfo.X);
        Assert.Equal(0.01f, parameters.ShadowMapInfo.Z);
        Assert.NotEqual(Matrix.Identity, parameters.LightView);
        Assert.NotEqual(Matrix.Identity, parameters.LightProjection);
        Assert.True(caster.TestViewFrustum(ref frustum));
        Assert.False(shadow.TryCreateD3D12Parameters([], opaque, out parameters, out frustum));
        Assert.Equal(0, parameters.HasShadowMap);
    }

    /// <summary>
    ///     Verifies the native shadow pass writes D32 depth, transitions it for sampling, and releases all tables.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRendersExistingMeshIntoShadowDepth() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 256, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 32, true);
        using var renderer = new SilkD3D12SceneRenderer(device, resourceHeap, samplerHeap);
        using var catalog = new D3D12ScenePassCatalog(device,
            Format.FormatR8G8B8A8Unorm,
            Format.FormatD32Float);
        using var caster = CreateColoredQuadNode(0, Vector4.One, 0.25f);
        caster.IsThrowingShadow = true;
        var candidates = new FastList<SceneNode>(1) {caster};
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity,
            Projection = Matrix.Identity,
            ViewProjection = Matrix.Identity,
            Viewport = new Vector4(8, 8, 0.125f, 0.125f),
            Resolution = new Vector4(8, 8, 0.125f, 0.125f),
            EyePos = new Vector3(0, 0, 2),
            DpiScale = 1
        };
        var parameters = new ShadowMapParamStruct {
            ShadowMapSize = new Vector2(8, 8),
            HasShadowMap = 1,
            LightView = Matrix.Identity,
            LightProjection = Matrix.Identity
        };
        var frustum = new BoundingFrustum(Matrix.Identity);
        context.Reset();

        var recorded = renderer.RenderShadows(context,
            candidates,
            catalog.ResolveShadow,
            catalog.ResolveBoneSkinning,
            in transforms,
            in parameters,
            ref frustum);
        var shadowResource = Assert.IsType<SilkD3D12Resource>(renderer.ShadowResource);
        Assert.Equal(ResourceStates.PixelShaderResource, shadowResource.State);
        Assert.Equal(Format.FormatR32Typeless, shadowResource.Description.Format);
        var footprint = device.GetCopyableFootprint(shadowResource, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        context.Transition(shadowResource, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, shadowResource, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        device.ThrowIfDeviceRemoved();
        Assert.Equal(1, recorded);
        var centerOffset = footprint.Offset + 4UL * footprint.Footprint.RowPitch + 4UL * sizeof(float);
        Assert.InRange(BitConverter.ToSingle(readback.Read(sizeof(float), centerOffset)), 0.24f, 0.26f);
        renderer.Dispose();
        Assert.Equal(0, resourceHeap.Count);
        Assert.Equal(0, samplerHeap.Count);
    }

    /// <summary>
    ///     Verifies an existing mesh core selects a default pass and renders from b0/b1 camera/model data on WARP.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRendersMeshCoreWithCameraAndModelBindings() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var rootSignature = device.CreateDefaultRootSignature();
        using var cache = new D3D12PipelineStateCache();
        var techniqueDescription = DefaultEffectsManager.LoadTechniqueDescriptions()
            .Single(description => description.Name == DefaultRenderTechniqueNames.Mesh);
        var passDescription = techniqueDescription.PassDescriptions!
            .Single(description => description.Name == DefaultPassNames.Colors);
        passDescription.DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil;
        passDescription.RasterStateDescription = DefaultRasterDescriptions.RsOutline;
        using var pass = passDescription.CreateD3D12(device,
            rootSignature,
            cache,
            techniqueDescription.InputLayoutDescription,
            PrimitiveTopology.TriangleList);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 118, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 10, true);
        using var bindings = new SilkD3D12MeshBindings(device, resourceHeap, samplerHeap);
        using var resources = new SilkD3D12ResourceManager(device, resourceHeap);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var renderTarget = device.CreateRenderTargetTexture2D(4, 4, Format.FormatR8G8B8A8Unorm);
        using var geometry = new DefaultMeshGeometryBufferModel {
            Geometry = new MeshGeometry3D {
                Positions = new Vector3Collection([
                    new Vector3(-1, 1, 0),
                    new Vector3(1, 1, 0),
                    new Vector3(-1, -1, 0),
                    new Vector3(1, -1, 0)
                ]),
                Colors = new Color4Collection([
                    new Vector4(1, 0, 0, 1),
                    new Vector4(1, 0, 0, 1),
                    new Vector4(1, 0, 0, 1),
                    new Vector4(1, 0, 0, 1)
                ]),
                Indices = new IntCollection([0, 1, 2, 2, 1, 3])
            }
        };
        using var instances = new MatrixInstanceBufferModel {Elements = [Matrix.Identity]};
        using var core = new MeshRenderCore {GeometryBuffer = geometry, InstanceBuffer = instances};
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity,
            Projection = Matrix.Identity,
            ViewProjection = Matrix.Identity,
            Viewport = new Vector4(4, 4, 0.25f, 0.25f),
            Resolution = new Vector4(4, 4, 0.25f, 0.25f),
            EyePos = new Vector3(0, 0, 2),
            DpiScale = 1
        };
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        core.AttachD3D12();
        context.Reset();
        context.ClearRenderTarget(renderTarget, renderTargetView, [0, 0, 1, 1]);
        context.SetRenderTarget(renderTargetView);
        context.SetViewport(4, 4);

        Assert.True(core.TryRenderD3D12(context, resources, pass, bindings, in transforms));
        context.Transition(renderTarget, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, renderTarget, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        device.ThrowIfDeviceRemoved();
        var centerPixelOffset = footprint.Offset + 2UL * footprint.Footprint.RowPitch + 2UL * 4;
        Assert.Equal([255, 0, 0, 255], readback.Read(4, centerPixelOffset));
        core.DetachD3D12();
        Assert.False(core.IsD3D12Attached);
        device.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Verifies an existing bone-skinned core updates managed bone matrices and renders the displaced mesh on WARP.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRendersExistingBoneSkinnedMeshCore() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var rootSignature = device.CreateDefaultRootSignature();
        using var cache = new D3D12PipelineStateCache();
        var techniqueDescription = DefaultEffectsManager.LoadTechniqueDescriptions()
            .Single(description => description.Name == DefaultRenderTechniqueNames.Mesh);
        var passDescription = techniqueDescription.PassDescriptions!
            .Single(description => description.Name == DefaultPassNames.Colors);
        passDescription.DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil;
        passDescription.RasterStateDescription = DefaultRasterDescriptions.RsOutline;
        using var pass = passDescription.CreateD3D12(device,
            rootSignature,
            cache,
            techniqueDescription.InputLayoutDescription,
            PrimitiveTopology.TriangleList);
        var preComputeDescription = techniqueDescription.PassDescriptions!
            .Single(description => description.Name == DefaultPassNames.PreComputeMeshBoneSkinned);
        using var preComputePass = preComputeDescription.CreateD3D12(device,
            rootSignature,
            cache,
            techniqueDescription.InputLayoutDescription,
            PrimitiveTopology.PointList);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 118, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 10, true);
        using var bindings = new SilkD3D12MeshBindings(device, resourceHeap, samplerHeap);
        using var resources = new SilkD3D12ResourceManager(device, resourceHeap);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var renderTarget = device.CreateRenderTargetTexture2D(8, 8, Format.FormatR8G8B8A8Unorm);
        var geometry = new BoneSkinnedMeshGeometry3D {
            Positions = new Vector3Collection([
                new Vector3(-0.9f, 0.4f, 0),
                new Vector3(-0.1f, 0.4f, 0),
                new Vector3(-0.9f, -0.4f, 0),
                new Vector3(-0.1f, -0.4f, 0)
            ]),
            Colors = new Color4Collection([
                new Vector4(1, 0, 0, 1),
                new Vector4(1, 0, 0, 1),
                new Vector4(1, 0, 0, 1),
                new Vector4(1, 0, 0, 1)
            ]),
            Indices = new IntCollection([0, 1, 2, 2, 1, 3]),
            VertexBoneIds = [
                new BoneIds {Bone1 = 0, Weights = Vector4.UnitX},
                new BoneIds {Bone1 = 0, Weights = Vector4.UnitX},
                new BoneIds {Bone1 = 0, Weights = Vector4.UnitX},
                new BoneIds {Bone1 = 0, Weights = Vector4.UnitX}
            ]
        };
        var source = new BoneSkinnedMeshBufferModel {Geometry = geometry};
        using var preCompute = new BoneSkinPreComputeBufferModel(source, DefaultVertex.SizeInBytes);
        using var core = new BoneSkinRenderCore {
            GeometryBuffer = preCompute,
            D3D12Material = new DiffuseMaterialCore()
        };
        var translation = Matrix.Identity;
        translation.M41 = 0.5f;
        core.BoneMatrices = [translation];
        core.MorphTargetWeights = [1];
        Assert.True(core.InitializeMorphTargets([
            new MorphTargetVertex {deltaPosition = new Vector3(0.1f, 0, 0)},
            new MorphTargetVertex {deltaPosition = new Vector3(0.1f, 0, 0)},
            new MorphTargetVertex {deltaPosition = new Vector3(0.1f, 0, 0)},
            new MorphTargetVertex {deltaPosition = new Vector3(0.1f, 0, 0)}
        ], 4));
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity,
            Projection = Matrix.Identity,
            ViewProjection = Matrix.Identity,
            Viewport = new Vector4(8, 8, 0.125f, 0.125f),
            Resolution = new Vector4(8, 8, 0.125f, 0.125f),
            EyePos = new Vector3(0, 0, 2),
            DpiScale = 1
        };
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        using var outputReadback = device.CreateBuffer(4UL * DefaultVertex.SizeInBytes, HeapType.Readback);
        using var filledSizeReadback = device.CreateBuffer(sizeof(uint), HeapType.Readback);
        core.AttachD3D12();
        context.Reset();
        context.ClearRenderTarget(renderTarget, renderTargetView, [0, 0, 1, 1]);
        context.SetRenderTarget(renderTargetView);
        context.SetViewport(8, 8);

        Assert.True(core.TryRenderD3D12(context,
            resources,
            pass,
            bindings,
            in transforms,
            null,
            null,
            preComputePass));
        var skinning = bindings.BoneSkinning!;
        context.Transition(skinning.Output, ResourceStates.CopySource);
        context.CopyBuffer(outputReadback, 0, skinning.Output, 0, skinning.OutputSizeInBytes);
        context.Transition(skinning.FilledSizeResource, ResourceStates.CopySource);
        context.CopyBuffer(filledSizeReadback, 0, skinning.FilledSizeResource, 0, sizeof(uint));
        context.Transition(renderTarget, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, renderTarget, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        device.ThrowIfDeviceRemoved();
        var centerPixelOffset = footprint.Offset + 4UL * footprint.Footprint.RowPitch + 4UL * 4;
        Assert.Equal([255, 0, 0, 255], readback.Read(4, centerPixelOffset));
        Assert.Equal(4U * DefaultVertex.SizeInBytes,
            MemoryMarshal.Read<uint>(filledSizeReadback.Read(sizeof(uint))));
        var skinnedVertices = MemoryMarshal.Cast<byte, DefaultVertex>(
            outputReadback.Read(4 * DefaultVertex.SizeInBytes));
        Assert.Equal(-0.3f, skinnedVertices[0].Position.X, 5);
        Assert.Equal(0.4f, skinnedVertices[0].Position.Y, 5);
        Assert.Equal(1, resources.GeometryCount);
        core.DetachD3D12();
        device.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Verifies an existing diffuse material uploads its texture, sampler, flags, and cbMesh data for a WARP draw.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRendersExistingDiffuseMaterialAndTexture() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var rootSignature = device.CreateDefaultRootSignature();
        using var cache = new D3D12PipelineStateCache();
        var techniqueDescription = DefaultEffectsManager.LoadTechniqueDescriptions()
            .Single(description => description.Name == DefaultRenderTechniqueNames.Mesh);
        var passDescription = techniqueDescription.PassDescriptions!
            .Single(description => description.Name == DefaultPassNames.Diffuse);
        passDescription.DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil;
        passDescription.RasterStateDescription = DefaultRasterDescriptions.RsOutline;
        using var pass = passDescription.CreateD3D12(device,
            rootSignature,
            cache,
            techniqueDescription.InputLayoutDescription,
            PrimitiveTopology.TriangleList);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 256, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 10, true);
        using var bindings = new SilkD3D12MeshBindings(device, resourceHeap, samplerHeap);
        using var resources = new SilkD3D12ResourceManager(device, resourceHeap);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var renderTarget = device.CreateRenderTargetTexture2D(4, 4, Format.FormatR8G8B8A8Unorm);
        var textureLoader = new ManagerTextureLoader(
            new TextureInfo([0, 255, 0, 255], Format.FormatR8G8B8A8Unorm, 1, 1, false));
        var material = new DiffuseMaterialCore {
            DiffuseColor = Vector4.One,
            DiffuseMap = new TextureModel(Guid.NewGuid(), textureLoader),
            EnableUnLit = true
        };
        using var geometry = new DefaultMeshGeometryBufferModel {
            Geometry = new MeshGeometry3D {
                Positions = new Vector3Collection([
                    new Vector3(-1, 1, 0),
                    new Vector3(1, 1, 0),
                    new Vector3(-1, -1, 0),
                    new Vector3(1, -1, 0)
                ]),
                TextureCoordinates = new Vector2Collection([
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1)
                ]),
                Colors = new Color4Collection([
                    Vector4.One,
                    Vector4.One,
                    Vector4.One,
                    Vector4.One
                ]),
                Indices = new IntCollection([0, 1, 2, 2, 1, 3])
            }
        };
        using var core = new MeshRenderCore {GeometryBuffer = geometry, D3D12Material = material};
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity,
            Projection = Matrix.Identity,
            ViewProjection = Matrix.Identity,
            Viewport = new Vector4(4, 4, 0.25f, 0.25f),
            Resolution = new Vector4(4, 4, 0.25f, 0.25f),
            EyePos = new Vector3(0, 0, 2),
            DpiScale = 1
        };
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        core.AttachD3D12();
        context.Reset();
        context.ClearRenderTarget(renderTarget, renderTargetView, [0, 0, 1, 1]);
        context.SetRenderTarget(renderTargetView);
        context.SetViewport(4, 4);

        Assert.True(core.TryRenderD3D12(context, resources, pass, bindings, in transforms));
        context.Transition(renderTarget, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, renderTarget, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        device.ThrowIfDeviceRemoved();
        var centerPixelOffset = footprint.Offset + 2UL * footprint.Footprint.RowPitch + 2UL * 4;
        Assert.Equal([0, 255, 0, 255], readback.Read(4, centerPixelOffset));
        Assert.Equal(1, textureLoader.LoadCount);
        Assert.Equal(1, textureLoader.CompleteCount);
        Assert.True(textureLoader.Succeeded);
        Assert.Equal(1, resources.TextureCount);
        core.DetachD3D12();
        device.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Verifies existing Phong, PBR, and tessellated materials consume their repository DXIL passes.
    /// </summary>
    /// <param name="usePbr">Whether to render the PBR rather than the Phong pass.</param>
    /// <param name="tessellate">Whether to use the three-control-point tessellation pass.</param>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [Trait("Category", "Warp")]
    public void WarpRendersExistingPhongPbrAndTessellatedMaterials(bool usePbr, bool tessellate) {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var rootSignature = device.CreateDefaultRootSignature();
        using var cache = new D3D12PipelineStateCache();
        var techniqueDescription = DefaultEffectsManager.LoadTechniqueDescriptions()
            .Single(description => description.Name == DefaultRenderTechniqueNames.Mesh);
        var material = usePbr
            ? (MaterialCore) new PbrMaterialCore {
                AlbedoColor = new Vector4(0, 0, 0, 1),
                EmissiveColor = Vector4.Zero,
                EnableTessellation = tessellate
            }
            : new PhongMaterialCore {
                DiffuseColor = new Vector4(0, 0, 0, 1),
                AmbientColor = Vector4.One,
                EmissiveColor = Vector4.Zero,
                SpecularColor = Vector4.Zero,
                ReflectiveColor = Vector4.Zero,
                EnableTessellation = tessellate
            };
        var passDescription = techniqueDescription.PassDescriptions!
            .Single(description => description.Name == D3D12MeshMaterialData.GetPassName(material));
        passDescription.DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil;
        passDescription.RasterStateDescription = DefaultRasterDescriptions.RsOutline;
        using var pass = passDescription.CreateD3D12(device,
            rootSignature,
            cache,
            techniqueDescription.InputLayoutDescription,
            PrimitiveTopology.TriangleList);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 118, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 10, true);
        using var bindings = new SilkD3D12MeshBindings(device, resourceHeap, samplerHeap);
        using var resources = new SilkD3D12ResourceManager(device, resourceHeap);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var renderTarget = device.CreateRenderTargetTexture2D(4, 4, Format.FormatR8G8B8A8Unorm);
        using var geometry = new DefaultMeshGeometryBufferModel {
            Geometry = new MeshGeometry3D {
                Positions = new Vector3Collection([
                    new Vector3(-1, 1, 0),
                    new Vector3(1, 1, 0),
                    new Vector3(-1, -1, 0),
                    new Vector3(1, -1, 0)
                ]),
                Normals = new Vector3Collection([
                    Vector3.UnitZ,
                    Vector3.UnitZ,
                    Vector3.UnitZ,
                    Vector3.UnitZ
                ]),
                TextureCoordinates = new Vector2Collection([
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1)
                ]),
                Colors = new Color4Collection([
                    Vector4.One,
                    Vector4.One,
                    Vector4.One,
                    Vector4.One
                ]),
                Indices = new IntCollection([0, 1, 2, 2, 1, 3])
            }
        };
        using var core = new MeshRenderCore {GeometryBuffer = geometry, D3D12Material = material};
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity,
            Projection = Matrix.Identity,
            ViewProjection = Matrix.Identity,
            Viewport = new Vector4(4, 4, 0.25f, 0.25f),
            Resolution = new Vector4(4, 4, 0.25f, 0.25f),
            EyePos = new Vector3(0, 0, 2),
            DpiScale = 1
        };
        var lightData = new LightsBufferModel {AmbientLight = new Vector4(1, 0, 0, 1)};
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        core.AttachD3D12();
        context.Reset();
        context.ClearRenderTarget(renderTarget, renderTargetView, [0, 0, 1, 1]);
        context.SetRenderTarget(renderTargetView);
        context.SetViewport(4, 4);

        Assert.True(core.TryRenderD3D12(context, resources, pass, bindings, in transforms, lightData));
        context.Transition(renderTarget, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, renderTarget, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        device.ThrowIfDeviceRemoved();
        var centerPixelOffset = footprint.Offset + 2UL * footprint.Footprint.RowPitch + 2UL * 4;
        Assert.Equal([255, 0, 0, 255], readback.Read(4, centerPixelOffset));
        core.DetachD3D12();
        device.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Verifies existing point and line cores render through their geometry-shader DX12 passes.
    /// </summary>
    /// <param name="renderPoint">Whether to render a point rather than a line.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Warp")]
    public void WarpRendersExistingPointAndLineCores(bool renderPoint) {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var rootSignature = device.CreateDefaultRootSignature();
        using var cache = new D3D12PipelineStateCache();
        var techniqueDescription = DefaultEffectsManager.LoadTechniqueDescriptions()
            .Single(description => description.Name == (renderPoint
                ? DefaultRenderTechniqueNames.Points
                : DefaultRenderTechniqueNames.Lines));
        var passDescription = techniqueDescription.PassDescriptions!
            .Single(description => description.Name == DefaultPassNames.Default);
        passDescription.DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil;
        passDescription.RasterStateDescription = DefaultRasterDescriptions.RsOutline;
        var topology = renderPoint ? PrimitiveTopology.PointList : PrimitiveTopology.LineList;
        using var pass = passDescription.CreateD3D12(device,
            rootSignature,
            cache,
            techniqueDescription.InputLayoutDescription,
            topology);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 118, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 10, true);
        using var bindings = new SilkD3D12PointLineBindings(device, resourceHeap, samplerHeap);
        using var resources = new SilkD3D12ResourceManager(device, resourceHeap);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var renderTarget = device.CreateRenderTargetTexture2D(16, 16, Format.FormatR8G8B8A8Unorm);
        using var geometry = renderPoint
            ? (IGeometryBufferModel) new DefaultPointGeometryBufferModel {
                Geometry = new PointGeometry3D {
                    Positions = new Vector3Collection([Vector3.Zero]),
                    Colors = new Color4Collection([Vector4.One])
                }
            }
            : new DefaultLineGeometryBufferModel {
                Geometry = new LineGeometry3D {
                    Positions = new Vector3Collection([
                        new Vector3(-0.75f, 0, 0),
                        new Vector3(0.75f, 0, 0)
                    ]),
                    Colors = new Color4Collection([Vector4.One, Vector4.One]),
                    Indices = new IntCollection([0, 1])
                }
            };
        var material = renderPoint
            ? (MaterialCore) new PointMaterialCore {
                Width = 6,
                Height = 6,
                PointColor = new Vector4(0, 1, 0, 1),
                FixedSize = true
            }
            : new LineMaterialCore {
                Thickness = 6,
                LineColor = new Vector4(1, 0, 0, 1),
                FixedSize = true
            };
        using var core = new PointLineRenderCore {GeometryBuffer = geometry, D3D12Material = material};
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity,
            Projection = Matrix.Identity,
            ViewProjection = Matrix.Identity,
            Viewport = new Vector4(16, 16, 1f / 16, 1f / 16),
            Resolution = new Vector4(16, 16, 1f / 16, 1f / 16),
            EyePos = new Vector3(0, 0, 2),
            DpiScale = 1
        };
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        core.AttachD3D12();
        context.Reset();
        context.ClearRenderTarget(renderTarget, renderTargetView, [0, 0, 1, 1]);
        context.SetRenderTarget(renderTargetView);
        context.SetViewport(16, 16);

        Assert.True(core.TryRenderD3D12(context, resources, pass, bindings, in transforms));
        context.Transition(renderTarget, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, renderTarget, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        device.ThrowIfDeviceRemoved();
        var centerPixelOffset = footprint.Offset + 8UL * footprint.Footprint.RowPitch + 8UL * 4;
        Assert.Equal(renderPoint ? [0, 255, 0, 255] : [255, 0, 0, 255],
            readback.Read(4, centerPixelOffset));
        core.DetachD3D12();
        device.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Verifies prepared existing billboard vertices and TextureModel data render through the DX12 billboard pass.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRendersExistingPreparedImageBillboard() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var rootSignature = device.CreateDefaultRootSignature();
        using var cache = new D3D12PipelineStateCache();
        var techniqueDescription = DefaultEffectsManager.LoadTechniqueDescriptions()
            .Single(description => description.Name == DefaultRenderTechniqueNames.BillboardText);
        var passDescription = techniqueDescription.PassDescriptions!
            .Single(description => description.Name == DefaultPassNames.Default);
        passDescription.DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil;
        passDescription.RasterStateDescription = DefaultRasterDescriptions.RsOutline;
        using var pass = passDescription.CreateD3D12(device,
            rootSignature,
            cache,
            techniqueDescription.InputLayoutDescription,
            PrimitiveTopology.PointList);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 256, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 10, true);
        using var bindings = new SilkD3D12PointLineBindings(device, resourceHeap, samplerHeap);
        using var resources = new SilkD3D12ResourceManager(device, resourceHeap);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var renderTarget = device.CreateRenderTargetTexture2D(16, 16, Format.FormatR8G8B8A8Unorm);
        var textureLoader = new ManagerTextureLoader(
            new TextureInfo([0, 255, 0, 255], Format.FormatR8G8B8A8Unorm, 1, 1, false));
        var texture = new TextureModel(Guid.NewGuid(), textureLoader);
        var billboard = new BillboardSingleImage3D(texture, 8, 8);
        using var geometry = new DefaultBillboardBufferModel {Geometry = billboard};
        var material = new BillboardMaterialCore {FixedSize = true, Type = BillboardType.Image};
        using var core = new PointLineRenderCore {GeometryBuffer = geometry, D3D12Material = material};
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity,
            Projection = Matrix.Identity,
            ViewProjection = Matrix.Identity,
            Viewport = new Vector4(16, 16, 1f / 16, 1f / 16),
            Resolution = new Vector4(16, 16, 1f / 16, 1f / 16),
            EyePos = new Vector3(0, 0, 2),
            DpiScale = 1
        };
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        core.AttachD3D12();
        context.Reset();
        context.ClearRenderTarget(renderTarget, renderTargetView, [0, 0, 1, 1]);
        context.SetRenderTarget(renderTargetView);
        context.SetViewport(16, 16);

        Assert.True(core.TryRenderD3D12(context, resources, pass, bindings, in transforms));
        context.Transition(renderTarget, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, renderTarget, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        device.ThrowIfDeviceRemoved();
        var centerPixelOffset = footprint.Offset + 8UL * footprint.Footprint.RowPitch + 8UL * 4;
        Assert.Equal([0, 255, 0, 255], readback.Read(4, centerPixelOffset));
        Assert.Equal(1, textureLoader.LoadCount);
        Assert.True(textureLoader.Succeeded);
        core.DetachD3D12();
        device.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Verifies texture identifiers share one upload and descriptor and clean up failed or removed resources.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpManagesSharedTextureResources() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var heap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1, true);
        using var manager = new SilkD3D12ResourceManager(device, heap);
        var contentId = Guid.NewGuid();
        var loader = new ManagerTextureLoader(
            new TextureInfo([1, 2, 3, 4], Format.FormatR8G8B8A8Unorm, 1, 1, false));
        var model = new TextureModel(contentId, loader);
        var alias = new TextureModel(contentId, new ManagerTextureLoader(TextureInfo.Null));
        var failedLoader = new ManagerTextureLoader(
            new TextureInfo([5, 6, 7, 8], Format.FormatR8G8B8A8Unorm, 1, 1, false));
        var failedModel = new TextureModel(Guid.NewGuid(), failedLoader);
        context.Reset();

        var first = manager.GetOrCreate(context, model);
        Assert.Same(first, manager.GetOrCreate(context, alias));
        Assert.Throws<InvalidOperationException>(() => manager.GetOrCreate(context, failedModel));
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        Assert.Equal(1, loader.LoadCount);
        Assert.Equal(1, loader.CompleteCount);
        Assert.True(loader.Succeeded);
        Assert.Equal(1, failedLoader.LoadCount);
        Assert.Equal(1, failedLoader.CompleteCount);
        Assert.False(failedLoader.Succeeded);
        Assert.Equal(1, manager.TextureCount);
        Assert.Equal(1, heap.Count);
        Assert.True(manager.Remove(contentId));
        Assert.False(manager.Remove(contentId));
        Assert.True(first.IsDisposed);
        Assert.Equal(0, manager.TextureCount);
        Assert.Equal(0, heap.Count);
        device.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Verifies resource managers reject descriptor heaps that shaders cannot use.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRejectsInvalidResourceManagerHeaps() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var invisibleHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 1, true);

        Assert.Throws<ArgumentException>(() => new SilkD3D12ResourceManager(device, invisibleHeap));
        Assert.Throws<ArgumentException>(() => new SilkD3D12ResourceManager(device, samplerHeap));
    }

    /// <summary>
    ///     Creates a small mesh node centered at the requested X coordinate for frustum-selection tests.
    /// </summary>
    /// <param name="centerX">The mesh center on the X axis.</param>
    /// <returns>The initialized mesh node.</returns>
    /// <summary>
    ///     Verifies FXAA constants and the SSAO kernel are deterministic and preserve their shader layouts.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void PostProcessCpuContractsAreDeterministic() {
        var fxaa = SilkD3D12SceneRenderer.CreateFxaaParameters(FxaaLevel.High, 200, 100);
        Assert.Equal(0.005f, fxaa.Color.X);
        Assert.Equal(0.01f, fxaa.Color.Y);
        Assert.Equal(0.75f, fxaa.Param.M11);
        Assert.Equal(0.125f, fxaa.Param.M12);
        Assert.Equal(0.0625f, fxaa.Param.M13);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SilkD3D12SceneRenderer.CreateFxaaParameters(FxaaLevel.None, 1, 1));

        var first = SilkD3D12SsaoResources.CreateKernel();
        var second = SilkD3D12SsaoResources.CreateKernel();
        Assert.Equal(SilkD3D12SsaoResources.KernelSize, first.Length);
        Assert.Equal(first, second);
        Assert.All(first, sample => {
            Assert.True(sample.Z > 0);
            Assert.Equal(0, sample.W);
            Assert.InRange(sample.X * sample.X + sample.Y * sample.Y + sample.Z * sample.Z, 0.009f, 1.001f);
        });
    }

    /// <summary>
    ///     Verifies the repository luma/FXAA ping-pong sequence preserves a stable solid WARP pixel.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRunsFxaaPingPongAndPreservesSolidColor() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var catalog = new D3D12ScenePassCatalog(device,
            Format.FormatB8G8R8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 512, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 40, true);
        using var postProcess = new SilkD3D12PostProcessResources(device, resourceHeap, samplerHeap);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var renderTarget = device.CreateRenderTargetTexture2D(8, 8, Format.FormatB8G8R8A8Unorm);
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity,
            Projection = Matrix.Identity,
            ViewProjection = Matrix.Identity,
            Viewport = new Vector4(8, 8, 0.125f, 0.125f),
            Resolution = new Vector4(8, 8, 0.125f, 0.125f),
            DpiScale = 1
        };
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var capturedReadback = device.CreateBuffer(totalBytes, HeapType.Readback);
        using var lumaReadback = device.CreateBuffer(totalBytes, HeapType.Readback);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        var parameters = SilkD3D12SceneRenderer.CreateFxaaParameters(FxaaLevel.High, 8, 8);
        var lumaPass = catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectFxaa,
            DefaultPassNames.LumaPass);
        var fxaaPass = catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectFxaa,
            DefaultPassNames.FxaaPass);
        context.Reset();
        context.ClearRenderTarget(renderTarget, renderTargetView, [1, 0, 0, 1]);
        postProcess.Capture(context, renderTarget, 8, 8);
        context.Transition(postProcess.First, ResourceStates.CopySource);
        context.CopyTextureToBuffer(capturedReadback, postProcess.First, in footprint);
        postProcess.Draw(context, lumaPass, 0, 1, in transforms, in parameters);
        context.Transition(postProcess.Second, ResourceStates.CopySource);
        context.CopyTextureToBuffer(lumaReadback, postProcess.Second, in footprint);
        context.Transition(renderTarget, ResourceStates.RenderTarget);
        postProcess.DrawToPresentation(context, fxaaPass, 1, renderTargetView, in transforms, in parameters);
        context.Transition(renderTarget, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, renderTarget, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        device.ThrowIfDeviceRemoved();
        var pixelOffset = footprint.Offset + 4UL * footprint.Footprint.RowPitch + 16;
        var capturedPixel = capturedReadback.Read(4, pixelOffset);
        var lumaPixel = lumaReadback.Read(4, pixelOffset);
        var pixel = readback.Read(4, pixelOffset);
        Assert.InRange(capturedPixel[2], 250, 255);
        Assert.True(lumaPixel[2] >= 250,
            $"Expected red luma output; capture=[{string.Join(',', capturedPixel)}], luma=[{string.Join(',', lumaPixel)}].");
        Assert.InRange(pixel[2], 250, 255);
        Assert.InRange(pixel[0], 0, 5);
        Assert.Equal(255, pixel[3]);
        postProcess.Dispose();
        Assert.Equal(0, resourceHeap.Count);
        Assert.Equal(0, samplerHeap.Count);
    }

    /// <summary>
    ///     Verifies bloom extraction, both blur directions, and additive composition preserve a bright WARP pixel.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRunsBloomPassOrderAndPreservesBrightColor() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var catalog = new D3D12ScenePassCatalog(device,
            Format.FormatB8G8R8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1024, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 80, true);
        using var renderer = new SilkD3D12SceneRenderer(device, resourceHeap, samplerHeap);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var renderTarget = device.CreateRenderTargetTexture2D(8, 8, Format.FormatB8G8R8A8Unorm);
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity,
            Projection = Matrix.Identity,
            ViewProjection = Matrix.Identity,
            Viewport = new Vector4(8, 8, 0.125f, 0.125f),
            Resolution = new Vector4(8, 8, 0.125f, 0.125f),
            DpiScale = 1
        };
        var parameters = new BorderEffectStruct {
            Color = new(0.5f, 0.5f, 0.5f, 0.5f),
            Param = new Matrix {M11 = 1, M12 = 1, M13 = 1, M14 = 1},
            ViewportScale = 1
        };
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        context.Reset();
        context.ClearRenderTarget(renderTarget, renderTargetView, [1, 0, 0, 1]);
        renderer.RenderBloom(context,
            renderTarget,
            renderTargetView,
            catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectBloom, DefaultPassNames.ScreenQuad),
            catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectBloom,
                DefaultPassNames.EffectBlurVertical),
            catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectBloom,
                DefaultPassNames.EffectBlurHorizontal),
            catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectBloom, DefaultPassNames.MeshOutline),
            in parameters,
            1,
            in transforms,
            8,
            8);
        context.Transition(renderTarget, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, renderTarget, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        device.ThrowIfDeviceRemoved();
        var pixel = readback.Read(4, footprint.Offset + 4UL * footprint.Footprint.RowPitch + 16);
        Assert.InRange(pixel[2], 250, 255);
        Assert.InRange(pixel[1], 0, 5);
        Assert.InRange(pixel[0], 0, 5);
        renderer.Dispose();
        Assert.Equal(0, resourceHeap.Count);
        Assert.Equal(0, samplerHeap.Count);
    }

    /// <summary>
    ///     Verifies an existing mesh mask, two blur directions, and outline composition record and alter WARP color.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRendersOutlineMaskBlurAndComposition() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var catalog = new D3D12ScenePassCatalog(device,
            Format.FormatB8G8R8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1024, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 100, true);
        using var renderer = new SilkD3D12SceneRenderer(device, resourceHeap, samplerHeap);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var renderTarget = device.CreateRenderTargetTexture2D(16, 16, Format.FormatB8G8R8A8Unorm);
        using var depthHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        using var depthView = depthHeap.Allocate();
        using var depth = device.CreateDepthStencilTexture2D(16, 16, Format.FormatD32FloatS8X24Uint);
        using var node = CreateDiffuseQuadNode(Vector4.One);
        var nodes = new FastList<SceneNode>(1) {node};
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity,
            Projection = Matrix.Identity,
            ViewProjection = Matrix.Identity,
            Viewport = new Vector4(16, 16, 1f / 16, 1f / 16),
            Resolution = new Vector4(16, 16, 1f / 16, 1f / 16),
            EyePos = new Vector3(0, 0, 2),
            DpiScale = 1
        };
        var parameters = new BorderEffectStruct {
            Color = new(1, 0, 0, 1),
            Param = new Matrix {M11 = 1, M12 = 1},
            ViewportScale = 1
        };
        var frustum = new BoundingFrustum(Matrix.Identity);
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        device.CreateDepthStencilView(depth, depthView);
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        context.Reset();
        context.ClearRenderTarget(renderTarget, renderTargetView, [0, 0, 1, 1]);
        context.ClearDepthStencil(depth, depthView);

        var recorded = renderer.RenderOutline(context,
            nodes,
            current => catalog.ResolvePostEffectGeometry(current,
                DefaultPassNames.EffectOutlineP1,
                Format.FormatD32FloatS8X24Uint),
            catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectMeshOutlineBlur,
                DefaultPassNames.EffectBlurVertical),
            catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectMeshOutlineBlur,
                DefaultPassNames.EffectBlurHorizontal),
            catalog.ResolvePostProcess(DefaultRenderTechniqueNames.PostEffectMeshOutlineBlur,
                DefaultPassNames.MeshOutline),
            in parameters,
            1,
            in transforms,
            false,
            ref frustum,
            renderTarget,
            renderTargetView,
            depthView,
            16,
            16);
        context.Transition(renderTarget, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, renderTarget, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        device.ThrowIfDeviceRemoved();
        Assert.Equal(1, recorded);
        var pixels = readback.Read(checked((int) totalBytes));
        Assert.Contains(Enumerable.Range(0, 16 * 16), index => {
            var offset = index / 16 * (int) footprint.Footprint.RowPitch + index % 16 * 4;
            return pixels[offset + 2] > pixels[offset];
        });
        renderer.Dispose();
        Assert.Equal(0, resourceHeap.Count);
        Assert.Equal(0, samplerHeap.Count);
    }

    /// <summary>
    ///     Verifies the XRay overlay consumes prepared occlusion depth/stencil state on WARP.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRendersXRayOverlayFromPreparedOcclusion() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var catalog = new D3D12ScenePassCatalog(device,
            Format.FormatB8G8R8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 256, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 20, true);
        using var renderer = new SilkD3D12SceneRenderer(device, resourceHeap, samplerHeap);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var renderTarget = device.CreateRenderTargetTexture2D(8, 8, Format.FormatB8G8R8A8Unorm);
        using var depthHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        using var depthView = depthHeap.Allocate();
        using var depth = device.CreateDepthStencilTexture2D(8, 8, Format.FormatD32FloatS8X24Uint);
        using var node = CreateDiffuseQuadNode(Vector4.One);
        var nodes = new FastList<SceneNode>(1) {node};
        var selectors = new Func<SceneNode, ShaderPass?>[] {
            current => catalog.ResolvePostEffectGeometry(current,
                DefaultPassNames.EffectMeshXRayP1,
                Format.FormatD32FloatS8X24Uint),
            current => catalog.ResolvePostEffectGeometry(current,
                DefaultPassNames.EffectMeshXRayP2,
                Format.FormatD32FloatS8X24Uint)
        };
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity,
            Projection = Matrix.Identity,
            ViewProjection = Matrix.Identity,
            Viewport = new Vector4(8, 8, 0.125f, 0.125f),
            Resolution = new Vector4(8, 8, 0.125f, 0.125f),
            EyePos = new Vector3(0, 0, 2),
            DpiScale = 1
        };
        var parameters = new BorderEffectStruct {Color = new(1, 0, 0, 1), Param = new Matrix {M11 = 0.5f}};
        var frustum = new BoundingFrustum(Matrix.Identity);
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        device.CreateDepthStencilView(depth, depthView);
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        context.Reset();
        context.ClearRenderTarget(renderTarget, renderTargetView, [0, 0, 1, 1]);
        context.ClearDepthStencil(depth, depthView, 0, 0);

        var recorded = renderer.RenderXRay(context,
            nodes,
            selectors,
            in parameters,
            in transforms,
            false,
            ref frustum,
            renderTargetView,
            depthView,
            8,
            8);
        context.Transition(renderTarget, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, renderTarget, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        device.ThrowIfDeviceRemoved();
        Assert.Equal(2, recorded);
        var pixels = readback.Read(checked((int) totalBytes));
        Assert.Contains(Enumerable.Range(0, 8 * 8), index => {
            var offset = index / 8 * (int) footprint.Footprint.RowPitch + index % 8 * 4;
            return pixels[offset + 2] > 0 || pixels[offset] < 255;
        });
        renderer.Dispose();
        Assert.Equal(0, resourceHeap.Count);
        Assert.Equal(0, samplerHeap.Count);
    }

    /// <summary>
    ///     Verifies SSAO records geometry, evaluates and blurs into a finite R16 WARP map.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpBuildsStableSsaoMap() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var catalog = new D3D12ScenePassCatalog(device,
            Format.FormatB8G8R8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 512, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 40, true);
        using var renderer = new SilkD3D12SceneRenderer(device, resourceHeap, samplerHeap);
        using var node = CreateDiffuseQuadNode(new Vector4(1, 1, 1, 1));
        var nodes = new FastList<SceneNode>(1) {node};
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity,
            Projection = Matrix.Identity,
            ViewProjection = Matrix.Identity,
            Viewport = new Vector4(8, 8, 0.125f, 0.125f),
            Resolution = new Vector4(8, 8, 0.125f, 0.125f),
            EyePos = new Vector3(0, 0, 2),
            SSAOEnabled = 1,
            SSAOBias = 1e-3f,
            SSAOIntensity = 1,
            DpiScale = 1
        };
        var frustum = new BoundingFrustum(Matrix.Identity);
        context.Reset();
        var occlusion = renderer.RenderSsao(context,
            nodes,
            catalog.ResolveSsaoGeometry,
            catalog.ResolvePostProcess(DefaultRenderTechniqueNames.Ssao, DefaultPassNames.Default),
            catalog.ResolvePostProcess(DefaultRenderTechniqueNames.Ssao, DefaultPassNames.EffectBlurHorizontal),
            catalog.ResolveBoneSkinning,
            in transforms,
            false,
            ref frustum,
            8,
            8,
            SsaoQuality.High,
            0.5f);
        var footprint = device.GetCopyableFootprint(occlusion, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        context.Transition(occlusion, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, occlusion, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        device.ThrowIfDeviceRemoved();
        Assert.Equal(SilkD3D12SsaoResources.OcclusionFormat, occlusion.Description.Format);
        var bytes = readback.Read(2, footprint.Offset + 4UL * footprint.Footprint.RowPitch + 8);
        var value = (float) BitConverter.UInt16BitsToHalf(BitConverter.ToUInt16(bytes));
        Assert.True(float.IsFinite(value));
        Assert.InRange(value, 0, 1.01f);
        renderer.Dispose();
        Assert.Equal(0, resourceHeap.Count);
        Assert.Equal(0, samplerHeap.Count);
    }

    /// <summary>
    ///     Verifies DirectWrite shapes Unicode text once and reuses the same atlas entry.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void GlyphAtlasCachesShapedUnicodeText() {
        using var atlas = new D3D12GlyphAtlas(32);

        var first = atlas.GetOrCreate("مرحبا 世界 👋",
            "Arial",
            FontWeight.Normal,
            FontStyle.Normal,
            18,
            256,
            64,
            TextAlignment.Leading,
            FlowDirection.RightToLeft);
        var second = atlas.GetOrCreate("مرحبا 世界 👋",
            "Arial",
            FontWeight.Normal,
            FontStyle.Normal,
            18,
            256,
            64,
            TextAlignment.Leading,
            FlowDirection.RightToLeft);

        Assert.Same(first, second);
        Assert.Equal(1, atlas.Count);
        Assert.True(first.GlyphCount > 0);
        Assert.True(first.Width > 0);
        Assert.True(first.Height > 0);
    }

    /// <summary>
    ///     Verifies atlas growth preserves previously allocated glyph coordinates.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void GlyphAtlasGrowsWithoutMovingCachedEntries() {
        using var atlas = new D3D12GlyphAtlas(8);
        var first = atlas.GetOrCreate("A",
            "Arial",
            FontWeight.Normal,
            FontStyle.Normal,
            20,
            64,
            64,
            TextAlignment.Leading,
            FlowDirection.LeftToRight);
        var original = (first.X, first.Y);

        for (var index = 0; index < 12; index++)
            atlas.GetOrCreate($"Unicode-{index}-世界",
                "Arial",
                FontWeight.Bold,
                FontStyle.Normal,
                28,
                256,
                64,
                TextAlignment.Leading,
                FlowDirection.LeftToRight);

        Assert.True(atlas.Width > 8 || atlas.Height > 8);
        Assert.Equal(original, (first.X, first.Y));
        Assert.Equal(13, atlas.Count);
    }

    /// <summary>
    ///     Verifies pixel projection, transformed quad vertices, and clip intersection remain deterministic.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void Scene2DCpuContractsAreDeterministic() {
        var transforms = D3D12Scene2DRenderer.CreateTransforms(200, 100);
        var vertices = D3D12Scene2DRenderer.CreateQuadVertices(new RectangleF(0, 0, 10, 20),
            Matrix3X2.Translation(5, 7),
            new Vector4(1, 0, 0, 1),
            new RectangleF(0, 0, 1, 1));
        var clip = D3D12Scene2DRenderer.Intersect(new RectangleF(0, 0, 50, 50),
            new RectangleF(25, 10, 50, 20));

        Assert.Equal(0.01f, transforms.Projection.M11);
        Assert.Equal(-0.02f, transforms.Projection.M22);
        Assert.Equal(new Vector2(5, 7), vertices[0].Position);
        Assert.Equal(new Vector2(15, 27), vertices[2].Position);
        Assert.Equal(new RectangleF(25, 10, 25, 20), clip);
    }

    /// <summary>
    ///     Verifies Scene2D traversal preserves sibling order and inherited clipping.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpPreparesScene2DInOrderWithClipping() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 512, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 40, true);
        using var resources = new SilkD3D12ResourceManager(device, resourceHeap);
        using var renderer = new D3D12Scene2DRenderer(device, resources, resourceHeap, samplerHeap);
        using var context2D = new D2DDeviceContext();
        using var red = new SolidColorBrush(context2D, new Vector4(1, 0, 0, 1));
        using var green = new SolidColorBrush(context2D, new Vector4(0, 1, 0, 1));
        using var root = new PanelNode2D {ClipToBound = true};
        using var first = new RectangleNode2D {Fill = red};
        using var second = new RectangleNode2D {Fill = green};
        root.RenderCore.LayoutClippingBound = new RectangleF(0, 0, 24, 16);
        first.RenderCore.LayoutBound = new RectangleF(0, 0, 16, 16);
        second.RenderCore.LayoutBound = new RectangleF(12, 0, 16, 16);
        root.AddChildNode(first);
        root.AddChildNode(second);

        renderer.BeginFrame();
        var prepared = renderer.Prepare([root], 32, 32, 1);

        Assert.Equal(2, prepared);
        Assert.Same(first, renderer.Draws[0].Node);
        Assert.Same(second, renderer.Draws[1].Node);
        Assert.Equal(new RectangleF(0, 0, 24, 16), renderer.Draws[0].Clip);
        Assert.Equal(renderer.Draws[0].Clip, renderer.Draws[1].Clip);
    }

    /// <summary>
    ///     Verifies native Sprite2D rendering draws shape geometry and survives a target resize.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRendersScene2DShapesAcrossResize() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 512, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 40, true);
        using var resources = new SilkD3D12ResourceManager(device, resourceHeap);
        using var renderer = new D3D12Scene2DRenderer(device, resources, resourceHeap, samplerHeap);
        using var catalog = new D3D12ScenePassCatalog(device,
            Format.FormatR8G8B8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var context2D = new D2DDeviceContext();
        using var red = new SolidColorBrush(context2D, new Vector4(1, 0, 0, 1));
        using var node = new RectangleNode2D {Fill = red};
        node.RenderCore.LayoutBound = new RectangleF(1, 1, 6, 6);

        foreach (var size in new uint[] {8, 16}) {
            using var renderTarget = device.CreateRenderTargetTexture2D(size,
                size,
                Format.FormatR8G8B8A8Unorm);
            device.CreateRenderTargetView(renderTarget, renderTargetView);
            var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
            using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
            renderer.BeginFrame();
            renderer.Prepare([node], size, size, 1);
            context.Reset();
            context.ClearRenderTarget(renderTarget, renderTargetView, [0, 0, 0, 1]);
            var recorded = renderer.RenderPrepared(context,
                catalog.ResolveSprite2D(),
                renderTargetView,
                size,
                size);
            context.Transition(renderTarget, ResourceStates.CopySource);
            context.CopyTextureToBuffer(readback, renderTarget, in footprint);
            context.Close();
            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

            var pixel = readback.Read(4,
                footprint.Offset + 4UL * footprint.Footprint.RowPitch + 4UL * 4);
            Assert.Equal(1, recorded);
            Assert.True(pixel[0] > 200,
                $"Expected red at {size}x{size}, got [{string.Join(',', pixel)}].");
            Assert.True(pixel[3] > 200,
                $"Expected opaque alpha at {size}x{size}, got [{string.Join(',', pixel)}].");
        }
    }

    /// <summary>
    ///     Verifies an existing encoded image stream is decoded, uploaded, and sampled by the native Sprite2D pass.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRendersScene2DImageAsTexturedQuad() {
        const uint size = 8;
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 512, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 40, true);
        using var resources = new SilkD3D12ResourceManager(device, resourceHeap);
        using var renderer = new D3D12Scene2DRenderer(device, resources, resourceHeap, samplerHeap);
        using var catalog = new D3D12ScenePassCatalog(device,
            Format.FormatR8G8B8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var renderTarget = device.CreateRenderTargetTexture2D(size,
            size,
            Format.FormatR8G8B8A8Unorm);
        using var image = D3D12TextureModelTests.CreateBmp([0, 255, 0, 255]);
        using var node = new ImageNode2D {ImageStream = image};
        node.RenderCore.LayoutBound = new RectangleF(1, 1, 6, 6);
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);

        renderer.BeginFrame();
        renderer.Prepare([node], size, size, 1);
        context.Reset();
        context.ClearRenderTarget(renderTarget, renderTargetView, [0, 0, 0, 1]);
        var recorded = renderer.RenderPrepared(context,
            catalog.ResolveSprite2D(),
            renderTargetView,
            size,
            size);
        context.Transition(renderTarget, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, renderTarget, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        var pixel = readback.Read(4,
            footprint.Offset + 4UL * footprint.Footprint.RowPitch + 4UL * 4);
        Assert.Equal(1, recorded);
        Assert.True(pixel[1] > 200, $"Expected green image pixel, got [{string.Join(',', pixel)}].");
        Assert.True(pixel[3] > 200, $"Expected opaque image pixel, got [{string.Join(',', pixel)}].");
    }

    /// <summary>
    ///     Verifies DirectWrite-shaped Unicode text reaches a WARP render target through the DX12 atlas.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRendersUnicodeTextFromGlyphAtlas() {
        const uint width = 96;
        const uint height = 40;
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 512, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 40, true);
        using var resources = new SilkD3D12ResourceManager(device, resourceHeap);
        using var renderer = new D3D12Scene2DRenderer(device, resources, resourceHeap, samplerHeap);
        using var catalog = new D3D12ScenePassCatalog(device,
            Format.FormatR8G8B8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var renderTarget = device.CreateRenderTargetTexture2D(width,
            height,
            Format.FormatR8G8B8A8Unorm);
        using var context2D = new D2DDeviceContext();
        using var white = new SolidColorBrush(context2D, new Vector4(1, 1, 1, 1));
        using var node = new TextNode2D {Text = "DX12 世界", Foreground = white, FontSize = 22};
        node.RenderCore.LayoutBound = new RectangleF(0, 0, width, height);
        ((TextRenderCore2D) node.RenderCore).MaxWidth = width;
        ((TextRenderCore2D) node.RenderCore).MaxHeight = height;
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        renderer.BeginFrame();
        renderer.Prepare([node], width, height, 1);
        context.Reset();
        context.ClearRenderTarget(renderTarget, renderTargetView, [0, 0, 0, 1]);
        var recorded = renderer.RenderPrepared(context,
            catalog.ResolveSprite2D(),
            renderTargetView,
            width,
            height);
        context.Transition(renderTarget, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, renderTarget, in footprint);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        var pixels = readback.Read(checked((int) totalBytes));
        Assert.Equal(1, recorded);
        Assert.True(renderer.Atlas.Count > 0);
        Assert.Contains(Enumerable.Range(0, checked((int) (width * height))), index => {
            var offset = index / (int) width * (int) footprint.Footprint.RowPitch + index % (int) width * 4;
            return pixels[offset] > 32 || pixels[offset + 1] > 32 || pixels[offset + 2] > 32;
        });
    }

    /// <summary>
    ///     Verifies desktop-frame dimensions, row pitch, byte count, and supported format contracts.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ScreenCaptureFrameValidatesFormatAndDimensions() {
        new ScreenCaptureFrame(2,
            1,
            Format.FormatB8G8R8A8Unorm,
            8,
            new byte[8]).Validate();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScreenCaptureFrame(0, 1, Format.FormatB8G8R8A8Unorm, 0, []).Validate());
        Assert.Throws<NotSupportedException>(() =>
            new ScreenCaptureFrame(1, 1, Format.FormatR8G8B8A8Unorm, 4, new byte[4]).Validate());
        Assert.Throws<ArgumentException>(() =>
            new ScreenCaptureFrame(2, 1, Format.FormatB8G8R8A8Unorm, 4, new byte[4]).Validate());
    }

    /// <summary>
    ///     Verifies D3D11 capture stays lazy, timeouts are non-destructive, and output changes release the old session.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ScreenCloneCoreOwnsLazyMonitorCaptureLifecycle() {
        List<FakeDesktopCaptureSource> sources = [];
        using var core = new ScreenCloneRenderCore(() => {
            var source = new FakeDesktopCaptureSource();
            sources.Add(source);
            return source;
        });

        Assert.False(core.IsCaptureStarted);
        Assert.Null(core.TryAcquireFrame(TimeSpan.Zero));
        Assert.True(core.IsCaptureStarted);
        Assert.Equal([0], sources[0].StartedOutputs);

        core.Output = 2;
        Assert.True(sources[0].IsDisposed);
        Assert.False(core.IsCaptureStarted);
        Assert.Null(core.TryAcquireFrame(TimeSpan.FromMilliseconds(1)));
        Assert.Equal([2], sources[1].StartedOutputs);

        core.StopCapture();
        Assert.True(sources[1].IsDisposed);
        Assert.False(core.IsCaptureStarted);

        var failingSource = new FakeDesktopCaptureSource {ThrowOnStart = true};
        using var failingCore = new ScreenCloneRenderCore(() => failingSource);
        Assert.Throws<InvalidOperationException>(() => failingCore.TryAcquireFrame(TimeSpan.Zero));
        Assert.True(failingSource.IsDisposed);
        Assert.False(failingCore.IsCaptureStarted);
    }

    /// <summary>
    ///     Verifies capture cropping and aspect preservation produce deterministic screen-pass constants.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ScreenCaptureConstantsApplyCropAndLetterbox() {
        using var core = new ScreenCloneRenderCore(() => new FakeDesktopCaptureSource()) {
            CloneRectangle = new Rectangle(50, 25, 100, 50),
            StretchToFill = false
        };

        var constants = SilkD3D12ScreenCaptureResources.CreateConstants(core, 200, 100, 100, 100);

        Assert.Equal(new Vector2(0.25f, 0.25f), constants.TexTopLeft);
        Assert.Equal(new Vector2(0.75f, 0.75f), constants.TexBottomRight);
        Assert.Equal(new Vector4(-1, 0.5f, 0, 1), constants.TopLeft);
        Assert.Equal(new Vector4(1, -0.5f, 0, 1), constants.BottomRight);
    }

    /// <summary>
    ///     Verifies captured BGRA frames replace a DX12 texture while timeout preserves the preceding frame.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpTransfersReplacesAndRetainsDesktopFrames() {
        const uint size = 4;
        var first = new ScreenCaptureFrame(size,
            size,
            Format.FormatB8G8R8A8Unorm,
            size * 4,
            Enumerable.Repeat(new byte[] {0, 0, 255, 255}, checked((int) (size * size)))
                .SelectMany(pixel => pixel)
                .ToArray());
        var second = new ScreenCaptureFrame(size,
            size,
            Format.FormatB8G8R8A8Unorm,
            size * 4,
            Enumerable.Repeat(new byte[] {0, 255, 0, 255}, checked((int) (size * size)))
                .SelectMany(pixel => pixel)
                .ToArray());
        using var source = new FakeDesktopCaptureSource(first, null, second);
        using var core = new ScreenCloneRenderCore(() => source) {StretchToFill = true};
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 256, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 24, true);
        using var capture = new SilkD3D12ScreenCaptureResources(device, resourceHeap, samplerHeap);
        using var catalog = new D3D12ScenePassCatalog(device,
            Format.FormatR8G8B8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var renderTarget = device.CreateRenderTargetTexture2D(size,
            size,
            Format.FormatR8G8B8A8Unorm);
        device.CreateRenderTargetView(renderTarget, renderTargetView);
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);

        byte[][] expected = [[255, 0], [255, 0], [0, 255]];
        for (var frameIndex = 0; frameIndex < expected.Length; frameIndex++) {
            context.Reset();
            context.Transition(renderTarget, ResourceStates.RenderTarget);
            context.ClearRenderTarget(renderTarget, renderTargetView, [0, 0, 0, 1]);
            context.SetRenderTarget(renderTargetView);
            context.SetViewport(size, size);
            Assert.True(capture.TryRender(context,
                catalog.ResolveScreenDuplication(),
                core,
                size,
                size));
            context.Transition(renderTarget, ResourceStates.CopySource);
            context.CopyTextureToBuffer(readback, renderTarget, in footprint);
            context.Close();
            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));
            var pixel = readback.Read(4, footprint.Offset + footprint.Footprint.RowPitch + 4);
            Assert.True(pixel[0] >= expected[frameIndex][0], $"Unexpected frame {frameIndex}: [{string.Join(',', pixel)}].");
            Assert.True(pixel[1] >= expected[frameIndex][1], $"Unexpected frame {frameIndex}: [{string.Join(',', pixel)}].");
            Assert.Equal(frameIndex == 2 ? 2 : 1, capture.Generation);
        }

        Assert.Equal((ulong) size, capture.Texture!.Description.Width);
        Assert.Equal(size, capture.Texture.Description.Height);
        Assert.Equal(Format.FormatB8G8R8A8Unorm, capture.Texture.Description.Format);
        capture.Dispose();
        Assert.True(capture.IsDisposed);
        Assert.Equal(0, resourceHeap.Count);
        Assert.Equal(0, samplerHeap.Count);
    }

    /// <summary>
    ///     Exercises real DXGI desktop duplication only when an interactive hardware session is explicitly requested.
    /// </summary>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public void HardwareCapturesOneInteractiveDesktopFrame() {
        using var source = new D3D11DesktopCaptureSource();
        source.Start(0);

        Assert.True(source.TryAcquire(TimeSpan.FromSeconds(2), out var frame));
        frame.Validate();
        Assert.True(frame.Width > 0);
        Assert.True(frame.Height > 0);
        source.Dispose();
        Assert.True(source.IsDisposed);
    }

    /// <summary>
    ///     Supplies deterministic frames and timeout responses without loading Direct3D 11.
    /// </summary>
    private sealed class FakeDesktopCaptureSource : IDesktopCaptureSource {
        /// <summary>The queued frame or timeout responses.</summary>
        private readonly Queue<ScreenCaptureFrame?> frames;

        /// <summary>Initializes a source with ordered responses.</summary>
        /// <param name="frames">Frames, where <see langword="null" /> represents timeout.</param>
        internal FakeDesktopCaptureSource(params ScreenCaptureFrame?[] frames) => this.frames = new(frames);

        /// <summary>Gets the outputs used to start this source.</summary>
        internal List<int> StartedOutputs { get; } = [];

        /// <summary>Gets whether the source was disposed.</summary>
        internal bool IsDisposed { get; private set; }

        /// <summary>Gets or sets whether starting the source fails.</summary>
        internal bool ThrowOnStart { get; init; }

        /// <inheritdoc />
        public void Start(int output) {
            StartedOutputs.Add(output);
            if (ThrowOnStart) throw new InvalidOperationException("Capture start failed.");
        }

        /// <inheritdoc />
        public bool TryAcquire(TimeSpan timeout, out ScreenCaptureFrame frame) {
            var response = frames.Count == 0 ? null : frames.Dequeue();
            frame = response.GetValueOrDefault();
            return response.HasValue;
        }

        /// <inheritdoc />
        public void Dispose() => IsDisposed = true;
    }

    private static MeshNode CreateFrustumTestNode(float centerX) => new() {
        Geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([
                new Vector3(centerX - 0.1f, 0.1f, 0),
                new Vector3(centerX + 0.1f, 0.1f, 0),
                new Vector3(centerX, -0.1f, 0)
            ]),
            Indices = new IntCollection([0, 1, 2])
        }
    };

    /// <summary>
    ///     Creates a colored quad scene node centered at the requested X coordinate.
    /// </summary>
    /// <param name="centerX">The quad center on the X axis.</param>
    /// <param name="color">The per-vertex color.</param>
    /// <param name="depth">The clip-space depth.</param>
    /// <returns>The initialized mesh node.</returns>
    private static MeshNode CreateColoredQuadNode(float centerX, Vector4 color, float depth = 0) => new() {
        Material = new ColorMaterialCore(),
        Geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([
                new Vector3(centerX - 0.8f, 0.8f, depth),
                new Vector3(centerX + 0.8f, 0.8f, depth),
                new Vector3(centerX - 0.8f, -0.8f, depth),
                new Vector3(centerX + 0.8f, -0.8f, depth)
            ]),
            Colors = new Color4Collection([color, color, color, color]),
            Indices = new IntCollection([0, 1, 2, 2, 1, 3])
        }
    };

    /// <summary>
    ///     Creates one centered transparent diffuse quad for OIT tests.
    /// </summary>
    /// <param name="color">The diffuse RGBA color.</param>
    /// <returns>The initialized mesh node.</returns>
    private static MeshNode CreateDiffuseQuadNode(Vector4 color) => new() {
        IsTransparent = true,
        Material = new DiffuseMaterialCore {DiffuseColor = color, EnableUnLit = true},
        Geometry = new MeshGeometry3D {
            Positions = new Vector3Collection([
                new Vector3(-0.8f, 0.8f, 0.25f),
                new Vector3(0.8f, 0.8f, 0.25f),
                new Vector3(-0.8f, -0.8f, 0.25f),
                new Vector3(0.8f, -0.8f, 0.25f)
            ]),
            Normals = new Vector3Collection([Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ]),
            Indices = new IntCollection([0, 1, 2, 2, 1, 3])
        }
    };

    /// <summary>
    ///     Tracks texture loading and completion across shared-resource registration.
    /// </summary>
    private sealed class ManagerTextureLoader : ITextureInfoLoader {
        /// <summary>
        ///     Initializes a tracking texture loader.
        /// </summary>
        /// <param name="info">The texture information to return.</param>
        internal ManagerTextureLoader(TextureInfo info) {
            Info = info;
        }

        /// <summary>
        ///     Gets the texture information returned from loading.
        /// </summary>
        private TextureInfo Info { get; }

        /// <summary>
        ///     Gets the load-call count.
        /// </summary>
        internal int LoadCount { get; private set; }

        /// <summary>
        ///     Gets the completion-call count.
        /// </summary>
        internal int CompleteCount { get; private set; }

        /// <summary>
        ///     Gets the last completion result.
        /// </summary>
        internal bool Succeeded { get; private set; }

        /// <summary>
        ///     Returns the configured texture information.
        /// </summary>
        /// <param name="id">The content identifier.</param>
        /// <returns>The configured texture information.</returns>
        public TextureInfo Load(Guid id) {
            LoadCount++;
            return Info;
        }

        /// <summary>
        ///     Records one completion result.
        /// </summary>
        /// <param name="id">The content identifier.</param>
        /// <param name="info">The completed texture information.</param>
        /// <param name="succeeded">Whether upload succeeded.</param>
        public void Complete(Guid id, TextureInfo info, bool succeeded) {
            CompleteCount++;
            Succeeded = succeeded;
        }
    }

    /// <summary>
    ///     Minimal concrete geometry core used to exercise the shared DX12 traversal boundary.
    /// </summary>
    private sealed class TraversalGeometryRenderCore : GeometryRenderCore {
        /// <inheritdoc />
        protected override void OnRender(RenderContext context, DeviceContextProxy deviceContext) { }

        /// <inheritdoc />
        protected override void OnRenderCustom(RenderContext context, DeviceContextProxy deviceContext) { }

        /// <inheritdoc />
        protected override void OnRenderShadow(RenderContext context, DeviceContextProxy deviceContext) { }

        /// <inheritdoc />
        protected override void OnRenderDepth(
            RenderContext context,
            DeviceContextProxy deviceContext,
            ShaderPass? customPass
        ) { }
    }

    /// <summary>
    ///     Minimal disposable used to observe deterministic release behavior.
    /// </summary>
    private sealed class TrackedDisposable : IDisposable {
        /// <summary>
        ///     Gets whether the instance has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        ///     Marks the instance as disposed.
        /// </summary>
        public void Dispose() => IsDisposed = true;
    }
}
