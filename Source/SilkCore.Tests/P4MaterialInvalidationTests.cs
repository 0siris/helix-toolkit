using System.ComponentModel;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using Xunit;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

namespace SilkCore.Tests;

/// <summary>Checks real material notifications and ancestor wakeups without creating a graphics device.</summary>
public sealed class P4MaterialInvalidationTests {
    /// <summary>Wakes independent drained roots and multiple observers of the same root on a shared color update.</summary>
    /// <param name="kind">The concrete material node kind.</param>
    /// <param name="native">Whether to use native attachment instead of the public manager path.</param>
    [Theory]
    [InlineData("mesh", true)]
    [InlineData("mesh", false)]
    [InlineData("line", true)]
    [InlineData("line", false)]
    [InlineData("point", true)]
    [InlineData("point", false)]
    [Trait("Category", "Unit")]
    public void SharedColorChangesWakeDrainedAncestorConsumers(string kind, bool native) {
        using var effects = new DefaultEffectsManager();
        using var firstRoot = new GroupNode();
        using var secondRoot = new GroupNode();
        using var parent = new GroupNode();
        using var first = CreateNode(kind);
        using var second = CreateNode(kind);
        var material = new PhongMaterialCore();
        first.Material = second.Material = material;
        firstRoot.AddChildNode(parent);
        parent.AddChildNode(first);
        secondRoot.AddChildNode(second);
        Attach(first, effects, native);
        Attach(second, effects, native);
        using var firstConsumer = new DemandObserver(firstRoot);
        using var sharedConsumer = new DemandObserver(firstRoot);
        using var secondConsumer = new DemandObserver(secondRoot);
        first.InvalidateRender();
        second.InvalidateRender();
        firstConsumer.Drain();
        sharedConsumer.Drain();
        secondConsumer.Drain();

        material.DiffuseColor = new Color4(1, 0, 0, 1);

        Assert.Equal(1, firstConsumer.Requests);
        Assert.Equal(1, sharedConsumer.Requests);
        Assert.Equal(1, secondConsumer.Requests);
        Assert.True(firstConsumer.Pending && sharedConsumer.Pending && secondConsumer.Pending);
        var replacement = new PhongMaterialCore();
        first.Material = second.Material = replacement;
        firstConsumer.Drain();
        sharedConsumer.Drain();
        secondConsumer.Drain();
        material.DiffuseColor = new Color4(0, 1, 0, 1);
        Assert.Equal(0, firstConsumer.Requests);
        Assert.Equal(0, sharedConsumer.Requests);
        Assert.Equal(0, secondConsumer.Requests);
        replacement.DiffuseColor = new Color4(0, 0, 1, 1);
        Assert.Equal(1, firstConsumer.Requests);
        Assert.Equal(1, sharedConsumer.Requests);
        Assert.Equal(1, secondConsumer.Requests);
    }

    /// <summary>Stops notifications while detached or disposed and restores the current material on rejoining.</summary>
    /// <param name="native">Whether to exercise native attachment rather than public manager attachment.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Category", "Unit")]
    public void DetachReplacementRejoinAndDisposeOwnSubscriptions(bool native) {
        using var effects = new DefaultEffectsManager();
        using var root = new GroupNode();
        using var node = CreateNode("mesh");
        var original = new PhongMaterialCore();
        var replacement = new PhongMaterialCore();
        node.Material = original;
        root.AddChildNode(node);
        Attach(node, effects, native);
        using var consumer = new DemandObserver(root);
        original.DiffuseColor = new Color4(1, 0, 0, 1);
        Assert.Equal(1, consumer.Requests);

        node.Detach();
        consumer.Drain();
        original.DiffuseColor = new Color4(0, 1, 0, 1);
        Assert.Equal(0, consumer.Requests);
        node.Material = replacement;
        consumer.Drain();
        replacement.DiffuseColor = new Color4(1, 0, 0, 1);
        Assert.Equal(0, consumer.Requests);
        Attach(node, effects, native);
        consumer.Drain();
        original.DiffuseColor = new Color4(0, 0, 1, 1);
        Assert.Equal(0, consumer.Requests);
        replacement.DiffuseColor = new Color4(0, 1, 0, 1);
        Assert.Equal(1, consumer.Requests);

        node.Material = null;
        consumer.Drain();
        replacement.DiffuseColor = new Color4(0, 0, 1, 1);
        Assert.Equal(0, consumer.Requests);
        node.Material = replacement;
        node.Dispose();
        consumer.Drain();
        replacement.DiffuseColor = new Color4(1, 0, 0, 1);
        Assert.Equal(0, consumer.Requests);
        Assert.False(consumer.Pending);
    }

    /// <summary>Observes once while either renderer owns a shared node, then stops at the final release.</summary>
    /// <param name="releaseLatest">Whether the second consumer releases before the first.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Unit")]
    public void SharedNativeLeasesRetainExactlyOneMaterialObserver(bool releaseLatest) {
        using var root = new GroupNode();
        using var node = CreateNode("mesh");
        var material = new RetainedArgsMaterial();
        node.Material = material;
        root.AddChildNode(node);
        var first = new object();
        var second = new object();
        node.RetainNativeAttachment(first, null);
        Assert.True(node.Attach());
        node.RetainNativeAttachment(second, null);
        Assert.True(node.Attach());
        Assert.True(node.Attach());
        using var consumer = new DemandObserver(root);
        material.Notify();
        Assert.Equal(1, consumer.Requests);
        node.ReleaseNativeAttachment(
            releaseLatest
                ? second
                : first
        );
        consumer.Drain();
        material.Notify();
        Assert.Equal(1, consumer.Requests);
        node.ReleaseNativeAttachment(
            releaseLatest
                ? first
                : second
        );
        consumer.Drain();
        material.Notify();
        Assert.Equal(0, consumer.Requests);
        node.RetainNativeAttachment(first, null);
        Assert.True(node.Attach());
        consumer.Drain();
        material.Notify();
        Assert.Equal(1, consumer.Requests);
        node.ReleaseNativeAttachment(first);
    }

    /// <summary>Excludes MaterialCore's ordinary event-args allocation to measure node and ancestor overhead.</summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void WarmNativeAttachmentAndNotificationDoNotAllocate() {
        Assert.Equal(
            "false", AppContext.GetData("System.GC.Concurrent")
                ?.ToString(), ignoreCase: true
        );
        using var root = new GroupNode();
        using var node = CreateNode("mesh");
        var material = new RetainedArgsMaterial();
        node.Material = material;
        root.AddChildNode(node);
        Assert.True(node.Attach());
        using var consumer = new DemandObserver(root);
        using var secondConsumer = new DemandObserver(root);
        for (var index = 0; index < 64; index++) {
            node.Attach();
            material.Notify();
            consumer.Drain();
            secondConsumer.Drain();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1_000; index++) {
            node.Attach();
            material.Notify();
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.Equal(1_000, consumer.Requests);
        Assert.Equal(1_000, secondConsumer.Requests);
        Assert.True(consumer.Pending);
        Assert.True(secondConsumer.Pending);
    }

    /// <summary>Creates real CPU-backed geometry sufficient for native attachment without a GPU.</summary>
    /// <param name="kind">The requested material node kind.</param>
    /// <returns>A mesh, line or point node with valid geometry.</returns>
    private static MaterialGeometryNode CreateNode(string kind) => kind switch {
        "mesh" => new MeshNode {
            Geometry = new MeshGeometry3D {
                Positions = [new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0)], Indices = [0, 1, 2]
            }
        },
        "line" => new LineNode {
            Geometry = new LineGeometry3D { Positions = [new Vector3(0, 0, 0), new Vector3(1, 0, 0)], Indices = [0, 1] }
        },
        "point" => new PointNode { Geometry = new PointGeometry3D { Positions = [new Vector3(0, 0, 0)], Indices = [0] } },
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    /// <summary>Exercises one of the two productive attachment entry points.</summary>
    /// <param name="node">The node to attach.</param>
    /// <param name="effects">The manager used by public attachment.</param>
    /// <param name="native">Whether to use native attachment.</param>
    private static void Attach(MaterialGeometryNode node, EffectsManager effects, bool native) {
        if (native) Assert.True(node.Attach());
        else {
            node.Attach(effects);
            Assert.True(node.IsAttached);
        }
    }

    /// <summary>Raises genuine MaterialCore notifications using one retained event-args instance.</summary>
    private sealed class RetainedArgsMaterial : MaterialCore {
        /// <summary>The reusable material parameter notification.</summary>
        private static readonly PropertyChangedEventArgs Args = new("DiffuseColor");

        /// <summary>Publishes a material parameter change without allocating event arguments.</summary>
        public void Notify() => RaisePropertyChanged(Args);
    }

    /// <summary>Records actual ancestor render notifications and models a drained demand consumer.</summary>
    private sealed class DemandObserver : IDisposable {
        /// <summary>The actual scene root observed by this consumer.</summary>
        private readonly GroupNode root;

        /// <summary>Subscribes an independent consumer to the root.</summary>
        /// <param name="root">The scene root whose descendant changes request frames.</param>
        public DemandObserver(GroupNode root) {
            this.root = root;
            root.Invalidated += OnInvalidated;
        }

        /// <summary>Gets the render request count since the last drain.</summary>
        public int Requests { get; private set; }

        /// <summary>Gets whether rendering is pending after a root notification.</summary>
        public bool Pending { get; private set; }

        /// <summary>Consumes pending demand before checking a new material change.</summary>
        public void Drain() {
            Requests = 0;
            Pending = false;
        }

        /// <summary>Records only render demand from the real ancestor event.</summary>
        /// <param name="sender">The changed descendant.</param>
        /// <param name="kind">The invalidation kind.</param>
        private void OnInvalidated(object? sender, InvalidateTypes kind) {
            if (kind != InvalidateTypes.Render) return;
            Requests++;
            Pending = true;
        }

        /// <summary>Removes the root observer.</summary>
        public void Dispose() => root.Invalidated -= OnInvalidated;
    }
}