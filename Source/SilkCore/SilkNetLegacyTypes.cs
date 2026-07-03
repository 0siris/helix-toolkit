using System.Collections;
#if SILKNET
#if !CORE
namespace HelixToolkit.Wpf.SharpDX
{
    using System.Collections;
    using System.Collections.Generic;

    public enum PlaneIntersectionType
    {
        Front,
        Back,
        Intersecting
    }

    public enum ContainmentType
    {
        Disjoint,
        Contains,
        Intersects
    }

    public struct Ray
    {
        public Vector3 Position;
        public Vector3 Direction;

        public Ray(Vector3 position, Vector3 direction)
        {
            Position = position;
            Direction = direction;
        }

        public Vector3 Origin
        {
            get => Position;
            set => Position = value;
        }

        public Vector3 GetPoint(float distance)
        {
            return Position + Direction * distance;
        }
    }

    public struct Plane
    {
        public Vector3 Normal;
        public float D;

        public Plane(Vector3 normal, float d)
        {
            Normal = normal;
            D = d;
        }

        public Plane(Vector3 point, Vector3 normal)
        {
            Normal = Collision.Normalize(normal);
            D = -Collision.Dot(Normal, point);
        }

        public bool Intersects(ref Ray ray, out float distance)
        {
            return Collision.RayIntersectsPlane(ref ray, ref this, out distance);
        }

        public PlaneIntersectionType Intersects(ref BoundingSphere sphere)
        {
            var distance = Collision.Dot(Normal, sphere.Center) + D;
            if (distance > sphere.Radius)
            {
                return PlaneIntersectionType.Front;
            }
            if (distance < -sphere.Radius)
            {
                return PlaneIntersectionType.Back;
            }
            return PlaneIntersectionType.Intersecting;
        }
    }

    public static class Collision
    {
        private const float Epsilon = 1e-6f;

        public static float Dot(Vector3 left, Vector3 right)
        {
            return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
        }

        public static Vector3 Cross(Vector3 left, Vector3 right)
        {
            return new Vector3(
                left.Y * right.Z - left.Z * right.Y,
                left.Z * right.X - left.X * right.Z,
                left.X * right.Y - left.Y * right.X);
        }

        public static Vector3 Normalize(Vector3 vector)
        {
            var length = vector.Length;
            return length > 0 ? vector / length : vector;
        }

        public static PlaneIntersectionType PlaneIntersectsPoint(ref Plane plane, ref Vector3 point)
        {
            var distance = Dot(plane.Normal, point) + plane.D;
            return distance > 0 ? PlaneIntersectionType.Front : distance < 0 ? PlaneIntersectionType.Back : PlaneIntersectionType.Intersecting;
        }

        public static bool RayIntersectsPlane(ref Ray ray, ref Plane plane, out float distance)
        {
            var denominator = Dot(plane.Normal, ray.Direction);
            if (global::System.Math.Abs(denominator) < Epsilon)
            {
                distance = 0;
                return false;
            }

            distance = -(Dot(plane.Normal, ray.Origin) + plane.D) / denominator;
            return true;
        }

        public static bool RayIntersectsPlane(ref Ray ray, ref Plane plane, out Vector3 point)
        {
            float distance;
            if (RayIntersectsPlane(ref ray, ref plane, out distance))
            {
                point = ray.Origin + ray.Direction * distance;
                return true;
            }

            point = default;
            return false;
        }

        public static bool RayIntersectsTriangle(ref Ray ray, ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3, out float distance)
        {
            var edge1 = vertex2 - vertex1;
            var edge2 = vertex3 - vertex1;
            var directionCrossEdge2 = Cross(ray.Direction, edge2);
            var determinant = Dot(edge1, directionCrossEdge2);

            if (global::System.Math.Abs(determinant) < Epsilon)
            {
                distance = 0;
                return false;
            }

            var inverseDeterminant = 1.0f / determinant;
            var distanceVector = ray.Origin - vertex1;
            var triangleU = Dot(distanceVector, directionCrossEdge2) * inverseDeterminant;
            if (triangleU < 0 || triangleU > 1)
            {
                distance = 0;
                return false;
            }

            var distanceCrossEdge1 = Cross(distanceVector, edge1);
            var triangleV = Dot(ray.Direction, distanceCrossEdge1) * inverseDeterminant;
            if (triangleV < 0 || triangleU + triangleV > 1)
            {
                distance = 0;
                return false;
            }

            distance = Dot(edge2, distanceCrossEdge1) * inverseDeterminant;
            return distance >= 0;
        }

        public static bool RayIntersectsTriangle(ref Ray ray, ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3, out Vector3 point)
        {
            float distance;
            if (RayIntersectsTriangle(ref ray, ref vertex1, ref vertex2, ref vertex3, out distance))
            {
                point = ray.Origin + ray.Direction * distance;
                return true;
            }

            point = default;
            return false;
        }

        public static void ClosestPointPointTriangle(ref Vector3 point, ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3, out Vector3 result)
        {
            var ab = vertex2 - vertex1;
            var ac = vertex3 - vertex1;
            var ap = point - vertex1;
            var d1 = Dot(ab, ap);
            var d2 = Dot(ac, ap);
            if (d1 <= 0 && d2 <= 0)
            {
                result = vertex1;
                return;
            }

            var bp = point - vertex2;
            var d3 = Dot(ab, bp);
            var d4 = Dot(ac, bp);
            if (d3 >= 0 && d4 <= d3)
            {
                result = vertex2;
                return;
            }

            var vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0)
            {
                result = vertex1 + ab * (d1 / (d1 - d3));
                return;
            }

            var cp = point - vertex3;
            var d5 = Dot(ab, cp);
            var d6 = Dot(ac, cp);
            if (d6 >= 0 && d5 <= d6)
            {
                result = vertex3;
                return;
            }

            var vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0)
            {
                result = vertex1 + ac * (d2 / (d2 - d6));
                return;
            }

            var va = d3 * d6 - d5 * d4;
            if (va <= 0 && d4 - d3 >= 0 && d5 - d6 >= 0)
            {
                result = vertex2 + (vertex3 - vertex2) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)));
                return;
            }

            var denominator = 1.0f / (va + vb + vc);
            result = vertex1 + ab * (vb * denominator) + ac * (vc * denominator);
        }
    }

    public struct Color
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public Color(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Color(float r, float g, float b, float a = 1f)
            : this(ToByte(r), ToByte(g), ToByte(b), ToByte(a))
        {
        }

        public static readonly Color Transparent = new Color(0, 0, 0, 0);
        public static readonly Color White = new Color(255, 255, 255, 255);
        public static readonly Color Black = new Color(0, 0, 0, 255);
        public static readonly Color Red = new Color(255, 0, 0, 255);
        public static readonly Color Green = new Color(0, 128, 0, 255);
        public static readonly Color Blue = new Color(0, 0, 255, 255);
        public static readonly Color Gold = new Color(255, 215, 0, 255);
        public static readonly Color Gray = new Color(128, 128, 128, 255);
        public static readonly Color DarkGray = new Color(169, 169, 169, 255);
        public static readonly Color Yellow = new Color(255, 255, 0, 255);
        public static readonly Color Silver = new Color(192, 192, 192, 255);
        public static readonly Color LightBlue = new Color(173, 216, 230, 255);
        public static readonly Color SkyBlue = new Color(135, 206, 235, 255);
        public static readonly Color LightGreen = new Color(144, 238, 144, 255);
        public static readonly Color BlanchedAlmond = new Color(255, 235, 205, 255);
        public static readonly Color Bisque = new Color(255, 228, 196, 255);
        public static readonly Color Zero = Transparent;

        public static Color FromRgb(byte red, byte green, byte blue)
        {
            return new Color(red, green, blue);
        }

        public static Color FromArgb(byte alpha, byte red, byte green, byte blue)
        {
            return new Color(red, green, blue, alpha);
        }

        private static byte ToByte(float value)
        {
            if (value <= 0)
            {
                return 0;
            }
            if (value >= 1)
            {
                return 255;
            }
            return (byte)(value * 255f);
        }

        public Color4 ToColor4()
        {
            return new Color4(R / 255f, G / 255f, B / 255f, A / 255f);
        }

        public static implicit operator Color4(Color color)
        {
            return color.ToColor4();
        }

        public static explicit operator Color(Color4 color)
        {
            return new Color(color.X, color.Y, color.Z, color.W);
        }
    }

    public struct BoundingBox
    {
        public Vector3 Minimum;
        public Vector3 Maximum;
        public Vector3 Size => Maximum - Minimum;

        public BoundingBox(Vector3 minimum, Vector3 maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public static bool operator ==(BoundingBox left, BoundingBox right)
        {
            return left.Minimum == right.Minimum && left.Maximum == right.Maximum;
        }

        public static bool operator !=(BoundingBox left, BoundingBox right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is BoundingBox other && this == other;
        }

        public override int GetHashCode()
        {
            return global::System.HashCode.Combine(Minimum, Maximum);
        }

        private static Vector3 Min(Vector3 left, Vector3 right)
        {
            return new Vector3(global::System.Math.Min(left.X, right.X), global::System.Math.Min(left.Y, right.Y), global::System.Math.Min(left.Z, right.Z));
        }

        private static Vector3 Max(Vector3 left, Vector3 right)
        {
            return new Vector3(global::System.Math.Max(left.X, right.X), global::System.Math.Max(left.Y, right.Y), global::System.Math.Max(left.Z, right.Z));
        }

        public static BoundingBox Merge(BoundingBox value1, BoundingBox value2)
        {
            Merge(ref value1, ref value2, out var result);
            return result;
        }

        public static void Merge(ref BoundingBox value1, ref BoundingBox value2, out BoundingBox result)
        {
            result = new BoundingBox(Min(value1.Minimum, value2.Minimum), Max(value1.Maximum, value2.Maximum));
        }

        public static BoundingBox FromSphere(BoundingSphere sphere)
        {
            var radius = new Vector3(sphere.Radius);
            return new BoundingBox(sphere.Center - radius, sphere.Center + radius);
        }

        public Vector3[] GetCorners()
        {
            return new[]
            {
                new Vector3(Minimum.X, Maximum.Y, Maximum.Z),
                new Vector3(Maximum.X, Maximum.Y, Maximum.Z),
                new Vector3(Maximum.X, Minimum.Y, Maximum.Z),
                new Vector3(Minimum.X, Minimum.Y, Maximum.Z),
                new Vector3(Minimum.X, Maximum.Y, Minimum.Z),
                new Vector3(Maximum.X, Maximum.Y, Minimum.Z),
                new Vector3(Maximum.X, Minimum.Y, Minimum.Z),
                new Vector3(Minimum.X, Minimum.Y, Minimum.Z)
            };
        }

        public ContainmentType Contains(Vector3 point)
        {
            return point.X < Minimum.X || point.X > Maximum.X
                || point.Y < Minimum.Y || point.Y > Maximum.Y
                || point.Z < Minimum.Z || point.Z > Maximum.Z
                ? ContainmentType.Disjoint
                : ContainmentType.Contains;
        }

        public ContainmentType Contains(ref BoundingBox box)
        {
            if (Maximum.X < box.Minimum.X || Minimum.X > box.Maximum.X
                || Maximum.Y < box.Minimum.Y || Minimum.Y > box.Maximum.Y
                || Maximum.Z < box.Minimum.Z || Minimum.Z > box.Maximum.Z)
            {
                return ContainmentType.Disjoint;
            }

            return Minimum.X <= box.Minimum.X && Maximum.X >= box.Maximum.X
                && Minimum.Y <= box.Minimum.Y && Maximum.Y >= box.Maximum.Y
                && Minimum.Z <= box.Minimum.Z && Maximum.Z >= box.Maximum.Z
                ? ContainmentType.Contains
                : ContainmentType.Intersects;
        }

        public ContainmentType Contains(ref BoundingSphere sphere)
        {
            var radius = new Vector3(sphere.Radius);
            var sphereBox = new BoundingBox(sphere.Center - radius, sphere.Center + radius);
            return Contains(ref sphereBox);
        }

        public bool Intersects(ref Ray ray)
        {
            var tmin = (Minimum.X - ray.Origin.X) / ray.Direction.X;
            var tmax = (Maximum.X - ray.Origin.X) / ray.Direction.X;
            if (tmin > tmax)
            {
                (tmin, tmax) = (tmax, tmin);
            }

            var tymin = (Minimum.Y - ray.Origin.Y) / ray.Direction.Y;
            var tymax = (Maximum.Y - ray.Origin.Y) / ray.Direction.Y;
            if (tymin > tymax)
            {
                (tymin, tymax) = (tymax, tymin);
            }
            if (tmin > tymax || tymin > tmax)
            {
                return false;
            }

            tmin = global::System.Math.Max(tmin, tymin);
            tmax = global::System.Math.Min(tmax, tymax);

            var tzmin = (Minimum.Z - ray.Origin.Z) / ray.Direction.Z;
            var tzmax = (Maximum.Z - ray.Origin.Z) / ray.Direction.Z;
            if (tzmin > tzmax)
            {
                (tzmin, tzmax) = (tzmax, tzmin);
            }

            return !(tmin > tzmax || tzmin > tmax);
        }
    }

    public sealed class DoubleKeyDictionary<K, T, V> : IEnumerable<KeyValuePair<(K, T), V>>
    {
        private readonly Dictionary<(K, T), V> dictionary = new Dictionary<(K, T), V>();

        public IEnumerable<V> Values => dictionary.Values;

        public void Add(K key1, T key2, V value)
        {
            dictionary.Add((key1, key2), value);
        }

        public bool Remove(K key1, T key2)
        {
            return dictionary.Remove((key1, key2));
        }

        public bool TryGetValue(K key1, T key2, out V value)
        {
            return dictionary.TryGetValue((key1, key2), out value);
        }

        public void Clear()
        {
            dictionary.Clear();
        }

        public IEnumerator<KeyValuePair<(K, T), V>> GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
#endif

namespace HelixToolkit.SharpDX.Core {
    public enum PlaneIntersectionType {
        Front,
        Back,
        Intersecting
    }

    public enum ContainmentType {
        Disjoint,
        Contains,
        Intersects
    }

    public struct Ray {
        public Vector3 Position;
        public Vector3 Direction;

        public Ray(Vector3 position, Vector3 direction) {
            Position = position;
            Direction = direction;
        }

        public Vector3 Origin {
            get => Position;
            set => Position = value;
        }

        public Vector3 GetPoint(float distance) {
            return Position + Direction * distance;
        }
    }

    public struct Plane {
        public Vector3 Normal;
        public float D;

        public Plane(Vector3 normal, float d) {
            Normal = normal;
            D = d;
        }

        public Plane(Vector3 point, Vector3 normal) {
            Normal = Collision.Normalize(normal);
            D = -Collision.Dot(Normal, point);
        }

        public bool Intersects(ref Ray ray, out float distance) {
            return Collision.RayIntersectsPlane(ref ray, ref this, out distance);
        }

        public PlaneIntersectionType Intersects(ref BoundingSphere sphere) {
            var distance = Collision.Dot(Normal, sphere.Center) + D;
            if (distance > sphere.Radius) return PlaneIntersectionType.Front;
            if (distance < -sphere.Radius) return PlaneIntersectionType.Back;
            return PlaneIntersectionType.Intersecting;
        }
    }

    public static class Collision {
        private const float Epsilon = 1e-6f;

        public static float Dot(Vector3 left, Vector3 right) {
            return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
        }

        public static Vector3 Cross(Vector3 left, Vector3 right) {
            return new Vector3(left.Y * right.Z - left.Z * right.Y,
                               left.Z * right.X - left.X * right.Z,
                               left.X * right.Y - left.Y * right.X);
        }

        public static Vector3 Normalize(Vector3 vector) {
            var length = vector.Length;
            return length > 0 ? vector / length : vector;
        }

        public static PlaneIntersectionType PlaneIntersectsPoint(ref Plane plane, ref Vector3 point) {
            var distance = Dot(plane.Normal, point) + plane.D;
            return distance > 0 ? PlaneIntersectionType.Front :
                   distance < 0 ? PlaneIntersectionType.Back : PlaneIntersectionType.Intersecting;
        }

        public static bool RayIntersectsPlane(ref Ray ray, ref Plane plane, out float distance) {
            var denominator = Dot(plane.Normal, ray.Direction);
            if (Math.Abs(denominator) < Epsilon) {
                distance = 0;
                return false;
            }

            distance = -(Dot(plane.Normal, ray.Origin) + plane.D) / denominator;
            return true;
        }

        public static bool RayIntersectsPlane(ref Ray ray, ref Plane plane, out Vector3 point) {
            float distance;
            if (RayIntersectsPlane(ref ray, ref plane, out distance)) {
                point = ray.Origin + ray.Direction * distance;
                return true;
            }

            point = default;
            return false;
        }

        public static bool RayIntersectsTriangle(
            ref Ray ray,
            ref Vector3 vertex1,
            ref Vector3 vertex2,
            ref Vector3 vertex3,
            out float distance
        ) {
            var edge1 = vertex2 - vertex1;
            var edge2 = vertex3 - vertex1;
            var directionCrossEdge2 = Cross(ray.Direction, edge2);
            var determinant = Dot(edge1, directionCrossEdge2);

            if (Math.Abs(determinant) < Epsilon) {
                distance = 0;
                return false;
            }

            var inverseDeterminant = 1.0f / determinant;
            var distanceVector = ray.Origin - vertex1;
            var triangleU = Dot(distanceVector, directionCrossEdge2) * inverseDeterminant;
            if (triangleU < 0 || triangleU > 1) {
                distance = 0;
                return false;
            }

            var distanceCrossEdge1 = Cross(distanceVector, edge1);
            var triangleV = Dot(ray.Direction, distanceCrossEdge1) * inverseDeterminant;
            if (triangleV < 0 || triangleU + triangleV > 1) {
                distance = 0;
                return false;
            }

            distance = Dot(edge2, distanceCrossEdge1) * inverseDeterminant;
            return distance >= 0;
        }

        public static bool RayIntersectsTriangle(
            ref Ray ray,
            ref Vector3 vertex1,
            ref Vector3 vertex2,
            ref Vector3 vertex3,
            out Vector3 point
        ) {
            float distance;
            if (RayIntersectsTriangle(ref ray, ref vertex1, ref vertex2, ref vertex3, out distance)) {
                point = ray.Origin + ray.Direction * distance;
                return true;
            }

            point = default;
            return false;
        }

        public static void ClosestPointPointTriangle(
            ref Vector3 point,
            ref Vector3 vertex1,
            ref Vector3 vertex2,
            ref Vector3 vertex3,
            out Vector3 result
        ) {
            var ab = vertex2 - vertex1;
            var ac = vertex3 - vertex1;
            var ap = point - vertex1;
            var d1 = Dot(ab, ap);
            var d2 = Dot(ac, ap);
            if (d1 <= 0 && d2 <= 0) {
                result = vertex1;
                return;
            }

            var bp = point - vertex2;
            var d3 = Dot(ab, bp);
            var d4 = Dot(ac, bp);
            if (d3 >= 0 && d4 <= d3) {
                result = vertex2;
                return;
            }

            var vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0) {
                result = vertex1 + ab * (d1 / (d1 - d3));
                return;
            }

            var cp = point - vertex3;
            var d5 = Dot(ab, cp);
            var d6 = Dot(ac, cp);
            if (d6 >= 0 && d5 <= d6) {
                result = vertex3;
                return;
            }

            var vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0) {
                result = vertex1 + ac * (d2 / (d2 - d6));
                return;
            }

            var va = d3 * d6 - d5 * d4;
            if (va <= 0 && d4 - d3 >= 0 && d5 - d6 >= 0) {
                result = vertex2 + (vertex3 - vertex2) * ((d4 - d3) / (d4 - d3 + (d5 - d6)));
                return;
            }

            var denominator = 1.0f / (va + vb + vc);
            result = vertex1 + ab * (vb * denominator) + ac * (vc * denominator);
        }
    }

    public struct Color {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public Color(byte r, byte g, byte b, byte a = 255) {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Color(float r, float g, float b, float a = 1f)
            : this(ToByte(r), ToByte(g), ToByte(b), ToByte(a)) { }

        public static readonly Color Transparent = new(0, 0, 0, 0);
        public static readonly Color White = new(255, 255, 255);
        public static readonly Color Black = new(0, 0, 0);
        public static readonly Color Red = new(255, 0, 0);
        public static readonly Color Green = new(0, 128, 0);
        public static readonly Color Blue = new(0, 0, 255);
        public static readonly Color Gold = new(255, 215, 0);
        public static readonly Color Gray = new(128, 128, 128);
        public static readonly Color DarkGray = new(169, 169, 169);
        public static readonly Color Yellow = new(255, 255, 0);
        public static readonly Color Silver = new(192, 192, 192);
        public static readonly Color LightBlue = new(173, 216, 230);
        public static readonly Color SkyBlue = new(135, 206, 235);
        public static readonly Color LightGreen = new(144, 238, 144);
        public static readonly Color BlanchedAlmond = new(255, 235, 205);
        public static readonly Color Bisque = new(255, 228, 196);
        public static readonly Color Zero = Transparent;

        public static Color FromRgb(byte red, byte green, byte blue) {
            return new Color(red, green, blue);
        }

        public static Color FromArgb(byte alpha, byte red, byte green, byte blue) {
            return new Color(red, green, blue, alpha);
        }

        private static byte ToByte(float value) {
            if (value <= 0) return 0;
            if (value >= 1) return 255;
            return (byte) (value * 255f);
        }

        public Color4 ToColor4() {
            return new Color4(R / 255f, G / 255f, B / 255f, A / 255f);
        }

        public static implicit operator Color4(Color color) {
            return color.ToColor4();
        }

        public static explicit operator Color(Color4 color) {
            return new Color(color.X, color.Y, color.Z, color.W);
        }
    }

    public struct BoundingBox {
        public Vector3 Minimum;
        public Vector3 Maximum;
        public Vector3 Size => Maximum - Minimum;

        public BoundingBox(Vector3 minimum, Vector3 maximum) {
            Minimum = minimum;
            Maximum = maximum;
        }

        public static bool operator ==(BoundingBox left, BoundingBox right) {
            return left.Minimum == right.Minimum && left.Maximum == right.Maximum;
        }

        public static bool operator !=(BoundingBox left, BoundingBox right) {
            return !(left == right);
        }

        public override bool Equals(object obj) {
            return obj is BoundingBox other && this == other;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Minimum, Maximum);
        }

        private static Vector3 Min(Vector3 left, Vector3 right) {
            return new Vector3(Math.Min(left.X, right.X), Math.Min(left.Y, right.Y), Math.Min(left.Z, right.Z));
        }

        private static Vector3 Max(Vector3 left, Vector3 right) {
            return new Vector3(Math.Max(left.X, right.X), Math.Max(left.Y, right.Y), Math.Max(left.Z, right.Z));
        }

        public static BoundingBox Merge(BoundingBox value1, BoundingBox value2) {
            Merge(ref value1, ref value2, out var result);
            return result;
        }

        public static void Merge(ref BoundingBox value1, ref BoundingBox value2, out BoundingBox result) {
            result = new BoundingBox(Min(value1.Minimum, value2.Minimum), Max(value1.Maximum, value2.Maximum));
        }

        public static BoundingBox FromSphere(BoundingSphere sphere) {
            var radius = new Vector3(sphere.Radius);
            return new BoundingBox(sphere.Center - radius, sphere.Center + radius);
        }

        public Vector3[] GetCorners() {
            return new[] {
                new Vector3(Minimum.X, Maximum.Y, Maximum.Z),
                new Vector3(Maximum.X, Maximum.Y, Maximum.Z),
                new Vector3(Maximum.X, Minimum.Y, Maximum.Z),
                new Vector3(Minimum.X, Minimum.Y, Maximum.Z),
                new Vector3(Minimum.X, Maximum.Y, Minimum.Z),
                new Vector3(Maximum.X, Maximum.Y, Minimum.Z),
                new Vector3(Maximum.X, Minimum.Y, Minimum.Z),
                new Vector3(Minimum.X, Minimum.Y, Minimum.Z)
            };
        }

        public ContainmentType Contains(Vector3 point) {
            return point.X < Minimum.X || point.X > Maximum.X
                                       || point.Y < Minimum.Y || point.Y > Maximum.Y
                                       || point.Z < Minimum.Z || point.Z > Maximum.Z
                       ? ContainmentType.Disjoint
                       : ContainmentType.Contains;
        }

        public ContainmentType Contains(ref BoundingBox box) {
            if (Maximum.X < box.Minimum.X || Minimum.X > box.Maximum.X
                                          || Maximum.Y < box.Minimum.Y || Minimum.Y > box.Maximum.Y
                                          || Maximum.Z < box.Minimum.Z || Minimum.Z > box.Maximum.Z)
                return ContainmentType.Disjoint;

            return Minimum.X <= box.Minimum.X && Maximum.X >= box.Maximum.X
                                              && Minimum.Y <= box.Minimum.Y && Maximum.Y >= box.Maximum.Y
                                              && Minimum.Z <= box.Minimum.Z && Maximum.Z >= box.Maximum.Z
                       ? ContainmentType.Contains
                       : ContainmentType.Intersects;
        }

        public ContainmentType Contains(ref BoundingSphere sphere) {
            var radius = new Vector3(sphere.Radius);
            var sphereBox = new BoundingBox(sphere.Center - radius, sphere.Center + radius);
            return Contains(ref sphereBox);
        }

        public bool Intersects(ref Ray ray) {
            var tmin = (Minimum.X - ray.Origin.X) / ray.Direction.X;
            var tmax = (Maximum.X - ray.Origin.X) / ray.Direction.X;
            if (tmin > tmax) (tmin, tmax) = (tmax, tmin);

            var tymin = (Minimum.Y - ray.Origin.Y) / ray.Direction.Y;
            var tymax = (Maximum.Y - ray.Origin.Y) / ray.Direction.Y;
            if (tymin > tymax) (tymin, tymax) = (tymax, tymin);
            if (tmin > tymax || tymin > tmax) return false;

            tmin = Math.Max(tmin, tymin);
            tmax = Math.Min(tmax, tymax);

            var tzmin = (Minimum.Z - ray.Origin.Z) / ray.Direction.Z;
            var tzmax = (Maximum.Z - ray.Origin.Z) / ray.Direction.Z;
            if (tzmin > tzmax) (tzmin, tzmax) = (tzmax, tzmin);

            return !(tmin > tzmax || tzmin > tmax);
        }
    }

    public sealed class DoubleKeyDictionary<K, T, V> : IEnumerable<KeyValuePair<(K, T), V>> {
        private readonly Dictionary<(K, T), V> dictionary = new();

        public IEnumerable<V> Values => dictionary.Values;

        public IEnumerator<KeyValuePair<(K, T), V>> GetEnumerator() {
            return dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public void Add(K key1, T key2, V value) {
            dictionary.Add((key1, key2), value);
        }

        public bool Remove(K key1, T key2) {
            return dictionary.Remove((key1, key2));
        }

        public bool TryGetValue(K key1, T key2, out V value) {
            return dictionary.TryGetValue((key1, key2), out value);
        }

        public void Clear() {
            dictionary.Clear();
        }
    }
}

namespace HelixToolkit.UWP {
    public enum PlaneIntersectionType {
        Front,
        Back,
        Intersecting
    }

    public enum ContainmentType {
        Disjoint,
        Contains,
        Intersects
    }

    public struct Ray {
        public Vector3 Position;
        public Vector3 Direction;

        public Ray(Vector3 position, Vector3 direction) {
            Position = position;
            Direction = direction;
        }

        public Vector3 Origin {
            get => Position;
            set => Position = value;
        }

        public Vector3 GetPoint(float distance) {
            return Position + Direction * distance;
        }
    }

    public struct Plane {
        public Vector3 Normal;
        public float D;

        public Plane(Vector3 normal, float d) {
            Normal = normal;
            D = d;
        }

        public Plane(Vector3 point, Vector3 normal) {
            Normal = Collision.Normalize(normal);
            D = -Collision.Dot(Normal, point);
        }

        public bool Intersects(ref Ray ray, out float distance) {
            return Collision.RayIntersectsPlane(ref ray, ref this, out distance);
        }

        public PlaneIntersectionType Intersects(ref BoundingSphere sphere) {
            var distance = Collision.Dot(Normal, sphere.Center) + D;
            if (distance > sphere.Radius) return PlaneIntersectionType.Front;
            if (distance < -sphere.Radius) return PlaneIntersectionType.Back;
            return PlaneIntersectionType.Intersecting;
        }
    }

    public static class Collision {
        private const float Epsilon = 1e-6f;

        public static float Dot(Vector3 left, Vector3 right) {
            return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
        }

        public static Vector3 Cross(Vector3 left, Vector3 right) {
            return new Vector3(left.Y * right.Z - left.Z * right.Y,
                               left.Z * right.X - left.X * right.Z,
                               left.X * right.Y - left.Y * right.X);
        }

        public static Vector3 Normalize(Vector3 vector) {
            var length = vector.Length;
            return length > 0 ? vector / length : vector;
        }

        public static PlaneIntersectionType PlaneIntersectsPoint(ref Plane plane, ref Vector3 point) {
            var distance = Dot(plane.Normal, point) + plane.D;
            return distance > 0 ? PlaneIntersectionType.Front :
                   distance < 0 ? PlaneIntersectionType.Back : PlaneIntersectionType.Intersecting;
        }

        public static bool RayIntersectsPlane(ref Ray ray, ref Plane plane, out float distance) {
            var denominator = Dot(plane.Normal, ray.Direction);
            if (Math.Abs(denominator) < Epsilon) {
                distance = 0;
                return false;
            }

            distance = -(Dot(plane.Normal, ray.Origin) + plane.D) / denominator;
            return true;
        }

        public static bool RayIntersectsPlane(ref Ray ray, ref Plane plane, out Vector3 point) {
            float distance;
            if (RayIntersectsPlane(ref ray, ref plane, out distance)) {
                point = ray.Origin + ray.Direction * distance;
                return true;
            }

            point = default;
            return false;
        }

        public static bool RayIntersectsTriangle(
            ref Ray ray,
            ref Vector3 vertex1,
            ref Vector3 vertex2,
            ref Vector3 vertex3,
            out float distance
        ) {
            var edge1 = vertex2 - vertex1;
            var edge2 = vertex3 - vertex1;
            var directionCrossEdge2 = Cross(ray.Direction, edge2);
            var determinant = Dot(edge1, directionCrossEdge2);

            if (Math.Abs(determinant) < Epsilon) {
                distance = 0;
                return false;
            }

            var inverseDeterminant = 1.0f / determinant;
            var distanceVector = ray.Origin - vertex1;
            var triangleU = Dot(distanceVector, directionCrossEdge2) * inverseDeterminant;
            if (triangleU < 0 || triangleU > 1) {
                distance = 0;
                return false;
            }

            var distanceCrossEdge1 = Cross(distanceVector, edge1);
            var triangleV = Dot(ray.Direction, distanceCrossEdge1) * inverseDeterminant;
            if (triangleV < 0 || triangleU + triangleV > 1) {
                distance = 0;
                return false;
            }

            distance = Dot(edge2, distanceCrossEdge1) * inverseDeterminant;
            return distance >= 0;
        }

        public static bool RayIntersectsTriangle(
            ref Ray ray,
            ref Vector3 vertex1,
            ref Vector3 vertex2,
            ref Vector3 vertex3,
            out Vector3 point
        ) {
            float distance;
            if (RayIntersectsTriangle(ref ray, ref vertex1, ref vertex2, ref vertex3, out distance)) {
                point = ray.Origin + ray.Direction * distance;
                return true;
            }

            point = default;
            return false;
        }

        public static void ClosestPointPointTriangle(
            ref Vector3 point,
            ref Vector3 vertex1,
            ref Vector3 vertex2,
            ref Vector3 vertex3,
            out Vector3 result
        ) {
            var ab = vertex2 - vertex1;
            var ac = vertex3 - vertex1;
            var ap = point - vertex1;
            var d1 = Dot(ab, ap);
            var d2 = Dot(ac, ap);
            if (d1 <= 0 && d2 <= 0) {
                result = vertex1;
                return;
            }

            var bp = point - vertex2;
            var d3 = Dot(ab, bp);
            var d4 = Dot(ac, bp);
            if (d3 >= 0 && d4 <= d3) {
                result = vertex2;
                return;
            }

            var vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0) {
                result = vertex1 + ab * (d1 / (d1 - d3));
                return;
            }

            var cp = point - vertex3;
            var d5 = Dot(ab, cp);
            var d6 = Dot(ac, cp);
            if (d6 >= 0 && d5 <= d6) {
                result = vertex3;
                return;
            }

            var vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0) {
                result = vertex1 + ac * (d2 / (d2 - d6));
                return;
            }

            var va = d3 * d6 - d5 * d4;
            if (va <= 0 && d4 - d3 >= 0 && d5 - d6 >= 0) {
                result = vertex2 + (vertex3 - vertex2) * ((d4 - d3) / (d4 - d3 + (d5 - d6)));
                return;
            }

            var denominator = 1.0f / (va + vb + vc);
            result = vertex1 + ab * (vb * denominator) + ac * (vc * denominator);
        }
    }

    public struct Color {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public Color(byte r, byte g, byte b, byte a = 255) {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Color(float r, float g, float b, float a = 1f)
            : this(ToByte(r), ToByte(g), ToByte(b), ToByte(a)) { }

        public static readonly Color Transparent = new(0, 0, 0, 0);
        public static readonly Color White = new(255, 255, 255);
        public static readonly Color Black = new(0, 0, 0);
        public static readonly Color Red = new(255, 0, 0);
        public static readonly Color Green = new(0, 128, 0);
        public static readonly Color Blue = new(0, 0, 255);
        public static readonly Color Gold = new(255, 215, 0);
        public static readonly Color Gray = new(128, 128, 128);
        public static readonly Color DarkGray = new(169, 169, 169);
        public static readonly Color Yellow = new(255, 255, 0);
        public static readonly Color Silver = new(192, 192, 192);
        public static readonly Color LightBlue = new(173, 216, 230);
        public static readonly Color SkyBlue = new(135, 206, 235);
        public static readonly Color LightGreen = new(144, 238, 144);
        public static readonly Color BlanchedAlmond = new(255, 235, 205);
        public static readonly Color Bisque = new(255, 228, 196);
        public static readonly Color Zero = Transparent;

        public static Color FromRgb(byte red, byte green, byte blue) {
            return new Color(red, green, blue);
        }

        public static Color FromArgb(byte alpha, byte red, byte green, byte blue) {
            return new Color(red, green, blue, alpha);
        }

        private static byte ToByte(float value) {
            if (value <= 0) return 0;
            if (value >= 1) return 255;
            return (byte) (value * 255f);
        }

        public Color4 ToColor4() {
            return new Color4(R / 255f, G / 255f, B / 255f, A / 255f);
        }

        public static implicit operator Color4(Color color) {
            return color.ToColor4();
        }

        public static explicit operator Color(Color4 color) {
            return new Color(color.X, color.Y, color.Z, color.W);
        }
    }

    public struct BoundingBox {
        public Vector3 Minimum;
        public Vector3 Maximum;
        public Vector3 Size => Maximum - Minimum;

        public BoundingBox(Vector3 minimum, Vector3 maximum) {
            Minimum = minimum;
            Maximum = maximum;
        }

        public static bool operator ==(BoundingBox left, BoundingBox right) {
            return left.Minimum == right.Minimum && left.Maximum == right.Maximum;
        }

        public static bool operator !=(BoundingBox left, BoundingBox right) {
            return !(left == right);
        }

        public override bool Equals(object obj) {
            return obj is BoundingBox other && this == other;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Minimum, Maximum);
        }

        private static Vector3 Min(Vector3 left, Vector3 right) {
            return new Vector3(Math.Min(left.X, right.X), Math.Min(left.Y, right.Y), Math.Min(left.Z, right.Z));
        }

        private static Vector3 Max(Vector3 left, Vector3 right) {
            return new Vector3(Math.Max(left.X, right.X), Math.Max(left.Y, right.Y), Math.Max(left.Z, right.Z));
        }

        public static BoundingBox Merge(BoundingBox value1, BoundingBox value2) {
            Merge(ref value1, ref value2, out var result);
            return result;
        }

        public static void Merge(ref BoundingBox value1, ref BoundingBox value2, out BoundingBox result) {
            result = new BoundingBox(Min(value1.Minimum, value2.Minimum), Max(value1.Maximum, value2.Maximum));
        }

        public static BoundingBox FromSphere(BoundingSphere sphere) {
            var radius = new Vector3(sphere.Radius);
            return new BoundingBox(sphere.Center - radius, sphere.Center + radius);
        }

        public Vector3[] GetCorners() {
            return new[] {
                new Vector3(Minimum.X, Maximum.Y, Maximum.Z),
                new Vector3(Maximum.X, Maximum.Y, Maximum.Z),
                new Vector3(Maximum.X, Minimum.Y, Maximum.Z),
                new Vector3(Minimum.X, Minimum.Y, Maximum.Z),
                new Vector3(Minimum.X, Maximum.Y, Minimum.Z),
                new Vector3(Maximum.X, Maximum.Y, Minimum.Z),
                new Vector3(Maximum.X, Minimum.Y, Minimum.Z),
                new Vector3(Minimum.X, Minimum.Y, Minimum.Z)
            };
        }

        public ContainmentType Contains(Vector3 point) {
            return point.X < Minimum.X || point.X > Maximum.X
                                       || point.Y < Minimum.Y || point.Y > Maximum.Y
                                       || point.Z < Minimum.Z || point.Z > Maximum.Z
                       ? ContainmentType.Disjoint
                       : ContainmentType.Contains;
        }

        public ContainmentType Contains(ref BoundingBox box) {
            if (Maximum.X < box.Minimum.X || Minimum.X > box.Maximum.X
                                          || Maximum.Y < box.Minimum.Y || Minimum.Y > box.Maximum.Y
                                          || Maximum.Z < box.Minimum.Z || Minimum.Z > box.Maximum.Z)
                return ContainmentType.Disjoint;

            return Minimum.X <= box.Minimum.X && Maximum.X >= box.Maximum.X
                                              && Minimum.Y <= box.Minimum.Y && Maximum.Y >= box.Maximum.Y
                                              && Minimum.Z <= box.Minimum.Z && Maximum.Z >= box.Maximum.Z
                       ? ContainmentType.Contains
                       : ContainmentType.Intersects;
        }

        public ContainmentType Contains(ref BoundingSphere sphere) {
            var radius = new Vector3(sphere.Radius);
            var sphereBox = new BoundingBox(sphere.Center - radius, sphere.Center + radius);
            return Contains(ref sphereBox);
        }

        public bool Intersects(ref Ray ray) {
            var tmin = (Minimum.X - ray.Origin.X) / ray.Direction.X;
            var tmax = (Maximum.X - ray.Origin.X) / ray.Direction.X;
            if (tmin > tmax) (tmin, tmax) = (tmax, tmin);

            var tymin = (Minimum.Y - ray.Origin.Y) / ray.Direction.Y;
            var tymax = (Maximum.Y - ray.Origin.Y) / ray.Direction.Y;
            if (tymin > tymax) (tymin, tymax) = (tymax, tymin);
            if (tmin > tymax || tymin > tmax) return false;

            tmin = Math.Max(tmin, tymin);
            tmax = Math.Min(tmax, tymax);

            var tzmin = (Minimum.Z - ray.Origin.Z) / ray.Direction.Z;
            var tzmax = (Maximum.Z - ray.Origin.Z) / ray.Direction.Z;
            if (tzmin > tzmax) (tzmin, tzmax) = (tzmax, tzmin);

            return !(tmin > tzmax || tzmin > tmax);
        }
    }

    public sealed class DoubleKeyDictionary<K, T, V> : IEnumerable<KeyValuePair<(K, T), V>> {
        private readonly Dictionary<(K, T), V> dictionary = new();

        public IEnumerable<V> Values => dictionary.Values;

        public IEnumerator<KeyValuePair<(K, T), V>> GetEnumerator() {
            return dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public void Add(K key1, T key2, V value) {
            dictionary.Add((key1, key2), value);
        }

        public bool Remove(K key1, T key2) {
            return dictionary.Remove((key1, key2));
        }

        public bool TryGetValue(K key1, T key2, out V value) {
            return dictionary.TryGetValue((key1, key2), out value);
        }

        public void Clear() {
            dictionary.Clear();
        }
    }
}

namespace HelixToolkit.SharpDX.Core {
    public struct BoundingFrustum {
        public BoundingFrustum(Matrix matrix) {
            Matrix = matrix;
            Left = NormalizePlane(new Plane(
                                      new Vector3(matrix.M14 + matrix.M11,
                                                  matrix.M24 + matrix.M21,
                                                  matrix.M34 + matrix.M31),
                                      matrix.M44 + matrix.M41));
            Right = NormalizePlane(new Plane(
                                       new Vector3(matrix.M14 - matrix.M11,
                                                   matrix.M24 - matrix.M21,
                                                   matrix.M34 - matrix.M31),
                                       matrix.M44 - matrix.M41));
            Top = NormalizePlane(new Plane(
                                     new Vector3(matrix.M14 - matrix.M12,
                                                 matrix.M24 - matrix.M22,
                                                 matrix.M34 - matrix.M32),
                                     matrix.M44 - matrix.M42));
            Bottom = NormalizePlane(new Plane(
                                        new Vector3(matrix.M14 + matrix.M12,
                                                    matrix.M24 + matrix.M22,
                                                    matrix.M34 + matrix.M32),
                                        matrix.M44 + matrix.M42));
            Near = NormalizePlane(new Plane(new Vector3(matrix.M13, matrix.M23, matrix.M33),
                                            matrix.M43));
            Far = NormalizePlane(new Plane(
                                     new Vector3(matrix.M14 - matrix.M13,
                                                 matrix.M24 - matrix.M23,
                                                 matrix.M34 - matrix.M33),
                                     matrix.M44 - matrix.M43));
        }

        public Matrix Matrix { get; }

        public Plane Left { get; }

        public Plane Right { get; }

        public Plane Top { get; }

        public Plane Bottom { get; }

        public Plane Near { get; }

        public Plane Far { get; }

        public bool IsOrthographic => Math.Abs(Matrix.M34) < 1e-6f;

        public Plane GetPlane(int index) {
            switch (index) {
                case 0:
                    return Left;
                case 1:
                    return Right;
                case 2:
                    return Top;
                case 3:
                    return Bottom;
                case 4:
                    return Near;
                case 5:
                    return Far;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        private static Plane NormalizePlane(Plane plane) {
            var length = plane.Normal.Length;
            if (length > 0) {
                plane.Normal /= length;
                plane.D /= length;
            }

            return plane;
        }
    }
}
#endif
