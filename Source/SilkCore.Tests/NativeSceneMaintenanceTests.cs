using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Cameras;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Utilities;
using Silk.NET.Direct3D12;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector2 = Silk.NET.Maths.Vector2D<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

namespace HelixToolkit.SharpDX.Core.Native;

/// Exercises the original maintenance hooks through genuine WARP native scene preparation.
public sealed class NativeSceneMaintenanceTests {
    /// Builds before picking, retains an unchanged tree, and rebuilds once after physical positions change.
    [Fact]
    [Trait("Category", "Warp")]
    public void PreparationBuildsAndInvalidatesTheExistingMeshOctree() {
        using var fixture = new Preparation();
        var geometry = CreateGeometry();
        using var tooth = new MeshNode { Geometry = geometry, Material = new DiffuseMaterialCore() };
        var builds = 0;
        geometry.PropertyChanged += (_, args) => {
            if (args.PropertyName == nameof(Geometry3D.Octree)) builds++;
        };
        Assert.Null(geometry.Octree);
        Assert.True(fixture.Context.AutoUpdateOctree);

        var prepared = fixture.Renderer.PrepareScene([tooth], fixture.Context);
        var first = geometry.Octree;
        var preparedTooth = Assert.Single(prepared);
        Assert.Same(tooth, preparedTooth);
        Assert.NotNull(first);
        Assert.True(first.TreeBuilt);
        Assert.False(geometry.OctreeDirty);
        Assert.Equal(1, builds);
        AssertHit(tooth, fixture.Context, 0, true);

        _ = fixture.Renderer.PrepareScene([tooth], fixture.Context);
        Assert.Same(first, geometry.Octree);
        Assert.Equal(1, builds);

        geometry.Positions = Positions(3);
        Assert.Null(geometry.Octree);
        Assert.True(geometry.OctreeDirty);
        _ = fixture.Renderer.PrepareScene([tooth], fixture.Context);
        var rebuilt = geometry.Octree;
        Assert.NotNull(rebuilt);
        Assert.NotSame(first, rebuilt);
        Assert.Equal(2, builds);
        AssertHit(tooth, fixture.Context, 0, false);
        AssertHit(tooth, fixture.Context, 3, true);
        _ = fixture.Renderer.PrepareScene([tooth], fixture.Context);
        Assert.Same(rebuilt, geometry.Octree);
        Assert.Equal(2, builds);
    }

    /// Maintains hidden descendants while respecting nonhittable gum and an explicitly disabled octree context.
    [Fact]
    [Trait("Category", "Warp")]
    public void HiddenHierarchyKeepsMaintenanceAndExistingOctreeGuards() {
        using var fixture = new Preparation();
        using var root = new GroupNode { Visible = false };
        var toothGeometry = CreateGeometry();
        var gumGeometry = CreateGeometry();
        using var tooth = new MeshNode { Geometry = toothGeometry };
        using var gum = new MeshNode { Geometry = gumGeometry, IsHitTestVisible = false };
        root.AddChildNode(tooth);
        root.AddChildNode(gum);

        var hidden = fixture.Renderer.PrepareScene([root], fixture.Context);
        Assert.Empty(hidden);
        Assert.NotNull(toothGeometry.Octree);
        Assert.Null(gumGeometry.Octree);
        Assert.True(gumGeometry.OctreeDirty);
        AssertHit(gum, fixture.Context, 0, false);

        toothGeometry.ClearOctree();
        fixture.Context.AutoUpdateOctree = false;
        root.Visible = true;
        var disabled = fixture.Renderer.PrepareScene([root], fixture.Context);
        Assert.Contains(disabled, node => ReferenceEquals(node, tooth));
        Assert.Null(toothGeometry.Octree);
        Assert.True(toothGeometry.OctreeDirty);
        Assert.Null(gumGeometry.Octree);
    }

    /// Runs every update before maintenance and excludes failed attachment, disposal and late detachment.
    [Fact]
    [Trait("Category", "Warp")]
    public void MaintenanceFollowsAllUpdatesAndRechecksNodeLifetime() {
        using var fixture = new Preparation();
        var calls = new List<string>();
        using var root = new GroupNode { Visible = false };
        using var first = new ObservedMesh("first", calls);
        using var second = new ObservedMesh("second", calls);
        using var failed = new ObservedMesh("failed", calls) { FailAttach = true };
        using var disposed = new ObservedMesh("disposed", calls);
        using var detached = new ObservedMesh("detached", calls);
        using var last = new ObservedMesh("last", calls) {
            AfterUpdate = () => { disposed.Dispose(); detached.Detach(); }
        };
        last.AfterMaintenance = () => last.Visible = false;
        using var alreadyDisposed = new ObservedMesh("alreadyDisposed", calls);
        alreadyDisposed.Dispose();
        root.AddChildNode(first);
        root.AddChildNode(second);
        var prepared = fixture.Renderer.PrepareScene(
            [root, failed, disposed, detached, last, alreadyDisposed], fixture.Context);

        Assert.Equal(["update:first", "update:second", "update:disposed", "update:detached", "update:last",
            "maintain:first", "maintain:second", "maintain:last"], calls);
        Assert.Empty(prepared);
    }

    /// Exercises the built-in group manager's pending insert and removal through native preparation.
    [Fact]
    [Trait("Category", "Warp")]
    public void GroupMaintenanceProcessesPendingInsertAndRemoval() {
        using var fixture = new Preparation();
        var manager = new GroupNodeGeometryBoundOctreeManager();
        using var group = new GroupNode { OctreeManager = manager };
        using var first = new MeshNode { Geometry = CreateGeometry(), Material = new DiffuseMaterialCore() };
        group.AddChildNode(first);
        manager.RequestRebuild();
        _ = fixture.Renderer.PrepareScene([group], fixture.Context);
        Assert.NotNull(group.Octree);
        AssertHit(group, fixture.Context, 0, true);

        using var inserted = new MeshNode { Geometry = CreateGeometry(3), Material = new DiffuseMaterialCore() };
        group.AddChildNode(inserted);
        _ = fixture.Renderer.PrepareScene([group], fixture.Context);
        Assert.NotNull(inserted.Geometry!.Octree);
        AssertHit(group, fixture.Context, 3, true);

        group.RemoveChildNode(inserted);
        _ = fixture.Renderer.PrepareScene([group], fixture.Context);
        AssertHit(group, fixture.Context, 3, false);
        AssertHit(group, fixture.Context, 0, true);
    }

    /// Dispatches original instance and batched maintenance instead of maintaining only ordinary mesh nodes.
    [Fact]
    [Trait("Category", "Warp")]
    public void PreparationUsesExistingInstanceAndBatchedHooks() {
        using var fixture = new Preparation();
        var manager = new InstancingRenderableOctreeManager();
        using var instances = new InstancingMeshNode {
            Geometry = CreateGeometry(), Material = new PhongMaterialCore(),
            Instances = [Matrix.Identity], OctreeManager = manager
        };
        var batchGeometry = CreateGeometry(3);
        using var batch = new MaintainedBatch {
            Geometries = [new BatchedMeshGeometryConfig(batchGeometry, Matrix.Identity, 0)],
            Materials = [new PhongMaterialCore()]
        };
        Assert.Null(manager.Octree);
        Assert.False(batch.BoundsTreeBuilt);

        _ = fixture.Renderer.PrepareScene([instances, batch], fixture.Context);
        Assert.NotNull(manager.Octree);
        Assert.True(manager.Octree.TreeBuilt);
        Assert.NotNull(instances.Geometry!.Octree);
        Assert.NotNull(batchGeometry.Octree);
        Assert.True(batch.BoundsTreeBuilt);
        AssertHit(instances, fixture.Context, 0, true);
        var instanceTree = manager.Octree;
        var meshTree = batchGeometry.Octree;
        _ = fixture.Renderer.PrepareScene([instances, batch], fixture.Context);
        Assert.Same(instanceTree, manager.Octree);
        Assert.Same(meshTree, batchGeometry.Octree);
    }

    /// Invokes the actual ray intersection and checks the physical hit coordinate.
    /// <param name="node">The prepared original scene node.</param>
    /// <param name="context">The native frame matrices.</param>
    /// <param name="x">The ray's horizontal world coordinate.</param>
    /// <param name="expected">Whether the triangle should intersect the ray.</param>
    private static void AssertHit(SceneNode node, RenderContext context, float x, bool expected) {
        var ray = new Ray(new Vector3(x, 0, 5), -Vector3.UnitZ);
        var hitContext = new HitTestContext(context, ray, Vector2.Zero);
        var hits = new List<HitTestResult>();
        var hit = node.HitTest(hitContext, ref hits);
        Assert.Equal(expected, hit);
        if (!expected) {
            Assert.Empty(hits);
            return;
        }
        Assert.NotEmpty(hits);
        foreach (var result in hits) {
            Assert.True(result.IsValid);
            Assert.InRange(Math.Abs(result.PointHit.X - x), 0, 0.0001);
            Assert.InRange(Math.Abs(result.PointHit.Z), 0, 0.0001);
        }
    }

    /// Creates analytic triangle geometry for physical picking checks.
    /// <param name="x">The horizontal world offset.</param>
    /// <returns>The original mesh geometry.</returns>
    private static MeshGeometry3D CreateGeometry(float x = 0) => new() {
        Positions = Positions(x),
        Normals = new Vector3Collection([Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ]),
        Indices = new IntCollection([0, 2, 1])
    };

    /// Creates three vertices around the requested world offset.
    /// <param name="x">The horizontal world offset.</param>
    /// <returns>The triangle positions.</returns>
    private static Vector3Collection Positions(float x) =>
        new([new(x - 0.5f, 0.5f, 0), new(x + 0.5f, 0.5f, 0), new(x, -0.5f, 0)]);

    /// Observes phase order without replacing the original mesh update and maintenance hooks.
    private sealed class ObservedMesh : MeshNode {
        /// The callback identity.
        private readonly string name;
        /// The ordered callback log.
        private readonly List<string> calls;
        /// Gets or sets whether native attachment deliberately fails.
        internal bool FailAttach { get; init; }
        /// Gets or sets a lifetime transition applied after this node's update.
        internal Action? AfterUpdate { get; init; }
        /// Gets or sets a visibility transition applied by the maintenance hook before draw pruning.
        internal Action? AfterMaintenance { get; set; }

        /// <summary>
        ///     Creates an observer with valid original mesh geometry and material.
        /// </summary>
        /// <param name="name">The callback identity.</param>
        /// <param name="calls">The ordered callback log.</param>
        internal ObservedMesh(string name, List<string> calls) {
            this.name = name;
            this.calls = calls;
            Geometry = CreateGeometry();
            Material = new DiffuseMaterialCore();
        }

        internal override bool Attach() => !FailAttach && base.Attach();

        public override void Update(RenderContext context) {
            calls.Add("update:" + name);
            base.Update(context);
            AfterUpdate?.Invoke();
        }

        public override void UpdateNotRender(RenderContext context) {
            calls.Add("maintain:" + name);
            base.UpdateNotRender(context);
            AfterMaintenance?.Invoke();
        }
    }

    /// Exposes the original protected batched bounds tree without replacing its maintenance hook.
    private sealed class MaintainedBatch : BatchedMeshNode {
        /// Gets whether the existing batch maintenance hook built its original bounds tree.
        internal bool BoundsTreeBuilt => BatchedGeometryOctree?.TreeBuilt == true;
    }

    /// Owns genuine WARP resources for isolated native scene preparation.
    private sealed class Preparation : IDisposable {
        /// The native WARP device.
        private readonly SilkD3D12Device device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        /// The renderer's shader resource descriptors.
        private readonly SilkD3D12DescriptorHeap resources;
        /// The renderer's sampler descriptors.
        private readonly SilkD3D12DescriptorHeap samplers;
        /// Gets the native renderer under test.
        internal SilkD3D12SceneRenderer Renderer { get; }
        /// Gets the original native context, including its unchanged octree default.
        internal RenderContext Context { get; } = new(new PerspectiveCameraCore(), 64, 64);

        /// Creates the original renderer with genuine native descriptor heaps.
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
