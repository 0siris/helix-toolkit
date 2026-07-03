/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


namespace HelixToolkit.SharpDX.Core;

/// <summary>
/// </summary>
public interface ITransform {
    /// <summary>
    ///     Local transform
    /// </summary>
    Matrix ModelMatrix { get; set; }

    /// <summary>
    ///     Transform from its parent
    /// </summary>
    Matrix ParentMatrix { get; set; }

    /// <summary>
    ///     Total model transform by ModelMatrix * ParentMatrix
    /// </summary>
    Matrix TotalModelMatrix { get; }
}

/// <summary>
/// </summary>
public interface ITransform2D {
    /// <summary>
    ///     Gets or sets the model matrix.
    /// </summary>
    /// <value>
    ///     The model matrix.
    /// </value>
    Matrix3x2 ModelMatrix { get; set; }

    /// <summary>
    ///     Gets or sets the parent matrix.
    /// </summary>
    /// <value>
    ///     The parent matrix.
    /// </value>
    Matrix3x2 ParentMatrix { get; set; }

    /// <summary>
    ///     Gets the total model matrix.
    /// </summary>
    /// <value>
    ///     The total model matrix.
    /// </value>
    Matrix3x2 TotalModelMatrix { get; }
}
