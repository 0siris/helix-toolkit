using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.Wpf.SharpDX.Element3D;
using HelixToolkit.Wpf.SharpDX.Material;
using Xunit;

namespace SilkToolkit.Tests;
[Collection(WpfCollection.Name)]
public sealed class WarpLifecycleTests {
    [Fact]
    [Trait("Category", "Warp")]
    public Task MeshCanAttachDetachAndReattachWithWarp() {
        return StaThread.RunAsync(() => {
            using var effectsManager = new DefaultEffectsManager(new EffectsManagerConfiguration {
                EnableSoftwareRendering = true
            });
            using var model = new MeshGeometryModel3D();
            model.Geometry = new MeshBuilder().ToMesh();
            model.Material = DiffuseMaterials.Red;

            var node = Assert.IsType<MeshNode>(model.SceneNode);
            node.Attach(effectsManager);
            Assert.True(model.IsAttached);
            Assert.Equal(DriverType.Warp, effectsManager.DriverType);

            node.Detach();
            Assert.False(model.IsAttached);

            node.Attach(effectsManager);
            Assert.True(model.IsAttached);
            node.Detach();
        });
    }

    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    public Task HardwareDeviceSmoke() {
        return StaThread.RunAsync(() => {
            using var effectsManager = new DefaultEffectsManager();
            Assert.Equal(DriverType.Hardware, effectsManager.DriverType);
            Assert.True(effectsManager.Initialized);
        });
    }
}
