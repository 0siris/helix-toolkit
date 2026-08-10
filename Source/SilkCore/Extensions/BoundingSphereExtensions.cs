/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core;

/// <summary>
/// </summary>
public static class BoundingSphereExtensions {
    /// <summary>
    ///     Froms the points.
    /// </summary>
    /// <param name="points">The points.</param>
    /// <param name="start">The start.</param>
    /// <param name="count">The count.</param>
    /// <returns></returns>
    public static BoundingSphere FromPoints(IList<Vector3> points, int start, int count) {
        if (points == null || start < 0 || start >= points.Count || count < 0 || start + count > points.Count)
            return new BoundingSphere();

        var upperEnd = start + count;

        //Find the center of all points.
        var center = Vector3.Zero;
        for (var i = start; i < upperEnd; ++i) {
            var p = points[i];
            SilkMath.Add(ref p, ref center, out center);
        }

        //This is the center of our sphere.
        center /= count;

        //Find the radius of the sphere
        var radius = 0f;
        for (var i = start; i < upperEnd; ++i) {
            //We are doing a relative distance comparison to find the maximum distance
            //from the center of our sphere.
            var p = points[i];
            SilkMath.DistanceSquared(ref center, ref p, out var distance);

            if (distance > radius)
                radius = distance;
        }

        //Find the real distance from the DistanceSquared.
        radius = (float)Math.Sqrt(radius);

        //Construct the sphere.
        return new BoundingSphere(center, radius);
    }

    /// <summary>
    ///     Froms the points.
    /// </summary>
    /// <param name="points">The points.</param>
    /// <returns></returns>
    public static BoundingSphere FromPoints(IList<Vector3> points) {
        if (points == null) return new BoundingSphere();

        return FromPoints(points, 0, points.Count);
    }

    public static BoundingSphere FromBox(BoundingBox box) {
        var center = box.Center();
        return new BoundingSphere(center, (box.Maximum - center).Length);
    }

    public static BoundingSphere Merge(BoundingSphere left, BoundingSphere right) {
        var difference = right.Center - left.Center;
        var distance = difference.Length;

        if (left.Radius >= distance + right.Radius) return left;
        if (right.Radius >= distance + left.Radius) return right;

        if (distance <= float.Epsilon) return new BoundingSphere(left.Center, Math.Max(left.Radius, right.Radius));

        var radius = (distance + left.Radius + right.Radius) * 0.5f;
        var center = left.Center + difference * ((radius - left.Radius) / distance);
        return new BoundingSphere(center, radius);
    }

    public static void Merge(ref BoundingSphere left, ref BoundingSphere right, out BoundingSphere result) {
        result = Merge(left, right);
    }

    public static bool Intersects(this BoundingSphere sphere, ref Ray ray) {
        var offset = ray.Position - sphere.Center;
        var a = SilkMath.Dot(ray.Direction, ray.Direction);
        if (a <= float.Epsilon) return SilkMath.Dot(offset, offset) <= sphere.Radius * sphere.Radius;

        var b = 2f * SilkMath.Dot(offset, ray.Direction);
        var c = SilkMath.Dot(offset, offset) - sphere.Radius * sphere.Radius;
        var discriminant = b * b - 4f * a * c;
        if (discriminant < 0) return false;

        var root = (float)Math.Sqrt(discriminant);
        var inverse = 0.5f / a;
        return (-b - root) * inverse >= 0 || (-b + root) * inverse >= 0;
    }

    public static ContainmentType Contains(BoundingSphere sphere, Vector3 point) {
        return SilkMath.DistanceSquared(sphere.Center, point) <= sphere.Radius * sphere.Radius
                   ? ContainmentType.Contains
                   : ContainmentType.Disjoint;
    }

    /// <summary>
    ///     Transforms the bounding sphere.
    /// </summary>
    /// <param name="b">The b.</param>
    /// <param name="m">The m.</param>
    /// <returns></returns>
    public static BoundingSphere TransformBoundingSphere(this BoundingSphere b, Matrix m) {
        var center = b.Center;
        var edgeX = b.Center + Vector3.UnitX * b.Radius;
        var edgeY = b.Center + Vector3.UnitY * b.Radius;
        var edgeZ = b.Center + Vector3.UnitZ * b.Radius;

        var worldCenter = SilkMath.Transform(center, m);
        var worldEdgeX = SilkMath.Transform(edgeX, m);
        var worldEdgeY = SilkMath.Transform(edgeY, m);
        var worldEdgeZ = SilkMath.Transform(edgeZ, m);

        var maxRadius = (float)Math.Sqrt(Math.Max(Math.Max((worldEdgeX - worldCenter).LengthSquared(),
                                                            (worldEdgeY - worldCenter).LengthSquared()),
                                                   (worldEdgeZ - worldCenter).LengthSquared()));

        return new BoundingSphere(worldCenter.ToXyz(), maxRadius);
    }
}
