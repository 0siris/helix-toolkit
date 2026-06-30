/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct2D;
using Silk.NET.DirectWrite;
using Silk.NET.DXGI;
using Silk.NET.Maths;
using SilkD2DDevicePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct2D.ID2D1Device>;
using SilkD2DDeviceContextPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct2D.ID2D1DeviceContext>;
using SilkD2DBitmapBasePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct2D.ID2D1Bitmap>;
using SilkD2DBitmapPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct2D.ID2D1Bitmap1>;
using SilkD2DSolidBrushPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct2D.ID2D1SolidColorBrush>;
using SilkDWriteFactoryPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.DirectWrite.IDWriteFactory>;
using SilkDWriteTextFormatPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.DirectWrite.IDWriteTextFormat>;
using SilkDWriteTextLayoutPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.DirectWrite.IDWriteTextLayout>;

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

        public unsafe sealed class D2DDevice : D2DNativeResource
        {
            private static readonly D2D D2DApi = D2D.GetApi();
            private static readonly Guid DxgiDeviceGuid = new Guid("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
            private SilkD2DDevicePtr nativeDevice;

            public D2DDevice(object nativeResource = null)
            {
                if (nativeResource is Native.SilkD3DDevice d3dDevice)
                {
                    IDXGIDevice* dxgiDevice = null;
                    var guid = DxgiDeviceGuid;
                    SilkMarshal.ThrowHResult(d3dDevice.Handle->QueryInterface(&guid, (void**)&dxgiDevice));
                    try
                    {
                        ID2D1Device* device = null;
                        SilkMarshal.ThrowHResult(D2DApi.D2D1CreateDevice(dxgiDevice, null, &device));
                        nativeDevice = new SilkD2DDevicePtr(device);
                        device->Release();
                    }
                    finally
                    {
                        dxgiDevice->Release();
                    }
                }
            }

            internal ID2D1Device* Handle => nativeDevice.Handle;

            public override void Dispose()
            {
                nativeDevice.Dispose();
                base.Dispose();
            }
        }

        public unsafe sealed class D2DDeviceContext : D2DNativeResource
        {
            private static readonly Guid DxgiSurfaceGuid = new Guid("cafcb56c-6ac3-4889-bf47-9e23bbd260ec");
            private SilkD2DDeviceContextPtr nativeContext;
            private BitmapProxy target;
            private Matrix3x2 transform = Matrix3x2.Identity;

            public D2DDeviceContext(object nativeResource = null)
            {
                if (nativeResource is D2DDevice device && device.Handle != null)
                {
                    ID2D1DeviceContext* context = null;
                    SilkMarshal.ThrowHResult(device.Handle->CreateDeviceContext(DeviceContextOptions.None, &context));
                    nativeContext = new SilkD2DDeviceContextPtr(context);
                    context->Release();
                }
            }

            public BitmapProxy Target
            {
                get => target;
                set
                {
                    target = value;
                    if (nativeContext.Handle != null)
                    {
                        nativeContext.Handle->SetTarget((ID2D1Image*)value?.Bitmap?.Handle);
                    }
                }
            }

            public D2DSizeF DotsPerInch { get; set; } = new D2DSizeF(96, 96);

            public int MaximumBitmapSize { get; set; }

            public Matrix3x2 Transform
            {
                get => transform;
                set
                {
                    transform = value;
                    if (nativeContext.Handle != null)
                    {
                        var nativeTransform = new Matrix3X2<float>(
                            value.M11,
                            value.M12,
                            value.M21,
                            value.M22,
                            value.M31,
                            value.M32);
                        nativeContext.Handle->SetTransform(&nativeTransform);
                    }
                }
            }

            public D2DFactory Factory { get; set; } = new D2DFactory();

            internal ID2D1DeviceContext* NativeHandle => nativeContext.Handle;

            internal bool HasNativeContext => nativeContext.Handle != null;

            public void BeginDraw()
            {
                if (nativeContext.Handle != null)
                {
                    nativeContext.Handle->BeginDraw();
                }
            }

            public void EndDraw()
            {
                if (nativeContext.Handle != null)
                {
                    SilkMarshal.ThrowHResult(nativeContext.Handle->EndDraw(null, null));
                }
            }

            public void Clear(Color4 color)
            {
                if (nativeContext.Handle != null)
                {
                    var value = ToSilkColor(color);
                    nativeContext.Handle->Clear(&value);
                }
            }

            public void DrawRectangle(RectangleF rect, Brush brush, float strokeWidth, StrokeStyle strokeStyle = null)
            {
                if (nativeContext.Handle != null && brush?.Handle != null)
                {
                    var value = ToSilkRect(rect);
                    nativeContext.Handle->DrawRectangle(&value, brush.Handle, strokeWidth, null);
                }
            }

            public void FillRectangle(RectangleF rect, Brush brush)
            {
                if (nativeContext.Handle != null && brush?.Handle != null)
                {
                    var value = ToSilkRect(rect);
                    nativeContext.Handle->FillRectangle(&value, brush.Handle);
                }
            }

            public void DrawRoundedRectangle(RoundedRectangle rect, Brush brush, float strokeWidth, StrokeStyle strokeStyle = null)
            {
                if (nativeContext.Handle != null && brush?.Handle != null)
                {
                    var value = new Silk.NET.Direct2D.RoundedRect(ToSilkRect(rect.Rect), rect.RadiusX, rect.RadiusY);
                    nativeContext.Handle->DrawRoundedRectangle(&value, brush.Handle, strokeWidth, null);
                }
            }

            public void FillRoundedRectangle(RoundedRectangle rect, Brush brush)
            {
                if (nativeContext.Handle != null && brush?.Handle != null)
                {
                    var value = new Silk.NET.Direct2D.RoundedRect(ToSilkRect(rect.Rect), rect.RadiusX, rect.RadiusY);
                    nativeContext.Handle->FillRoundedRectangle(&value, brush.Handle);
                }
            }

            public void DrawEllipse(Ellipse ellipse, Brush brush, float strokeWidth, StrokeStyle strokeStyle = null)
            {
                if (nativeContext.Handle != null && brush?.Handle != null)
                {
                    var value = new Silk.NET.Direct2D.Ellipse(ellipse.Point, ellipse.RadiusX, ellipse.RadiusY);
                    nativeContext.Handle->DrawEllipse(&value, brush.Handle, strokeWidth, null);
                }
            }

            public void FillEllipse(Ellipse ellipse, Brush brush)
            {
                if (nativeContext.Handle != null && brush?.Handle != null)
                {
                    var value = new Silk.NET.Direct2D.Ellipse(ellipse.Point, ellipse.RadiusX, ellipse.RadiusY);
                    nativeContext.Handle->FillEllipse(&value, brush.Handle);
                }
            }

            public void DrawGeometry(PathGeometry geometry, Brush brush, float strokeWidth, StrokeStyle strokeStyle = null)
            {
            }

            public void FillGeometry(PathGeometry geometry, Brush brush)
            {
            }

            public void DrawImage(Utilities.BitmapProxy image, Vector2 targetOffset, RectangleF imageRectangle, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.Linear, CompositeMode compositeMode = CompositeMode.SourceOver)
            {
                if (nativeContext.Handle != null && image?.Bitmap?.Handle != null)
                {
                    var destination = new Box2D<float>(
                        targetOffset.X,
                        targetOffset.Y,
                        targetOffset.X + imageRectangle.Width,
                        targetOffset.Y + imageRectangle.Height);
                    var source = ToSilkRect(imageRectangle);
                    nativeContext.Handle->DrawBitmap(
                        (ID2D1Bitmap*)image.Bitmap.Handle,
                        &destination,
                        1,
                        (Silk.NET.Direct2D.BitmapInterpolationMode)interpolationMode,
                        &source);
                }
            }

            public void DrawBitmap(Bitmap bitmap, RectangleF destinationRectangle, float opacity, BitmapInterpolationMode interpolationMode)
            {
                if (nativeContext.Handle != null && bitmap?.Handle != null)
                {
                    var destination = ToSilkRect(destinationRectangle);
                    nativeContext.Handle->DrawBitmap(
                        bitmap.Handle,
                        &destination,
                        opacity,
                        (Silk.NET.Direct2D.BitmapInterpolationMode)interpolationMode,
                        null);
                }
            }

            public void DrawTextLayout(Vector2 origin, TextLayout textLayout, Brush brush, DrawTextOptions options = DrawTextOptions.None)
            {
                if (nativeContext.Handle != null && textLayout?.Handle != null && brush?.Handle != null)
                {
                    nativeContext.Handle->DrawTextLayout(origin, (Silk.NET.Direct2D.IDWriteTextLayout*)textLayout.Handle, brush.Handle, Silk.NET.Direct2D.DrawTextOptions.None);
                }
            }

            internal D2DBitmap CreateTargetBitmap(Texture2D texture, D2DBitmapProperties properties)
            {
                if (nativeContext.Handle == null || texture == null)
                {
                    return new D2DBitmap(texture == null ? default : new Size2(texture.Description.Width, texture.Description.Height));
                }

                IDXGISurface* surface = null;
                var guid = DxgiSurfaceGuid;
                SilkMarshal.ThrowHResult(texture.Handle->QueryInterface(&guid, (void**)&surface));
                try
                {
                    var bitmapProperties = new BitmapProperties1
                    {
                        PixelFormat = new Silk.NET.Direct2D.PixelFormat(properties.PixelFormat.Format, (Silk.NET.Direct2D.AlphaMode)properties.PixelFormat.AlphaMode),
                        DpiX = properties.DpiX,
                        DpiY = properties.DpiY,
                        BitmapOptions = (BitmapOptions)properties.Options
                    };
                    ID2D1Bitmap1* bitmap = null;
                    SilkMarshal.ThrowHResult(nativeContext.Handle->CreateBitmapFromDxgiSurface(surface, &bitmapProperties, &bitmap));
                    var result = new D2DBitmap(new Size2(texture.Description.Width, texture.Description.Height), new SilkD2DBitmapPtr(bitmap));
                    bitmap->Release();
                    return result;
                }
                finally
                {
                    surface->Release();
                }
            }

            internal Bitmap CreateBitmap(byte[] pixels, int width, int height, int stride)
            {
                if (nativeContext.Handle == null || pixels == null || pixels.Length == 0)
                {
                    return new Bitmap(new Size2F(width, height));
                }

                fixed (byte* data = pixels)
                {
                    var properties = new Silk.NET.Direct2D.BitmapProperties
                    {
                        PixelFormat = new Silk.NET.Direct2D.PixelFormat(Format.FormatB8G8R8A8Unorm, Silk.NET.Direct2D.AlphaMode.Premultiplied),
                        DpiX = DotsPerInch.Width,
                        DpiY = DotsPerInch.Height
                    };
                    ID2D1Bitmap* bitmap = null;
                    SilkMarshal.ThrowHResult(nativeContext.Handle->CreateBitmap(
                        new Vector2D<uint>((uint)width, (uint)height),
                        data,
                        (uint)stride,
                        &properties,
                        &bitmap));
                    var result = new Bitmap(new Size2F(width, height), new SilkD2DBitmapBasePtr(bitmap));
                    bitmap->Release();
                    return result;
                }
            }

            public override void Dispose()
            {
                Target = null;
                nativeContext.Dispose();
                base.Dispose();
            }

            private static Box2D<float> ToSilkRect(RectangleF rect)
            {
                return new Box2D<float>(rect.Left, rect.Top, rect.Right, rect.Bottom);
            }

            private static D3Dcolorvalue ToSilkColor(Color4 color)
            {
                return new D3Dcolorvalue(color.X, color.Y, color.Z, color.W);
            }
        }

        public sealed class WICImagingFactory : D2DNativeResource
        {
            public WICImagingFactory(object nativeResource = null)
                : base(nativeResource)
            {
            }
        }

        public unsafe sealed class DirectWriteFactory : D2DNativeResource
        {
            private static readonly DWrite DWriteApi = DWrite.GetApi();
            private static readonly Guid FactoryGuid = new Guid("b859ee5a-d838-4b5b-a2e8-1adc7d93db48");
            private SilkDWriteFactoryPtr nativeFactory;

            public DirectWriteFactory(object nativeResource = null)
            {
                IUnknown* factory = null;
                var guid = FactoryGuid;
                SilkMarshal.ThrowHResult(DWriteApi.DWriteCreateFactory(Silk.NET.DirectWrite.FactoryType.Shared, &guid, &factory));
                nativeFactory = new SilkDWriteFactoryPtr((Silk.NET.DirectWrite.IDWriteFactory*)factory);
                factory->Release();
            }

            internal Silk.NET.DirectWrite.IDWriteFactory* Handle => nativeFactory.Handle;

            public override void Dispose()
            {
                nativeFactory.Dispose();
                base.Dispose();
            }
        }

        public sealed class D2DColorContext : D2DNativeResource
        {
            public D2DColorContext(object nativeResource = null)
                : base(nativeResource)
            {
            }
        }

        public unsafe sealed class D2DBitmap : D2DNativeResource
        {
            private SilkD2DBitmapPtr nativeBitmap;

            public D2DBitmap(Size2 size, object nativeResource = null)
            {
                Size = new D2DSizeF(size.Width, size.Height);
                if (nativeResource is SilkD2DBitmapPtr bitmap)
                {
                    nativeBitmap = bitmap;
                }
            }

            public D2DSizeF Size { get; }

            internal ID2D1Bitmap1* Handle => nativeBitmap.Handle;

            public override void Dispose()
            {
                nativeBitmap.Dispose();
                base.Dispose();
            }
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

    public unsafe class Brush : Native.D2DNativeResource
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

        internal virtual ID2D1Brush* Handle => null;
    }

    public unsafe sealed class SolidColorBrush : Brush
    {
        private SilkD2DSolidBrushPtr nativeBrush;

        public SolidColorBrush(Native.D2DDeviceContext context, Color4 color)
        {
            Color = color;
            Create(context, color, 1);
        }

        public SolidColorBrush(Native.D2DDeviceContext context, Color4 color, BrushProperties properties)
        {
            Color = color;
            Properties = properties;
            Create(context, color, properties.Opacity);
        }

        public Color4 Color { get; }

        public BrushProperties Properties { get; }

        internal override ID2D1Brush* Handle => (ID2D1Brush*)nativeBrush.Handle;

        public override void Dispose()
        {
            nativeBrush.Dispose();
            base.Dispose();
        }

        private void Create(Native.D2DDeviceContext context, Color4 color, float opacity)
        {
            if (context?.NativeHandle == null)
            {
                return;
            }

            var value = new D3Dcolorvalue(color.X, color.Y, color.Z, color.W);
            var properties = new Silk.NET.Direct2D.BrushProperties { Opacity = opacity };
            ID2D1SolidColorBrush* brush = null;
            SilkMarshal.ThrowHResult(context.NativeHandle->CreateSolidColorBrush(&value, &properties, &brush));
            nativeBrush = new SilkD2DSolidBrushPtr(brush);
            brush->Release();
        }
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

    public unsafe sealed class Bitmap : Native.D2DNativeResource
    {
        private SilkD2DBitmapBasePtr nativeBitmap;
        private Utilities.BitmapProxy target;
        private Texture2D texture;

        public Bitmap(Size2F size, object nativeResource = null)
        {
            Size = size;
            if (nativeResource is SilkD2DBitmapBasePtr bitmap)
            {
                nativeBitmap = bitmap;
            }
        }

        internal Bitmap(Size2F size, Texture2D texture, Utilities.BitmapProxy target)
        {
            Size = size;
            this.texture = texture;
            this.target = target;
        }

        public Size2F Size { get; }

        public int Width => (int)Math.Ceiling(Size.Width);

        public int Height => (int)Math.Ceiling(Size.Height);

        internal ID2D1Bitmap* Handle => target?.Bitmap?.Handle != null
            ? (ID2D1Bitmap*)target.Bitmap.Handle
            : nativeBitmap.Handle;

        internal Texture2D Texture => texture;

        public override void Dispose()
        {
            nativeBitmap.Dispose();
            target?.Dispose();
            target = null;
            texture?.Dispose();
            texture = null;
            base.Dispose();
        }
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

    public unsafe sealed class TextFormat : Native.D2DNativeResource
    {
        private SilkDWriteTextFormatPtr nativeFormat;

        public TextFormat(Native.DirectWriteFactory factory, string fontFamily, FontWeight fontWeight, FontStyle fontStyle, float fontSize)
        {
            Factory = factory;
            FontFamily = fontFamily;
            FontWeight = fontWeight;
            FontStyle = fontStyle;
            FontSize = fontSize;
            if (factory?.Handle != null)
            {
                Silk.NET.DirectWrite.IDWriteTextFormat* format = null;
                var family = fontFamily ?? "Arial";
                var locale = string.Empty;
                fixed (char* familyPtr = family)
                fixed (char* localePtr = locale)
                {
                    SilkMarshal.ThrowHResult(factory.Handle->CreateTextFormat(
                        familyPtr,
                        null,
                        (Silk.NET.DirectWrite.FontWeight)fontWeight,
                        (Silk.NET.DirectWrite.FontStyle)fontStyle,
                        FontStretch.Normal,
                        fontSize,
                        localePtr,
                        &format));
                    nativeFormat = new SilkDWriteTextFormatPtr(format);
                    format->Release();
                }
            }
        }

        public Native.DirectWriteFactory Factory { get; }

        public string FontFamily { get; }

        public FontWeight FontWeight { get; }

        public FontStyle FontStyle { get; }

        public float FontSize { get; }

        internal Silk.NET.DirectWrite.IDWriteTextFormat* Handle => nativeFormat.Handle;

        public override void Dispose()
        {
            nativeFormat.Dispose();
            base.Dispose();
        }
    }

    public unsafe sealed class TextLayout : Native.D2DNativeResource
    {
        private SilkDWriteTextLayoutPtr nativeLayout;
        private TextAlignment textAlignment = TextAlignment.Leading;

        public TextLayout(Native.DirectWriteFactory factory, string text, TextFormat textFormat, float maxWidth, float maxHeight)
        {
            Factory = factory;
            Text = text ?? string.Empty;
            TextFormat = textFormat;
            MaxWidth = maxWidth;
            MaxHeight = maxHeight;
            if (factory?.Handle != null && textFormat?.Handle != null)
            {
                Silk.NET.DirectWrite.IDWriteTextLayout* layout = null;
                fixed (char* textPtr = Text)
                {
                    SilkMarshal.ThrowHResult(factory.Handle->CreateTextLayout(
                        textPtr,
                        (uint)Text.Length,
                        textFormat.Handle,
                        NormalizeSize(maxWidth),
                        NormalizeSize(maxHeight),
                        &layout));
                    nativeLayout = new SilkDWriteTextLayoutPtr(layout);
                    layout->Release();
                }
                Silk.NET.DirectWrite.TextMetrics metrics = default;
                SilkMarshal.ThrowHResult(nativeLayout.Handle->GetMetrics(&metrics));
                Metrics = new TextMetrics
                {
                    Width = metrics.Width,
                    WidthIncludingTrailingWhitespace = metrics.WidthIncludingTrailingWhitespace,
                    Height = metrics.Height
                };
            }
            else
            {
                var height = Math.Max(1, textFormat?.FontSize ?? 12);
                var width = Math.Min(float.IsInfinity(maxWidth) || maxWidth <= 0 ? float.MaxValue : maxWidth, Text.Length * height * 0.55f);
                Metrics = new TextMetrics { Width = width, WidthIncludingTrailingWhitespace = width, Height = height };
            }
        }

        public Native.DirectWriteFactory Factory { get; }

        public string Text { get; }

        public TextFormat TextFormat { get; }

        public float MaxWidth { get; }

        public float MaxHeight { get; }

        public TextAlignment TextAlignment
        {
            get => textAlignment;
            set
            {
                textAlignment = value;
                if (nativeLayout.Handle != null)
                {
                    SilkMarshal.ThrowHResult(nativeLayout.Handle->SetTextAlignment((Silk.NET.DirectWrite.TextAlignment)value));
                }
            }
        }

        public TextMetrics Metrics { get; }

        internal Silk.NET.DirectWrite.IDWriteTextLayout* Handle => nativeLayout.Handle;

        public override void Dispose()
        {
            nativeLayout.Dispose();
            base.Dispose();
        }

        private static float NormalizeSize(float value)
        {
            return float.IsInfinity(value) || value <= 0 ? float.MaxValue : value;
        }
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
