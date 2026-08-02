using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Model;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
/// </summary>
/// <seealso cref="Element3D" />
public class PostEffectMeshXRay : Element3D {
    protected override SceneNode OnCreateSceneNode() {
        return new NodePostEffectXRay();
    }

    /// <summary>
    ///     Assigns the default values to core.
    /// </summary>
    /// <param name="core">The core.</param>
    protected override void AssignDefaultValuesToSceneNode(SceneNode core) {
        base.AssignDefaultValuesToSceneNode(core);
        if (core is NodePostEffectXRay c) {
            c.EffectName = EffectName;
            c.Color = OutlineColor.ToColor4();
            c.OutlineFadingFactor = (float)OutlineFadingFactor;
            c.EnableDoublePass = EnableDoublePass;
        }
    }

    #region Dependency Properties

    /// <summary>
    ///     The effect name property
    /// </summary>
    public static readonly DependencyProperty EffectNameProperty =
        DependencyProperty.Register("EffectName",
                                    typeof(string),
                                    typeof(PostEffectMeshXRay),
                                    new PropertyMetadata(DefaultRenderTechniqueNames.PostEffectMeshXRay,
                                                         (d, e) => {
                                                             ((d as Element3DCore).SceneNode as NodePostEffectXRay)
                                                                 .EffectName = (string)e.NewValue;
                                                         }));

    /// <summary>
    ///     Gets or sets the name of the effect.
    /// </summary>
    /// <value>
    ///     The name of the effect.
    /// </value>
    public string EffectName {
        get => (string)GetValue(EffectNameProperty);
        set => SetValue(EffectNameProperty, value);
    }


    /// <summary>
    ///     The outline color property
    /// </summary>
    public static DependencyProperty OutlineColorProperty = DependencyProperty.Register("OutlineColor",
        typeof(Color),
        typeof(PostEffectMeshXRay),
        new PropertyMetadata(Colors.Blue,
                             (d, e) => {
                                 ((d as Element3DCore).SceneNode as NodePostEffectXRay).Color =
                                     ((Color)e.NewValue).ToColor4();
                             }));

    /// <summary>
    ///     Gets or sets the color of the outline.
    /// </summary>
    /// <value>
    ///     The color of the outline.
    /// </value>
    public Color OutlineColor {
        get => (Color)GetValue(OutlineColorProperty);
        set => SetValue(OutlineColorProperty, value);
    }

    /// <summary>
    ///     The outline fading factor property
    /// </summary>
    public static DependencyProperty OutlineFadingFactorProperty = DependencyProperty.Register("OutlineFadingFactor",
        typeof(double),
        typeof(PostEffectMeshXRay),
        new PropertyMetadata(1.5,
                             (d, e) => {
                                 ((d as Element3DCore).SceneNode as NodePostEffectXRay).OutlineFadingFactor =
                                     (float)(double)e.NewValue;
                             }));

    /// <summary>
    ///     Gets or sets the outline fading factor.
    /// </summary>
    /// <value>
    ///     The outline fading factor.
    /// </value>
    public double OutlineFadingFactor {
        get => (double)GetValue(OutlineFadingFactorProperty);
        set => SetValue(OutlineFadingFactorProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [double pass]. Double pass uses stencil buffer to reduce overlapping
    ///     artifacts
    /// </summary>
    public static readonly DependencyProperty EnableDoublePassProperty =
        DependencyProperty.Register("EnableDoublePass",
                                    typeof(bool),
                                    typeof(PostEffectMeshXRay),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                             ((d as Element3DCore).SceneNode as NodePostEffectXRay)
                                                                 .EnableDoublePass = (bool)e.NewValue;
                                                         }));


    /// <summary>
    ///     Gets or sets a value indicating whether [double pass]. Double pass uses stencil buffer to reduce overlapping
    ///     artifacts
    /// </summary>
    public bool EnableDoublePass {
        get => (bool)GetValue(EnableDoublePassProperty);
        set => SetValue(EnableDoublePassProperty, value);
    }

    #endregion
}
