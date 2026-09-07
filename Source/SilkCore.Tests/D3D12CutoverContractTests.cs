using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities;

using Matrix = Silk.NET.Maths.Matrix4X4<float>;

namespace SilkCore.Tests;

/// <summary>
///     Verifies renderer-independent contracts introduced by the final Direct3D 12 cutover.
/// </summary>
public sealed class D3D12CutoverContractTests {
    /// <summary>
    ///     Verifies the render core exposes only the canonical renderer lifecycle.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void RenderCoreUsesCanonicalLifecycle() {
        using var core = new LifecycleRenderCore();

        core.Attach();
        core.Attach();

        Assert.True(core.IsAttached);
        Assert.Equal(1, core.AttachCount);

        core.Detach();
        core.Detach();

        Assert.False(core.IsAttached);
        Assert.Equal(1, core.DetachCount);
    }

    /// <summary>
    ///     Verifies the public node detach path releases a node attached by the Direct3D 12 traversal.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void SceneNodeDetachReleasesCanonicalRenderCore() {
        using var node = new LifecycleSceneNode();

        Assert.True(node.Attach());
        Assert.True(node.Core.IsAttached);

        node.Detach();

        Assert.False(node.Core.IsAttached);
        Assert.Equal(1, node.Core.DetachCount);
    }

    /// <summary>
    ///     Verifies disposal cannot bypass canonical renderer detachment.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void RenderCoreDisposeDetachesCanonicalLifecycle() {
        var core = new LifecycleRenderCore();
        core.Attach();

        core.Dispose();

        Assert.False(core.IsAttached);
        Assert.Equal(1, core.DetachCount);
    }

    /// <summary>
    ///     Verifies the Direct3D 11 technique, device, and command-context boundary stays absent from render cores.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void RenderCoreLegacyD3D11BoundaryIsAbsent() {
        var type = typeof(RenderCore);

        Assert.Null(type.GetProperty("EffectTechnique"));
        Assert.Null(type.GetProperty("Device"));
        Assert.DoesNotContain(type.GetMethods(), method =>
            method.Name is "Render" or "RenderShadow" or "RenderCustom" or "RenderDepth" or "Update");
        Assert.DoesNotContain(type.GetMethods(), method =>
            method.Name == "Attach" && method.GetParameters().Length != 0);
    }

    /// <summary>
    ///     Verifies scene nodes no longer expose the legacy Direct3D 11 render dispatch surface.
    /// </summary>
    /// <param name="methodName">The removed render method name.</param>
    [Theory]
    [InlineData("Render")]
    [InlineData("RenderShadow")]
    [InlineData("RenderCustom")]
    [InlineData("RenderDepth")]
    [Trait("Category", "Unit")]
    public void SceneNodeLegacyRenderDispatchIsAbsent(string methodName) {
        Assert.DoesNotContain(typeof(SceneNode).GetMethods(), method => method.Name == methodName);
    }

    /// <summary>
    ///     Verifies removed renderer-host contracts cannot accidentally return to the public SilkCore assembly.
    /// </summary>
    /// <param name="typeName">The removed assembly-qualified type name.</param>
    [Theory]
    [InlineData("HelixToolkit.SharpDX.Core.Interface.IRenderHost")]
    [InlineData("HelixToolkit.SharpDX.Core.Interface.IRenderer")]
    [InlineData("HelixToolkit.SharpDX.Core.Render.RenderHost.DefaultRenderHost")]
    [InlineData("HelixToolkit.SharpDX.Core.Render.RenderBuffers.DX11SwapChainRenderBufferProxy")]
    [InlineData("HelixToolkit.SharpDX.Core.Core.OrderIndependentTransparentRenderCore")]
    [InlineData("HelixToolkit.SharpDX.Core.Core.OitDepthPeeling")]
    [InlineData("HelixToolkit.SharpDX.Core.Core.SsaoCore")]
    [InlineData("HelixToolkit.SharpDX.Core.Interface.RenderParameter")]
    [InlineData("HelixToolkit.SharpDX.Core.ShaderManager.MaterialVariablePool")]
    [InlineData("HelixToolkit.SharpDX.Core.ShaderManager.TextureResourceManager")]
    [InlineData("HelixToolkit.SharpDX.Core.ShaderManager.StatePoolManager")]
    [InlineData("HelixToolkit.SharpDX.Core.ShaderManager.ShaderPoolManager")]
    [InlineData("HelixToolkit.SharpDX.Core.ShaderManager.IBufferPool")]
    [InlineData("HelixToolkit.SharpDX.Core.ShaderManager.ConstantBufferPool")]
    [InlineData("HelixToolkit.SharpDX.Core.Render.DeviceContextPool")]
    [InlineData("HelixToolkit.SharpDX.Core.Core.Buffers.GeometryBufferManager")]
    [InlineData("HelixToolkit.SharpDX.Core.SharpDX.Toolkit.Graphics.GraphicsResource")]
    [InlineData("HelixToolkit.SharpDX.Core.SharpDX.Toolkit.Graphics.Texture")]
    [InlineData("HelixToolkit.SharpDX.Core.SharpDX.Toolkit.Graphics.Texture1D")]
    [InlineData("HelixToolkit.SharpDX.Core.SharpDX.Toolkit.Graphics.Texture2D")]
    [InlineData("HelixToolkit.SharpDX.Core.SharpDX.Toolkit.Graphics.Texture3D")]
    [InlineData("HelixToolkit.SharpDX.Core.SharpDX.Toolkit.Graphics.TextureCube")]
    [InlineData("HelixToolkit.SharpDX.Core.Utilities.TextureLoader")]
    [Trait("Category", "Unit")]
    public void LegacyRenderHostTypesAreAbsent(string typeName) {
        Assert.Null(typeof(MeshNode).Assembly.GetType(typeName, false, false));
    }

    /// <summary>
    ///     Verifies Silk.NET Direct3D 11 source references remain confined to Desktop Duplication.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void Direct3D11SourceReferencesAreConfinedToDesktopCapture() {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "SilkCore")))
            directory = directory.Parent;

        var sourceDirectory = Assert.IsType<DirectoryInfo>(directory);
        var references = Directory.EnumerateFiles(Path.Combine(sourceDirectory.FullName, "SilkCore"), "*.cs",
                SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("Silk.NET.Direct3D11", StringComparison.Ordinal))
            .Select(Path.GetFullPath)
            .ToArray();

        var captureSource = Path.GetFullPath(Path.Combine(sourceDirectory.FullName,
            "SilkCore",
            "Native",
            "D3D11DesktopCaptureSource.cs"));
        Assert.Equal([captureSource], references);
    }

    /// <summary>
    ///     Verifies a scene node retains its custom technique selection without attaching Direct3D 11 resources.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void SceneNodeResolvesCustomTechniqueForD3D12() {
        using var effectsManager = new DefaultEffectsManager();
        using var node = new MeshNode {
            OnSetRenderTechnique = manager => manager[DefaultRenderTechniqueNames.Lines]
        };

        var techniqueName = node.ResolveD3D12TechniqueName(effectsManager);

        Assert.Equal(DefaultRenderTechniqueNames.Lines, techniqueName);
        Assert.Same(effectsManager, node.EffectsManager);
        Assert.False(node.IsAttached);
    }

    /// <summary>
    ///     Verifies one scene node cannot silently switch technique registries during its Direct3D 12 lifetime.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void SceneNodeRejectsDifferentD3D12EffectsManager() {
        using var first = new DefaultEffectsManager();
        using var second = new DefaultEffectsManager();
        using var node = new MeshNode();
        node.ResolveD3D12TechniqueName(first);

        Assert.Throws<InvalidOperationException>(() => node.ResolveD3D12TechniqueName(second));
    }

    /// <summary>
    ///     Minimal render core used to observe canonical lifecycle calls.
    /// </summary>
    private sealed class LifecycleRenderCore : RenderCore {
        /// <summary>
        ///     Initializes the lifecycle test core.
        /// </summary>
        internal LifecycleRenderCore() : base(HelixToolkit.SharpDX.Core.Interface.RenderType.None) { }

        /// <summary>
        ///     Gets the successful attach-call count.
        /// </summary>
        internal int AttachCount { get; private set; }

        /// <summary>
        ///     Gets the detach-call count.
        /// </summary>
        internal int DetachCount { get; private set; }

        /// <inheritdoc />
        protected override bool OnAttachD3D12() {
            AttachCount++;
            return true;
        }

        /// <inheritdoc />
        protected override void OnDetachD3D12() => DetachCount++;
    }

    /// <summary>
    ///     Minimal scene node exposing the lifecycle test core.
    /// </summary>
    private sealed class LifecycleSceneNode : SceneNode {
        /// <summary>
        ///     Gets the lazily created lifecycle render core.
        /// </summary>
        internal LifecycleRenderCore Core => Assert.IsType<LifecycleRenderCore>(RenderCore);

        /// <inheritdoc />
        protected override RenderCore OnCreateRenderCore() => new LifecycleRenderCore();

        /// <inheritdoc />
        protected override bool CanHitTest(HitTestContext? context) => false;

        /// <inheritdoc />
        protected override bool OnHitTest(
            HitTestContext context,
            Matrix totalModelMatrix,
            ref List<HitTestResult> hits
        ) => false;
    }
}
