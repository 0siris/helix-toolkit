using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.Wpf.SharpDX.Element3D.Abstract;

namespace HelixToolkit.Wpf.SharpDX.Element3D;

/// <summary>
/// </summary>
public class EnvironmentMap3D : Model.Elements3D.AbstractElements3D.Element3D {
    /// <summary>
    ///     The texture property
    /// </summary>
    public static readonly DependencyProperty TextureProperty = DependencyProperty.Register("Texture",
        typeof(TextureModel),
        typeof(EnvironmentMap3D),
        new PropertyMetadata(null,
                             (d, e) => {
                                 if (d is Element3DCore { SceneNode: EnvironmentMapNode node })
                                     node.Texture = e.NewValue as TextureModel;
                             }));

    public static readonly DependencyProperty SkipRenderingProperty = DependencyProperty.Register("SkipRendering",
        typeof(bool),
        typeof(EnvironmentMap3D),
        new PropertyMetadata(false,
                             (d, e) => {
                                 if (d is Element3DCore { SceneNode: EnvironmentMapNode node })
                                     node.SkipRendering = (bool)e.NewValue;
                             }));

    /// <summary>
    ///     Gets or sets the texture.
    /// </summary>
    /// <value>
    ///     The texture.
    /// </value>
    public TextureModel? Texture {
        get => (TextureModel)GetValue(TextureProperty);
        set => SetValue(TextureProperty, value);
    }

    /// <summary>
    ///     Skip environment map rendering, but still keep it available for other object to use.
    /// </summary>
    public bool SkipRendering {
        get => (bool)GetValue(SkipRenderingProperty);
        set => SetValue(SkipRenderingProperty, value);
    }

    /// <summary>
    ///     Called when [create scene node].
    /// </summary>
    /// <returns></returns>
    protected override SceneNode OnCreateSceneNode() => new EnvironmentMapNode();

    /// <summary>
    ///     Assigns the default values to scene node.
    /// </summary>
    /// <param name="core">The core.</param>
    protected override void AssignDefaultValuesToSceneNode(SceneNode core) {
        base.AssignDefaultValuesToSceneNode(core);
        if (SceneNode is EnvironmentMapNode node) node.Texture = Texture;
    }
}
