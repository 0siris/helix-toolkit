/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using SharpDX.Toolkit.Graphics;

namespace HelixToolkit.SharpDX.Core.Utilities;

/// <summary>
///     Utilities to load textures.
/// </summary>
public static class TextureLoader {
    /// <summary>
    ///     Loads a texture from a file as a resource.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="fileName">The file name.</param>
    /// <returns></returns>
    public static Resource FromFileAsResource(NativeD3DDevice device, string fileName) {
        return Texture.Load(device, fileName)?.Resource;
    }

    /// <summary>
    ///     Loads a texture from a file as a shader resource view.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="disableAutoGenMipMap"></param>
    /// <returns></returns>
    public static ShaderResourceView FromFileAsShaderResourceView(
        NativeD3DDevice device,
        string fileName,
        bool disableAutoGenMipMap = false
    ) {
        using var texture = Texture.Load(device, fileName);
        if (texture == null) return null;
        if (!disableAutoGenMipMap &&
            texture.Description.MipLevels ==
            1) // Check if it already has mipmaps or not, if loaded DDS file, it may already has precompiled mipmaps, don't need to generate again
            if (GenerateMipMaps(device, texture, out var textureMipmap))
                using (textureMipmap) {
                    return device.CreateShaderResourceView(textureMipmap);
                }

        return device.CreateShaderResourceView(texture.Resource);
    }

    /// <summary>
    ///     Loads a texture from a memory buffer as a shader resource view.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="memory">The memory buffer.</param>
    /// <param name="disableAutoGenMipMap"></param>
    /// <returns></returns>
    public static ShaderResourceView FromMemoryAsShaderResourceView(
        NativeD3DDevice device,
        byte[] memory,
        bool disableAutoGenMipMap = false
    ) {
        using var memStream = new MemoryStream(memory);
        return FromMemoryAsShaderResourceView(device, memStream, disableAutoGenMipMap);
    }

    /// <summary>
    ///     Loads a texture from a memory buffer as a shader resource view.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="memory">The memory stream.</param>
    /// <param name="disableAutoGenMipMap"></param>
    /// <returns></returns>
    public static ShaderResourceView FromMemoryAsShaderResourceView(
        NativeD3DDevice device,
        Stream memory,
        bool disableAutoGenMipMap = false
    ) {
        using var texture = Texture.Load(device, memory);
        if (texture == null) return null;
        if (!disableAutoGenMipMap &&
            texture.Description.MipLevels ==
            1) // Check if it already has mipmaps or not, if loaded DDS file, it may already has precompiled mipmaps, don't need to generate again
            if (GenerateMipMaps(device, texture, out var textureMipmap))
                using (textureMipmap) {
                    return device.CreateShaderResourceView(textureMipmap);
                }

        return device.CreateShaderResourceView(texture.Resource);
    }

    /// <summary>
    ///     Froms the memory as shader resource.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="memory">The memory.</param>
    /// <param name="disableAutoGenMipMap">if set to <c>true</c> [disable automatic gen mip map].</param>
    /// <returns></returns>
    public static Resource FromMemoryAsShaderResource(
        NativeD3DDevice device,
        Stream memory,
        bool disableAutoGenMipMap = false
    ) {
        var texture = Texture.Load(device, memory);
        if (texture == null) return null;
        if (!disableAutoGenMipMap &&
            texture.Description.MipLevels ==
            1) // Check if it already has mipmaps or not, if loaded DDS file, it may already has precompiled mipmaps, don't need to generate again
            try {
                GenerateMipMaps(device, texture, out var textureMipmap);
                return textureMipmap;
            } catch (Exception ex) {
                throw new Exception(ex.Message);
            }

        return texture.Resource;
    }

    /// <summary>
    ///     Generates the mip maps.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <param name="texture">The texture.</param>
    /// <param name="textMip">Returns a new texture with mipmaps if succeeded. Otherwise returns the input texture</param>
    /// <returns>True succeed. False: Format not supported.</returns>
    /// <exception cref="InvalidDataException">Input texture is invalid.</exception>
    public static bool GenerateMipMaps(NativeD3DDevice device, Texture texture, out Resource textMip) {
        textMip = texture?.Resource;
        return false;
    }

    public static int GetSubResourceIndex(int arraySlice, int mipLevels, int mipSlice) {
        return arraySlice * mipLevels + mipSlice;
    }
}
