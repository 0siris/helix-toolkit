using Assimp;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Animations;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using Xunit;
using HxAnimation = HelixToolkit.SharpDX.Core.Model.Animations.Animation;
using HxImporter = HelixToolkit.SharpDX.Core.Assimp.Importer;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Rotation = Silk.NET.Maths.Quaternion<float>;
using Vector = Silk.NET.Maths.Vector3D<float>;

namespace HelixToolkit.SharpDX.Core.Assimp;

/// <summary>Exercises independent Assimp channels through the real importer and animation consumer.</summary>
public sealed class ImporterAnimationTests {
    /// <summary>Two position keys retain their times, defaults, node identity, and interpolated placement.</summary>
    [Fact]
    public void PositionOnlyNonzeroTimesTest() {
        var channel = new NodeAnimationChannel { NodeName = "Animated" };
        channel.PositionKeys.AddRange([
            new VectorKey(5, new Vector3D(2, 4, 6)),
            new VectorKey(15, new Vector3D(10, 12, 14))
        ]);
        using var importer = CreateImporter();
        var scene = CreateScene(channel, 10, 20);
        var animation = Import(importer, scene);
        var track = Assert.Single(animation.NodeAnimationCollection);
        var times = track.KeyFrames.Select(k => k.Time).ToArray();
        var updaters = new[] { animation }.CreateAnimationUpdaters();
        var updater = updaters["Motion"];

        Assert.Equal(new[] { 0.5f, 1.5f }, times);
        Assert.Same(importer.SceneNodes.Single(n => n.Name == "Animated"), track.Node);
        Assert.True(track.Node.IsAnimationNode);
        Assert.Equal(0, channel.RotationKeyCount);
        Assert.Equal(0, channel.ScalingKeyCount);
        foreach (var frame in track.KeyFrames) {
            Assert.Equal(Rotation.Identity, frame.Rotation);
            Assert.Equal(Vector.One, frame.Scale);
        }
        updater.Update(0.25f, 1);
        Assert.Equal(77f, track.Node.ModelMatrix.M41);
        AssertPose(updater, track.Node, 0.5f, new Vector(2, 4, 6), 0, Vector.One);
        AssertPose(updater, track.Node, 1, new Vector(6, 8, 10), 0, Vector.One);
        AssertPose(updater, track.Node, 1.5f, new Vector(10, 12, 14), 0, Vector.One);
    }

    /// <summary>
    ///     Unequal disjoint channels retain the entire timeline and interpolated TRS values.
    /// </summary>
    /// <param name="parallel">Whether the existing parallel animation loader is enabled.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnequalDisjointTimelinesTest(bool parallel) {
        var channel = new NodeAnimationChannel { NodeName = "Animated" };
        channel.PositionKeys.AddRange([
            new VectorKey(0, new Vector3D(0, 2, 4)),
            new VectorKey(6, new Vector3D(12, 14, 16))
        ]);
        channel.RotationKeys.AddRange([RotationKey(1, 0), RotationKey(3, 60), RotationKey(7, 180)]);
        channel.ScalingKeys.AddRange([
            new VectorKey(2, new Vector3D(2, 3, 4)),
            new VectorKey(4, new Vector3D(4, 5, 6))
        ]);
        using var importer = CreateImporter(parallel);
        var animation = Import(importer, CreateScene(channel));
        var track = Assert.Single(animation.NodeAnimationCollection);
        var times = track.KeyFrames.Select(k => k.Time).ToArray();
        var updater = new NodeAnimationUpdater(animation);

        Assert.Equal(new[] { 0f, 1, 2, 3, 4, 6, 7 }, times);
        AssertPose(updater, track.Node, 0, new Vector(0, 2, 4), 0, new Vector(2, 3, 4));
        AssertPose(updater, track.Node, 3, new Vector(6, 8, 10), 60, new Vector(3, 4, 5));
        AssertPose(updater, track.Node, 3.5f, new Vector(7, 9, 11), 75, new Vector(3.5f, 4.5f, 5.5f));
        AssertPose(updater, track.Node, 6.5f, new Vector(12, 14, 16), 165, new Vector(4, 5, 6));
        AssertPose(updater, track.Node, 7, new Vector(12, 14, 16), 180, new Vector(4, 5, 6));
    }

    /// <summary>Matching counts do not imply matching key times.</summary>
    [Fact]
    public void EqualCountsDifferentTimesTest() {
        var channel = new NodeAnimationChannel { NodeName = "Animated" };
        channel.PositionKeys.AddRange([
            new VectorKey(0, new Vector3D(0, 2, 4)), new VectorKey(4, new Vector3D(8, 10, 12))
        ]);
        channel.RotationKeys.AddRange([RotationKey(1, 0), RotationKey(5, 120)]);
        channel.ScalingKeys.AddRange([
            new VectorKey(2, new Vector3D(2, 3, 4)), new VectorKey(6, new Vector3D(6, 7, 8))
        ]);
        using var importer = CreateImporter();
        var animation = Import(importer, CreateScene(channel));
        var track = Assert.Single(animation.NodeAnimationCollection);
        var times = track.KeyFrames.Select(k => k.Time).ToArray();
        var updater = new NodeAnimationUpdater(animation);

        Assert.Equal(new[] { 0f, 1, 2, 4, 5, 6 }, times);
        AssertPose(updater, track.Node, 2, new Vector(4, 6, 8), 30, new Vector(2, 3, 4));
        AssertPose(updater, track.Node, 4, new Vector(8, 10, 12), 90, new Vector(4, 5, 6));
        AssertPose(updater, track.Node, 5.5f, new Vector(8, 10, 12), 120, new Vector(5.5f, 6.5f, 7.5f));
    }

    /// <summary>Shared and duplicate timestamps emit one frame, with the last duplicate value winning.</summary>
    [Fact]
    public void SharedDuplicateTimesTest() {
        var channel = new NodeAnimationChannel { NodeName = "Animated" };
        channel.PositionKeys.AddRange([
            new VectorKey(0, new Vector3D(1, 2, 3)),
            new VectorKey(0, new Vector3D(2, 3, 4)),
            new VectorKey(2, new Vector3D(4, 5, 6)),
            new VectorKey(2, new Vector3D(6, 7, 8))
        ]);
        channel.RotationKeys.AddRange([RotationKey(0, 0), RotationKey(2, 60), RotationKey(2, 90)]);
        channel.ScalingKeys.AddRange([
            new VectorKey(0, new Vector3D(2, 3, 4)),
            new VectorKey(1, new Vector3D(3, 4, 5)),
            new VectorKey(2, new Vector3D(4, 5, 6))
        ]);
        using var importer = CreateImporter();
        var animation = Import(importer, CreateScene(channel));
        var track = Assert.Single(animation.NodeAnimationCollection);
        var times = track.KeyFrames.Select(k => k.Time).ToArray();
        var updater = new NodeAnimationUpdater(animation);

        Assert.Equal(new[] { 0f, 1, 2 }, times);
        AssertPose(updater, track.Node, 0, new Vector(2, 3, 4), 0, new Vector(2, 3, 4));
        AssertPose(updater, track.Node, 1, new Vector(4, 5, 6), 45, new Vector(3, 4, 5));
        AssertPose(updater, track.Node, 2, new Vector(6, 7, 8), 90, new Vector(4, 5, 6));
        Assert.Equal(4, channel.PositionKeyCount);
        Assert.Equal(3, channel.RotationKeyCount);
    }

    /// <summary>Real single-key components remain constant across a multi-key position channel.</summary>
    [Fact]
    public void ConstantComponentsTest() {
        var channel = new NodeAnimationChannel { NodeName = "Animated" };
        channel.PositionKeys.AddRange([
            new VectorKey(2, new Vector3D(2, 4, 6)), new VectorKey(4, new Vector3D(6, 8, 10))
        ]);
        channel.RotationKeys.AddRange([RotationKey(3, 15), RotationKey(3, 60)]);
        channel.ScalingKeys.Add(new VectorKey(3, new Vector3D(1, 1, 1)));
        channel.ScalingKeys.Add(new VectorKey(3, new Vector3D(2, 3, 4)));
        using var importer = CreateImporter();
        var animation = Import(importer, CreateScene(channel));
        var track = Assert.Single(animation.NodeAnimationCollection);
        var times = track.KeyFrames.Select(k => k.Time).ToArray();
        var updater = new NodeAnimationUpdater(animation);

        Assert.Equal(new[] { 2f, 3, 4 }, times);
        AssertPose(updater, track.Node, 2, new Vector(2, 4, 6), 60, new Vector(2, 3, 4));
        AssertPose(updater, track.Node, 3, new Vector(4, 6, 8), 60, new Vector(2, 3, 4));
        AssertPose(updater, track.Node, 4, new Vector(6, 8, 10), 60, new Vector(2, 3, 4));
    }

    /// <summary>
    ///     Single rotation or scale channels keep their values and the missing-component defaults.
    /// </summary>
    /// <param name="rotationOnly">Whether the fixture contains only rotation keys.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MissingPositionDefaultsTest(bool rotationOnly) {
        var channel = new NodeAnimationChannel { NodeName = "Animated" };
        if (rotationOnly)
            channel.RotationKeys.AddRange([RotationKey(0, 30), RotationKey(2, 90)]);
        else
            channel.ScalingKeys.AddRange([
                new VectorKey(0, new Vector3D(2, 3, 4)), new VectorKey(2, new Vector3D(4, 5, 6))
            ]);
        using var importer = CreateImporter();
        var animation = Import(importer, CreateScene(channel));
        var track = Assert.Single(animation.NodeAnimationCollection);
        var updater = new NodeAnimationUpdater(animation);

        Assert.Equal(2, track.KeyFrames.Count);
        AssertPose(updater, track.Node, 1, Vector.Zero, rotationOnly ? 60 : 0,
            rotationOnly ? Vector.One : new Vector(3, 4, 5));
        Assert.Equal(0, channel.PositionKeyCount);
    }

    /// <summary>
    ///     File timing wins when present; configured fallback changes both actual key times and duration.
    /// </summary>
    /// <param name="fileTicks">The file's tick rate, or zero to request the configured fallback.</param>
    /// <param name="configuredTicks">The configured fallback rate.</param>
    /// <param name="expectedEnd">The expected final key time in seconds.</param>
    [Theory]
    [InlineData(0, 25, 2)]
    [InlineData(0, 50, 1)]
    [InlineData(10, 50, 5)]
    [InlineData(60, 25, 0.833333333)]
    public void ConfiguredTimebaseTest(double fileTicks, float configuredTicks, float expectedEnd) {
        var channel = new NodeAnimationChannel { NodeName = "Animated" };
        channel.PositionKeys.AddRange([
            new VectorKey(0, new Vector3D(2, 4, 6)), new VectorKey(50, new Vector3D(10, 12, 14))
        ]);
        using var importer = CreateImporter();
        importer.Configuration.TickesPerSecond = configuredTicks;
        var animation = Import(importer, CreateScene(channel, fileTicks, 50));
        var track = Assert.Single(animation.NodeAnimationCollection);
        var updater = new NodeAnimationUpdater(animation);

        Assert.Equal(expectedEnd, track.KeyFrames[1].Time, 5);
        Assert.Equal(expectedEnd, animation.EndTime, 5);
        AssertPose(updater, track.Node, expectedEnd / 2, new Vector(6, 8, 10), 0, Vector.One);
    }

    /// <summary>Node and morph animations keep their shared name, bone association, and grouped consumer updates.</summary>
    [Fact]
    public void BoneMorphGroupingTest() {
        var channel = new NodeAnimationChannel { NodeName = "Animated" };
        channel.PositionKeys.AddRange([
            new VectorKey(0, new Vector3D(2, 4, 6)), new VectorKey(2, new Vector3D(10, 12, 14))
        ]);
        var source = CreateScene(channel, duration: 2);
        var mesh = source.Meshes[0];
        var bone = new global::Assimp.Bone { Name = "Animated", OffsetMatrix = Matrix4x4.Identity };
        bone.VertexWeights.Add(new VertexWeight(0, 1));
        mesh.Bones.Add(bone);
        var attachment = new MeshAnimationAttachment { Name = "Raised" };
        attachment.Vertices.AddRange(mesh.Vertices.Select(v => new Vector3D(v.X, v.Y, v.Z + 1)));
        mesh.MeshAnimationAttachments.Add(attachment);
        var morph = new MeshMorphAnimationChannel { Name = "Animated*0" };
        var first = new MeshMorphKey { Time = 0 };
        first.Values.Add(0);
        first.Weights.Add(0.25);
        var last = new MeshMorphKey { Time = 2 };
        last.Values.Add(0);
        last.Weights.Add(0.75);
        morph.MeshMorphKeys.AddRange([first, last]);
        source.Animations[0].MeshMorphAnimationChannels.Add(morph);
        using var importer = CreateImporter();

        var result = importer.ToHelixToolkitScene(source, out var imported);
        Assert.Equal(ErrorCode.Succeed, result);
        Assert.NotNull(imported);
        var nodeAnimation = imported.Animations.Single(a => a.AnimationType == AnimationType.Node);
        var morphAnimation = imported.Animations.Single(a => a.AnimationType == AnimationType.MorphTarget);
        var node = Assert.Single(nodeAnimation.NodeAnimationCollection).Node;
        var skin = Assert.IsType<BoneSkinMeshNode>(morphAnimation.RootNode);
        var importedBone = Assert.Single(skin.Bones!);
        var updaters = imported.Animations.CreateAnimationUpdaters();
        var group = Assert.IsType<AnimationGroupUpdater>(updaters["Motion"]);

        Assert.Equal(2, group.Children.Count);
        Assert.Same(node, importedBone.Node);
        Assert.Same(skin, Assert.Single(nodeAnimation.BoneSkinMeshes));
        AssertPose(group, node, 1, new Vector(6, 8, 10), 0, Vector.One);
        Assert.Equal(0.5f, skin.MorphTargetWeights[0], 5);
        AssertPose(group, node, 2, new Vector(10, 12, 14), 0, Vector.One);
        Assert.Equal(0.75f, skin.MorphTargetWeights[0], 5);
    }

    /// <summary>
    ///     Creates an importer without GPU, skeleton, texture, or octree initialization.
    /// </summary>
    /// <param name="parallel">Whether the existing parallel loader is enabled.</param>
    /// <returns>The owning importer.</returns>
    private static HxImporter CreateImporter(bool parallel = false) => new() {
        Configuration = new ImporterConfiguration { BuildOctree = false, EnableParallelProcessing = parallel }
    };

    /// <summary>
    ///     Builds an independent Assimp scene with a real triangle and an animated group.
    /// </summary>
    /// <param name="channel">The source channel.</param>
    /// <param name="ticks">The source tick rate.</param>
    /// <param name="duration">The source duration in ticks.</param>
    /// <returns>The source scene.</returns>
    private static Scene CreateScene(NodeAnimationChannel channel, double ticks = 1, double duration = 7) {
        var scene = new Scene { RootNode = new Node("Root") };
        var node = new Node("Animated", scene.RootNode) {
            Transform = new Matrix4x4(1, 0, 0, 77, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1)
        };
        scene.RootNode.Children.Add(node);
        var mesh = new Mesh("Triangle", PrimitiveType.Triangle) { MaterialIndex = 0 };
        mesh.Vertices.AddRange([new Vector3D(0, 0, 0), new Vector3D(1, 0, 0), new Vector3D(0, 1, 0)]);
        mesh.Faces.Add(new Face([0, 1, 2]));
        scene.Meshes.Add(mesh);
        scene.Materials.Add(new Material { Name = "Plain", ShadingMode = ShadingMode.Blinn });
        node.MeshIndices.Add(0);
        var animation = new global::Assimp.Animation {
            Name = "Motion", DurationInTicks = duration, TicksPerSecond = ticks
        };
        animation.NodeAnimationChannels.Add(channel);
        scene.Animations.Add(animation);
        return scene;
    }

    /// <summary>
    ///     Imports through the public scene path and retrieves the original named animation.
    /// </summary>
    /// <param name="importer">The owning importer.</param>
    /// <param name="scene">The source Assimp scene.</param>
    /// <returns>The imported node animation.</returns>
    private static HxAnimation Import(HxImporter importer, Scene scene) {
        var result = importer.ToHelixToolkitScene(scene, out var imported);
        Assert.Equal(ErrorCode.Succeed, result);
        Assert.NotNull(imported);
        var animation = Assert.Single(imported.Animations);
        Assert.Equal("Motion", animation.Name);
        Assert.Same(animation, Assert.Single(importer.Animations));
        return animation;
    }

    /// <summary>
    ///     Creates a literal Z-axis rotation key using the Assimp WXYZ boundary order.
    /// </summary>
    /// <param name="time">The key's source time.</param>
    /// <param name="degrees">The Z-axis angle in degrees.</param>
    /// <returns>The Assimp rotation key.</returns>
    private static QuaternionKey RotationKey(double time, double degrees) {
        var halfAngle = degrees * Math.PI / 360;
        return new QuaternionKey(time, new global::Assimp.Quaternion((float) Math.Cos(halfAngle),
            0, 0, (float) Math.Sin(halfAngle)));
    }

    /// <summary>
    ///     Checks consumer output against an analytic row-vector TRS matrix, including nonuniform scale.
    /// </summary>
    /// <param name="updater">The actual animation consumer.</param>
    /// <param name="node">The imported animated node.</param>
    /// <param name="time">The sample time in seconds.</param>
    /// <param name="translation">The expected translation.</param>
    /// <param name="degrees">The expected Z rotation in degrees.</param>
    /// <param name="scale">The expected nonuniform scale.</param>
    private static void AssertPose(IAnimationUpdater updater, SceneNode node, float time,
        Vector translation, double degrees, Vector scale) {
        updater.Update(time, 1);
        var c = (float) Math.Cos(degrees * Math.PI / 180);
        var s = (float) Math.Sin(degrees * Math.PI / 180);
        var expected = new Matrix(scale.X * c, scale.X * s, 0, 0,
            -scale.Y * s, scale.Y * c, 0, 0, 0, 0, scale.Z, 0,
            translation.X, translation.Y, translation.Z, 1);
        var actual = node.ModelMatrix;
        for (var row = 0; row < 4; ++row)
            for (var column = 0; column < 4; ++column)
                Assert.Equal(expected[row, column], actual[row, column], 4);
    }
}
