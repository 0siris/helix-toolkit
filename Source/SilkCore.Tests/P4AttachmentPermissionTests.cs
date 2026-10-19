using System.ComponentModel;
using System.Reflection;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using Xunit;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

namespace SilkCore.Tests;

/// <summary>Checks attachment cancellation and reentrant material teardown using actual CPU-backed node state.</summary>
public sealed class P4AttachmentPermissionTests {
    /// <summary>Stops before geometry registration when either technique callback destroys the manager generation.</summary>
    /// <param name="useDelegate">Whether to use the public technique delegate instead of the virtual hook.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Unit")]
    public void TechniqueCallbackCanRetireManagerBeforeGeometryRegistration(bool useDelegate) {
        using var effects = new DefaultEffectsManager();
        var registry = effects.GeometryBufferManager;
        using var node = CreateMesh();
        if (useDelegate) {
            node.OnSetRenderTechnique = manager => {
                var technique = manager[DefaultRenderTechniqueNames.Mesh];
                effects.DisposeAllResources();
                return technique;
            };
        } else node.SelectingTechnique = effects.DisposeAllResources;

        node.Attach(effects, () => effects.Initialized);

        Assert.False(effects.Initialized);
        Assert.False(node.IsAttached);
        Assert.False(node.RenderCore.IsAttached);
        Assert.Equal(0, node.BufferCreations);
        Assert.Equal(0, registry.Count);
        Assert.False(node.HasAttachmentPermission);
        effects.Reinitialize();
        node.SelectingTechnique = null;
        node.OnSetRenderTechnique = null;
        node.Attach(effects);
        Assert.True(node.IsAttached);
        Assert.Equal(1, node.BufferCreations);
    }

    /// <summary>Checks permission after native core attachment and default-value hooks before resource access.</summary>
    /// <param name="duringDefaults">Whether to cancel in default assignment instead of the core attachment hook.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Unit")]
    public void CoreHooksCanRetireManagerBeforeGeometryRegistration(bool duringDefaults) {
        using var effects = new DefaultEffectsManager();
        using var node = CreateMesh();
        var core = Assert.IsType<CallbackMeshCore>(node.RenderCore);
        if (duringDefaults) node.AssigningDefaults = effects.DisposeAllResources;
        else core.Attaching = effects.DisposeAllResources;

        node.Attach(effects, () => effects.Initialized);

        Assert.False(effects.Initialized);
        Assert.True(node.IsAttached);
        Assert.True(core.IsAttached);
        Assert.Null(core.GeometryBuffer);
        Assert.Equal(0, node.BufferCreations);
        Assert.Equal(duringDefaults ? 1 : 0, node.DefaultAssignments);
        Assert.False(node.HasAttachmentPermission);
        node.Detach();
        Assert.False(node.IsAttached);
        Assert.False(core.IsAttached);
    }

    /// <summary>Stops nested group recursion between child attachments and leaves retirement to the caller.</summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void RecursiveGroupsStopLaterChildrenWithoutDetachingEarlierChildren() {
        using var effects = new DefaultEffectsManager();
        using var root = new GroupNode();
        using var nested = new GroupNode();
        using var first = CreateMesh();
        using var laterNested = CreateMesh();
        using var laterRoot = CreateMesh();
        nested.AddChildNode(first);
        nested.AddChildNode(laterNested);
        root.AddChildNode(nested);
        root.AddChildNode(laterRoot);
        var laterSelections = 0;
        laterNested.SelectingTechnique = () => laterSelections++;
        laterRoot.SelectingTechnique = () => laterSelections++;
        first.Attached += (_, _) => effects.DisposeAllResources();

        root.Attach(effects, () => effects.Initialized);

        Assert.True(root.IsAttached);
        Assert.True(nested.IsAttached);
        Assert.True(first.IsAttached);
        Assert.True(first.RenderCore.IsAttached);
        Assert.Equal(1, first.BufferCreations);
        Assert.False(laterNested.IsAttached);
        Assert.False(laterRoot.IsAttached);
        Assert.Equal(0, laterSelections);
        Assert.Equal(0, laterNested.BufferCreations);
        Assert.Equal(0, laterRoot.BufferCreations);
        root.Detach();
        Assert.False(first.IsAttached);
        Assert.False(first.RenderCore.IsAttached);
    }

    /// <summary>Restores attachment permission after an exception and preserves public attachment without a predicate.</summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void PermissionIsRestoredOnExceptionAndPublicAttachmentRetainsLegacyBehavior() {
        using var effects = new DefaultEffectsManager();
        using var node = CreateMesh();
        node.SelectingTechnique = () => throw new InvalidOperationException("callback");
        Assert.Throws<InvalidOperationException>(() => node.Attach(effects, static () => true));
        Assert.False(node.HasAttachmentPermission);
        node.SelectingTechnique = null;
        node.Attach(effects, static () => false);
        Assert.False(node.IsAttached);
        Assert.False(node.HasAttachmentPermission);
        node.Attach(effects);
        Assert.True(node.IsAttached);
        Assert.Equal(1, node.BufferCreations);
    }

    /// <summary>Reuses the retained permission delegate without allocating on an already attached node.</summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void WarmPermissionChecksDoNotAllocate() {
        Assert.Equal("false", AppContext.GetData("System.GC.Concurrent")?.ToString(), ignoreCase: true);
        using var effects = new DefaultEffectsManager();
        using var node = CreateMesh();
        Func<bool> permission = static () => true;
        node.Attach(effects, permission);
        Assert.True(node.IsAttached);
        for (var index = 0; index < 64; index++) node.Attach(effects, permission);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1_000; index++) node.Attach(effects, permission);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.False(node.HasAttachmentPermission);
    }

    /// <summary>Prevents a buffer invalidation handler from retaining a new material during either detach or disposal.</summary>
    /// <param name="native">Whether to use the native-only attachment entry point.</param>
    /// <param name="dispose">Whether to dispose the node rather than detach it.</param>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [Trait("Category", "Unit")]
    public void BufferInvalidationMaterialReplacementCannotResubscribeDuringTeardown(bool native, bool dispose) {
        using var effects = new DefaultEffectsManager();
        using var node = CreateMesh();
        var original = new NotifyingMaterial();
        var replacement = new NotifyingMaterial();
        node.Material = original;
        if (native) Assert.True(node.Attach());
        else node.Attach(effects);
        var core = Assert.IsType<CallbackMeshCore>(node.RenderCore);
        Assert.True(core.IsAttached);
        Assert.NotNull(core.GeometryBuffer);
        Assert.Equal(1, SubscriberCount(original));
        var replacements = 0;
        var wakeups = 0;
        node.Invalidated += (_, kind) => {
            if (kind != InvalidateTypes.Render) return;
            wakeups++;
            if (core.IsAttached && core.GeometryBuffer is null && replacements == 0) {
                replacements++;
                node.Material = replacement;
                Assert.Same(replacement, core.D3D12Material);
                Assert.Equal(0, SubscriberCount(replacement));
            }
        };

        if (dispose) node.Dispose();
        else node.Detach();

        Assert.Equal(1, replacements);
        Assert.Same(replacement, node.Material);
        Assert.False(core.IsAttached);
        Assert.Equal(0, SubscriberCount(original));
        Assert.Equal(0, SubscriberCount(replacement));
        wakeups = 0;
        original.Notify();
        replacement.Notify();
        Assert.Equal(0, wakeups);
        if (dispose) return;
        if (native) Assert.True(node.Attach());
        else node.Attach(effects);
        Assert.Equal(1, SubscriberCount(replacement));
        wakeups = 0;
        replacement.Notify();
        Assert.Equal(1, wakeups);
    }

    /// <summary>Counts actual event subscribers to catch retention even when disposed nodes no longer raise invalidation.</summary>
    /// <param name="material">The material whose subscription ownership is checked.</param>
    /// <returns>The number of property-change subscribers.</returns>
    private static int SubscriberCount(MaterialCore material) {
        var field = typeof(ObservableObject).GetField("PropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (field.GetValue(material) as PropertyChangedEventHandler)?.GetInvocationList().Length ?? 0;
    }

    /// <summary>Creates a real mesh node with geometry and inspectable attachment callbacks.</summary>
    /// <returns>The mesh node used by these regressions.</returns>
    private static CallbackMeshNode CreateMesh() => new() {
        Geometry = new MeshGeometry3D {
            Positions = [new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0)],
            Indices = [0, 1, 2]
        }
    };

    /// <summary>Exercises the actual mesh attachment and buffer-registration path with custom user hooks.</summary>
    private sealed class CallbackMeshNode : MeshNode {
        /// <summary>Gets or sets the callback invoked after resolving a real technique.</summary>
        public Action? SelectingTechnique { get; set; }

        /// <summary>Gets or sets the callback invoked while assigning render-core defaults.</summary>
        public Action? AssigningDefaults { get; set; }

        /// <summary>Gets the number of attempted geometry buffer registrations.</summary>
        public int BufferCreations { get; private set; }

        /// <summary>Gets the number of default-value assignments.</summary>
        public int DefaultAssignments { get; private set; }

        /// <summary>Gets whether a permission callback remains scoped to this node.</summary>
        public bool HasAttachmentPermission => AttachmentPermission is not null;

        /// <inheritdoc />
        protected override RenderCore OnCreateRenderCore() => new CallbackMeshCore();

        /// <inheritdoc />
        protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) {
            var technique = base.OnCreateRenderTechnique(effectsManager);
            SelectingTechnique?.Invoke();
            return technique;
        }

        /// <inheritdoc />
        protected override void AssignDefaultValuesToCore(RenderCore core) {
            DefaultAssignments++;
            base.AssignDefaultValuesToCore(core);
            AssigningDefaults?.Invoke();
        }

        /// <inheritdoc />
        protected override IAttachableBufferModel OnCreateBufferModel(Guid modelGuid, Geometry3D? geometry) {
            BufferCreations++;
            return base.OnCreateBufferModel(modelGuid, geometry);
        }
    }

    /// <summary>Runs a custom callback during the productive render-core attachment hook without a GPU.</summary>
    private sealed class CallbackMeshCore : MeshRenderCore {
        /// <summary>Gets or sets the callback invoked during core attachment.</summary>
        public Action? Attaching { get; set; }

        /// <inheritdoc />
        protected override bool OnAttachD3D12() {
            Attaching?.Invoke();
            return base.OnAttachD3D12();
        }
    }

    /// <summary>Publishes actual material parameter changes without allocating event arguments.</summary>
    private sealed class NotifyingMaterial : MaterialCore {
        /// <summary>The retained property-change arguments.</summary>
        private static readonly PropertyChangedEventArgs Args = new("DiffuseColor");

        /// <summary>Raises a material property-change notification.</summary>
        public void Notify() => RaisePropertyChanged(Args);
    }
}
