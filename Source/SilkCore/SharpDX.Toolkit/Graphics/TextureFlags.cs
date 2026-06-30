/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
using System;

namespace SharpDX.Toolkit.Graphics
{
    /// <summary>
    /// Specifies usage of a texture.
    /// </summary>
    [Flags]
    public enum TextureFlags
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0,

        /// <summary>
        /// The texture will be used as a shader resource view.
        /// </summary>
        ShaderResource = 1,

        /// <summary>
        /// The texture will be used as a render target view.
        /// </summary>
        RenderTarget = 2,

        /// <summary>
        /// The texture will be used as an unordered access view.
        /// </summary>
        UnorderedAccess = 4,
    }
}
