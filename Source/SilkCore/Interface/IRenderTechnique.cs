/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core;

/// <summary>
/// </summary>
public interface IRenderTechnique : IDisposable, IGuid {
    /// <summary>
    /// </summary>
    TechniqueDescription Description { get; }

    /// <summary>
    /// </summary>
    bool IsNull { get; }

    /// <summary>
    /// </summary>
    string Name { get; }

    /// <summary>
    /// </summary>
    NativeD3DDevice? Device { get; }

    /// <summary>
    ///     Input layout for all passes
    /// </summary>
    InputLayoutProxy Layout { get; }

    /// <summary>
    ///     All shader pass names
    /// </summary>
    IEnumerable<string> ShaderPassNames { get; }

    IConstantBufferPool ConstantBufferPool { get; }

    IEffectsManager EffectsManager { get; }

    /// <summary>
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    ShaderPass this[int index] { get; }

    /// <summary>
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    ShaderPass this[string name] { get; }

    /// <summary>
    ///     Get pass by name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    ShaderPass GetPass(string name);

    /// <summary>
    ///     Get pass by index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    ShaderPass GetPass(int index);

    /// <summary>
    ///     Adds the pass.
    /// </summary>
    /// <param name="description">The description.</param>
    /// <returns></returns>
    bool AddPass(ShaderPassDescription description);

    /// <summary>
    ///     Removes the pass.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    bool RemovePass(string name);
}
