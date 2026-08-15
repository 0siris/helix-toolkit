using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Extensions;
using HelixToolkit.Wpf.SharpDX.Material;

namespace CustomShaderDemo.Materials;

public class CustomPointMaterial : PointMaterial {
    protected override MaterialCore OnCreateCore() => new CustomPointMaterialCore() {
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

    protected override Freezable CreateInstanceCore() => new CustomPointMaterial() {
        Name = Name
    };
}
