using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.SharpDX.Core.Cameras;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX;
using Xunit;
using Binding = System.Windows.Data.Binding;
using DiffuseMaterial = HelixToolkit.Wpf.SharpDX.DiffuseMaterial;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;

namespace SilkToolkit.Tests;
[Collection(WpfCollection.Name)]
public sealed class WpfContractTests {
    [Fact]
    [Trait("Category", "Wpf")]
    public Task CameraDependencyPropertiesUpdateCore() {
        return StaThread.RunAsync(() => {
            var camera = new PerspectiveCamera();
            _ = camera.CameraInternal;

            camera.FieldOfView = 61;
            camera.NearPlaneDistance = 0.25;
            camera.Position = new Point3D(1, 2, 3);

            var core = Assert.IsType<PerspectiveCameraCore>(camera.CameraInternal);
            Assert.Equal(61f, core.FieldOfView);
            Assert.Equal(0.25f, core.NearPlaneDistance);
            Assert.Equal(1f, core.Position.X);
            Assert.Equal(2f, core.Position.Y);
            Assert.Equal(3f, core.Position.Z);
        });
    }

    [Fact]
    [Trait("Category", "Wpf")]
    public Task CameraPropertySupportsBinding() {
        return StaThread.RunAsync(() => {
            var source = new FieldOfViewSource { Value = 72 };
            var camera = new PerspectiveCamera();

            BindingOperations.SetBinding(camera,
                                         PerspectiveCamera.FieldOfViewProperty,
                                         new Binding(nameof(FieldOfViewSource.Value)) { Source = source });

            Assert.Equal(72, camera.FieldOfView);
            Assert.Equal(72f, Assert.IsType<PerspectiveCameraCore>(camera.CameraInternal).FieldOfView);
        });
    }

    [Fact]
    [Trait("Category", "Wpf")]
    public Task MaterialCloneCopiesValuesWithoutSharingTheWrapper() {
        return StaThread.RunAsync(() => {
            var material = new DiffuseMaterial {
                Name = "test",
                DiffuseColor = Colors.CornflowerBlue.ToColor4(),
                EnableFlatShading = true,
                EnableUnLit = true,
                VertexColorBlendingFactor = 0.35
            };

            var clone = Assert.IsType<DiffuseMaterial>(material.Clone());

            Assert.NotSame(material, clone);
            Assert.Equal(material.Name, clone.Name);
            Assert.Equal(material.DiffuseColor, clone.DiffuseColor);
            Assert.Equal(material.EnableFlatShading, clone.EnableFlatShading);
            Assert.Equal(material.EnableUnLit, clone.EnableUnLit);
            Assert.Equal(material.VertexColorBlendingFactor, clone.VertexColorBlendingFactor);
        });
    }

    [Fact]
    [Trait("Category", "Wpf")]
    public Task ElementPropertiesForwardToSceneNode() {
        return StaThread.RunAsync(() => {
            using var model = new MeshGeometryModel3D();
            var node = Assert.IsType<MeshNode>(model.SceneNode);
            var material = DiffuseMaterials.Red;

            model.Material = material;
            model.IsTransparent = true;
            model.IsHitTestVisible = false;
            model.IsRendering = false;
            model.RenderWireframe = true;
            model.WireframeColor = Colors.Orange;

            Assert.Same(material.Core, node.Material);
            Assert.True(node.IsTransparent);
            Assert.False(node.IsHitTestVisible);
            Assert.False(node.Visible);
            Assert.True(node.RenderWireframe);
            Assert.Equal(Colors.Orange.ToColor4(), node.WireframeColor);
        });
    }

    [Fact]
    [Trait("Category", "Wpf")]
    public Task DirectionalLightPropertiesForwardToSceneNode() {
        return StaThread.RunAsync(() => {
            using var light = new DirectionalLight3D();
            var node = Assert.IsType<DirectionalLightNode>(light.SceneNode);

            light.Direction = new Vector3D(1, -2, 3);
            light.Color = Colors.Gold;

            Assert.Equal(1f, node.Direction.X);
            Assert.Equal(-2f, node.Direction.Y);
            Assert.Equal(3f, node.Direction.Z);
            Assert.Equal(Colors.Gold.ToColor4(), node.Color);
        });
    }

    private sealed class FieldOfViewSource : DependencyObject {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(FieldOfViewSource));

        public double Value {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
    }
}
