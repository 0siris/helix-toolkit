using SharpAssimp;
using System.Numerics;
using SharpAssimp.Unmanaged;
using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core.Assimp;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Animations;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using Xunit;
using HxExporter = HelixToolkit.SharpDX.Core.Assimp.Exporter;
using HxAnimation = HelixToolkit.SharpDX.Core.Model.Animations.Animation;

namespace SilkAssimp.Tests;

/// <summary>Exercises primitive faces, native output and exporter failure ownership without a renderer or window.</summary>
public class AssimpExportPrimitiveOwnershipTests {
    /// <summary>
    ///     Exports independent literal one-, two- and three-segment fixtures without triangulating their indices.
    /// </summary>
    /// <param name="segments">The number of literal segments to export.</param>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void LinesRetainPairs(int segments) {
        int[] indices = segments switch { 1 => [0, 1], 2 => [0, 1, 2, 3], _ => [0, 1, 2, 3, 4, 5] };
        int[][] expected = segments switch { 1 => [[0, 1]], 2 => [[0, 1], [2, 3]], _ => [[0, 1], [2, 3], [4, 5]] };
        using var node = CreateNode(new LineGeometry3D { Positions = Positions(), Indices = new(indices) });
        using var exporter = new HxExporter();
        var result = exporter.ToAssimpScene(node, out var scene);
        Assert.Equal(ErrorCode.Succeed, result);
        AssertFaces(scene.Meshes.Single(), PrimitiveType.Line, expected);
    }

    /// <summary>
    ///     Point indices select individual vertices; absent and empty indices emit every position once in order.
    /// </summary>
    /// <param name="indexPolicy">Zero for absent indices, one for empty indices, two for explicit indices.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void PointsHaveIndividualFaces(int indexPolicy) {
        var geometry = new PointGeometry3D {
            Positions = new([new(2, 3, 4), new(5, 6, 7), new(8, 9, 10)]),
            Indices = indexPolicy switch { 0 => null, 1 => new(), _ => new([2, 0, 2]) }
        };
        int[][] expected = indexPolicy == 2 ? [[2], [0], [2]] : [[0], [1], [2]];
        using var node = CreateNode(geometry);
        using var exporter = new HxExporter();
        var result = exporter.ToAssimpScene(node, out var scene);
        Assert.Equal(ErrorCode.Succeed, result);
        AssertFaces(scene.Meshes.Single(), PrimitiveType.Point, expected);
    }

    /// <summary>
    ///     Rejects incomplete primitives and indices outside the actual vertex buffer before native marshaling.
    /// </summary>
    /// <param name="kind">The primitive kind.</param>
    /// <param name="indices">The invalid literal index list.</param>
    [Theory]
    [InlineData("line", new[] { 0, 1, 2 })]
    [InlineData("triangle", new[] { 0, 1 })]
    [InlineData("line", new[] { -1, 1 })]
    [InlineData("triangle", new[] { 0, 1, 6 })]
    [InlineData("point", new[] { 6 })]
    [InlineData("point", new[] { -1 })]
    public void InvalidPrimitiveIndicesFailBeforeNativeExport(string kind, int[] indices) {
        Geometry3D geometry = kind switch {
            "line" => new LineGeometry3D(),
            "point" => new PointGeometry3D(),
            _ => new MeshGeometry3D()
        };
        geometry.Positions = Positions();
        geometry.Indices = new(indices);
        using var node = CreateNode(geometry);
        using var exporter = new HxExporter();
        Assert.Throws<ArgumentException>(() => exporter.ToAssimpScene(node, out _));
    }

    /// <summary>Unindexed line and triangle buffers remain faceless rather than inventing connectivity.</summary>
    [Fact]
    public void OnlyUnindexedPointsGenerateFaces() {
        using var exporter = new HxExporter();
        using var line = CreateNode(new LineGeometry3D { Positions = Positions() });
        using var triangle = CreateNode(new MeshGeometry3D { Positions = Positions() });
        exporter.ToAssimpScene(line, out var lines);
        exporter.ToAssimpScene(triangle, out var triangles);
        Assert.Empty(lines.Meshes.Single().Faces);
        Assert.Empty(triangles.Meshes.Single().Faces);
    }

    /// <summary>Triangle winding, per-vertex attributes, material identity and real bone weights remain intact.</summary>
    [Fact]
    public void TriangleSkinningAndAttributesRemainIntact() {
        var geometry = new BoneSkinnedMeshGeometry3D {
            Positions = new([new(0, 0, 0), new(2, 0, 0), new(0, 3, 0)]), Indices = new([2, 0, 1]),
            Normals = new([new(0, 0, 1), new(0, 0, 1), new(0, 0, 1)]),
            Tangents = new([new(1, 0, 0), new(1, 0, 0), new(1, 0, 0)]),
            BiTangents = new([new(0, 1, 0), new(0, 1, 0), new(0, 1, 0)]),
            TextureCoordinates = new([new(0, 0), new(1, 0), new(0, 1)]),
            Colors = new([new(1, 0, 0, 1), new(0, 1, 0, 1), new(0, 0, 1, 1)]),
            VertexBoneIds = [new() { Bone1 = 0, Weights = new(1, 0, 0, 0) },
                new() { Bone1 = 0, Weights = new(0.25f, 0, 0, 0) },
                new() { Bone1 = 0, Weights = new(0.75f, 0, 0, 0) }]
        };
        using var node = new BoneSkinMeshNode {
            Geometry = geometry, Material = new PhongMaterialCore { Name = "Skin" },
            Bones = [new() { Name = "Joint", InvBindPose = Silk.NET.Maths.Matrix4X4<float>.Identity }]
        };
        using var exporter = new HxExporter();
        exporter.ToAssimpScene(node, out var scene);
        var mesh = scene.Meshes.Single();
        AssertFaces(mesh, PrimitiveType.Triangle, [[2, 0, 1]]);
        Assert.Equal("Skin", scene.Materials[mesh.MaterialIndex].Name);
        Assert.Equal(new Vector3(0, 3, 0), mesh.Vertices[2]);
        Assert.Equal(new Vector3(0, 0, 1), mesh.Normals[1]);
        Assert.Equal(new Vector3(1, 0, 0), mesh.Tangents[1]);
        Assert.Equal(new Vector3(0, 1, 0), mesh.BiTangents[1]);
        Assert.Equal(new Vector3(1, 0, 0), mesh.TextureCoordinateChannels[0][1]);
        Assert.Equal(new Vector4(0, 0, 1, 1), mesh.VertexColorChannels[0][2]);
        var bone = Assert.Single(mesh.Bones);
        Assert.Equal("Joint", bone.Name);
        Assert.Equal(new[] { 0, 1, 2 }, bone.VertexWeights.Select(weight => weight.VertexID));
        Assert.Equal(new[] { 1f, 0.25f, 0.75f }, bone.VertexWeights.Select(weight => weight.Weight));
    }

    /// <summary>
    ///     Native Assimp binary blob and file output retain line, point and triangle faces after context disposal.
    /// </summary>
    /// <param name="kind">The primitive exported through the actual native format.</param>
    [Theory]
    [InlineData("line")]
    [InlineData("point")]
    [InlineData("triangle")]
    public void NativeBlobAndFileRetainPrimitiveFaces(string kind) {
        Geometry3D geometry = kind switch {
            "line" => new LineGeometry3D { Indices = new([0, 1, 2, 3, 4, 5]) },
            "point" => new PointGeometry3D(),
            _ => new MeshGeometry3D { Indices = new([0, 1, 2, 3, 4, 5]) }
        };
        geometry.Positions = Positions();
        int[][] expected = kind switch {
            "line" => [[0, 1], [2, 3], [4, 5]],
            "point" => [[0], [1], [2], [3], [4], [5]],
            _ => [[0, 1, 2], [3, 4, 5]]
        };
        var primitive = kind switch { "line" => PrimitiveType.Line, "point" => PrimitiveType.Point, _ => PrimitiveType.Triangle };
        using var node = CreateNode(geometry);
        var path = Path.Combine(Path.GetTempPath(), $"SilkAssimp-Export-{Guid.NewGuid():N}.assbin");
        try {
            ExportDataBlob? blob;
            using (var exporter = new HxExporter()) {
                var blobResult = exporter.ExportToBlob(node, "assbin", out blob);
                var fileResult = exporter.ExportToFile(path, node, "assbin");
                Assert.Equal(ErrorCode.Succeed, blobResult);
                Assert.Equal(ErrorCode.Succeed, fileResult);
            }
            Assert.NotNull(blob);
            Assert.NotEmpty(blob.Data);
            using var stream = new MemoryStream(blob.Data);
            var fromBlob = AssimpLibrary.Instance.ImportFileFromStream(stream, PostProcessSteps.None, "assbin", IntPtr.Zero);
            AssertNativeScene(fromBlob, primitive, expected);
            var fromFile = AssimpLibrary.Instance.ImportFile(path, PostProcessSteps.None, IntPtr.Zero);
            AssertNativeScene(fromFile, primitive, expected);
        } finally {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    /// <summary>
    ///     Conversion exceptions preserve their identity and cannot retain a failed scene's temporary animations.
    /// </summary>
    /// <param name="external">Whether the caller supplies the real Assimp context.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedSceneConversionCanBeReusedWithoutStaleAnimations(bool external) {
        using var context = new AssimpContext();
        using var node = CreateNode(new MeshGeometry3D { Positions = Positions(), Indices = new([0, 1, 2]) });
        var scene = new HelixToolkitScene(node, [new HxAnimation(AnimationType.Node) { Name = "FailedAnimation" }]);
        using var exporter = new FailOnceExporter();
        exporter.Configuration.ExternalContext = external ? context : null;
        var events = new List<Exception>();
        exporter.AssimpExceptionOccurred += (_, error) => events.Add(error);
        var path = Path.Combine(Path.GetTempPath(), $"SilkAssimp-Failed-{Guid.NewGuid():N}.assbin");
        var error = Assert.Throws<InvalidOperationException>(() => exporter.ExportToFile(path, scene, "assbin"));
        Assert.Same(exporter.Failure, error);
        Assert.False(File.Exists(path));
        exporter.ToAssimpScene(node, out var reused);
        Assert.Empty(reused.Animations);
        var status = exporter.ExportToBlob(node, "assbin", out var blob);
        Assert.Equal(ErrorCode.Succeed, status);
        Assert.NotNull(blob);
        Assert.Empty(events);
        Assert.False(context.IsDisposed);
        using var stream = new MemoryStream(blob.Data);
        var native = context.ImportFileFromStream(stream, PostProcessSteps.None, "assbin");
        Assert.Empty(native.Animations);
    }

    /// <summary>
    ///     Unsupported formats and native errors retain status/event behavior and caller context ownership.
    /// </summary>
    /// <param name="external">Whether the caller supplies the real Assimp context.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FormatAndNativeFailuresPreserveCallerOwnership(bool external) {
        using var context = new AssimpContext();
        using var node = CreateNode(new MeshGeometry3D { Positions = Positions(), Indices = new([0, 1, 2]) });
        using var exporter = new HxExporter();
        exporter.Configuration.ExternalContext = external ? context : null;
        var errors = new List<Exception>();
        exporter.AssimpExceptionOccurred += (_, error) => errors.Add(error);
        var path = Path.Combine(Path.GetTempPath(), $"SilkAssimp-Invalid-{Guid.NewGuid():N}.assbin");
        var unsupported = exporter.ExportToFile(path + ".unsupported", node, "missing-format");
        Assert.Equal(ErrorCode.Failed | ErrorCode.FileTypeNotSupported, unsupported);
        Assert.Empty(errors);
        var failure = exporter.ExportToFile(path, node, "missing-format");
        Assert.Equal(ErrorCode.Failed, failure);
        Assert.Empty(errors);
        Assert.False(File.Exists(path));
        var result = exporter.ExportToBlob(node, "assbin", out var blob);
        Assert.Equal(ErrorCode.Succeed, result);
        Assert.NotNull(blob);
        exporter.Dispose();
        Assert.False(context.IsDisposed);
        using var stream = new MemoryStream(blob.Data);
        var imported = context.ImportFileFromStream(stream, PostProcessSteps.None, "assbin");
        AssertFaces(imported.Meshes.Single(), PrimitiveType.Triangle, [[0, 1, 2]]);
    }

    /// <summary>Exceptions from the real context remain observable through the existing export failure event.</summary>
    [Fact]
    public void DisposedExternalContextReportsNativeCallException() {
        using var context = new AssimpContext();
        context.Dispose();
        using var node = CreateNode(new PointGeometry3D { Positions = Positions() });
        using var exporter = new HxExporter();
        exporter.Configuration.ExternalContext = context;
        var errors = new List<Exception>();
        exporter.AssimpExceptionOccurred += (_, error) => errors.Add(error);
        var status = exporter.ExportToBlob(node, "assbin", out var blob);
        Assert.Equal(ErrorCode.Failed, status);
        Assert.Null(blob);
        var error = Assert.Single(errors);
        Assert.IsType<ObjectDisposedException>(error);
        exporter.Configuration.ExternalContext = null;
        var reused = exporter.ExportToBlob(node, "assbin", out var successful);
        Assert.Equal(ErrorCode.Succeed, reused);
        Assert.NotNull(successful);
    }

    /// <summary>Blob conversion failures also preserve exception behavior and permit a successful retry.</summary>
    [Fact]
    public void BlobConversionFailureCanBeRetried() {
        using var node = CreateNode(new PointGeometry3D { Positions = Positions() });
        using var exporter = new FailOnceExporter();
        var error = Assert.Throws<InvalidOperationException>(() => exporter.ExportToBlob(node, "assbin", out _));
        Assert.Same(exporter.Failure, error);
        var result = exporter.ExportToBlob(node, "assbin", out var blob);
        Assert.Equal(ErrorCode.Succeed, result);
        Assert.NotNull(blob);
    }

    /// <summary>An empty point buffer cannot invent native vertices or point faces.</summary>
    [Fact]
    public void EmptyPointsRemainEmpty() {
        using var node = CreateNode(new PointGeometry3D());
        using var exporter = new HxExporter();
        exporter.ToAssimpScene(node, out var scene);
        var mesh = scene.Meshes.Single();
        Assert.Empty(mesh.Vertices);
        AssertFaces(mesh, PrimitiveType.Point, []);
    }

    /// <summary>Actual Collada output preserves the physical placement of a triangle below a transformed parent.</summary>
    [Fact]
    public void NativeColladaRetainsPhysicalChildPlacement() {
        using var root = new GroupNode {
            Name = "Parent", ModelMatrix = new(0, 1, 0, 0, -1, 0, 0, 0, 0, 0, 1, 0, 10, -2, 7, 1)
        };
        using var child = CreateNode(new MeshGeometry3D {
            Positions = new([new(0, 0, 0), new(2, 0, 0), new(0, 3, 0)]), Indices = new([0, 1, 2])
        });
        child.Name = "Placed";
        child.ModelMatrix = new(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 3, 4, 5, 1);
        root.AddChildNode(child);
        ExportDataBlob? blob;
        using (var exporter = new HxExporter()) {
            var status = exporter.ExportToBlob(root, "collada", out blob);
            Assert.Equal(ErrorCode.Succeed, status);
        }
        Assert.NotNull(blob);
        using var context = new AssimpContext();
        using var stream = new MemoryStream(blob.Data);
        var imported = context.ImportFileFromStream(stream, PostProcessSteps.None, "dae");
        var placed = imported.RootNode.FindNode("Placed");
        Assert.NotNull(placed);
        var mesh = imported.Meshes[placed.MeshIndices.Single()];
        var first = mesh.Vertices[0];
        var second = mesh.Vertices[1];
        for (var current = placed; current != null; current = current.Parent) {
            first = Vector3.Transform(first, Matrix4x4.Transpose(current.Transform));
            second = Vector3.Transform(second, Matrix4x4.Transpose(current.Transform));
        }
        Assert.Equal(new Vector3(6, 1, 12), first);
        Assert.Equal(new Vector3(6, 3, 12), second);
        AssertFaces(mesh, PrimitiveType.Triangle, [[0, 1, 2]]);
    }

    /// <summary>A successful animated file retains real keyframes without contaminating the next root-only export.</summary>
    [Fact]
    public void SuccessfulAnimatedSceneClearsTemporaryState() {
        using var root = new GroupNode { Name = "Root" };
        using var node = CreateNode(new MeshGeometry3D { Positions = Positions(), Indices = new([0, 1, 2]) });
        node.Name = "Animated";
        root.AddChildNode(node);
        var animation = new HxAnimation(AnimationType.Node) {
            Name = "Move", StartTime = 0, EndTime = 1,
            NodeAnimationCollection = [new() {
                Node = node, KeyFrames = new([
                    new() { Time = 0, Translation = new(0, 0, 0), Scale = new(1, 1, 1), Rotation = new(0, 0, 0, 1) },
                    new() { Time = 1, Translation = new(3, 4, 5), Scale = new(1, 1, 1), Rotation = new(0, 0, 0, 1) }
                ])
            }]
        };
        var scene = new HelixToolkitScene(root, [animation]);
        var path = Path.Combine(Path.GetTempPath(), $"SilkAssimp-Animated-{Guid.NewGuid():N}.assbin");
        try {
            using var exporter = new HxExporter();
            var status = exporter.ExportToFile(path, scene, "assbin");
            Assert.Equal(ErrorCode.Succeed, status);
            using var context = new AssimpContext();
            var imported = context.ImportFile(path, PostProcessSteps.None);
            var nativeAnimation = Assert.Single(imported.Animations);
            var channel = Assert.Single(nativeAnimation.NodeAnimationChannels);
            Assert.Equal("Move", nativeAnimation.Name);
            Assert.Equal(new Vector3(3, 4, 5), channel.PositionKeys[1].Value);
            exporter.ToAssimpScene(root, out var reused);
            Assert.Empty(reused.Animations);
        } finally {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    /// <summary>
    ///     Creates real geometry nodes using their existing material owners.
    /// </summary>
    /// <param name="geometry">The source geometry.</param>
    /// <returns>The disposable scene node.</returns>
    private static MaterialGeometryNode CreateNode(Geometry3D geometry) => geometry switch {
        LineGeometry3D => new LineNode { Geometry = geometry, Material = new LineMaterialCore() },
        PointGeometry3D => new PointNode { Geometry = geometry, Material = new PointMaterialCore() },
        _ => new MeshNode { Geometry = geometry, Material = new PhongMaterialCore() }
    };

    /// <summary>
    ///     Supplies six distinct vertices so each primitive has an independent literal index oracle.
    /// </summary>
    /// <returns>The six positions.</returns>
    private static Vector3Collection Positions() => new([
        new(0, 0, 0), new(2, 0, 0), new(0, 3, 0), new(0, 0, 4), new(5, 0, 4), new(0, 6, 4)
    ]);

    /// <summary>
    ///     Compares native or managed mesh faces with independently specified literal connectivity.
    /// </summary>
    /// <param name="mesh">The actual converted mesh.</param>
    /// <param name="primitive">The expected primitive type.</param>
    /// <param name="expected">The expected face indices.</param>
    private static void AssertFaces(Mesh mesh, PrimitiveType primitive, int[][] expected) {
        Assert.Equal(primitive, mesh.PrimitiveType);
        AssertConnectivity(mesh, expected);
    }

    /// <summary>
    ///     Compares literal faces without relying on AssimpNet's incomplete native primitive flag deserialization.
    /// </summary>
    /// <param name="mesh">The actual converted mesh.</param>
    /// <param name="expected">The expected face indices.</param>
    private static void AssertConnectivity(Mesh mesh, int[][] expected) {
        Assert.Equal(expected.Length, mesh.FaceCount);
        for (var i = 0; i < expected.Length; i++) Assert.Equal(expected[i], mesh.Faces[i].Indices);
    }

    /// <summary>
    ///     Reads actual native primitive flags before managed conversion and always releases the imported scene.
    /// </summary>
    /// <param name="scene">The native imported scene owned by this assertion.</param>
    /// <param name="primitive">The expected primitive flag.</param>
    /// <param name="expected">The independently specified face indices.</param>
    private static void AssertNativeScene(IntPtr scene, PrimitiveType primitive, int[][] expected) {
        try {
            Assert.NotEqual(IntPtr.Zero, scene);
            var nativeScene = MemoryHelper.Read<AiScene>(scene);
            Assert.Equal(1u, nativeScene.NumMeshes);
            var nativeMesh = MemoryHelper.Read<AiMesh>(Marshal.ReadIntPtr(nativeScene.Meshes));
            Assert.Equal(primitive, nativeMesh.PrimitiveTypes);
            var managed = Scene.FromUnmanagedScene(scene);
            AssertConnectivity(managed.Meshes.Single(), expected);
        } finally {
            if (scene != IntPtr.Zero) AssimpLibrary.Instance.ReleaseImport(scene);
        }
    }

    /// <summary>Exercises the existing virtual conversion extension point once, then performs real conversion.</summary>
    private sealed class FailOnceExporter : HxExporter {
        /// Gets the exact conversion exception expected by the caller.
        public InvalidOperationException Failure { get; } = new("Conversion failed once");

        /// Tracks whether the single conversion failure has already occurred.
        private bool failed;

        protected override Mesh OnCreateAssimpMesh(MeshInfo info) {
            if (!failed) {
                failed = true;
                throw Failure;
            }
            return base.OnCreateAssimpMesh(info);
        }
    }
}