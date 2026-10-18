using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Helper;
using DoubleOrSingle = float;

// Original types retain their source namespaces; Silk-only types keep their existing namespaces.
namespace HelixToolkit.SharpDX.Core
{

/// <summary>
///     Generates normalized maps from the source two-dimensional gradient noise field.
/// </summary>
public static class MathHelper {
    /// <summary>
    ///     Generates a freshly reseeded noise map normalized using the completed field's range.
    /// </summary>
    /// <param name="width">The nonnegative width.</param>
    /// <param name="height">The nonnegative height.</param>
    /// <param name="octaves">The nonnegative octave count with finite single-precision sampling products.</param>
    /// <param name="result">The row-major finite map in [0, 1]; constant fields contain zeros.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     A dimension or octave count is negative, or the requested sampling products are non-finite.
    /// </exception>
    /// <exception cref="OverflowException">The pixel count exceeds <see cref="int.MaxValue"/>.</exception>
    /// <remarks>
    ///     Empty dimensions return an empty map. Zero octaves return zeros. Each map uses one immutable permutation,
    ///     starting at frequency 0.5 and amplitude 1, doubling frequency and halving amplitude per octave.
    /// </remarks>
    public static void GenerateNoiseMap(int width, int height, int octaves, out DoubleOrSingle[] result) {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);
        ArgumentOutOfRangeException.ThrowIfNegative(octaves);
        var count = checked(width * height);
        if (count > 0 && octaves > 0) {
            var lastFrequency = MathF.ScaleB(0.5f, octaves - 1);
            if (!float.IsFinite(lastFrequency) ||
                !float.IsFinite((Math.Max(width, height) - 1) * lastFrequency))
                throw new ArgumentOutOfRangeException(nameof(octaves), "Sampling products must remain finite");
        }

        var data = new DoubleOrSingle[count];
        var permutation = Noise2d.ReseedAndCapture();
        if (count == 0 || octaves == 0) {
            result = data;
            return;
        }

        var frequency = 0.5f;
        var amplitude = 1f;

        for (var octave = 0; octave < octaves; octave++) {
            Parallel.For(0,
                         count,
                         offset => {
                             var i = offset % width;
                             var j = offset / width;
                             var noise = Noise2d.Noise(i * frequency / width, j * frequency / height, permutation);
                             data[offset] += noise * amplitude;
                         });

            frequency *= 2;
            amplitude /= 2;
        }

        var min = DoubleOrSingle.MaxValue;
        var max = DoubleOrSingle.MinValue;
        foreach (var value in data) {
            min = Math.Min(min, value);
            max = Math.Max(max, value);
        }

        if (min == max)
            Array.Clear(data);
        else {
            for (var i = 0; i < data.Length; ++i)
                data[i] = (data[i] - min) / (max - min);
        }
        result = data;
    }
}

}
namespace HelixToolkit.SharpDX.Core
{

/// <summary>
///     implements improved Perlin noise in 2D.
///     Transcribed from http://www.siafoo.net/snippet/144?nolinenos#perlin2003
///     From StackOverflow: https://stackoverflow.com/questions/8659351/2d-perlin-noise
/// </summary>
public static class Noise2d {
    /// <summary>
    ///     Supplies table randomness, locked during permutation creation after static initialization.
    /// </summary>
    private static readonly Random Random = new();

    /// <summary>
    ///     Holds the atomically published permutation; published arrays are never modified.
    /// </summary>
    private static int[] _permutation;

    /// <summary>
    ///     Holds the 256 fixed unit gradients created during serialized static initialization.
    /// </summary>
    private static readonly Vector2[] Gradients;

    /// <summary>
    ///     Initializes the first permutation and immutable gradient table.
    /// </summary>
    static Noise2d() {
        CalculatePermutation(out _permutation);
        CalculateGradients(out Gradients);
    }

    /// <summary>
    ///     Creates and shuffles a private permutation while protecting the shared random generator.
    /// </summary>
    /// <param name="p">The newly allocated 256-entry permutation.</param>
    private static void CalculatePermutation(out int[] p) {
        p = [.. Enumerable.Range(0, 256)];

        lock (Random) {
            for (var i = 0; i < p.Length; i++) {
                var source = Random.Next(p.Length);
                (p[i], p[source]) = (p[source], p[i]);
            }
        }
    }

    /// <summary>
    ///     Publishes a new permutation for future samples without changing captured fields or gradients.
    /// </summary>
    public static void Reseed() {
        ReseedAndCapture();
    }

    /// <summary>
    ///     Publishes a fresh permutation and returns that exact immutable table for a complete map.
    /// </summary>
    /// <returns>The newly published permutation, which callers must never modify.</returns>
    internal static int[] ReseedAndCapture() {
        CalculatePermutation(out var permutation);
        Volatile.Write(ref _permutation, permutation);
        return permutation;
    }

    /// <summary>
    ///     Creates the fixed gradient table during serialized static initialization.
    /// </summary>
    /// <param name="grad">The 256 normalized nonzero random gradients.</param>
    private static void CalculateGradients(out Vector2[] grad) {
        grad = new Vector2[256];

        for (var i = 0; i < grad.Length; i++) {
            Vector2 gradient;

            do {
                gradient = new Vector2((float)(Random.NextDouble() * 2 - 1), (float)(Random.NextDouble() * 2 - 1));
            } while (SharedFunctions.LengthSquared(ref gradient) is >= 1 or 0);

            gradient.Normalize();

            grad[i] = gradient;
        }
    }

    /// <summary>
    ///     Evaluates the complement of the quintic fade polynomial at an absolute corner displacement.
    /// </summary>
    /// <param name="t">The corner displacement in [-1, 1].</param>
    /// <returns>The corner's quintic weight.</returns>
    private static DoubleOrSingle Drop(DoubleOrSingle t) {
        t = Math.Abs(t);
        return 1f - t * t * t * (t * (t * 6 - 15) + 10);
    }

    /// <summary>
    ///     Combines the two axis weights for a cell corner.
    /// </summary>
    /// <param name="u">The horizontal displacement.</param>
    /// <param name="v">The vertical displacement.</param>
    /// <returns>The product of both quintic weights.</returns>
    private static DoubleOrSingle Q(DoubleOrSingle u, DoubleOrSingle v) => Drop(u) * Drop(v);

    /// <summary>
    ///     Samples the periodic two-dimensional gradient field using one published permutation.
    /// </summary>
    /// <param name="x">The finite horizontal coordinate.</param>
    /// <param name="y">The finite vertical coordinate.</param>
    /// <returns>The gradient sum clamped to [-1, 1], with a period of 256 along each axis.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate is non-finite.</exception>
    public static DoubleOrSingle Noise(DoubleOrSingle x, DoubleOrSingle y)
        => Noise(x, y, Volatile.Read(ref _permutation));

    /// <summary>
    ///     Samples the source four-corner field with a fixed immutable permutation.
    /// </summary>
    /// <param name="x">The finite horizontal coordinate.</param>
    /// <param name="y">The finite vertical coordinate.</param>
    /// <param name="permutation">The captured 256-entry permutation, never modified by the caller.</param>
    /// <returns>The quintic-weighted gradient sum clamped to [-1, 1].</returns>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate is non-finite.</exception>
    internal static DoubleOrSingle Noise(DoubleOrSingle x, DoubleOrSingle y, int[] permutation) {
        if (!float.IsFinite(x))
            throw new ArgumentOutOfRangeException(nameof(x), "The coordinate must be finite");
        if (!float.IsFinite(y))
            throw new ArgumentOutOfRangeException(nameof(y), "The coordinate must be finite");

        // Reduce before integer conversion so every finite float has a safe periodic cell index.
        x %= permutation.Length;
        y %= permutation.Length;
        var cell = new Vector2((DoubleOrSingle)Math.Floor(x), (DoubleOrSingle)Math.Floor(y));

        DoubleOrSingle total = 0;

        var corners = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1) };

        foreach (var n in corners) {
            var ij = cell + n;
            var uv = new Vector2(x - ij.X, y - ij.Y);

            var index = permutation[(int)ij.X & 255];
            index = permutation[(index + (int)ij.Y) & 255];

            var grad = Gradients[index % Gradients.Length];

            total += Q(uv.X, uv.Y) * SharedFunctions.DotProduct(ref grad, ref uv);
        }

        return Math.Max(Math.Min(total, 1f), -1f);
    }
}

}
