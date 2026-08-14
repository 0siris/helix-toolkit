/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

#pragma warning disable CS8601, CS8602 // WPF invokes dependency-property callbacks with the owning material and initialized core.

namespace HelixToolkit.Wpf.SharpDX;

public class PointMaterial : Material {
    public PointMaterial() { }

    public PointMaterial(PointMaterialCore core) : base(core) {
        Color = core.PointColor.ToColor();
        Size = new Size(core.Width, core.Height);
        Figure = core.Figure;
        FigureRatio = core.FigureRatio;
        Name = core.Name;
        EnableDistanceFading = core.EnableDistanceFading;
        FadingNearDistance = core.FadingNearDistance;
        FadingFarDistance = core.FadingFarDistance;
    }

    protected override MaterialCore OnCreateCore() => new PointMaterialCore {
        PointColor = Color.ToColor4(),
        Width = (float)Size.Width,
        Height = (float)Size.Height,
        Figure = Figure,
        FigureRatio = (float)FigureRatio,
        Name = Name,
        EnableDistanceFading = EnableDistanceFading,
        FadingNearDistance = (float)FadingNearDistance,
        FadingFarDistance = (float)FadingFarDistance
    };

    protected override Freezable CreateInstanceCore() => new PointMaterial {
        Name = Name
    };

    #region Dependency Properties

    public static readonly DependencyProperty ColorProperty =
        DependencyProperty.Register("Color",
                                    typeof(Color),
                                    typeof(PointMaterial),
#if WINUI
                new PropertyMetadata(Microsoft.UI.Colors.Black, (d, e) =>
#else
                                    new PropertyMetadata(Colors.Black,
                                                         (d, e) =>
#endif
                                                         {
                                                             ((d as PointMaterial).Core as PointMaterialCore)
                                                                 .PointColor = ((Color)e.NewValue).ToColor4();
                                                         }));

    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register("Size",
                                    typeof(Size),
                                    typeof(PointMaterial),
                                    new PropertyMetadata(new Size(1.0, 1.0),
                                                         (d, e) => {
                                                             var size = (Size)e.NewValue;
                                                             ((d as PointMaterial).Core as PointMaterialCore).Width =
                                                                 (float)size.Width;
                                                             ((d as PointMaterial).Core as PointMaterialCore).Height =
                                                                 (float)size.Height;
                                                         }));

    public static readonly DependencyProperty FigureProperty =
        DependencyProperty.Register("Figure",
                                    typeof(PointFigure),
                                    typeof(PointMaterial),
                                    new PropertyMetadata(PointFigure.Rect,
                                                         (d, e) => {
                                                             ((d as PointMaterial).Core as PointMaterialCore).Figure =
                                                                 (PointFigure)e.NewValue;
                                                         }));

    public static readonly DependencyProperty FigureRatioProperty =
        DependencyProperty.Register("FigureRatio",
                                    typeof(double),
                                    typeof(PointMaterial),
                                    new PropertyMetadata(0.25,
                                                         (d, e) => {
                                                             ((d as PointMaterial).Core as PointMaterialCore)
                                                                 .FigureRatio = (float)(double)e.NewValue;
                                                         }));

    public static readonly DependencyProperty EnableDistanceFadingProperty =
        DependencyProperty.Register("EnableDistanceFading",
                                    typeof(bool),
                                    typeof(PointMaterial),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                             ((d as PointMaterial).Core as PointMaterialCore)
                                                                 .EnableDistanceFading = (bool)e.NewValue;
                                                         }));

    public static readonly DependencyProperty FadingNearDistanceProperty =
        DependencyProperty.Register("FadingNearDistance",
                                    typeof(double),
                                    typeof(PointMaterial),
                                    new PropertyMetadata(0.0,
                                                         (d, e) => {
                                                             ((d as PointMaterial).Core as PointMaterialCore)
                                                                 .FadingNearDistance = (float)(double)e.NewValue;
                                                         }));

    public static readonly DependencyProperty FadingFarDistanceProperty =
        DependencyProperty.Register("FadingFarDistance",
                                    typeof(double),
                                    typeof(PointMaterial),
                                    new PropertyMetadata(100.0,
                                                         (d, e) => {
                                                             ((d as PointMaterial).Core as PointMaterialCore)
                                                                 .FadingFarDistance = (float)(double)e.NewValue;
                                                         }));

    // Using a DependencyProperty as the backing store for EnableColorBlending.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty EnableColorBlendingProperty =
        DependencyProperty.Register("EnableColorBlending",
                                    typeof(bool),
                                    typeof(PointMaterial),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                             ((d as PointMaterial).Core as PointMaterialCore)
                                                                 .EnableColorBlending = (bool)e.NewValue;
                                                         }));

    // Using a DependencyProperty as the backing store for BlendingFactor.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty BlendingFactorProperty =
        DependencyProperty.Register("BlendingFactor",
                                    typeof(double),
                                    typeof(PointMaterial),
                                    new PropertyMetadata(0.0,
                                                         (d, e) => {
                                                             ((d as PointMaterial).Core as PointMaterialCore)
                                                                 .BlendingFactor = (float)(double)e.NewValue;
                                                         }));


    /// <summary>
    ///     Gets or sets the point color.
    /// </summary>
    /// <value>
    ///     The color.
    /// </value>
    public Color Color {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>
    ///     Gets or sets the size.
    /// </summary>
    /// <value>
    ///     The size.
    /// </value>
    public Size Size {
        get => (Size)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>
    ///     Gets or sets the figure.
    /// </summary>
    /// <value>
    ///     The figure.
    /// </value>
    public PointFigure Figure {
        get => (PointFigure)GetValue(FigureProperty);
        set => SetValue(FigureProperty, value);
    }

    /// <summary>
    ///     Gets or sets the figure ratio.
    /// </summary>
    /// <value>
    ///     The figure ratio.
    /// </value>
    public double FigureRatio {
        get => (double)GetValue(FigureRatioProperty);
        set => SetValue(FigureRatioProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable distance fading].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable distance fading]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableDistanceFading {
        get => (bool)GetValue(EnableDistanceFadingProperty);
        set => SetValue(EnableDistanceFadingProperty, value);
    }

    /// <summary>
    ///     Gets or sets the fading near distance.
    /// </summary>
    /// <value>
    ///     The fading near distance.
    /// </value>
    public double FadingNearDistance {
        get => (double)GetValue(FadingNearDistanceProperty);
        set => SetValue(FadingNearDistanceProperty, value);
    }

    /// <summary>
    ///     Gets or sets the fading far distance.
    /// </summary>
    /// <value>
    ///     The fading far distance.
    /// </value>
    public double FadingFarDistance {
        get => (double)GetValue(FadingFarDistanceProperty);
        set => SetValue(FadingFarDistanceProperty, value);
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
