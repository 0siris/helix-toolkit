using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Native;
using Silk.NET.DXGI;

namespace SilkCore.Tests;

/// Protects the ordinary startup path with attached mesh placeholders that have no geometry yet.
public sealed class EmptyGeometryPassTests {
    /// Keeps an empty placeholder attached without constructing a graphics pipeline for undefined topology.
    [Fact]
    [Trait("Category", "Warp")]
    public void AttachedEmptyMeshDoesNotCreateGraphicsPass() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(driverType: SilkDriverType.Warp);
        using var effects = new DefaultEffectsManager();
        using var node = new MeshNode { Material = new PhongMaterialCore() };
        using var catalog = new D3D12ScenePassCatalog(device, Format.FormatR8G8B8A8Unorm,
            Format.FormatD32FloatS8X24Uint, effectsManager: effects);
        node.Attach(effects);
        var buffer = ((IGeometryRenderCore)node.RenderCore).GeometryBuffer;
        Assert.True(node.IsAttached);
        Assert.Same(EmptyGeometryBufferModel.Empty, buffer);
        var pass = catalog.Resolve(node);
        var shadow = catalog.ResolveShadow(node);
        var ssao = catalog.ResolveSsaoGeometry(node);
        var weighted = catalog.ResolveWeightedTransparency(node);
        var peelingInitialization = catalog.ResolveDepthPeelingInitialization(node);
        var peeling = catalog.ResolveDepthPeeling(node);
        Assert.Null(pass);
        Assert.Null(shadow);
        Assert.Null(ssao);
        Assert.Null(weighted);
        Assert.Null(peelingInitialization);
        Assert.Null(peeling);
    }
}
