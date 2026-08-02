/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HelixToolkit.Logger;
using Microsoft.Extensions.Logging;

namespace SharpDX.Toolkit.Graphics;

internal static class WICHelper {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;

    /// <summary>
    ///     Loads the first frame of an image supported by WIC.
    /// </summary>
    /// <remarks>Animated GIFs and multi-page TIFFs are intentionally loaded as frame 0 only.</remarks>
    public static Image LoadFromWICMemory(nint pSource, int size, bool makeACopy, GCHandle? handle) {
        if (pSource == nint.Zero || size <= 0) return null;

        var encoded = new byte[size];
        Marshal.Copy(pSource, encoded, 0, size);
        try {
            using var stream = new MemoryStream(encoded, false);
            var decoder = BitmapDecoder.Create(stream,
                                               BitmapCreateOptions.PreservePixelFormat,
                                               BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count == 0) return null;
            if (decoder.Frames.Count > 1)
                Logger.Warn("WIC image contains {FrameCount} frames; only frame 0 is loaded.", decoder.Frames.Count);

            BitmapSource source = decoder.Frames[0];
            if (source.Format != PixelFormats.Bgra32)
                source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

            var stride = checked(source.PixelWidth * 4);
            var image = Image.New2D(source.PixelWidth, source.PixelHeight, 1, PixelFormat.B8G8R8A8.UNorm);
            try {
                source.CopyPixels(Int32Rect.Empty, image.DataPointer, image.TotalSizeInBytes, stride);
                return image;
            } catch {
                image.Dispose();
                throw;
            }
        } catch (FileFormatException ex) {
            Logger.Warn(ex, "WIC could not decode the image data.");
            return null;
        } catch (NotSupportedException ex) {
            Logger.Warn(ex, "WIC does not support the image data.");
            return null;
        }
    }

    public static void SaveGifToWICMemory(
        PixelBuffer[] pixelBuffers,
        int count,
        ImageDescription description,
        Stream imageStream
    ) {
        SaveToWICMemory(pixelBuffers, count, description, imageStream, ImageFileType.Gif);
    }

    public static void SaveTiffToWICMemory(
        PixelBuffer[] pixelBuffers,
        int count,
        ImageDescription description,
        Stream imageStream
    ) {
        SaveToWICMemory(pixelBuffers, count, description, imageStream, ImageFileType.Tiff);
    }

    public static void SaveBmpToWICMemory(
        PixelBuffer[] pixelBuffers,
        int count,
        ImageDescription description,
        Stream imageStream
    ) {
        SaveToWICMemory(pixelBuffers, count, description, imageStream, ImageFileType.Bmp);
    }

    public static void SaveJpgToWICMemory(
        PixelBuffer[] pixelBuffers,
        int count,
        ImageDescription description,
        Stream imageStream
    ) {
        SaveToWICMemory(pixelBuffers, count, description, imageStream, ImageFileType.Jpg);
    }

    public static void SavePngToWICMemory(
        PixelBuffer[] pixelBuffers,
        int count,
        ImageDescription description,
        Stream imageStream
    ) {
        SaveToWICMemory(pixelBuffers, count, description, imageStream, ImageFileType.Png);
    }

    public static void SaveWmpToWICMemory(
        PixelBuffer[] pixelBuffers,
        int count,
        ImageDescription description,
        Stream imageStream
    ) {
        SaveToWICMemory(pixelBuffers, count, description, imageStream, ImageFileType.Wmp);
    }

    internal static void SaveBgra32(
        nint data,
        int width,
        int height,
        int rowPitch,
        Stream imageStream,
        ImageFileType fileType
    ) {
        var pixels = new byte[checked(width * height * 4)];
        for (var row = 0; row < height; ++row)
            Marshal.Copy(nint.Add(data, row * rowPitch), pixels, row * width * 4, width * 4);
        var source = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
        var encoder = CreateEncoder(fileType);
        encoder.Frames.Add(BitmapFrame.Create(source));
        encoder.Save(imageStream);
    }

    private static void SaveToWICMemory(
        PixelBuffer[] pixelBuffers,
        int count,
        ImageDescription description,
        Stream imageStream,
        ImageFileType fileType
    ) {
        if (pixelBuffers == null || count <= 0 || imageStream == null)
            throw new ArgumentException("A pixel buffer and destination stream are required.");

        var source = pixelBuffers[0];
        if (source.Format == Format.B8G8R8A8_UNorm || source.Format == Format.B8G8R8X8_UNorm) {
            SaveBgra32(source.DataPointer, source.Width, source.Height, source.RowStride, imageStream, fileType);
            return;
        }

        if (source.Format == Format.R8G8B8A8_UNorm) {
            var pixels = new byte[checked(source.Width * source.Height * 4)];
            for (var row = 0; row < source.Height; ++row)
                Marshal.Copy(nint.Add(source.DataPointer, row * source.RowStride),
                             pixels,
                             row * source.Width * 4,
                             source.Width * 4);
            for (var i = 0; i < pixels.Length; i += 4) {
                var red = pixels[i];
                pixels[i] = pixels[i + 2];
                pixels[i + 2] = red;
            }

            var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            try {
                SaveBgra32(handle.AddrOfPinnedObject(),
                           source.Width,
                           source.Height,
                           source.Width * 4,
                           imageStream,
                           fileType);
            } finally {
                handle.Free();
            }

            return;
        }

        throw new NotSupportedException($"WIC saving does not support pixel format {source.Format}.");
    }

    private static BitmapEncoder CreateEncoder(ImageFileType fileType) {
        return fileType switch {
            ImageFileType.Bmp => new BmpBitmapEncoder(),
            ImageFileType.Gif => new GifBitmapEncoder(),
            ImageFileType.Jpg => new JpegBitmapEncoder(),
            ImageFileType.Png => new PngBitmapEncoder(),
            ImageFileType.Tiff => new TiffBitmapEncoder(),
            ImageFileType.Wmp => new WmpBitmapEncoder(),
            _ => throw new NotSupportedException($"WIC saving does not support {fileType}.")
        };
    }

}
