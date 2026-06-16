/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SharpDX.Toolkit.Graphics
{
    internal static class WICHelper
    {
        public static Image LoadFromWICMemory(IntPtr pSource, int size, bool makeACopy, GCHandle? handle)
        {
            throw new NotSupportedException("WIC loading is not ported to the Silk.NET backend yet.");
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
