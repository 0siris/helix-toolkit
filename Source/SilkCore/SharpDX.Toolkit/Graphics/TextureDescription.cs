/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.InteropServices;

namespace SharpDX.Toolkit.Graphics;

/// <summary>
///     A Common description for all textures.
/// </summary>
/// <remarks>
///     This class exposes the union of all fields exposed by fields in <see cref="Direct3D11.Texture1DDescription" />,
///     <see cref="Direct3D11.Texture2DDescription" />, <see cref="Direct3D11.Texture3DDescription" />.
///     It provides also 2-way implicit conversions for 1D, 2D, 3D textures descriptions.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct TextureDescription : IEquatable<TextureDescription> {
    /// <summary>
    ///     The dimension of a texture.
    /// </summary>
    public TextureDimension Dimension;

    /// <summary>
    ///     Texture width in texels.
    /// </summary>
    /// <remarks>
    ///     This field is valid for all textures: <see cref="Texture1D" />, <see cref="Texture2D" />, <see cref="Texture3D" />
    ///     and <see cref="TextureCube" />.
    /// </remarks>
    /// <msdn-id>ff476252</msdn-id>
    /// <unmanaged>unsigned int Width</unmanaged>
    /// <unmanaged-short>unsigned int Width</unmanaged-short>
    public int Width;

    /// <summary>
    ///     Texture height in texels.
    /// </summary>
    /// <remarks>
    ///     This field is only valid for <see cref="Texture2D" />, <see cref="Texture3D" /> and <see cref="TextureCube" />.
    /// </remarks>
    /// <msdn-id>ff476254</msdn-id>
    /// <unmanaged>unsigned int Height</unmanaged>
    /// <unmanaged-short>unsigned int Height</unmanaged-short>
    public int Height;

    /// <summary>
    ///     Texture depth in texels.
    /// </summary>
    /// <remarks>
    ///     This field is only valid for <see cref="Texture3D" />.
    /// </remarks>
    /// <msdn-id>ff476254</msdn-id>
    /// <unmanaged>unsigned int Depth</unmanaged>
    /// <unmanaged-short>unsigned int Depth</unmanaged-short>
    public int Depth;

    /// <summary>
    ///     Number of textures in the array.
    /// </summary>
    /// <remarks>
    ///     This field is only valid for <see cref="Texture1D" />, <see cref="Texture2D" /> and <see cref="TextureCube" />
    /// </remarks>
    /// <remarks>
    ///     This field is only valid for textures: <see cref="Texture1D" />, <see cref="Texture2D" /> and
    ///     <see cref="TextureCube" />.
    /// </remarks>
    /// <msdn-id>ff476252</msdn-id>
    /// <unmanaged>unsigned int ArraySize</unmanaged>
    /// <unmanaged-short>unsigned int ArraySize</unmanaged-short>
    public int ArraySize;

    /// <summary>
    ///     Maximum number of mipmap levels in the texture.
    /// </summary>
    /// <msdn-id>ff476252</msdn-id>
    /// <unmanaged>unsigned int MipLevels</unmanaged>
    /// <unmanaged-short>unsigned int MipLevels</unmanaged-short>
    public int MipLevels;

    /// <summary>
    ///     Texture format.
    /// </summary>
    /// <msdn-id>ff476252</msdn-id>
    /// <unmanaged>DXGI_FORMAT Format</unmanaged>
    /// <unmanaged-short>DXGI_FORMAT Format</unmanaged-short>
    public Format Format;

    /// <summary>
    ///     Structure that specifies multisampling parameters for the texture.
    /// </summary>
    /// <remarks>
    ///     This field is only valid for <see cref="Texture2D" />.
    /// </remarks>
    /// <msdn-id>ff476253</msdn-id>
    /// <unmanaged>DXGI_SAMPLE_DESC SampleDesc</unmanaged>
    /// <unmanaged-short>DXGI_SAMPLE_DESC SampleDesc</unmanaged-short>
    public SampleDescription SampleDescription;

    /// <summary>
    ///     Value that identifies how the texture is to be read from and written to.
    /// </summary>
    /// <msdn-id>ff476252</msdn-id>
    /// <unmanaged>D3D11_USAGE Usage</unmanaged>
    /// <unmanaged-short>D3D11_USAGE Usage</unmanaged-short>
    public ResourceUsage Usage;

    /// <summary>
    ///     Flags for binding to pipeline stages.
    /// </summary>
    /// <msdn-id>ff476252</msdn-id>
    /// <unmanaged>D3D11_BIND_FLAG BindFlags</unmanaged>
    /// <unmanaged-short>D3D11_BIND_FLAG BindFlags</unmanaged-short>
    public BindFlags BindFlags;

    /// <summary>
    ///     Flags to specify the types of CPU access allowed.
    /// </summary>
    /// <msdn-id>ff476252</msdn-id>
    /// <unmanaged>D3D11_CPU_ACCESS_FLAG CPUAccessFlags</unmanaged>
    /// <unmanaged-short>D3D11_CPU_ACCESS_FLAG CPUAccessFlags</unmanaged-short>
    public CpuAccessFlags CpuAccessFlags;

    /// <summary>
    ///     Flags that identify other, less common resource options.
    /// </summary>
    /// <msdn-id>ff476252</msdn-id>
    /// <unmanaged>D3D11_RESOURCE_MISC_FLAG MiscFlags</unmanaged>
    /// <unmanaged-short>D3D11_RESOURCE_MISC_FLAG MiscFlags</unmanaged-short>
    public ResourceOptionFlags OptionFlags;

    /// <summary>
    ///     Gets the staging description for this instance..
    /// </summary>
    /// <returns>A Staging description</returns>
    public TextureDescription ToStagingDescription() {
        var copy = this;
        copy.BindFlags = BindFlags.None;
        copy.CpuAccessFlags = CpuAccessFlags.Read | CpuAccessFlags.Write;
        copy.Usage = ResourceUsage.Staging;
        copy.OptionFlags = copy.Dimension == TextureDimension.TextureCube
                               ? ResourceOptionFlags.TextureCube
                               : ResourceOptionFlags.None;
        return copy;
    }

    /// <summary>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(TextureDescription other) {
        return Dimension.Equals(other.Dimension) && Width == other.Width && Height == other.Height &&
               Depth == other.Depth && ArraySize == other.ArraySize && MipLevels == other.MipLevels &&
               Format.Equals(other.Format) && SampleDescription.Equals(other.SampleDescription) &&
               Usage.Equals(other.Usage) && BindFlags.Equals(other.BindFlags) &&
               CpuAccessFlags.Equals(other.CpuAccessFlags) && OptionFlags.Equals(other.OptionFlags);
    }

    /// <summary>
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj) {
        if (ReferenceEquals(null, obj)) return false;
        return obj is TextureDescription && Equals((TextureDescription)obj);
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode() {
        unchecked {
            var hashCode = Dimension.GetHashCode();
            hashCode = (hashCode * 397) ^ Width;
            hashCode = (hashCode * 397) ^ Height;
            hashCode = (hashCode * 397) ^ Depth;
            hashCode = (hashCode * 397) ^ ArraySize;
            hashCode = (hashCode * 397) ^ MipLevels;
            hashCode = (hashCode * 397) ^ Format.GetHashCode();
            hashCode = (hashCode * 397) ^ SampleDescription.GetHashCode();
            hashCode = (hashCode * 397) ^ Usage.GetHashCode();
            hashCode = (hashCode * 397) ^ BindFlags.GetHashCode();
            hashCode = (hashCode * 397) ^ CpuAccessFlags.GetHashCode();
            hashCode = (hashCode * 397) ^ OptionFlags.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(TextureDescription left, TextureDescription right) {
        return left.Equals(right);
    }

    /// <summary>
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(TextureDescription left, TextureDescription right) {
        return !left.Equals(right);
    }

    /// <summary>
    ///     Performs an explicit conversion from <see cref="Texture2DDescription" /> to <see cref="TextureDescription" />.
    /// </summary>
    /// <param name="description">The texture description.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator TextureDescription(NativeTexture1DDescription description) {
        return new TextureDescription {
            Dimension = TextureDimension.Texture1D,
            Width = description.Width,
            Height = 1,
            Depth = 1,
            MipLevels = description.MipLevels,
            ArraySize = description.ArraySize,
            Format = description.Format,
            SampleDescription = new SampleDescription { Count = 1, Quality = 0 },
            Usage = description.Usage,
            BindFlags = description.BindFlags,
            CpuAccessFlags = description.CpuAccessFlags,
            OptionFlags = description.OptionFlags
        };
    }

    /// <summary>
    ///     Performs an explicit conversion from <see cref="TextureDescription" /> to <see cref="Texture2DDescription" />.
    /// </summary>
    /// <param name="description">The texture description.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator NativeTexture1DDescription(TextureDescription description) {
        return new NativeTexture1DDescription {
            Width = description.Width,
            MipLevels = description.MipLevels,
            ArraySize = description.ArraySize,
            Format = description.Format,
            Usage = description.Usage,
            BindFlags = description.BindFlags,
            CpuAccessFlags = description.CpuAccessFlags,
            OptionFlags = description.OptionFlags
        };
    }

    /// <summary>
    ///     Performs an explicit conversion from <see cref="Texture2DDescription" /> to <see cref="TextureDescription" />.
    /// </summary>
    /// <param name="description">The texture description.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator TextureDescription(NativeTexture2DDescription description) {
        var dimension = description.ArraySize == 6 && (description.OptionFlags & ResourceOptionFlags.TextureCube) != 0
                            ? TextureDimension.TextureCube
                            : TextureDimension.Texture2D;

        return new TextureDescription {
            Dimension = dimension,
            Width = description.Width,
            Height = description.Height,
            Depth = 1,
            MipLevels = description.MipLevels,
            ArraySize = description.ArraySize,
            Format = description.Format,
            SampleDescription = description.SampleDescription,
            Usage = description.Usage,
            BindFlags = description.BindFlags,
            CpuAccessFlags = description.CpuAccessFlags,
            OptionFlags = description.OptionFlags
        };
    }

    /// <summary>
    ///     Performs an explicit conversion from <see cref="TextureDescription" /> to <see cref="Texture2DDescription" />.
    /// </summary>
    /// <param name="description">The texture description.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator NativeTexture2DDescription(TextureDescription description) {
        return new NativeTexture2DDescription {
            Width = description.Width,
            Height = description.Height,
            MipLevels = description.MipLevels,
            ArraySize = description.ArraySize,
            Format = description.Format,
            SampleDescription = description.SampleDescription,
            Usage = description.Usage,
            BindFlags = description.BindFlags,
            CpuAccessFlags = description.CpuAccessFlags,
            OptionFlags = description.OptionFlags
        };
    }

    /// <summary>
    ///     Performs an explicit conversion from <see cref="Texture2DDescription" /> to <see cref="TextureDescription" />.
    /// </summary>
    /// <param name="description">The texture description.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator TextureDescription(NativeTexture3DDescription description) {
        return new TextureDescription {
            Dimension = TextureDimension.Texture3D,
            Width = description.Width,
            Height = description.Height,
            Depth = description.Depth,
            ArraySize = 1,
            MipLevels = description.MipLevels,
            Format = description.Format,
            SampleDescription = new SampleDescription { Count = 1, Quality = 0 },
            Usage = description.Usage,
            BindFlags = description.BindFlags,
            CpuAccessFlags = description.CpuAccessFlags,
            OptionFlags = description.OptionFlags
        };
    }

    /// <summary>
    ///     Performs an explicit conversion from <see cref="TextureDescription" /> to <see cref="Texture3DDescription" />.
    /// </summary>
    /// <param name="description">The texture description.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator NativeTexture3DDescription(TextureDescription description) {
        return new NativeTexture3DDescription {
            Width = description.Width,
            Height = description.Height,
            Depth = description.Depth,
            MipLevels = description.MipLevels,
            Format = description.Format,
            Usage = description.Usage,
            BindFlags = description.BindFlags,
            CpuAccessFlags = description.CpuAccessFlags,
            OptionFlags = description.OptionFlags
        };
    }


    /// <summary>
    ///     Performs an explicit conversion from <see cref="ImageDescription" /> to <see cref="TextureDescription" />.
    /// </summary>
    /// <param name="description">The image description.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator TextureDescription(ImageDescription description) {
        return new TextureDescription {
            Dimension = description.Dimension,
            Width = description.Width,
            Height = description.Height,
            Depth = description.Depth,
            ArraySize = description.ArraySize,
            MipLevels = description.MipLevels,
            Format = description.Format,
            SampleDescription = new SampleDescription { Count = 1, Quality = 0 },
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.None,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = description.Dimension == TextureDimension.TextureCube
                              ? ResourceOptionFlags.TextureCube
                              : ResourceOptionFlags.None
        };
    }

    /// <summary>
    ///     Performs an explicit conversion from <see cref="ImageDescription" /> to <see cref="TextureDescription" />.
    /// </summary>
    /// <param name="description">The image description.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator ImageDescription(TextureDescription description) {
        return new ImageDescription {
            Dimension = description.Dimension,
            Width = description.Width,
            Height = description.Height,
            Depth = description.Depth,
            ArraySize = description.ArraySize,
            MipLevels = description.MipLevels,
            Format = description.Format
        };
    }
}
