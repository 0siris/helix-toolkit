/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.InteropServices;

namespace SharpDX.Toolkit.Graphics;

/// <summary>
///     A description for <see cref="Image" />.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ImageDescription : IEquatable<ImageDescription>
{
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
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(ImageDescription other)
    {
        return Dimension.Equals(other.Dimension) && Width == other.Width && Height == other.Height &&
               Depth == other.Depth && ArraySize == other.ArraySize && MipLevels == other.MipLevels &&
               Format.Equals(other.Format);
    }

    /// <summary>
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
        if (obj is ImageDescription o) return Equals(o);

        return false;
        //if (ReferenceEquals(null, obj)) return false;
        //return obj is ImageDescription && Equals((ImageDescription) obj);
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Dimension.GetHashCode();
            hashCode = (hashCode * 397) ^ Width;
            hashCode = (hashCode * 397) ^ Height;
            hashCode = (hashCode * 397) ^ Depth;
            hashCode = (hashCode * 397) ^ ArraySize;
            hashCode = (hashCode * 397) ^ MipLevels;
            hashCode = (hashCode * 397) ^ Format.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(ImageDescription left, ImageDescription right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(ImageDescription left, ImageDescription right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return string.Format(
            "Dimension: {0}, Width: {1}, Height: {2}, Depth: {3}, Format: {4}, ArraySize: {5}, MipLevels: {6}",
            Dimension, Width, Height, Depth, Format, ArraySize, MipLevels);
    }
}