/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.Wpf.SharpDX.Element3D.Abstract;
using HelixToolkit.Wpf.SharpDX.Extensions;
using Media = System.Windows.Media;

namespace HelixToolkit.Wpf.SharpDX.Element3D;

/// <summary>
/// </summary>
/// <seealso cref="GeometryModel3D" />
public class LineGeometryModel3D : GeometryModel3D {
    protected readonly LineMaterialCore Material = new();

    /// <summary>
    ///     Called when [create scene node].
    /// </summary>
    /// <returns></returns>
    protected override SceneNode OnCreateSceneNode() => new LineNode { Material = Material };

    /// <summary>
    ///     Assigns the default values to core.
    /// </summary>
    /// <param name="core">The core.</param>
    protected override void AssignDefaultValuesToSceneNode(SceneNode core) {
        Material.LineColor = Color.ToColor4();
        Material.Thickness = (float)Thickness;
        Material.Smoothness = (float)Smoothness;
        Material.FixedSize = FixedSize;
        base.AssignDefaultValuesToSceneNode(core);
    }

    #region Dependency Properties

    /// <summary>
    ///     The color property
    /// </summary>
    public static readonly DependencyProperty ColorProperty =
        DependencyProperty.Register("Color",
                                    typeof(Media.Color),
                                    typeof(LineGeometryModel3D),
#if WINUI
                new PropertyMetadata(Microsoft.UI.Colors.Black, (d, e) =>
#else
                                    new PropertyMetadata(Media.Colors.Black,
                                                         (d, e) =>
#endif
                                                         {
                                                             if (d is LineGeometryModel3D model)
                                                                 model.Material.LineColor = ((Media.Color)e.NewValue).ToColor4();
                                                         }));

    /// <summary>
    ///     The thickness property
    /// </summary>
    public static readonly DependencyProperty ThicknessProperty =
        DependencyProperty.Register("Thickness",
                                    typeof(double),
                                    typeof(LineGeometryModel3D),
                                    new PropertyMetadata(1.0,
                                                         (d, e) => {
                                                             if (d is LineGeometryModel3D model)
                                                                 model.Material.Thickness = (float)(double)e.NewValue;
                                                         }));

    /// <summary>
    ///     The smoothness property
    /// </summary>
    public static readonly DependencyProperty SmoothnessProperty =
        DependencyProperty.Register("Smoothness",
                                    typeof(double),
                                    typeof(LineGeometryModel3D),
                                    new PropertyMetadata(0.0,
                                                         (d, e) => {
                                                             if (d is LineGeometryModel3D model)
                                                                 model.Material.Smoothness = (float)(double)e.NewValue;
                                                         }));

    /// <summary>
    ///     The hit test thickness property
    /// </summary>
    public static readonly DependencyProperty HitTestThicknessProperty =
        DependencyProperty.Register("HitTestThickness",
                                    typeof(double),
                                    typeof(LineGeometryModel3D),
                                    new PropertyMetadata(1.0,
                                                         (d, e) => {
                                                             if (d is Element3DCore { SceneNode: LineNode node })
                                                                 node.HitTestThickness = (double)e.NewValue;
                                                         }));


    /// <summary>
    ///     Fixed sized billboard. Default = true.
    ///     <para>When FixedSize = true, the billboard render size will be scale to normalized device coordinates(screen) size</para>
    ///     <para>When FixedSize = false, the billboard render size will be actual size in 3D world space</para>
    /// </summary>
    public static readonly DependencyProperty FixedSizeProperty
        = DependencyProperty.Register("FixedSize",
                                      typeof(bool),
                                      typeof(LineGeometryModel3D),
                                      new PropertyMetadata(true,
                                                           (d, e) => {
                                                               if (d is LineGeometryModel3D model)
                                                                   model.Material.FixedSize = (bool)e.NewValue;
                                                           }));

    /// <summary>
    ///     Gets or sets the color.
    /// </summary>
    /// <value>
    ///     The color.
    /// </value>
    public Media.Color Color {
        get => (Media.Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>
    ///     Gets or sets the thickness.
    /// </summary>
    /// <value>
    ///     The thickness.
    /// </value>
    public double Thickness {
        get => (double)GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    /// <summary>
    ///     Gets or sets the smoothness.
    /// </summary>
    /// <value>
    ///     The smoothness.
    /// </value>
    public double Smoothness {
        get => (double)GetValue(SmoothnessProperty);
        set => SetValue(SmoothnessProperty, value);
    }

    /// <summary>
    ///     Used only for point/line hit test
    /// </summary>
    public double HitTestThickness {
        get => (double)GetValue(HitTestThicknessProperty);
        set => SetValue(HitTestThicknessProperty, value);
    }

    /// <summary>
    ///     Fixed sized billboard. Default = true.
    ///     <para>When FixedSize = true, the billboard render size will be scale to normalized device coordinates(screen) size</para>
    ///     <para>When FixedSize = false, the billboard render size will be actual size in 3D world space</para>
    /// </summary>
    public bool FixedSize {
        get => (bool)GetValue(FixedSizeProperty);
        set => SetValue(FixedSizeProperty, value);
    }

    #endregion
}
