/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Text;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;
using HelixToolkit.SharpDX.Core.Utilities.ImagePacker;

namespace HelixToolkit.SharpDX.Core.Extensions;

public enum Direct2DImageFormat {
    Png,
    Gif,
    Ico,
    Jpeg,
    Wmp,
    Tiff,
    Bmp
}

public static class BitmapExtensions {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;

    private static class ImageContainerFormats {
        public static readonly Guid Bmp = new("0af1d87e-fcfe-4188-bdeb-a7906471cbe3");
        public static readonly Guid Png = new("1b7cfaf4-713f-473c-bbcd-6137425faeaf");
        public static readonly Guid Ico = new("a3a860c4-338f-4c17-919a-fba4b5628f21");
        public static readonly Guid Jpeg = new("19e4a5aa-5662-4fc5-a0c0-1758028e1057");
        public static readonly Guid Wmp = new("57a37caa-367a-4540-916b-f183c5093a4b");
        public static readonly Guid Tiff = new("163bcc30-e2e9-4f0b-961d-a3e9fdb788a3");
        public static readonly Guid Gif = new("1f8a5601-7d4d-4cbd-9c82-1bc8d4eeb9a5");
    }

    public static MemoryStream ToBitmapStream(
        this string text,
        int fontSize,
        Color4 foreground,
        Color4 background,
        string fontFamily,
        FontWeight fontWeight,
        FontStyle fontStyle,
        Vector4 padding,
        ref float width,
        ref float height,
        bool predefinedSize,
        IDevice2DResources deviceResources
    ) {
        using var layout = text.GetTextLayoutMetrices(deviceResources, fontSize, fontFamily, fontWeight, fontStyle);
        var metrices = layout.Metrics;
        if (!predefinedSize) {
            width = (float)Math.Ceiling(metrices.WidthIncludingTrailingWhitespace + padding.X + padding.Z);
            height = (float)Math.Ceiling(metrices.Height + padding.Y + padding.W);
        } else {
            var scale = width / height;
            width = (float)Math.Ceiling(metrices.WidthIncludingTrailingWhitespace + padding.X + padding.Z);
            height = width / scale;
        }

        using var bitmap = CreateBitmapStream(deviceResources,
                                               (int)width,
                                               (int)height,
                                               Direct2DImageFormat.Bmp,
                                               target => {
                                                   target.Clear(background);
                                                   using var brush = new SolidColorBrush(target, foreground);
                                                   target.DrawTextLayout(
                                                           new Vector2(padding.X, padding.Y),
                                                           layout,
                                                           brush);
                                               });
        return bitmap is { } validBitmap
            ? validBitmap.ToMemoryStream(deviceResources)
            : new MemoryStream();
    }

    public static TextLayout GetTextLayoutMetrices(
        this string text,
        IDevice2DResources deviceResources,
        int fontSize,
        string fontFamily,
        FontWeight fontWeight,
        FontStyle fontStyle,
        float maxWidth = float.MaxValue,
        float maxHeight = float.MaxValue
    ) {
        using var format =
               new TextFormat(deviceResources.DirectWriteFactory, fontFamily, fontWeight, fontStyle, fontSize);
        return new TextLayout(deviceResources.DirectWriteFactory, text, format, maxWidth, maxHeight);
    }

    public static Guid ToWicImageFormat(this Direct2DImageFormat format) {
        switch (format) {
            case Direct2DImageFormat.Bmp:
                return ImageContainerFormats.Bmp;
            case Direct2DImageFormat.Ico:
                return ImageContainerFormats.Ico;
            case Direct2DImageFormat.Gif:
                return ImageContainerFormats.Gif;
            case Direct2DImageFormat.Jpeg:
                return ImageContainerFormats.Jpeg;
            case Direct2DImageFormat.Png:
                return ImageContainerFormats.Png;
            case Direct2DImageFormat.Tiff:
                return ImageContainerFormats.Tiff;
            case Direct2DImageFormat.Wmp:
                return ImageContainerFormats.Wmp;
        }

        throw new NotSupportedException();
    }

    public static Bitmap? CreateBitmapStream(
        IDevice2DResources deviceResources,
        int width,
        int height,
        Direct2DImageFormat imageType,
        Action<D2DDeviceContext> drawingAction
    ) {
        if (width <= 0 || height <= 0) return null;

        var visual = new System.Windows.Media.DrawingVisual();
        using (var drawingContext = visual.RenderOpen()) {
            var context = deviceResources.DeviceContext2D;
            context.BeginManagedDraw(drawingContext, width, height);
            try {
                drawingAction(context);
            } finally {
                context.EndManagedDraw();
            }
        }

        var target = new System.Windows.Media.Imaging.RenderTargetBitmap(width,
            height,
            96,
            96,
            System.Windows.Media.PixelFormats.Pbgra32);
        target.Render(visual);
        var pixels = new byte[checked(width * height * 4)];
        target.CopyPixels(pixels, width * 4, 0);
        return new Bitmap(new Size2F(width, height), pixels: pixels);
    }


    public static MemoryStream ToMemoryStream(
        this Bitmap bitmap,
        IDevice2DResources deviceResources,
        Direct2DImageFormat imageType = Direct2DImageFormat.Bmp
    ) {
        var width = Math.Max(1, bitmap.Width);
        var height = Math.Max(1, bitmap.Height);
        var stride = width * 4;
        var source = System.Windows.Media.Imaging.BitmapSource.Create(width,
            height,
            96,
            96,
            System.Windows.Media.PixelFormats.Pbgra32,
            null,
            bitmap.Pixels,
            stride);
        var systemStream = new MemoryStream();
        if (imageType == Direct2DImageFormat.Ico) {
            using var imageStream = new MemoryStream();
            var png = new System.Windows.Media.Imaging.PngBitmapEncoder();
            png.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(source));
            png.Save(imageStream);
            using var writer = new BinaryWriter(systemStream, Encoding.UTF8, true);
            writer.Write((ushort) 0);
            writer.Write((ushort) 1);
            writer.Write((ushort) 1);
            writer.Write(checked((byte) (width == 256 ? 0 : width)));
            writer.Write(checked((byte) (height == 256 ? 0 : height)));
            writer.Write((byte) 0);
            writer.Write((byte) 0);
            writer.Write((ushort) 1);
            writer.Write((ushort) 32);
            writer.Write(checked((uint) imageStream.Length));
            writer.Write(22u);
            writer.Write(imageStream.GetBuffer(), 0, checked((int) imageStream.Length));
        } else {
            System.Windows.Media.Imaging.BitmapEncoder encoder = imageType switch {
                Direct2DImageFormat.Png => new System.Windows.Media.Imaging.PngBitmapEncoder(),
                Direct2DImageFormat.Gif => new System.Windows.Media.Imaging.GifBitmapEncoder(),
                Direct2DImageFormat.Jpeg => new System.Windows.Media.Imaging.JpegBitmapEncoder(),
                Direct2DImageFormat.Wmp => new System.Windows.Media.Imaging.WmpBitmapEncoder(),
                Direct2DImageFormat.Tiff => new System.Windows.Media.Imaging.TiffBitmapEncoder(),
                Direct2DImageFormat.Bmp => new System.Windows.Media.Imaging.BmpBitmapEncoder(),
                _ => throw new ArgumentOutOfRangeException(nameof(imageType))
            };
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(source));
            encoder.Save(systemStream);
        }

        systemStream.Position = 0;
        return systemStream;
    }

    public static MemoryStream CreateSolidColorBitmapStream(
        IDevice2DResources deviceResources,
        int width,
        int height,
        Direct2DImageFormat imageType,
        Color4 color
    ) {
        using var bmp = CreateBitmapStream(deviceResources,
                                            width,
                                            height,
                                            imageType,
                                            target => {
                                                using var brush =
                                                       new SolidColorBrush(
                                                           target,
                                                           color,
                                                           new BrushProperties { Opacity = color.GetAlpha() });
                                                target.FillRectangle(new RectangleF(0, 0, width, height), brush);
                                            });
        return bmp is { } validBitmap
            ? validBitmap.ToMemoryStream(deviceResources, imageType)
            : new MemoryStream();
    }

    public static MemoryStream CreateLinearGradientBitmapStream(
        IDevice2DResources deviceResources,
        int width,
        int height,
        Direct2DImageFormat imageType,
        Vector2 startPoint,
        Vector2 endPoint,
        GradientStop[] gradients,
        ExtendMode extendMode = ExtendMode.Clamp,
        Gamma gamma = Gamma.StandardRgb
    ) {
        using var bmp = CreateBitmapStream(deviceResources,
                                            width,
                                            height,
                                            imageType,
                                            target => {
                                                using var gradientCol =
                                                       new GradientStopCollection(
                                                           target,
                                                           gradients,
                                                           gamma,
                                                           extendMode);
                                                using var brush = new LinearGradientBrush(target,
                                                           new LinearGradientBrushProperties {
                                                               StartPoint = startPoint,
                                                               EndPoint = endPoint
                                                           },
                                                           gradientCol);
                                                target.FillRectangle(
                                                    new RectangleF(0, 0, width, height),
                                                    brush);
                                            });
        return bmp is { } validBitmap
            ? validBitmap.ToMemoryStream(deviceResources, imageType)
            : new MemoryStream();
    }

    public static MemoryStream CreateRadiusGradientBitmapStream(
        IDevice2DResources deviceResources,
        int width,
        int height,
        Direct2DImageFormat imageType,
        Vector2 center,
        Vector2 gradientOriginOffset,
        float radiusX,
        float radiusY,
        GradientStop[] gradients,
        ExtendMode extendMode = ExtendMode.Clamp,
        Gamma gamma = Gamma.StandardRgb
    ) {
        using var bmp = CreateBitmapStream(deviceResources,
                                            width,
                                            height,
                                            imageType,
                                            target => {
                                                using var gradientCol =
                                                       new GradientStopCollection(
                                                           target,
                                                           gradients,
                                                           gamma,
                                                           extendMode);
                                                using var brush = new RadialGradientBrush(target,
                                                           new RadialGradientBrushProperties {
                                                               Center = center,
                                                               GradientOriginOffset = gradientOriginOffset,
                                                               RadiusX = radiusX,
                                                               RadiusY = radiusY
                                                           },
                                                           gradientCol);
                                                target.FillRectangle(
                                                    new RectangleF(0, 0, width, height),
                                                    brush);
                                            });
        return bmp is { } validBitmap
            ? validBitmap.ToMemoryStream(deviceResources, imageType)
            : new MemoryStream();
    }


    public static MemoryStream CreateViewBoxTexture(
        IDevice2DResources deviceResources,
        string front,
        string back,
        string left,
        string right,
        string top,
        string down,
        Color4 frontFaceColor,
        Color4 backFaceColor,
        Color4 leftFaceColor,
        Color4 rightFaceColor,
        Color4 topFaceColor,
        Color4 bottomFaceColor,
        Color4 frontTextColor,
        Color4 backTextColor,
        Color4 leftTextColor,
        Color4 rightTextColor,
        Color4 topTextColor,
        Color4 bottomTextColor,
        string fontFamily = "Arial",
        FontWeight fontWeight = FontWeight.SemiBold,
        FontStyle fontStyle = FontStyle.Normal,
        int fontSize = 64,
        int faceSize = 100
    ) {
        using var bmp = CreateBitmapStream(deviceResources,
                                            faceSize * 6,
                                            faceSize,
                                            Direct2DImageFormat.Bmp,
                                            target => {
                                                target.Clear(new Color4(0, 0, 0, 1));
                                                var faceRect = new RectangleF(0, 0, faceSize, faceSize);
                                                var faceColors = new[] {
                                                    frontFaceColor, backFaceColor, leftFaceColor, rightFaceColor,
                                                    topFaceColor, bottomFaceColor
                                                };
                                                var textColors = new[] {
                                                    frontTextColor, backTextColor, leftTextColor, rightTextColor,
                                                    topTextColor, bottomTextColor
                                                };
                                                var texts = new[] { front, back, right, left, top, down };
                                                for (var i = 0; i < 6; ++i) {
                                                    using (var layout = texts[i].GetTextLayoutMetrices(deviceResources,
                                                               fontSize,
                                                               fontFamily,
                                                               fontWeight,
                                                               fontStyle,
                                                               faceSize,
                                                               faceSize)) {
                                                        var metrices = layout.Metrics;
                                                        var offset = new Vector2(
                                                            (faceSize - metrices.WidthIncludingTrailingWhitespace) / 2,
                                                            (faceSize - metrices.Height) / 2);
                                                        offset.X += faceRect.Left;
                                                        using (var brush = new SolidColorBrush(target, faceColors[i])) {
                                                            target.FillRectangle(faceRect, brush);
                                                        }

                                                        using (var brush = new SolidColorBrush(target, textColors[i])) {
                                                            target.DrawTextLayout(offset, layout, brush);
                                                        }
                                                    }

                                                    faceRect.Left += faceSize;
                                                    faceRect.Width = faceSize;
                                                }
                                            });
        return bmp is { } validBitmap
            ? validBitmap.ToMemoryStream(deviceResources)
            : new MemoryStream();
    }

    public static TextureModel? CreateViewBoxTextureModel(
        IDevice2DResources deviceResources,
        string front,
        string back,
        string left,
        string right,
        string top,
        string down,
        Color4 frontFaceColor,
        Color4 backFaceColor,
        Color4 leftFaceColor,
        Color4 rightFaceColor,
        Color4 topFaceColor,
        Color4 bottomFaceColor,
        Color4 frontTextColor,
        Color4 backTextColor,
        Color4 leftTextColor,
        Color4 rightTextColor,
        Color4 topTextColor,
        Color4 bottomTextColor,
        string fontFamily = "Arial",
        FontWeight fontWeight = FontWeight.SemiBold,
        FontStyle fontStyle = FontStyle.Normal,
        int fontSize = 64,
        int faceSize = 100
    ) {
        using var bmp = CreateBitmapStream(deviceResources,
                                            faceSize * 6,
                                            faceSize,
                                            Direct2DImageFormat.Bmp,
                                            target => {
                                                target.Clear(new Color4(0, 0, 0, 1));
                                                var faceRect = new RectangleF(0, 0, faceSize, faceSize);
                                                var faceColors = new[] {
                                                    frontFaceColor, backFaceColor, leftFaceColor, rightFaceColor,
                                                    topFaceColor, bottomFaceColor
                                                };
                                                var textColors = new[] {
                                                    frontTextColor, backTextColor, leftTextColor, rightTextColor,
                                                    topTextColor, bottomTextColor
                                                };
                                                var texts = new[] { front, back, right, left, top, down };
                                                for (var i = 0; i < 6; ++i) {
                                                    using (var layout = texts[i].GetTextLayoutMetrices(deviceResources,
                                                               fontSize,
                                                               fontFamily,
                                                               fontWeight,
                                                               fontStyle,
                                                               faceSize,
                                                               faceSize)) {
                                                        var metrices = layout.Metrics;
                                                        var offset = new Vector2(
                                                            (faceSize - metrices.WidthIncludingTrailingWhitespace) / 2,
                                                            (faceSize - metrices.Height) / 2);
                                                        offset.X += faceRect.Left;
                                                        using (var brush = new SolidColorBrush(target, faceColors[i])) {
                                                            target.FillRectangle(faceRect, brush);
                                                        }

                                                        using (var brush = new SolidColorBrush(target, textColors[i])) {
                                                            target.DrawTextLayout(offset, layout, brush);
                                                        }
                                                    }

                                                    faceRect.Left += faceSize;
                                                    faceRect.Width = faceSize;
                                                }
                                            });
        return bmp?.ToTextureModel(deviceResources);
    }

    private static TextureModel ToTextureModel(this Bitmap bitmap, IDevice2DResources deviceResources) {
        var width = Math.Max(1, bitmap.Width);
        var height = Math.Max(1, bitmap.Height);
        return new TextureModel(bitmap.Pixels, Format.FormatB8G8R8A8Unorm, width, height);
    }

    /// <summary>
    ///     Create a <see cref="BillboardImage3D" /> from a list of <see cref="TextInfoExt" />
    ///     <para>
    ///         This is used to create a batched text billboard with a single merged texture.
    ///         And use <see cref="BillboardImage3D" /> for rendering.
    ///     </para>
    ///     <para>
    ///         This is designed to substitute <see cref="BillboardSingleText3D" /> or <see cref="BillboardText3D" />
    ///         when user needs to render many different texts with different text properties (such as font style, font size,
    ///         etc) and languages
    ///     </para>
    /// </summary>
    /// <param name="items">The items.</param>
    /// <param name="effectsManager">The effects manager.</param>
    /// <param name="maxWidth">The maximum width.</param>
    /// <param name="maxHeight">The maximum height.</param>
    /// <param name="squareImage">if set to <c>true</c> [square image].</param>
    /// <returns></returns>
    public static BillboardImage3D? ToBillboardImage3D(
        this ICollection<TextInfoExt> items,
        IEffectsManager effectsManager,
        int maxWidth = 2048,
        int maxHeight = 2048,
        bool squareImage = true
    ) {
        using var imagePacker = new TextInfoExtPacker(effectsManager);
        var code = imagePacker.Pack(items,
                                    true,
                                    squareImage,
                                    maxWidth,
                                    maxHeight,
                                    2,
                                    out var bitmap,
                                    out var imageWidth,
                                    out var imageHeight,
                                    out var map);
        
        if (code == ImagePackReturnCode.Succeed && bitmap is { } packedBitmap && map is { } imageMap)
            using (packedBitmap) {
                var stream = packedBitmap.ToMemoryStream(effectsManager, Direct2DImageFormat.Png);
                var model = new BillboardImage3D(stream);
                foreach (var imageInfo in items.Select((x, i) => {
                    var rect = imageMap[i];
                    return new ImageInfo {
                        Width = rect.Width,
                        Height = rect.Height,
                        Position = x.Origin,
                        UvTopLeft = new Vector2(rect.Left / imageWidth, rect.Top / imageHeight),
                        UvBottomRight = new Vector2(rect.Right / imageWidth, rect.Bottom / imageHeight),
                        HorizontalAlignment = x.HorizontalAlignment,
                        VerticalAlignment = x.VerticalAlignment,
                        Scale = x.Scale
                    };
                }))
                    model.ImageInfos.Add(imageInfo);
                return model;
            }

        Logger.Error("Failed to pack TextInfoExts, Error Code = {Value0}", code.ToString());
        return null;
    }
}
