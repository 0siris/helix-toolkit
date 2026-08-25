/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Utilities.Buffers;
using Silk.NET.Core.Native;
using Silk.NET.Direct2D;
using Silk.NET.DirectWrite;
using Silk.NET.DXGI;
using IDWriteTextFormat = Silk.NET.DirectWrite.IDWriteTextFormat;
using IDWriteTextLayout = Silk.NET.DirectWrite.IDWriteTextLayout;
using SilkD2DBitmapBasePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct2D.ID2D1Bitmap>;
using SilkD2DSolidBrushPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct2D.ID2D1SolidColorBrush>;
using SilkDWriteTextFormatPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.DirectWrite.IDWriteTextFormat>;
using SilkDWriteTextLayoutPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.DirectWrite.IDWriteTextLayout>;

namespace HelixToolkit.SharpDX.Core.Native;

public unsafe class Brush : D2DNativeResource {
    public Brush(object? nativeResource = null)
        : base(nativeResource) { }

    internal virtual ID2D1Brush* Handle => null;

    public T? QueryInterface<T>()
        where T : class
        => this as T;
}

public sealed unsafe class SolidColorBrush : Brush {
    private SilkD2DSolidBrushPtr nativeBrush;

    public SolidColorBrush(D2DDeviceContext context, Color4 color) {
        Color = color;
        Create(context, color, 1);
    }

    public SolidColorBrush(D2DDeviceContext context, Color4 color, BrushProperties properties) {
        Color = color;
        Properties = properties;
        Create(context, color, properties.Opacity);
    }

    public Color4 Color { get; }

    public BrushProperties Properties { get; }

    internal override ID2D1Brush* Handle => (ID2D1Brush*) nativeBrush.Handle;

    public override void Dispose() {
        nativeBrush.Dispose();
        base.Dispose();
    }

    private void Create(D2DDeviceContext context, Color4 color, float opacity) {
        if (context.NativeHandle == null) return;

        var value = new D3Dcolorvalue(color.X, color.Y, color.Z, color.W);
        var properties = new Silk.NET.Direct2D.BrushProperties {
            Opacity = opacity
        };
        ID2D1SolidColorBrush* brush = null;
        SilkMarshal.ThrowHResult(context.NativeHandle->CreateSolidColorBrush(&value, &properties, &brush));
        nativeBrush = new SilkD2DSolidBrushPtr(brush);
        brush->Release();
    }
}

public struct BrushProperties {
    public float Opacity;
}

public sealed class StrokeStyle : D2DNativeResource {
    public StrokeStyle(D2DFactory factory, StrokeStyleProperties properties, float[]? dashes = null) {
        Properties = properties;
        Dashes = dashes ?? [];
    }

    public StrokeStyleProperties Properties { get; }

    public float[] Dashes { get; }

    public T? QueryInterface<T>()
        where T : class
        => this as T;
}

public struct StrokeStyleProperties {
    public CapStyle DashCap;
    public CapStyle StartCap;
    public CapStyle EndCap;
    public float DashOffset;
    public LineJoin LineJoin;
    public float MiterLimit;
    public DashStyle DashStyle;
}

public sealed unsafe class Bitmap : D2DNativeResource {
    private SilkD2DBitmapBasePtr nativeBitmap;
    private BitmapProxy? target;

    public Bitmap(Size2F size, object? nativeResource = null) {
        Size = size;
        if (nativeResource is SilkD2DBitmapBasePtr bitmap) nativeBitmap = bitmap;
    }

    internal Bitmap(Size2F size, Texture2D texture, BitmapProxy target) {
        Size = size;
        Texture = texture;
        this.target = target;
    }

    public Size2F Size { get; }

    public int Width => (int) Math.Ceiling(Size.Width);

    public int Height => (int) Math.Ceiling(Size.Height);

    internal ID2D1Bitmap* Handle => target?.Bitmap is { } bitmap
        ? (ID2D1Bitmap*) bitmap.Handle
        : nativeBitmap.Handle;

    internal Texture2D? Texture { get; private set; }

    public override void Dispose() {
        nativeBitmap.Dispose();
        target?.Dispose();
        target = null;
        Texture?.Dispose();
        Texture = null;
        base.Dispose();
    }
}

public struct GradientStop {
    public float Position;
    public Color4 Color;
}

public enum ExtendMode { Clamp, Wrap, Mirror }

public enum Gamma { StandardRgb, Linear }

public sealed class GradientStopCollection : D2DNativeResource {
    public GradientStopCollection(
        D2DDeviceContext context,
        GradientStop[] gradients,
        Gamma gamma,
        ExtendMode extendMode
    ) {
        Gradients = gradients;
        Gamma = gamma;
        ExtendMode = extendMode;
    }

    public GradientStop[] Gradients { get; }

    public Gamma Gamma { get; }

    public ExtendMode ExtendMode { get; }
}

public struct LinearGradientBrushProperties {
    public Vector2 StartPoint;
    public Vector2 EndPoint;
}

public sealed class LinearGradientBrush : Brush {
    public LinearGradientBrush(
        D2DDeviceContext context,
        LinearGradientBrushProperties properties,
        GradientStopCollection gradientStops
    ) {
        Properties = properties;
        GradientStops = gradientStops;
    }

    public LinearGradientBrushProperties Properties { get; }

    public GradientStopCollection GradientStops { get; }
}

public struct RadialGradientBrushProperties {
    public Vector2 Center;
    public Vector2 GradientOriginOffset;
    public float RadiusX;
    public float RadiusY;
}

public sealed class RadialGradientBrush : Brush {
    public RadialGradientBrush(
        D2DDeviceContext context,
        RadialGradientBrushProperties properties,
        GradientStopCollection gradientStops
    ) {
        Properties = properties;
        GradientStops = gradientStops;
    }

    public RadialGradientBrushProperties Properties { get; }

    public GradientStopCollection GradientStops { get; }
}

public struct RoundedRectangle {
    public RectangleF Rect;
    public float RadiusX;
    public float RadiusY;
}

public struct Ellipse {
    public Vector2 Point;
    public float RadiusX;
    public float RadiusY;
}

public sealed class PathGeometry : D2DNativeResource {
    public PathGeometry(D2DFactory factory) {
        Factory = factory;
    }

    public D2DFactory Factory { get; }

    public GeometrySink Open() => new(this);
}

public sealed class GeometrySink : D2DNativeResource {
    internal GeometrySink(PathGeometry geometry) {
        Geometry = geometry;
    }

    public PathGeometry Geometry { get; }

    public D2DFillMode FillMode { get; private set; }

    public void SetFillMode(D2DFillMode fillMode) {
        FillMode = fillMode;
    }

    public void BeginFigure(Vector2 startPoint, FigureBegin figureBegin) { }

    public void SetSegmentFlags(PathSegment flags) { }

    public void AddLine(Vector2 point) { }

    public void AddBezier(BezierSegmentData segment) { }

    public void AddArc(ArcSegmentData segment) { }

    public void EndFigure(FigureEnd figureEnd) { }

    public void Close() { }
}

public struct BezierSegmentData {
    public Vector2 Point1;
    public Vector2 Point2;
    public Vector2 Point3;
}

public struct ArcSegmentData {
    public Vector2 Point;
    public Size2F Size;
    public float RotationAngle;
    public SweepDirection SweepDirection;
    public ArcSize ArcSize;
}

public sealed unsafe class TextFormat : D2DNativeResource {
    private SilkDWriteTextFormatPtr nativeFormat;

    public TextFormat(
        DirectWriteFactory factory,
        string fontFamily,
        FontWeight fontWeight,
        FontStyle fontStyle,
        float fontSize
    ) {
        Factory = factory;
        FontFamily = fontFamily;
        FontWeight = fontWeight;
        FontStyle = fontStyle;
        FontSize = fontSize;
        if (factory.Handle != null) {
            IDWriteTextFormat* format = null;
            var family = fontFamily;
            var locale = string.Empty;
            fixed (char* familyPtr = family)
            fixed (char* localePtr = locale) {
                SilkMarshal.ThrowHResult(factory.Handle->CreateTextFormat(familyPtr,
                    null,
                    (Silk.NET.DirectWrite.FontWeight)
                    fontWeight,
                    (Silk.NET.DirectWrite.FontStyle)
                    fontStyle,
                    FontStretch.Normal,
                    fontSize,
                    localePtr,
                    &format));
                nativeFormat = new SilkDWriteTextFormatPtr(format);
                format->Release();
            }
        }
    }

    public DirectWriteFactory Factory { get; }

    public string FontFamily { get; }

    public FontWeight FontWeight { get; }

    public FontStyle FontStyle { get; }

    public float FontSize { get; }

    internal IDWriteTextFormat* Handle => nativeFormat.Handle;

    public override void Dispose() {
        nativeFormat.Dispose();
        base.Dispose();
    }
}

public sealed unsafe class TextLayout : D2DNativeResource {
    private SilkDWriteTextLayoutPtr nativeLayout;

    public TextLayout(
        DirectWriteFactory? factory,
        string text,
        TextFormat textFormat,
        float maxWidth,
        float maxHeight
    ) {
        Factory = factory;
        Text = text;
        TextFormat = textFormat;
        MaxWidth = maxWidth;
        MaxHeight = maxHeight;
        if (factory is { } actualFactory && actualFactory.Handle != null) {
            IDWriteTextLayout* layout = null;
            fixed (char* textPtr = Text) {
                SilkMarshal.ThrowHResult(actualFactory.Handle->CreateTextLayout(textPtr,
                    (uint) Text.Length,
                    textFormat.Handle,
                    NormalizeSize(maxWidth),
                    NormalizeSize(maxHeight),
                    &layout));
                nativeLayout = new SilkDWriteTextLayoutPtr(layout);
                layout->Release();
            }

            Silk.NET.DirectWrite.TextMetrics metrics = default;
            SilkMarshal.ThrowHResult(nativeLayout.Handle->GetMetrics(&metrics));
            Metrics = new TextMetrics {
                Width = metrics.Width,
                WidthIncludingTrailingWhitespace = metrics.WidthIncludingTrailingWhitespace,
                Height = metrics.Height
            };
        } else {
            var height = Math.Max(1, textFormat.FontSize);
            var width = Math.Min(float.IsInfinity(maxWidth) || maxWidth <= 0
                    ? float.MaxValue
                    : maxWidth,
                Text.Length * height * 0.55f);
            Metrics = new TextMetrics {
                Width = width,
                WidthIncludingTrailingWhitespace = width,
                Height = height
            };
        }
    }

    public DirectWriteFactory? Factory { get; }

    public string Text { get; }

    public TextFormat TextFormat { get; }

    public float MaxWidth { get; }

    public float MaxHeight { get; }

    public TextAlignment TextAlignment {
        get;
        set {
            field = value;
            if (nativeLayout.Handle != null)
                SilkMarshal.ThrowHResult(
                    nativeLayout.Handle->SetTextAlignment((Silk.NET.DirectWrite.TextAlignment) value));
        }
    } = TextAlignment.Leading;

    /// <summary>
    ///     Gets or sets the text reading direction used by native DirectWrite shaping.
    /// </summary>
    public FlowDirection FlowDirection {
        get;
        set {
            field = value;
            if (nativeLayout.Handle != null)
                SilkMarshal.ThrowHResult(
                    nativeLayout.Handle->SetReadingDirection(value == FlowDirection.LeftToRight
                        ? Silk.NET.DirectWrite.ReadingDirection.LeftToRight
                        : Silk.NET.DirectWrite.ReadingDirection.RightToLeft));
        }
    } = FlowDirection.LeftToRight;

    public TextMetrics Metrics { get; }

    internal IDWriteTextLayout* Handle => nativeLayout.Handle;

    public override void Dispose() {
        nativeLayout.Dispose();
        base.Dispose();
    }

    private static float NormalizeSize(float value) => float.IsInfinity(value) || value <= 0
        ? float.MaxValue
        : value;
}

public struct TextMetrics {
    public float Width;
    public float WidthIncludingTrailingWhitespace;
    public float Height;
}

public enum CapStyle {
    Flat,
    Square,
    Round,
    Triangle
}

public enum DashStyle {
    Solid,
    Dash,
    Dot,
    DashDot,
    DashDotDot,
    Custom
}

public enum LineJoin {
    Miter,
    Bevel,
    Round,
    MiterOrBevel
}

public enum SweepDirection { CounterClockwise, Clockwise }

public enum ArcSize { Small, Large }

public enum FigureBegin { Filled, Hollow }

public enum FigureEnd { Open, Closed }

[Flags]
public enum PathSegment { None = 0, ForceUnstroked = 1, ForceRoundLineJoin = 2 }

public enum D2DFillMode { Alternate, Winding }

public enum TextAlignment {
    Leading,
    Trailing,
    Center,
    Justified
}

public enum FlowDirection { LeftToRight, RightToLeft }

public enum FontWeight {
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

public enum FontStyle { Normal, Oblique, Italic }

[Flags]
public enum DrawTextOptions { None = 0 }

public enum BitmapInterpolationMode { NearestNeighbor, Linear }

public enum CompositeMode { SourceOver }
