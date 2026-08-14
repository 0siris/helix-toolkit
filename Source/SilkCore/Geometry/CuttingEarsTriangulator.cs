// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CuttingEarsTriangulator.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Provides a cutting ears triangulation algorithm for simple polygons with no holes. O(n^2)
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HelixToolkit.Wpf;

using Int32Collection = List<int>;
using Point = Vector2;


#pragma warning disable 0436
/// <summary>
///     Provides a cutting ears triangulation algorithm for simple polygons with no holes. O(n^2)
/// </summary>
/// <remarks>
///     Based on <a href="http://www.flipcode.com/archives/Efficient_Polygon_Triangulation.shtml">code</a>
///     References
///     <a href="http://en.wikipedia.org/wiki/Polygon_triangulation"></a>
///     <a href="http://computacion.cs.cinvestav.mx/~anzures/geom/triangulation.php"></a>
///     <a href="http://www.codeproject.com/KB/recipes/cspolygontriangulation.aspx"></a>
/// </remarks>
public static class CuttingEarsTriangulator {
    /// <summary>
    ///     The epsilon.
    /// </summary>
    private const double Epsilon = 1e-10;

    /// <summary>
    ///     Triangulate a polygon using the cutting ears algorithm.
    /// </summary>
    /// <remarks>
    ///     The algorithm does not support holes.
    /// </remarks>
    /// <param name="contour">
    ///     the polygon contour
    /// </param>
    /// <returns>
    ///     collection of triangle points
    /// </returns>
    public static Int32Collection? Triangulate(IList<Point> contour) {
        // allocate and initialize list of indices in polygon
        var result = new Int32Collection();

        var n = contour.Count;
        if (n < 3) return null;

        var V = new int[n];

        // we want a counter-clockwise polygon in V
        if (Area(contour) > 0)
            for (var v = 0; v < n; v++)
                V[v] = v;
        else
            for (var v = 0; v < n; v++)
                V[v] = n - 1 - v;

        var nv = n;

        // remove nv-2 Vertices, creating 1 triangle every time
        var count = 2 * nv; // error detection

        for (var v = nv - 1; nv > 2;) {
            // if we loop, it is probably a non-simple polygon
            if (0 >= count--)
                // ERROR - probable bad polygon!
                return null;

            // three consecutive vertices in current polygon, <u,v,w>
            var u = v;
            if (nv <= u) u = 0; // previous

            v = u + 1;
            if (nv <= v) v = 0; // new v

            var w = v + 1;
            if (nv <= w) w = 0; // next

            if (Snip(contour, u, v, w, nv, V)) {
                int s, t;

                // true names of the vertices
                var a = V[u];
                var b = V[v];
                var c = V[w];

                // output Triangle
                result.Add(a);
                result.Add(b);
                result.Add(c);

                // remove v from remaining polygon
                for (s = v, t = v + 1; t < nv; s++, t++) V[s] = V[t];

                nv--;

                // resest error detection counter
                count = 2 * nv;
            }
        }

        return result;
    }

    /// <summary>
    ///     Calculates the area.
    /// </summary>
    /// <param name="contour">The contour.</param>
    /// <returns>The area.</returns>
    private static double Area(IList<Point> contour) {
        var n = contour.Count;
        var area = 0.0;
        for (int p = n - 1, q = 0; q < n; p = q++) area += contour[p].X * contour[q].Y - contour[q].X * contour[p].Y;

        return area * 0.5f;
    }

    /// <summary>
    ///     Decide if point (Px,Py) is inside triangle defined by (Ax,Ay) (Bx,By) (Cx,Cy).
    /// </summary>
    /// <param name="ax">
    ///     The ax.
    /// </param>
    /// <param name="ay">
    ///     The ay.
    /// </param>
    /// <param name="bx">
    ///     The bx.
    /// </param>
    /// <param name="by">
    ///     The by.
    /// </param>
    /// <param name="cx">
    ///     The cx.
    /// </param>
    /// <param name="cy">
    ///     The cy.
    /// </param>
    /// <param name="px">
    ///     The px.
    /// </param>
    /// <param name="py">
    ///     The py.
    /// </param>
    /// <returns>
    ///     The inside triangle.
    /// </returns>
    private static bool InsideTriangle(
        double ax,
        double ay,
        double bx,
        double by,
        double cx,
        double cy,
        double px,
        double py
    ) {
        double aDx, aDy, bDx, bDy, cDx, cDy, apx, apy, bpx, bpy, cpx, cpy;
        double cCrosSap, bCrosScp, aCrosSbp;

        aDx = cx - bx;
        aDy = cy - by;
        bDx = ax - cx;
        bDy = ay - cy;
        cDx = bx - ax;
        cDy = by - ay;
        apx = px - ax;
        apy = py - ay;
        bpx = px - bx;
        bpy = py - by;
        cpx = px - cx;
        cpy = py - cy;

        aCrosSbp = aDx * bpy - aDy * bpx;
        cCrosSap = cDx * apy - cDy * apx;
        bCrosScp = bDx * cpy - bDy * cpx;

        // use an absolute tolerance when comparing floating point values
        const double epsilon = -1e-10;
        return aCrosSbp > epsilon && bCrosScp > epsilon && cCrosSap > epsilon;
    }

    /// <summary>
    ///     The snip.
    /// </summary>
    /// <param name="contour">The contour.</param>
    /// <param name="u">The u.</param>
    /// <param name="V">The vertices.</param>
    /// <param name="w">The w.</param>
    /// <param name="n">The n.</param>
    /// <param name="v">The v.</param>
    /// <returns>The snip.</returns>
    private static bool Snip(IList<Point> contour, int u, int v, int w, int n, int[] V) {
        int p;
        double ax, ay, bx, by, cx, cy, px, py;

        ax = contour[V[u]].X;
        ay = contour[V[u]].Y;

        bx = contour[V[v]].X;
        by = contour[V[v]].Y;

        cx = contour[V[w]].X;
        cy = contour[V[w]].Y;

        if (Epsilon > (bx - ax) * (cy - ay) - (by - ay) * (cx - ax)) return false;

        for (p = 0; p < n; p++) {
            if (p == u || p == v || p == w) continue;

            px = contour[V[p]].X;
            py = contour[V[p]].Y;
            if (InsideTriangle(ax, ay, bx, by, cx, cy, px, py)) return false;
        }

        return true;
    }
}
#pragma warning restore 0436
