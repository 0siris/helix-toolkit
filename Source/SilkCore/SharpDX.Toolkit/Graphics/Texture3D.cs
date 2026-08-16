/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core.SharpDX.Toolkit.Graphics;

/// <summary>
///     A Texture 3D front end to the native D3D texture.
/// </summary>
public class Texture3D : Texture3DBase {
    internal Texture3D(NativeD3DDevice device, NativeTexture3DDescription description3D, params DataBox[] dataBox)
        : base(device, description3D, dataBox) { }

    internal Texture3D(NativeD3DDevice device, NativeD3DTexture3D texture) : base(device, texture) { }

    /// <summary>
    ///     Makes a copy of this texture.
    /// </summary>
    /// <remarks>
    ///     This method doesn't copy the content of the texture.
    /// </remarks>
    /// <returns>
    ///     A copy of this texture.
    /// </returns>
    public override Texture Clone() => new Texture3D(
        GraphicsDevice ?? throw new InvalidOperationException("The texture has no graphics device."), Description);

    /// <summary>
    ///     Creates a new texture from a <see cref="NativeTexture3DDescription" />.
    /// </summary>
    /// <param name="device">The <see cref="NativeD3DDevice" />.</param>
    /// <param name="description">The description.</param>
    /// <returns>
    ///     A new instance of <see cref="Texture3D" /> class.
    /// </returns>
    /// <msdn-id>ff476522</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture3D([In] const D3D11_TEXTURE3D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture3D** ppTexture3D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture3D</unmanaged-short>
    public static Texture3D New(NativeD3DDevice device, NativeTexture3DDescription description) 
        => new(device, description);

    /// <summary>
    ///     Creates a new texture from a <see cref="NativeD3DTexture3D" />.
    /// </summary>
    /// <param name="device">The <see cref="NativeD3DDevice" />.</param>
    /// <param name="texture">The native texture <see cref="Texture3D" />.</param>
    /// <returns>
    ///     A new instance of <see cref="Texture3D" /> class.
    /// </returns>
    /// <msdn-id>ff476522</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture3D([In] const D3D11_TEXTURE3D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture3D** ppTexture3D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture3D</unmanaged-short>
    public static Texture3D New(NativeD3DDevice device, NativeD3DTexture3D texture) 
        => new(device, texture);

    /// <summary>
    ///     Creates a new <see cref="Texture3D" /> with a single mipmap.
    /// </summary>
    /// <param name="device">The <see cref="NativeD3DDevice" />.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="depth">The depth.</param>
    /// <param name="format">Describes the format to use.</param>
    /// <param name="usage">The usage.</param>
    /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
    /// <returns>
    ///     A new instance of <see cref="Texture3D" /> class.
    /// </returns>
    /// <msdn-id>ff476522</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture3D([In] const D3D11_TEXTURE3D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture3D** ppTexture3D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture3D</unmanaged-short>
    public static Texture3D New(
        NativeD3DDevice device,
        int width,
        int height,
        int depth,
        PixelFormat format,
        TextureFlags flags = TextureFlags.ShaderResource,
        ResourceUsage usage = ResourceUsage.Default
    )
        => New(device, width, height, depth, false, format, flags, usage);

    /// <summary>
    ///     Creates a new <see cref="Texture3D" />.
    /// </summary>
    /// <param name="device">The <see cref="NativeD3DDevice" />.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="depth">The depth.</param>
    /// <param name="mipCount">
    ///     Number of mipmaps, set to true to have all mipmaps, set to an int >=1 for a particular mipmap
    ///     count.
    /// </param>
    /// <param name="format">Describes the format to use.</param>
    /// <param name="usage">The usage.</param>
    /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
    /// <returns>
    ///     A new instance of <see cref="Texture3D" /> class.
    /// </returns>
    /// <msdn-id>ff476522</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture3D([In] const D3D11_TEXTURE3D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture3D** ppTexture3D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture3D</unmanaged-short>
    public static Texture3D New(
        NativeD3DDevice device,
        int width,
        int height,
        int depth,
        MipMapCount mipCount,
        PixelFormat format,
        TextureFlags flags = TextureFlags.ShaderResource,
        ResourceUsage usage = ResourceUsage.Default
    )
        => new(device, NewDescription(width, height, depth, format, flags, mipCount, usage));

    /// <summary>
    ///     Creates a new <see cref="Texture3D" /> with texture data for the firs map.
    /// </summary>
    /// <typeparam name="T">Type of the data to upload to the texture</typeparam>
    /// <param name="device">The <see cref="NativeD3DDevice" />.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="depth">The depth.</param>
    /// <param name="format">Describes the format to use.</param>
    /// <param name="usage">The usage.</param>
    /// <param name="textureData">The texture data, width * height * depth data </param>
    /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
    /// <returns>A new instance of <see cref="Texture3D" /> class.</returns>
    /// <remarks>
    ///     The first dimension of mipMapTextures describes the number of is an array ot Texture3D Array
    /// </remarks>
    /// <msdn-id>ff476522</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture3D([In] const D3D11_TEXTURE3D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture3D** ppTexture3D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture3D</unmanaged-short>
    public static unsafe Texture3D New<T>(
        NativeD3DDevice device,
        int width,
        int height,
        int depth,
        PixelFormat format,
        T[] textureData,
        TextureFlags flags = TextureFlags.ShaderResource,
        ResourceUsage usage = ResourceUsage.Immutable
    ) where T : unmanaged {
        fixed (T* textureDataPtr = textureData)
            return New(device,
                       width,
                       height,
                       depth,
                       1,
                       format,
                       [GetDataBox(format, width, height, depth, textureData, (nint)textureDataPtr)],
                       flags,
                       usage);
    }

    /// <summary>
    ///     Creates a new <see cref="Texture3D" />.
    /// </summary>
    /// <param name="device">The <see cref="NativeD3DDevice" />.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="depth">The depth.</param>
    /// <param name="mipCount">
    ///     Number of mipmaps, set to true to have all mipmaps, set to an int >=1 for a particular mipmap
    ///     count.
    /// </param>
    /// <param name="format">Describes the format to use.</param>
    /// <param name="usage">The usage.</param>
    /// <param name="textureData">DataBox used to fill texture data.</param>
    /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
    /// <returns>
    ///     A new instance of <see cref="Texture3D" /> class.
    /// </returns>
    /// <msdn-id>ff476522</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture3D([In] const D3D11_TEXTURE3D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture3D** ppTexture3D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture3D</unmanaged-short>
    public static Texture3D New(
        NativeD3DDevice device,
        int width,
        int height,
        int depth,
        MipMapCount mipCount,
        PixelFormat format,
        DataBox[] textureData,
        TextureFlags flags = TextureFlags.ShaderResource,
        ResourceUsage usage = ResourceUsage.Default
    )
    // TODO Add check for number of texture data according to width/height/depth/mipCount.
        => new(device, NewDescription(width, height, depth, format, flags, mipCount, usage), textureData);

    /// <summary>
    ///     Creates a new <see cref="Texture3D" /> directly from an <see cref="Image" />.
    /// </summary>
    /// <param name="device">The <see cref="NativeD3DDevice" />.</param>
    /// <param name="image">An image in CPU memory.</param>
    /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
    /// <param name="usage">The usage.</param>
    /// <returns>A new instance of <see cref="Texture3D" /> class.</returns>
    /// <msdn-id>ff476522</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture3D([In] const D3D11_TEXTURE3D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture3D** ppTexture3D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture3D</unmanaged-short>
    public static Texture3D New(
        NativeD3DDevice device,
        Image image,
        TextureFlags flags = TextureFlags.ShaderResource,
        ResourceUsage usage = ResourceUsage.Immutable
    ) {
        if (image.Description.Dimension != TextureDimension.Texture3D)
            throw new ArgumentException("Invalid image. Must be 3D", nameof(image));

        return new Texture3D(device, CreateTextureDescriptionFromImage(image, flags, usage), image.ToDataBox());
    }

    /// <summary>
    ///     Loads a 3D texture from a stream.
    /// </summary>
    /// <param name="device">The <see cref="NativeD3DDevice" />.</param>
    /// <param name="stream">The stream to load the texture from.</param>
    /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
    /// <param name="usage">Usage of the resource. Default is <see cref="ResourceUsage.Immutable" /> </param>
    /// <exception cref="ArgumentException">If the texture is not of type 3D</exception>
    /// <returns>A texture</returns>
    public new static Texture3D Load(
        NativeD3DDevice device,
        Stream stream,
        TextureFlags flags = TextureFlags.ShaderResource,
        ResourceUsage usage = ResourceUsage.Immutable
    ) {
        var texture = Texture.Load(device, stream, flags, usage);
        if (texture is not Texture3D typedTexture)
            throw new ArgumentException($"Texture is not type of [Texture3D] but [{texture?.GetType().Name ?? "null"}]");
        return typedTexture;
    }

    /// <summary>
    ///     Loads a 3D texture from a stream.
    /// </summary>
    /// <param name="device">The <see cref="NativeD3DDevice" />.</param>
    /// <param name="filePath">The file to load the texture from.</param>
    /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
    /// <param name="usage">Usage of the resource. Default is <see cref="ResourceUsage.Immutable" /> </param>
    /// <exception cref="ArgumentException">If the texture is not of type 3D</exception>
    /// <returns>A texture</returns>
    public new static Texture3D Load(
        NativeD3DDevice device,
        string filePath,
        TextureFlags flags = TextureFlags.ShaderResource,
        ResourceUsage usage = ResourceUsage.Immutable
    ) {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Load(device, stream, flags, usage);
    }
}
