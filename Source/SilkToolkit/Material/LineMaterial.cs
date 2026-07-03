/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Shaders;
using Media = System.Windows.Media;

namespace HelixToolkit.Wpf.SharpDX;

public class LineMaterial : Material {
    public LineMaterial() { }

    public LineMaterial(LineMaterialCore core) : base(core) {
        Name = core.Name;
        Color = core.LineColor.ToColor();
        Smoothness = core.Smoothness;
        Thickness = core.Thickness;
        EnableDistanceFading = EnableDistanceFading;
        FadingNearDistance = core.FadingNearDistance;
        FadingFarDistance = core.FadingFarDistance;
        Texture = core.Texture;
        TextureScale = core.TextureScale;
        SamplerDescription = core.SamplerDescription;
        FixedSize = core.FixedSize;
    }

    protected override MaterialCore OnCreateCore() {
        return new LineMaterialCore {
            Name = Name,
            LineColor = Color.ToColor4(),
            Smoothness = (float) Smoothness,
            Thickness = (float) Thickness,
            EnableDistanceFading = EnableDistanceFading,
            FadingNearDistance = (float) FadingNearDistance,
            FadingFarDistance = (float) FadingFarDistance,
            Texture = Texture,
            TextureScale = (float) TextureScale,
            SamplerDescription = SamplerDescription,
            FixedSize = FixedSize
        };
    }

#if !NETFX_CORE && !WINUI
    protected override Freezable CreateInstanceCore() {
        return new LineMaterial {
            Name = Name,
            Color = Color,
            Smoothness = Smoothness,
            Thickness = Thickness,
            EnableDistanceFading = EnableDistanceFading,
            FadingNearDistance = FadingNearDistance,
            FadingFarDistance = FadingFarDistance,
            Texture = Texture,
            TextureScale = TextureScale,
            SamplerDescription = SamplerDescription,
            FixedSize = FixedSize
        };
    }
#endif

#region Dependency Properties

    /// <summary>
    ///     The color property
    /// </summary>
    public static readonly DependencyProperty ColorProperty =
        DependencyProperty.Register("Color",
                                    typeof(Media.Color),
                                    typeof(LineMaterial),
#if WINUI
                new PropertyMetadata(Microsoft.UI.Colors.Black, (d, e) =>
#else
                                    new PropertyMetadata(Media.Colors.Black,
                                                         (d, e) =>
#endif
                                                         {
                                                             ((d as LineMaterial).Core as LineMaterialCore).LineColor =
                                                                 ((Media.Color) e.NewValue).ToColor4();
                                                         }));

    /// <summary>
    ///     The thickness property
    /// </summary>
    public static readonly DependencyProperty ThicknessProperty =
        DependencyProperty.Register("Thickness",
                                    typeof(double),
                                    typeof(LineMaterial),
                                    new PropertyMetadata(1.0,
                                                         (d, e) => {
                                                             ((d as LineMaterial).Core as LineMaterialCore).Thickness =
                                                                 (float) (double) e.NewValue;
                                                         }));

    /// <summary>
    ///     The smoothness property
    /// </summary>
    public static readonly DependencyProperty SmoothnessProperty =
        DependencyProperty.Register("Smoothness",
                                    typeof(double),
                                    typeof(LineMaterial),
                                    new PropertyMetadata(0.0,
                                                         (d, e) => {
                                                             ((d as LineMaterial).Core as LineMaterialCore).Smoothness =
                                                                 (float) (double) e.NewValue;
                                                         }));

    public static readonly DependencyProperty EnableDistanceFadingProperty =
        DependencyProperty.Register("EnableDistanceFading",
                                    typeof(bool),
                                    typeof(LineMaterial),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             ((d as LineMaterial).Core as LineMaterialCore)
                                                                 .EnableDistanceFading = (bool) e.NewValue;
                                                         }));

    public static readonly DependencyProperty FadingNearDistanceProperty =
        DependencyProperty.Register("FadingNearDistance",
                                    typeof(double),
                                    typeof(LineMaterial),
                                    new PropertyMetadata(0.0,
                                                         (d, e) => {
                                                             ((d as LineMaterial).Core as LineMaterialCore)
                                                                 .FadingNearDistance = (float) (double) e.NewValue;
                                                         }));

    public static readonly DependencyProperty FadingFarDistanceProperty =
        DependencyProperty.Register("FadingFarDistance",
                                    typeof(double),
                                    typeof(LineMaterial),
                                    new PropertyMetadata(100.0,
                                                         (d, e) => {
                                                             ((d as LineMaterial).Core as LineMaterialCore)
                                                                 .FadingFarDistance = (float) (double) e.NewValue;
                                                         }));

    public static readonly DependencyProperty TextureProperty =
        DependencyProperty.Register("Texture",
                                    typeof(TextureModel),
                                    typeof(LineMaterial),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             ((d as LineMaterial).Core as LineMaterialCore).Texture =
                                                                 e.NewValue == null ? null : (TextureModel) e.NewValue;
                                                         }));


    public static readonly DependencyProperty TextureScaleProperty =
        DependencyProperty.Register("TextureScale",
                                    typeof(double),
                                    typeof(LineMaterial),
                                    new PropertyMetadata(1.0,
                                                         (d, e) => {
                                                             ((d as LineMaterial).Core as LineMaterialCore)
                                                                 .TextureScale = (float) (double) e.NewValue;
                                                         }));


    public static readonly DependencyProperty SamplerDescriptionProperty =
        DependencyProperty.Register("SamplerDescription",
                                    typeof(SamplerStateDescription),
                                    typeof(LineMaterial),
                                    new PropertyMetadata(DefaultSamplers.LineSamplerUWrapVClamp,
                                                         (d, e) => {
                                                             ((d as LineMaterial).Core as LineMaterialCore)
                                                                 .SamplerDescription =
                                                                 (SamplerStateDescription) e.NewValue;
                                                         }));

    public static readonly DependencyProperty FixedSizeProperty =
        DependencyProperty.Register("FixedSize",
                                    typeof(bool),
                                    typeof(LineMaterial),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             ((d as LineMaterial).Core as LineMaterialCore).FixedSize =
                                                                 (bool) e.NewValue;
                                                         }));


    /// <summary>
    ///     Gets or sets the color.
    /// </summary>
    /// <value>
    ///     The color.
    /// </value>
    public Media.Color Color {
        get => (Media.Color) GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>
    ///     Gets or sets the thickness.
    /// </summary>
    /// <value>
    ///     The thickness.
    /// </value>
    public double Thickness {
        get => (double) GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    /// <summary>
    ///     Gets or sets the smoothness.
    /// </summary>
    /// <value>
    ///     The smoothness.
    /// </value>
    public double Smoothness {
        get => (double) GetValue(SmoothnessProperty);
        set => SetValue(SmoothnessProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable distance fading].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable distance fading]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableDistanceFading {
        get => (bool) GetValue(EnableDistanceFadingProperty);
        set => SetValue(EnableDistanceFadingProperty, value);
    }

    /// <summary>
    ///     Gets or sets the fading near distance.
    /// </summary>
    /// <value>
    ///     The fading near distance.
    /// </value>
    public double FadingNearDistance {
        get => (double) GetValue(FadingNearDistanceProperty);
        set => SetValue(FadingNearDistanceProperty, value);
    }

    /// <summary>
    ///     Gets or sets the fading far distance.
    /// </summary>
    /// <value>
    ///     The fading far distance.
    /// </value>
    public double FadingFarDistance {
        get => (double) GetValue(FadingFarDistanceProperty);
        set => SetValue(FadingFarDistanceProperty, value);
    }

    /// <summary>
    ///     Gets or sets the texture.
    /// </summary>
    /// <value>
    ///     The texture.
    /// </value>
    public TextureModel Texture {
        get => (TextureModel) GetValue(TextureProperty);
        set => SetValue(TextureProperty, value);
    }

    /// <summary>
    ///     Gets or sets the texture scale. Scale on texture X length
    /// </summary>
    /// <value>
    ///     The texture scale.
    /// </value>
    public double TextureScale {
        get => (double) GetValue(TextureScaleProperty);
        set => SetValue(TextureScaleProperty, value);
    }

    /// <summary>
    ///     Gets or sets the sampler.
    /// </summary>
    /// <value>
    ///     The sampler.
    /// </value>
    public SamplerStateDescription SamplerDescription {
        get => (SamplerStateDescription) GetValue(SamplerDescriptionProperty);
        set => SetValue(SamplerDescriptionProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [fixed size].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [fixed size]; otherwise, <c>false</c>.
    /// </value>
    public bool FixedSize {
        get => (bool) GetValue(FixedSizeProperty);
        set => SetValue(FixedSizeProperty, value);
    }

#endregion
}
