using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Core.Buffers;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX.Element3D;
using HelixToolkit.Wpf.SharpDX.Material;
using Silk.NET.Maths;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>
///     Verifies WPF model data crosses repeated Direct3D 12 resource lifecycles.
/// </summary>
[Collection(WpfCollection.Name)]
public sealed class WarpLifecycleTests {
    /// <summary>
    ///     Verifies one WPF mesh model can create and release repeated WARP buffer sets.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public Task MeshGeometryModelCreatesRepeatedDx12BuffersOnWarp() {
        return StaThread.RunAsync(() => {
            using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110,
                SilkDriverType.Warp);
            using var model = new MeshGeometryModel3D();
            var builder = new MeshBuilder();
            builder.AddTriangle(Vector3D<float>.Zero, Vector3D<float>.UnitX, Vector3D<float>.UnitY);
            model.Geometry = builder.ToMesh();
            model.Material = DiffuseMaterials.Red;
            using var bufferModel = new DefaultMeshGeometryBufferModel {Geometry = model.Geometry};

            using (var first = SilkD3D12DefaultMeshBuffers.Create(device, bufferModel)) {
                Assert.False(first.IsDisposed);
                Assert.Equal((uint) model.Geometry.Indices!.Count, first.IndexCount);
            }
            using var second = SilkD3D12DefaultMeshBuffers.Create(device, bufferModel);
            Assert.False(second.IsDisposed);
            device.ThrowIfDeviceRemoved();
        });
    }

    /// <summary>
    ///     Verifies a native Direct3D 12 hardware device can be created explicitly.
    /// </summary>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    public Task HardwareDeviceSmoke() {
        return StaThread.RunAsync(() => {
            using var device = SilkD3D12DeviceFactory.CreateDefault();
            Assert.NotEqual(nint.Zero, device.NativePointer);
            device.ThrowIfDeviceRemoved();
        });
    }
}
