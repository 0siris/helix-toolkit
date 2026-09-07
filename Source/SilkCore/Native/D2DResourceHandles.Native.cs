/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Utilities.Buffers;
using Silk.NET.Core.Native;
using Silk.NET.Direct2D;
using Silk.NET.DirectWrite;
using Silk.NET.DXGI;
using Silk.NET.Maths;
using AlphaMode = Silk.NET.Direct2D.AlphaMode;
using FactoryType = Silk.NET.DirectWrite.FactoryType;
using IDWriteFactory = Silk.NET.DirectWrite.IDWriteFactory;
using SilkD2DBitmapBasePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct2D.ID2D1Bitmap>;
using SilkD2DBitmapPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct2D.ID2D1Bitmap1>;
using SilkD2DDeviceContextPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct2D.ID2D1DeviceContext>;
using SilkD2DDevicePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct2D.ID2D1Device>;
using SilkDWriteFactoryPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.DirectWrite.IDWriteFactory>;
using Media = System.Windows.Media;
using Imaging = System.Windows.Media.Imaging;

namespace HelixToolkit.SharpDX.Core.Native;

public abstract class D2DNativeResource : IDisposable {
    protected D2DNativeResource(object? nativeResource = null) {
        NativeResource = nativeResource;
    }

    public object? NativeResource { get; }

    public bool IsDisposed { get; private set; }

    public virtual void Dispose() {
        if (IsDisposed) return;

        if (NativeResource is IDisposable disposable)
            disposable.Dispose();

        IsDisposed = true;
        GC.SuppressFinalize(this);
    }
}

public sealed class D2DFactory : D2DNativeResource {
    public D2DFactory(object? nativeResource = null)
        : base(nativeResource) { }
}

public sealed unsafe class D2DDevice : D2DNativeResource {
    private static readonly D2D D2DApi = D2D.GetApi();
    private static readonly Guid DxgiDeviceGuid = new("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
    private SilkD2DDevicePtr nativeDevice;

    internal ID2D1Device* Handle => nativeDevice.Handle;

    public override void Dispose() {
        nativeDevice.Dispose();
        base.Dispose();
    }
}

public sealed unsafe class D2DDeviceContext : D2DNativeResource {
    private static readonly Guid DxgiSurfaceGuid = new("cafcb56c-6ac3-4889-bf47-9e23bbd260ec");
    private SilkD2DDeviceContextPtr nativeContext;
    private Media.DrawingContext? managedContext;
    private int managedWidth;
    private int managedHeight;

    public D2DDeviceContext(object? nativeResource = null) {
        if (nativeResource is D2DDevice device && device.Handle != null) {
            ID2D1DeviceContext* context = null;
            SilkMarshal.ThrowHResult(device.Handle->CreateDeviceContext(DeviceContextOptions.None, &context));
            nativeContext = new SilkD2DDeviceContextPtr(context);
            context->Release();
        }
    }

    public BitmapProxy? Target {
        get;
        set {
            field = value;
            if (nativeContext.Handle != null)
                nativeContext.Handle->SetTarget(value is null
                    ? null
                    : (ID2D1Image*) value.Bitmap.Handle);
        }
    }

    public D2DSizeF DotsPerInch { get; set; } = new(96, 96);

    public int MaximumBitmapSize { get; set; }

    public Matrix3X2 Transform {
        get;
        set {
            field = value;
            if (nativeContext.Handle != null) {
                var nativeTransform = new Matrix3X2<float>(value.M11,
                    value.M12,
                    value.M21,
                    value.M22,
                    value.M31,
                    value.M32);
                nativeContext.Handle->SetTransform(&nativeTransform);
            }
        }
    } = Matrix3X2.Identity;

    public D2DFactory Factory { get; set; } = new();

    internal ID2D1DeviceContext* NativeHandle => nativeContext.Handle;

    internal bool HasNativeContext => nativeContext.Handle != null;

    public void BeginDraw() {
        if (nativeContext.Handle != null) nativeContext.Handle->BeginDraw();
    }

    public void EndDraw() {
        if (nativeContext.Handle != null) SilkMarshal.ThrowHResult(nativeContext.Handle->EndDraw(null, null));
    }

    public void Clear(Color4 color) {
        if (nativeContext.Handle != null) {
            var value = ToSilkColor(color);
            nativeContext.Handle->Clear(&value);
        } else managedContext?.DrawRectangle(ToManagedBrush(color),
            null,
            new System.Windows.Rect(0, 0, managedWidth, managedHeight));
    }

    public void DrawRectangle(RectangleF rect, Brush brush, float strokeWidth, StrokeStyle? strokeStyle = null) {
        if (nativeContext.Handle != null && brush.Handle != null) {
            var value = ToSilkRect(rect);
            nativeContext.Handle->DrawRectangle(&value, brush.Handle, strokeWidth, null);
        }
    }

    public void FillRectangle(RectangleF rect, Brush brush) {
        if (nativeContext.Handle != null && brush.Handle != null) {
            var value = ToSilkRect(rect);
            nativeContext.Handle->FillRectangle(&value, brush.Handle);
        } else managedContext?.DrawRectangle(ToManagedBrush(brush, rect), null, ToManagedRect(rect));
    }

    public void DrawRoundedRectangle(
        RoundedRectangle rect,
        Brush brush,
        float strokeWidth,
        StrokeStyle? strokeStyle = null
    ) {
        if (nativeContext.Handle != null && brush.Handle != null) {
            var value = new RoundedRect(ToSilkRect(rect.Rect), rect.RadiusX, rect.RadiusY);
            nativeContext.Handle->DrawRoundedRectangle(&value, brush.Handle, strokeWidth, null);
        }
    }

    public void FillRoundedRectangle(RoundedRectangle rect, Brush brush) {
        if (nativeContext.Handle != null && brush.Handle != null) {
            var value = new RoundedRect(ToSilkRect(rect.Rect), rect.RadiusX, rect.RadiusY);
            nativeContext.Handle->FillRoundedRectangle(&value, brush.Handle);
        }
    }

    public void DrawEllipse(Ellipse ellipse, Brush brush, float strokeWidth, StrokeStyle? strokeStyle = null) {
        if (nativeContext.Handle != null && brush.Handle != null) {
            var value = new Silk.NET.Direct2D.Ellipse(ellipse.Point, ellipse.RadiusX, ellipse.RadiusY);
            nativeContext.Handle->DrawEllipse(&value, brush.Handle, strokeWidth, null);
        }
    }

    public void FillEllipse(Ellipse ellipse, Brush brush) {
        if (nativeContext.Handle != null && brush.Handle != null) {
            var value = new Silk.NET.Direct2D.Ellipse(ellipse.Point, ellipse.RadiusX, ellipse.RadiusY);
            nativeContext.Handle->FillEllipse(&value, brush.Handle);
        }
    }

    public void DrawGeometry(
        PathGeometry geometry,
        Brush brush,
        float strokeWidth,
        StrokeStyle? strokeStyle = null
    ) { }

    public void FillGeometry(PathGeometry geometry, Brush brush) { }

    public void DrawImage(
        BitmapProxy image,
        Vector2 targetOffset,
        RectangleF imageRectangle,
        BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.Linear,
        CompositeMode compositeMode = CompositeMode.SourceOver
    ) {
        if (nativeContext.Handle != null && image.Bitmap.Handle != null) {
            var destination = new Box2D<float>(targetOffset.X,
                targetOffset.Y,
                targetOffset.X + imageRectangle.Width,
                targetOffset.Y + imageRectangle.Height);
            var source = ToSilkRect(imageRectangle);
            nativeContext.Handle->DrawBitmap((ID2D1Bitmap*) image.Bitmap.Handle,
                &destination,
                1,
                (Silk.NET.Direct2D.BitmapInterpolationMode) interpolationMode,
                &source);
        }
    }

    public void DrawBitmap(
        Bitmap bitmap,
        RectangleF destinationRectangle,
        float opacity,
        BitmapInterpolationMode interpolationMode
    ) {
        if (nativeContext.Handle != null && bitmap.Handle != null) {
            var destination = ToSilkRect(destinationRectangle);
            nativeContext.Handle->DrawBitmap(bitmap.Handle,
                &destination,
                opacity,
                (Silk.NET.Direct2D.BitmapInterpolationMode) interpolationMode,
                null);
        } else if (managedContext is { } context) {
            var source = Imaging.BitmapSource.Create(bitmap.Width,
                bitmap.Height,
                96,
                96,
                Media.PixelFormats.Pbgra32,
                null,
                bitmap.Pixels,
                bitmap.Width * 4);
            context.DrawImage(source, ToManagedRect(destinationRectangle));
        }
    }

    public void DrawTextLayout(
        Vector2 origin,
        TextLayout textLayout,
        Brush brush,
        DrawTextOptions options = DrawTextOptions.None
    ) {
        if (nativeContext.Handle != null && textLayout.Handle != null && brush.Handle != null)
            nativeContext.Handle->DrawTextLayout(origin,
                (Silk.NET.Direct2D.IDWriteTextLayout*) textLayout.Handle,
                brush.Handle,
                Silk.NET.Direct2D.DrawTextOptions.None);
        else if (managedContext is { } context && brush is SolidColorBrush solidBrush) {
            var format = textLayout.TextFormat;
            var typeface = new Media.Typeface(new Media.FontFamily(format.FontFamily),
                format.FontStyle == FontStyle.Italic ? System.Windows.FontStyles.Italic : System.Windows.FontStyles.Normal,
                System.Windows.FontWeight.FromOpenTypeWeight((int) format.FontWeight),
                System.Windows.FontStretches.Normal);
            var text = new Media.FormattedText(textLayout.Text,
                System.Globalization.CultureInfo.CurrentUICulture,
                textLayout.FlowDirection == FlowDirection.LeftToRight
                    ? System.Windows.FlowDirection.LeftToRight
                    : System.Windows.FlowDirection.RightToLeft,
                typeface,
                format.FontSize,
                ToManagedBrush(solidBrush.Color),
                1);
            context.DrawText(text, new System.Windows.Point(origin.X, origin.Y));
        }
    }

    internal void BeginManagedDraw(Media.DrawingContext context, int width, int height) {
        managedContext = context;
        managedWidth = width;
        managedHeight = height;
    }

    internal void EndManagedDraw() {
        managedContext = null;
        managedWidth = 0;
        managedHeight = 0;
    }

    internal Bitmap CreateBitmap(byte[] pixels, int width, int height, int stride) {
        if (nativeContext.Handle == null || pixels.Length == 0)
            return new Bitmap(new Size2F(width, height));

        fixed (byte* data = pixels) {
            var properties = new BitmapProperties {
                PixelFormat = new PixelFormat(Format.FormatB8G8R8A8Unorm, AlphaMode.Premultiplied),
                DpiX = DotsPerInch.Width,
                DpiY = DotsPerInch.Height
            };
            ID2D1Bitmap* bitmap = null;
            SilkMarshal.ThrowHResult(nativeContext.Handle->CreateBitmap(
                new Vector2D<uint>((uint) width, (uint) height),
                data,
                (uint) stride,
                &properties,
                &bitmap));
            var result = new Bitmap(new Size2F(width, height), new SilkD2DBitmapBasePtr(bitmap), pixels);
            bitmap->Release();
            return result;
        }
    }

    public override void Dispose() {
        Target = null;
        nativeContext.Dispose();
        base.Dispose();
    }

    private static Box2D<float> ToSilkRect(RectangleF rect) => new(rect.Left, rect.Top, rect.Right, rect.Bottom);

    private static D3Dcolorvalue ToSilkColor(Color4 color) => new(color.X, color.Y, color.Z, color.W);

    private static System.Windows.Rect ToManagedRect(RectangleF rect) =>
        new(rect.Left, rect.Top, rect.Width, rect.Height);

    private static Media.Brush ToManagedBrush(Brush brush, RectangleF bounds) => brush switch {
        SolidColorBrush solid => ToManagedBrush(solid.Color),
        LinearGradientBrush linear => new Media.LinearGradientBrush(
            new Media.GradientStopCollection(linear.GradientStops.Gradients.Select(stop =>
                new Media.GradientStop(ToManagedColor(stop.Color), stop.Position))),
            new System.Windows.Point(linear.Properties.StartPoint.X, linear.Properties.StartPoint.Y),
            new System.Windows.Point(linear.Properties.EndPoint.X, linear.Properties.EndPoint.Y)) {
            MappingMode = Media.BrushMappingMode.Absolute
        },
        RadialGradientBrush radial => new Media.RadialGradientBrush(
            new Media.GradientStopCollection(radial.GradientStops.Gradients.Select(stop =>
                new Media.GradientStop(ToManagedColor(stop.Color), stop.Position)))) {
            MappingMode = Media.BrushMappingMode.Absolute,
            Center = new System.Windows.Point(radial.Properties.Center.X, radial.Properties.Center.Y),
            GradientOrigin = new System.Windows.Point(
                radial.Properties.Center.X + radial.Properties.GradientOriginOffset.X,
                radial.Properties.Center.Y + radial.Properties.GradientOriginOffset.Y),
            RadiusX = radial.Properties.RadiusX,
            RadiusY = radial.Properties.RadiusY
        },
        _ => Media.Brushes.Transparent
    };

    private static Media.SolidColorBrush ToManagedBrush(Color4 color) => new(ToManagedColor(color));

    private static System.Windows.Media.Color ToManagedColor(Color4 color) =>
        System.Windows.Media.Color.FromScRgb(color.W, color.X, color.Y, color.Z);
}

public sealed class WicImagingFactory : D2DNativeResource {
    public WicImagingFactory(object? nativeResource = null)
        : base(nativeResource) { }
}

public sealed unsafe class DirectWriteFactory : D2DNativeResource {
    private static readonly DWrite DWriteApi = DWrite.GetApi();
    private static readonly Guid FactoryGuid = new("b859ee5a-d838-4b5b-a2e8-1adc7d93db48");
    private SilkDWriteFactoryPtr nativeFactory;

    public DirectWriteFactory(object? nativeResource = null) {
        IUnknown* factory = null;
        var guid = FactoryGuid;
        SilkMarshal.ThrowHResult(DWriteApi.DWriteCreateFactory(FactoryType.Shared, &guid, &factory));
        nativeFactory = new SilkDWriteFactoryPtr((IDWriteFactory*) factory);
        factory->Release();
    }

    internal IDWriteFactory* Handle => nativeFactory.Handle;

    public override void Dispose() {
        nativeFactory.Dispose();
        base.Dispose();
    }
}

public sealed class D2DColorContext : D2DNativeResource {
    public D2DColorContext(object? nativeResource = null)
        : base(nativeResource) { }
}

public sealed unsafe class D2DBitmap : D2DNativeResource {
    private SilkD2DBitmapPtr nativeBitmap;

    public D2DBitmap(Size2 size, object? nativeResource = null) {
        Size = new D2DSizeF(size.Width, size.Height);
        if (nativeResource is SilkD2DBitmapPtr bitmap) nativeBitmap = bitmap;
    }

    public D2DSizeF Size { get; }

    internal ID2D1Bitmap1* Handle => nativeBitmap.Handle;

    public override void Dispose() {
        nativeBitmap.Dispose();
        base.Dispose();
    }
}

[Flags]
public enum D2DBitmapOptions { None = 0, Target = 1, CannotDraw = 2 }

public enum D2DAlphaMode {
    Unknown = 0,
    Premultiplied = 1,
    Straight = 2,
    Ignore = 3
}

public struct D2DPixelFormat {
    public D2DPixelFormat(Format format, D2DAlphaMode alphaMode) {
        Format = format;
        AlphaMode = alphaMode;
    }

    public Format Format;
    public D2DAlphaMode AlphaMode;
}

public sealed class D2DBitmapProperties {
    public D2DBitmapProperties(
        D2DPixelFormat pixelFormat,
        float dpiX,
        float dpiY,
        D2DBitmapOptions options,
        D2DColorContext? colorContext = null
    ) {
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

    public D2DColorContext? ColorContext { get; }
}

public readonly struct D2DSizeF {
    public D2DSizeF(float width, float height) {
        Width = width;
        Height = height;
    }

    public float Width { get; }

    public float Height { get; }
}
