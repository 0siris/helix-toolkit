using System.Windows;
using HelixToolkit.Wpf.SharpDX;

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
