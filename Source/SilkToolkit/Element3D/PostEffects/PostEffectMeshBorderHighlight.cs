using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
///     Highlight the border of meshes
/// </summary>
public class PostEffectMeshBorderHighlight : PostEffectMeshOutlineBlur
{
    /// <summary>
    ///     The draw mode property
    /// </summary>
    public static readonly DependencyProperty DrawModeProperty =
        DependencyProperty.Register("DrawMode", typeof(OutlineMode), typeof(PostEffectMeshBorderHighlight),
            new PropertyMetadata(OutlineMode.Merged,
                (d, e) =>
                {
                    ((d as Element3D).SceneNode as NodePostEffectBorderHighlight).DrawMode = (OutlineMode) e.NewValue;
                }));

    /// <summary>
    ///     Gets or sets the draw mode.
    /// </summary>
    /// <value>
    ///     The draw mode.
    /// </value>
    public OutlineMode DrawMode
    {
        get => (OutlineMode) GetValue(DrawModeProperty);
        set => SetValue(DrawModeProperty, value);
    }


    protected override SceneNode OnCreateSceneNode()
    {
        return new NodePostEffectBorderHighlight();
    }
}