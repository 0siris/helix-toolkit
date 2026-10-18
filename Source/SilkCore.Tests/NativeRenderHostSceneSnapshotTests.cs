using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Cameras;
using HelixToolkit.SharpDX.Core.Model.Scene;
using Silk.NET.Direct3D12;

namespace HelixToolkit.SharpDX.Core.Native;

/// Verifies the complete native frame hierarchy independently of draw pruning.
public sealed class NativeRenderHostSceneSnapshotTests {
    /// Keeps hidden descendants and attach-failed parents in source preorder with root depth zero.
    [Fact]
    [Trait("Category", "Warp")]
    public void ActualPreparationIncludesHiddenAndAttachFailedHierarchy() {
        using var fixture = new Preparation();
        using var root = new GroupNode { Visible = false };
        using var failed = new FailedGroup();
        using var child = new GroupNode();
        using var sibling = new GroupNode();
        root.AddChildNode(failed);
        failed.AddChildNode(child);
        var draws = fixture.Renderer.PrepareScene([root, sibling], fixture.Context);
        var snapshot = fixture.Renderer.PerFrameFlattenedScene.ToArray();
        var depths = snapshot.Select(pair => pair.Key).ToArray();
        var nodes = snapshot.Select(pair => pair.Value).ToArray();
        Assert.Equal([0, 1, 2, 0], depths);
        Assert.Equal<SceneNode>([root, failed, child, sibling], nodes);
        Assert.True(child.RenderCore.IsAttached);
        Assert.DoesNotContain(draws, node => ReferenceEquals(node, root));
        Assert.DoesNotContain(draws, node => ReferenceEquals(node, child));
        Assert.Contains(draws, node => ReferenceEquals(node, sibling));
    }

    /// Clears frame ownership on renderer disposal and excludes nodes disposed before actual preparation.
    [Fact]
    [Trait("Category", "Warp")]
    public void SnapshotChangesOnlyOnPreparationAndClearsOnDisposal() {
        using var fixture = new Preparation();
        using var root = new GroupNode();
        using var child = new GroupNode();
        root.AddChildNode(child);
        _ = fixture.Renderer.PrepareScene([root], fixture.Context);
        var snapshot = fixture.Renderer.PerFrameFlattenedScene;
        child.Dispose();
        Assert.Equal(2, snapshot.Count);
        _ = fixture.Renderer.PrepareScene([root], fixture.Context);
        var remaining = Assert.Single(snapshot);
        Assert.Same(root, remaining.Value);
        fixture.Renderer.Dispose();
        Assert.Empty(snapshot);
        Assert.False(root.RenderCore.IsAttached);
        Assert.Null(root.RenderHost);
    }

    /// Retains both source pair callers and existing tuple callers on the shared depth traversal.
    [Fact]
    public void PairAndTupleTraversalRetainIdenticalDepthAndPruning() {
        using var context = new RenderContext(new PerspectiveCameraCore(), 64, 64);
        using var root = new GroupNode();
        using var child = new GroupNode();
        root.AddChildNode(child);
        IList<SceneNode> roots = new SceneNode[] { root };
        var pairs = new FastList<KeyValuePair<int, SceneNode>>();
        var tuples = new FastList<(int Key, SceneNode Value)>();
        roots.PreorderDft(context, (_, _) => true, pairs);
        roots.PreorderDft(context, (_, _) => true, tuples);
        var expected = tuples.Select(pair => new KeyValuePair<int, SceneNode>(pair.Key, pair.Value)).ToArray();
        Assert.Equal(expected, pairs.ToArray());
    }

    /// Uses the application override in actual native attachment and releases replaced or detached buffers.
    [Fact]
    [Trait("Category", "Warp")]
    public void NativeGeometryUsesApplicationBufferOverride() {
        using var fixture = new Preparation();
        using var node = new NativeBufferMesh { Geometry = CreateTriangle() };
        _ = fixture.Renderer.PrepareScene([node], fixture.Context);
        var first = node.Buffer;
        Assert.NotNull(first);
        Assert.Same(first, ((IGeometryRenderCore)node.RenderCore).GeometryBuffer);
        node.Geometry = CreateTriangle();
        var replacement = node.Buffer;
        Assert.NotSame(first, replacement);
        Assert.True(first.IsDisposed);
        Assert.Same(replacement, ((IGeometryRenderCore)node.RenderCore).GeometryBuffer);
        node.Detach();
        Assert.True(replacement!.IsDisposed);
    }

    /// <summary>
    ///     Creates a valid source geometry for native buffer attachment.
    /// </summary>
    /// <returns>The triangle geometry.</returns>
    private static MeshGeometry3D CreateTriangle() => new() {
        Positions = [new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)],
        Indices = [0, 1, 2]
    };

    /// Exposes the actual application buffer factory without changing renderer ownership.
    private sealed class NativeBufferMesh : MeshNode {
        /// Gets the latest buffer generation returned to the native owner.
        internal DefaultMeshGeometryBufferModel? Buffer { get; private set; }

        protected override IAttachableBufferModel OnCreateNativeBufferModel(Geometry3D? geometry)
            => Buffer = new DefaultMeshGeometryBufferModel { Geometry = geometry };
    }

    /// Fails only this parent's native attachment while retaining the original child hierarchy.
    private sealed class FailedGroup : GroupNode {
        internal override bool Attach() => false;
    }

    /// Owns the genuine native renderer and its descriptor heaps for headless preparation.
    private sealed class Preparation : IDisposable {
        /// The real WARP device.
        private readonly SilkD3D12Device device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        /// The renderer resource heap.
        private readonly SilkD3D12DescriptorHeap resources;
        /// The renderer sampler heap.
        private readonly SilkD3D12DescriptorHeap samplers;
        /// Gets the actual renderer.
        internal SilkD3D12SceneRenderer Renderer { get; }
        /// Gets the actual preparation context.
        internal RenderContext Context { get; } = new(new PerspectiveCameraCore(), 64, 64);

        /// Creates the real native owners without a window.
        internal Preparation() {
            resources = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 2048, true);
            samplers = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 128, true);
            Renderer = new SilkD3D12SceneRenderer(device, resources, samplers);
        }

        public void Dispose() {
            Renderer.Dispose();
            Context.Dispose();
            samplers.Dispose();
            resources.Dispose();
            device.Dispose();
        }
    }
}
