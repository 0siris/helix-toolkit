using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Model;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
/// </summary>
public class EnvironmentMap3D : Element3D
{
    /// <summary>
    ///     The texture property
    /// </summary>
    public static readonly DependencyProperty TextureProperty = DependencyProperty.Register("Texture",
        typeof(TextureModel), typeof(EnvironmentMap3D),
        new PropertyMetadata(null,
            (d, e) => { ((d as Element3DCore).SceneNode as EnvironmentMapNode).Texture = (TextureModel) e.NewValue; }));

    public static readonly DependencyProperty SkipRenderingProperty = DependencyProperty.Register("SkipRendering",
        typeof(bool), typeof(EnvironmentMap3D),
        new PropertyMetadata(false,
            (d, e) => { ((d as Element3DCore).SceneNode as EnvironmentMapNode).SkipRendering = (bool) e.NewValue; }));

    /// <summary>
    ///     Gets or sets the texture.
    /// </summary>
    /// <value>
    ///     The texture.
    /// </value>
    public TextureModel Texture
    {
        get => (TextureModel) GetValue(TextureProperty);
        set => SetValue(TextureProperty, value);
    }

    /// <summary>
    ///     Skip environment map rendering, but still keep it available for other object to use.
    /// </summary>
    public bool SkipRendering
    {
        get => (bool) GetValue(SkipRenderingProperty);
        set => SetValue(SkipRenderingProperty, value);
    }

    /// <summary>
    ///     Called when [create scene node].
    /// </summary>
    /// <returns></returns>
    protected override SceneNode OnCreateSceneNode()
    {
        return new EnvironmentMapNode();
    }

    /// <summary>
    ///     Assigns the default values to scene node.
    /// </summary>
    /// <param name="core">The core.</param>
    protected override void AssignDefaultValuesToSceneNode(SceneNode core)
    {
        base.AssignDefaultValuesToSceneNode(core);
        (SceneNode as EnvironmentMapNode).Texture = Texture;
    }
}