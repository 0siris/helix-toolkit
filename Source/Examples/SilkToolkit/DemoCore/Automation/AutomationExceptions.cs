// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AutomationExceptions.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DemoCore.Automation;

/// <summary>
///     The demo exposes no scene host, so model and scene operations are unavailable.
/// </summary>
public sealed class NoSceneHostException : InvalidOperationException {
    /// <summary>
    ///     Initializes a new instance of the <see cref="NoSceneHostException" /> class.
    /// </summary>
    public NoSceneHostException()
        : base("no-scene-host: this demo registers no scene host, model loading is unavailable.") {
    }
}

/// <summary>
///     A model file could not be converted into a scene.
/// </summary>
public sealed class ModelLoadException : InvalidOperationException {
    /// <summary>
    ///     Initializes a new instance of the <see cref="ModelLoadException" /> class.
    /// </summary>
    /// <param name="message">The failure reason.</param>
    public ModelLoadException(string message)
        : base(message) {
    }
}

/// <summary>
///     A scene node has no computable world bounds.
/// </summary>
public sealed class NoNodeBoundException : InvalidOperationException {
    /// <summary>
    ///     Initializes a new instance of the <see cref="NoNodeBoundException" /> class.
    /// </summary>
    /// <param name="id">The node id.</param>
    public NoNodeBoundException(Guid id)
        : base($"no-bound: scene node '{id}' has no computable bounds.") {
    }
}
