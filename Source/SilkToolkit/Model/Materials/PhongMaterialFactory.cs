// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PhongMaterialFactory.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//
// </summary>
// --------------------------------------------------------------------------------------------------------------------


using System.Collections.ObjectModel;
using System.Linq;
using Color = HelixToolkit.SharpDX.Core.Color;

namespace HelixToolkit.Wpf.SharpDX;

public class PhongMaterialCollection : ObservableCollection<PhongMaterial> {
    public PhongMaterialCollection() {
        Add(PhongMaterials.Black);
        Add(PhongMaterials.BlackPlastic);
        Add(PhongMaterials.BlackRubber);
        Add(PhongMaterials.Blue);
        Add(PhongMaterials.Brass);
        Add(PhongMaterials.Bronze);
        Add(PhongMaterials.Chrome);
        Add(PhongMaterials.Copper);
        Add(PhongMaterials.DefaultVRML);
        Add(PhongMaterials.Emerald);
        Add(PhongMaterials.Glass);
        Add(PhongMaterials.Gold);
        Add(PhongMaterials.Green);
        Add(PhongMaterials.Indigo);
        Add(PhongMaterials.Jade);
        Add(PhongMaterials.LightGray);
        Add(PhongMaterials.MediumGray);
        Add(PhongMaterials.Obsidian);
        Add(PhongMaterials.Orange);
        Add(PhongMaterials.Pearl);
        Add(PhongMaterials.Pewter);
        Add(PhongMaterials.PolishedBronze);
        Add(PhongMaterials.PolishedCopper);
        Add(PhongMaterials.PolishedGold);
        Add(PhongMaterials.PolishedSilver);
        Add(PhongMaterials.Red);
        Add(PhongMaterials.Ruby);
        Add(PhongMaterials.Silver);
        Add(PhongMaterials.Turquoise);
        Add(PhongMaterials.Violet);
        Add(PhongMaterials.White);
        Add(PhongMaterials.Yellow);
    }
}

/// <summary>
/// </summary>
public static class PhongMaterials {
    static PhongMaterials() {
        Materials = [];
    }

    public static PhongMaterialCollection Materials { get; }

    // factory
    public static PhongMaterial Red =>
        new() {
            Name = "Red",
            AmbientColor = ToColor(0.1, 0.1, 0.1),
            DiffuseColor = Color.Red,
            SpecularColor = ToColor(0.0225, 0.0225, 0.0225),
            EmissiveColor = ToColor(0.0, 0.0, 0.0),
            SpecularShininess = 12.8f
        };

    public static PhongMaterial Blue =>
        new() {
            Name = "Blue",
            AmbientColor = ToColor(0.1, 0.1, 0.1),
            DiffuseColor = Color.Blue,
            SpecularColor = ToColor(0.0225, 0.0225, 0.0225),
            EmissiveColor = ToColor(0.0, 0.0, 0.0),
            SpecularShininess = 12.8f
        };

    public static PhongMaterial Green =>
        new() {
            Name = "Green",
            AmbientColor = ToColor(0.1, 0.1, 0.1),
            DiffuseColor = Color.Green,
            SpecularColor = ToColor(0.0225, 0.0225, 0.0225),
            EmissiveColor = ToColor(0.0, 0.0, 0.0),
            SpecularShininess = 12.8f
        };

    public static PhongMaterial Orange =>
        new() {
            Name = "Orange",
            AmbientColor = ToColor(0.1, 0.1, 0.1),
            DiffuseColor = ToColor(0.992157, 0.513726, 0.0),
            SpecularColor = ToColor(0.0225, 0.0225, 0.0225),
            EmissiveColor = ToColor(0.0, 0.0, 0.0),
            SpecularShininess = 12.8f
        };

    public static PhongMaterial BlanchedAlmond =>
        new() {
            Name = "BlanchedAlmond",
            AmbientColor = ToColor(0.1, 0.1, 0.1),
            DiffuseColor = Color.BlanchedAlmond,
            SpecularColor = ToColor(0.0225, 0.0225, 0.0225),
            EmissiveColor = ToColor(0.0, 0.0, 0.0),
            SpecularShininess = 12.8f
        };

    public static PhongMaterial Bisque =>
        new() {
            Name = "Bisque",
            AmbientColor = ToColor(0.1, 0.1, 0.1),
            DiffuseColor = Color.Bisque,
            SpecularColor = ToColor(0.0225, 0.0225, 0.0225),
            EmissiveColor = ToColor(0.0, 0.0, 0.0),
            SpecularShininess = 12.8f
        };

    public static PhongMaterial Yellow =>
        new() {
            Name = "Yellow",
            AmbientColor = ToColor(0.1, 0.1, 0.1),
            DiffuseColor = ToColor(1.0, 0.964706, 0.0),
            SpecularColor = ToColor(0.0225, 0.0225, 0.0225),
            EmissiveColor = ToColor(0.0, 0.0, 0.0),
            SpecularShininess = 12.8f
        };

    public static PhongMaterial Indigo =>
        new() {
            Name = "Indigo",
            AmbientColor = ToColor(0.1, 0.1, 0.1),
            DiffuseColor = ToColor(0.0980392, 0.0, 0.458824),
            SpecularColor = ToColor(0.0225, 0.0225, 0.0225),
            EmissiveColor = ToColor(0.0, 0.0, 0.0),
            SpecularShininess = 12.8f
        };

    public static PhongMaterial Violet =>
        new() {
            Name = "Violet",
            AmbientColor = ToColor(0.1, 0.1, 0.1),
            DiffuseColor = ToColor(0.635294, 0.0, 1.0),
            SpecularColor = ToColor(0.0225, 0.0225, 0.0225),
            EmissiveColor = ToColor(0.0, 0.0, 0.0),
            SpecularShininess = 12.8f
        };

    public static PhongMaterial White =>
        new() {
            Name = "White",
            AmbientColor = ToColor(0.1, 0.1, 0.1),
            DiffuseColor = ToColor(0.992157, 0.992157, 0.992157),
            SpecularColor = ToColor(0.0225, 0.0225, 0.0225),
            EmissiveColor = ToColor(0.0, 0.0, 0.0),
            SpecularShininess = 12.8f
        };

    public static PhongMaterial PureWhite =>
        new() {
            Name = "PureWhite",
            AmbientColor = ToColor(1, 1, 1),
            DiffuseColor = ToColor(1, 1, 1),
            SpecularColor = ToColor(0.0, 0.0, 0.0),
            EmissiveColor = ToColor(0.0, 0.0, 0.0),
            SpecularShininess = 1000000f
        };

    public static PhongMaterial Black =>
        new() {
            Name = "Black",
            AmbientColor = ToColor(0.1, 0.1, 0.1),
            DiffuseColor = ToColor(0.0, 0.0, 0.0),
            SpecularColor = ToColor(0.0225, 0.0225, 0.0225),
            EmissiveColor = ToColor(0.0, 0.0, 0.0),
            SpecularShininess = 12.8f
        };

    public static PhongMaterial Gray =>
        new() {
            Name = "Gray",
            AmbientColor = ToColor(0.1, 0.1, 0.1),
            DiffuseColor = ToColor(0.254902, 0.254902, 0.254902),
            SpecularColor = ToColor(0.0225, 0.0225, 0.0225),
            EmissiveColor = ToColor(0.0, 0.0, 0.0),
            SpecularShininess = 12.8f
        };

    public static PhongMaterial MediumGray =>
        new() {
            Name = "MediumGray",
            AmbientColor = ToColor(0.1, 0.1, 0.1),
            DiffuseColor = ToColor(0.454902, 0.454902, 0.454902),
            SpecularColor = ToColor(0.0225, 0.0225, 0.0225),
            EmissiveColor = ToColor(0.0, 0.0, 0.0),
            SpecularShininess = 12.8f
        };

    public static PhongMaterial LightGray =>
        new() {
            Name = "LightGray",
            AmbientColor = ToColor(0.1, 0.1, 0.1),
            DiffuseColor = ToColor(0.682353, 0.682353, 0.682353),
            SpecularColor = ToColor(0.0225, 0.0225, 0.0225),
            EmissiveColor = ToColor(0.0, 0.0, 0.0),
            SpecularShininess = 12.8f
        };

    // Materials from: http://globe3d.sourceforge.net/g3d_html/gl-materials__ads.htm
    public static PhongMaterial Glass =>
        new() {
            Name = "Glass",
            AmbientColor = ToColor(0.0, 0.0, 0.0),
            DiffuseColor = ToColor(0.588235, 0.670588, 0.729412),
            SpecularColor = ToColor(0.9, 0.9, 0.9),
            EmissiveColor = ToColor(0.0, 0.0, 0.0),
            SpecularShininess = 96.0f
        };

    public static PhongMaterial Brass =>
        new() {
            Name = "Brass",
            AmbientColor = ToColor(0.329412, 0.223529, 0.027451),
            DiffuseColor = ToColor(0.780392, 0.568627, 0.113725),
            SpecularColor = ToColor(0.992157, 0.941176, 0.807843),
            EmissiveColor = ToColor(0.0, 0.0, 0.0, 0.0),
            SpecularShininess = 27.8974f
        };

    public static PhongMaterial Bronze =>
        new() {
            Name = "Bronze",
            AmbientColor = ToColor(0.2125, 0.1275, 0.054),
            DiffuseColor = ToColor(0.714, 0.4284, 0.18144),
            SpecularColor = ToColor(0.393548, 0.271906, 0.166721),
            EmissiveColor = ToColor(0.0, 0.0, 0.0, 0.0),
            SpecularShininess = 25.6f
        };

    public static PhongMaterial PolishedBronze =>
        new() {
            Name = "PolishedBronze",
            AmbientColor = ToColor(0.25, 0.148, 0.06475),
            DiffuseColor = ToColor(0.4, 0.2368, 0.1036),
            SpecularColor = ToColor(0.774597, 0.458561, 0.200621),
            EmissiveColor = ToColor(0.0, 0.0, 0.0, 0.0),
            SpecularShininess = 76.8f
        };

    public static PhongMaterial Chrome =>
        new() {
            Name = "Chrome",
            AmbientColor = ToColor(0.25f, 0.25f, 0.25f),
            DiffuseColor = ToColor(0.4f, 0.4f, 0.4f),
            SpecularColor = ToColor(0.774597f, 0.774597f, 0.774597f),
            EmissiveColor = ToColor(0f, 0f, 0f, 0f),
            SpecularShininess = 76.8f
        };

    public static PhongMaterial Copper =>
        new() {
            Name = "Copper",
            AmbientColor = ToColor(0.19125, 0.0735, 0.0225),
            DiffuseColor = ToColor(0.7038, 0.27048, 0.0828),
            SpecularColor = ToColor(0.256777, 0.137622, 0.086014),
            EmissiveColor = ToColor(0.0, 0.0, 0.0, 0.0),
            SpecularShininess = 12.8f
        };

    public static PhongMaterial PolishedCopper =>
        new() {
            Name = "PolishedCopper",
            AmbientColor = ToColor(0.2295, 0.08825, 0.0275),
            DiffuseColor = ToColor(0.5508, 0.2118, 0.066),
            SpecularColor = ToColor(0.580594, 0.223257, 0.0695701),
            EmissiveColor = ToColor(0.0, 0.0, 0.0, 0.0),
            SpecularShininess = 51.2f
        };

    public static PhongMaterial Gold =>
        new() {
            Name = "Gold",
            AmbientColor = ToColor(0.24725, 0.1995, 0.0745),
            DiffuseColor = ToColor(0.75164, 0.60648, 0.22648),
            SpecularColor = ToColor(0.628281, 0.555802, 0.366065),
            EmissiveColor = ToColor(0.0, 0.0, 0.0, 0.0),
            SpecularShininess = 51.2f
        };

    public static PhongMaterial PolishedGold =>
        new() {
            Name = "PolishedGold",
            AmbientColor = ToColor(0.24725, 0.2245, 0.0645),
            DiffuseColor = ToColor(0.34615, 0.3143, 0.0903),
            SpecularColor = ToColor(0.797357, 0.723991, 0.208006),
            EmissiveColor = ToColor(0.0, 0.0, 0.0, 0.0),
            SpecularShininess = 83.2f
        };


    public static PhongMaterial Pewter =>
        new() {
            Name = "Pewter",
            AmbientColor = ToColor(0.105882, 0.058824, 0.113725),
            DiffuseColor = ToColor(0.427451, 0.470588, 0.541176),
            SpecularColor = ToColor(0.333333, 0.333333, 0.521569),
            EmissiveColor = ToColor(0.0, 0.0, 0.0, 0.0),
            SpecularShininess = 9.84615f
        };

    public static PhongMaterial Silver =>
        new() {
            Name = "Silver",
            AmbientColor = ToColor(0.19225, 0.19225, 0.19225),
            DiffuseColor = ToColor(0.50754, 0.50754, 0.50754),
            SpecularColor = ToColor(0.508273, 0.508273, 0.508273),
            EmissiveColor = ToColor(0.0, 0.0, 0.0, 0.0),
            SpecularShininess = 51.2f
        };

    public static PhongMaterial PolishedSilver =>
        new() {
            Name = "PolishedSilver",
            AmbientColor = ToColor(0.23125, 0.23125, 0.23125),
            DiffuseColor = ToColor(0.2775, 0.2775, 0.2775),
            SpecularColor = ToColor(0.773911, 0.773911, 0.773911),
            EmissiveColor = ToColor(0.0, 0.0, 0.0, 0.0),
            SpecularShininess = 89.6f
        };

    public static PhongMaterial Emerald =>
        new() {
            Name = "Emerald",
            AmbientColor = ToColor(0.0215, 0.1745, 0.0215, 0.55),
            DiffuseColor = ToColor(0.07568, 0.61424, 0.07568, 0.55),
            SpecularColor = ToColor(0.633, 0.727811, 0.633, 0.55),
            EmissiveColor = ToColor(0.0, 0.0, 0.0, 0.0),
            SpecularShininess = 76.8f
        };

    public static PhongMaterial Jade =>
        new() {
            Name = "Jade",
            AmbientColor = ToColor(0.135, 0.2225, 0.1575, 0.95),
            DiffuseColor = ToColor(0.54, 0.89, 0.63, 0.95),
            SpecularColor = ToColor(0.316228, 0.316228, 0.316228, 0.95),
            EmissiveColor = ToColor(0.0, 0.0, 0.0, 0.0),
            SpecularShininess = 12.8f
        };

    public static PhongMaterial Obsidian =>
        new() {
            Name = "Obsidian",
            AmbientColor = ToColor(0.05375, 0.05, 0.06625, 0.82),
            DiffuseColor = ToColor(0.18275, 0.17, 0.22525, 0.82),
            SpecularColor = ToColor(0.332741, 0.328634, 0.346435, 0.82),
            EmissiveColor = ToColor(0.0, 0.0, 0.0, 0.0),
            SpecularShininess = 38.4f
        };

    public static PhongMaterial Pearl =>
        new() {
            Name = "Pearl",
            AmbientColor = ToColor(0.25, 0.20725, 0.20725, 0.922),
            DiffuseColor = ToColor(1.0, 0.829, 0.829, 0.922),
            SpecularColor = ToColor(0.296648, 0.296648, 0.296648, 0.922),
            EmissiveColor = ToColor(0.0, 0.0, 0.0, 0.0),
            SpecularShininess = 11.264f
        };

    public static PhongMaterial Ruby =>
        new() {
            Name = "Ruby",
            AmbientColor = ToColor(0.1745, 0.01175, 0.01175, 0.55),
            DiffuseColor = ToColor(0.61424, 0.04136, 0.04136, 0.55),
            SpecularColor = ToColor(0.727811, 0.626959, 0.626959, 0.55),
            EmissiveColor = ToColor(0.0, 0.0, 0.0, 0.0),
            SpecularShininess = 76.8f
        };

    public static PhongMaterial Turquoise =>
        new() {
            Name = "Turquoise",
            AmbientColor = ToColor(0.1, 0.18725, 0.1745, 0.8),
            DiffuseColor = ToColor(0.396, 0.74151, 0.69102, 0.8),
            SpecularColor = ToColor(0.297254, 0.30829, 0.306678, 0.8),
            EmissiveColor = ToColor(0.0, 0.0, 0.0, 0.0),
            SpecularShininess = 12.8f
        };

    public static PhongMaterial BlackPlastic =>
        new() {
            Name = "BlackPlastic",
            AmbientColor = ToColor(0.0, 0.0, 0.0),
            DiffuseColor = ToColor(0.01, 0.01, 0.01),
            SpecularColor = ToColor(0.50, 0.50, 0.50),
            EmissiveColor = ToColor(0.0, 0.0, 0.0, 0.0),
            SpecularShininess = 32f
        };

    public static PhongMaterial BlackRubber =>
        new() {
            Name = "BlackRubber",
            AmbientColor = ToColor(0.02, 0.02, 0.02),
            DiffuseColor = ToColor(0.01, 0.01, 0.01),
            SpecularColor = ToColor(0.4, 0.4, 0.4),
            EmissiveColor = ToColor(0.0, 0.0, 0.0, 0.0),
            SpecularShininess = 10f
        };

    public static PhongMaterial DefaultVRML =>
        new() {
            Name = "DefaultVRML",
            AmbientColor = ToColor(0.2, 0.2, 0.2),
            DiffuseColor = ToColor(0.8, 0.8, 0.8),
            SpecularColor = ToColor(0.0, 0.0, 0.0),
            EmissiveColor = ToColor(0.0, 0.0, 0.0),
            SpecularShininess = 25.6f
        };

    public static PhongMaterial GetMaterial(string materialName) {
        var mat = Materials.FirstOrDefault(x => x.Name == materialName);
        return mat != null ? mat : DefaultVRML;
    }

    public static Color4 ToColor(double r, double g, double b, double a = 1.0) =>
        //return new Color4((float)r, (float)g, (float)b, (float)a);
        System.Windows.Media.Color.FromScRgb((float)a, (float)r, (float)g, (float)b).ToColor4();
}
