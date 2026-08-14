/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace SharpDX.Toolkit.Graphics;

/// <summary>
///     A Texture 1D front end to the native D3D texture.
/// </summary>
public class Texture1D : Texture1DBase {
    internal Texture1D(NativeD3DDevice device, NativeTexture1DDescription description1D, params DataBox[] dataBox) :
        base(device, description1D, dataBox) { }

    internal Texture1D(NativeD3DDevice device, NativeD3DTexture1D texture) : base(device, texture) { }

    /// <summary>
    ///     Makes a copy of this texture.
    /// </summary>
    /// <remarks>
    ///     This method doesn't copy the content of the texture.
    /// </remarks>
    /// <returns>
    ///     A copy of this texture.
    /// </returns>
    public override Texture Clone() => new Texture1D(GraphicsDevice, Description);

    /// <summary>
    ///     Creates a new texture from a <see cref="Texture1DDescription" />.
    /// </summary>
    /// <param name="device">The <see cref="Direct3D11.Device" />.</param>
    /// <param name="description">The description.</param>
    /// <returns>
    ///     A new instance of <see cref="Texture1D" /> class.
    /// </returns>
    /// <msdn-id>ff476520</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture1D([In] const D3D11_TEXTURE1D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture1D** ppTexture1D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture1D</unmanaged-short>
    public static Texture1D New(NativeD3DDevice device, NativeTexture1DDescription description) => new(device, description);

    /// <summary>
    ///     Creates a new texture from a <see cref="Direct3D11.Texture1D" />.
    /// </summary>
    /// <param name="device">The <see cref="Direct3D11.Device" />.</param>
    /// <param name="texture">The native texture <see cref="Direct3D11.Texture1D" />.</param>
    /// <returns>
    ///     A new instance of <see cref="Texture1D" /> class.
    /// </returns>
    /// <msdn-id>ff476520</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture1D([In] const D3D11_TEXTURE1D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture1D** ppTexture1D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture1D</unmanaged-short>
    public static Texture1D New(NativeD3DDevice device, NativeD3DTexture1D texture) => new(device, texture);

    /// <summary>
    ///     Creates a new <see cref="Texture1D" /> with a single mipmap.
    /// </summary>
    /// <param name="device">The <see cref="Direct3D11.Device" />.</param>
    /// <param name="width">The width.</param>
    /// <param name="format">Describes the format to use.</param>
    /// <param name="usage">The usage.</param>
    /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
    /// <param name="arraySize">Size of the texture 2D array, default to 1.</param>
    /// <returns>
    ///     A new instance of <see cref="Texture1D" /> class.
    /// </returns>
    /// <msdn-id>ff476520</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture1D([In] const D3D11_TEXTURE1D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture1D** ppTexture1D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture1D</unmanaged-short>
    public static Texture1D New(
        NativeD3DDevice device,
        int width,
        PixelFormat format,
        TextureFlags flags = TextureFlags.ShaderResource,
        int arraySize = 1,
        ResourceUsage usage = ResourceUsage.Default
    )
        => New(device, width, false, format, flags, arraySize, usage);

    /// <summary>
    ///     Creates a new <see cref="Texture1D" />.
    /// </summary>
    /// <param name="device">The <see cref="Direct3D11.Device" />.</param>
    /// <param name="width">The width.</param>
    /// <param name="mipCount">
    ///     Number of mipmaps, set to true to have all mipmaps, set to an int >=1 for a particular mipmap
    ///     count.
    /// </param>
    /// <param name="format">Describes the format to use.</param>
    /// <param name="usage">The usage.</param>
    /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
    /// <param name="arraySize">Size of the texture 2D array, default to 1.</param>
    /// <returns>
    ///     A new instance of <see cref="Texture1D" /> class.
    /// </returns>
    /// <msdn-id>ff476520</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture1D([In] const D3D11_TEXTURE1D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture1D** ppTexture1D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture1D</unmanaged-short>
    public static Texture1D New(
        NativeD3DDevice device,
        int width,
        MipMapCount mipCount,
        PixelFormat format,
        TextureFlags flags = TextureFlags.ShaderResource,
        int arraySize = 1,
        ResourceUsage usage = ResourceUsage.Default
    )
        => new(device, NewDescription(width, format, flags, mipCount, arraySize, usage));

    /// <summary>
    ///     Creates a new <see cref="Texture1D" /> with a single level of mipmap.
    /// </summary>
    /// <typeparam name="T">Type of the initial data to upload to the texture</typeparam>
    /// <param name="device">The <see cref="Direct3D11.Device" />.</param>
    /// <param name="width">The width.</param>
    /// <param name="format">Describes the format to use.</param>
    /// <param name="usage">The usage.</param>
    /// <param name="textureData">Texture data. Size of must be equal to sizeof(Format) * width </param>
    /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
    /// <returns>A new instance of <see cref="Texture1D" /> class.</returns>
    /// <msdn-id>ff476521</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture1D([In] const D3D11_Texture1D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture1D** ppTexture1D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture1D</unmanaged-short>
    /// <remarks>
    ///     The first dimension of mipMapTextures describes the number of array (Texture1D Array), second dimension is the
    ///     mipmap, the third is the texture data for a particular mipmap.
    /// </remarks>
    public static unsafe Texture1D New<T>(
        NativeD3DDevice device,
        int width,
        PixelFormat format,
        T[] textureData,
        TextureFlags flags = TextureFlags.ShaderResource,
        ResourceUsage usage = ResourceUsage.Immutable
    )
        where T : unmanaged {
        fixed (T* textureDataPtr = textureData)
            return new Texture1D(device,
                                 NewDescription(width, format, flags, 1, 1, usage),
                                 GetDataBox(format, width, 1, 1, textureData, (nint)textureDataPtr));
    }

    /// <summary>
    ///     Creates a new <see cref="Texture1D" /> directly from an <see cref="Image" />.
    /// </summary>
    /// <param name="device">The <see cref="Direct3D11.Device" />.</param>
    /// <param name="image">An image in CPU memory.</param>
    /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
    /// <param name="usage">The usage.</param>
    /// <returns>A new instance of <see cref="Texture1D" /> class.</returns>
    /// <msdn-id>ff476521</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture1D([In] const D3D11_TEXTURE1D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture1D** ppTexture1D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture1D</unmanaged-short>
    public static Texture1D New(
        NativeD3DDevice device,
        Image image,
        TextureFlags flags = TextureFlags.ShaderResource,
        ResourceUsage usage = ResourceUsage.Immutable
    ) {
        if (image == null)
            ArgumentNullException.ThrowIfNull(image);
        if (image.Description.Dimension != TextureDimension.Texture1D)
            throw new ArgumentException("Invalid image. Must be 1D", "image");

        return new Texture1D(device, CreateTextureDescriptionFromImage(image, flags, usage), image.ToDataBox());
    }

    /// <summary>
    ///     Loads a 1D texture from a stream.
    /// </summary>
    /// <param name="device">The <see cref="Direct3D11.Device" />.</param>
    /// <param name="stream">The stream to load the texture from.</param>
    /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
    /// <param name="usage">Usage of the resource. Default is <see cref="ResourceUsage.Immutable" /> </param>
    /// <exception cref="ArgumentException">If the texture is not of type 1D</exception>
    /// <returns>A texture</returns>
    public new static Texture1D Load(
        NativeD3DDevice device,
        Stream stream,
        TextureFlags flags = TextureFlags.ShaderResource,
        ResourceUsage usage = ResourceUsage.Immutable
    ) {
        var texture = Texture.Load(device, stream, flags, usage);
        if (!(texture is Texture1D))
            throw new ArgumentException(string.Format("Texture is not type of [Texture1D] but [{0}]",
                                                      texture.GetType().Name));
        return (Texture1D)texture;
    }

    /// <summary>
    ///     Loads a 1D texture from a stream.
    /// </summary>
    /// <param name="device">The <see cref="Direct3D11.Device" />.</param>
    /// <param name="filePath">The file to load the texture from.</param>
    /// <param name="flags">Sets the texture flags (for unordered access...etc.)</param>
    /// <param name="usage">Usage of the resource. Default is <see cref="ResourceUsage.Immutable" /> </param>
    /// <exception cref="ArgumentException">If the texture is not of type 1D</exception>
    /// <returns>A texture</returns>
    public new static Texture1D Load(
        NativeD3DDevice device,
        string filePath,
        TextureFlags flags = TextureFlags.ShaderResource,
        ResourceUsage usage = ResourceUsage.Immutable
    ) {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Load(device, stream, flags, usage);
    }
}
