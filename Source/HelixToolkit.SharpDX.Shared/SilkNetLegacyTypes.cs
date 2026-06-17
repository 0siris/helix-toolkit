#if SILKNET
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
    }

    public struct BoundingBox
    {
        public Vector3 Minimum;
        public Vector3 Maximum;

        public BoundingBox(Vector3 minimum, Vector3 maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
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
            var tmin = (Minimum.X - ray.Position.X) / ray.Direction.X;
            var tmax = (Maximum.X - ray.Position.X) / ray.Direction.X;
            if (tmin > tmax)
            {
                (tmin, tmax) = (tmax, tmin);
            }

            var tymin = (Minimum.Y - ray.Position.Y) / ray.Direction.Y;
            var tymax = (Maximum.Y - ray.Position.Y) / ray.Direction.Y;
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

            var tzmin = (Minimum.Z - ray.Position.Z) / ray.Direction.Z;
            var tzmax = (Maximum.Z - ray.Position.Z) / ray.Direction.Z;
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

namespace HelixToolkit.SharpDX.Core
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
    }

    public struct BoundingBox
    {
        public Vector3 Minimum;
        public Vector3 Maximum;

        public BoundingBox(Vector3 minimum, Vector3 maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
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
            var tmin = (Minimum.X - ray.Position.X) / ray.Direction.X;
            var tmax = (Maximum.X - ray.Position.X) / ray.Direction.X;
            if (tmin > tmax)
            {
                (tmin, tmax) = (tmax, tmin);
            }

            var tymin = (Minimum.Y - ray.Position.Y) / ray.Direction.Y;
            var tymax = (Maximum.Y - ray.Position.Y) / ray.Direction.Y;
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

            var tzmin = (Minimum.Z - ray.Position.Z) / ray.Direction.Z;
            var tzmax = (Maximum.Z - ray.Position.Z) / ray.Direction.Z;
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

namespace HelixToolkit.UWP
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
    }

    public struct BoundingBox
    {
        public Vector3 Minimum;
        public Vector3 Maximum;

        public BoundingBox(Vector3 minimum, Vector3 maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
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
            var tmin = (Minimum.X - ray.Position.X) / ray.Direction.X;
            var tmax = (Maximum.X - ray.Position.X) / ray.Direction.X;
            if (tmin > tmax)
            {
                (tmin, tmax) = (tmax, tmin);
            }

            var tymin = (Minimum.Y - ray.Position.Y) / ray.Direction.Y;
            var tymax = (Maximum.Y - ray.Position.Y) / ray.Direction.Y;
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

            var tzmin = (Minimum.Z - ray.Position.Z) / ray.Direction.Z;
            var tzmax = (Maximum.Z - ray.Position.Z) / ray.Direction.Z;
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
