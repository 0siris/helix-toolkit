/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SharpDX.Toolkit.Graphics
{
    internal static class DDSHelper
    {
        public static Image LoadFromDDSMemory(IntPtr pSource, int size, bool makeACopy, GCHandle? handle)
        {
            throw new NotSupportedException("DDS loading is not ported to the Silk.NET backend yet.");
        }

        public static void SaveToDDSStream(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            throw new NotSupportedException("DDS saving is not ported to the Silk.NET backend yet.");
        }
    }
}
