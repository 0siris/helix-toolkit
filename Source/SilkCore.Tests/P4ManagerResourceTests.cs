using System.Reflection;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;
using Xunit;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

namespace SilkCore.Tests;

/// <summary>Checks complete manager resource generations and same-instance recovery without a graphics device.</summary>
public sealed class P4ManagerResourceTests {
    /// <summary>Publishes fresh, live resources and original descriptions before notifying reinitialized consumers.</summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void RestartReplacesCompleteGenerationAndPreservesEventOrdering() {
        using var effects = new EffectsManager();
        var description = new TechniqueDescription("P4");
        effects.AddTechnique(description);
        var technique = Assert.IsType<Technique>(effects["P4"]);
        var generation = new ResourceGeneration(effects);
        var events = new List<string>();
        EventHandler<EventArgs> disposing = (_, _) => {
            Assert.True(effects.Initialized);
            Assert.False(effects.IsDisposed);
            generation.AssertLive();
            Assert.Same(generation.Pool, effects.StructArrayPool);
            Assert.False(technique.IsDisposed);
            events.Add("disposing");
        };
        EventHandler<EventArgs> reinitialized = (_, _) => {
            Assert.True(effects.Initialized);
            Assert.False(effects.IsDisposed);
            var replacement = new ResourceGeneration(effects);
            replacement.AssertReplaces(generation);
            generation = replacement;
            Assert.Same(description, Assert.Single(effects.TechniqueDescriptions));
            Assert.Equal("P4", Assert.Single(effects.RenderTechniques));
            var replacementTechnique = Assert.IsType<Technique>(effects["P4"]);
            Assert.NotSame(technique, replacementTechnique);
            Assert.True(technique.IsDisposed);
            Assert.Same(description, replacementTechnique.Description);
            technique = replacementTechnique;
            events.Add("reinitialized");
        };
        effects.DisposingResources += disposing;
        effects.Reinitialized += reinitialized;

        for (var epoch = 0; epoch < 3; epoch++) {
            var geometry = new MeshGeometry3D {
                Positions = [new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0)],
                Indices = [0, 1, 2]
            };
            var buffer = Assert.IsType<DefaultMeshGeometryBufferModel>(
                effects.GeometryBufferManager.Register<DefaultMeshGeometryBufferModel>(Guid.NewGuid(), geometry));
            using var storage = effects.StructArrayPool.Register(sizeof(int));
            var id = storage.GetId();
            var value = epoch + 41;
            Assert.True(storage.Write(id, 0, ref value));
            Assert.True(storage.Read<int>(id, 0, out var read));
            Assert.Equal(value, read);
            Assert.Same(effects, buffer.EffectsManager);

            effects.DisposeAllResources();

            Assert.False(effects.Initialized);
            Assert.False(effects.IsDisposed);
            generation.AssertDisposed();
            Assert.True(buffer.IsDisposed);
            Assert.True(storage.IsDisposed);
            Assert.True(technique.IsDisposed);
            Assert.Empty(effects.RenderTechniques);
            Assert.Throws<InvalidOperationException>(() => generation.Pool.Register(sizeof(int)));
            effects.DisposeAllResources();
            Assert.Equal(epoch * 2 + 1, events.Count);

            effects.Reinitialize();

            Assert.Equal((epoch + 1) * 2, events.Count);
            Assert.Equal("disposing", events[^2]);
            Assert.Equal("reinitialized", events[^1]);
            var retained = generation;
            effects.Reinitialize();
            Assert.Equal((epoch + 1) * 2, events.Count);
            Assert.Same(retained.Pool, effects.StructArrayPool);
            Assert.Same(retained.Geometry, effects.GeometryBufferManager);
            Assert.Same(retained.Drawing[2], effects.DeviceContext2D);
        }

        effects.DisposingResources -= disposing;
        effects.Reinitialized -= reinitialized;
        effects.Dispose();
        generation.AssertDisposed();
        Assert.True(effects.IsDisposed);
    }

    /// <summary>Reuses the same manager for genuine 2D attachment, context creation and CPU-backed rendering.</summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void RestartRestoresResourcesForScene2DAndStructStorage() {
        using var effects = new EffectsManager();
        using var root = new PanelNode2D { EnableBitmapCache = false };
        using var child = new RectangleNode2D { EnableBitmapCache = false };
        root.AddChildNode(child);
        D2DDeviceContext? previousContext = null;
        ArrayStorage? previousStorage = null;
        for (var epoch = 0; epoch < 3; epoch++) {
            using (var context = new RenderContext2D(effects.DeviceContext2D, effects, 16, 16)) {
                Assert.False(context.DeviceContext.HasNativeContext);
                Assert.Same(effects.Factory2D, context.DeviceContext.Factory);
                Assert.NotSame(previousContext, context.DeviceContext);
                using var target = new BitmapProxy("P4", context.DeviceContext, new Size2(16, 16),
                    new D2DBitmapProperties());
                root.Attach();
                root.Update(context);
                child.Update(context);
                Assert.True(child.IsAttached);
                Assert.True(child.IsRenderable);
                context.PushRenderTarget(target, true);
                root.Render(context);
                context.PopRenderTarget();
                Assert.False(context.HasTarget);
                root.Detach();
                previousContext = context.DeviceContext;
            }

            using var storage = effects.StructArrayPool.Register(sizeof(long));
            Assert.NotSame(previousStorage, storage);
            var id = storage.GetId();
            var value = 100L + epoch;
            Assert.True(storage.Write(id, 0, ref value));
            Assert.True(storage.Read<long>(id, 0, out var read));
            Assert.Equal(value, read);
            previousStorage = storage;
            effects.DisposeAllResources();
            Assert.True(previousContext.IsDisposed);
            Assert.True(storage.IsDisposed);
            Assert.False(effects.IsDisposed);
            effects.Reinitialize();
        }
    }

    /// <summary>Releases a partially prepared generation without manager disposal or false lifecycle notifications.</summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void FailedRestartLeavesResourcesUnpublishedAndCanRetry() {
        using var effects = new EffectsManager();
        var first = new TechniqueDescription("first");
        var second = new TechniqueDescription("second");
        effects.AddTechnique(first);
        effects.AddTechnique(second);
        effects.DisposeAllResources();
        var reinitialized = 0;
        var disposing = 0;
        effects.Reinitialized += (_, _) => reinitialized++;
        effects.DisposingResources += (_, _) => disposing++;
        second.Name = first.Name;

        Assert.Throws<ArgumentException>(effects.Reinitialize);

        Assert.False(effects.Initialized);
        Assert.False(effects.IsDisposed);
        Assert.Equal(0, reinitialized);
        Assert.Equal(0, disposing);
        Assert.Empty(effects.RenderTechniques);
        foreach (var name in new[] { "geometryBufferManager", "structArrayPool", "factory2D", "device2D",
                     "deviceContext2D", "wicImgFactory", "directWriteFactory" }) {
            var field = typeof(EffectsManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            Assert.Null(field.GetValue(effects));
        }

        second.Name = "second";
        effects.Reinitialize();
        Assert.True(effects.Initialized);
        Assert.Equal(1, reinitialized);
        Assert.Equal(0, disposing);
        new ResourceGeneration(effects).AssertLive();
        Assert.Same(first, effects["first"].Description);
        Assert.Same(second, effects["second"].Description);
    }

    /// <summary>Retains actual manager-owned wrappers to verify generation identity and lifetime.</summary>
    private sealed class ResourceGeneration {
        /// <summary>The CPU-backed shared geometry registry.</summary>
        public readonly GeometryBufferManager Geometry;

        /// <summary>The shared struct storage owner.</summary>
        public readonly StructArrayPool Pool;

        /// <summary>The Direct2D, imaging and text resource wrappers in stable dependency order.</summary>
        public readonly D2DNativeResource[] Drawing;

        /// <summary>Captures every actual shared resource and checks its context/factory association.</summary>
        /// <param name="effects">The initialized manager to inspect.</param>
        public ResourceGeneration(EffectsManager effects) {
            Geometry = Assert.IsType<GeometryBufferManager>(effects.GeometryBufferManager);
            Pool = Assert.IsType<StructArrayPool>(effects.StructArrayPool);
            Drawing = [effects.Factory2D, effects.Device2D, effects.DeviceContext2D,
                effects.WicImgFactory, effects.DirectWriteFactory];
            Assert.Same(effects.Factory2D, effects.DeviceContext2D.Factory);
            AssertLive();
        }

        /// <summary>Requires all wrappers and the struct pool to be alive.</summary>
        public void AssertLive() {
            Assert.False(Pool.IsDisposed);
            Assert.All(Drawing, resource => Assert.False(resource.IsDisposed));
        }

        /// <summary>Requires all wrappers to be disposed and the geometry registry emptied.</summary>
        public void AssertDisposed() {
            Assert.True(Pool.IsDisposed);
            Assert.Equal(0, Geometry.Count);
            Assert.All(Drawing, resource => Assert.True(resource.IsDisposed));
        }

        /// <summary>Requires a complete replacement rather than retaining any previous shared resource.</summary>
        /// <param name="previous">The fully disposed resource generation.</param>
        public void AssertReplaces(ResourceGeneration previous) {
            previous.AssertDisposed();
            AssertLive();
            Assert.NotSame(previous.Geometry, Geometry);
            Assert.NotSame(previous.Pool, Pool);
            for (var index = 0; index < Drawing.Length; index++) Assert.NotSame(previous.Drawing[index], Drawing[index]);
        }
    }
}
