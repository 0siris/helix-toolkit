/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Interface;
/// <summary>
/// </summary>
public interface IShaderPoolManager : IDisposable {
    /// <summary>
    ///     Registers the shader. Shader object live time is managed by ShaderPoolManager. Shader should not be disposed
    ///     manually.
    /// </summary>
    /// <param name="description">The description.</param>
    /// <returns></returns>
    ShaderBase? RegisterShader(ShaderDescription description);

    /// <summary>
    ///     Registers the input layout. Input layout object live time is managed by ShaderPoolManager. Input layout should not
    ///     be disposed manually
    /// </summary>
    /// <param name="description">The description.</param>
    /// <returns></returns>
    InputLayoutProxy? RegisterInputLayout(InputLayoutDescription description);
}

/// <summary>
/// </summary>
public interface IStatePoolManager : IDisposable {
    /// <summary>
    ///     Registers the specified desc. This function increments state proxy internal reference counter. Must be disposed if
    ///     not used.
    /// </summary>
    /// <param name="desc">The desc.</param>
    /// <returns></returns>
    BlendStateProxy Register(BlendStateDescription desc);

    /// <summary>
    ///     Registers the specified desc. This function increments state proxy internal reference counter. Must be disposed if
    ///     not used.
    /// </summary>
    /// <param name="desc">The desc.</param>
    /// <returns></returns>
    RasterizerStateProxy Register(RasterizerStateDescription desc);

    /// <summary>
    ///     Registers the specified desc. This function increments state proxy internal reference counter. Must be disposed if
    ///     not used.
    /// </summary>
    /// <param name="desc">The desc.</param>
    /// <returns></returns>
    DepthStencilStateProxy Register(DepthStencilStateDescription desc);

    /// <summary>
    ///     Registers the specified desc. This function increments state proxy internal reference counter. Must be disposed if
    ///     not used.
    /// </summary>
    /// <param name="desc">The desc.</param>
    /// <returns></returns>
    SamplerStateProxy Register(SamplerStateDescription desc);
}
