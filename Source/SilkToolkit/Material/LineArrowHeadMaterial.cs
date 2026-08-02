/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Windows;
using HelixToolkit.SharpDX.Core.Model;

namespace HelixToolkit.Wpf.SharpDX;

public class LineArrowHeadMaterial : LineMaterial {
    public static readonly DependencyProperty ArrowSizeProperty =
        DependencyProperty.Register("ArrowSize",
                                    typeof(double),
                                    typeof(LineArrowHeadMaterial),
                                    new PropertyMetadata(0.1,
                                                         (d, e) => {
                                                             ((d as LineMaterial).Core as LineArrowHeadMaterialCore)
                                                                 .ArrowSize = (float)(double)e.NewValue;
                                                         }));

    public double ArrowSize {
        get => (double)GetValue(ArrowSizeProperty);
        set => SetValue(ArrowSizeProperty, value);
    }


    protected override MaterialCore OnCreateCore() {
        return new LineArrowHeadMaterialCore {
            Name = Name,
            LineColor = Color.ToColor4(),
            Smoothness = (float)Smoothness,
            Thickness = (float)Thickness,
            EnableDistanceFading = EnableDistanceFading,
            FadingNearDistance = (float)FadingNearDistance,
            FadingFarDistance = (float)FadingFarDistance,
            Texture = Texture,
            TextureScale = (float)TextureScale,
            SamplerDescription = SamplerDescription,
            ArrowSize = (float)ArrowSize,
            FixedSize = FixedSize
        };
    }
}

public class LineArrowHeadTailMaterial : LineArrowHeadMaterial {
    protected override MaterialCore OnCreateCore() {
        return new LineArrowHeadTailMaterialCore {
            Name = Name,
            LineColor = Color.ToColor4(),
            Smoothness = (float)Smoothness,
            Thickness = (float)Thickness,
            EnableDistanceFading = EnableDistanceFading,
            FadingNearDistance = (float)FadingNearDistance,
            FadingFarDistance = (float)FadingFarDistance,
            Texture = Texture,
            TextureScale = (float)TextureScale,
            SamplerDescription = SamplerDescription,
            ArrowSize = (float)ArrowSize,
            FixedSize = FixedSize
        };
    }
}
