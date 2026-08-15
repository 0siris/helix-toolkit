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
public class PostEffectMeshXRayGrid : Element3D {
    protected override SceneNode OnCreateSceneNode() => new NodePostEffectXRayGrid();

    /// <summary>
    ///     Assigns the default values to core.
    /// </summary>
    /// <param name="core">The core.</param>
    protected override void AssignDefaultValuesToSceneNode(SceneNode core) {
        base.AssignDefaultValuesToSceneNode(core);
        if (core is NodePostEffectXRayGrid c) {
            c.EffectName = EffectName;
            c.Color = GridColor.ToColor4();
            c.GridDensity = GridDensity;
            c.DimmingFactor = (float)DimmingFactor;
            c.BlendingFactor = (float)BlendingFactor;
        }
    }

    #region Dependency Properties

    /// <summary>
    ///     The effect name property
    /// </summary>
    public static readonly DependencyProperty EffectNameProperty =
        DependencyProperty.Register("EffectName",
                                    typeof(string),
                                    typeof(PostEffectMeshXRayGrid),
                                    new PropertyMetadata(DefaultRenderTechniqueNames.PostEffectMeshXRayGrid,
                                                         (d, e) => {
                                                             if (d is Element3DCore { SceneNode: NodePostEffectXRayGrid node })
                                                                 node.EffectName = (string)e.NewValue;
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
    public static DependencyProperty GridColorProperty = DependencyProperty.Register("GridColor",
        typeof(Color),
        typeof(PostEffectMeshXRayGrid),
        new PropertyMetadata(Colors.DarkBlue,
                             (d, e) => {
                                 if (d is Element3DCore { SceneNode: NodePostEffectXRayGrid node })
                                     node.Color = ((Color)e.NewValue).ToColor4();
                             }));

    /// <summary>
    ///     Gets or sets the color of the outline.
    /// </summary>
    /// <value>
    ///     The color of the outline.
    /// </value>
    public Color GridColor {
        get => (Color)GetValue(GridColorProperty);
        set => SetValue(GridColorProperty, value);
    }

    /// <summary>
    ///     Gets or sets the grid density.
    /// </summary>
    /// <value>
    ///     The grid density.
    /// </value>
    public int GridDensity {
        get => (int)GetValue(GridDensityProperty);
        set => SetValue(GridDensityProperty, value);
    }

    /// <summary>
    ///     The grid density property
    /// </summary>
    public static readonly DependencyProperty GridDensityProperty =
        DependencyProperty.Register("GridDensity",
                                    typeof(int),
                                    typeof(PostEffectMeshXRayGrid),
                                    new PropertyMetadata(8,
                                                         (d, e) => {
                                                             if (d is Element3DCore { SceneNode: NodePostEffectXRayGrid node })
                                                                 node.GridDensity = (int)e.NewValue;
                                                         }));

    /// <summary>
    ///     Gets or sets the dimming factor.
    /// </summary>
    /// <value>
    ///     The dimming factor.
    /// </value>
    public double DimmingFactor {
        get => (double)GetValue(DimmingFactorProperty);
        set => SetValue(DimmingFactorProperty, value);
    }

    /// <summary>
    ///     The dimming factor property
    /// </summary>
    public static readonly DependencyProperty DimmingFactorProperty =
        DependencyProperty.Register("DimmingFactor",
                                    typeof(double),
                                    typeof(PostEffectMeshXRayGrid),
                                    new PropertyMetadata(0.8,
                                                         (d, e) => {
                                                             if (d is Element3DCore { SceneNode: NodePostEffectXRayGrid node })
                                                                 node.DimmingFactor = (float)(double)e.NewValue;
                                                         }));


    /// <summary>
    ///     Gets or sets the blending factor for grid and original mesh color blending
    /// </summary>
    /// <value>
    ///     The blending factor.
    /// </value>
    public double BlendingFactor {
        get => (double)GetValue(BlendingFactorProperty);
        set => SetValue(BlendingFactorProperty, value);
    }

    /// <summary>
    ///     The blending factor property
    /// </summary>
    public static readonly DependencyProperty BlendingFactorProperty =
        DependencyProperty.Register("BlendingFactor",
                                    typeof(double),
                                    typeof(PostEffectMeshXRayGrid),
                                    new PropertyMetadata(1.0,
                                                         (d, e) => {
                                                             if (d is Element3DCore { SceneNode: NodePostEffectXRayGrid node })
                                                                 node.BlendingFactor = (float)(double)e.NewValue;
                                                         }));

    #endregion
}
