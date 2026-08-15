// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Geometry3DExtensions.cs" company="Helix Toolkit">
//   Copyright (c) 2017 Helix Toolkit contributors
// </copyright>
// <summary>
// Contains extension methods for geometry.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Geometry;

namespace HelixToolkit.Wpf.SharpDX.Extensions;

/// <summary>
///     Contains extension methods for geometry.
/// </summary>
public static class Geometry3DExtensions {
    /// <summary>
    ///     Returns a copy of this <see cref="Geometry3D" /> with unshared vertices.
    ///     Note that <see cref="Geometry3D.Indices" /> could be set to null if unindexed drawing was supported.
    /// </summary>
    /// <typeparam name="T">The concrete type of this <see cref="Geometry3D" />.</typeparam>
    /// <param name="source">This respective <see cref="Geometry3D" />.</param>
    /// <returns>A copy of this <see cref="Geometry3D" /> with unshared vertices.</returns>
    public static T ToUnshared<T>(this T source) where T : Geometry3D, new() {
        if (source.Indices is not { } indices || source.Positions is not { } positions)
            throw new InvalidOperationException("Unshared geometry requires indices and positions.");

        var result = new T {
            Indices = new IntCollection(indices.Count),
            Positions = new Vector3Collection(indices.Count)
        };
        if (source.Colors is { } colors && colors.Count == positions.Count) {
            result.Colors = new Color4Collection(indices.Count);
            for (var i = 0; i < indices.Count; i++) {
                result.Colors.Add(colors[indices[i]]);
                result.Positions.Add(positions[indices[i]]);
                result.Indices.Add(i);
            }
        } else {
            for (var i = 0; i < indices.Count; i++) {
                result.Positions.Add(positions[indices[i]]);
                result.Indices.Add(i);
            }
        }

        return result;
    }
}
