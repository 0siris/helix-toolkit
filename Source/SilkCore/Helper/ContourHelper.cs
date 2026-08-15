using System.Runtime.CompilerServices;

namespace HelixToolkit.SharpDX.Core;

using DoubleOrSingle = float;
using Point = Vector2;
using Point3D = Color3;
using Vector3D = Color3;

/// <summary>
///     Provides functionality to calculate a contour slice through a 3 vertex facet.(Modified from HelixToolkit.Wpf
///     version)
/// </summary>
/// <remarks>
///     See <a href="http://paulbourke.net/papers/conrec/">CONREC</a> for further information.
/// </remarks>
public class ContourHelper {
    /// <summary>
    ///     Provides the indices for the various <see cref="ContourFacetResult" /> cases.
    /// </summary>
    private static readonly IDictionary<ContourFacetResult, int[,]> ResultIndices
        = new Dictionary<ContourFacetResult, int[,]> {
            {ContourFacetResult.ZeroOnly, new[,] {{0, 1}, {0, 2}}},
            {ContourFacetResult.OneAndTwo, new[,] {{0, 2}, {0, 1}}},
            {ContourFacetResult.OneOnly, new[,] {{1, 2}, {1, 0}}},
            {ContourFacetResult.ZeroAndTwo, new[,] {{1, 0}, {1, 2}}},
            {ContourFacetResult.TwoOnly, new[,] {{2, 0}, {2, 1}}},
            {ContourFacetResult.ZeroAndOne, new[,] {{2, 1}, {2, 0}}}
        };

    /// <summary>
    ///     The parameter 'a' of the plane equation.
    /// </summary>
    private readonly DoubleOrSingle a;

    /// <summary>
    ///     The parameter 'b' of the plane equation.
    /// </summary>
    private readonly DoubleOrSingle b;

    /// <summary>
    ///     The parameter 'c' of the plane equation.
    /// </summary>
    private readonly DoubleOrSingle c;

    /// <summary>
    ///     The parameter 'd' of the plane equation.
    /// </summary>
    private readonly DoubleOrSingle d;

    /// <summary>
    ///     The indices.
    /// </summary>
    private readonly int[] indices = new int[3];

    /// <summary>
    ///     The original mesh normal vectors.
    /// </summary>
    private readonly Vector3D[] meshNormals;

    /// <summary>
    ///     The original mesh positions.
    /// </summary>
    private readonly Point3D[] meshPositions;

    /// <summary>
    ///     The original mesh texture coordinates.
    /// </summary>
    private readonly Point[] meshTextureCoordinates;

    /// <summary>
    ///     The normal vectors.
    /// </summary>
    private readonly Vector3D[] normals;

    /// <summary>
    ///     The points.
    /// </summary>
    private readonly Point3D[] points = new Point3D[3];

    /// <summary>
    ///     The sides.
    /// </summary>
    private readonly DoubleOrSingle[] sides = new DoubleOrSingle[3];

    /// <summary>
    ///     The texture coordinates.
    /// </summary>
    private readonly Point[] textures;

    /// <summary>
    ///     The position count.
    /// </summary>
    private int positionCount;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ContourHelper" /> class.
    /// </summary>
    /// <param name="planeOrigin">The plane origin.</param>
    /// <param name="planeNormal">The plane normal.</param>
    /// <param name="originalMesh">The original mesh.</param>
    public ContourHelper(Point3D planeOrigin, Vector3D planeNormal, MeshGeometry3D originalMesh) {
        if (originalMesh.Positions is not { } positions)
            throw new ArgumentException("The mesh must contain positions.", nameof(originalMesh));

        var sourceNormals = originalMesh.Normals;
        var sourceTextureCoordinates = originalMesh.TextureCoordinates;
        var hasNormals = sourceNormals is { Count: > 0 };
        var hasTextureCoordinates = sourceTextureCoordinates is { Count: > 0 };
        normals = hasNormals ? new Vector3D[3] : [];
        textures = hasTextureCoordinates ? new Point[3] : [];
        positionCount = positions.Count;

        meshPositions = [.. positions];
        meshNormals = sourceNormals is { } normalValues ? [.. normalValues] : [];
        meshTextureCoordinates = sourceTextureCoordinates is { } textureValues ? [.. textureValues] : [];

        // Determine the equation of the plane as
        // ax + by + cz + d = 0
        var l = (float)Math.Sqrt(planeNormal.X * planeNormal.X + planeNormal.Y * planeNormal.Y +
                                  planeNormal.Z * planeNormal.Z);
        a = planeNormal.X / l;
        b = planeNormal.Y / l;
        c = planeNormal.Z / l;
        d = -(planeNormal.X * planeOrigin.X + planeNormal.Y * planeOrigin.Y + planeNormal.Z * planeOrigin.Z);
    }

    /// <summary>
    ///     Create a contour slice through a 3 vertex facet.
    /// </summary>
    /// <param name="index0">The 0th point index.</param>
    /// <param name="index1">The 1st point index.</param>
    /// <param name="index2">The 2nd point index.</param>
    /// <param name="newPositions">Any new positions that are created, when the contour plane slices through the vertex.</param>
    /// <param name="newNormals">Any new normal vectors that are created, when the contour plane slices through the vertex.</param>
    /// <param name="newTextureCoordinates">
    ///     Any new texture coordinates that are created, when the contour plane slices through
    ///     the vertex.
    /// </param>
    /// <param name="triangleIndices">Triangle indices for the triangle(s) above the plane.</param>
    public void ContourFacet(
        int index0,
        int index1,
        int index2,
        out Point3D[] newPositions,
        out Vector3D[] newNormals,
        out Point[] newTextureCoordinates,
        out int[] triangleIndices
    ) {
        SetData(index0, index1, index2);

        var facetResult = GetContourFacet();

        switch (facetResult) {
            case ContourFacetResult.ZeroOnly:
                triangleIndices = [index0, positionCount++, positionCount++];
                break;
            case ContourFacetResult.OneAndTwo:
                triangleIndices = [index1, index2, positionCount, positionCount++, positionCount++, index1];
                break;
            case ContourFacetResult.OneOnly:
                triangleIndices = [index1, positionCount++, positionCount++];
                break;
            case ContourFacetResult.ZeroAndTwo:
                triangleIndices = [index2, index0, positionCount, positionCount++, positionCount++, index2];
                break;
            case ContourFacetResult.TwoOnly:
                triangleIndices = [index2, positionCount++, positionCount++];
                break;
            case ContourFacetResult.ZeroAndOne:
                triangleIndices = [index0, index1, positionCount, positionCount++, positionCount++, index0];
                break;
            case ContourFacetResult.All:
                newPositions = [];
                newNormals = [];
                newTextureCoordinates = [];
                triangleIndices = [index0, index1, index2];
                return;
            default:
                newPositions = [];
                newNormals = [];
                newTextureCoordinates = [];
                triangleIndices = [];
                return;
        }

        var facetIndices = ResultIndices[facetResult];
        newPositions = [
            CreateNewPosition(facetIndices[0, 0], facetIndices[0, 1]),
            CreateNewPosition(facetIndices[1, 0], facetIndices[1, 1])
        ];

        if (normals.Length != 0)
            newNormals = [
                CreateNewNormal(facetIndices[0, 0], facetIndices[0, 1]),
                CreateNewNormal(facetIndices[1, 0], facetIndices[1, 1])
            ];
        else
            newNormals = [];

        if (textures.Length != 0)
            newTextureCoordinates = [
                CreateNewTexture(facetIndices[0, 0], facetIndices[0, 1]),
                CreateNewTexture(facetIndices[1, 0], facetIndices[1, 1])
            ];
        else
            newTextureCoordinates = [];
    }

    /// <summary>
    ///     Calculates a new point coordinate.
    /// </summary>
    /// <param name="firstPoint">
    ///     The first point coordinate.
    /// </param>
    /// <param name="secondPoint">
    ///     The second point coordinate.
    /// </param>
    /// <param name="firstSide">
    ///     The first side.
    /// </param>
    /// <param name="secondSide">
    ///     The second side.
    /// </param>
    /// <returns>The new coordinate.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DoubleOrSingle CalculatePoint(
        DoubleOrSingle firstPoint,
        DoubleOrSingle secondPoint,
        DoubleOrSingle firstSide,
        DoubleOrSingle secondSide
    )
        => firstPoint - firstSide * (secondPoint - firstPoint) / (secondSide - firstSide);

    /// <summary>
    ///     Gets the <see cref="ContourFacetResult" /> for the current facet.
    /// </summary>
    /// <returns>a facet result.</returns>
    private ContourFacetResult GetContourFacet() {
        if (IsSideAlone(0)) return sides[0] > 0 ? ContourFacetResult.ZeroOnly : ContourFacetResult.OneAndTwo;

        if (IsSideAlone(1)) return sides[1] > 0 ? ContourFacetResult.OneOnly : ContourFacetResult.ZeroAndTwo;

        if (IsSideAlone(2)) return sides[2] > 0 ? ContourFacetResult.TwoOnly : ContourFacetResult.ZeroAndOne;

        if (AllSidesBelowContour()) return ContourFacetResult.All;

        return ContourFacetResult.None;
    }

    /// <summary>
    ///     Initializes the facet data and calculates the <see cref="sides" /> values from the specified triangle indices.
    /// </summary>
    /// <param name="index0">The first triangle index of the facet.</param>
    /// <param name="index1">The second triangle index of the facet.</param>
    /// <param name="index2">The third triangle index of the facet.</param>
    private void SetData(int index0, int index1, int index2) {
        indices[0] = index0;
        indices[1] = index1;
        indices[2] = index2;

        points[0] = meshPositions[index0];
        points[1] = meshPositions[index1];
        points[2] = meshPositions[index2];

        if (normals.Length != 0) {
            normals[0] = meshNormals[index0];
            normals[1] = meshNormals[index1];
            normals[2] = meshNormals[index2];
        }

        if (textures.Length != 0) {
            textures[0] = meshTextureCoordinates[index0];
            textures[1] = meshTextureCoordinates[index1];
            textures[2] = meshTextureCoordinates[index2];
        }

        sides[0] = a * points[0].X + b * points[0].Y + c * points[0].Z + d;
        sides[1] = a * points[1].X + b * points[1].Y + c * points[1].Z + d;
        sides[2] = a * points[2].X + b * points[2].Y + c * points[2].Z + d;
    }

    /// <summary>
    ///     Calculates the position at the plane intersection for the side specified by two triangle indices.
    /// </summary>
    /// <param name="index0">The first index.</param>
    /// <param name="index1">The second index.</param>
    /// <returns>The interpolated position.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Point3D CreateNewPosition(int index0, int index1) {
        var firstPoint = points[index0];
        var secondPoint = points[index1];
        var firstSide = sides[index0];
        var secondSide = sides[index1];
        return new Point3D(CalculatePoint(firstPoint.X, secondPoint.X, firstSide, secondSide),
                           CalculatePoint(firstPoint.Y, secondPoint.Y, firstSide, secondSide),
                           CalculatePoint(firstPoint.Z, secondPoint.Z, firstSide, secondSide));
    }

    /// <summary>
    ///     Calculates the normal at the plane intersection for the side specified by two triangle indices.
    /// </summary>
    /// <param name="index0">The first index.</param>
    /// <param name="index1">The second index.</param>
    /// <returns>The interpolated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector3D CreateNewNormal(int index0, int index1) {
        var firstPoint = normals[index0];
        var secondPoint = normals[index1];
        var firstSide = sides[index0];
        var secondSide = sides[index1];
        return new Vector3D(CalculatePoint(firstPoint.X, secondPoint.X, firstSide, secondSide),
                            CalculatePoint(firstPoint.Y, secondPoint.Y, firstSide, secondSide),
                            CalculatePoint(firstPoint.Z, secondPoint.Z, firstSide, secondSide));
    }

    /// <summary>
    ///     Calculates the texture coordinate at the plane intersection for the side specified by two triangle indices.
    /// </summary>
    /// <param name="index0">The first index.</param>
    /// <param name="index1">The second index.</param>
    /// <returns>The interpolated texture coordinate.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Point CreateNewTexture(int index0, int index1) {
        var firstTexture = textures[index0];
        var secondTexture = textures[index1];
        var firstSide = sides[index0];
        var secondSide = sides[index1];

        return new Point(CalculatePoint(firstTexture.X, secondTexture.X, firstSide, secondSide),
                         CalculatePoint(firstTexture.Y, secondTexture.Y, firstSide, secondSide));
    }

    /// <summary>
    ///     Determines whether the vertex at the specified index is at the opposite side of the other two vertices.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns><c>true</c> if the vertex is on its own side.</returns>
    private bool IsSideAlone(int index) {
        Func<int, int> getNext = i => i + 1 > 2 ? 0 : i + 1;

        var firstSideIndex = getNext(index);
        var secondSideIndex = getNext(firstSideIndex);
        return sides[index] * sides[firstSideIndex] < 0
               && sides[index] * sides[secondSideIndex] < 0;
    }

    /// <summary>
    ///     Determines whether all sides of the facet are below the contour.
    /// </summary>
    /// <returns><c>true</c> if all sides are below the contour.</returns>
    private bool AllSidesBelowContour() => sides[0] >= 0
                                           && sides[1] >= 0
                                           && sides[2] >= 0;

    /// <summary>
    ///     The contour facet result.
    /// </summary>
    private enum ContourFacetResult {
        /// <summary>
        ///     All of the points fall above the contour plane.
        /// </summary>
        None,

        /// <summary>
        ///     Only the 0th point falls below the contour plane.
        /// </summary>
        ZeroOnly,

        /// <summary>
        ///     The 1st and 2nd points fall below the contour plane.
        /// </summary>
        OneAndTwo,

        /// <summary>
        ///     Only the 1st point falls below the contour plane.
        /// </summary>
        OneOnly,

        /// <summary>
        ///     The 0th and 2nd points fall below the contour plane.
        /// </summary>
        ZeroAndTwo,

        /// <summary>
        ///     Only the second point falls below the contour plane.
        /// </summary>
        TwoOnly,

        /// <summary>
        ///     The 0th and 1st points fall below the contour plane.
        /// </summary>
        ZeroAndOne,

        /// <summary>
        ///     All of the points fall below the contour plane.
        /// </summary>
        All
    }
}
