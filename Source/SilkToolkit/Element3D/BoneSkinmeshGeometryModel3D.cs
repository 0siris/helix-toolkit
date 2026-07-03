using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Model;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
/// </summary>
public class BoneSkinMeshGeometryModel3D : MeshGeometryModel3D {
    public static DependencyProperty BoneMatricesProperty = DependencyProperty.Register("BoneMatrices",
        typeof(Matrix[]),
        typeof(BoneSkinMeshGeometryModel3D),
        new PropertyMetadata(BoneMatricesStruct.DefaultBones,
                             (d, e) => {
                                 ((d as Element3DCore).SceneNode as BoneSkinMeshNode).BoneMatrices =
                                     (Matrix[]) e.NewValue;
                             }));

    public Matrix[] BoneMatrices {
        get => (Matrix[]) GetValue(BoneMatricesProperty);
        set => SetValue(BoneMatricesProperty, value);
    }

    protected override SceneNode OnCreateSceneNode() {
        return new BoneSkinMeshNode();
    }
}
