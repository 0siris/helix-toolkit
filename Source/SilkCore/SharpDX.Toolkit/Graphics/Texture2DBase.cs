/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace SharpDX.Toolkit.Graphics;

/// <summary>
///     Abstract class front end to the native D3D Texture2D.
/// </summary>
public abstract class Texture2DBase : Texture {
    /// <summary>
    /// </summary>
    protected new readonly NativeD3DTexture2D Resource;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Texture2DBase" /> class.
    /// </summary>
    /// <param name="device">The <see cref="Direct3D11.Device" />.</param>
    /// <param name="description2D">The description.</param>
    /// <msdn-id>ff476521</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture2D([In] const D3D11_TEXTURE2D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture2D** ppTexture2D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture2D</unmanaged-short>
    protected internal Texture2DBase(NativeD3DDevice device, NativeTexture2DDescription description2D)
        : base(device, description2D) {
        Resource = device.CreateTexture2D(description2D);
        Initialize(Resource);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Texture2DBase" /> class.
    /// </summary>
    /// <param name="device">The <see cref="Direct3D11.Device" />.</param>
    /// <param name="description2D">The description.</param>
    /// <param name="dataBoxes">A variable-length parameters list containing data rectangles.</param>
    /// <msdn-id>ff476521</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture2D([In] const D3D11_TEXTURE2D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture2D** ppTexture2D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture2D</unmanaged-short>
    protected internal Texture2DBase(
        NativeD3DDevice device,
        NativeTexture2DDescription description2D,
        DataBox[] dataBoxes
    )
        : base(device, description2D) {
        Resource = device.CreateTexture2D(description2D, dataBoxes);
        Initialize(Resource);
    }

    /// <summary>
    ///     Specialised constructor for use only by derived classes.
    /// </summary>
    /// <param name="device">The <see cref="Direct3D11.Device" />.</param>
    /// <param name="texture">The texture.</param>
    /// <msdn-id>ff476521</msdn-id>
    /// <unmanaged>
    ///     HRESULT ID3D11Device::CreateTexture2D([In] const D3D11_TEXTURE2D_DESC* pDesc,[In, Buffer, Optional] const
    ///     D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture2D** ppTexture2D)
    /// </unmanaged>
    /// <unmanaged-short>ID3D11Device::CreateTexture2D</unmanaged-short>
    protected internal Texture2DBase(NativeD3DDevice device, NativeD3DTexture2D texture)
        : base(device, texture.Description) {
        Resource = texture;
        Initialize(Resource);
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    protected virtual Format GetDefaultViewFormat() => Description.Format;

    /// <summary>
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="format"></param>
    /// <param name="textureFlags"></param>
    /// <param name="mipCount"></param>
    /// <param name="arraySize"></param>
    /// <param name="usage"></param>
    /// <returns></returns>
    protected static NativeTexture2DDescription NewDescription(
        int width,
        int height,
        PixelFormat format,
        TextureFlags textureFlags,
        int mipCount,
        int arraySize,
        ResourceUsage usage
    ) {
        if ((textureFlags & TextureFlags.UnorderedAccess) != 0)
            usage = ResourceUsage.Default;

        var desc = new NativeTexture2DDescription {
            Width = width,
            Height = height,
            ArraySize = arraySize,
            SampleDescription = new SampleDescription { Count = 1, Quality = 0 },
            BindFlags = GetBindFlagsFromTextureFlags(textureFlags),
            Format = format,
            MipLevels = CalculateMipMapCount(mipCount, width, height),
            Usage = usage,
            CpuAccessFlags = GetCpuAccessFlagsFromUsage(usage),
            OptionFlags = ResourceOptionFlags.None
        };


        // If the texture is a RenderTarget + ShaderResource + MipLevels > 1, then allow for GenerateMipMaps method
        if ((desc.BindFlags & BindFlags.RenderTarget) != 0 && (desc.BindFlags & BindFlags.ShaderResource) != 0 &&
            desc.MipLevels > 1) desc.OptionFlags |= ResourceOptionFlags.GenerateMipMaps;

        return desc;
    }

    protected override void Dispose(bool disposeManagedResources) {
        Resource.Dispose();
        base.Dispose(disposeManagedResources);
    }
}
