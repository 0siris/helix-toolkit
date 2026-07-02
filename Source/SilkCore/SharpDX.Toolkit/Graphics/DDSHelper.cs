/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.InteropServices;

namespace SharpDX.Toolkit.Graphics;

internal static class DDSHelper
{
    public static unsafe Image LoadFromDDSMemory(nint pSource, int size, bool makeACopy, GCHandle? handle)
    {
        var headerSize = sizeof(uint) + Utilities.SizeOf<DDS.Header>();
        if (pSource == nint.Zero || size < headerSize || *(uint*) pSource != DDS.MagicHeader) return null;

        try
        {
            var header = *(DDS.Header*) ((byte*) pSource + sizeof(uint));
            if (header.Size != Utilities.SizeOf<DDS.Header>()
                || header.PixelFormat.Size != Utilities.SizeOf<DDS.PixelFormat>())
                return null;

            var dataOffset = headerSize;
            var format = GetFormat(header.PixelFormat, out var expand24Bit);
            var arraySize = 1;
            var dimension = TextureDimension.Texture2D;
            var depth = 1;

            if (header.PixelFormat.FourCC == FourCC('D', 'X', '1', '0'))
            {
                if (size < dataOffset + Utilities.SizeOf<DDS.HeaderDXT10>()) return null;

                var extended = *(DDS.HeaderDXT10*) ((byte*) pSource + dataOffset);
                dataOffset += Utilities.SizeOf<DDS.HeaderDXT10>();
                format = extended.DXGIFormat;
                arraySize = extended.ArraySize;
                switch (extended.ResourceDimension)
                {
                    case DDS.ResourceDimension.Texture1D:
                        dimension = TextureDimension.Texture1D;
                        break;
                    case DDS.ResourceDimension.Texture2D:
                        dimension = (extended.MiscFlags & ResourceOptionFlags.TextureCube) != 0
                            ? TextureDimension.TextureCube
                            : TextureDimension.Texture2D;
                        if (dimension == TextureDimension.TextureCube) arraySize *= 6;
                        break;
                    case DDS.ResourceDimension.Texture3D:
                        dimension = TextureDimension.Texture3D;
                        depth = Math.Max(1, header.Depth);
                        break;
                    default:
                        throw new NotSupportedException("Unsupported DDS resource dimension.");
                }
            }
            else if ((header.CubemapFlags & DDS.CubemapFlags.CubeMap) != 0)
            {
                if ((header.CubemapFlags & DDS.CubemapFlags.AllFaces) != DDS.CubemapFlags.AllFaces)
                    throw new NotSupportedException("Partial DDS cubemaps are not supported.");
                dimension = TextureDimension.TextureCube;
                arraySize = 6;
            }
            else if ((header.CubemapFlags & DDS.CubemapFlags.Volume) != 0)
            {
                dimension = TextureDimension.Texture3D;
                depth = Math.Max(1, header.Depth);
            }

            if (format == Format.Unknown || arraySize <= 0)
                throw new NotSupportedException("Unsupported DDS pixel format.");

            var description = new ImageDescription
            {
                Width = header.Width,
                Height = dimension == TextureDimension.Texture1D ? 1 : header.Height,
                Depth = depth,
                ArraySize = arraySize,
                Dimension = dimension,
                Format = format,
                MipLevels = Math.Max(1, header.MipMapCount)
            };

            var image = new Image(description, nint.Zero, 0, null, false);
            try
            {
                var dataSize = size - dataOffset;
                if (expand24Bit)
                {
                    Expand24Bit((byte*) pSource + dataOffset, dataSize, image);
                }
                else
                {
                    if (dataSize < image.TotalSizeInBytes)
                        throw new InvalidOperationException("Unexpected end of DDS data.");
                    Utilities.CopyMemory(image.DataPointer, nint.Add(pSource, dataOffset), image.TotalSizeInBytes);
                }
            }
            catch
            {
                image.Dispose();
                throw;
            }

            return image;
        }
        finally
        {
            if (handle.HasValue) handle.Value.Free();
        }
    }

    private static Format GetFormat(DDS.PixelFormat pixelFormat, out bool expand24Bit)
    {
        // ponytail: cover formats shipped by this repository; use DirectXTex if broader legacy DDS support is required.
        expand24Bit = false;
        if ((pixelFormat.Flags & DDS.PixelFormatFlags.FourCC) != 0)
        {
            if (pixelFormat.FourCC == FourCC('D', 'X', 'T', '1')) return Format.BC1_UNorm;
            if (pixelFormat.FourCC == FourCC('D', 'X', 'T', '2')
                || pixelFormat.FourCC == FourCC('D', 'X', 'T', '3')) return Format.BC2_UNorm;
            if (pixelFormat.FourCC == FourCC('D', 'X', 'T', '4')
                || pixelFormat.FourCC == FourCC('D', 'X', 'T', '5')) return Format.BC3_UNorm;
            if (pixelFormat.FourCC == FourCC('B', 'C', '4', 'U')
                || pixelFormat.FourCC == FourCC('A', 'T', 'I', '1')) return Format.BC4_UNorm;
            if (pixelFormat.FourCC == FourCC('B', 'C', '4', 'S')) return Format.BC4_SNorm;
            if (pixelFormat.FourCC == FourCC('B', 'C', '5', 'U')
                || pixelFormat.FourCC == FourCC('A', 'T', 'I', '2')) return Format.BC5_UNorm;
            if (pixelFormat.FourCC == FourCC('B', 'C', '5', 'S')) return Format.BC5_SNorm;
            return Format.Unknown;
        }

        if (pixelFormat.RGBBitCount == 32
            && pixelFormat.RBitMask == 0x00ff0000
            && pixelFormat.GBitMask == 0x0000ff00
            && pixelFormat.BBitMask == 0x000000ff)
            return pixelFormat.ABitMask == 0 ? Format.B8G8R8X8_UNorm : Format.B8G8R8A8_UNorm;
        if (pixelFormat.RGBBitCount == 32
            && pixelFormat.RBitMask == 0x000000ff
            && pixelFormat.GBitMask == 0x0000ff00
            && pixelFormat.BBitMask == 0x00ff0000)
            return Format.R8G8B8A8_UNorm;
        if (pixelFormat.RGBBitCount == 24
            && pixelFormat.RBitMask == 0x00ff0000
            && pixelFormat.GBitMask == 0x0000ff00
            && pixelFormat.BBitMask == 0x000000ff)
        {
            expand24Bit = true;
            return Format.R8G8B8A8_UNorm;
        }

        if (pixelFormat.RGBBitCount == 8
            && (pixelFormat.Flags & DDS.PixelFormatFlags.Luminance) != 0)
            return Format.R8_UNorm;
        return Format.Unknown;
    }

    private static unsafe void Expand24Bit(byte* source, int sourceSize, Image image)
    {
        var offset = 0;
        foreach (var buffer in image.pixelBuffers)
        {
            var pixelCount = buffer.Width * buffer.Height;
            var required = checked(pixelCount * 3);
            if (sourceSize - offset < required) throw new InvalidOperationException("Unexpected end of DDS data.");

            var destination = (byte*) buffer.DataPointer;
            for (var pixel = 0; pixel < pixelCount; pixel++)
            {
                destination[0] = source[offset + 2];
                destination[1] = source[offset + 1];
                destination[2] = source[offset];
                destination[3] = 255;
                destination += 4;
                offset += 3;
            }
        }
    }

    private static int FourCC(char c0, char c1, char c2, char c3)
    {
        return c0 | (c1 << 8) | (c2 << 16) | (c3 << 24);
    }

    public static void SaveToDDSStream(PixelBuffer[] pixelBuffers, int count, ImageDescription description,
        Stream imageStream)
    {
        throw new NotSupportedException("DDS saving is not ported to the Silk.NET backend yet.");
    }
}