using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Shaders;
using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Model;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace HelixToolkit.SharpDX.Core.Native;

/// Exercises the native 64-bit stream-output counter through validation and actual GPU bone precomputation.
public sealed class NativeStreamOutputCounterTests {
    /// Rejects a four-byte counter and a four-byte-aligned offset before recording an invalid native binding.
    [Fact]
    [Trait("Category", "Warp")]
    public void RejectsUndersizedAndMisalignedCounterTest() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        using var context = device.CreateCommandContext();
        using var output = device.CreateBuffer(256);
        using var undersized = device.CreateBuffer(sizeof(uint));
        using var counter = device.CreateBuffer(2 * sizeof(ulong));
        context.Reset();
        context.Transition(output, ResourceStates.StreamOut);
        context.Transition(undersized, ResourceStates.StreamOut);
        context.Transition(counter, ResourceStates.StreamOut);

        Assert.Throws<ArgumentOutOfRangeException>(() => context.SetStreamOutputTarget(output, undersized));
        Assert.Throws<ArgumentOutOfRangeException>(() => context.SetStreamOutputTarget(output, counter, 0, 4));
        context.SetStreamOutputTarget(output, counter, 0, sizeof(ulong));
        context.SetStreamOutputTarget(null);
        context.Close();
    }

    /// Checks all GPU-transformed vertices, a covered pixel and the complete reset counter across two frames.
    [Fact]
    [Trait("Category", "Warp")]
    public void SkinnedVerticesAndCompleteCounterReadbackTest() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var root = device.CreateDefaultRootSignature();
        using var pipelines = new D3D12PipelineStateCache();
        var technique = DefaultEffectsManager.LoadTechniqueDescriptions()
            .Single(description => description.Name == DefaultRenderTechniqueNames.Mesh);
        var materialDescription = technique.PassDescriptions!
            .Single(description => description.Name == DefaultPassNames.Diffuse);
        materialDescription.DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil;
        materialDescription.RasterStateDescription = DefaultRasterDescriptions.RsOutline;
        using var materialPass = materialDescription.CreateD3D12(device, root, pipelines,
            technique.InputLayoutDescription, PrimitiveTopology.TriangleList);
        var skinDescription = technique.PassDescriptions!
            .Single(description => description.Name == DefaultPassNames.PreComputeMeshBoneSkinned);
        using var skinPass = skinDescription.CreateD3D12(device, root, pipelines,
            technique.InputLayoutDescription, PrimitiveTopology.PointList);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 256, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 16, true);
        using var bindings = new SilkD3D12MeshBindings(device, resourceHeap, samplerHeap);
        using var resources = new SilkD3D12ResourceManager(device, resourceHeap);
        using var targetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var targetView = targetHeap.Allocate();
        using var target = device.CreateRenderTargetTexture2D(16, 16, Format.FormatR8G8B8A8Unorm);
        device.CreateRenderTargetView(target, targetView);
        Vector3[] original = [new(-0.75f, 0.5f, 0), new(-0.25f, 0.5f, 0),
            new(-0.75f, -0.5f, 0), new(-0.25f, -0.5f, 0)];
        var geometry = new BoneSkinnedMeshGeometry3D {
            Positions = new Vector3Collection(original),
            Colors = new Color4Collection(Enumerable.Repeat(new Vector4(1, 0, 0, 1), 4)),
            Indices = new IntCollection([0, 1, 2, 2, 1, 3]),
            VertexBoneIds = Enumerable.Repeat(new BoneIds { Bone1 = 0, Weights = Vector4.UnitX }, 4).ToArray()
        };
        using var source = new BoneSkinnedMeshBufferModel { Geometry = geometry };
        using var precompute = new BoneSkinPreComputeBufferModel(source, DefaultVertex.SizeInBytes);
        using var core = new BoneSkinRenderCore {
            GeometryBuffer = precompute,
            D3D12Material = new DiffuseMaterialCore { DiffuseColor = new Vector4(1, 0, 0, 1), EnableUnLit = true }
        };
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity, Projection = Matrix.Identity, ViewProjection = Matrix.Identity,
            Viewport = new Vector4(16, 16, 1f / 16, 1f / 16),
            Resolution = new Vector4(16, 16, 1f / 16, 1f / 16), EyePos = new Vector3(0, 0, 2), DpiScale = 1
        };
        var footprint = device.GetCopyableFootprint(target, out var totalBytes);
        using var pixels = device.CreateBuffer(totalBytes, HeapType.Readback);
        using var vertices = device.CreateBuffer(4UL * DefaultVertex.SizeInBytes, HeapType.Readback);
        using var counter = device.CreateBuffer(sizeof(ulong), HeapType.Readback);
        core.Attach();
        for (var frame = 0; frame < 2; frame++) {
            var translation = Matrix.Identity;
            translation.M41 = frame == 0 ? 0.5f : 0.75f;
            core.BoneMatrices = [translation];
            context.Reset();
            context.Transition(target, ResourceStates.RenderTarget);
            context.ClearRenderTarget(target, targetView, [0, 0, 1, 1]);
            context.SetRenderTarget(targetView);
            context.SetViewport(16, 16);
            context.SetDescriptorHeaps(resourceHeap, samplerHeap);
            var recorded = core.TryRenderD3D12(context, resources, materialPass, bindings,
                in transforms, null, null, skinPass);
            Assert.True(recorded);
            var skinning = bindings.BoneSkinning!;
            context.Transition(skinning.Output, ResourceStates.CopySource);
            context.CopyBuffer(vertices, 0, skinning.Output, 0, 4UL * DefaultVertex.SizeInBytes);
            context.Transition(skinning.FilledSizeResource, ResourceStates.CopySource);
            context.CopyBuffer(counter, 0, skinning.FilledSizeResource, 0, sizeof(ulong));
            context.Transition(target, ResourceStates.CopySource);
            context.CopyTextureToBuffer(pixels, target, in footprint);
            context.Close();
            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(10));
            var actualCount = MemoryMarshal.Read<ulong>(counter.Read(sizeof(ulong)));
            var actualVertices = MemoryMarshal.Cast<byte, DefaultVertex>(vertices.Read(4 * DefaultVertex.SizeInBytes));
            var pixel = pixels.Read(4, footprint.Offset + 8UL * footprint.Footprint.RowPitch + 8UL * 4);
            Assert.Equal(4UL * DefaultVertex.SizeInBytes, actualCount);
            Assert.Equal(new byte[] { 255, 0, 0, 255 }, pixel);
            for (var index = 0; index < original.Length; index++) {
                Assert.Equal(original[index].X + translation.M41, actualVertices[index].Position.X, 5);
                Assert.Equal(original[index].Y, actualVertices[index].Position.Y, 5);
                Assert.Equal(original[index].Z, actualVertices[index].Position.Z, 5);
            }
        }
        device.ThrowIfDeviceRemoved();
    }
}
