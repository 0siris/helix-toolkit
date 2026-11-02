using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Native;
using Silk.NET.Direct3D12;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

namespace SilkCore.Tests;

/// <summary>Checks native allocation reuse without windows, command recording or queue submission.</summary>
public sealed class IntersectionPublicationReuseTests {
    /// <summary>Checks that rejected work is distinguishable from empty and legal publications without GPU work.</summary>
    [Theory]
    [InlineData(0u, true)]
    [InlineData(1u, true)]
    [InlineData(33553920u, true)]
    [InlineData(33553921u, false)]
    [InlineData(uint.MaxValue, false)]
    public void PublicationDispatchSupportMatchesNativeLimit(uint count, bool expected) {
        var supported = MeshIntersectionRenderCore.SupportsCandidateCount(count);
        Assert.Equal(expected, supported);
    }

    /// <summary>Checks native dispatch boundaries without allocating candidates or using a GPU device.</summary>
    [Theory]
    [InlineData(1u, 1u)]
    [InlineData(511u, 1u)]
    [InlineData(512u, 1u)]
    [InlineData(513u, 2u)]
    [InlineData(33553920u, 65535u)]
    public void DispatchGroupCountMatchesSourceKernel(uint count, uint expected) {
        var groups = SilkD3D12IntersectionResources.GetDispatchGroupCount(count);
        Assert.Equal(expected, groups);
    }

    /// <summary>Rejects invalid native dispatch dimensions without creating a large candidate array or GPU work.</summary>
    [Theory]
    [InlineData(0u)]
    [InlineData(33553921u)]
    [InlineData(uint.MaxValue)]
    public void DispatchGroupCountRejectsInvalidNativeDimensions(uint count) {
        Assert.Throws<ArgumentOutOfRangeException>(() => SilkD3D12IntersectionResources.GetDispatchGroupCount(count));
    }

    /// <summary>Retains actual buffers and descriptor ranges across pose and candidate-count changes.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void PoseAndCandidatePublicationReusesCapacityAndReleasesExactlyOnce() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        using var heap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1024, true);
        using var samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 128, true);
        using var root = device.CreateDefaultRootSignature();
        using var pipelines = new D3D12PipelineStateCache();
        using var manager = new SilkD3D12ResourceManager(device, heap);
        var initialResources = device.ResourceStates.Count;
        var mesh = new IntersectionMeshSnapshot([new Vector3(0), new Vector3(1, 0, 0), new Vector3(0, 1, 0)], [0, 1, 2]);
        var original = new IntersectionDispatchInput(mesh, mesh, Matrix.Identity, Matrix.Identity,
            [new IntersectionCandidate(0, 0, 1), new IntersectionCandidate(0, 0, 2)]);
        using var native = new SilkD3D12IntersectionResources(device, heap, samplers, manager, [original], root, pipelines);
        var segments = native.Segments;
        var arguments = native.Arguments;
        var allocatedResources = device.ResourceStates.Count;
        var descriptors = heap.Count;
        var samplerDescriptors = samplers.Count;
        var pose = Matrix.Identity;
        pose.M41 = 3;
        var moved = new IntersectionDispatchInput(mesh, mesh, pose, Matrix.Identity, [new IntersectionCandidate(0, 0, 7)]);
        var reused = native.TryUpdate([moved]);
        Assert.True(reused);
        Assert.True(native.CanReuse());
        Assert.Same(moved, native.Inputs[0]);
        Assert.Equal(2u, native.Capacity);
        Assert.Same(segments, native.Segments);
        Assert.Same(arguments, native.Arguments);
        Assert.Equal(allocatedResources, device.ResourceStates.Count);
        Assert.Equal(descriptors, heap.Count);
        Assert.Equal(samplerDescriptors, samplers.Count);
        Assert.Equal(Matrix.Identity, original.WorldA);
        Assert.Equal(2, original.Candidates.Length);
        var restored = native.TryUpdate([original]);
        Assert.True(restored);
        Assert.True(native.CanReuse());
        var tooLarge = new IntersectionDispatchInput(mesh, mesh, pose, Matrix.Identity,
            [new IntersectionCandidate(0, 0), new IntersectionCandidate(0, 0), new IntersectionCandidate(0, 0)]);
        var grew = native.TryUpdate([tooLarge]);
        Assert.False(grew);
        Assert.Same(original, native.Inputs[0]);
        using (var replacement = new SilkD3D12IntersectionResources(device, heap, samplers, manager,
                   [tooLarge], root, pipelines)) {
            Assert.Equal(3u, replacement.Capacity);
            Assert.NotSame(segments, replacement.Segments);
        }
        var editedPosition = new IntersectionMeshSnapshot(
            [new Vector3(0), new Vector3(2, 0, 0), new Vector3(0, 1, 0)], [0, 1, 2]);
        var editedTopology = new IntersectionMeshSnapshot(
            [new Vector3(0), new Vector3(1, 0, 0), new Vector3(0, 1, 0)], [0, 2, 1]);
        foreach (var edited in new[] { editedPosition, editedTopology }) {
            var changed = new IntersectionDispatchInput(edited, mesh, pose, Matrix.Identity,
                [new IntersectionCandidate(0, 0)]);
            var geometryReused = native.TryUpdate([changed]);
            Assert.False(geometryReused);
            Assert.Same(original, native.Inputs[0]);
        }
        var emptyReused = native.TryUpdate([]);
        Assert.False(emptyReused);
        Assert.Same(original, native.Inputs[0]);
        native.Dispose();
        native.Dispose();
        Assert.True(segments.IsDisposed);
        Assert.True(arguments.IsDisposed);
        Assert.Equal(initialResources, device.ResourceStates.Count);
        Assert.Equal(0, heap.Count);
        Assert.Equal(0, samplers.Count);
        var disposedReused = native.TryUpdate([original]);
        Assert.False(disposedReused);
    }
}
