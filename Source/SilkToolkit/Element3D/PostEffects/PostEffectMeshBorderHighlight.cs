using System.Windows;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Model.Scene.PostEffects;

namespace HelixToolkit.Wpf.SharpDX.Element3D.PostEffects;

/// <summary>
///     Highlight the border of meshes
/// </summary>
public class PostEffectMeshBorderHighlight : PostEffectMeshOutlineBlur {
    /// <summary>
    ///     The draw mode property
    /// </summary>
    public static readonly DependencyProperty DrawModeProperty =
        DependencyProperty.Register("DrawMode",
                                    typeof(OutlineMode),
                                    typeof(PostEffectMeshBorderHighlight),
                                    new PropertyMetadata(OutlineMode.Merged,
                                                         (d, e) => {
                                                             if (d is Model.Elements3D.AbstractElements3D.Element3D { SceneNode: NodePostEffectBorderHighlight node })
                                                                 node.DrawMode = (OutlineMode)e.NewValue;
                                                         }));

    /// <summary>
    ///     Gets or sets the draw mode.
    /// </summary>
    /// <value>
    ///     The draw mode.
    /// </value>
    public OutlineMode DrawMode {
        get => (OutlineMode) GetValue(DrawModeProperty);
        set => SetValue(DrawModeProperty, value);
    }


    protected override SceneNode OnCreateSceneNode() => new NodePostEffectBorderHighlight();
}
