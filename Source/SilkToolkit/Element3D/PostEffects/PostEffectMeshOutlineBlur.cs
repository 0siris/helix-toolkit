using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Model;
using Color = System.Windows.Media.Color;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
///     Highlight the border of meshes
/// </summary>
/// <seealso cref="Element3D" />
public class PostEffectMeshOutlineBlur : Element3D {
    /// <summary>
    ///     The effect name property
    /// </summary>
    public static readonly DependencyProperty EffectNameProperty =
        DependencyProperty.Register("EffectName",
                                    typeof(string),
                                    typeof(PostEffectMeshOutlineBlur),
                                    new PropertyMetadata(DefaultRenderTechniqueNames.PostEffectMeshOutlineBlur,
                                                         (d, e) => {
                                                             ((d as Element3DCore).SceneNode as
                                                              NodePostEffectMeshOutlineBlur).EffectName =
                                                                 (string) e.NewValue;
                                                         }));

    /// <summary>
    ///     The color property
    /// </summary>
    public static readonly DependencyProperty ColorProperty =
        DependencyProperty.Register("Color",
                                    typeof(Color),
                                    typeof(PostEffectMeshOutlineBlur),
                                    new PropertyMetadata(Color.FromArgb(255, 255, 0, 0),
                                                         (d, e) => {
                                                             ((d as Element3DCore).SceneNode as
                                                              NodePostEffectMeshOutlineBlur).Color =
                                                                 ((Color) e.NewValue).ToColor4();
                                                         }));

    /// <summary>
    ///     The scale x property
    /// </summary>
    public static readonly DependencyProperty ScaleXProperty =
        DependencyProperty.Register("ScaleX",
                                    typeof(double),
                                    typeof(PostEffectMeshOutlineBlur),
                                    new PropertyMetadata(1.0,
                                                         (d, e) => {
                                                             ((d as Element3DCore).SceneNode as
                                                              NodePostEffectMeshOutlineBlur).ScaleX =
                                                                 (float) (double) e.NewValue;
                                                         }));

    /// <summary>
    ///     The scale y property
    /// </summary>
    public static readonly DependencyProperty ScaleYProperty =
        DependencyProperty.Register("ScaleY",
                                    typeof(double),
                                    typeof(PostEffectMeshOutlineBlur),
                                    new PropertyMetadata(1.0,
                                                         (d, e) => {
                                                             ((d as Element3DCore).SceneNode as
                                                              NodePostEffectMeshOutlineBlur).ScaleY =
                                                                 (float) (double) e.NewValue;
                                                         }));

    /// <summary>
    ///     The number of blur pass property
    /// </summary>
    public static readonly DependencyProperty NumberOfBlurPassProperty =
        DependencyProperty.Register("NumberOfBlurPass",
                                    typeof(int),
                                    typeof(PostEffectMeshOutlineBlur),
                                    new PropertyMetadata(1,
                                                         (d, e) => {
                                                             ((d as Element3DCore).SceneNode as
                                                              NodePostEffectMeshOutlineBlur).NumberOfBlurPass =
                                                                 (int) e.NewValue;
                                                         }));

    /// <summary>
    ///     Gets or sets the name of the effect.
    /// </summary>
    /// <value>
    ///     The name of the effect.
    /// </value>
    public string EffectName {
        get => (string) GetValue(EffectNameProperty);
        set => SetValue(EffectNameProperty, value);
    }


    /// <summary>
    ///     Gets or sets the color.
    /// </summary>
    /// <value>
    ///     The color.
    /// </value>
    public Color Color {
        get => (Color) GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }


    /// <summary>
    ///     Gets or sets the scale x.
    /// </summary>
    /// <value>
    ///     The scale x.
    /// </value>
    public double ScaleX {
        get => (double) GetValue(ScaleXProperty);
        set => SetValue(ScaleXProperty, value);
    }

    /// <summary>
    ///     Gets or sets the scale y.
    /// </summary>
    /// <value>
    ///     The scale y.
    /// </value>
    public double ScaleY {
        get => (double) GetValue(ScaleYProperty);
        set => SetValue(ScaleYProperty, value);
    }

    /// <summary>
    ///     Gets or sets the number of blur pass.
    /// </summary>
    /// <value>
    ///     The number of blur pass.
    /// </value>
    public int NumberOfBlurPass {
        get => (int) GetValue(NumberOfBlurPassProperty);
        set => SetValue(NumberOfBlurPassProperty, value);
    }

    protected override SceneNode OnCreateSceneNode() {
        return new NodePostEffectMeshOutlineBlur();
    }

    protected override void AssignDefaultValuesToSceneNode(SceneNode core) {
        base.AssignDefaultValuesToSceneNode(core);
        if (core is NodePostEffectMeshOutlineBlur c) {
            c.EffectName = EffectName;
            c.Color = Color.ToColor4();
            c.ScaleX = (float) ScaleX;
            c.ScaleY = (float) ScaleY;
            c.NumberOfBlurPass = NumberOfBlurPass;
        }
    }
}
