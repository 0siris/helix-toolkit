/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
/// </summary>
public class PointGeometryModel3D : GeometryModel3D {
    protected readonly PointMaterialCore material = new();

    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override SceneNode OnCreateSceneNode() {
        return new PointNode { Material = material };
    }

    /// <summary>
    ///     Assigns the default values to core.
    /// </summary>
    /// <param name="core">The core.</param>
    protected override void AssignDefaultValuesToSceneNode(SceneNode core) {
        material.Width = (float)Size.Width;
        material.Height = (float)Size.Height;
        material.Figure = Figure;
        material.FigureRatio = (float)FigureRatio;
        material.PointColor = Color.ToColor4();
        material.FixedSize = FixedSize;
        base.AssignDefaultValuesToSceneNode(core);
    }

    #region Dependency Properties

    public static readonly DependencyProperty ColorProperty =
        DependencyProperty.Register("Color",
                                    typeof(Color),
                                    typeof(PointGeometryModel3D),
#if WINUI
                new PropertyMetadata(Microsoft.UI.Colors.Black, (d, e) =>
#else
                                    new PropertyMetadata(Colors.Black,
                                                         (d, e) =>
#endif
                                                         {
                                                             (d as PointGeometryModel3D).material.PointColor =
                                                                 ((Color)e.NewValue).ToColor4();
                                                         }));


    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register("Size",
                                    typeof(Size),
                                    typeof(PointGeometryModel3D),
                                    new PropertyMetadata(new Size(1.0, 1.0),
                                                         (d, e) => {
                                                             var size = (Size)e.NewValue;
                                                             (d as PointGeometryModel3D).material.Width =
                                                                 (float)size.Width;
                                                             (d as PointGeometryModel3D).material.Height =
                                                                 (float)size.Height;
                                                         }));

    public static readonly DependencyProperty FigureProperty =
        DependencyProperty.Register("Figure",
                                    typeof(PointFigure),
                                    typeof(PointGeometryModel3D),
                                    new PropertyMetadata(PointFigure.Rect,
                                                         (d, e) => {
                                                             (d as PointGeometryModel3D).material.Figure =
                                                                 (PointFigure)e.NewValue;
                                                         }));

    public static readonly DependencyProperty FigureRatioProperty =
        DependencyProperty.Register("FigureRatio",
                                    typeof(double),
                                    typeof(PointGeometryModel3D),
                                    new PropertyMetadata(0.25,
                                                         (d, e) => {
                                                             (d as PointGeometryModel3D).material.FigureRatio =
                                                                 (float)(double)e.NewValue;
                                                         }));

    public static readonly DependencyProperty HitTestThicknessProperty =
        DependencyProperty.Register("HitTestThickness",
                                    typeof(double),
                                    typeof(PointGeometryModel3D),
                                    new PropertyMetadata(4.0,
                                                         (d, e) => {
                                                             ((d as PointGeometryModel3D).SceneNode as PointNode)
                                                                 .HitTestThickness = (float)(double)e.NewValue;
                                                         }));

    /// <summary>
    ///     Fixed sized. Default = true.
    ///     <para>When FixedSize = true, the render size will be scale to normalized device coordinates(screen) size</para>
    ///     <para>When FixedSize = false, the render size will be actual size in 3D world space</para>
    /// </summary>
    public static readonly DependencyProperty FixedSizeProperty
        = DependencyProperty.Register("FixedSize",
                                      typeof(bool),
                                      typeof(PointGeometryModel3D),
                                      new PropertyMetadata(true,
                                                           (d, e) => {
                                                               (d as PointGeometryModel3D).material.FixedSize =
                                                                   (bool)e.NewValue;
                                                           }));

    // Using a DependencyProperty as the backing store for EnableColorBlending.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty EnableColorBlendingProperty =
        DependencyProperty.Register("EnableColorBlending",
                                    typeof(bool),
                                    typeof(PointGeometryModel3D),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                             (d as PointGeometryModel3D).material.EnableColorBlending =
                                                                 (bool)e.NewValue;
                                                         }));

    // Using a DependencyProperty as the backing store for BlendingFactor.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty BlendingFactorProperty =
        DependencyProperty.Register("BlendingFactor",
                                    typeof(double),
                                    typeof(PointGeometryModel3D),
                                    new PropertyMetadata(0.0,
                                                         (d, e) => {
                                                             (d as PointGeometryModel3D).material.BlendingFactor =
                                                                 (float)(double)e.NewValue;
                                                         }));

    public Color Color {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public Size Size {
        get => (Size)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public PointFigure Figure {
        get => (PointFigure)GetValue(FigureProperty);
        set => SetValue(FigureProperty, value);
    }

    public double FigureRatio {
        get => (double)GetValue(FigureRatioProperty);
        set => SetValue(FigureRatioProperty, value);
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

    /// <summary>
    ///     Gets or sets a value indicating whether [enable color blending].
    ///     <para>
    ///         Once enabled, final color
    ///         = <see cref="BlendingFactor" /> * <see cref="Color" /> + (1 - <see cref="BlendingFactor" />) * Vertex Color.
    ///     </para>
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable color blending]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableColorBlending {
        get => (bool)GetValue(EnableColorBlendingProperty);
        set => SetValue(EnableColorBlendingProperty, value);
    }

    /// <summary>
    ///     Gets or sets the blending factor.
    ///     <para>Used when <see cref="EnableColorBlending" /> = true.</para>
    /// </summary>
    /// <value>
    ///     The blending factor.
    /// </value>
    public double BlendingFactor {
        get => (double)GetValue(BlendingFactorProperty);
        set => SetValue(BlendingFactorProperty, value);
    }

    #endregion
}
