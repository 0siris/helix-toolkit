using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Element3D;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;

namespace CustomShaderDemo;

public class CustomMeshGeometryModel3D : MeshGeometryModel3D {
    public static readonly DependencyProperty HeightScaleProperty = DependencyProperty.Register("HeightScale",
        typeof(double),
        typeof(CustomMeshGeometryModel3D),
        new PropertyMetadata(5.0,
            (d, e) => {
                if (d is Element3D {SceneNode: CustomMeshNode node}) {
                    node.HeightScale = (float) (double) e.NewValue;
                }
            }));

    public double HeightScale {
        set => SetValue(HeightScaleProperty, value);
        get => (double) GetValue(HeightScaleProperty);
    }

    protected override SceneNode OnCreateSceneNode() => new CustomMeshNode();

    protected override void AssignDefaultValuesToSceneNode(SceneNode core) {
        base.AssignDefaultValuesToSceneNode(core);
        if (core is CustomMeshNode node) {
            node.HeightScale = (float) HeightScale;
        }
    }
}