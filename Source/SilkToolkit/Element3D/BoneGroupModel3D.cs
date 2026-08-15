using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D;

namespace HelixToolkit.Wpf.SharpDX.Element3D;

/// <summary>
///     Used to share bone matrices for multiple <see cref="BoneSkinMeshGeometryModel3D" />
/// </summary>
public sealed class BoneGroupModel3D : GroupModel3D {
    /// <summary>
    ///     The bone matrices property
    /// </summary>
    public static readonly DependencyProperty BoneMatricesProperty =
        DependencyProperty.Register("BoneMatrices",
                                    typeof(Matrix[]),
                                    typeof(BoneGroupModel3D),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             if (d is Model.Elements3D.AbstractElements3D.Element3D { SceneNode: BoneGroupNode node })
                                                                 node.BoneMatrices = (Matrix[])e.NewValue;
                                                         }));

    /// <summary>
    ///     Gets or sets the bone matrices.
    /// </summary>
    /// <value>
    ///     The bone matrices.
    /// </value>
    public Matrix[] BoneMatrices {
        get => (Matrix[])GetValue(BoneMatricesProperty);
        set => SetValue(BoneMatricesProperty, value);
    }

    protected override SceneNode OnCreateSceneNode() => new BoneGroupNode();
}
