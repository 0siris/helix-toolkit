using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.Wpf.SharpDX.Utilities;
using Color = HelixToolkit.SharpDX.Core.Color;

#pragma warning disable CS8601, CS8602 // WPF invokes dependency-property callbacks with the owning material and initialized core.

namespace HelixToolkit.Wpf.SharpDX.Material;

/// <summary>
/// </summary>
[DataContract]
public class ColorStripeMaterial : Material {
    /// <summary>
    ///     The diffuse color property
    /// </summary>
    public static readonly DependencyProperty DiffuseColorProperty =
        DependencyProperty.Register("DiffuseColor",
            typeof(Color4),
            typeof(ColorStripeMaterial),
            new PropertyMetadata((Color4) Color.White,
                (d, e) => {
                    ((d as Material).Core as ColorStripeMaterialCore)
                        .DiffuseColor = (Color4) e.NewValue;
                }));

    /// <summary>
    ///     The color stripe property
    /// </summary>
    public static readonly DependencyProperty ColorStripeXProperty =
        DependencyProperty.Register("ColorStripeX",
            typeof(IList<Color4>),
            typeof(ColorStripeMaterial),
            new PropertyMetadata(null,
                (d, e) => {
                    ((d as Material).Core as ColorStripeMaterialCore)
                        .ColorStripeX = (IList<Color4>) e.NewValue;
                }));

    /// <summary>
    ///     The color stripe property
    /// </summary>
    public static readonly DependencyProperty ColorStripeYProperty =
        DependencyProperty.Register("ColorStripeY",
            typeof(IList<Color4>),
            typeof(ColorStripeMaterial),
            new PropertyMetadata(null,
                (d, e) => {
                    ((d as Material).Core as ColorStripeMaterialCore)
                        .ColorStripeY = (IList<Color4>) e.NewValue;
                }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty ColorStripeSamplerProperty =
        DependencyProperty.Register("ColorStripeSampler",
            typeof(SamplerStateDescription),
            typeof(ColorStripeMaterial),
            new PropertyMetadata(DefaultSamplers.LinearSamplerClampAni1,
                (d, e) => {
                    ((d as Material).Core as ColorStripeMaterialCore)
                        .ColorStripeSampler =
                        (SamplerStateDescription) e.NewValue;
                }));

    /// <summary>
    ///     The color stripe x enabled property
    /// </summary>
    public static readonly DependencyProperty ColorStripeXEnabledProperty =
        DependencyProperty.Register("ColorStripeXEnabled",
            typeof(bool),
            typeof(ColorStripeMaterial),
            new PropertyMetadata(true,
                (d, e) => {
                    ((d as Material).Core as ColorStripeMaterialCore)
                        .ColorStripeXEnabled = (bool) e.NewValue;
                }));

    /// <summary>
    ///     The color stripe y enabled property
    /// </summary>
    public static readonly DependencyProperty ColorStripeYEnabledProperty =
        DependencyProperty.Register("ColorStripeYEnabled",
            typeof(bool),
            typeof(ColorStripeMaterial),
            new PropertyMetadata(true,
                (d, e) => {
                    ((d as Material).Core as ColorStripeMaterialCore)
                        .ColorStripeYEnabled = (bool) e.NewValue;
                }));

    public ColorStripeMaterial() { }

    public ColorStripeMaterial(ColorStripeMaterialCore core) : base(core) {
        DiffuseColor = core.DiffuseColor;
        ColorStripeSampler = core.ColorStripeSampler;
        ColorStripeX = core.ColorStripeX;
        ColorStripeXEnabled = core.ColorStripeXEnabled;
        ColorStripeY = core.ColorStripeY;
        ColorStripeYEnabled = core.ColorStripeYEnabled;
    }

    /// <summary>
    ///     Gets or sets the diffuse color for the material.
    /// </summary>

    [TypeConverter(typeof(Color4Converter))]
    public Color4 DiffuseColor {
        get => (Color4) GetValue(DiffuseColorProperty);
        set => SetValue(DiffuseColorProperty, value);
    }

    /// <summary>
    ///     Gets or sets the color stripe.
    /// </summary>
    /// <value>
    ///     The color stripe.
    /// </value>
    public IList<Color4> ColorStripeX {
        get => (IList<Color4>) GetValue(ColorStripeXProperty);
        set => SetValue(ColorStripeXProperty, value);
    }

    /// <summary>
    ///     Gets or sets the color stripe.
    /// </summary>
    /// <value>
    ///     The color stripe.
    /// </value>
    public IList<Color4> ColorStripeY {
        get => (IList<Color4>) GetValue(ColorStripeYProperty);
        set => SetValue(ColorStripeYProperty, value);
    }


    /// <summary>
    ///     Gets or sets a value indicating whether [color stripe x enabled].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [color stripe x enabled]; otherwise, <c>false</c>.
    /// </value>
    public bool ColorStripeXEnabled {
        get => (bool) GetValue(ColorStripeXEnabledProperty);
        set => SetValue(ColorStripeXEnabledProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [color stripe y enabled].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [color stripe y enabled]; otherwise, <c>false</c>.
    /// </value>
    public bool ColorStripeYEnabled {
        get => (bool) GetValue(ColorStripeYEnabledProperty);
        set => SetValue(ColorStripeYEnabledProperty, value);
    }

    /// <summary>
    /// </summary>
    public SamplerStateDescription ColorStripeSampler {
        get => (SamplerStateDescription) GetValue(ColorStripeSamplerProperty);
        set => SetValue(ColorStripeSamplerProperty, value);
    }

    protected override MaterialCore OnCreateCore() => new ColorStripeMaterialCore {
        DiffuseColor = DiffuseColor,
        ColorStripeSampler = ColorStripeSampler,
        ColorStripeX = ColorStripeX,
        ColorStripeXEnabled = ColorStripeXEnabled,
        ColorStripeY = ColorStripeY,
        ColorStripeYEnabled = ColorStripeYEnabled
    };

    protected override Freezable CreateInstanceCore() => new ColorStripeMaterial {
        DiffuseColor = DiffuseColor,
        ColorStripeSampler = ColorStripeSampler,
        ColorStripeX = ColorStripeX,
        ColorStripeXEnabled = ColorStripeXEnabled,
        ColorStripeY = ColorStripeY,
        ColorStripeYEnabled = ColorStripeYEnabled,
        Name = Name
    };
}

#pragma warning restore CS8601, CS8602