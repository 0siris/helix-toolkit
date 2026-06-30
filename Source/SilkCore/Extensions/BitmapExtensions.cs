/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using HelixToolkit.SharpDX.Core.Native;
using Microsoft.Extensions.Logging;

#if !NETFX_CORE
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
#endif

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
    using Utilities.ImagePacker;

    public enum Direct2DImageFormat
    {
        Png, Gif, Ico, Jpeg, Wmp, Tiff, Bmp
    }
    public static class BitmapExtensions
    {
        static readonly ILogger logger = Logger.LogManager.Create(nameof(BitmapExtensions));

        private static class ImageContainerFormats
        {
            public static readonly Guid Bmp = new Guid("0af1d87e-fcfe-4188-bdeb-a7906471cbe3");
            public static readonly Guid Png = new Guid("1b7cfaf4-713f-473c-bbcd-6137425faeaf");
            public static readonly Guid Ico = new Guid("a3a860c4-338f-4c17-919a-fba4b5628f21");
            public static readonly Guid Jpeg = new Guid("19e4a5aa-5662-4fc5-a0c0-1758028e1057");
            public static readonly Guid Wmp = new Guid("57a37caa-367a-4540-916b-f183c5093a4b");
            public static readonly Guid Tiff = new Guid("163bcc30-e2e9-4f0b-961d-a3e9fdb788a3");
            public static readonly Guid Gif = new Guid("1f8a5601-7d4d-4cbd-9c82-1bc8d4eeb9a5");
        }

        public static MemoryStream ToBitmapStream(this string text, int fontSize, Color4 foreground,
            Color4 background, string fontFamily, FontWeight fontWeight, FontStyle fontStyle, Vector4 padding, ref float width, ref float height, bool predefinedSize,
            IDevice2DResources deviceResources)
        {
            using (var layout = GetTextLayoutMetrices(text, deviceResources, fontSize, fontFamily, fontWeight, fontStyle))
            {
                var metrices = layout.Metrics;
                if (!predefinedSize)
                {
                    width = (float)Math.Ceiling(metrices.WidthIncludingTrailingWhitespace + padding.X + padding.Z);
                    height = (float)Math.Ceiling(metrices.Height + padding.Y + padding.W);
                }
                else
                {
                    var scale = width / height;
                    width = (float)Math.Ceiling(metrices.WidthIncludingTrailingWhitespace + padding.X + padding.Z);
                    height = width / scale;
                }

                using (var bitmap = CreateBitmapStream(deviceResources, (int)width, (int)height, Direct2DImageFormat.Bmp, (target) =>
                 {
                     target.Clear(background);
                     using (var brush = new SolidColorBrush(target, foreground))
                     {
                         target.DrawTextLayout(new Vector2(padding.X, padding.Y), layout, brush);
                     }
                 }))
                {
                    return bitmap.ToMemoryStream(deviceResources, Direct2DImageFormat.Bmp);
                }
            }
        }

        public static TextLayout GetTextLayoutMetrices(this string text, IDevice2DResources deviceResources, int fontSize, string fontFamily, FontWeight fontWeight, FontStyle fontStyle,
            float maxWidth = float.MaxValue, float maxHeight = float.MaxValue)
        {
            using (var format = new TextFormat(deviceResources.DirectWriteFactory, fontFamily, fontWeight, fontStyle, fontSize))
            {
                return new TextLayout(deviceResources.DirectWriteFactory, text, format, maxWidth, maxHeight);
            }
        }

        public static Guid ToWICImageFormat(this Direct2DImageFormat format)
        {
            switch (format)
            {
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

        public static Bitmap CreateBitmapStream(IDevice2DResources deviceResources, int width, int height, Direct2DImageFormat imageType, Action<D2DDeviceContext> drawingAction)
        {
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            if (deviceResources is not IDevice3DResources device3D
                || device3D.NativeDeviceResources?.Device == null
                || deviceResources.DeviceContext2D?.HasNativeContext != true)
            {
                return new Bitmap(new Size2F(width, height));
            }

            var texture = device3D.NativeDeviceResources.Device.CreateTexture2D(new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.FormatB8G8R8A8Unorm,
                SampleDescription = new SampleDescription(1, 0),
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default
            });
            Utilities.BitmapProxy target = null;
            try
            {
                var context = deviceResources.DeviceContext2D;
                var properties = Utilities.BitmapProxy.CreateDescription(
                    context.DotsPerInch.Width,
                    context.DotsPerInch.Height,
                    Format.FormatB8G8R8A8Unorm,
                    D2DAlphaMode.Premultiplied,
                    D2DBitmapOptions.Target);
                var nativeBitmap = context.CreateTargetBitmap(texture, properties);
                target = new Utilities.BitmapProxy(
                    nameof(BitmapExtensions),
                    context,
                    new Size2(width, height),
                    properties,
                    nativeBitmap);
                var previousTarget = context.Target;
                try
                {
                    context.Target = target;
                    context.Transform = Matrix3x2.Identity;
                    context.BeginDraw();
                    drawingAction?.Invoke(context);
                    context.EndDraw();
                }
                finally
                {
                    context.Target = previousTarget;
                }

                var bitmap = new Bitmap(new Size2F(width, height), texture, target);
                texture = null;
                target = null;
                return bitmap;
            }
            finally
            {
                target?.Dispose();
                texture?.Dispose();
            }
        }


        public static MemoryStream ToMemoryStream(this Bitmap bitmap,
            IDevice2DResources deviceResources,
            Direct2DImageFormat imageType = Direct2DImageFormat.Bmp)
        {
            if (bitmap == null)
            {
                return null;
            }

            if (bitmap.Texture != null && deviceResources is IDeviceResources resources)
            {
                var stream = new MemoryStream();
                if (Utilities.ScreenCapture.SaveWICTextureToStream(resources, bitmap.Texture, stream, imageType))
                {
                    return stream;
                }
                stream.Dispose();
            }

            var width = Math.Max(1, bitmap.Width);
            var height = Math.Max(1, bitmap.Height);
            var stride = width * 4;
            var pixelDataSize = stride * height;
            var systemStream = new MemoryStream(54 + pixelDataSize);
            using (var writer = new BinaryWriter(systemStream, System.Text.Encoding.UTF8, true))
            {
                writer.Write((byte)'B');
                writer.Write((byte)'M');
                writer.Write(54 + pixelDataSize);
                writer.Write((short)0);
                writer.Write((short)0);
                writer.Write(54);
                writer.Write(40);
                writer.Write(width);
                writer.Write(-height);
                writer.Write((short)1);
                writer.Write((short)32);
                writer.Write(0);
                writer.Write(pixelDataSize);
                writer.Write(96 * 39);
                writer.Write(96 * 39);
                writer.Write(0);
                writer.Write(0);
                writer.Write(new byte[pixelDataSize]);
            }

            systemStream.Position = 0;
            return systemStream;
        }

        public static MemoryStream CreateSolidColorBitmapStream(IDevice2DResources deviceResources,
            int width, int height, Direct2DImageFormat imageType, Color4 color)
        {
            using (var bmp = CreateBitmapStream(deviceResources, width, height, imageType, (target) =>
            {
                using (var brush = new SolidColorBrush(target, color, new BrushProperties() { Opacity = color.GetAlpha() }))
                {
                    target.FillRectangle(new RectangleF(0, 0, width, height), brush);
                }
            }))
            {
                return bmp.ToMemoryStream(deviceResources, imageType);
            }
        }

        public static MemoryStream CreateLinearGradientBitmapStream(IDevice2DResources deviceResources,
            int width, int height, Direct2DImageFormat imageType, Vector2 startPoint, Vector2 endPoint, GradientStop[] gradients,
            ExtendMode extendMode = ExtendMode.Clamp, Gamma gamma = Gamma.StandardRgb)
        {
#if !NETFX_CORE
            return CreateWpfGradientBitmapStream(
                width,
                height,
                imageType,
                new System.Windows.Media.LinearGradientBrush(
                    ToWpfGradientStops(gradients),
                    new Point(startPoint.X, startPoint.Y),
                    new Point(endPoint.X, endPoint.Y))
                {
                    MappingMode = BrushMappingMode.Absolute,
                    SpreadMethod = ToWpfSpreadMethod(extendMode)
                });
#else
            using (var bmp = CreateBitmapStream(deviceResources, width, height, imageType, (target) =>
             {
                 using (var gradientCol = new GradientStopCollection(target, gradients, gamma, extendMode))
                 {
                     using (var brush = new LinearGradientBrush(target, new LinearGradientBrushProperties()
                     {
                         StartPoint = startPoint,
                         EndPoint = endPoint
                     }, gradientCol))
                     {
                         target.FillRectangle(new RectangleF(0, 0, width, height), brush);
                     }
                 }
             }))
            {
                return bmp.ToMemoryStream(deviceResources, imageType);
            }
#endif
        }

        public static MemoryStream CreateRadiusGradientBitmapStream(IDevice2DResources deviceResources,
            int width, int height, Direct2DImageFormat imageType, Vector2 center, Vector2 gradientOriginOffset,
            float radiusX, float radiusY, GradientStop[] gradients,
            ExtendMode extendMode = ExtendMode.Clamp, Gamma gamma = Gamma.StandardRgb)
        {
#if !NETFX_CORE
            return CreateWpfGradientBitmapStream(
                width,
                height,
                imageType,
                new System.Windows.Media.RadialGradientBrush(ToWpfGradientStops(gradients))
                {
                    MappingMode = BrushMappingMode.Absolute,
                    Center = new Point(center.X, center.Y),
                    GradientOrigin = new Point(center.X + gradientOriginOffset.X, center.Y + gradientOriginOffset.Y),
                    RadiusX = radiusX,
                    RadiusY = radiusY,
                    SpreadMethod = ToWpfSpreadMethod(extendMode)
                });
#else
            using (var bmp = CreateBitmapStream(deviceResources, width, height, imageType, (target) =>
            {
                using (var gradientCol = new GradientStopCollection(target, gradients, gamma, extendMode))
                {
                    using (var brush = new RadialGradientBrush(target, new RadialGradientBrushProperties()
                    {
                        Center = center,
                        GradientOriginOffset = gradientOriginOffset,
                        RadiusX = radiusX,
                        RadiusY = radiusY,
                    }, gradientCol))
                    {
                        target.FillRectangle(new RectangleF(0, 0, width, height), brush);
                    }
                }
            }))
            {
                return bmp.ToMemoryStream(deviceResources, imageType);
            }
#endif
        }

#if !NETFX_CORE
        private static MemoryStream CreateWpfGradientBitmapStream(
            int width,
            int height,
            Direct2DImageFormat imageType,
            System.Windows.Media.Brush brush)
        {
            var visual = new DrawingVisual();
            using (var drawingContext = visual.RenderOpen())
            {
                drawingContext.DrawRectangle(brush, null, new Rect(0, 0, width, height));
            }
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            BitmapEncoder encoder = imageType switch
            {
                Direct2DImageFormat.Bmp => new BmpBitmapEncoder(),
                Direct2DImageFormat.Gif => new GifBitmapEncoder(),
                Direct2DImageFormat.Jpeg => new JpegBitmapEncoder(),
                Direct2DImageFormat.Png => new PngBitmapEncoder(),
                Direct2DImageFormat.Tiff => new TiffBitmapEncoder(),
                Direct2DImageFormat.Wmp => new WmpBitmapEncoder(),
                _ => throw new NotSupportedException($"WPF encoding does not support {imageType}.")
            };
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            var stream = new MemoryStream();
            encoder.Save(stream);
            stream.Position = 0;
            return stream;
        }

        private static System.Windows.Media.GradientStopCollection ToWpfGradientStops(GradientStop[] gradients)
        {
            var result = new System.Windows.Media.GradientStopCollection();
            foreach (var gradient in gradients ?? Array.Empty<GradientStop>())
            {
                result.Add(new System.Windows.Media.GradientStop(
                    System.Windows.Media.Color.FromScRgb(
                        gradient.Color.W,
                        gradient.Color.X,
                        gradient.Color.Y,
                        gradient.Color.Z),
                    gradient.Position));
            }
            return result;
        }

        private static GradientSpreadMethod ToWpfSpreadMethod(ExtendMode extendMode)
        {
            return extendMode switch
            {
                ExtendMode.Wrap => GradientSpreadMethod.Repeat,
                ExtendMode.Mirror => GradientSpreadMethod.Reflect,
                _ => GradientSpreadMethod.Pad
            };
        }
#endif

        public static MemoryStream CreateViewBoxTexture(IDevice2DResources deviceResources, string front, string back, string left, string right, string top, string down,
            Color4 frontFaceColor, Color4 backFaceColor, Color4 leftFaceColor, Color4 rightFaceColor, Color4 topFaceColor, Color4 bottomFaceColor,
            Color4 frontTextColor, Color4 backTextColor, Color4 leftTextColor, Color4 rightTextColor, Color4 topTextColor, Color4 bottomTextColor,
            string fontFamily = "Arial",
            FontWeight fontWeight = FontWeight.SemiBold, FontStyle fontStyle = FontStyle.Normal, int fontSize = 64, int faceSize = 100)
        {
            using (var bmp = CreateBitmapStream(deviceResources, faceSize * 6, faceSize, Direct2DImageFormat.Bmp, (target) =>
             {
                 target.Clear(new Color4(0, 0, 0, 1));
                 var faceRect = new RectangleF(0, 0, faceSize, faceSize);
                 var faceColors = new Color4[] { frontFaceColor, backFaceColor, leftFaceColor, rightFaceColor, topFaceColor, bottomFaceColor };
                 var textColors = new Color4[] { frontTextColor, backTextColor, leftTextColor, rightTextColor, topTextColor, bottomTextColor };
                 var texts = new string[] { front, back, right, left, top, down };
                 for (var i = 0; i < 6; ++i)
                 {
                     using (var layout = GetTextLayoutMetrices(texts[i], deviceResources, fontSize, fontFamily, fontWeight, fontStyle, faceSize, faceSize))
                     {
                         var metrices = layout.Metrics;
                         var offset = new Vector2((faceSize - metrices.WidthIncludingTrailingWhitespace) / 2, (faceSize - metrices.Height) / 2);
                         offset.X += faceRect.Left;
                         using (var brush = new SolidColorBrush(target, faceColors[i]))
                         {
                             target.FillRectangle(faceRect, brush);
                         }
                         using (var brush = new SolidColorBrush(target, textColors[i]))
                         {
                             target.DrawTextLayout(offset, layout, brush);
                         }
                     }
                     faceRect.Left += faceSize;
                     faceRect.Width = faceSize;
                 }
             }))
            {
                return bmp.ToMemoryStream(deviceResources, Direct2DImageFormat.Bmp);
            }
        }

        /// <summary>
        /// Create a <see cref="BillboardImage3D"/> from a list of <see cref="TextInfoExt"/>
        /// <para>
        /// This is used to create a batched text billboard with a single merged texture. 
        /// And use <see cref="BillboardImage3D"/> for rendering.
        /// </para>
        /// <para>This is designed to substitute <see cref="BillboardSingleText3D"/> or <see cref="BillboardText3D"/>
        /// when user needs to render many different texts with different text properties (such as font style, font size, etc) and languages
        /// </para>
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="effectsManager">The effects manager.</param>
        /// <param name="maxWidth">The maximum width.</param>
        /// <param name="maxHeight">The maximum height.</param>
        /// <param name="squareImage">if set to <c>true</c> [square image].</param>
        /// <returns></returns>
        public static BillboardImage3D ToBillboardImage3D(this IEnumerable<TextInfoExt> items,
            IEffectsManager effectsManager, int maxWidth = 2048, int maxHeight = 2048, bool squareImage = true)
        {
            using (var imagePacker = new TextInfoExtPacker(effectsManager))
            {
                var code = imagePacker.Pack(items, true, squareImage, maxWidth, maxHeight, 2,
                    out var bitmap, out var imageWidth, out var imageHeight,
                    out var map);
                if (code == ImagePackReturnCode.Succeed)
                {
                    using (bitmap)
                    {
                        var stream = bitmap.ToMemoryStream(effectsManager, Direct2DImageFormat.Png);
                        var model = new BillboardImage3D(stream);
                        foreach (var imageInfo in items.Select((x, i) =>
                         {
                             var rect = map[i];
                             return new ImageInfo()
                             {
                                 Width = rect.Width,
                                 Height = rect.Height,
                                 Position = x.Origin,
                                 UV_TopLeft = new Vector2(rect.Left / imageWidth, rect.Top / imageHeight),
                                 UV_BottomRight = new Vector2(rect.Right / imageWidth, rect.Bottom / imageHeight),
                                 HorizontalAlignment = x.HorizontalAlignment,
                                 VerticalAlignment = x.VerticalAlignment,
                                 Scale = x.Scale,
                             };
                         }))
                        {

                            model.ImageInfos.Add(imageInfo);
                        }
                        return model;
                    }
                }
                else
                {
                    logger.LogError("Failed to pack TextInfoExts, Error Code = {0}", code.ToString());
                    return null;
                }
            }
        }
    }
}
