/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace SharpDX.Toolkit.Graphics;

/// <summary>
///     An unmanaged buffer of pixels.
/// </summary>
public sealed class PixelBuffer
{
    /// <summary>
    ///     True when RowStride == sizeof(pixelformat) * width
    /// </summary>
    private readonly bool isStrictRowStride;

    private Format format;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PixelBuffer" /> struct.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="format">The format.</param>
    /// <param name="rowStride">The row pitch.</param>
    /// <param name="bufferStride">The slice pitch.</param>
    /// <param name="dataPointer">The pixels.</param>
    public PixelBuffer(int width, int height, Format format, int rowStride, int bufferStride, nint dataPointer)
    {
        if (dataPointer == nint.Zero)
            throw new ArgumentException("Pointer cannot be equal to IntPtr.Zero", "dataPointer");

        this.Width = width;
        this.Height = height;
        this.format = format;
        this.RowStride = rowStride;
        this.BufferStride = bufferStride;
        this.DataPointer = dataPointer;
        PixelSize = FormatHelper.SizeOfInBytes(this.format);
        isStrictRowStride = PixelSize * width == rowStride;
    }

    /// <summary>
    ///     Gets the width.
    /// </summary>
    /// <value>The width.</value>
    public int Width { get; }

    /// <summary>
    ///     Gets the height.
    /// </summary>
    /// <value>The height.</value>
    public int Height { get; }

    /// <summary>
    ///     Gets the format (this value can be changed)
    /// </summary>
    /// <value>The format.</value>
    public Format Format
    {
        get => format;
        set
        {
            if (PixelSize != FormatHelper.SizeOfInBytes(value))
                throw new ArgumentException(string.Format(
                    "Format [{0}] doesn't have same pixel size in bytes than current format [{1}]", value, format));
            format = value;
        }
    }

    /// <summary>
    ///     Gets the pixel size in bytes.
    /// </summary>
    /// <value>The pixel size in bytes.</value>
    public int PixelSize { get; }

    /// <summary>
    ///     Gets the row stride in number of bytes.
    /// </summary>
    /// <value>The row stride in number of bytes.</value>
    public int RowStride { get; }

    /// <summary>
    ///     Gets the total size in bytes of this pixel buffer.
    /// </summary>
    /// <value>The size in bytes of the pixel buffer.</value>
    public int BufferStride { get; }

    /// <summary>
    ///     Gets the pointer to the pixel buffer.
    /// </summary>
    /// <value>The pointer to the pixel buffer.</value>
    public nint DataPointer { get; }

    /// <summary>
    ///     Copies this pixel buffer to a destination pixel buffer.
    /// </summary>
    /// <param name="pixelBuffer">The destination pixel buffer.</param>
    /// <remarks>
    ///     The destination pixel buffer must have exactly the same dimensions (width, height) and format than this instance.
    ///     Destination buffer can have different row stride.
    /// </remarks>
    public unsafe void CopyTo(PixelBuffer pixelBuffer)
    {
        // Check that buffers are identical
        if (Width != pixelBuffer.Width
            || Height != pixelBuffer.Height
            || PixelSize != FormatHelper.SizeOfInBytes(pixelBuffer.Format))
            throw new ArgumentException("Invalid destination pixelBufferArray. Mush have same Width, Height and Format",
                "pixelBuffer");

        // If buffers have same size, than we can copy it directly
        if (BufferStride == pixelBuffer.BufferStride)
        {
            Utilities.CopyMemory(pixelBuffer.DataPointer, DataPointer, BufferStride);
        }
        else
        {
            var srcPointer = (byte*) DataPointer;
            var dstPointer = (byte*) pixelBuffer.DataPointer;
            var rowStride = Math.Min(RowStride, pixelBuffer.RowStride);

            // Copy per scanline
            for (var i = 0; i < Height; i++)
            {
                Utilities.CopyMemory(new nint(dstPointer), new nint(srcPointer), rowStride);
                srcPointer += RowStride;
                dstPointer += pixelBuffer.RowStride;
            }
        }
    }

    /// <summary>
    ///     Gets the pixel value at a specified position.
    /// </summary>
    /// <typeparam name="T">Type of the pixel data</typeparam>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <returns>The pixel value.</returns>
    /// <remarks>
    ///     Caution, this method doesn't check bounding.
    /// </remarks>
    public unsafe T GetPixel<T>(int x, int y) where T : unmanaged
    {
        return Utilities.Read<T>(new nint((byte*) DataPointer + RowStride * y + x * PixelSize));
    }

    /// <summary>
    ///     Gets the pixel value at a specified position.
    /// </summary>
    /// <typeparam name="T">Type of the pixel data</typeparam>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <param name="value">The pixel value.</param>
    /// <remarks>
    ///     Caution, this method doesn't check bounding.
    /// </remarks>
    public unsafe void SetPixel<T>(int x, int y, T value) where T : unmanaged
    {
        Utilities.Write(new nint((byte*) DataPointer + RowStride * y + x * PixelSize), ref value);
    }

    /// <summary>
    ///     Gets scanline pixels from the buffer.
    /// </summary>
    /// <typeparam name="T">Type of the pixel data</typeparam>
    /// <param name="yOffset">The y line offset.</param>
    /// <returns>Scanline pixels from the buffer</returns>
    /// <exception cref="System.ArgumentException">If the sizeof(T) is an invalid size</exception>
    /// <remarks>
    ///     This method is working on a row basis. The <paramref name="yOffset" /> is specifying the first row to get
    ///     the pixels from.
    /// </remarks>
    public T[] GetPixels<T>(int yOffset = 0) where T : unmanaged
    {
        var sizeOfOutputPixel = Utilities.SizeOf<T>();
        var totalSize = Width * Height * PixelSize;
        if (totalSize % sizeOfOutputPixel != 0)
            throw new ArgumentException(
                string.Format("Invalid sizeof(T), not a multiple of current size [{0}]in bytes ", totalSize));

        var buffer = new T[totalSize / sizeOfOutputPixel];
        GetPixels(buffer, yOffset);
        return buffer;
    }

    /// <summary>
    ///     Gets scanline pixels from the buffer.
    /// </summary>
    /// <typeparam name="T">Type of the pixel data</typeparam>
    /// <param name="pixels">An allocated scanline pixel buffer</param>
    /// <param name="yOffset">The y line offset.</param>
    /// <returns>Scanline pixels from the buffer</returns>
    /// <exception cref="System.ArgumentException">If the sizeof(T) is an invalid size</exception>
    /// <remarks>
    ///     This method is working on a row basis. The <paramref name="yOffset" /> is specifying the first row to get
    ///     the pixels from.
    /// </remarks>
    public void GetPixels<T>(T[] pixels, int yOffset = 0) where T : unmanaged
    {
        GetPixels(pixels, yOffset, 0, pixels.Length);
    }

    /// <summary>
    ///     Gets scanline pixels from the buffer.
    /// </summary>
    /// <typeparam name="T">Type of the pixel data</typeparam>
    /// <param name="pixels">An allocated scanline pixel buffer</param>
    /// <param name="yOffset">The y line offset.</param>
    /// <param name="pixelIndex">Offset into the destination <paramref name="pixels" /> buffer.</param>
    /// <param name="pixelCount">Number of pixels to write into the destination <paramref name="pixels" /> buffer.</param>
    /// <exception cref="System.ArgumentException">If the sizeof(T) is an invalid size</exception>
    /// <remarks>
    ///     This method is working on a row basis. The <paramref name="yOffset" /> is specifying the first row to get
    ///     the pixels from.
    /// </remarks>
    public unsafe void GetPixels<T>(T[] pixels, int yOffset, int pixelIndex, int pixelCount) where T : unmanaged
    {
        var pixelPointer = (byte*) DataPointer + yOffset * RowStride;
        if (isStrictRowStride)
        {
            Utilities.Read(new nint(pixelPointer), pixels, 0, pixelCount);
        }
        else
        {
            var sizeOfOutputPixel = Utilities.SizeOf<T>() * pixelCount;
            var sizePerWidth = sizeOfOutputPixel / Width;
            var remainingPixels = sizeOfOutputPixel % Width;
            for (var i = 0; i < sizePerWidth; i++)
            {
                Utilities.Read(new nint(pixelPointer), pixels, pixelIndex, Width);
                pixelPointer += RowStride;
                pixelIndex += Width;
            }

            if (remainingPixels > 0) Utilities.Read(new nint(pixelPointer), pixels, pixelIndex, remainingPixels);
        }
    }

    /// <summary>
    ///     Sets scanline pixels to the buffer.
    /// </summary>
    /// <typeparam name="T">Type of the pixel data</typeparam>
    /// <param name="sourcePixels">Source pixel buffer</param>
    /// <param name="yOffset">The y line offset.</param>
    /// <exception cref="System.ArgumentException">If the sizeof(T) is an invalid size</exception>
    /// <remarks>
    ///     This method is working on a row basis. The <paramref name="yOffset" /> is specifying the first row to get
    ///     the pixels from.
    /// </remarks>
    public void SetPixels<T>(T[] sourcePixels, int yOffset = 0) where T : unmanaged
    {
        SetPixels(sourcePixels, yOffset, 0, sourcePixels.Length);
    }

    /// <summary>
    ///     Sets scanline pixels to the buffer.
    /// </summary>
    /// <typeparam name="T">Type of the pixel data</typeparam>
    /// <param name="sourcePixels">Source pixel buffer</param>
    /// <param name="yOffset">The y line offset.</param>
    /// <param name="pixelIndex">Offset into the source <paramref name="sourcePixels" /> buffer.</param>
    /// <param name="pixelCount">Number of pixels to write into the source <paramref name="sourcePixels" /> buffer.</param>
    /// <exception cref="System.ArgumentException">If the sizeof(T) is an invalid size</exception>
    /// <remarks>
    ///     This method is working on a row basis. The <paramref name="yOffset" /> is specifying the first row to get
    ///     the pixels from.
    /// </remarks>
    public unsafe void SetPixels<T>(T[] sourcePixels, int yOffset, int pixelIndex, int pixelCount) where T : unmanaged
    {
        var pixelPointer = (byte*) DataPointer + yOffset * RowStride;
        if (isStrictRowStride)
        {
            Utilities.Write(new nint(pixelPointer), sourcePixels, 0, pixelCount);
        }
        else
        {
            var sizeOfOutputPixel = Utilities.SizeOf<T>() * pixelCount;
            var sizePerWidth = sizeOfOutputPixel / Width;
            var remainingPixels = sizeOfOutputPixel % Width;
            for (var i = 0; i < sizePerWidth; i++)
            {
                Utilities.Write(new nint(pixelPointer), sourcePixels, pixelIndex, Width);
                pixelPointer += RowStride;
                pixelIndex += Width;
            }

            if (remainingPixels > 0) Utilities.Write(new nint(pixelPointer), sourcePixels, pixelIndex, remainingPixels);
        }
    }
}