using SharpAssimp;
using System.Numerics;
using HelixToolkit.SharpDX.Core.Assimp;
using HelixToolkit.SharpDX.Core.Model.Animations;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene;
using Xunit;
using HxAnimation = HelixToolkit.SharpDX.Core.Model.Animations.Animation;
using HxExporter = HelixToolkit.SharpDX.Core.Assimp.Exporter;

namespace SilkAssimp.Tests;

/// <summary>Serializes changes to the source-compatible process-wide export tick rate.</summary>
[CollectionDefinition("Export timebase", DisableParallelization = true)]
public sealed class ExportTimebaseCollection;

/// <summary>Verifies configurable animation timing through real native assbin export and reimport.</summary>
[Collection("Export timebase")]
public sealed class ExporterAnimationTimebaseTests {
    /// <summary>The unchanged export default remains thirty ticks per second.</summary>
    [Fact]
    public void DefaultTimebaseIsThirtyTest() {
        var field = typeof(HxExporter).GetField(nameof(HxExporter.DefaultTicksPerSecond))!;
        var value = field.GetValue(null);
        Assert.Equal(30d, value);
    }

    /// <summary>
    ///     Retains duration, component timestamps, and actual values at the selected source-compatible tick rate.
    /// </summary>
    /// <param name="rate">The mutable public export rate in ticks per second.</param>
    [Theory]
    [InlineData(30d)]
    [InlineData(60d)]
    [InlineData(24d)]
    public void ConfiguredTimebaseSurvivesNativeFileTest(double rate) {
        var field = typeof(HxExporter).GetField(nameof(HxExporter.DefaultTicksPerSecond))!;
        var original = field.GetValue(null);
        Assert.False(field.IsLiteral);
        using var root = new GroupNode { Name = "Root" };
        using var node = new MeshNode {
            Name = "Animated",
            Geometry = new MeshGeometry3D {
                Positions = new([new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)]), Indices = new([0, 1, 2])
            },
            Material = new PhongMaterialCore()
        };
        root.AddChildNode(node);
        var animation = new HxAnimation(AnimationType.Node) {
            Name = "Motion", StartTime = 0, EndTime = 2,
            NodeAnimationCollection = [new() {
                Node = node,
                KeyFrames = new([
                    new() { Time = 0, Translation = new(2, 4, 6), Scale = new(1, 1, 1), Rotation = new(0, 0, 0, 1) },
                    new() { Time = 0.5f, Translation = new(3, 5, 7), Scale = new(2, 3, 4), Rotation = new(0, 0, 0, 1) },
                    new() { Time = 2, Translation = new(8, 9, 10), Scale = new(4, 5, 6), Rotation = new(0, 0, 1, 0) }
                ])
            }]
        };
        var scene = new HelixToolkitScene(root, [animation]);
        var path = Path.Combine(Path.GetTempPath(), $"SilkAssimp-Timebase-{Guid.NewGuid():N}.assbin");
        try {
            field.SetValue(null, rate);
            using var exporter = new HxExporter();
            var result = exporter.ExportToFile(path, scene, "assbin");
            using var context = new AssimpContext();
            var imported = context.ImportFile(path, PostProcessSteps.None);
            var actual = Assert.Single(imported.Animations);
            var channel = Assert.Single(actual.NodeAnimationChannels);
            var positionTimes = channel.PositionKeys.Select(key => key.Time).ToArray();
            var rotationTimes = channel.RotationKeys.Select(key => key.Time).ToArray();
            var scaleTimes = channel.ScalingKeys.Select(key => key.Time).ToArray();
            double[] expected = [0, rate / 2, rate * 2];
            Assert.Equal(ErrorCode.Succeed, result);
            Assert.Equal(rate, actual.TicksPerSecond);
            Assert.Equal(rate * 2, actual.DurationInTicks);
            Assert.Equal(expected, positionTimes);
            Assert.Equal(expected, rotationTimes);
            Assert.Equal(expected, scaleTimes);
            Assert.Equal(new Vector3(3, 5, 7), channel.PositionKeys[1].Value);
            Assert.Equal(new Vector3(4, 5, 6), channel.ScalingKeys[2].Value);
            Assert.Equal(2d, channel.PositionKeys[2].Time / actual.TicksPerSecond);
            exporter.ToAssimpScene(root, out var reused);
            Assert.Empty(reused.Animations);
        } finally {
            field.SetValue(null, original);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
