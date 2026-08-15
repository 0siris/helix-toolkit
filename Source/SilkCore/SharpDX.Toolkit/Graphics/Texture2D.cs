/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace SharpDX.Toolkit.Graphics;

/// <summary>
///     A Texture 2D front end to the native D3D texture.
/// </summary>
public class Texture2D : Texture2DBase {
    internal Texture2D(NativeD3DDevice device, NativeTexture2DDescription description2D, params DataBox[] dataBoxes) :
        base(device, description2D, dataBoxes) {
        Initialize(Resource);
    }

    internal Texture2D(NativeD3DDevice device, NativeD3DTexture2D texture) : base(device, texture) {
        Initialize(Resource);
    }

    /// <summary>
    ///     Makes a copy of this texture.
    /// </summary>
    /// <remarks>
    ///     This method doesn't copy the content of the texture.
    /// </remarks>
    /// <returns>
    ///     A copy of this texture.
    /// </returns>
    public override Texture Clone() => new Texture2D(
        GraphicsDevice ?? throw new InvalidOperationException("The texture has no graphics device."), Description);

    /// <summary>
    ///     Creates a new texture from a <see cref="Texture2DDescription" />.
    /// </summary>
    /// <param name="device">The <see cref="Direct3D11.Device" />.</param>
    /// <param name="description">The description.</param>
    /// <returns>
    ///     A new instance of <see cref="Texture2D" /> class.
    /// </returns>
    /// <msdn-id>ff476521</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture2D([In] const D3D11_TEXTURE2D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture2D** ppTexture2D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture2D</unmanaged-short>
    public static Texture2D New(NativeD3DDevice device, NativeTexture2DDescription description) => new(device, description);

    /// <summary>
    ///     Creates a new texture from a <see cref="Direct3D11.Texture2D" />.
    /// </summary>
    /// <param name="device">The <see cref="Direct3D11.Device" />.</param>
    /// <param name="texture">The native texture <see cref="Direct3D11.Texture2D" />.</param>
    /// <returns>
    ///     A new instance of <see cref="Texture2D" /> class.
    /// </returns>
    /// <msdn-id>ff476521</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture2D([In] const D3D11_TEXTURE2D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture2D** ppTexture2D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture2D</unmanaged-short>
    public static Texture2D New(NativeD3DDevice device, NativeD3DTexture2D texture) => new(device, texture);

    /// <summary>
    ///     Creates a new <see cref="Texture2D" /> with a single mipmap.
    /// </summary>
    /// <param name="device">The <see cref="Direct3D11.Device" />.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="format">Describes the format to use.</param>
    /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
    /// <param name="arraySize">Size of the texture 2D array, default to 1.</param>
    /// <param name="usage">The usage.</param>
    /// <returns>A new instance of <see cref="Texture2D" /> class.</returns>
    /// <msdn-id>ff476521</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture2D([In] const D3D11_TEXTURE2D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture2D** ppTexture2D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture2D</unmanaged-short>
    public static Texture2D New(
        NativeD3DDevice device,
        int width,
        int height,
        PixelFormat format,
        TextureFlags flags = TextureFlags.ShaderResource,
        int arraySize = 1,
        ResourceUsage usage = ResourceUsage.Default
    )
        => New(device, width, height, false, format, flags, arraySize, usage);

    /// <summary>
    ///     Creates a new <see cref="Texture2D" />.
    /// </summary>
    /// <param name="device">The <see cref="Direct3D11.Device" />.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="format">Describes the format to use.</param>
    /// <param name="mipCount">
    ///     Number of mipmaps, set to true to have all mipmaps, set to an int >=1 for a particular mipmap
    ///     count.
    /// </param>
    /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
    /// <param name="arraySize">Size of the texture 2D array, default to 1.</param>
    /// <param name="usage">The usage.</param>
    /// <returns>A new instance of <see cref="Texture2D" /> class.</returns>
    /// <msdn-id>ff476521</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture2D([In] const D3D11_TEXTURE2D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture2D** ppTexture2D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture2D</unmanaged-short>
    public static Texture2D New(
        NativeD3DDevice device,
        int width,
        int height,
        MipMapCount mipCount,
        PixelFormat format,
        TextureFlags flags = TextureFlags.ShaderResource,
        int arraySize = 1,
        ResourceUsage usage = ResourceUsage.Default
    )
        => new(device, NewDescription(width, height, format, flags, mipCount, arraySize, usage));

    /// <summary>
    ///     Creates a new <see cref="Texture2D" /> with a single level of mipmap.
    /// </summary>
    /// <typeparam name="T">Type of the pixel data to upload to the texture.</typeparam>
    /// <param name="device">The <see cref="Direct3D11.Device" />.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="format">Describes the format to use.</param>
    /// <param name="usage">The usage.</param>
    /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
    /// <param name="textureData">The texture data for a single mipmap and a single array slice. See remarks</param>
    /// <returns>A new instance of <see cref="Texture2D" /> class.</returns>
    /// <msdn-id>ff476521</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture2D([In] const D3D11_TEXTURE2D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture2D** ppTexture2D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture2D</unmanaged-short>
    /// <remarks>
    ///     Each value in textureData is a pixel in the destination texture.
    /// </remarks>
    public static unsafe Texture2D New<T>(
        NativeD3DDevice device,
        int width,
        int height,
        PixelFormat format,
        T[] textureData,
        TextureFlags flags = TextureFlags.ShaderResource,
        ResourceUsage usage = ResourceUsage.Immutable
    )
        where T : unmanaged {
        fixed (T* textureDataPtr = textureData)
            return New(device,
                       width,
                       height,
                       1,
                       format,
                       [GetDataBox(format, width, height, 1, textureData, (nint)textureDataPtr)],
                       flags,
                       1,
                       usage);
    }

    /// <summary>
    ///     Creates a new <see cref="Texture2D" />.
    /// </summary>
    /// <param name="device">The <see cref="Direct3D11.Device" />.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="format">Describes the format to use.</param>
    /// <param name="mipCount">
    ///     Number of mipmaps, set to true to have all mipmaps, set to an int >=1 for a particular mipmap
    ///     count.
    /// </param>
    /// <param name="textureData">Texture data through an array of <see cref="DataBox" /> </param>
    /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
    /// <param name="arraySize">Size of the texture 2D array, default to 1.</param>
    /// <param name="usage">The usage.</param>
    /// <returns>A new instance of <see cref="Texture2D" /> class.</returns>
    /// <msdn-id>ff476521</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture2D([In] const D3D11_TEXTURE2D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture2D** ppTexture2D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture2D</unmanaged-short>
    public static Texture2D New(
        NativeD3DDevice device,
        int width,
        int height,
        MipMapCount mipCount,
        PixelFormat format,
        DataBox[] textureData,
        TextureFlags flags = TextureFlags.ShaderResource,
        int arraySize = 1,
        ResourceUsage usage = ResourceUsage.Default
    )
        => new(device,
            NewDescription(width, height, format, flags, mipCount, arraySize, usage),
            textureData);

    /// <summary>
    ///     Creates a new <see cref="Texture2D" /> directly from an <see cref="Image" />.
    /// </summary>
    /// <param name="device">The <see cref="Direct3D11.Device" />.</param>
    /// <param name="image">An image in CPU memory.</param>
    /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
    /// <param name="usage">The usage.</param>
    /// <returns>A new instance of <see cref="Texture2D" /> class.</returns>
    /// <msdn-id>ff476521</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture2D([In] const D3D11_TEXTURE2D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture2D** ppTexture2D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture2D</unmanaged-short>
    public static Texture2D New(
        NativeD3DDevice device,
        Image image,
        TextureFlags flags = TextureFlags.ShaderResource,
        ResourceUsage usage = ResourceUsage.Immutable
    ) {
        if (image.Description.Dimension != TextureDimension.Texture2D)
            throw new ArgumentException("Invalid image. Must be 2D", "image");

        return new Texture2D(device, CreateTextureDescriptionFromImage(image, flags, usage), image.ToDataBox());
    }

    /// <summary>
    ///     Loads a 2D texture from a stream.
    /// </summary>
    /// <param name="device">The <see cref="Direct3D11.Device" />.</param>
    /// <param name="stream">The stream to load the texture from.</param>
    /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
    /// <param name="usage">Usage of the resource. Default is <see cref="ResourceUsage.Immutable" /> </param>
    /// <exception cref="ArgumentException">If the texture is not of type 2D</exception>
    /// <returns>A texture</returns>
    public new static Texture2D Load(
        NativeD3DDevice device,
        Stream stream,
        TextureFlags flags = TextureFlags.ShaderResource,
        ResourceUsage usage = ResourceUsage.Immutable
    ) {
        var texture = Texture.Load(device, stream, flags, usage);
        if (texture is not Texture2D typedTexture)
            throw new ArgumentException($"Texture is not type of [Texture2D] but [{texture?.GetType().Name ?? "null"}]");
        return typedTexture;
    }

    /// <summary>
    ///     Loads a 2D texture from a stream.
    /// </summary>
    /// <param name="device">The <see cref="Direct3D11.Device" />.</param>
    /// <param name="filePath">The file to load the texture from.</param>
    /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
    /// <param name="usage">Usage of the resource. Default is <see cref="ResourceUsage.Immutable" /> </param>
    /// <exception cref="ArgumentException">If the texture is not of type 2D</exception>
    /// <returns>A texture</returns>
    public new static Texture2D Load(
        NativeD3DDevice device,
        string filePath,
        TextureFlags flags = TextureFlags.ShaderResource,
        ResourceUsage usage = ResourceUsage.Immutable
    ) {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Load(device, stream, flags, usage);
    }

    /// <summary>
    ///     Implicit casting operator to <see cref="Direct3D11.Resource" />
    /// </summary>
    /// <param name="from">The GraphicsResource to convert from.</param>
    public static implicit operator NativeD3DResource?(Texture2D? from) => from?.Resource;
}
