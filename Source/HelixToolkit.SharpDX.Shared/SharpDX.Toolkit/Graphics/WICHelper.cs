/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
using System;
using System.IO;
using System.Runtime.InteropServices;
#if !NETFX_CORE
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
#endif

namespace SharpDX.Toolkit.Graphics
{
    internal static class WICHelper
    {
        public static Image LoadFromWICMemory(IntPtr pSource, int size, bool makeACopy, GCHandle? handle)
        {
#if !NETFX_CORE
            if (pSource == IntPtr.Zero || size <= 0)
            {
                return null;
            }

            var encoded = new byte[size];
            Marshal.Copy(pSource, encoded, 0, size);

            try
            {
                using (var stream = new MemoryStream(encoded, false))
                {
                    var decoder = BitmapDecoder.Create(
                        stream,
                        BitmapCreateOptions.PreservePixelFormat,
                        BitmapCacheOption.OnLoad);
                    if (decoder.Frames.Count == 0)
                    {
                        return null;
                    }

                    BitmapSource source = decoder.Frames[0];
                    if (source.Format != PixelFormats.Bgra32)
                    {
                        source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
                    }

                    var stride = checked(source.PixelWidth * 4);
                    var image = Image.New2D(
                        source.PixelWidth,
                        source.PixelHeight,
                        1,
                        PixelFormat.B8G8R8A8.UNorm);
                    source.CopyPixels(Int32Rect.Empty, image.DataPointer, image.TotalSizeInBytes, stride);
                    if (handle.HasValue)
                    {
                        handle.Value.Free();
                    }
                    return image;
                }
            }
            catch (FileFormatException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
#else
            throw new NotSupportedException("WIC loading is not ported to the Silk.NET backend yet.");
#endif
        }

        public static void SaveGifToWICMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            throw CreateSaveException();
        }

        public static void SaveTiffToWICMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            throw CreateSaveException();
        }

        public static void SaveBmpToWICMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            throw CreateSaveException();
        }

        public static void SaveJpgToWICMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            throw CreateSaveException();
        }

        public static void SavePngToWICMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            throw CreateSaveException();
        }

        public static void SaveWmpToWICMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            throw CreateSaveException();
        }

        private static NotSupportedException CreateSaveException()
        {
            return new NotSupportedException("WIC saving is not ported to the Silk.NET backend yet.");
        }
    }
}
