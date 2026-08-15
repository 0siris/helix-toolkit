using System.Windows;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.Wpf.SharpDX.Element3D.Abstract;

namespace HelixToolkit.Wpf.SharpDX.Element3D;

/// <summary>
/// </summary>
public class BoneSkinMeshGeometryModel3D : MeshGeometryModel3D {
    public static DependencyProperty BoneMatricesProperty = DependencyProperty.Register("BoneMatrices",
        typeof(Matrix[]),
        typeof(BoneSkinMeshGeometryModel3D),
        new PropertyMetadata(BoneMatricesStruct.DefaultBones,
                             (d, e) => {
                                 if (d is Element3DCore { SceneNode: BoneSkinMeshNode node })
                                     node.BoneMatrices = (Matrix[])e.NewValue;
                             }));

    public Matrix[] BoneMatrices {
        get => (Matrix[])GetValue(BoneMatricesProperty);
        set => SetValue(BoneMatricesProperty, value);
    }

    protected override SceneNode OnCreateSceneNode() => new BoneSkinMeshNode();
}
