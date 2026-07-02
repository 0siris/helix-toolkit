/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

#if !CORE

using System;
using System.Linq;
using System.Windows;
using HelixToolkit.SharpDX.Core.Native;
using Media = System.Windows.Media;

namespace HelixToolkit.Wpf.SharpDX.Extensions;

public static class CommonExtensions
{
#if NETFX_CORE || WINUI
        public static FontWeight ToDXFontWeight(this FontWeight fontWeight)
#else
    public static FontWeight ToDXFontWeight(this System.Windows.FontWeight fontWeight)
#endif
    {
#if NETFX_CORE || WINUI
            var w = fontWeight.Weight;
            if (w == FontWeights.Black.Weight)
            {
                return FontWeight.Black;
            }
            else if (w == FontWeights.Bold.Weight)
            {
                return FontWeight.Bold;
            }
            else if (w == FontWeights.ExtraBlack.Weight)
            {
                return FontWeight.Black;
            }
            else if (w == FontWeights.ExtraBold.Weight)
            {
                return FontWeight.ExtraBold;
            }
            else if (w == FontWeights.ExtraLight.Weight)
            {
                return FontWeight.ExtraLight;
            }
            else if (w == FontWeights.Light.Weight)
            {
                return FontWeight.Light;
            }
            else if (w == FontWeights.Medium.Weight)
            {
                return FontWeight.Medium;
            }
            else if (w == FontWeights.Normal.Weight)
            {
                return FontWeight.Normal;
            }
            else if (w == FontWeights.SemiBold.Weight)
            {
                return FontWeight.SemiBold;
            }
            else if (w == FontWeights.Thin.Weight)
            {
                return FontWeight.Thin;
            }
            else
            {
                return FontWeight.Normal;
            }
#else
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
#endif
    }

#if NETFX_CORE || WINUI
        public static FontStyle ToDXFontStyle(this FontStyle style)
#else
    public static FontStyle ToDXFontStyle(this System.Windows.FontStyle style)
#endif
    {
#if NETFX_CORE || WINUI
            if (style == FontStyle.Italic)
            {
                return FontStyle.Italic;
            }
            else if (style == FontStyle.Normal)
            {
                return FontStyle.Normal;
            }
            else if (style == FontStyle.Oblique)
            {
                return FontStyle.Oblique;
            }
            else
            {
                return FontStyle.Normal;
            }
#else
        if (style == FontStyles.Italic) return FontStyle.Italic;

        if (style == FontStyles.Normal) return FontStyle.Normal;

        if (style == FontStyles.Oblique) return FontStyle.Oblique;

        return FontStyle.Normal;
#endif
    }

    public static ExtendMode ToD2DExtendMode(this Media.GradientSpreadMethod mode)
    {
        switch (mode)
        {
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

    public static Gamma ToD2DColorInterpolationMode(this Media.ColorInterpolationMode mode)
    {
        switch (mode)
        {
            case Media.ColorInterpolationMode.ScRgbLinearInterpolation:
                return Gamma.Linear;
            case Media.ColorInterpolationMode.SRgbLinearInterpolation:
                return Gamma.StandardRgb;
            default:
                return Gamma.Linear;
        }
    }

    public static Brush ToD2DBrush(this Media.Brush brush, D2DDeviceContext target)
    {
        if (brush is Media.SolidColorBrush solid) return new SolidColorBrush(target, solid.Color.ToColor4());

        if (brush is Media.LinearGradientBrush linear)
            return new LinearGradientBrush(target,
                new LinearGradientBrushProperties
                    {StartPoint = linear.StartPoint.ToVector2(), EndPoint = linear.EndPoint.ToVector2()},
                new GradientStopCollection
                (
                    target,
                    linear.GradientStops.Select(x => new GradientStop
                        {Color = x.Color.ToColor4(), Position = (float) x.Offset}).ToArray(),
                    linear.ColorInterpolationMode.ToD2DColorInterpolationMode(),
                    linear.SpreadMethod.ToD2DExtendMode()
                )
            );
#if NETFX_CORE || WINUI
#else

        if (brush is Media.RadialGradientBrush radial)
            return new RadialGradientBrush(target,
                new RadialGradientBrushProperties
                {
                    Center = radial.Center.ToVector2(),
                    GradientOriginOffset = radial.GradientOrigin.ToVector2(),
                    RadiusX = (float) radial.RadiusX,
                    RadiusY = (float) radial.RadiusY
                },
                new GradientStopCollection
                (
                    target,
                    radial.GradientStops.Select(x => new GradientStop
                        {Color = x.Color.ToColor4(), Position = (float) x.Offset}).ToArray(),
                    radial.ColorInterpolationMode.ToD2DColorInterpolationMode(),
                    radial.SpreadMethod.ToD2DExtendMode()
                ));
#endif

        throw new NotImplementedException("Brush does not support yet.");
    }

    public static CapStyle ToD2DCapStyle(this Media.PenLineCap cap)
    {
        switch (cap)
        {
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

    public static LineJoin ToD2DLineJoin(this Media.PenLineJoin lineJoin)
    {
        switch (lineJoin)
        {
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
#if !NETFX_CORE && !WINUI
    public static DashStyle ToD2DDashStyle(this Media.DashStyle style)
    {
        if (style == Media.DashStyles.Dash) return DashStyle.Dash;

        if (style == Media.DashStyles.DashDot) return DashStyle.DashDot;

        if (style == Media.DashStyles.DashDotDot) return DashStyle.DashDotDot;

        if (style == Media.DashStyles.Dot) return DashStyle.Dot;

        return DashStyle.Solid;
    }
#else
#endif

#if NETFX_CORE || WINUI
        public static TextAlignment ToD2DTextAlignment(this TextAlignment alignment)
#else
    public static TextAlignment ToD2DTextAlignment(this System.Windows.TextAlignment alignment)
#endif
    {
        switch (alignment)
        {
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

#if NETFX_CORE || WINUI
        public static FlowDirection ToD2DFlowDir(this FlowDirection direction)
#else
    public static FlowDirection ToD2DFlowDir(this System.Windows.FlowDirection direction)
#endif
    {
        switch (direction)
        {
            case System.Windows.FlowDirection.LeftToRight:
                return FlowDirection.LeftToRight;
            case System.Windows.FlowDirection.RightToLeft:
                return FlowDirection.RightToLeft;
            default:
                return FlowDirection.LeftToRight;
        }
    }
}
#endif