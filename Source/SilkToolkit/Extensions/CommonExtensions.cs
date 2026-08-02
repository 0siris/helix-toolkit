/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using System;
using System.Linq;
using System.Windows;
using HelixToolkit.SharpDX.Core.Native;
using Media = System.Windows.Media;

namespace HelixToolkit.Wpf.SharpDX.Extensions;

public static class CommonExtensions {
    public static FontWeight ToDXFontWeight(this System.Windows.FontWeight fontWeight) {
        if (fontWeight == FontWeights.Black) return FontWeight.Black;

        if (fontWeight == FontWeights.Bold) return FontWeight.Bold;

        if (fontWeight == FontWeights.DemiBold) return FontWeight.SemiBold;

        if (fontWeight == FontWeights.ExtraBlack) return FontWeight.Black;

        if (fontWeight == FontWeights.ExtraBold) return FontWeight.ExtraBold;

        if (fontWeight == FontWeights.ExtraLight) return FontWeight.ExtraLight;

        if (fontWeight == FontWeights.Heavy) return FontWeight.Black;

        if (fontWeight == FontWeights.Light) return FontWeight.Light;

        if (fontWeight == FontWeights.Medium) return FontWeight.Medium;

        if (fontWeight == FontWeights.Normal) return FontWeight.Normal;

        if (fontWeight == FontWeights.Regular) return FontWeight.Normal;

        if (fontWeight == FontWeights.SemiBold) return FontWeight.SemiBold;

        if (fontWeight == FontWeights.Thin) return FontWeight.Thin;

        if (fontWeight == FontWeights.UltraBlack) return FontWeight.Black;

        if (fontWeight == FontWeights.UltraBold) return FontWeight.ExtraBold;

        if (fontWeight == FontWeights.UltraLight) return FontWeight.ExtraLight;

        return FontWeight.Normal;
    }

    public static FontStyle ToDXFontStyle(this System.Windows.FontStyle style) {
        if (style == FontStyles.Italic) return FontStyle.Italic;

        if (style == FontStyles.Normal) return FontStyle.Normal;

        if (style == FontStyles.Oblique) return FontStyle.Oblique;

        return FontStyle.Normal;
    }

    public static ExtendMode ToD2DExtendMode(this Media.GradientSpreadMethod mode) {
        switch (mode) {
            case Media.GradientSpreadMethod.Pad:
                return ExtendMode.Clamp;
            case Media.GradientSpreadMethod.Reflect:
                return ExtendMode.Mirror;
            case Media.GradientSpreadMethod.Repeat:
                return ExtendMode.Wrap;
            default:
                return ExtendMode.Wrap;
        }
    }

    public static Gamma ToD2DColorInterpolationMode(this Media.ColorInterpolationMode mode) {
        switch (mode) {
            case Media.ColorInterpolationMode.ScRgbLinearInterpolation:
                return Gamma.Linear;
            case Media.ColorInterpolationMode.SRgbLinearInterpolation:
                return Gamma.StandardRgb;
            default:
                return Gamma.Linear;
        }
    }

    #pragma warning disable CA2000 // Gradient stop collections are owned by the returned Direct2D brush.
    public static Brush ToD2DBrush(this Media.Brush brush, D2DDeviceContext target) {
        if (brush is Media.SolidColorBrush solid) return new SolidColorBrush(target, solid.Color.ToColor4());

        if (brush is Media.LinearGradientBrush linear)
            return new LinearGradientBrush(target,
                                           new LinearGradientBrushProperties {
                                               StartPoint = linear.StartPoint.ToVector2(),
                                               EndPoint = linear.EndPoint.ToVector2()
                                           },
                                           new GradientStopCollection(target,
                                                                      [.. linear.GradientStops.Select(x => new GradientStop {
                                                                          Color = x.Color.ToColor4(),
                                                                          Position = (float)x.Offset
                                                                      })],
                                                                      linear.ColorInterpolationMode
                                                                            .ToD2DColorInterpolationMode(),
                                                                      linear.SpreadMethod.ToD2DExtendMode()));

        if (brush is Media.RadialGradientBrush radial)
            return new RadialGradientBrush(target,
                                           new RadialGradientBrushProperties {
                                               Center = radial.Center.ToVector2(),
                                               GradientOriginOffset = radial.GradientOrigin.ToVector2(),
                                               RadiusX = (float)radial.RadiusX,
                                               RadiusY = (float)radial.RadiusY
                                           },
                                           new GradientStopCollection(target,
                                                                      [.. radial.GradientStops.Select(x => new GradientStop {
                                                                          Color = x.Color.ToColor4(),
                                                                          Position = (float)x.Offset
                                                                      })],
                                                                      radial.ColorInterpolationMode
                                                                            .ToD2DColorInterpolationMode(),
                                                                      radial.SpreadMethod.ToD2DExtendMode()));

        throw new NotImplementedException("Brush does not support yet.");
    }
    #pragma warning restore CA2000

    public static CapStyle ToD2DCapStyle(this Media.PenLineCap cap) {
        switch (cap) {
            case Media.PenLineCap.Flat:
                return CapStyle.Flat;
            case Media.PenLineCap.Round:
                return CapStyle.Round;
            case Media.PenLineCap.Square:
                return CapStyle.Square;
            case Media.PenLineCap.Triangle:
                return CapStyle.Triangle;
            default:
                return CapStyle.Flat;
        }
    }

    public static LineJoin ToD2DLineJoin(this Media.PenLineJoin lineJoin) {
        switch (lineJoin) {
            case Media.PenLineJoin.Bevel:
                return LineJoin.Bevel;
            case Media.PenLineJoin.Miter:
                return LineJoin.Miter;
            case Media.PenLineJoin.Round:
                return LineJoin.Round;
            default:
                return LineJoin.Bevel;
        }
    }
    public static DashStyle ToD2DDashStyle(this Media.DashStyle style) {
        if (style == Media.DashStyles.Dash) return DashStyle.Dash;

        if (style == Media.DashStyles.DashDot) return DashStyle.DashDot;

        if (style == Media.DashStyles.DashDotDot) return DashStyle.DashDotDot;

        if (style == Media.DashStyles.Dot) return DashStyle.Dot;

        return DashStyle.Solid;
    }

    public static TextAlignment ToD2DTextAlignment(this System.Windows.TextAlignment alignment) {
        switch (alignment) {
            case System.Windows.TextAlignment.Center:
                return TextAlignment.Center;
            case System.Windows.TextAlignment.Left:
                return TextAlignment.Leading;
            case System.Windows.TextAlignment.Right:
                return TextAlignment.Trailing;
            case System.Windows.TextAlignment.Justify:
                return TextAlignment.Justified;
            default:
                return TextAlignment.Leading;
        }
    }

    public static FlowDirection ToD2DFlowDir(this System.Windows.FlowDirection direction) {
        switch (direction) {
            case System.Windows.FlowDirection.LeftToRight:
                return FlowDirection.LeftToRight;
            case System.Windows.FlowDirection.RightToLeft:
                return FlowDirection.RightToLeft;
            default:
                return FlowDirection.LeftToRight;
        }
    }
}
