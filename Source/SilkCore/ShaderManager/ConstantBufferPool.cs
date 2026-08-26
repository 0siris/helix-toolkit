/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.ShaderManager;
/// <summary>
///     Pool to store and share constant buffers. Do not dispose constant buffer object externally.
/// </summary>
public interface IConstantBufferPool : IDisposable {
    /// <summary>
    ///     Gets the count.
    /// </summary>
    /// <value>
    ///     The count.
    /// </value>
    int Count { get; }

    /// <summary>
    ///     Gets the device.
    /// </summary>
    /// <value>
    ///     The device.
    /// </value>
    object Device { get; }

    /// <summary>
    ///     Registers the specified description.
    /// </summary>
    /// <param name="description">The description.</param>
    /// <returns></returns>
    ConstantBufferProxy Register(ConstantBufferDescription description);

    /// <summary>
    ///     Registers the specified name.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="structSize">Size of the structure.</param>
    /// <returns></returns>
    ConstantBufferProxy Register(string name, int structSize);
}
