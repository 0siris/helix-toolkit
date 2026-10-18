namespace HelixToolkit.SharpDX.Core;

public static class PlaneExtensions {
    public static bool PlaneIntersectsPlane(ref Plane p1, ref Plane p2, out Ray intersection) {
        var dir = SilkMath.Cross(p1.Normal, p2.Normal);
        var det = SilkMath.Dot(dir, dir);
        if (Math.Abs(det) > float.Epsilon) {
            var p = (SilkMath.Cross(dir, p2.Normal) * p1.D + SilkMath.Cross(p1.Normal, dir) * p2.D) / det;
            intersection = new Ray(p, dir);
            return true;
        }

        intersection = default;
        return false;
    }
}
