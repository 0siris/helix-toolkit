/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.SharpDX.Toolkit.Graphics;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Contains every subresource required to upload one texture, array, or cube map.
/// </summary>
/// <param name="Width">The texture width.</param>
/// <param name="Height">The texture height.</param>
/// <param name="Depth">The texture depth.</param>
/// <param name="Format">The DXGI pixel format.</param>
/// <param name="Dimension">The native resource dimension.</param>
/// <param name="ArraySize">The texture array size.</param>
/// <param name="MipLevels">The mip-level count.</param>
/// <param name="IsCubeMap">Whether the six array slices form one cube map.</param>
/// <param name="Subresources">The array-major, mip-minor source subresources.</param>
internal readonly record struct D3D12TextureUploadData(
    uint Width,
    uint Height,
    ushort Depth,
    Format Format,
    ResourceDimension Dimension,
    ushort ArraySize,
    ushort MipLevels,
    bool IsCubeMap,
    D3D12SubresourceData[] Subresources
) {
    /// <summary>
    ///     Gets the base subresource bytes for compatibility with base-mip callers and tests.
    /// </summary>
    internal byte[] Pixels => Subresources[0].Data.ToArray();

    /// <summary>
    ///     Gets the base subresource row pitch.
    /// </summary>
    internal uint RowPitch => Subresources[0].RowPitch;
}

/// <summary>
///     Owns a Direct3D 12 texture, its initial upload allocation, and its shader-resource descriptor.
/// </summary>
internal sealed class SilkD3D12TextureModelResource : IDisposable {
    /// <summary>
    ///     Initializes an uploaded texture-model resource.
    /// </summary>
    /// <param name="resource">The default-heap texture.</param>
    /// <param name="uploadResource">The upload allocation retained until disposal.</param>
    /// <param name="shaderResourceView">The shader-resource descriptor.</param>
    private SilkD3D12TextureModelResource(
        SilkD3D12Resource resource,
        SilkD3D12Resource uploadResource,
        SilkD3D12Descriptor shaderResourceView,
        bool isCubeMap
    ) {
        Resource = resource;
        UploadResource = uploadResource;
        ShaderResourceView = shaderResourceView;
        IsCubeMap = isCubeMap;
    }

    /// <summary>
    ///     Gets the default-heap texture resource.
    /// </summary>
    internal SilkD3D12Resource Resource { get; }

    /// <summary>
    ///     Gets the initial upload allocation.
    /// </summary>
    // ponytail: keep the upload alive until the render host provides fence-scoped deferred release.
    internal SilkD3D12Resource UploadResource { get; }

    /// <summary>
    ///     Gets the shader-resource descriptor.
    /// </summary>
    internal SilkD3D12Descriptor ShaderResourceView { get; }

    /// <summary>
    ///     Gets whether the resource must be viewed as a cube map.
    /// </summary>
    internal bool IsCubeMap { get; }

    /// <summary>
    ///     Gets whether the owned resources have been disposed.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Loads an existing texture model, records its initial upload, and creates its shader-resource view.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="context">The open command context receiving the upload commands.</param>
    /// <param name="descriptorHeap">The shader-visible CBV/SRV/UAV heap receiving the view.</param>
    /// <param name="textureModel">The existing texture model.</param>
    /// <returns>The uploaded Direct3D 12 texture resource.</returns>
    internal static SilkD3D12TextureModelResource Create(
        SilkD3D12Device device,
        SilkD3D12CommandContext context,
        SilkD3D12DescriptorHeap descriptorHeap,
        TextureModel textureModel
    ) {
        device.AssertArgumentNotNull();
        context.AssertArgumentNotNull();
        descriptorHeap.AssertArgumentNotNull();
        textureModel.AssertArgumentNotNull();
        ObjectDisposedException.ThrowIf(descriptorHeap.IsDisposed, descriptorHeap);
        if (descriptorHeap.Type != DescriptorHeapType.CbvSrvUav || !descriptorHeap.IsShaderVisible)
            throw new ArgumentException("Texture SRVs require a shader-visible CBV/SRV/UAV heap.",
                nameof(descriptorHeap));

        var info = textureModel.Load();
        SilkD3D12Resource? resource = null;
        SilkD3D12Resource? upload = null;
        SilkD3D12Descriptor? descriptor = null;
        var completionAttempted = false;
        try {
            var data = PrepareUploadData(info);
            resource = data.Dimension switch {
                ResourceDimension.Texture1D => device.CreateTexture1D(data.Width,
                    data.Format,
                    arraySize: data.ArraySize,
                    mipLevels: data.MipLevels),
                ResourceDimension.Texture2D => device.CreateTexture2D(data.Width,
                    data.Height,
                    data.Format,
                    arraySize: data.ArraySize,
                    mipLevels: data.MipLevels),
                ResourceDimension.Texture3D => device.CreateTexture3D(data.Width,
                    data.Height,
                    data.Depth,
                    data.Format,
                    mipLevels: data.MipLevels),
                _ => throw new NotSupportedException($"The texture dimension {data.Dimension} is not supported.")
            };
            upload = device.CreateTextureUploadBuffer(resource,
                data.Subresources,
                out var footprints);
            descriptor = descriptorHeap.Allocate();
            if (data.Dimension == ResourceDimension.Texture1D)
                device.CreateTexture1DShaderResourceView(resource, descriptor);
            else if (data.Dimension == ResourceDimension.Texture3D)
                device.CreateTexture3DShaderResourceView(resource, descriptor);
            else
                device.CreateShaderResourceView(resource, descriptor, data.IsCubeMap);
            context.CopyBufferToTexture(resource, upload, footprints);
            context.Transition(resource,
                ResourceStates.PixelShaderResource | ResourceStates.NonPixelShaderResource);

            completionAttempted = true;
            textureModel.Complete(info, true);
            return new SilkD3D12TextureModelResource(resource, upload, descriptor, data.IsCubeMap);
        } catch {
            descriptor?.Dispose();
            upload?.Dispose();
            resource?.Dispose();
            if (!completionAttempted) textureModel.Complete(info, false);
            throw;
        }
    }

    /// <summary>
    ///     Converts a supported texture description into complete Direct3D 12 subresource uploads.
    /// </summary>
    /// <param name="info">The texture information.</param>
    /// <returns>The prepared texture upload.</returns>
    internal static D3D12TextureUploadData PrepareUploadData(TextureInfo info) {
        info.AssertArgumentNotNull();
        return info.DataType switch {
            TextureDataType.ByteArray => PrepareBytes(info.TextureRaw,
                info.PixelFormat,
                info.Width,
                info.Height,
                info.Depth,
                info.Dimension),
            TextureDataType.Color4 => PrepareBytes(MemoryMarshal.AsBytes(info.Color4Array.AsSpan()).ToArray(),
                Format.FormatR32G32B32A32Float,
                info.Width,
                info.Height,
                info.Depth,
                info.Dimension),
            TextureDataType.RawPointer => PreparePointer(info),
            TextureDataType.Stream when info.IsCompressed => PrepareEncodedStream(info.Texture),
            TextureDataType.Stream => PrepareRawStream(info),
            _ => throw new NotSupportedException("The texture model does not contain uploadable data.")
        };
    }

    /// <summary>
    ///     Releases the descriptor and both native allocations.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;

        ShaderResourceView.Dispose();
        UploadResource.Dispose();
        Resource.Dispose();
        IsDisposed = true;
    }

    /// <summary>
    ///     Validates and packages managed texture bytes.
    /// </summary>
    /// <param name="pixels">The source pixels.</param>
    /// <param name="format">The pixel format.</param>
    /// <param name="width">The texture width.</param>
    /// <param name="height">The texture height.</param>
    /// <param name="dimension">The declared texture dimension.</param>
    /// <returns>The prepared upload.</returns>
    private static D3D12TextureUploadData PrepareBytes(
        byte[] pixels,
        Format format,
        int width,
        int height,
        int depth,
        int dimension
    ) {
        var rowPitch = GetRowPitch(format, width, height, depth, dimension, out var expectedLength);
        if (pixels.Length != expectedLength)
            throw new ArgumentException("The texture data length does not match its dimensions and format.",
                nameof(pixels));

        return new D3D12TextureUploadData(checked((uint) width),
            checked((uint) Math.Max(1, height)),
            checked((ushort) Math.Max(1, depth)),
            format,
            ToResourceDimension(dimension),
            1,
            1,
            false,
            [new D3D12SubresourceData(pixels,
                rowPitch,
                checked(rowPitch * (uint) Math.Max(1, height)))]);
    }

    /// <summary>
    ///     Copies texture pixels from an unmanaged pointer.
    /// </summary>
    /// <param name="info">The pointer-backed texture information.</param>
    /// <returns>The prepared upload.</returns>
    private static D3D12TextureUploadData PreparePointer(TextureInfo info) {
        if (info.RawPointer == nint.Zero)
            throw new ArgumentException("The texture pointer is null.", nameof(info));
        var rowPitch = GetRowPitch(info.PixelFormat,
            info.Width,
            info.Height,
            info.Depth,
            info.Dimension,
            out var length);
        var pixels = new byte[length];
        Marshal.Copy(info.RawPointer, pixels, 0, length);
        return new D3D12TextureUploadData(checked((uint) info.Width),
            checked((uint) Math.Max(1, info.Height)),
            checked((ushort) Math.Max(1, info.Depth)),
            info.PixelFormat,
            ToResourceDimension(info.Dimension),
            1,
            1,
            false,
            [new D3D12SubresourceData(pixels,
                rowPitch,
                checked(rowPitch * (uint) Math.Max(1, info.Height)))]);
    }

    /// <summary>
    ///     Reads a raw two-dimensional texture stream.
    /// </summary>
    /// <param name="info">The stream-backed texture information.</param>
    /// <returns>The prepared upload.</returns>
    private static D3D12TextureUploadData PrepareRawStream(TextureInfo info) {
        var rowPitch = GetRowPitch(info.PixelFormat,
            info.Width,
            info.Height,
            info.Depth,
            info.Dimension,
            out var length);
        var pixels = ReadExactStream(info.Texture, length);
        return new D3D12TextureUploadData(checked((uint) info.Width),
            checked((uint) Math.Max(1, info.Height)),
            checked((ushort) Math.Max(1, info.Depth)),
            info.PixelFormat,
            ToResourceDimension(info.Dimension),
            1,
            1,
            false,
            [new D3D12SubresourceData(pixels,
                rowPitch,
                checked(rowPitch * (uint) Math.Max(1, info.Height)))]);
    }

    /// <summary>
    ///     Decodes an image stream through the existing image loader.
    /// </summary>
    /// <param name="stream">The encoded image stream.</param>
    /// <returns>The prepared upload.</returns>
    private static D3D12TextureUploadData PrepareEncodedStream(Stream stream) {
        var originalPosition = stream.CanSeek ? stream.Position : 0;
        try {
            if (stream.CanSeek) stream.Position = 0;
            using var image = Image.Load(stream)
                              ?? throw new NotSupportedException("The encoded texture stream could not be decoded.");
            return PrepareImage(image);
        } finally {
            if (stream.CanSeek) stream.Position = originalPosition;
        }
    }

    /// <summary>
    ///     Copies every subresource from an existing decoded image.
    /// </summary>
    /// <param name="image">The decoded image.</param>
    /// <returns>The complete texture upload.</returns>
    internal static D3D12TextureUploadData PrepareImage(Image image) {
        image.AssertArgumentNotNull();
        var description = image.Description;
        var isCubeMap = description.Dimension == TextureDimension.TextureCube;
        var is3D = description.Dimension == TextureDimension.Texture3D;
        var is1D = description.Dimension == TextureDimension.Texture1D;
        if (!is1D && description.Dimension != TextureDimension.Texture2D && !is3D && !isCubeMap)
            throw new NotSupportedException("The decoded texture dimension is not supported by DX12.");
        if (description.Width <= 0 || description.Height <= 0 || description.ArraySize <= 0 ||
            description.MipLevels <= 0)
            throw new ArgumentException("The decoded texture description is incomplete.", nameof(image));
        if (isCubeMap && description.ArraySize != 6)
            throw new ArgumentException("Cube maps require exactly six array slices.", nameof(image));

        var subresources = new D3D12SubresourceData[is3D
            ? description.MipLevels
            : checked(description.ArraySize * description.MipLevels)];
        var index = 0;
        if (is3D) {
            for (var mipIndex = 0; mipIndex < description.MipLevels; mipIndex++) {
                var mipDepth = Math.Max(1, description.Depth >> mipIndex);
                var firstSlice = image.GetPixelBuffer(0, mipIndex);
                if (firstSlice.RowStride <= 0 || firstSlice.BufferStride <= 0)
                    throw new ArgumentException("A decoded texture subresource has invalid pitches.", nameof(image));
                var pixels = new byte[checked(firstSlice.BufferStride * mipDepth)];
                for (var slice = 0; slice < mipDepth; slice++) {
                    var pixelBuffer = image.GetPixelBuffer(slice, mipIndex);
                    if (pixelBuffer.RowStride != firstSlice.RowStride || pixelBuffer.BufferStride != firstSlice.BufferStride)
                        throw new ArgumentException("A decoded volume mip has inconsistent slice pitches.", nameof(image));
                    Marshal.Copy(pixelBuffer.DataPointer,
                        pixels,
                        checked(slice * firstSlice.BufferStride),
                        firstSlice.BufferStride);
                }
                subresources[index++] = new D3D12SubresourceData(pixels,
                    checked((uint) firstSlice.RowStride),
                    checked((uint) firstSlice.BufferStride));
            }
        } else {
            for (var arrayIndex = 0; arrayIndex < description.ArraySize; arrayIndex++)
            for (var mipIndex = 0; mipIndex < description.MipLevels; mipIndex++) {
                var pixelBuffer = image.GetPixelBuffer(arrayIndex, mipIndex);
                if (pixelBuffer.RowStride <= 0 || pixelBuffer.BufferStride <= 0)
                    throw new ArgumentException("A decoded texture subresource has invalid pitches.", nameof(image));
                var pixels = new byte[pixelBuffer.BufferStride];
                Marshal.Copy(pixelBuffer.DataPointer, pixels, 0, pixels.Length);
                subresources[index++] = new D3D12SubresourceData(pixels,
                    checked((uint) pixelBuffer.RowStride),
                    checked((uint) pixelBuffer.BufferStride));
            }
        }

        return new D3D12TextureUploadData(checked((uint) description.Width),
            checked((uint) description.Height),
            checked((ushort) description.Depth),
            description.Format,
            description.Dimension switch {
                TextureDimension.Texture1D => ResourceDimension.Texture1D,
                TextureDimension.Texture3D => ResourceDimension.Texture3D,
                _ => ResourceDimension.Texture2D
            },
            checked((ushort) (is3D ? 1 : description.ArraySize)),
            checked((ushort) description.MipLevels),
            isCubeMap,
            subresources);
    }

    /// <summary>
    ///     Computes an uncompressed source row pitch and byte length.
    /// </summary>
    /// <param name="format">The pixel format.</param>
    /// <param name="width">The texture width.</param>
    /// <param name="height">The texture height.</param>
    /// <param name="depth">The texture depth.</param>
    /// <param name="dimension">The declared texture dimension.</param>
    /// <param name="length">The complete byte length.</param>
    /// <returns>The source row pitch.</returns>
    private static uint GetRowPitch(
        Format format,
        int width,
        int height,
        int depth,
        int dimension,
        out int length
    ) {
        if (dimension is < 1 or > 3)
            throw new NotSupportedException("The texture dimension must be 1D, 2D, or 3D.");
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (dimension >= 2 && height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (dimension == 3 && depth <= 0) throw new ArgumentOutOfRangeException(nameof(depth));
        var toolkitFormat = (HelixToolkit.SharpDX.Core.SharpDX.Toolkit.Graphics.Format) format;
        if (format == Format.FormatUnknown || FormatHelper.IsCompressed(toolkitFormat) ||
            FormatHelper.IsPacked(toolkitFormat) || FormatHelper.IsVideo(toolkitFormat))
            throw new NotSupportedException($"The base-mip upload does not support {format}.");

        var bitsPerPixel = FormatHelper.SizeOfInBits(toolkitFormat);
        if (bitsPerPixel <= 0 || bitsPerPixel % 8 != 0)
            throw new NotSupportedException($"The pixel size for {format} is unavailable.");
        var bytesPerPixel = bitsPerPixel / 8;
        var rowPitch = checked(width * bytesPerPixel);
        length = checked(rowPitch * Math.Max(1, height) * Math.Max(1, depth));
        return checked((uint) rowPitch);
    }

    /// <summary>
    ///     Maps the existing managed dimension contract to a native resource dimension.
    /// </summary>
    /// <param name="dimension">The managed dimension value.</param>
    /// <returns>The native resource dimension.</returns>
    private static ResourceDimension ToResourceDimension(int dimension) => dimension switch {
        1 => ResourceDimension.Texture1D,
        2 => ResourceDimension.Texture2D,
        3 => ResourceDimension.Texture3D,
        _ => throw new NotSupportedException("The texture dimension must be 1D, 2D, or 3D.")
    };

    /// <summary>
    ///     Reads exactly one raw texture from a stream and restores seekable streams.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="length">The required byte length.</param>
    /// <returns>The texture bytes.</returns>
    private static byte[] ReadExactStream(Stream stream, int length) {
        var originalPosition = stream.CanSeek ? stream.Position : 0;
        try {
            if (stream.CanSeek) stream.Position = 0;
            var pixels = new byte[length];
            stream.ReadExactly(pixels);
            if (stream.ReadByte() != -1)
                throw new ArgumentException("The raw texture stream contains more data than declared.", nameof(stream));
            return pixels;
        } finally {
            if (stream.CanSeek) stream.Position = originalPosition;
        }
    }
}
