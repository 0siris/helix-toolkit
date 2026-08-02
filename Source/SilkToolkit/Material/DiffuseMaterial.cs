using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.Wpf.SharpDX.Utilities;

namespace HelixToolkit.Wpf.SharpDX;

public class DiffuseMaterial : Material {
    /// <summary>
    ///     The diffuse color property
    /// </summary>
    public static readonly DependencyProperty DiffuseColorProperty =
        DependencyProperty.Register("DiffuseColor",
                                    typeof(Color4),
                                    typeof(DiffuseMaterial),
                                    new PropertyMetadata((Color4)Color.White,
                                                         (d, e) => {
                                                             ((d as Material).Core as DiffuseMaterialCore)
                                                                 .DiffuseColor = (Color4)e.NewValue;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty DiffuseMapProperty =
        DependencyProperty.Register("DiffuseMap",
                                    typeof(TextureModel),
                                    typeof(DiffuseMaterial),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             ((d as Material).Core as DiffuseMaterialCore).DiffuseMap =
                                                                 e.NewValue as TextureModel;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty DiffuseMapSamplerProperty =
        DependencyProperty.Register("DiffuseMapSampler",
                                    typeof(SamplerStateDescription),
                                    typeof(DiffuseMaterial),
                                    new PropertyMetadata(DefaultSamplers.LinearSamplerWrapAni4,
                                                         (d, e) => {
                                                             ((d as Material).Core as DiffuseMaterialCore)
                                                                 .DiffuseMapSampler =
                                                                 (SamplerStateDescription)e.NewValue;
                                                         }));

    /// <summary>
    ///     The uv transform property
    /// </summary>
    public static readonly DependencyProperty UVTransformProperty =
        DependencyProperty.Register("UVTransform",
                                    typeof(UVTransform),
                                    typeof(DiffuseMaterial),
                                    new PropertyMetadata(UVTransform.Identity,
                                                         (d, e) => {
                                                             ((d as Material).Core as DiffuseMaterialCore).UVTransform =
                                                                 (UVTransform)e.NewValue;
                                                         }));

    /// <summary>
    ///     The enable un lit property
    /// </summary>
    public static readonly DependencyProperty EnableUnLitProperty =
        DependencyProperty.Register("EnableUnLit",
                                    typeof(bool),
                                    typeof(DiffuseMaterial),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                             ((d as Material).Core as DiffuseMaterialCore).EnableUnLit =
                                                                 (bool)e.NewValue;
                                                         }));

    public static readonly DependencyProperty EnableFlatShadingProperty =
        DependencyProperty.Register("EnableFlatShading",
                                    typeof(bool),
                                    typeof(DiffuseMaterial),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                             ((d as Material).Core as DiffuseMaterialCore)
                                                                 .EnableFlatShading = (bool)e.NewValue;
                                                         }));

    public static readonly DependencyProperty VertexColorBlendingFactorProperty =
        DependencyProperty.Register("VertexColorBlendingFactor",
                                    typeof(double),
                                    typeof(DiffuseMaterial),
                                    new PropertyMetadata(0.0,
                                                         (d, e) => {
                                                             ((d as Material).Core as DiffuseMaterialCore)
                                                                 .VertexColorBlendingFactor =
                                                                 (float)(double)e.NewValue;
                                                         }));

    public DiffuseMaterial() { }

    public DiffuseMaterial(DiffuseMaterialCore core) : base(core) {
        DiffuseColor = core.DiffuseColor;
        DiffuseMap = core.DiffuseMap;
        UVTransform = core.UVTransform;
        DiffuseMapSampler = core.DiffuseMapSampler;
        EnableUnLit = core.EnableUnLit;
        EnableFlatShading = core.EnableFlatShading;
        VertexColorBlendingFactor = core.VertexColorBlendingFactor;
    }

    /// <summary>
    ///     Gets or sets the diffuse color for the material.
    ///     For details see: http://msdn.microsoft.com/en-us/library/windows/desktop/bb147175(v=vs.85).aspx
    /// </summary>

    [TypeConverter(typeof(Color4Converter))]
    public Color4 DiffuseColor {
        get => (Color4)GetValue(DiffuseColorProperty);
        set => SetValue(DiffuseColorProperty, value);
    }

    /// <summary>
    ///     Gets or sets the diffuse map.
    /// </summary>
    /// <value>
    ///     The diffuse map.
    /// </value>
    public TextureModel DiffuseMap {
        get => (TextureModel)GetValue(DiffuseMapProperty);
        set => SetValue(DiffuseMapProperty, value);
    }

    /// <summary>
    /// </summary>
    public SamplerStateDescription DiffuseMapSampler {
        get => (SamplerStateDescription)GetValue(DiffuseMapSamplerProperty);
        set => SetValue(DiffuseMapSamplerProperty, value);
    }

    /// <summary>
    ///     Gets or sets the texture uv transform.
    /// </summary>
    /// <value>
    ///     The uv transform.
    /// </value>
    public UVTransform UVTransform {
        get => (UVTransform)GetValue(UVTransformProperty);
        set => SetValue(UVTransformProperty, value);
    }


    /// <summary>
    ///     Gets or sets a value indicating whether whether disable lighting. Directly render diffuse color and diffuse map.
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable un lit]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableUnLit {
        get => (bool)GetValue(EnableUnLitProperty);
        set => SetValue(EnableUnLitProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable flat shading].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable flat shading]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableFlatShading {
        get => (bool)GetValue(EnableFlatShadingProperty);
        set => SetValue(EnableFlatShadingProperty, value);
    }

    /// <summary>
    ///     Gets or sets the vertex color blending factor.
    ///     Final Diffuse Color = (1 - VertexColorBlendingFactor) * Diffuse + VertexColorBlendingFactor * Vertex Color
    /// </summary>
    /// <value>
    ///     The vertex color blending factor.
    /// </value>
    public double VertexColorBlendingFactor {
        get => (double)GetValue(VertexColorBlendingFactorProperty);
        set => SetValue(VertexColorBlendingFactorProperty, value);
    }

    protected override MaterialCore OnCreateCore() {
        return new DiffuseMaterialCore {
            DiffuseColor = DiffuseColor,
            DiffuseMap = DiffuseMap,
            UVTransform = UVTransform,
            DiffuseMapSampler = DiffuseMapSampler,
            EnableUnLit = EnableUnLit,
            EnableFlatShading = EnableFlatShading,
            VertexColorBlendingFactor = (float)VertexColorBlendingFactor
        };
    }

    public virtual DiffuseMaterial CloneMaterial() {
        return new DiffuseMaterial {
            DiffuseColor = DiffuseColor,
            DiffuseMap = DiffuseMap,
            DiffuseMapSampler = DiffuseMapSampler,
            UVTransform = UVTransform,
            Name = Name,
            EnableUnLit = EnableUnLit,
            EnableFlatShading = EnableFlatShading,
            VertexColorBlendingFactor = VertexColorBlendingFactor
        };
    }

    protected override Freezable CreateInstanceCore() {
        return CloneMaterial();
    }
}

public class DiffuseMaterialCollection : ObservableCollection<DiffuseMaterial> {
    public DiffuseMaterialCollection() {
        Add(DiffuseMaterials.Black);
        Add(DiffuseMaterials.BlackPlastic);
        Add(DiffuseMaterials.BlackRubber);
        Add(DiffuseMaterials.Blue);
        Add(DiffuseMaterials.LightBlue);
        Add(DiffuseMaterials.SkyBlue);
        Add(DiffuseMaterials.Brass);
        Add(DiffuseMaterials.Bronze);
        Add(DiffuseMaterials.Chrome);
        Add(DiffuseMaterials.Copper);
        Add(DiffuseMaterials.DefaultVRML);
        Add(DiffuseMaterials.Emerald);
        Add(DiffuseMaterials.Glass);
        Add(DiffuseMaterials.Gold);
        Add(DiffuseMaterials.Green);
        Add(DiffuseMaterials.LightGreen);
        Add(DiffuseMaterials.Indigo);
        Add(DiffuseMaterials.Jade);
        Add(DiffuseMaterials.Gray);
        Add(DiffuseMaterials.LightGray);
        Add(DiffuseMaterials.MediumGray);
        Add(DiffuseMaterials.Obsidian);
        Add(DiffuseMaterials.Orange);
        Add(DiffuseMaterials.Pearl);
        Add(DiffuseMaterials.Pewter);
        Add(DiffuseMaterials.PolishedBronze);
        Add(DiffuseMaterials.PolishedCopper);
        Add(DiffuseMaterials.PolishedGold);
        Add(DiffuseMaterials.PolishedSilver);
        Add(DiffuseMaterials.Red);
        Add(DiffuseMaterials.Ruby);
        Add(DiffuseMaterials.Silver);
        Add(DiffuseMaterials.Turquoise);
        Add(DiffuseMaterials.Violet);
        Add(DiffuseMaterials.White);
        Add(DiffuseMaterials.Yellow);
    }
}

public static class DiffuseMaterials {
    static DiffuseMaterials() {
        Materials = [];
    }

    public static DiffuseMaterialCollection Materials { get; }

    // factory
    public static DiffuseMaterial Red =>
        new() {
            Name = "Red",
            DiffuseColor = Color.Red
        };

    public static DiffuseMaterial Blue =>
        new() {
            Name = "Blue",
            DiffuseColor = Color.Blue
        };

    public static DiffuseMaterial LightBlue =>
        new() {
            Name = "LightBlue",
            DiffuseColor = Color.LightBlue
        };

    public static DiffuseMaterial SkyBlue =>
        new() {
            Name = "SkyBlue",
            DiffuseColor = Color.SkyBlue
        };

    public static DiffuseMaterial Green =>
        new() {
            Name = "Green",
            DiffuseColor = Color.Green
        };

    public static DiffuseMaterial LightGreen =>
        new() {
            Name = "LightGreen",
            DiffuseColor = Color.LightGreen
        };

    public static DiffuseMaterial Orange =>
        new() {
            Name = "Orange",
            DiffuseColor = ToColor(0.992157, 0.513726, 0.0)
        };

    public static DiffuseMaterial BlanchedAlmond =>
        new() {
            Name = "BlanchedAlmond",
            DiffuseColor = Color.BlanchedAlmond
        };

    public static DiffuseMaterial Bisque =>
        new() {
            Name = "Bisque",
            DiffuseColor = Color.Bisque
        };

    public static DiffuseMaterial Yellow =>
        new() {
            Name = "Yellow",
            DiffuseColor = ToColor(1.0, 0.964706, 0.0)
        };

    public static DiffuseMaterial Indigo =>
        new() {
            Name = "Indigo",
            DiffuseColor = ToColor(0.0980392, 0.0, 0.458824)
        };

    public static DiffuseMaterial Violet =>
        new() {
            Name = "Violet",
            DiffuseColor = ToColor(0.635294, 0.0, 1.0)
        };

    public static DiffuseMaterial White =>
        new() {
            Name = "White",
            DiffuseColor = ToColor(0.992157, 0.992157, 0.992157)
        };

    public static DiffuseMaterial PureWhite =>
        new() {
            Name = "PureWhite",
            DiffuseColor = ToColor(1, 1, 1)
        };

    public static DiffuseMaterial Black =>
        new() {
            Name = "Black",
            DiffuseColor = ToColor(0.0, 0.0, 0.0)
        };

    public static DiffuseMaterial Gray =>
        new() {
            Name = "Gray",
            DiffuseColor = ToColor(0.254902, 0.254902, 0.254902)
        };

    public static DiffuseMaterial MediumGray =>
        new() {
            Name = "MediumGray",
            DiffuseColor = ToColor(0.454902, 0.454902, 0.454902)
        };

    public static DiffuseMaterial LightGray =>
        new() {
            Name = "LightGray",
            DiffuseColor = ToColor(0.682353, 0.682353, 0.682353)
        };

    // Materials from: http://globe3d.sourceforge.net/g3d_html/gl-materials__ads.htm
    public static DiffuseMaterial Glass =>
        new() {
            Name = "Glass",
            DiffuseColor = ToColor(0.588235, 0.670588, 0.729412)
        };

    public static DiffuseMaterial Brass =>
        new() {
            Name = "Brass",
            DiffuseColor = ToColor(0.780392, 0.568627, 0.113725)
        };

    public static DiffuseMaterial Bronze =>
        new() {
            Name = "Bronze",
            DiffuseColor = ToColor(0.714, 0.4284, 0.18144)
        };

    public static DiffuseMaterial PolishedBronze =>
        new() {
            Name = "PolishedBronze",
            DiffuseColor = ToColor(0.4, 0.2368, 0.1036)
        };

    public static DiffuseMaterial Chrome =>
        new() {
            Name = "Chrome",
            DiffuseColor = ToColor(0.4f, 0.4f, 0.4f)
        };

    public static DiffuseMaterial Copper =>
        new() {
            Name = "Copper",
            DiffuseColor = ToColor(0.7038, 0.27048, 0.0828)
        };

    public static DiffuseMaterial PolishedCopper =>
        new() {
            Name = "PolishedCopper",
            DiffuseColor = ToColor(0.5508, 0.2118, 0.066)
        };

    public static DiffuseMaterial Gold =>
        new() {
            Name = "Gold",
            DiffuseColor = ToColor(0.75164, 0.60648, 0.22648)
        };

    public static DiffuseMaterial PolishedGold =>
        new() {
            Name = "PolishedGold",
            DiffuseColor = ToColor(0.34615, 0.3143, 0.0903)
        };


    public static DiffuseMaterial Pewter =>
        new() {
            Name = "Pewter",
            DiffuseColor = ToColor(0.427451, 0.470588, 0.541176)
        };

    public static DiffuseMaterial Silver =>
        new() {
            Name = "Silver",
            DiffuseColor = ToColor(0.50754, 0.50754, 0.50754)
        };

    public static DiffuseMaterial PolishedSilver =>
        new() {
            Name = "PolishedSilver",
            DiffuseColor = ToColor(0.2775, 0.2775, 0.2775)
        };

    public static DiffuseMaterial Emerald =>
        new() {
            Name = "Emerald",
            DiffuseColor = ToColor(0.07568, 0.61424, 0.07568, 0.55)
        };

    public static DiffuseMaterial Jade =>
        new() {
            Name = "Jade",
            DiffuseColor = ToColor(0.54, 0.89, 0.63, 0.95)
        };

    public static DiffuseMaterial Obsidian =>
        new() {
            Name = "Obsidian",
            DiffuseColor = ToColor(0.18275, 0.17, 0.22525, 0.82)
        };

    public static DiffuseMaterial Pearl =>
        new() {
            Name = "Pearl",
            DiffuseColor = ToColor(1.0, 0.829, 0.829, 0.922)
        };

    public static DiffuseMaterial Ruby =>
        new() {
            Name = "Ruby",
            DiffuseColor = ToColor(0.61424, 0.04136, 0.04136, 0.55)
        };

    public static DiffuseMaterial Turquoise =>
        new() {
            Name = "Turquoise",
            DiffuseColor = ToColor(0.396, 0.74151, 0.69102, 0.8)
        };

    public static DiffuseMaterial BlackPlastic =>
        new() {
            Name = "BlackPlastic",
            DiffuseColor = ToColor(0.01, 0.01, 0.01)
        };

    public static DiffuseMaterial BlackRubber =>
        new() {
            Name = "BlackRubber",
            DiffuseColor = ToColor(0.01, 0.01, 0.01)
        };

    public static DiffuseMaterial DefaultVRML =>
        new() {
            Name = "DefaultVRML",
            DiffuseColor = ToColor(0.8, 0.8, 0.8)
        };

    public static DiffuseMaterial GetMaterial(string materialName) {
        var mat = Materials.FirstOrDefault(x => x.Name == materialName);
        return mat ?? DefaultVRML;
    }

    public static Color4 ToColor(double r, double g, double b, double a = 1.0) {
        return FromScRgb((float)a, (float)r, (float)g, (float)b);
    }

    /// <summary>
    ///     FromScRgb
    /// </summary>
    public static Color FromScRgb(float a, float r, float g, float b) {
        var c1 = new Color();
        if (a < 0.0f)
            a = 0.0f;
        else if (a > 1.0f) a = 1.0f;

        c1.A = (byte)(a * 255.0f + 0.5f);
        c1.R = ScRgbTosRgb(r);
        c1.G = ScRgbTosRgb(g);
        c1.B = ScRgbTosRgb(b);
        return c1;
    }

    /// <summary>
    ///     private helper function to set context values from a color value with a set context and ScRgb values
    /// </summary>
    private static byte ScRgbTosRgb(float val) {
        if (!(val > 0.0)) // Handles NaN case too
            return 0;

        if (val <= 0.0031308) return (byte)(255.0f * val * 12.92f + 0.5f);

        if (val < 1.0) return (byte)(255.0f * (1.055f * (float)Math.Pow(val, 1.0 / 2.4) - 0.055f) + 0.5f);

        return 255;
    }
}
