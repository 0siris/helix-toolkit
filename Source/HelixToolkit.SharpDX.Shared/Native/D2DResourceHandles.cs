/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System;

#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX
#else
#if CORE
namespace HelixToolkit.SharpDX.Core
#else
namespace HelixToolkit.UWP
#endif
#endif
{
    namespace Native
    {
        using Utilities;

        public abstract class D2DNativeResource : IDisposable
        {
            protected D2DNativeResource(object nativeResource = null)
            {
                NativeResource = nativeResource;
            }

            public object NativeResource { get; }

            public bool IsDisposed { get; private set; }

            public virtual void Dispose()
            {
                if (IsDisposed)
                {
                    return;
                }

                if (NativeResource is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                IsDisposed = true;
            }
        }

        public sealed class D2DFactory : D2DNativeResource
        {
            public D2DFactory(object nativeResource = null)
                : base(nativeResource)
            {
            }
        }

        public sealed class D2DDevice : D2DNativeResource
        {
            public D2DDevice(object nativeResource = null)
                : base(nativeResource)
            {
            }
        }

        public sealed class D2DDeviceContext : D2DNativeResource
        {
            public D2DDeviceContext(object nativeResource = null)
                : base(nativeResource)
            {
            }

            public BitmapProxy Target { get; set; }

            public D2DSizeF DotsPerInch { get; set; } = new D2DSizeF(96, 96);

            public int MaximumBitmapSize { get; set; }

            public Matrix3x2 Transform { get; set; } = Matrix3x2.Identity;

            public D2DFactory Factory { get; set; } = new D2DFactory();

            public void BeginDraw()
            {
            }

            public void EndDraw()
            {
            }

            public void Clear(Color4 color)
            {
            }

            public void DrawRectangle(RectangleF rect, Brush brush, float strokeWidth, StrokeStyle strokeStyle = null)
            {
            }

            public void FillRectangle(RectangleF rect, Brush brush)
            {
            }

            public void DrawRoundedRectangle(RoundedRectangle rect, Brush brush, float strokeWidth, StrokeStyle strokeStyle = null)
            {
            }

            public void FillRoundedRectangle(RoundedRectangle rect, Brush brush)
            {
            }

            public void DrawEllipse(Ellipse ellipse, Brush brush, float strokeWidth, StrokeStyle strokeStyle = null)
            {
            }

            public void FillEllipse(Ellipse ellipse, Brush brush)
            {
            }

            public void DrawGeometry(PathGeometry geometry, Brush brush, float strokeWidth, StrokeStyle strokeStyle = null)
            {
            }

            public void FillGeometry(PathGeometry geometry, Brush brush)
            {
            }

            public void DrawImage(Utilities.BitmapProxy image, Vector2 targetOffset, RectangleF imageRectangle, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.Linear, CompositeMode compositeMode = CompositeMode.SourceOver)
            {
            }

            public void DrawBitmap(Bitmap bitmap, RectangleF destinationRectangle, float opacity, BitmapInterpolationMode interpolationMode)
            {
            }

            public void DrawTextLayout(Vector2 origin, TextLayout textLayout, Brush brush, DrawTextOptions options = DrawTextOptions.None)
            {
            }
        }

        public sealed class WICImagingFactory : D2DNativeResource
        {
            public WICImagingFactory(object nativeResource = null)
                : base(nativeResource)
            {
            }
        }

        public sealed class DirectWriteFactory : D2DNativeResource
        {
            public DirectWriteFactory(object nativeResource = null)
                : base(nativeResource)
            {
            }
        }

        public sealed class D2DColorContext : D2DNativeResource
        {
            public D2DColorContext(object nativeResource = null)
                : base(nativeResource)
            {
            }
        }

        public sealed class D2DBitmap : D2DNativeResource
        {
            public D2DBitmap(Size2 size, object nativeResource = null)
                : base(nativeResource)
            {
                Size = new D2DSizeF(size.Width, size.Height);
            }

            public D2DSizeF Size { get; }
        }

        [Flags]
        public enum D2DBitmapOptions
        {
            None = 0,
            Target = 1,
            CannotDraw = 2
        }

        public enum D2DAlphaMode
        {
            Unknown = 0,
            Premultiplied = 1,
            Straight = 2,
            Ignore = 3
        }

        public struct D2DPixelFormat
        {
            public D2DPixelFormat(Format format, D2DAlphaMode alphaMode)
            {
                Format = format;
                AlphaMode = alphaMode;
            }

            public Format Format;
            public D2DAlphaMode AlphaMode;
        }

        public sealed class D2DBitmapProperties
        {
            public D2DBitmapProperties(D2DPixelFormat pixelFormat, float dpiX, float dpiY, D2DBitmapOptions options, D2DColorContext colorContext = null)
            {
                PixelFormat = pixelFormat;
                DpiX = dpiX;
                DpiY = dpiY;
                Options = options;
                ColorContext = colorContext;
            }

            public D2DPixelFormat PixelFormat { get; }

            public float DpiX { get; }

            public float DpiY { get; }

            public D2DBitmapOptions Options { get; }

            public D2DColorContext ColorContext { get; }
        }

        public readonly struct D2DSizeF
        {
            public D2DSizeF(float width, float height)
            {
                Width = width;
                Height = height;
            }

            public float Width { get; }

            public float Height { get; }
        }
    }

    public class Brush : Native.D2DNativeResource
    {
        public Brush(object nativeResource = null)
            : base(nativeResource)
        {
        }

        public T QueryInterface<T>()
            where T : class
        {
            return this as T;
        }
    }

    public sealed class SolidColorBrush : Brush
    {
        public SolidColorBrush(Native.D2DDeviceContext context, Color4 color)
        {
            Color = color;
        }

        public SolidColorBrush(Native.D2DDeviceContext context, Color4 color, BrushProperties properties)
        {
            Color = color;
            Properties = properties;
        }

        public Color4 Color { get; }

        public BrushProperties Properties { get; }
    }

    public struct BrushProperties
    {
        public float Opacity;
    }

    public sealed class StrokeStyle : Native.D2DNativeResource
    {
        public StrokeStyle(Native.D2DFactory factory, StrokeStyleProperties properties, float[] dashes = null)
        {
            Properties = properties;
            Dashes = dashes ?? Array.Empty<float>();
        }

        public StrokeStyleProperties Properties { get; }

        public float[] Dashes { get; }

        public T QueryInterface<T>()
            where T : class
        {
            return this as T;
        }
    }

    public struct StrokeStyleProperties
    {
        public CapStyle DashCap;
        public CapStyle StartCap;
        public CapStyle EndCap;
        public float DashOffset;
        public LineJoin LineJoin;
        public float MiterLimit;
        public DashStyle DashStyle;
    }

    public sealed class Bitmap : Native.D2DNativeResource
    {
        public Bitmap(Size2F size, object nativeResource = null)
            : base(nativeResource)
        {
            Size = size;
        }

        public Size2F Size { get; }

        public int Width => (int)Math.Ceiling(Size.Width);

        public int Height => (int)Math.Ceiling(Size.Height);
    }

    public struct GradientStop
    {
        public float Position;
        public Color4 Color;
    }

    public enum ExtendMode
    {
        Clamp,
        Wrap,
        Mirror
    }

    public enum Gamma
    {
        StandardRgb,
        Linear
    }

    public sealed class GradientStopCollection : Native.D2DNativeResource
    {
        public GradientStopCollection(Native.D2DDeviceContext context, GradientStop[] gradients, Gamma gamma, ExtendMode extendMode)
        {
            Gradients = gradients ?? Array.Empty<GradientStop>();
            Gamma = gamma;
            ExtendMode = extendMode;
        }

        public GradientStop[] Gradients { get; }

        public Gamma Gamma { get; }

        public ExtendMode ExtendMode { get; }
    }

    public struct LinearGradientBrushProperties
    {
        public Vector2 StartPoint;
        public Vector2 EndPoint;
    }

    public sealed class LinearGradientBrush : Brush
    {
        public LinearGradientBrush(Native.D2DDeviceContext context, LinearGradientBrushProperties properties, GradientStopCollection gradientStops)
        {
            Properties = properties;
            GradientStops = gradientStops;
        }

        public LinearGradientBrushProperties Properties { get; }

        public GradientStopCollection GradientStops { get; }
    }

    public struct RadialGradientBrushProperties
    {
        public Vector2 Center;
        public Vector2 GradientOriginOffset;
        public float RadiusX;
        public float RadiusY;
    }

    public sealed class RadialGradientBrush : Brush
    {
        public RadialGradientBrush(Native.D2DDeviceContext context, RadialGradientBrushProperties properties, GradientStopCollection gradientStops)
        {
            Properties = properties;
            GradientStops = gradientStops;
        }

        public RadialGradientBrushProperties Properties { get; }

        public GradientStopCollection GradientStops { get; }
    }

    public struct RoundedRectangle
    {
        public RectangleF Rect;
        public float RadiusX;
        public float RadiusY;
    }

    public struct Ellipse
    {
        public Vector2 Point;
        public float RadiusX;
        public float RadiusY;
    }

    public sealed class PathGeometry : Native.D2DNativeResource
    {
        public PathGeometry(Native.D2DFactory factory)
        {
            Factory = factory;
        }

        public Native.D2DFactory Factory { get; }

        public GeometrySink Open()
        {
            return new GeometrySink(this);
        }
    }

    public sealed class GeometrySink : Native.D2DNativeResource
    {
        internal GeometrySink(PathGeometry geometry)
        {
            Geometry = geometry;
        }

        public PathGeometry Geometry { get; }

        public D2DFillMode FillMode { get; private set; }

        public void SetFillMode(D2DFillMode fillMode)
        {
            FillMode = fillMode;
        }

        public void BeginFigure(Vector2 startPoint, FigureBegin figureBegin)
        {
        }

        public void SetSegmentFlags(PathSegment flags)
        {
        }

        public void AddLine(Vector2 point)
        {
        }

        public void AddBezier(BezierSegmentData segment)
        {
        }

        public void AddArc(ArcSegmentData segment)
        {
        }

        public void EndFigure(FigureEnd figureEnd)
        {
        }

        public void Close()
        {
        }
    }

    public struct BezierSegmentData
    {
        public Vector2 Point1;
        public Vector2 Point2;
        public Vector2 Point3;
    }

    public struct ArcSegmentData
    {
        public Vector2 Point;
        public Size2F Size;
        public float RotationAngle;
        public SweepDirection SweepDirection;
        public ArcSize ArcSize;
    }

    public sealed class TextFormat : Native.D2DNativeResource
    {
        public TextFormat(Native.DirectWriteFactory factory, string fontFamily, FontWeight fontWeight, FontStyle fontStyle, float fontSize)
        {
            Factory = factory;
            FontFamily = fontFamily;
            FontWeight = fontWeight;
            FontStyle = fontStyle;
            FontSize = fontSize;
        }

        public Native.DirectWriteFactory Factory { get; }

        public string FontFamily { get; }

        public FontWeight FontWeight { get; }

        public FontStyle FontStyle { get; }

        public float FontSize { get; }
    }

    public sealed class TextLayout : Native.D2DNativeResource
    {
        public TextLayout(Native.DirectWriteFactory factory, string text, TextFormat textFormat, float maxWidth, float maxHeight)
        {
            Factory = factory;
            Text = text ?? string.Empty;
            TextFormat = textFormat;
            MaxWidth = maxWidth;
            MaxHeight = maxHeight;
            var height = Math.Max(1, textFormat?.FontSize ?? 12);
            var width = Math.Min(float.IsInfinity(maxWidth) || maxWidth <= 0 ? float.MaxValue : maxWidth, Text.Length * height * 0.55f);
            Metrics = new TextMetrics { Width = width, WidthIncludingTrailingWhitespace = width, Height = height };
        }

        public Native.DirectWriteFactory Factory { get; }

        public string Text { get; }

        public TextFormat TextFormat { get; }

        public float MaxWidth { get; }

        public float MaxHeight { get; }

        public TextAlignment TextAlignment { get; set; } = TextAlignment.Leading;

        public TextMetrics Metrics { get; }
    }

    public struct TextMetrics
    {
        public float Width;
        public float WidthIncludingTrailingWhitespace;
        public float Height;
    }

    public enum CapStyle
    {
        Flat,
        Square,
        Round,
        Triangle
    }

    public enum DashStyle
    {
        Solid,
        Dash,
        Dot,
        DashDot,
        DashDotDot,
        Custom
    }

    public enum LineJoin
    {
        Miter,
        Bevel,
        Round,
        MiterOrBevel
    }

    public enum SweepDirection
    {
        CounterClockwise,
        Clockwise
    }

    public enum ArcSize
    {
        Small,
        Large
    }

    public enum FigureBegin
    {
        Filled,
        Hollow
    }

    public enum FigureEnd
    {
        Open,
        Closed
    }

    [Flags]
    public enum PathSegment
    {
        None = 0,
        ForceUnstroked = 1,
        ForceRoundLineJoin = 2
    }

    public enum D2DFillMode
    {
        Alternate,
        Winding
    }

    public enum TextAlignment
    {
        Leading,
        Trailing,
        Center,
        Justified
    }

    public enum FlowDirection
    {
        LeftToRight,
        RightToLeft
    }

    public enum FontWeight
    {
        Thin = 100,
        ExtraLight = 200,
        Light = 300,
        Normal = 400,
        Medium = 500,
        SemiBold = 600,
        Bold = 700,
        ExtraBold = 800,
        Black = 900
    }

    public enum FontStyle
    {
        Normal,
        Oblique,
        Italic
    }

    [Flags]
    public enum DrawTextOptions
    {
        None = 0
    }

    public enum BitmapInterpolationMode
    {
        NearestNeighbor,
        Linear
    }

    public enum CompositeMode
    {
        SourceOver
    }
}
