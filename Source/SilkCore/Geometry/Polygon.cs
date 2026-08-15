// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Polygon.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Represents a 2D polygon.
// </summary>
// --------------------------------------------------------------------------------------------------------------------


namespace HelixToolkit.SharpDX.Core.Geometry;

using Int32Collection = List<int>;
using PointCollection = List<Vector2>;

/// <summary>
///     Represents a 2D polygon.
/// </summary>
public class Polygon {
    // http://softsurfer.com/Archive/algorithm_0101/algorithm_0101.htm
    /// <summary>
    ///     The points.
    /// </summary>
    internal PointCollection? points;

    /// <summary>
    ///     Gets or sets the points.
    /// </summary>
    /// <value>The points.</value>
    public PointCollection Points {
        get => points ??= [];

        set => points = value;
    }

    /// <summary>
    ///     Triangulate the polygon by using the sweep line algorithm
    /// </summary>
    /// <returns>An index collection.</returns>
    public Int32Collection? Triangulate() => SweepLinePolygonTriangulator.Triangulate(Points);
}
