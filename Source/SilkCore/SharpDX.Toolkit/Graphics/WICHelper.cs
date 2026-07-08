/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.InteropServices;

namespace SharpDX.Toolkit.Graphics;

internal static class WICHelper {
    public static Image LoadFromWICMemory(nint pSource, int size, bool makeACopy, GCHandle? handle) {
        throw new NotSupportedException("WIC loading is not ported to the Silk.NET backend yet.");
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
        throw new NotSupportedException("WIC saving is only supported by the WPF target.");
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

}
