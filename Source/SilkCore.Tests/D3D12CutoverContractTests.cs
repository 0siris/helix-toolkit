using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.ShaderManager;

namespace SilkCore.Tests;

/// <summary>
///     Verifies renderer-independent contracts introduced by the final Direct3D 12 cutover.
/// </summary>
public sealed class D3D12CutoverContractTests {
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
}
