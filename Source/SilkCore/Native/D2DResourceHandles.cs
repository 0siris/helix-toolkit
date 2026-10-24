using HelixToolkit.SharpDX.Core.Utilities;
/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

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

/// <summary>Retains existing stroke properties and lazily owns their Direct2D stroke style.</summary>
public sealed unsafe class StrokeStyle : D2DNativeResource {
    /// <summary>The borrowed factory whose native reference creates the style.</summary>
    private readonly D2DFactory factory;

    /// <summary>The style reference retained after first native use.</summary>
    private ComPtr<ID2D1StrokeStyle> nativeStyle;

    /// <summary>Retains the explicit stroke policy without allocating native resources yet.</summary>
    /// <param name="factory">The geometry factory used when the native style is requested.</param>
    /// <param name="properties">The existing cap, join and dash policy.</param>
    /// <param name="dashes">The existing dash array, or null for no custom dash array.</param>
    /// <exception cref="ArgumentNullException">The factory is null.</exception>
    public StrokeStyle(D2DFactory factory, StrokeStyleProperties properties, float[]? dashes = null) {
        ArgumentNullException.ThrowIfNull(factory);
        this.factory = factory;
        Properties = properties;
        Dashes = dashes ?? [];
    }

    /// <summary>Gets the explicit cap, join and dash policy supplied by the caller.</summary>
    public StrokeStyleProperties Properties { get; }

    /// <summary>Gets the existing custom dash array, read when the native style is first created.</summary>
    public float[] Dashes { get; }

    /// <summary>Gets the borrowed factory so geometry and stroke resources use the same native factory.</summary>
    internal D2DFactory Factory => factory;

    /// <summary>Gets the retained native style, creating it through the borrowed factory when needed.</summary>
    /// <exception cref="ObjectDisposedException">This style or its not-yet-used factory is disposed.</exception>
    internal ID2D1StrokeStyle* Handle {
        get {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if (nativeStyle.Handle != null) return nativeStyle.Handle;
            var properties = new Silk.NET.Direct2D.StrokeStyleProperties {
                StartCap = (Silk.NET.Direct2D.CapStyle) Properties.StartCap,
                EndCap = (Silk.NET.Direct2D.CapStyle) Properties.EndCap,
                DashCap = (Silk.NET.Direct2D.CapStyle) Properties.DashCap,
                LineJoin = (Silk.NET.Direct2D.LineJoin) Properties.LineJoin,
                MiterLimit = Properties.MiterLimit,
                DashStyle = (Silk.NET.Direct2D.DashStyle) Properties.DashStyle,
                DashOffset = Properties.DashOffset
            };
            ID2D1StrokeStyle* style = null;
            fixed (float* dashes = Dashes)
                SilkMarshal.ThrowHResult(factory.Handle->CreateStrokeStyle(
                    &properties, dashes, checked((uint) Dashes.Length), &style));
            nativeStyle = new ComPtr<ID2D1StrokeStyle>(style);
            style->Release();
            return nativeStyle.Handle;
        }
    }

    /// <summary>Returns this wrapper when it implements the requested managed resource type.</summary>
    /// <typeparam name="T">The managed resource type.</typeparam>
    /// <returns>This wrapper cast to the requested type, or null.</returns>
    public T? QueryInterface<T>()
        where T : class
        => this as T;

    /// <summary>Releases the retained style without disposing its borrowed factory.</summary>
    public override void Dispose() {
        if (IsDisposed) return;
        nativeStyle.Dispose();
        base.Dispose();
    }
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

    public Bitmap(Size2F size, object? nativeResource = null, byte[]? pixels = null) {
        Size = size;
        Pixels = pixels ?? new byte[checked(Width * Height * 4)];
        if (nativeResource is SilkD2DBitmapBasePtr bitmap) nativeBitmap = bitmap;
    }

    public Size2F Size { get; }

    public int Width => (int) Math.Ceiling(Size.Width);

    public int Height => (int) Math.Ceiling(Size.Height);

    internal byte[] Pixels { get; }

    internal ID2D1Bitmap* Handle => target?.Bitmap is { } bitmap
        ? (ID2D1Bitmap*) bitmap.Handle
        : nativeBitmap.Handle;

    public override void Dispose() {
        nativeBitmap.Dispose();
        target?.Dispose();
        target = null;
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

/// <summary>Owns a Direct2D path populated by the existing Figure and GeometrySink producers.</summary>
public sealed unsafe class PathGeometry : D2DNativeResource {
    /// <summary>The retained native path reference.</summary>
    private ComPtr<ID2D1PathGeometry> nativeGeometry;

    /// <summary>Creates one empty native path through the supplied geometry factory.</summary>
    /// <param name="factory">The borrowed geometry factory.</param>
    /// <exception cref="ArgumentNullException">The factory is null.</exception>
    public PathGeometry(D2DFactory factory) {
        ArgumentNullException.ThrowIfNull(factory);
        Factory = factory;
        ID2D1PathGeometry* geometry = null;
        SilkMarshal.ThrowHResult(factory.Handle->CreatePathGeometry(&geometry));
        nativeGeometry = new ComPtr<ID2D1PathGeometry>(geometry);
        geometry->Release();
    }

    /// <summary>Gets the borrowed factory used to create this path.</summary>
    public D2DFactory Factory { get; }

    /// <summary>Gets the native path while this wrapper remains alive.</summary>
    /// <exception cref="ObjectDisposedException">This path is disposed.</exception>
    internal ID2D1PathGeometry* Handle {
        get {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return nativeGeometry.Handle;
        }
    }

    /// <summary>Opens this path for its native figure and segment commands.</summary>
    /// <returns>A sink whose successful Close completes the path.</returns>
    public GeometrySink Open() => new(this);

    /// <summary>Releases the native path without disposing its borrowed factory.</summary>
    public override void Dispose() {
        if (IsDisposed) return;
        nativeGeometry.Dispose();
        base.Dispose();
    }
}

/// <summary>Forwards the existing figure and segment commands to one native Direct2D geometry sink.</summary>
public sealed unsafe class GeometrySink : D2DNativeResource {
    /// <summary>The native sink retained until explicit disposal.</summary>
    private ComPtr<ID2D1GeometrySink> nativeSink;

    /// <summary>Whether Close has already been attempted, including an HRESULT failure.</summary>
    private bool closed;

    /// <summary>Opens the supplied path and retains its native sink reference.</summary>
    /// <param name="geometry">The borrowed path being populated.</param>
    internal GeometrySink(PathGeometry geometry) {
        Geometry = geometry;
        ID2D1GeometrySink* sink = null;
        SilkMarshal.ThrowHResult(geometry.Handle->Open(&sink));
        nativeSink = new ComPtr<ID2D1GeometrySink>(sink);
        sink->Release();
    }

    /// <summary>Gets the borrowed path being populated by this sink.</summary>
    public PathGeometry Geometry { get; }

    /// <summary>Gets the fill rule most recently supplied to this sink.</summary>
    public D2DFillMode FillMode { get; private set; }

    /// <summary>Gets the open native sink, rejecting commands after Close or disposal.</summary>
    /// <exception cref="ObjectDisposedException">The sink has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Close has already been attempted.</exception>
    internal ID2D1GeometrySink* Handle {
        get {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if (closed) throw new InvalidOperationException("The geometry sink is closed");
            return nativeSink.Handle;
        }
    }

    /// <summary>Sets the native fill rule without adding or closing any figure.</summary>
    /// <param name="fillMode">The existing fill rule.</param>
    public void SetFillMode(D2DFillMode fillMode) {
        Handle->SetFillMode((Silk.NET.Direct2D.FillMode) fillMode);
        FillMode = fillMode;
    }

    /// <summary>Starts one native figure at the supplied local position.</summary>
    /// <param name="startPoint">The first local point.</param>
    /// <param name="figureBegin">Whether the figure participates in fill.</param>
    public void BeginFigure(Vector2 startPoint, FigureBegin figureBegin) =>
        Handle->BeginFigure(startPoint, (Silk.NET.Direct2D.FigureBegin) figureBegin);

    /// <summary>Sets the native flags for subsequent segments.</summary>
    /// <param name="flags">The existing stroke and join flags.</param>
    public void SetSegmentFlags(PathSegment flags) => Handle->SetSegmentFlags((Silk.NET.Direct2D.PathSegment) flags);

    /// <summary>Appends one connected native line endpoint.</summary>
    /// <param name="point">The next local endpoint, including any source duplicate.</param>
    public void AddLine(Vector2 point) => Handle->AddLine(point);

    /// <summary>Forwards one existing cubic Bezier command without implementing a new curve algorithm.</summary>
    /// <param name="segment">The two control points and final endpoint.</param>
    public void AddBezier(BezierSegmentData segment) {
        var value = new Silk.NET.Direct2D.BezierSegment {
            Point1 = segment.Point1, Point2 = segment.Point2, Point3 = segment.Point3
        };
        Handle->AddBezier(&value);
    }

    /// <summary>Forwards one existing arc command without adding a new tessellator.</summary>
    /// <param name="segment">The endpoint, radii and existing arc policy.</param>
    public void AddArc(ArcSegmentData segment) {
        var value = new Silk.NET.Direct2D.ArcSegment {
            Point = segment.Point,
            Size = new Vector2(segment.Size.Width, segment.Size.Height),
            RotationAngle = segment.RotationAngle,
            SweepDirection = (Silk.NET.Direct2D.SweepDirection) segment.SweepDirection,
            ArcSize = (Silk.NET.Direct2D.ArcSize) segment.ArcSize
        };
        Handle->AddArc(&value);
    }

    /// <summary>Ends the native figure with the caller's explicit open or closed policy.</summary>
    /// <param name="figureEnd">Whether the native figure closes back to its start.</param>
    public void EndFigure(FigureEnd figureEnd) => Handle->EndFigure((Silk.NET.Direct2D.FigureEnd) figureEnd);

    /// <summary>Completes the native path and checks all deferred sink errors.</summary>
    /// <exception cref="InvalidOperationException">Close has already been attempted.</exception>
    public void Close() {
        var sink = Handle;
        closed = true;
        SilkMarshal.ThrowHResult(sink->Close());
    }

    /// <summary>Releases the sink without implicitly completing an unfinished figure or path.</summary>
    public override void Dispose() {
        if (IsDisposed) return;
        nativeSink.Dispose();
        base.Dispose();
    }
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
