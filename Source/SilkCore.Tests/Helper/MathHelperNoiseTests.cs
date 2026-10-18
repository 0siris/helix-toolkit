using HelixToolkit.SharpDX.Core;
using System.Reflection;
using Silk.NET.Maths;

namespace HelixToolkit.SharpDX.Core.Helper;

/// <summary>
///     Isolates tests that reseed the process-wide noise permutation.
/// </summary>
[CollectionDefinition("Noise permutation", DisableParallelization = true)]
public sealed class NoisePermutationCollection;

/// <summary>
///     Verifies the source 2D gradient field and normalization without graphics or native windows.
/// </summary>
[Collection("Noise permutation")]
[Trait("Category", "Unit")]
public sealed class MathHelperNoiseTests {
    /// <summary>
    ///     Checks signed cells against an independently evaluated quintic gradient sum.
    /// </summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="y">The vertical coordinate.</param>
    [Theory]
    [InlineData(-1f, 0f)]
    [InlineData(-1.25f, -2.375f)]
    [InlineData(-0.125f, 0.375f)]
    [InlineData(0.125f, -0.375f)]
    [InlineData(0.125f, 0.375f)]
    [InlineData(12.25f, 7.375f)]
    public void NoiseGradientOracleTest(float x, float y) {
        Noise2d.Reseed();
        var permutation = ReadTable<int>("_permutation");
        var gradients = ReadTable<Vector2D<float>>("Gradients");
        var expected = EvaluateGradientSum(x, y, permutation, gradients);

        var actual = Noise2d.Noise(x, y);

        Assert.InRange(Math.Abs(actual - expected), 0, 0.000002);
        Assert.InRange(actual, -1f, 1f);
    }

    /// <summary>
    ///     Checks the lattice zeros, signed period and continuity across cell boundaries.
    /// </summary>
    [Fact]
    public void NoisePeriodAndContinuityTest() {
        for (var cell = -4; cell <= 4; cell++) {
            var atLattice = Noise2d.Noise(cell, -cell);
            var left = Noise2d.Noise(cell - 0.0001f, 0.375f);
            var right = Noise2d.Noise(cell + 0.0001f, 0.375f);
            var sample = Noise2d.Noise(cell + 0.125f, -0.375f);
            var horizontalPeriod = Noise2d.Noise(cell + 256.125f, -0.375f);
            var verticalPeriod = Noise2d.Noise(cell + 0.125f, 255.625f);

            Assert.Equal(0f, atLattice);
            Assert.InRange(Math.Abs(left - right), 0f, 0.001f);
            Assert.Equal(sample, horizontalPeriod);
            Assert.Equal(sample, verticalPeriod);
        }
    }

    /// <summary>
    ///     Defines empty and constant fields as finite zero arrays.
    /// </summary>
    /// <param name="width">The map width.</param>
    /// <param name="height">The map height.</param>
    /// <param name="octaves">The octave count.</param>
    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(1, 1, 8)]
    [InlineData(4, 3, 0)]
    [InlineData(0, 0, 1)]
    [InlineData(0, 7, 8)]
    [InlineData(7, 0, 8)]
    public void GenerateNoiseMapConstantTest(int width, int height, int octaves) {
        MathHelper.GenerateNoiseMap(width, height, octaves, out var actual);

        Assert.Equal(width * height, actual.Length);
        Assert.All(actual, value => Assert.Equal(0f, value));
    }

    /// <summary>
    ///     Compares every final pixel with the independently accumulated and normalized source field.
    /// </summary>
    /// <param name="width">The map width.</param>
    /// <param name="height">The map height.</param>
    /// <param name="octaves">The octave count.</param>
    [Theory]
    [InlineData(19, 13, 1)]
    [InlineData(19, 13, 4)]
    [InlineData(19, 13, 8)]
    [InlineData(1, 13, 5)]
    [InlineData(17, 1, 6)]
    public void GenerateNoiseMapFinalFieldTest(int width, int height, int octaves) {
        // Multiple fresh permutations discriminate intermediate-octave extrema in either field direction.
        for (var attempt = 0; attempt < 4; attempt++) {
            MathHelper.GenerateNoiseMap(width, height, octaves, out var actual);
            var permutation = ReadTable<int>("_permutation");
            var gradients = ReadTable<Vector2D<float>>("Gradients");
            var expected = new double[width * height];
            var frequency = 0.5f;
            var amplitude = 1f;
            for (var octave = 0; octave < octaves; octave++) {
                for (var offset = 0; offset < expected.Length; offset++) {
                    var x = (offset % width) * frequency / width;
                    var y = (offset / width) * frequency / height;
                    expected[offset] += EvaluateGradientSum(x, y, permutation, gradients) * amplitude;
                }
                frequency *= 2;
                amplitude /= 2;
            }
            var min = expected.Min();
            var max = expected.Max();
            var actualMin = actual.Min();
            var actualMax = actual.Max();

            Assert.Equal(expected.Length, actual.Length);
            Assert.Equal(0f, actualMin);
            Assert.Equal(1f, actualMax);
            for (var offset = 0; offset < actual.Length; offset++) {
                var normalized = (expected[offset] - min) / (max - min);
                Assert.True(float.IsFinite(actual[offset]));
                Assert.InRange(actual[offset], 0f, 1f);
                Assert.InRange(Math.Abs(actual[offset] - normalized), 0, 0.00002);
            }
        }
    }

    /// <summary>
    ///     Rejects invalid dimensions and octave counts before allocation or random-state changes.
    /// </summary>
    /// <param name="width">The map width.</param>
    /// <param name="height">The map height.</param>
    /// <param name="octaves">The octave count.</param>
    /// <param name="parameter">The rejected parameter.</param>
    [Theory]
    [InlineData(-1, 1, 1, "width")]
    [InlineData(1, -1, 1, "height")]
    [InlineData(1, 1, -1, "octaves")]
    [InlineData(1, 1, 130, "octaves")]
    [InlineData(3, 1, 129, "octaves")]
    [InlineData(2, 1, int.MaxValue, "octaves")]
    public void GenerateNoiseMapInvalidInputTest(int width, int height, int octaves, string parameter) {
        var error = Assert.Throws<ArgumentOutOfRangeException>(
            () => MathHelper.GenerateNoiseMap(width, height, octaves, out _));

        Assert.Equal(parameter, error.ParamName);
    }

    /// <summary>
    ///     Rejects overflowing pixel counts without allocating a wrapped array.
    /// </summary>
    [Fact]
    public void GenerateNoiseMapCountOverflowTest() {
        Assert.Throws<OverflowException>(() => MathHelper.GenerateNoiseMap(65536, 65536, 1, out _));
    }

    /// <summary>
    ///     Accepts the finite frequency boundary and ignores unused frequencies for empty maps.
    /// </summary>
    [Fact]
    public void GenerateNoiseMapFrequencyBoundaryTest() {
        MathHelper.GenerateNoiseMap(1, 1, 129, out var constant);
        MathHelper.GenerateNoiseMap(2, 1, 129, out var finite);
        MathHelper.GenerateNoiseMap(3, 1, 128, out var larger);
        MathHelper.GenerateNoiseMap(int.MaxValue, 0, int.MaxValue, out var empty);

        Assert.Equal(new float[] { 0 }, constant);
        Assert.All(finite, value => Assert.InRange(value, 0f, 1f));
        Assert.All(larger, value => Assert.InRange(value, 0f, 1f));
        Assert.Empty(empty);
    }

    /// <summary>
    ///     Rejects non-finite coordinates and supports finite floats beyond integer conversion range.
    /// </summary>
    [Fact]
    public void NoiseFloatingRangeTest() {
        foreach (var value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity }) {
            var horizontalError = Assert.Throws<ArgumentOutOfRangeException>(() => Noise2d.Noise(value, 0));
            var verticalError = Assert.Throws<ArgumentOutOfRangeException>(() => Noise2d.Noise(0, value));

            Assert.Equal("x", horizontalError.ParamName);
            Assert.Equal("y", verticalError.ParamName);
        }
        foreach (var value in new[] { float.MaxValue, float.MinValue, 2147483648f, -2147483648f }) {
            var actual = Noise2d.Noise(value, value);
            Assert.Equal(0f, actual);
        }
    }

    /// <summary>
    ///     Checks that public samples under bounded concurrent reseeding belong to a complete published table.
    /// </summary>
    /// <returns>A task completing after the bounded reader and publisher finish.</returns>
    [Fact]
    public async Task NoiseConcurrentReseedTest() {
        var tables = new List<int[]> { Noise2d.ReseedAndCapture() };
        var gradients = ReadTable<Vector2D<float>>("Gradients");
        var samples = new float[4096];
        var publisher = Task.Run(() => {
            for (var i = 0; i < 128; i++)
                tables.Add(Noise2d.ReseedAndCapture());
        }, TestContext.Current.CancellationToken);
        var reader = Task.Run(() => {
            for (var i = 0; i < samples.Length; i++)
                samples[i] = Noise2d.Noise(-1.25f, 2.375f);
        }, TestContext.Current.CancellationToken);
        await Task.WhenAll(publisher, reader);
        var allowed = tables.Select(table => EvaluateGradientSum(-1.25, 2.375, table, gradients)).ToArray();
        var gradientsAfter = ReadTable<Vector2D<float>>("Gradients");

        Assert.Equal(gradients, gradientsAfter);
        foreach (var table in tables) {
            var sorted = table.Order().ToArray();
            var expected = Enumerable.Range(0, 256).ToArray();
            Assert.Equal(expected, sorted);
        }
        foreach (var sample in samples) {
            var nearestError = allowed.Min(candidate => Math.Abs(candidate - sample));
            Assert.InRange(nearestError, 0, 0.000002);
        }

        var captured = tables[0];
        var before = Noise2d.Noise(-0.125f, 0.375f, captured);
        Noise2d.Reseed();
        var after = Noise2d.Noise(-0.125f, 0.375f, captured);
        Assert.Equal(before, after);
    }

    /// <summary>
    ///     Exercises actual maps during bounded concurrent map creation and public reseeding.
    /// </summary>
    /// <returns>A task completing after every finite normalized map has been checked.</returns>
    [Fact]
    public async Task GenerateNoiseMapConcurrentTest() {
        var workers = Enumerable.Range(0, 4).Select(_ => Task.Run(() => {
            for (var iteration = 0; iteration < 8; iteration++) {
                MathHelper.GenerateNoiseMap(17, 11, 8, out var map);
                var min = map.Min();
                var max = map.Max();

                Assert.Equal(187, map.Length);
                Assert.Equal(0f, min);
                Assert.Equal(1f, max);
                Assert.All(map, value => Assert.InRange(value, 0f, 1f));
            }
        }, TestContext.Current.CancellationToken)).ToArray();
        var publisher = Task.Run(() => {
            for (var i = 0; i < 128; i++)
                Noise2d.Reseed();
        }, TestContext.Current.CancellationToken);
        await Task.WhenAll(workers.Append(publisher));
    }

    /// <summary>
    ///     Reads the actual immutable tables solely to supply the independent mathematical oracle.
    /// </summary>
    /// <typeparam name="T">The table element type.</typeparam>
    /// <param name="name">The private table field name.</param>
    /// <returns>A copy of the current table.</returns>
    private static T[] ReadTable<T>(string name) {
        var field = typeof(Noise2d).GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!;
        return (T[])((T[])field.GetValue(null)!).Clone();
    }

    /// <summary>
    ///     Evaluates the literal four-corner gradient formula in double precision without production helpers.
    /// </summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="y">The vertical coordinate.</param>
    /// <param name="permutation">The actual permutation snapshot.</param>
    /// <param name="gradients">The fixed gradient table.</param>
    /// <returns>The clamped quintic-weighted gradient sum.</returns>
    private static double EvaluateGradientSum(double x, double y, int[] permutation, Vector2D<float>[] gradients) {
        var cellX = (int)Math.Floor(x);
        var cellY = (int)Math.Floor(y);
        var total = 0d;
        for (var dx = 0; dx <= 1; dx++) {
            for (var dy = 0; dy <= 1; dy++) {
                var column = ((cellX + dx) % 256 + 256) % 256;
                var row = ((permutation[column] + cellY + dy) % 256 + 256) % 256;
                var gradient = gradients[permutation[row]];
                var u = x - cellX - dx;
                var v = y - cellY - dy;
                var a = Math.Abs(u);
                var b = Math.Abs(v);
                var weightX = 1 - 10 * Math.Pow(a, 3) + 15 * Math.Pow(a, 4) - 6 * Math.Pow(a, 5);
                var weightY = 1 - 10 * Math.Pow(b, 3) + 15 * Math.Pow(b, 4) - 6 * Math.Pow(b, 5);
                total += weightX * weightY * (gradient.X * u + gradient.Y * v);
            }
        }
        return Math.Clamp(total, -1, 1);
    }
}
