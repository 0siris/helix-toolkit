using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Utilities;
/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


namespace HelixToolkit.SharpDX.Core;

/// <summary>
/// </summary>
public interface IVertexExtraBufferModel : IGUID, IDisposable {
    /// <summary>
    ///     Gets a value indicating whether this <see cref="IVertexExtraBufferModel" /> is initialized.
    /// </summary>
    /// <value>
    ///     <c>true</c> if initialized; otherwise, <c>false</c>.
    /// </value>
    bool Initialized { get; }

    /// <summary>
    ///     Gets a value indicating whether this <see cref="IVertexExtraBufferModel" /> is changed.
    /// </summary>
    /// <value>
    ///     <c>true</c> if changed; otherwise, <c>false</c>.
    /// </value>
    bool Changed { get; }

    /// <summary>
    ///     Gets the buffer.
    /// </summary>
    /// <value>
    ///     The buffer.
    /// </value>
    IElementsBufferProxy? Buffer { get; }

    /// <summary>
    ///     Initializes this instance.
    /// </summary>
    void Initialize();
}
