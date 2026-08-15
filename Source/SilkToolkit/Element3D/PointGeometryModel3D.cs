/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Windows;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.Wpf.SharpDX.Element3D.Abstract;
using HelixToolkit.Wpf.SharpDX.Extensions;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

namespace HelixToolkit.Wpf.SharpDX.Element3D;

/// <summary>
/// </summary>
public class PointGeometryModel3D : GeometryModel3D {
    protected readonly PointMaterialCore Material = new();

    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override SceneNode OnCreateSceneNode() => new PointNode { Material = Material };

    /// <summary>
    ///     Assigns the default values to core.
    /// </summary>
    /// <param name="core">The core.</param>
    protected override void AssignDefaultValuesToSceneNode(SceneNode core) {
        Material.Width = (float)Size.Width;
        Material.Height = (float)Size.Height;
        Material.Figure = Figure;
        Material.FigureRatio = (float)FigureRatio;
        Material.PointColor = Color.ToColor4();
        Material.FixedSize = FixedSize;
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
                                                             if (d is PointGeometryModel3D model)
                                                                 model.Material.PointColor = ((Color)e.NewValue).ToColor4();
                                                         }));


    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register("Size",
                                    typeof(Size),
                                    typeof(PointGeometryModel3D),
                                    new PropertyMetadata(new Size(1.0, 1.0),
                                                         (d, e) => {
                                                             if (d is not PointGeometryModel3D model) return;
                                                             var size = (Size)e.NewValue;
                                                             model.Material.Width = (float)size.Width;
                                                             model.Material.Height = (float)size.Height;
                                                         }));

    public static readonly DependencyProperty FigureProperty =
        DependencyProperty.Register("Figure",
                                    typeof(PointFigure),
                                    typeof(PointGeometryModel3D),
                                    new PropertyMetadata(PointFigure.Rect,
                                                         (d, e) => {
                                                             if (d is PointGeometryModel3D model)
                                                                 model.Material.Figure = (PointFigure)e.NewValue;
                                                         }));

    public static readonly DependencyProperty FigureRatioProperty =
        DependencyProperty.Register("FigureRatio",
                                    typeof(double),
                                    typeof(PointGeometryModel3D),
                                    new PropertyMetadata(0.25,
                                                         (d, e) => {
                                                             if (d is PointGeometryModel3D model)
                                                                 model.Material.FigureRatio = (float)(double)e.NewValue;
                                                         }));

    public static readonly DependencyProperty HitTestThicknessProperty =
        DependencyProperty.Register("HitTestThickness",
                                    typeof(double),
                                    typeof(PointGeometryModel3D),
                                    new PropertyMetadata(4.0,
                                                         (d, e) => {
                                                             if (d is PointGeometryModel3D { SceneNode: PointNode node })
                                                                 node.HitTestThickness = (float)(double)e.NewValue;
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
                                                               if (d is PointGeometryModel3D model)
                                                                   model.Material.FixedSize = (bool)e.NewValue;
                                                           }));

    // Using a DependencyProperty as the backing store for EnableColorBlending.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty EnableColorBlendingProperty =
        DependencyProperty.Register("EnableColorBlending",
                                    typeof(bool),
                                    typeof(PointGeometryModel3D),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                             if (d is PointGeometryModel3D model)
                                                                 model.Material.EnableColorBlending = (bool)e.NewValue;
                                                         }));

    // Using a DependencyProperty as the backing store for BlendingFactor.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty BlendingFactorProperty =
        DependencyProperty.Register("BlendingFactor",
                                    typeof(double),
                                    typeof(PointGeometryModel3D),
                                    new PropertyMetadata(0.0,
                                                         (d, e) => {
                                                             if (d is PointGeometryModel3D model)
                                                                 model.Material.BlendingFactor = (float)(double)e.NewValue;
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
