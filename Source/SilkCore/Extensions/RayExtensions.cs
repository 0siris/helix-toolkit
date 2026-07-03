namespace HelixToolkit.SharpDX.Core;

public static class RayExtensions {
    public static bool Intersects(this Ray ray, ref BoundingBox box) {
        return Intersects(ref box, ref ray);
    }

    public static bool Intersects(this Ray ray, BoundingBox box) {
        return Intersects(ref box, ref ray);
    }

    public static bool Intersects(this BoundingBox box, ref Ray ray) {
        return Intersects(ref box, ref ray);
    }

    public static bool Intersects(this Ray ray, ref BoundingSphere sphere) {
        return sphere.Intersects(ref ray);
    }

    public static bool Intersects(this Ray ray, BoundingSphere sphere) {
        return sphere.Intersects(ref ray);
    }

    private static bool Intersects(ref BoundingBox box, ref Ray ray) {
        var minimumDistance = 0f;
        var maximumDistance = float.MaxValue;

        return IntersectsAxis(ray.Position.X,
                              ray.Direction.X,
                              box.Minimum.X,
                              box.Maximum.X,
                              ref minimumDistance,
                              ref maximumDistance)
               && IntersectsAxis(ray.Position.Y,
                                 ray.Direction.Y,
                                 box.Minimum.Y,
                                 box.Maximum.Y,
                                 ref minimumDistance,
                                 ref maximumDistance)
               && IntersectsAxis(ray.Position.Z,
                                 ray.Direction.Z,
                                 box.Minimum.Z,
                                 box.Maximum.Z,
                                 ref minimumDistance,
                                 ref maximumDistance);
    }

    private static bool IntersectsAxis(
        float origin,
        float direction,
        float minimum,
        float maximum,
        ref float minimumDistance,
        ref float maximumDistance
    ) {
        if (Math.Abs(direction) < 1e-6f) return origin >= minimum && origin <= maximum;

        var inverse = 1f / direction;
        var near = (minimum - origin) * inverse;
        var far = (maximum - origin) * inverse;
        if (near > far) {
            var swap = near;
            near = far;
            far = swap;
        }

        minimumDistance = Math.Max(minimumDistance, near);
        maximumDistance = Math.Min(maximumDistance, far);
        return minimumDistance <= maximumDistance;
    }

    /// <summary>
    ///     Planes the intersection with a ray.
    /// </summary>
    /// <param name="ray">The ray.</param>
    /// <param name="position">The plane position.</param>
    /// <param name="normal">The plane normal.</param>
    /// <param name="intersection">The intersection point.</param>
    /// <returns>Return true if intersect, return false if ray is not intersect with the plane</returns>
    public static bool PlaneIntersection(this Ray ray, Vector3 position, Vector3 normal, out Vector3 intersection) {
        // http://paulbourke.net/geometry/planeline/
        var dn = SilkMath.Dot(normal, ray.Direction);
        if (dn == 0) {
            intersection = new Vector3();
            return false;
        }

        var u = SilkMath.Dot(normal, position - ray.Position) / dn;
        intersection = ray.Position + u * ray.Direction;
        return true;
    }

    public static Ray UnProject(
        this Vector2 point2d,
        ref Matrix view,
        ref Matrix projection,
        float nearPlane,
        float w,
        float h,
        bool isPerpective
    ) {
        var px = point2d.X;
        var py = point2d.Y;

        var matrix = MatrixExtensions.PsudoInvert(ref view);

        var v = new Vector3 {
            X = (2 * px / w - 1) / projection.M11,
            Y = -(2 * py / h - 1) / projection.M22,
            Z = 1 / projection.M33
        };
        SilkMath.TransformCoordinate(ref v, ref matrix, out var zf);
        Vector3 zn;
        if (isPerpective) {
            zn = new Vector3(matrix.M41, matrix.M42, matrix.M43);
        } else {
            v.Z = 0;
            SilkMath.TransformCoordinate(ref v, ref matrix, out zn);
        }

        var r = zf - zn;
        r.Normalize();

        return new Ray(zn + r * nearPlane, r);
    }
}
