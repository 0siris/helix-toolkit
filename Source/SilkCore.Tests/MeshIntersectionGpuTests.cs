using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Shaders;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;
using Color4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkCore.Tests;
/// <summary>Exercises actual selected-pair GPU intersections and indirect line rendering without desktop windows.</summary>
public sealed class MeshIntersectionGpuTests {
    /// <summary>Rejects invalid source identities and cancellation before replacing an immutable dispatch publication.</summary>
    [Fact]
    public void InputValidationAndCancellationRetainPriorGeneration() {
        var mesh = new IntersectionMeshSnapshot([new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0)], [0, 1, 2]);
        using var core = new MeshIntersectionRenderCore();
        core.SetInputs([new IntersectionDispatchInput(mesh, mesh, Matrix.Identity, Matrix.Identity, [])]);
        var prior = core.Inputs;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => core.SetInputs([], cancellation.Token));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IntersectionDispatchInput(mesh, mesh,
            Matrix.Identity, Matrix.Identity, [new IntersectionCandidate(1, 0)]));
        Assert.Throws<ArgumentException>(() => new IntersectionMeshSnapshot([new Vector3(float.NaN)], []));
        Assert.Throws<ArgumentException>(() => new IntersectionMeshSnapshot([new Vector3(0)], [0, 0]));
        Assert.Same(prior, core.Inputs);
    }
    /// <summary>Releases all partial allocations when the real descriptor allocator rejects an undersized host heap.</summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void FailedNativeInitializationReturnsDescriptorRanges() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var heap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1, true);
        using var samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 1, true);
        using var root = device.CreateDefaultRootSignature();
        using var pipelines = new D3D12PipelineStateCache();
        using var manager = new SilkD3D12ResourceManager(device, heap);
        var mesh = new IntersectionMeshSnapshot([new Vector3(0), new Vector3(1, 0, 0), new Vector3(0, 1, 0)], [0, 1, 2]);
        var input = new IntersectionDispatchInput(mesh, mesh, Matrix.Identity, Matrix.Identity, [new IntersectionCandidate(0, 0)]);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SilkD3D12IntersectionResources(device, heap, samplers,
            manager, [input], root, pipelines));
        Assert.Equal(0, heap.Count);
        Assert.Equal(0, samplers.Count);
        device.ThrowIfDeviceRemoved();
    }
    /// <summary>
    ///     Checks source layouts, independent transverse geometry, tail guarding and native line output.
    /// </summary>
    /// <param name="shared">Whether both inputs borrow actual render index buffers.</param>
    /// <param name="rotated">Whether noncommuting independent local-to-world matrices are used.</param>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [Trait("Category", "Warp")]
    public void DispatchesOriginalPairsAndDrawsGpuSegments(bool shared, bool rotated) {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var catalog = new D3D12ScenePassCatalog(device, Format.FormatR8G8B8A8Unorm,
            Format.FormatD32FloatS8X24Uint);
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1024, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 128, true);
        using var renderer = new SilkD3D12SceneRenderer(device, resourceHeap, samplerHeap);
        using var targetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var targetView = targetHeap.Allocate();
        using var target = device.CreateRenderTargetTexture2D(32, 32, Format.FormatR8G8B8A8Unorm);
        device.CreateRenderTargetView(target, targetView);
        using var depthHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        using var depthView = depthHeap.Allocate();
        using var depth = device.CreateDepthStencilTexture2D(32, 32, Format.FormatD32FloatS8X24Uint);
        device.CreateDepthStencilView(depth, depthView);
        using var firstBuffer = new DefaultMeshGeometryBufferModel();
        using var secondBuffer = new DefaultMeshGeometryBufferModel();
        var firstPositions = new[] { new Vector3(-0.8f, -0.8f, 0.5f), new Vector3(0.8f, -0.8f, 0.5f), new Vector3(0, 0.8f, 0.5f) };
        if (rotated) firstPositions = firstPositions.Select(position => position + new Vector3(10, 0, 0)).ToArray();
        var secondPositions = new[] { new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 1) };
        firstBuffer.Geometry = new MeshGeometry3D { Positions = new Vector3Collection(firstPositions), Indices = new IntCollection([0, 1, 2]) };
        secondBuffer.Geometry = new MeshGeometry3D { Positions = new Vector3Collection(secondPositions), Indices = new IntCollection([0, 1, 2]) };
        using var firstRenderNode = new MeshNode { Geometry = firstBuffer.Geometry };
        using var secondRenderNode = new MeshNode { Geometry = secondBuffer.Geometry };
        firstRenderNode.Attach();
        secondRenderNode.Attach();
        var first = new IntersectionMeshSnapshot(firstPositions, [0, 1, 2], renderNode: shared ? firstRenderNode : null);
        var second = new IntersectionMeshSnapshot(secondPositions, [0, 1, 2], renderNode: shared ? secondRenderNode : null);
        Assert.Equal(shared, first.CanShare());
        Assert.Equal(shared, second.CanShare());
        using var node = new GpuLineNode();
        node.Material = new LineMaterialCore { LineColor = new Color4(1, 0, 0, 1), Thickness = 3 };
        var core = (MeshIntersectionRenderCore)node.RenderCore;
        var candidates = Enumerable.Range(0, 513).Select(id => new IntersectionCandidate(0, 0, id + 1000)).ToArray();
        var pose = Matrix.Identity;
        pose.M41 = 0.1f;
        if (rotated) { pose.M11 = 0; pose.M12 = 1; pose.M21 = -1; pose.M22 = 0; }
        var firstPose = pose;
        if (rotated) firstPose.M42 = -10;
        var initialSeparatedPose = pose;
        initialSeparatedPose.M41 = 4;
        core.SetInputs([new IntersectionDispatchInput(first, second, firstPose, pose, candidates),
            new IntersectionDispatchInput(first, second, firstPose, initialSeparatedPose, [new IntersectionCandidate(0, 0, -500)])]);
        candidates[0] = new IntersectionCandidate(0, 0, -1);
        firstPositions[0] = new Vector3(999);
        var transforms = new GlobalTransformStruct {
            View = Matrix.Identity, Projection = Matrix.Identity, ViewProjection = Matrix.Identity,
            Viewport = new Vector4(32, 32, 1f / 32, 1f / 32), Resolution = new Vector4(32, 32, 1f / 32, 1f / 32),
            EyePos = new Vector3(0, 0, 2), DpiScale = 1
        };
        var frustum = new BoundingFrustum(Matrix.Identity);
        var nodes = new FastList<SceneNode> { node };
        var footprint = device.GetCopyableFootprint(target, out var totalBytes);
        using var pixels = device.CreateBuffer(totalBytes, HeapType.Readback);
        using var segments = device.CreateBuffer(514 * 28, HeapType.Readback);
        using var arguments = device.CreateBuffer(16, HeapType.Readback);
        SilkD3D12IntersectionResources? previous = null;
        SilkD3D12IntersectionResources? lastRetired = null;
        for (var frame = 0; frame < 8; frame++) {
            if (frame == 3 && shared) {
                // Same counts are insufficient: this unrelated render edit must invalidate the borrowed shared path.
                firstBuffer.Geometry.Positions[0] = new Vector3(-10, -10, -10);
            }
            if (frame == 4) {
                core.SetInputs([new IntersectionDispatchInput(first, second, firstPose, pose, [new IntersectionCandidate(0, 0, 77)])]);
                node.Material = new LineMaterialCore { LineColor = new Color4(0, 1, 0, 1), Thickness = 5 };
            }
            if (frame == 5) {
                var separated = pose;
                separated.M41 = 4;
                core.SetInputs([new IntersectionDispatchInput(first, second, firstPose, separated, [new IntersectionCandidate(0, 0, 88)])]);
            }
            if (frame == 6)
                core.SetInputs([new IntersectionDispatchInput(first, first, firstPose, firstPose, [new IntersectionCandidate(0, 0, 99)])]);
            if (frame == 7) {
                node.Detach();
                core.SetInputs([new IntersectionDispatchInput(first, second, firstPose, pose, [new IntersectionCandidate(0, 0, 77)])]);
            }
            renderer.BeginFrame();
            if (lastRetired is not null) Assert.True(lastRetired.IsDisposed);
            context.Reset();
            context.Transition(target, ResourceStates.RenderTarget);
            context.ClearRenderTarget(target, targetView, [0, 0, 1, 1]);
            context.ClearDepthStencil(depth, depthView, frame is 1 or 7 ? 0 : 1);
            context.SetRenderTargets(targetView, depthView);
            context.SetViewport(32, 32);
            var parameters = new BorderEffectStruct();
            var selectors = new Func<SceneNode, ShaderPass?>[] {
                current => catalog.ResolvePostEffectGeometry(current, DefaultPassNames.EffectMeshXRayP1, Format.FormatD32FloatS8X24Uint),
                current => catalog.ResolvePostEffectGeometry(current, DefaultPassNames.EffectMeshXRayP2, Format.FormatD32FloatS8X24Uint)
            };
            var recorded = frame == 7
                ? renderer.RenderXRay(context, nodes, selectors, in parameters, in transforms, false,
                    ref frustum, targetView, depthView, 32, 32)
                : renderer.RenderVisible(context, nodes, catalog.Resolve, in transforms, false, ref frustum);
            var native = renderer.FindIntersectionResources(core)!;
            if (previous is not null) {
                if (frame >= 4 || frame == 3 && shared) {
                    Assert.NotSame(previous, native);
                    Assert.Equal(frame == 7, previous.IsDisposed);
                    lastRetired = previous;
                }
                else Assert.Same(previous, native);
            }
            previous = native;
            context.Transition(native.Segments, ResourceStates.CopySource);
            context.CopyBuffer(segments, 0, native.Segments, 0, (ulong)native.Capacity * 28);
            context.Transition(native.Arguments, ResourceStates.CopySource);
            context.CopyBuffer(arguments, 0, native.Arguments, 0, 16);
            context.Transition(target, ResourceStates.CopySource);
            context.CopyTextureToBuffer(pixels, target, in footprint);
            context.Close();
            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(15));
            var draw = MemoryMarshal.Cast<byte, uint>(arguments.Read(16)).ToArray();
            var output = MemoryMarshal.Cast<byte, IntersectionSegment>(segments.Read(513 * 28)).ToArray();
            Assert.Equal(frame == 7 ? 2 : 1, recorded);
            var expectedCount = frame is 5 or 6 ? 0 : frame >= 4 ? 1 : 513;
            Assert.Equal(new uint[] { 2, (uint)expectedCount, 0, 0 }, draw);
            var validOutput = output.Take(expectedCount).ToArray();
            var expectedIds = expectedCount == 0 ? [] : frame >= 4 ? new[] { 77 } : Enumerable.Range(1000, 513);
            Assert.Equal(expectedIds, validOutput.Select(segment => segment.PairId).Order());
            foreach (var segment in validOutput) {
                Assert.Equal(0.5f, segment.Origin.Z, 5);
                Assert.Equal(0.5f, segment.End.Z, 5);
                if (rotated) {
                    Assert.Equal(0.1f, segment.Origin.X, 5);
                    Assert.Equal(0.1f, segment.End.X, 5);
                    Assert.Equal(-0.25f, Math.Min(segment.Origin.Y, segment.End.Y), 5);
                    Assert.Equal(0.25f, Math.Max(segment.Origin.Y, segment.End.Y), 5);
                } else {
                    Assert.Equal(0, segment.Origin.Y, 5);
                    Assert.Equal(0, segment.End.Y, 5);
                    Assert.Equal(-0.15f, Math.Min(segment.Origin.X, segment.End.X), 5);
                    Assert.Equal(0.35f, Math.Max(segment.Origin.X, segment.End.X), 5);
                }
            }
            var pixel = pixels.Read(4, footprint.Offset + 16UL * footprint.Footprint.RowPitch + 16UL * 4);
            Assert.True(expectedCount == 0 || frame == 1 ? pixel[2] > 200 && pixel[0] < 40 && pixel[1] < 40 :
                pixel[frame >= 4 ? 1 : 0] > 200 && (frame == 7 ? pixel[2] > 200 : pixel[2] < 40),
                $"Frame {frame}: expected material-colored GPU line, got {string.Join(',', pixel)}");
        }
        core.SetInputs([]);
        renderer.BeginFrame();
        context.Reset();
        context.Transition(target, ResourceStates.RenderTarget);
        context.SetRenderTargets(targetView, depthView);
        context.SetViewport(32, 32);
        renderer.RenderVisible(context, nodes, catalog.Resolve, in transforms, false, ref frustum);
        var empty = renderer.FindIntersectionResources(core)!;
        context.Transition(empty.Arguments, ResourceStates.CopySource);
        context.CopyBuffer(arguments, 0, empty.Arguments, 0, 16);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(15));
        renderer.BeginFrame();
        Assert.True(previous!.IsDisposed);
        var emptyDraw = MemoryMarshal.Cast<byte, uint>(arguments.Read(16)).ToArray();
        Assert.Equal(new uint[] { 2, 0, 0, 0 }, emptyDraw);
        node.Dispose();
        renderer.BeginFrame();
        renderer.Dispose();
        Assert.Equal(0, resourceHeap.Count);
        Assert.Equal(0, samplerHeap.Count);
        Assert.Equal(28, Marshal.SizeOf<IntersectionSegment>());
        Assert.Equal(16, Marshal.SizeOf<IntersectionCandidate>());
        Assert.Equal(144, Marshal.SizeOf<IntersectionTransforms>());
        Assert.Equal(12, Marshal.OffsetOf<IntersectionSegment>(nameof(IntersectionSegment.End)).ToInt32());
        Assert.Equal(24, Marshal.OffsetOf<IntersectionSegment>(nameof(IntersectionSegment.PairId)).ToInt32());
        Assert.Equal(8, Marshal.OffsetOf<IntersectionCandidate>(nameof(IntersectionCandidate.PairId)).ToInt32());
        Assert.Equal(64, Marshal.OffsetOf<IntersectionTransforms>(nameof(IntersectionTransforms.WorldB)).ToInt32());
        Assert.Equal(128, Marshal.OffsetOf<IntersectionTransforms>(nameof(IntersectionTransforms.CandidateCount)).ToInt32());
        device.ThrowIfDeviceRemoved();
    }
    /// <summary>Uses the actual toolkit node/material lifecycle with the new compute render core.</summary>
    private sealed class GpuLineNode : LineNode {
        /// <summary>
        ///     Creates the compute core while retaining the ordinary line node contract.
        /// </summary>
        /// <returns>The selected-pair intersection render core.</returns>
        protected override HelixToolkit.SharpDX.Core.Core.RenderCore OnCreateRenderCore() => new ApplicationIntersectionCore();
        /// <summary>Creates valid bounds for scene selection; native output comes solely from compute.</summary>
        public GpuLineNode() {
            Geometry = new LineGeometry3D { Positions = new Vector3Collection([new Vector3(-1), new Vector3(1)]), Indices = new IntCollection([0, 1]) };
        }
    }

    /// <summary>Exercises application inheritance through the existing native compute and indirect draw tests.</summary>
    private sealed class ApplicationIntersectionCore : MeshIntersectionRenderCore { }
}
